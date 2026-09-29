// DeckDrive localization: zh / en
using System.Collections.Generic;
using System.Globalization;

namespace DeckDrive
{
    internal static class Loc
    {
        public static string Lang = "zh";

        public static void Init(string saved)
        {
            if (saved == "zh" || saved == "en") Lang = saved;
            else Lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "zh" : "en";
        }

        public static string T(string key)
        {
            var d = Lang == "zh" ? zh : en;
            string v;
            if (d.TryGetValue(key, out v)) return v;
            if (en.TryGetValue(key, out v)) return v;
            return key;
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        private static readonly Dictionary<string, string> zh = new Dictionary<string, string>
        {
            {"AlreadyRunning", "DeckDrive 已经在运行了（看右下角托盘图标）。"},
            {"AppTitle", "DeckDrive"},
            {"StatusNotMounted", "未挂载"},
            {"StatusMountedFmt", "已挂载  {0}: 主目录{1}"},
            {"SdOkFmt", "  {0}: TF卡"},
            {"SdNone", "  TF卡无"},
            {"MenuMount", "挂载 Deck"},
            {"MenuUnmount", "卸载 Deck"},
            {"MenuAuto", "插线自动挂载"},
            {"MenuWizard", "设置向导..."},
            {"MenuLang", "语言 Language"},
            {"MenuExit", "退出"},
            {"TrayNotMounted", "DeckDrive - 未挂载"},
            {"TrayMounting", "DeckDrive - 挂载中..."},
            {"TrayUnmounting", "DeckDrive - 卸载中..."},
            {"TrayMountedFmt", "DeckDrive - 已挂载 {0}"},
            {"MsgNoRclone", "未找到 rclone。请打开设置向导安装，或运行 windows\\setup.ps1。"},
            {"MsgNoDeck", "检测不到 Steam Deck（10.66.0.1 无响应）。\n请检查 USB 线是否插好、Deck 是否开机。"},
            {"MsgHomeFail", "主目录挂载失败。请检查 USB 线连接和 Deck 端设置。"},
            {"MsgMountErr", "挂载出错："},
            {"MsgNoLetters", "找不到两个空闲盘符，请手动指定。"},
            {"BalloonMountedTitle", "挂载完成"},
            {"BalloonHomeFmt", "{0}: = Deck 主目录"},
            {"BalloonSdFmt", "{0}: = TF 卡"},
            {"BalloonSdNone", "TF 卡未挂载（可能未插入）"},
            {"BalloonUnmountedTitle", "已卸载"},
            {"BalloonUnmountedText", "盘符已弹出。"},
            // wizard
            {"WzTitle", "DeckDrive 首次设置向导"},
            {"WzIntro", "以下项目全部变绿即可正常使用。带按钮的项可一键修复（系统会弹窗请求授权，点\"是\"）。"},
            {"WzStepRclone", "rclone（挂载核心组件）"},
            {"WzStepWinfsp", "WinFsp（虚拟磁盘驱动）"},
            {"WzStepAdapter", "USB 网卡 IP 配置（10.66.0.2）"},
            {"WzStepConfig", "Deck 连接配置（需密码）"},
            {"WzStepDeck", "Deck 连通测试（10.66.0.1）"},
            {"WzBtnInstall", "安装"},
            {"WzBtnFixIp", "修复"},
            {"WzBtnCreate", "创建"},
            {"WzBtnRecheck", "重新检测"},
            {"WzBtnClose", "完成"},
            {"WzPwdLabel", "Deck 密码："},
            {"WzPwdEmpty", "请输入 Deck 的登录密码（deck 用户的密码）。"},
            {"WzNeedCable", "未检测到 Deck 的 USB 网卡。\n请插上 USB 线后点\"重新检测\"。\n（不插线也可以先完成其他项）"},
            {"WzConfigOk", "配置创建成功，连接测试通过！"},
            {"WzConfigFail", "配置已创建但连接测试失败：密码可能不正确，或 Deck 不在线。\n已删除该配置，请检查后重试。"},
            {"WzWorking", "处理中，请稍候..."},
            {"WzOk", "正常"},
            {"WzMissing", "缺失"},
            {"WzNoCable", "未插线"},
        };

        private static readonly Dictionary<string, string> en = new Dictionary<string, string>
        {
            {"AlreadyRunning", "DeckDrive is already running (check the system tray)."},
            {"AppTitle", "DeckDrive"},
            {"StatusNotMounted", "Not mounted"},
            {"StatusMountedFmt", "Mounted  {0}: home{1}"},
            {"SdOkFmt", "  {0}: SD card"},
            {"SdNone", "  no SD card"},
            {"MenuMount", "Mount Deck"},
            {"MenuUnmount", "Unmount Deck"},
            {"MenuAuto", "Auto-mount on plug"},
            {"MenuWizard", "Setup wizard..."},
            {"MenuLang", "Language 语言"},
            {"MenuExit", "Exit"},
            {"TrayNotMounted", "DeckDrive - not mounted"},
            {"TrayMounting", "DeckDrive - mounting..."},
            {"TrayUnmounting", "DeckDrive - unmounting..."},
            {"TrayMountedFmt", "DeckDrive - mounted {0}"},
            {"MsgNoRclone", "rclone not found. Open the setup wizard to install it, or run windows\\setup.ps1."},
            {"MsgNoDeck", "Steam Deck not detected (10.66.0.1 not responding).\nCheck the USB cable and that the Deck is powered on."},
            {"MsgHomeFail", "Failed to mount the home folder. Check the cable and the Deck-side setup."},
            {"MsgMountErr", "Mount error: "},
            {"MsgNoLetters", "Could not find two free drive letters."},
            {"BalloonMountedTitle", "Mounted"},
            {"BalloonHomeFmt", "{0}: = Deck home folder"},
            {"BalloonSdFmt", "{0}: = SD card"},
            {"BalloonSdNone", "SD card not mounted (maybe not inserted)"},
            {"BalloonUnmountedTitle", "Unmounted"},
            {"BalloonUnmountedText", "Drives ejected."},
            // wizard
            {"WzTitle", "DeckDrive First-Run Setup"},
            {"WzIntro", "Everything must turn green. Items with a button can be fixed with one click (Windows will ask for permission - click Yes)."},
            {"WzStepRclone", "rclone (mount engine)"},
            {"WzStepWinfsp", "WinFsp (virtual disk driver)"},
            {"WzStepAdapter", "USB adapter IP (10.66.0.2)"},
            {"WzStepConfig", "Deck connection config (password)"},
            {"WzStepDeck", "Deck reachability (10.66.0.1)"},
            {"WzBtnInstall", "Install"},
            {"WzBtnFixIp", "Fix"},
            {"WzBtnCreate", "Create"},
            {"WzBtnRecheck", "Re-check"},
            {"WzBtnClose", "Done"},
            {"WzPwdLabel", "Deck password:"},
            {"WzPwdEmpty", "Enter the Deck login password (user 'deck')."},
            {"WzNeedCable", "Deck USB adapter not found.\nPlug in the cable and click Re-check.\n(You can finish the other items without the cable.)"},
            {"WzConfigOk", "Config created and connection test passed!"},
            {"WzConfigFail", "Config was created but the connection test failed:\nwrong password or Deck offline. The config was removed - please retry."},
            {"WzWorking", "Working, please wait..."},
            {"WzOk", "OK"},
            {"WzMissing", "Missing"},
            {"WzNoCable", "no cable"},
        };
    }
}
