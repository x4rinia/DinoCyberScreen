[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $projectRoot 'DinoCyberScreen.sln'
$project = Join-Path $projectRoot 'src\DinoCyberScreen\DinoCyberScreen.csproj'
$launcherProject = Join-Path $projectRoot 'src\DinoCyberScreen.Launcher\DinoCyberScreen.Launcher.csproj'
$releaseDirectory = Join-Path $projectRoot 'release'
$launcherStaging = Join-Path $projectRoot '.launcher-publish'

$absoluteRelease = [IO.Path]::GetFullPath($releaseDirectory)
if ([IO.Path]::GetDirectoryName($absoluteRelease) -ne [IO.Path]::GetFullPath($projectRoot) -or
    [IO.Path]::GetFileName($absoluteRelease) -ne 'release') {
    throw "Unsafe release directory: $absoluteRelease"
}
if (Test-Path -LiteralPath $absoluteRelease) {
    Remove-Item -LiteralPath $absoluteRelease -Recurse -Force
}

$absoluteStaging = [IO.Path]::GetFullPath($launcherStaging)
if ([IO.Path]::GetDirectoryName($absoluteStaging) -ne [IO.Path]::GetFullPath($projectRoot) -or
    [IO.Path]::GetFileName($absoluteStaging) -ne '.launcher-publish') {
    throw "Unsafe launcher staging directory: $absoluteStaging"
}
if (Test-Path -LiteralPath $absoluteStaging) {
    Remove-Item -LiteralPath $absoluteStaging -Recurse -Force
}

$dinoScrDir = Join-Path $releaseDirectory 'Dino_SCR'

dotnet restore $solution
dotnet build $solution -c Release --no-restore
dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore -o $dinoScrDir
dotnet publish $launcherProject -c Release -r win-x64 --self-contained true --no-restore -o $launcherStaging

$executable = Join-Path $dinoScrDir 'DinoCyberScreen.exe'
$screensaver = Join-Path $dinoScrDir 'DinoCyberScreen.scr'
$launcher = Join-Path $launcherStaging 'DinoCyberScreen.Launcher.exe'
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Publish output missing: $executable"
}
if (-not (Test-Path -LiteralPath $launcher)) {
    throw "Launcher publish output missing: $launcher"
}

Copy-Item -LiteralPath $launcher -Destination $screensaver -Force
$installScript = @'
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
rem Legacy-Treiber frueherer Builds entfernen, damit Dino_SCR sauber aktualisiert werden kann.
sc stop R0DinoCyberScreen >nul 2>&1
sc delete R0DinoCyberScreen >nul 2>&1
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
if exist "%TARGET_DIR%" rmdir /s /q "%TARGET_DIR%"
if exist "%TARGET_DIR%" (
  echo FEHLER: Der bestehende Zielordner "%TARGET_DIR%" konnte nicht vollstaendig bereinigt werden.
  echo Bitte laufende DinoCyberScreen-Prozesse beenden und die Installation erneut starten.
  pause
  exit /b 4
)
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
'@
Set-Content -Path (Join-Path $releaseDirectory 'Install-Screensaver.bat') -Value $installScript -Encoding Ascii

$uninstallScript = @'
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
sc stop R0DinoCyberScreen >nul 2>&1
sc delete R0DinoCyberScreen >nul 2>&1
timeout /t 2 /nobreak >nul

rem --- 2. Dateien entfernen ---
set "TARGET_DIR=%SystemRoot%\Dino_SCR"
set "TARGET_SCR=%SystemRoot%\System32\DinoCyberScreen.scr"

echo.
echo Entferne DinoCyberScreen...

if exist "%TARGET_SCR%" (
  del /f /q "%TARGET_SCR%"
  echo - %TARGET_SCR% geloescht.
)

if exist "%TARGET_DIR%" (
  rmdir /s /q "%TARGET_DIR%"
  if exist "%TARGET_DIR%" (
    echo FEHLER: %TARGET_DIR% konnte nicht vollstaendig entfernt werden.
    exit /b 5
  )
  echo - %TARGET_DIR% geloescht.
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
'@
Set-Content -Path (Join-Path $releaseDirectory 'Uninstall-Screensaver.bat') -Value $uninstallScript -Encoding Ascii
Remove-Item -LiteralPath $absoluteStaging -Recurse -Force

# Clean up unnecessary files from payload
Remove-Item -Path (Join-Path $dinoScrDir "*.pdb") -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $dinoScrDir "*.xml") -Force -ErrorAction SilentlyContinue

$requiredFiles = @(
    $screensaver,
    $executable,
    (Join-Path $releaseDirectory 'Install-Screensaver.bat'),
    (Join-Path $releaseDirectory 'Uninstall-Screensaver.bat'),
    (Join-Path $dinoScrDir 'Web\index.html'),
    (Join-Path $dinoScrDir 'Web\assets\ankylo-hologram.png'),
    (Join-Path $dinoScrDir 'Web\assets\dino-special-rainbow.png')
)
foreach ($requiredFile in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $requiredFile)) {
        throw "Release file missing: $requiredFile"
    }
}
$scrFileCount = (Get-ChildItem -LiteralPath $dinoScrDir -Recurse -File).Count
Write-Host "Release ready in: $absoluteRelease ($scrFileCount payload files)" -ForegroundColor Cyan
