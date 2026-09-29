@echo off
REM Build DeckDriveTray.exe with the .NET Framework 4.x compiler
REM (preinstalled on every Windows 10/11, no SDK needed).

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo Compiler not found: %CSC%
    pause
    exit /b 1
)

"%CSC%" /nologo /target:winexe /platform:anycpu /utf8output ^
    /out:DeckDriveTray.exe Program.cs Loc.cs WizardForm.cs ^
    /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Management.dll

if errorlevel 1 (
    echo.
    echo BUILD FAILED
    pause
    exit /b 1
)

echo.
echo BUILD OK: DeckDriveTray.exe
pause
