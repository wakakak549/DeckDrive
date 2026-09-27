# Steam Deck USB drive - one-time setup for Windows
# Right-click this file -> "Run with PowerShell", approve the admin prompt.
#Requires -RunAsAdministrator
$ErrorActionPreference = "Continue"

Write-Host "== Step 1/4: Install rclone and WinFsp ==" -ForegroundColor Cyan
winget install -e --id Rclone.Rclone --accept-package-agreements --accept-source-agreements
winget install -e --id WinFsp.WinFsp --accept-package-agreements --accept-source-agreements

# refresh PATH so we can call rclone right away
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

Write-Host ""
Write-Host "== Step 2/4: Set static IP 10.66.0.2 on the USB network adapter ==" -ForegroundColor Cyan
$adapter = Get-NetAdapter | Where-Object {
    $_.InterfaceDescription -match "RNDIS|Remote NDIS|NCM|USB Ethernet" -and $_.Status -ne "Disabled"
} | Select-Object -First 1

if (-not $adapter) {
    Write-Host "WARNING: USB network adapter not found." -ForegroundColor Yellow
    Write-Host "Plug the Steam Deck into this PC with a USB-C data cable, wait 10 seconds, then re-run this script."
} else {
    Get-NetIPAddress -InterfaceIndex $adapter.ifIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Remove-NetIPAddress -Confirm:$false -ErrorAction SilentlyContinue
    New-NetIPAddress -InterfaceIndex $adapter.ifIndex -IPAddress 10.66.0.2 -PrefixLength 24 | Out-Null
    Write-Host ("OK: adapter '" + $adapter.Name + "' now has IP 10.66.0.2") -ForegroundColor Green
}

Write-Host ""
Write-Host "== Step 3/4: Create the rclone remote named 'deck' ==" -ForegroundColor Cyan
$pw = Read-Host "Enter the Steam Deck login password (user 'deck')" -AsSecureString
$pwPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($pw))
$obscured = (& rclone obscure $pwPlain).Trim()
& rclone config create deck sftp host 10.66.0.1 user deck pass $obscured shell_type unix | Out-Null
Write-Host "Remote 'deck' created. Testing connection..."
& rclone lsd deck:/home/deck --max-depth 1
if ($LASTEXITCODE -eq 0) {
    Write-Host "OK: connected to the Steam Deck." -ForegroundColor Green
} else {
    Write-Host "WARNING: connection test failed. Check that the cable is plugged in and the Deck setup is done." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "== Step 4/4: All done ==" -ForegroundColor Cyan
Write-Host "From now on: plug in the cable, then double-click mount-deck.bat to get drive X:"
Read-Host "Press Enter to close"
