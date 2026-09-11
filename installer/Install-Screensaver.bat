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
sc stop DinoCyberScreen >nul 2>&1
timeout /t 2 /nobreak >nul

rem --- 2. Quelle definieren ---
set "SOURCE_DIR=%~dp0Dino_SCR"
set "TARGET_DIR=%SystemRoot%\Dino_SCR"
set "TARGET_SCR=%SystemRoot%\System32\DinoCyberScreen.scr"

rem --- 3. Quelldatei pruefen ---
if not exist "%SOURCE_DIR%\DinoCyberScreen.exe" (
  echo.
  echo FEHLER: Quelldatei nicht gefunden!
  echo Erwarteter Pfad: "%SOURCE_DIR%\DinoCyberScreen.exe"
  echo Bitte starte das Skript direkt aus dem Release-Ordner.
  pause
  exit /b 3
)

if not exist "%SOURCE_DIR%\DinoCyberScreen.scr" (
  echo FEHLER: DinoCyberScreen.scr fehlt im Quellordner.
  pause
  exit /b 3
)

rem --- 4. Zielordner erstellen ---
echo.
echo Installiere DinoCyberScreen nach "%TARGET_DIR%"...
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"
if errorlevel 1 (
  echo FEHLER: Der Zielordner "%TARGET_DIR%" konnte nicht erstellt werden.
  pause
  exit /b 4
)

rem --- 5. Dateien kopieren ---
"%SystemRoot%\System32\robocopy.exe" "%SOURCE_DIR%" "%TARGET_DIR%" /E /COPY:DAT /DCOPY:DAT /R:2 /W:1 /NFL /NDL /NJH /NJS /NP /XF "*.sys" >nul
if errorlevel 8 (
  echo FEHLER: Dateien konnten nicht kopiert werden. Robocopy Code: %ERRORLEVEL%
  pause
  exit /b 5
)

rem --- 6. Ueberpruefen EXE ---
if not exist "%TARGET_DIR%\DinoCyberScreen.exe" (
  echo FEHLER: Installation fehlgeschlagen. "%TARGET_DIR%\DinoCyberScreen.exe" fehlt.
  pause
  exit /b 7
)

rem --- 7. SCR kopieren und ueberpruefen ---
copy /Y "%SOURCE_DIR%\DinoCyberScreen.scr" "%TARGET_SCR%" >nul
if errorlevel 1 (
  echo FEHLER: DinoCyberScreen.scr konnte nicht nach System32 kopiert werden.
  pause
  exit /b 6
)

if not exist "%TARGET_SCR%" (
  echo FEHLER: "%TARGET_SCR%" wurde nach dem Kopieren nicht gefunden.
  pause
  exit /b 7
)

rem --- Erfolgreich ---
echo.
echo DinoCyberScreen wurde erfolgreich installiert!
echo - Payload: %TARGET_DIR%
echo - Screensaver: %TARGET_SCR%
echo.

if /I "%~1"=="/NOOPEN" goto :done
choice /M "Windows-Bildschirmschonereinstellungen jetzt oeffnen"
if errorlevel 2 goto :done
control.exe desk.cpl,,@screensaver

:done
exit /b 0
