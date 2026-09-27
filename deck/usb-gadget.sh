#!/bin/bash
# Steam Deck USB network gadget
# Makes the Deck appear as a USB network adapter when plugged into a PC.
# Usage: usb-gadget.sh start|stop [rndis|ncm]
#   rndis = default, works on Windows 10/11 (older)
#   ncm   = fallback for newer Windows 11 builds that dropped RNDIS

set -e

GADGET_DIR=/sys/kernel/config/usb_gadget/deck
DEV_IP=10.66.0.1
PREFIX=24
FUNC_TYPE="${2:-rndis}"

start() {
    modprobe libcomposite
    mountpoint -q /sys/kernel/config || mount -t configfs none /sys/kernel/config

    if [ -d "$GADGET_DIR" ]; then
        stop || true
    fi

    mkdir -p "$GADGET_DIR"
    cd "$GADGET_DIR"

    echo 0x28de > idVendor      # Valve
    echo 0x1205 > idProduct
    echo 0x0200 > bcdUSB
    echo 0x0100 > bcdDevice

    mkdir -p strings/0x409
    echo "0123456789"        > strings/0x409/serialnumber
    echo "Valve"             > strings/0x409/manufacturer
    echo "Steam Deck USB Net" > strings/0x409/product

    mkdir -p configs/c.1/strings/0x409
    echo "USB Network" > configs/c.1/strings/0x409/configuration
    echo 250 > configs/c.1/MaxPower

    if [ "$FUNC_TYPE" = "ncm" ]; then
        mkdir -p functions/ncm.usb0
        echo "02:00:00:00:00:01" > functions/ncm.usb0/dev_addr
        echo "02:00:00:00:00:02" > functions/ncm.usb0/host_addr
        ln -s functions/ncm.usb0 configs/c.1/
    else
        mkdir -p functions/rndis.usb0
        echo "02:00:00:00:00:01" > functions/rndis.usb0/dev_addr
        echo "02:00:00:00:00:02" > functions/rndis.usb0/host_addr
        # Microsoft OS descriptors: let Windows auto-load its RNDIS driver
        echo 1       > os_desc/use
        echo 0xcd    > os_desc/b_vendor_code
        echo MSFT100 > os_desc/qw_sign
        echo RNDIS   > functions/rndis.usb0/os_desc/interface.rndis/compatible_id
        echo 5162001 > functions/rndis.usb0/os_desc/interface.rndis/sub_compatible_id
        ln -s functions/rndis.usb0 configs/c.1/
        ln -s configs/c.1 os_desc/
    fi

    ls /sys/class/udc | head -n1 > UDC

    # wait for usb0 to appear, then give it an IP address
    for i in $(seq 1 20); do
        [ -d /sys/class/net/usb0 ] && break
        sleep 0.5
    done
    ip link set usb0 up
    ip addr replace "$DEV_IP/$PREFIX" dev usb0

    echo "USB gadget up: $FUNC_TYPE on usb0, IP $DEV_IP"
}

stop() {
    [ -d "$GADGET_DIR" ] || exit 0
    cd "$GADGET_DIR"
    echo "" > UDC || true
    rm -f os_desc/c.1
    rm -f configs/c.1/*.usb0
    rmdir functions/*.usb0 2>/dev/null || true
    rmdir configs/c.1/strings/0x409 configs/c.1 2>/dev/null || true
    rmdir strings/0x409 2>/dev/null || true
    cd /
    rmdir "$GADGET_DIR" 2>/dev/null || true
}

case "$1" in
    start) start ;;
    stop)  stop  ;;
    *) echo "Usage: $0 start|stop [rndis|ncm]"; exit 1 ;;
esac
