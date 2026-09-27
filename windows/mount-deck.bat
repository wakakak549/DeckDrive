@echo off
REM Mount the Steam Deck:
REM   X: = Deck home folder (/home/deck)
REM   Y: = Deck TF/SD card  (/run/media/mmcblk0p1)
REM Run unmount-deck.bat to eject both drives.

where rclone >nul 2>nul
if errorlevel 1 (
    echo rclone not found. Please run setup.ps1 first.
    pause
    exit /b 1
)

echo [1/2] Mounting Deck home folder as drive X: ...
start "Steam Deck X" /min rclone mount deck:/home/deck X: --vfs-cache-mode minimal --volname "Steam Deck"
timeout /t 3 >nul
if exist X:\ (
    echo       OK - X: is ready.
) else (
    echo       FAILED - X: not mounted. Check the cable and the Deck side setup.
    pause
    exit /b 1
)

echo [2/2] Mounting Deck SD card as drive Y: ...
start "Steam Deck Y" /min rclone mount deck:/run/media/mmcblk0p1 Y: --vfs-cache-mode minimal --volname "Deck SD Card"
timeout /t 3 >nul
if exist Y:\ (
    echo       OK - Y: is ready ^(SD card^).
) else (
    echo       Note - Y: not mounted. No SD card inserted, or card has an issue.
)

echo.
echo All done. Two minimized windows keep the drives alive - do not close them.
echo To eject: run unmount-deck.bat
pause
