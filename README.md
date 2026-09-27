# Steam Deck USB Drive for Windows

English | **[简体中文](README.zh-CN.md)**

Mount your Steam Deck as a Windows drive letter over a single USB-C cable — no WiFi needed.

After setup, plugging in the cable and double-clicking one file gives you two drives in Explorer:

- **X: (Steam Deck)** → the Deck's home folder (`/home/deck`)
- **Y: (Deck SD Card)** → the Deck's microSD card (`/run/media/mmcblk0p1`)

Copy, delete, rename — it works just like a USB flash drive. Typical speed: 50–150 MB/s.

## How it works

```
Steam Deck  --(USB-C cable, acts as a virtual network adapter)-->  Windows
SSH/SFTP file transfer over this mini-network
rclone mount + WinFsp turns the remote folders into drive letters
```

No custom software was written for this — it glues together three mature, free, open-source pieces: Linux USB gadget mode (built into SteamOS), OpenSSH (built into SteamOS), and rclone + WinFsp on Windows.

## What you need

- A Steam Deck (you'll use **Desktop Mode** once)
- A **USB-C data cable** (a charge-only cable will NOT work; a phone's original cable is usually fine)
- A Windows 10 / 11 PC

Setup has two one-time steps (~5 minutes each). Daily use takes 10 seconds.

---

## Step 1: Steam Deck setup (one-time)

1. Copy the **3 files** from the `deck` folder (`usb-gadget.sh`, `usb-gadget.service`, `install.sh`) to your Deck.
   - Easiest: copy them to a USB flash drive, plug it into the Deck (via dock/adapter), and copy them to the home folder in Desktop Mode.
   - Or transfer wirelessly with KDE Connect / Warpinator.
2. On the Deck, press the power button → **Switch to Desktop**.
3. Open the app launcher → System → **Konsole** (the black terminal icon).
4. Set a login password if you never did (you'll need it on the Windows side):
   ```
   passwd
   ```
   Type the new password twice (nothing shows on screen while typing — that's normal).
5. Go to the folder where you put the 3 files, e.g.:
   ```
   cd ~/deck
   ```
6. Run the installer:
   ```
   chmod +x install.sh
   ./install.sh
   ```
   Enter your password when asked.
7. When you see `Setup finished!`, you're done. The Deck side runs automatically on every boot from now on.

---

## Step 2: Windows setup (one-time)

1. Connect the Deck to the PC with the USB-C cable (Deck powered on; Gaming Mode or Desktop Mode both fine).
2. Wait ~10 seconds. Windows will detect a new "network adapter" — that's the Deck pretending to be one. This is expected.
3. Run `setup.ps1` as administrator:
   - Open the `windows` folder, click the address bar, and **copy the folder path** (like `C:\...\steamdeck-usb-drive-for-windows\windows`).
   - Press `Win + X` → choose **Terminal (Admin)** or **Windows PowerShell (Admin)**, click **Yes** on the prompt.
   - Paste these two lines (replace the first one with your copied path):
     ```powershell
     cd "paste the windows folder path here"
     Set-ExecutionPolicy -Scope Process Bypass -Force; .\setup.ps1
     ```
   - Note: `Set-ExecutionPolicy -Scope Process Bypass` only allows scripts in this window and resets when you close it. Your system security settings stay untouched.
4. The script automatically:
   - Installs two free tools: **rclone** and **WinFsp**
   - Assigns a static IP to the "USB network adapter"
   - Asks for your Deck password (from Step 1)
   - Tests the connection by listing folders on the Deck
5. `All done` means success.

> If step 3 says "USB network adapter not found": replug the cable, try a different cable or USB port, then re-run `setup.ps1`.

---

## Daily use (10 seconds)

1. Plug in the USB-C cable.
2. Double-click **`mount-deck.bat`** in the `windows` folder.
3. After a few seconds, two drives appear:
   - **X: (Steam Deck)** → home folder
   - **Y: (Deck SD Card)** → microSD card
   - Two minimized windows keep the drives alive — **don't close them**.
4. When finished: double-click `unmount-deck.bat` to eject both drives, then unplug.

> If X: or Y: is already taken by another device, right-click `mount-deck.bat` → Edit, and change the letters to free ones (there are several places — change all of them).

Tip: right-click `mount-deck.bat` → Send to → Desktop (create shortcut) for one-click mounting.

---

## FAQ

**Q: What exactly are X: and Y:?**
- X: = the Deck's `/home/deck` (home folder — screenshots, recordings, most game data)
- Y: = the microSD card (`/run/media/mmcblk0p1`). **If no card is inserted, Y: fails to mount but X: still works.**
- Want the whole filesystem? Change `deck:/home/deck` to `deck:/` in the script (not recommended — easy to delete system files by accident).

**Q: Nothing happens when I plug in the cable?**
- Try a cable you know can transfer data (a phone's original cable is the safest bet).
- On the Deck, open Konsole and run `systemctl status usb-gadget` — a green `active (exited)` means it's working.

**Q: Newer Windows 11 doesn't recognize the network adapter?**
Recent Windows 11 builds are phasing out the old RNDIS protocol. On the Deck, run:
```
sudo nano /etc/systemd/system/usb-gadget.service
```
Change `Environment=FUNC=rndis` to `Environment=FUNC=ncm`, press `Ctrl+O` then Enter to save, `Ctrl+X` to exit, then:
```
sudo systemctl daemon-reload && sudo systemctl restart usb-gadget
```
Re-run `setup.ps1` on the Windows side.

**Q: Stopped working after a big SteamOS update?**
Just re-run `./install.sh` on the Deck (Step 1.6).

**Q: How fast is it?**
Typically 50–150 MB/s depending on cable and port. Great for large files; thousands of tiny files will be slower — that's normal.

**Q: Is it secure?**
The "network" contains only your PC and your Deck. The password never leaves your hands; nothing goes through the internet.

**Q: I have DeckMTP installed?**
Both solutions use the same USB controller — disable one of them:
```
sudo systemctl disable --now deckmtp   # disable DeckMTP
```

---

## License

MIT — see [LICENSE](LICENSE). Use it however you like; no warranty provided.
