@echo off
setlocal EnableExtensions

rem --- 1. Admin-Rechte pruefen ---
"%SystemRoot%\System32\fltmc.exe" >nul 2>&1
if errorlevel 1 (
  echo Administratorrechte werden angefordert...
  set "ARGS=%*"
  if "%*"=="" (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  ) else (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -ArgumentList '%*' -Verb RunAs"
  )
  exit /b
)

echo Beende laufende Bildschirmschoner-Prozesse...
powershell -Command "Get-Process -Name '*DinoCyber*' -ErrorAction SilentlyContinue | Stop-Process -Force"
timeout /t 2 /nobreak >nul

rem --- 2. Dateien entfernen ---
set "TARGET_DIR=%SystemRoot%\Dino_SCR"
set "TARGET_SCR=%SystemRoot%\System32\DinoCyberScreen.scr"

echo.
echo Entferne DinoCyberScreen...

if exist "%TARGET_DIR%" (
  rmdir /s /q "%TARGET_DIR%"
  echo - %TARGET_DIR% geloescht.
)

if exist "%TARGET_SCR%" (
  del /f /q "%TARGET_SCR%"
  echo - %TARGET_SCR% geloescht.
)

echo.
echo Deinstallation abgeschlossen.
echo.

if /I "%~1"=="/NOOPEN" goto :done
choice /M "Windows-Bildschirmschonereinstellungen jetzt oeffnen"
if errorlevel 2 goto :done
control.exe desk.cpl,,@screensaver

:done
exit /b 0
