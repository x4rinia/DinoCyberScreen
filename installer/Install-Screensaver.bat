@echo off
setlocal EnableExtensions

rem Append a dot so the quoted source never ends with a backslash (Robocopy parsing).
set "SOURCE=%~dp0."
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

if not exist "%SOURCE%\DinoCyberScreen.exe" (
  echo FEHLER: DinoCyberScreen.exe fehlt im Release-Ordner.
  exit /b 3
)
if not exist "%SOURCE%\DinoCyberScreen.scr" (
  echo FEHLER: DinoCyberScreen.scr fehlt im Release-Ordner.
  exit /b 3
)
if not exist "%SOURCE%\Web\index.html" (
  echo FEHLER: Web\index.html fehlt im Release-Ordner.
  exit /b 3
)
if not exist "%SOURCE%\Web\assets\ankylo-hologram.png" (
  echo FEHLER: Das Ankylosaurus-Asset fehlt im Release-Ordner.
  exit /b 3
)

echo Installiere DinoCyberScreen nach "%INSTALL%"...
if exist "%INSTALL%" rmdir /s /q "%INSTALL%"
mkdir "%INSTALL%"
if errorlevel 1 goto :directory_error

"%SystemRoot%\System32\robocopy.exe" "%SOURCE%" "%INSTALL%" /E /COPY:DAT /DCOPY:DAT /R:2 /W:1 /NFL /NDL /NJH /NJS /NP /XF Install-Screensaver.bat Uninstall-Screensaver.bat /XD DinoCyberScreen.exe.WebView2 >nul
if errorlevel 8 goto :payload_error

copy /Y "%SOURCE%\DinoCyberScreen.scr" "%SYSTEM_SCR%" >nul
if errorlevel 1 goto :screensaver_error

if not exist "%INSTALL%\DinoCyberScreen.exe" goto :verify_error
if not exist "%INSTALL%\Web\index.html" goto :verify_error
if not exist "%SYSTEM_SCR%" goto :verify_error

echo.
echo DinoCyberScreen wurde erfolgreich installiert.
echo Payload: %INSTALL%
echo Screensaver: %SYSTEM_SCR%

if /I "%~1"=="/NOOPEN" goto :done
choice /M "Windows-Bildschirmschonereinstellungen jetzt oeffnen"
if errorlevel 2 goto :done
control.exe desk.cpl,,@screensaver

:done
exit /b 0

:directory_error
echo.
echo FEHLER: Der Installationsordner konnte nicht erstellt werden.
exit /b 4

:payload_error
set "ROBOCOPY_RESULT=%ERRORLEVEL%"
echo.
echo FEHLER: Die Anwendungsdateien konnten nicht kopiert werden. Robocopy-Code: %ROBOCOPY_RESULT%
exit /b 5

:screensaver_error
echo.
echo FEHLER: DinoCyberScreen.scr konnte nicht nach System32 kopiert werden.
exit /b 6

:verify_error
echo.
echo FEHLER: Die installierten Dateien konnten nicht verifiziert werden.
exit /b 7
