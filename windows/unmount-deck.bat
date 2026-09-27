@echo off
REM Eject drive X: by stopping the rclone background process.
taskkill /F /IM rclone.exe >nul 2>nul
if errorlevel 1 (
    echo Drive X: was not mounted.
) else (
    echo Drive X: ejected.
)
pause
