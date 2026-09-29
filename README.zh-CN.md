# DeckDrive · Steam Deck 变 Windows 盘符

**[English](README.md)** | 简体中文

一根 USB-C 线连接 Steam Deck 和电脑，**插上就自动出现两个盘符**（主目录 + TF 卡），拔掉自动卸载。不需要 WiFi。

- **主目录盘** → Deck 的 `/home/deck`（截图、录屏、大部分游戏数据）
- **TF 卡盘** → Deck 的 `/run/media/mmcblk0p1`

盘符自动挑选（优先上次的字母，被占了从 Z 往前找）。复制、删除、重命名，跟用 U 盘一样。速度一般 50~150 MB/s，纯流式传输，不经过 C 盘中转。

## 原理一句话

Deck 通过 USB 假装成网卡 → 与电脑组成私有微型网络 → SSH/SFTP 传文件 → rclone + WinFsp 把远程文件夹变成本地盘符。托盘程序负责检测插拔、自动挂载。

## 你要准备的东西

- Steam Deck 一台（需要进**桌面模式**操作一次）
- **USB-C 数据线**一根（必须能传数据；手机原装线一般都可以）
- Windows 10 / 11 电脑（系统自带 .NET 4.8，托盘程序双击即用，无需安装运行环境）

---

## 第一步：Deck 端设置（一次性，约 5 分钟）

1. 把 `deck` 文件夹的 **3 个文件**（`usb-gadget.sh`、`usb-gadget.service`、`install.sh`）复制到 Deck 上。
   - 最简单：复制到 **U 盘**，通过扩展坞/转接头插到 Deck，桌面模式拷贝到主目录。
   - 或用 KDE Connect、Warpinator 无线传。
2. Deck 按电源键 → **切换到桌面模式**。
3. 打开 **Konsole** 终端。
4. 设置登录密码（设过就跳过），**记住它，Windows 端要用**：
   ```
   passwd
   ```
5. 进入放文件的目录，例如 `cd ~/deck`，运行：
   ```
   chmod +x install.sh
   ./install.sh
   ```
6. 看到 `Setup finished!` 完成。以后 Deck 端**永远不用再动**。

---

## 第二步：Windows 端设置（一次性，约 3 分钟）

1. 用 USB-C 线连接 Deck 和电脑，等 10 秒（Windows 会识别出一个新"网卡"，正常现象）。
2. 双击 **`DeckDriveTray.exe`**。
3. 首次运行自动弹出**设置向导**，逐项检查：rclone、WinFsp、网卡 IP、连接配置（这里输入 Deck 密码）、连通测试。
   - 缺什么点对应按钮，系统弹授权窗口时点"是"。
   - 全部变绿 → 点"完成"。
4. 以后程序安静待在右下角托盘：灰 = 未挂载，绿 = 已挂载。

> 中文系统默认中文界面；托盘菜单「语言 Language」可切英文。

---

## 日常使用（零操作）

- **插线** → 几秒内自动挂出两个盘
- **拔线** → 自动卸载
- 托盘菜单：挂载 / 卸载（手动控制）、插线自动挂载（可关）、设置向导、语言、退出

> 传文件时别拔线——和 U 盘一个道理，可能损坏文件。

---

## 进阶：手动脚本（备用方式）

不想用托盘程序，或在别的电脑上临时用一次：

- `windows\setup.ps1`：命令行版一次性配置（管理员 PowerShell 运行）
- `windows\mount-deck.bat`：双击挂载 X: / Y:；`unmount-deck.bat` 卸载
- `app\build.bat`：用 Windows 自带的 .NET 编译器自行编译托盘程序（源码在 `app\`）

---

## 常见问题

**Q: 盘符是怎么定的？**  
优先复用上次的盘符；被占用就从 Z 往前自动找两个空字母，大字母给主目录、小字母给 TF 卡。不用记字母，认卷标 "Steam Deck" 和 "Deck SD Card" 即可。

**Q: 没插 TF 卡会怎样？**  
TF 卡盘挂载失败，主目录盘照常使用。

**Q: 拷文件时 C 盘也在读写？**  
旧版本用 `--vfs-cache-mode writes/minimal`，文件会在 C 盘缓存中转。现版本用 **`off`** 模式：纯内存流式，C 盘零中转。代价：不支持"在盘上原地编辑文件"，整文件拷进拷出完全正常。

**Q: 插上线电脑没反应？**

- 换一根确认能传数据的线（最常见的原因）。
- Deck 上 Konsole 运行 `systemctl status usb-gadget`，绿色 `active (exited)` 即正常。

**Q: Windows 11 新版识别不出网卡？**  
新版 Win11 在淘汰 RNDIS 协议。Deck 上运行 `sudo nano /etc/systemd/system/usb-gadget.service`，把 `Environment=FUNC=rndis` 改成 `ncm`，保存后 `sudo systemctl daemon-reload && sudo systemctl restart usb-gadget`。

**Q: SteamOS 大更新后失效了？**  
Deck 上重新跑一遍 `./install.sh` 即可。

**Q: 安全吗？**  
这条"网线"只有你的电脑和 Deck 两台设备，密码只存在你电脑的 rclone 配置里，不经过任何外部网络。

**Q: 装过 DeckMTP？**  
两者抢同一个 USB 控制器，停用一个：`sudo systemctl disable --now deckmtp`

---

## License

MIT — 见 [LICENSE](LICENSE)。随意使用，不提供担保。
