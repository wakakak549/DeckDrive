# DeckDrive — Steam Deck USB Drive for Windows

English | **[简体中文](README.zh-CN.md)**

One USB-C cable. **Plug in and two drives appear automatically** (home folder + microSD card). Unplug and they eject themselves. No WiFi needed.

- **Home drive** → the Deck's `/home/deck` (screenshots, recordings, most game data)
- **SD card drive** → the Deck's `/run/media/mmcblk0p1`

Drive letters are picked automatically (reuses your previous letters, scans backwards from Z if taken). Copy, delete, rename — just like a USB flash drive. Typical speed 50–150 MB/s, pure streaming with zero C: drive staging.

## How it works

The Deck pretends to be a USB network adapter → a tiny private network forms between PC and Deck → files travel over SSH/SFTP → rclone + WinFsp turns remote folders into drive letters. A tray app detects plug/unplug and mounts automatically.

## What you need

- A Steam Deck (you'll use **Desktop Mode** once)
- A **USB-C data cable** (charge-only cables will NOT work; a phone's original cable is usually fine)
- A Windows 10 / 11 PC (.NET 4.8 is built into Windows — the tray app runs as-is, no runtime to install)

---

## Step 1: Steam Deck setup (one-time, ~5 min)

1. Copy the **3 files** from the `deck` folder (`usb-gadget.sh`, `usb-gadget.service`, `install.sh`) to your Deck.
   - Easiest: copy to a USB flash drive, plug it into the Deck (via dock/adapter), copy to the home folder in Desktop Mode.
   - Or transfer wirelessly with KDE Connect / Warpinator.
2. On the Deck, press the power button → **Switch to Desktop**.
3. Open the **Konsole** terminal.
4. Set a login password if you never did (**you'll need it on the Windows side**):
   ```
   passwd
   ```
5. Go to the folder with the files, e.g. `cd ~/deck`, then run:
   ```
   chmod +x install.sh
   ./install.sh
   ```
6. `Setup finished!` means done. The Deck side runs automatically on every boot from now on.

---

## Step 2: Windows setup (one-time, ~3 min)

1. Connect the Deck to the PC with the USB-C cable and wait ~10 seconds (Windows detects a new "network adapter" — that's expected).
2. Double-click **`DeckDriveTray.exe`**.
3. The **setup wizard** opens on first run and checks five things: rclone, WinFsp, adapter IP, connection config (enter your Deck password here), and reachability.
   - Click the button on any red item; click **Yes** when Windows asks for permission.
   - When everything is green, click **Done**.
4. The app then lives quietly in the system tray: gray = not mounted, green = mounted.

> The UI follows your system language; switch anytime via the tray menu "Language 语言".

---

## Daily use (zero clicks)

- **Plug in** → both drives mount within seconds
- **Unplug** → drives eject automatically
- Tray menu: Mount / Unmount (manual control), Auto-mount on plug (toggle), Setup wizard, Language, Exit

> Don't unplug during a file transfer — same rule as a USB flash drive.

---

## Advanced: manual scripts (fallback)

If you prefer not to use the tray app, or need a one-off on another PC:

- `windows\setup.ps1`: command-line one-time setup (run in an admin PowerShell)
- `windows\mount-deck.bat`: double-click to mount X: / Y:; `unmount-deck.bat` to eject
- `app\build.bat`: rebuild the tray app yourself with Windows' built-in .NET compiler (source in `app\`)

---

## FAQ

**Q: How are drive letters chosen?**  
Previous letters are reused when free; otherwise two free letters are scanned backwards from Z — the bigger one goes to the home folder, the smaller to the SD card. You don't need to remember letters; look for the volume labels "Steam Deck" and "Deck SD Card".

**Q: What if no microSD card is inserted?**  
The SD card drive fails to mount; the home drive works normally.

**Q: Why was my C: drive busy while copying (older versions)?**  
Previous versions used `--vfs-cache-mode writes/minimal`, which staged files on C:. The current version uses **`off`**: pure in-memory streaming, zero C: staging. Trade-off: editing files in place on the drive isn't supported; copy in/out works perfectly.

**Q: Nothing happens when I plug in the cable?**

- Try a cable you know can transfer data (the most common cause).
- On the Deck, run `systemctl status usb-gadget` in Konsole — a green `active (exited)` means it's working.

**Q: Newer Windows 11 doesn't recognize the network adapter?**  
Recent Windows 11 builds are phasing out RNDIS. On the Deck, run `sudo nano /etc/systemd/system/usb-gadget.service`, change `Environment=FUNC=rndis` to `ncm`, save, then `sudo systemctl daemon-reload && sudo systemctl restart usb-gadget`.

**Q: Stopped working after a big SteamOS update?**  
Re-run `./install.sh` on the Deck.

**Q: Is it secure?**  
The "network" contains only your PC and your Deck. Your password lives only in the rclone config on your PC; nothing goes through the internet.

**Q: I have DeckMTP installed?**  
Both use the same USB controller — disable one of them: `sudo systemctl disable --now deckmtp`

---

## License

MIT — see [LICENSE](LICENSE). Use it however you like; no warranty provided.
