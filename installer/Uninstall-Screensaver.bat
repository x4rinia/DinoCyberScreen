@echo off
setlocal EnableExtensions

set "INSTALL=%SystemRoot%\Dino_SCR"
set "SYSTEM_SCR=%SystemRoot%\System32\DinoCyberScreen.scr"

if not defined SystemRoot (
  echo FEHLER: SystemRoot ist nicht definiert.
  exit /b 1
)

"%SystemRoot%\System32\fltmc.exe" >nul 2>&1
if errorlevel 1 (
  echo Administratorrechte werden angefordert...
  powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -ArgumentList '%*' -Verb RunAs"
  exit /b
)

if /I not "%INSTALL%"=="%SystemRoot%\Dino_SCR" (
  echo FEHLER: Unerwarteter Installationspfad: "%INSTALL%"
  exit /b 2
)
if /I not "%SYSTEM_SCR%"=="%SystemRoot%\System32\DinoCyberScreen.scr" (
  echo FEHLER: Unerwarteter Screensaver-Pfad: "%SYSTEM_SCR%"
  exit /b 2
)

if exist "%SYSTEM_SCR%" del /f /q "%SYSTEM_SCR%"
if exist "%INSTALL%" rmdir /s /q "%INSTALL%"

if exist "%SYSTEM_SCR%" goto :remove_error
if exist "%INSTALL%" goto :remove_error

echo DinoCyberScreen wurde entfernt.
exit /b 0

:remove_error
echo FEHLER: DinoCyberScreen konnte nicht vollstaendig entfernt werden.
exit /b 3
