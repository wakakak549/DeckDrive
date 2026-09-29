// DeckDrive Tray - v0.3
// v0.1: tray skeleton + manual mount/unmount + smart drive letters
// v0.2: auto mount/unmount on plug/unplug (doorbell = WM_DEVICECHANGE, identity = ping)
// v0.3: first-run wizard (replaces setup.ps1) + zh/en language switch
// Target: .NET Framework 4.8 (preinstalled on Windows 10/11), no dependencies.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace DeckDrive
{
    static class Program
    {
        private static Mutex singleInstance;

        [STAThread]
        static void Main()
        {
            bool created;
            singleInstance = new Mutex(true, "DeckDriveTray_SingleInstance", out created);
            if (!created)
            {
                Loc.Init(null);
                MessageBox.Show(Loc.T("AlreadyRunning"), Loc.T("AppTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
            GC.KeepAlive(singleInstance);
        }
    }

    // Hidden form whose only job is receiving WM_DEVICECHANGE (USB doorbell).
    internal class MessageForm : Form
    {
        public const int WM_DEVICECHANGE = 0x0219;
        public event EventHandler DeviceChanged;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DEVICECHANGE && DeviceChanged != null)
                DeviceChanged(this, EventArgs.Empty);
            base.WndProc(ref m);
        }
    }

    public class TrayApp : ApplicationContext
    {
        private NotifyIcon tray;
        private ToolStripMenuItem miStatus, miMount, miUnmount, miAuto, miLangZh, miLangEn;
        private MessageForm hiddenForm;
        private Icon iconIdle, iconBusy, iconMounted;
        private WizardForm wizard;

        private System.Windows.Forms.Timer debounceTimer;   // merges device-event storms
        private System.Windows.Forms.Timer watchdogTimer;   // periodic state check

        private readonly string cfgDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeckDrive");
        private string CfgPath { get { return Path.Combine(cfgDir, "config.ini"); } }
        private readonly Dictionary<string, string> cfg = new Dictionary<string, string>();

        private readonly List<Process> mountProcs = new List<Process>();
        private string rclonePath;
        private char homeLetter, sdLetter;
        private bool mounted, sdMounted;
        private bool busy;
        private bool manualSuppress;   // manual unmount wins until real unplug
        private bool autoMountEnabled = true;

        public TrayApp()
        {
            hiddenForm = new MessageForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized,
                FormBorderStyle = FormBorderStyle.FixedToolWindow
            };
            hiddenForm.Show();
            hiddenForm.Hide();
            hiddenForm.DeviceChanged += (s, e) => RestartDebounce();

            iconIdle = MakeIcon(Color.FromArgb(120, 120, 120));
            iconBusy = MakeIcon(Color.FromArgb(230, 145, 40));
            iconMounted = MakeIcon(Color.FromArgb(46, 160, 90));

            LoadConfig();
            string lang;
            Loc.Init(cfg.TryGetValue("Language", out lang) ? lang : null);
            autoMountEnabled = !cfg.ContainsKey("AutoMount") || cfg["AutoMount"] != "0";
            rclonePath = SysCheck.FindRclone();

            tray = new NotifyIcon
            {
                Icon = iconIdle,
                Visible = true
            };
            tray.DoubleClick += (s, e) =>
            {
                if (busy) return;
                manualSuppress = mounted;
                ToggleMount(!mounted);
            };
            RebuildMenu();

            // device events only accelerate detection; the watchdog covers everything
            debounceTimer = new System.Windows.Forms.Timer { Interval = 2500 };
            debounceTimer.Tick += (s, e) => { debounceTimer.Stop(); Probe(); };

            watchdogTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            watchdogTimer.Tick += (s, e) => Probe();
            watchdogTimer.Start();

            // startup: wizard first if anything is missing; otherwise probe
            var starter = new System.Windows.Forms.Timer { Interval = 1200 };
            starter.Tick += (s, e) =>
            {
                starter.Stop();
                if (!AllOk()) OpenWizard();
                else Probe();
            };
            starter.Start();
        }

        private bool AllOk()
        {
            if (rclonePath == null) return false;
            if (!SysCheck.WinFspInstalled()) return false;
            bool hasIp;
            string adapter = SysCheck.FindDeckAdapter(out hasIp);
            if (adapter != null && !hasIp) return false;   // present but misconfigured
            return SysCheck.DeckConfigExists(rclonePath);
        }

        // ---------- menu ----------

        private void RebuildMenu()
        {
            var menu = new ContextMenuStrip();

            miStatus = new ToolStripMenuItem { Enabled = false };
            miMount = new ToolStripMenuItem(Loc.T("MenuMount"), null, (s, e) =>
            {
                manualSuppress = false;
                ToggleMount(true);
            });
            miUnmount = new ToolStripMenuItem(Loc.T("MenuUnmount"), null, (s, e) =>
            {
                manualSuppress = true;   // don't auto-remount while cable stays plugged
                ToggleMount(false);
            });
            miAuto = new ToolStripMenuItem(Loc.T("MenuAuto"), null, (s, e) =>
            {
                autoMountEnabled = miAuto.Checked;
                cfg["AutoMount"] = autoMountEnabled ? "1" : "0";
                SaveConfig();
            })
            { Checked = autoMountEnabled, CheckOnClick = true };

            var miWizard = new ToolStripMenuItem(Loc.T("MenuWizard"), null, (s, e) => OpenWizard());

            var langMenu = new ToolStripMenuItem(Loc.T("MenuLang"));
            miLangZh = new ToolStripMenuItem("中文", null, (s, e) => SetLanguage("zh"))
                { Checked = Loc.Lang == "zh" };
            miLangEn = new ToolStripMenuItem("English", null, (s, e) => SetLanguage("en"))
                { Checked = Loc.Lang == "en" };
            langMenu.DropDownItems.Add(miLangZh);
            langMenu.DropDownItems.Add(miLangEn);

            var miExit = new ToolStripMenuItem(Loc.T("MenuExit"), null, (s, e) => ExitApp());

            menu.Items.Add(miStatus);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miMount);
            menu.Items.Add(miUnmount);
            menu.Items.Add(miAuto);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miWizard);
            menu.Items.Add(langMenu);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miExit);

            tray.ContextMenuStrip = menu;
            RefreshStatusText();
        }

        private void SetLanguage(string lang)
        {
            if (Loc.Lang == lang) return;
            Loc.Lang = lang;
            cfg["Language"] = lang;
            SaveConfig();
            RebuildMenu();
        }

        private void RefreshStatusText()
        {
            if (mounted)
            {
                miStatus.Text = Loc.F("StatusMountedFmt", homeLetter,
                    sdMounted ? Loc.F("SdOkFmt", sdLetter) : Loc.T("SdNone"));
                tray.Text = Truncate(Loc.F("TrayMountedFmt",
                    homeLetter + ":" + (sdMounted ? " " + sdLetter + ":" : "")));
            }
            else
            {
                miStatus.Text = Loc.T("StatusNotMounted");
                tray.Text = Loc.T("TrayNotMounted");
            }
            miMount.Enabled = !mounted && !busy;
            miUnmount.Enabled = mounted && !busy;
        }

        private static string Truncate(string t)
        {
            return t.Length > 63 ? t.Substring(0, 63) : t;
        }

        // ---------- wizard ----------

        private void OpenWizard()
        {
            UI(() =>
            {
                if (wizard != null && !wizard.IsDisposed)
                {
                    wizard.Activate();
                    return;
                }
                wizard = new WizardForm();
                wizard.FormClosed += (s, e) =>
                {
                    rclonePath = SysCheck.FindRclone();
                    Probe();
                };
                wizard.Show();
            });
        }

        // ---------- detection ----------

        private void RestartDebounce()
        {
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        // Idempotent state reconciliation. Safe against any trigger source.
        private void Probe()
        {
            if (busy) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (busy) return;
                bool pingOk = SysCheck.Ping(800);

                if (mounted)
                {
                    if (!pingOk && !SysCheck.Ping(800))   // double-check before auto-unmount
                        StartOp(false, true);
                }
                else
                {
                    if (!pingOk)
                    {
                        manualSuppress = false;      // device is gone -> next plug can auto-mount
                        return;
                    }
                    if (autoMountEnabled && !manualSuppress)
                        StartOp(true, true);
                }
            });
        }

        // ---------- mount / unmount ----------

        // Manual toggle (menu / double-click): errors get a message box.
        private void ToggleMount(bool wantMount)
        {
            StartOp(wantMount, false);
        }

        private void StartOp(bool wantMount, bool auto)
        {
            if (busy) return;
            busy = true;
            UI(RefreshStatusText);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (wantMount) DoMount(auto); else DoUnmount(false);
                }
                finally
                {
                    busy = false;
                    UI(RefreshStatusText);
                }
            });
        }

        private void DoMount(bool auto)
        {
            SetIcon(iconBusy, Loc.T("TrayMounting"));
            try
            {
                if (rclonePath == null)
                {
                    Fail(Loc.T("MsgNoRclone"), !auto);
                    return;
                }

                if (!SysCheck.Ping(1000))
                {
                    // In auto mode this just means "the doorbell rang for someone else" - stay silent.
                    Fail(Loc.T("MsgNoDeck"), !auto);
                    return;
                }

                PickLetters();

                var p1 = StartMount("deck:/home/deck", homeLetter, "Steam Deck");
                if (!WaitDrive(homeLetter, 15, p1))
                {
                    KillProc(p1);
                    if (auto) manualSuppress = true;   // don't retry in a loop; wait for replug
                    Fail(Loc.T("MsgHomeFail"), !auto);
                    return;
                }
                mountProcs.Add(p1);

                var p2 = StartMount("deck:/run/media/mmcblk0p1", sdLetter, "Deck SD Card");
                sdMounted = WaitDrive(sdLetter, 10, p2);
                if (sdMounted) mountProcs.Add(p2); else KillProc(p2);

                mounted = true;
                SaveConfig();

                UI(RefreshStatusText);
                SetIcon(iconMounted, Loc.F("TrayMountedFmt",
                    homeLetter + ":" + (sdMounted ? " " + sdLetter + ":" : "")));
                Balloon(Loc.T("BalloonMountedTitle"),
                    Loc.F("BalloonHomeFmt", homeLetter) + "\n"
                    + (sdMounted ? Loc.F("BalloonSdFmt", sdLetter) : Loc.T("BalloonSdNone")),
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                if (auto) manualSuppress = true;
                Fail(Loc.T("MsgMountErr") + ex.Message, !auto);
            }
        }

        private void DoUnmount(bool silent)
        {
            SetIcon(iconBusy, Loc.T("TrayUnmounting"));
            foreach (var p in mountProcs.ToArray()) KillProc(p);
            mountProcs.Clear();
            Thread.Sleep(1500);
            mounted = false;
            sdMounted = false;
            UI(RefreshStatusText);
            SetIcon(iconIdle, Loc.T("TrayNotMounted"));
            if (!silent) Balloon(Loc.T("BalloonUnmountedTitle"),
                Loc.T("BalloonUnmountedText"), ToolTipIcon.Info);
        }

        private Process StartMount(string remote, char letter, string volname)
        {
            var psi = new ProcessStartInfo
            {
                FileName = rclonePath,
                Arguments = "mount " + remote + " " + letter + ": --vfs-cache-mode off --volname \"" + volname + "\"",
                CreateNoWindow = true,
                UseShellExecute = false
                // NOTE: do not redirect stdout/stderr - rclone runs for hours and
                // a full pipe buffer would deadlock it. CreateNoWindow hides output.
            };
            return Process.Start(psi);
        }

        private bool WaitDrive(char letter, int seconds, Process proc)
        {
            for (int i = 0; i < seconds * 2; i++)
            {
                if (Directory.Exists(letter + ":\\")) return true;
                if (proc != null && proc.HasExited) return false;   // rclone died - fail fast
                Thread.Sleep(500);
            }
            return false;
        }

        private void KillProc(Process p)
        {
            try { if (p != null && !p.HasExited) p.Kill(); } catch { }
        }

        // ---------- drive letters ----------

        private void PickLetters()
        {
            var used = new HashSet<char>(DriveInfo.GetDrives()
                .Select(d => char.ToUpperInvariant(d.Name[0])));
            Func<char, bool> free = c => !used.Contains(c) && !Directory.Exists(c + ":\\");

            homeLetter = (char)0;
            sdLetter = (char)0;

            string h, s;
            if (cfg.TryGetValue("HomeLetter", out h) && h.Length > 0 && free(h[0]))
                homeLetter = char.ToUpperInvariant(h[0]);
            if (cfg.TryGetValue("SdLetter", out s) && s.Length > 0 && free(s[0])
                && char.ToUpperInvariant(s[0]) != homeLetter)
                sdLetter = char.ToUpperInvariant(s[0]);

            for (char c = 'Z'; c >= 'E' && (homeLetter == 0 || sdLetter == 0); c--)
            {
                if (!free(c) || c == homeLetter || c == sdLetter) continue;
                if (homeLetter == 0) homeLetter = c;
                else if (sdLetter == 0) sdLetter = c;
            }

            if (homeLetter == 0 || sdLetter == 0)
                throw new Exception(Loc.T("MsgNoLetters"));

            cfg["HomeLetter"] = homeLetter.ToString();
            cfg["SdLetter"] = sdLetter.ToString();
        }

        // ---------- config ----------

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(CfgPath)) return;
                foreach (var line in File.ReadAllLines(CfgPath))
                {
                    int i = line.IndexOf('=');
                    if (i > 0) cfg[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
                }
            }
            catch { }
        }

        private void SaveConfig()
        {
            try
            {
                Directory.CreateDirectory(cfgDir);
                File.WriteAllLines(CfgPath, cfg.Select(kv => kv.Key + "=" + kv.Value).ToArray());
            }
            catch { }
        }

        // ---------- UI helpers ----------

        private void UI(Action a)
        {
            if (hiddenForm.InvokeRequired) hiddenForm.BeginInvoke(a);
            else a();
        }

        private void SetIcon(Icon icon, string text)
        {
            UI(() =>
            {
                tray.Icon = icon;
                tray.Text = Truncate(text);
            });
        }

        private void Balloon(string title, string text, ToolTipIcon icon)
        {
            UI(() => tray.ShowBalloonTip(3000, title, text, icon));
        }

        // showDialog=true only for user-initiated actions; auto path stays silent.
        private void Fail(string msg, bool showDialog)
        {
            UI(() =>
            {
                tray.Icon = mounted ? iconMounted : iconIdle;
                RefreshStatusText();
                if (showDialog)
                    MessageBox.Show(msg, Loc.T("AppTitle"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            });
        }

        private void ExitApp()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (mounted) { try { DoUnmount(true); } catch { } }
                UI(() =>
                {
                    tray.Visible = false;
                    Application.ExitThread();
                });
            });
        }

        // ---------- icon drawing ----------

        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);

        private static Icon MakeIcon(Color color)
        {
            using (var bmp = new Bitmap(32, 32))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    using (var brush = new SolidBrush(color))
                        g.FillEllipse(brush, 1, 1, 30, 30);
                    using (var font = new Font("Segoe UI", 15, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (var white = new SolidBrush(Color.White))
                    {
                        var sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        g.DrawString("D", font, white, new RectangleF(0, 1, 32, 32), sf);
                    }
                }
                IntPtr h = bmp.GetHicon();
                Icon tmp = Icon.FromHandle(h);
                Icon icon = (Icon)tmp.Clone();
                DestroyIcon(h);
                return icon;
            }
        }
    }
}
