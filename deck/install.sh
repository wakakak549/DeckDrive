#!/bin/bash
# One-time setup on the Steam Deck (Desktop Mode).
# Run:  chmod +x install.sh && ./install.sh
set -e

SRC_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "== Steam Deck USB network drive setup =="

# 1. install the gadget script into your home folder
#    (home folder survives SteamOS system updates, /usr/local does not)
cp "$SRC_DIR/usb-gadget.sh" /home/deck/usb-gadget.sh
chmod +x /home/deck/usb-gadget.sh

# 2. install and start the systemd service (auto-start on every boot)
sudo cp "$SRC_DIR/usb-gadget.service" /etc/systemd/system/usb-gadget.service
sudo systemctl daemon-reload
sudo systemctl enable --now usb-gadget.service

# 3. enable the built-in SSH/SFTP server
sudo systemctl enable --now sshd

echo
echo "--------------------------------------------------"
echo "Setup finished!"
echo
echo "Deck side IP: 10.66.0.1 (created automatically when you plug in the cable)"
echo
echo "IMPORTANT: if you never set a login password on this Deck,"
echo "run this now and choose one (you need it on the Windows side):"
echo "    passwd"
echo "--------------------------------------------------"
