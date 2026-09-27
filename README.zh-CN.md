# DeckDrive · Steam Deck 变 Windows 盘符 · 小白教程

**[English](README.md)** | 简体中文

效果：一根 USB-C 线把 Steam Deck 连到电脑，双击一个图标，资源管理器里就多出一个 **X: 盘**，里面就是 Deck 的文件。复制、删除、重命名，跟用 U 盘一样。不需要 WiFi。

## 你要准备的东西

- Steam Deck 一台（需要进**桌面模式**操作一次）
- **USB-C 数据线**一根（必须能传数据，不是只能充电的线；用手机原装线一般都可以）
- Windows 10 / 11 电脑一台
- 这个文件夹里的 6 个文件

整个设置分两大步，每步只做一次。以后日常使用只要 10 秒钟。

---

## 第一步：Deck 端设置（一次性，约 5 分钟）

1. 把这个文件夹里 `deck` 子文件夹的 **3 个文件**（`usb-gadget.sh`、`usb-gadget.service`、`install.sh`）复制到 Deck 上。
   - 最简单的方法：复制到 **U 盘**（或手机），通过扩展坞/转接头插到 Deck 上，在桌面模式拷贝到「主目录」。
   - 或者：用 KDE Connect、Warpinator 之类的工具无线传过去也行。
2. 在 Deck 上按电源键 → **切换到桌面模式**。
3. 打开「开始菜单」→ 系统 → **Konsole**（黑色图标的终端程序）。
4. 先给 Deck 设置一个登录密码（如果以前设过就跳过）：
   ```
   passwd
   ```
   输入两遍新密码（输入时屏幕不显示，正常的，输完按回车）。**记住这个密码，Windows 那边要用。**
5. 进入你放那 3 个文件的文件夹。比如放在主目录的 `deck` 文件夹里：
   ```
   cd ~/deck
   ```
6. 运行安装脚本：
   ```
   chmod +x install.sh
   ./install.sh
   ```
   中途会问你密码（就是上一步设的），输入后回车。
7. 看到 `Setup finished!` 就完成了。Deck 端以后**永远不用再动**，开机自动生效。

---

## 第二步：Windows 端设置（一次性，约 5 分钟）

1. 用 USB-C 线把 Deck 和电脑连起来（Deck 保持开机，游戏模式、桌面模式都行）。
2. 等 10 秒左右，Windows 会识别出一个新的"网卡"（这是正常现象，就是 Deck 假装的）。
3. 以管理员身份运行 `setup.ps1`，步骤如下：
   - 打开 `windows` 文件夹，在地址栏单击，**复制这个文件夹的路径**（形如 `C:\...\DeckDrive\windows`）。
   - 按 `Win + X` → 选择「**终端(管理员)**」或「**Windows PowerShell (管理员)**」，弹出"是否允许更改"点**是**。
   - 在黑色窗口里依次粘贴执行下面两行（第一行换成你刚才复制的路径）：
     ```powershell
     cd "这里粘贴windows文件夹路径"
     Set-ExecutionPolicy -Scope Process Bypass -Force; .\setup.ps1
     ```
   - 说明：`Set-ExecutionPolicy -Scope Process Bypass` 只对当前这个窗口放行脚本，关掉窗口就失效，不影响系统安全设置。
4. 脚本会自动做四件事：
   - 安装两个免费软件：**rclone** 和 **WinFsp**（挂载盘符用的）
   - 给这个"USB 网卡"配好 IP 地址
   - 问你 Deck 的密码（第一步设的那个），输入后回车
   - 测试连接，列出 Deck 上的文件夹
5. 看到 `All done` 就完成了。

> 如果第 3 步运行后提示"找不到 USB 网卡"：拔掉线重插、换一根线、或换个 USB 口，然后重新运行 `setup.ps1`。

---

## 日常使用（每次只要 10 秒）

1. 插上 USB-C 线。
2. 双击 `windows` 文件夹里的 **`mount-deck.bat`**。
3. 稍等几秒，会同时挂出两个盘：
   - **X: 盘**（Steam Deck）→ Deck 的主目录
   - **Y: 盘**（Deck SD Card）→ Deck 的 TF 卡
   - 同时任务栏里有两个最小化的小窗口（**别关它们，关了盘就没了**）。
4. 用完了：双击 `unmount-deck.bat` 弹出两个盘，再拔线。

> 如果 X: 或 Y: 盘符已被占用（比如插了其他硬盘），右键 `mount-deck.bat` → 编辑，把里面的 `X:` `Y:` 换成空闲的字母。

可以把 `mount-deck.bat` 右键 → 发送到 → 桌面快捷方式，以后在桌面双击就行。

---

## 常见问题

**Q: X: 盘和 Y: 盘分别是什么？**  
- X: = Deck 的 `/home/deck`（主目录，截图、录屏、大部分游戏数据都在这）
- Y: = Deck 的 TF 卡（对应系统里的 `/run/media/mmcblk0p1`），**没插 TF 卡时 Y: 会挂载失败，不影响 X: 正常使用**
- 想看 Deck 的整个系统：把脚本里的 `deck:/home/deck` 改成 `deck:/`（不建议，容易误删系统文件）。

**Q: 插上线电脑没反应？**

- 换一根确认能传数据的线（手机原装线最靠谱）。
- Deck 上打开 Konsole 运行 `systemctl status usb-gadget`，看到 `active (exited)` 绿色字样说明正常。

**Q: Windows 11 新版识别不出网卡？**  
新版 Win11 在淘汰一种叫 RNDIS 的老协议。在 Deck 上运行：

```
sudo nano /etc/systemd/system/usb-gadget.service
```

把 `Environment=FUNC=rndis` 改成 `Environment=FUNC=ncm`，按 `Ctrl+O` 回车保存、`Ctrl+X` 退出，然后：

```
sudo systemctl daemon-reload && sudo systemctl restart usb-gadget
```

Windows 端重新运行一次 `setup.ps1` 即可。

**Q: SteamOS 系统大更新后失效了？**  
重新做一次"第一步"的第 6 小节（再跑一遍 `./install.sh`）就行。

**Q: 速度怎么样？**  
一般 50~150 MB/s，取决于线材和接口。拷电影级别的文件很快；海量小文件（几千个）会慢一些，这是正常现象。

**Q: 拷文件时 C 盘也在读写？**  
旧版本用了 `--vfs-cache-mode writes`，文件会先缓存到 C 盘再上传。现已改为 `minimal` 模式：普通复制直接流式传输，不经过 C 盘；只有"在盘上直接编辑文件"时才临时使用缓存（缓存在 `C:\Users\你\AppData\Local\rclone\vfs`）。

**Q: 安全吗？**  
这条"网线"只有你的电脑和 Deck 两台设备，密码只有你知道，不经过任何外部网络。

**Q: 我之前装过 DeckMTP？**  
两个方案会抢同一个 USB 接口，请停用其中一个：

```
sudo systemctl disable --now deckmtp   # 停用 DeckMTP
```
