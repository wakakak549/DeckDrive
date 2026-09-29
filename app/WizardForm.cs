// DeckDrive first-run wizard + system checks.
// Replaces windows/setup.ps1: installs rclone/WinFsp (elevated on demand),
// configures the USB adapter IP, and creates the rclone remote.
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;

namespace DeckDrive
{
    internal static class SysCheck
    {
        public const string DeckIp = "10.66.0.1";
        public const string PcIp = "10.66.0.2";

        public static string FindRclone()
        {
            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathEnv.Split(';'))
            {
                if (dir.Trim().Length == 0) continue;
                try
                {
                    string p = Path.Combine(dir.Trim(), "rclone.exe");
                    if (File.Exists(p)) return p;
                }
                catch { }
            }
            string winget = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\WinGet\Links\rclone.exe");
            if (File.Exists(winget)) return winget;
            if (File.Exists(@"C:\Program Files\rclone\rclone.exe"))
                return @"C:\Program Files\rclone\rclone.exe";
            return null;
        }

        public static bool WinFspInstalled()
        {
            return Directory.Exists(@"C:\Program Files (x86)\WinFsp")
                || Directory.Exists(@"C:\Program Files\WinFsp");
        }

        public static bool Ping(int timeoutMs)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(DeckIp, timeoutMs);
                    return reply != null && reply.Status == IPStatus.Success;
                }
            }
            catch { return false; }
        }

        // Finds the Deck's USB network adapter. Returns the interface name for netsh,
        // or null if the adapter is absent (cable unplugged). hasIp = our static IP set.
        public static string FindDeckAdapter(out bool hasIp)
        {
            hasIp = false;
            // Preferred: our unique gadget VID/PID (set in deck/usb-gadget.sh)
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT NetConnectionID FROM Win32_NetworkAdapter WHERE PNPDeviceID LIKE '%VID_28DE&PID_1205%'"))
                {
                    foreach (ManagementObject o in searcher.Get())
                    {
                        string name = o["NetConnectionID"] as string;
                        if (!string.IsNullOrEmpty(name))
                        {
                            hasIp = AdapterHasIp(name);
                            return name;
                        }
                    }
                }
            }
            catch { }
            // Fallback: match common RNDIS/NCM descriptions
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    string d = ni.Description ?? "";
                    if (d.IndexOf("RNDIS", StringComparison.OrdinalIgnoreCase) >= 0
                        || d.IndexOf("Remote NDIS", StringComparison.OrdinalIgnoreCase) >= 0
                        || d.IndexOf("NCM", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasIp = AdapterHasIp(ni.Name);
                        return ni.Name;
                    }
                }
            }
            catch { }
            return null;
        }

        private static bool AdapterHasIp(string name)
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.Name != name) continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                        if (ua.Address.ToString() == PcIp) return true;
                }
            }
            catch { }
            return false;
        }

        public static bool DeckConfigExists(string rclone)
        {
            if (rclone == null) return false;
            string outp = RunCapture(rclone, "listremotes", 15000);
            return outp != null && outp.IndexOf("deck:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool TestDeckSftp(string rclone)
        {
            string outp = RunCapture(rclone, "lsd deck: --max-depth 1", 20000);
            return outp != null;
        }

        // Runs a short-lived process and captures stdout. Returns null on timeout/error.
        public static string RunCapture(string file, string args, int timeoutMs)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                var p = Process.Start(psi);
                // drain stderr in background so a chatty process cannot deadlock
                var err = p.StandardError.ReadToEndAsync();
                string outp = p.StandardOutput.ReadToEnd();
                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(); } catch { }
                    return null;
                }
                // non-zero exit = failure (e.g. wrong SFTP password returns empty stdout
                // but exits non-zero - we must not treat that as success)
                return p.ExitCode == 0 ? (outp ?? "") : null;
            }
            catch { return null; }
        }

        // Elevated one-shot action (UAC prompt). Returns true if the process exited 0.
        public static bool RunElevated(string file, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    Verb = "runas",
                    UseShellExecute = true
                };
                var p = Process.Start(psi);
                if (p == null) return false;
                p.WaitForExit();
                return p.ExitCode == 0;
            }
            catch { return false; }   // user cancelled UAC
        }

        public static void RefreshPathFromRegistry()
        {
            string machine = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
            string user = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            Environment.SetEnvironmentVariable("PATH", machine + ";" + user);
        }
    }

    public class WizardForm : Form
    {
        public event EventHandler AllGreen;

        private Label stRclone, stWinfsp, stAdapter, stConfig, stDeck;
        private Button btnRclone, btnWinfsp, btnAdapter, btnConfig, btnRecheck, btnClose;
        private TextBox txtPwd;
        private string rclone;
        private string adapterName;
        private bool working;

        public WizardForm()
        {
            Text = Loc.T("WzTitle");
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 360);

            var root = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(14),
                AutoScroll = true
            };

            var intro = new Label
            {
                Text = Loc.T("WzIntro"),
                AutoSize = true,
                MaximumSize = new Size(480, 0),
                Margin = new Padding(0, 0, 0, 10)
            };
            root.Controls.Add(intro);

            AddRow(root, Loc.T("WzStepRclone"), out stRclone, out btnRclone,
                Loc.T("WzBtnInstall"), (s, e) => DoWork(() => InstallRclone()));
            AddRow(root, Loc.T("WzStepWinfsp"), out stWinfsp, out btnWinfsp,
                Loc.T("WzBtnInstall"), (s, e) => DoWork(() => InstallWinFsp()));
            AddRow(root, Loc.T("WzStepAdapter"), out stAdapter, out btnAdapter,
                Loc.T("WzBtnFixIp"), (s, e) => DoWork(() => FixAdapterIp()));

            // config row has a password box instead of a plain description
            var row = new Panel { Width = 490, Height = 34, Margin = new Padding(0, 3, 0, 3) };
            stConfig = new Label { Width = 44, Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft };
            btnConfig = new Button { Text = Loc.T("WzBtnCreate"), Width = 76, Dock = DockStyle.Right };
            btnConfig.Click += (s, e) => DoWork(() => CreateConfig());
            txtPwd = new TextBox { Width = 130, Dock = DockStyle.Right, UseSystemPasswordChar = true };
            var lblPwd = new Label
            {
                Text = Loc.T("WzPwdLabel"),
                AutoSize = true,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 7, 0, 0)
            };
            var lblStep = new Label
            {
                Text = Loc.T("WzStepConfig"),
                AutoSize = true,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 7, 12, 0)
            };
            row.Controls.Add(lblPwd);       // dock order: rightmost added first fills last
            row.Controls.Add(txtPwd);
            row.Controls.Add(btnConfig);
            row.Controls.Add(lblStep);
            row.Controls.Add(stConfig);
            root.Controls.Add(row);

            AddRow(root, Loc.T("WzStepDeck"), out stDeck, out btnRecheck,
                Loc.T("WzBtnRecheck"), (s, e) => RefreshChecks());

            var bottom = new Panel { Width = 490, Height = 40, Margin = new Padding(0, 10, 0, 0) };
            btnClose = new Button
            {
                Text = Loc.T("WzBtnClose"),
                Width = 90,
                Height = 30,
                Dock = DockStyle.Right,
                Enabled = false
            };
            btnClose.Click += (s, e) => Close();
            bottom.Controls.Add(btnClose);
            root.Controls.Add(bottom);

            Controls.Add(root);
            Shown += (s, e) => RefreshChecks();
        }

        private void AddRow(FlowLayoutPanel root, string desc,
            out Label status, out Button btn, string btnText, EventHandler onClick)
        {
            var row = new Panel { Width = 490, Height = 34, Margin = new Padding(0, 3, 0, 3) };
            status = new Label { Width = 44, Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft };
            btn = new Button { Text = btnText, Width = 76, Dock = DockStyle.Right };
            btn.Click += onClick;
            var lbl = new Label
            {
                Text = desc,
                AutoSize = true,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 7, 0, 0)
            };
            row.Controls.Add(lbl);
            row.Controls.Add(btn);
            row.Controls.Add(status);
            root.Controls.Add(row);
        }

        private void DoWork(Action work)
        {
            if (working) return;
            working = true;
            Cursor = Cursors.WaitCursor;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { work(); }
                catch { }
                finally
                {
                    BeginInvoke((Action)(() =>
                    {
                        Cursor = Cursors.Default;
                        working = false;
                        RefreshChecks();
                    }));
                }
            });
        }

        private void RefreshChecks()
        {
            if (InvokeRequired) { BeginInvoke((Action)RefreshChecks); return; }

            SysCheck.RefreshPathFromRegistry();
            rclone = SysCheck.FindRclone();
            bool winfsp = SysCheck.WinFspInstalled();
            bool hasIp;
            adapterName = SysCheck.FindDeckAdapter(out hasIp);
            bool adapterOk = adapterName == null || hasIp;   // absent = cable not plugged = tolerable
            bool configOk = SysCheck.DeckConfigExists(rclone);
            bool deckOk = SysCheck.Ping(800);

            Mark(stRclone, rclone != null);
            Mark(stWinfsp, winfsp);
            Mark(stAdapter, adapterOk, adapterName == null ? Loc.T("WzNoCable") : null);
            Mark(stConfig, configOk);
            Mark(stDeck, deckOk);

            btnRclone.Enabled = rclone == null;
            btnWinfsp.Enabled = !winfsp;
            btnAdapter.Enabled = !adapterOk;
            btnConfig.Enabled = rclone != null && !configOk;

            btnClose.Enabled = rclone != null && winfsp && adapterOk && configOk;
        }

        private void Mark(Label l, bool ok, string note = null)
        {
            l.Text = ok ? "✓" : "✗";
            l.ForeColor = ok ? Color.FromArgb(46, 160, 90) : Color.FromArgb(200, 60, 60);
            if (note != null && !ok) l.Text = "—";
        }

        // ---------- fix actions ----------

        private void InstallRclone()
        {
            SysCheck.RunElevated("winget",
                "install -e --id Rclone.Rclone --accept-package-agreements --accept-source-agreements");
            SysCheck.RefreshPathFromRegistry();
        }

        private void InstallWinFsp()
        {
            SysCheck.RunElevated("winget",
                "install -e --id WinFsp.WinFsp --accept-package-agreements --accept-source-agreements");
        }

        private void FixAdapterIp()
        {
            if (adapterName == null)
            {
                BeginInvoke((Action)(() => MessageBox.Show(this, Loc.T("WzNeedCable"),
                    Loc.T("AppTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information)));
                return;
            }
            SysCheck.RunElevated("netsh",
                "interface ip set address name=\"" + adapterName + "\" static "
                + SysCheck.PcIp + " 255.255.255.0");
        }

        private void CreateConfig()
        {
            string pwd = txtPwd.Text;
            if (pwd.Length == 0)
            {
                BeginInvoke((Action)(() => MessageBox.Show(this, Loc.T("WzPwdEmpty"),
                    Loc.T("AppTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                return;
            }
            if (rclone == null) return;

            string obscured = SysCheck.RunCapture(rclone, "obscure \"" + pwd.Replace("\"", "") + "\"", 10000);
            if (obscured == null) return;
            obscured = obscured.Trim();

            SysCheck.RunCapture(rclone,
                "config create deck sftp host " + SysCheck.DeckIp + " user deck pass \""
                + obscured + "\" shell_type unix", 15000);

            bool ok = SysCheck.TestDeckSftp(rclone);
            if (!ok)
                SysCheck.RunCapture(rclone, "config delete deck", 10000);

            BeginInvoke((Action)(() =>
            {
                if (ok) txtPwd.Text = "";
                MessageBox.Show(this, Loc.T(ok ? "WzConfigOk" : "WzConfigFail"),
                    Loc.T("AppTitle"), MessageBoxButtons.OK,
                    ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (btnClose.Enabled && AllGreen != null) AllGreen(this, EventArgs.Empty);
            base.OnFormClosing(e);
        }
    }
}
