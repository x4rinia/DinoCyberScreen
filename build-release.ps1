[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $projectRoot 'DinoCyberScreen.sln'
$project = Join-Path $projectRoot 'src\DinoCyberScreen\DinoCyberScreen.csproj'
$launcherProject = Join-Path $projectRoot 'src\DinoCyberScreen.Launcher\DinoCyberScreen.Launcher.csproj'
$installerDirectory = Join-Path $projectRoot 'installer'
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

dotnet restore $solution
dotnet build $solution -c Release --no-restore
dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore -o $releaseDirectory
dotnet publish $launcherProject -c Release -r win-x64 --self-contained true --no-restore -o $launcherStaging

$executable = Join-Path $releaseDirectory 'DinoCyberScreen.exe'
$screensaver = Join-Path $releaseDirectory 'DinoCyberScreen.scr'
$launcher = Join-Path $launcherStaging 'DinoCyberScreen.Launcher.exe'
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Publish output missing: $executable"
}
if (-not (Test-Path -LiteralPath $launcher)) {
    throw "Launcher publish output missing: $launcher"
}

Copy-Item -LiteralPath $launcher -Destination $screensaver -Force
Copy-Item -LiteralPath (Join-Path $installerDirectory 'Install-Screensaver.bat') -Destination $releaseDirectory -Force
Copy-Item -LiteralPath (Join-Path $installerDirectory 'Uninstall-Screensaver.bat') -Destination $releaseDirectory -Force
Remove-Item -LiteralPath $absoluteStaging -Recurse -Force

$requiredFiles = @(
    $screensaver,
    (Join-Path $releaseDirectory 'Install-Screensaver.bat'),
    (Join-Path $releaseDirectory 'Uninstall-Screensaver.bat'),
    (Join-Path $releaseDirectory 'Web\index.html'),
    (Join-Path $releaseDirectory 'Web\assets\ankylo-hologram.png')
)
foreach ($requiredFile in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $requiredFile)) {
        throw "Release file missing: $requiredFile"
    }
}
Write-Host "Release ready: $releaseDirectory" -ForegroundColor Cyan

# ── Dino_SCR folder: minimal set of runtime files ───────────────────────────
$dinoScrDir = Join-Path $projectRoot 'Dino_SCR'
$absoluteDinoScr = [IO.Path]::GetFullPath($dinoScrDir)
if ([IO.Path]::GetDirectoryName($absoluteDinoScr) -ne [IO.Path]::GetFullPath($projectRoot) -or
    [IO.Path]::GetFileName($absoluteDinoScr) -ne 'Dino_SCR') {
    throw "Unsafe Dino_SCR directory: $absoluteDinoScr"
}
if (Test-Path -LiteralPath $absoluteDinoScr) {
    Remove-Item -LiteralPath $absoluteDinoScr -Recurse -Force
}
New-Item -ItemType Directory -Path $absoluteDinoScr | Out-Null

# Copy only the runtime payload (no source files, no build artifacts, no docs)
# Exclude: *.pdb, *.xml, *.bat, *.deps.json, *.runtimeconfig.json, Install/Uninstall scripts
& "$env:SystemRoot\System32\robocopy.exe" `
    $releaseDirectory $absoluteDinoScr `
    /E /COPY:DAT /DCOPY:DAT /R:2 /W:1 /NFL /NDL /NJH /NJS /NP `
    /XF "*.pdb" "*.xml" "*.bat" "*.deps.json" "*.runtimeconfig.json" | Out-Null
# robocopy exit codes 0-7 are success; 8+ indicate errors
if ($LASTEXITCODE -ge 8) { throw "robocopy failed copying to Dino_SCR (exit $LASTEXITCODE)" }

# Verify the key screensaver stub is present
$scrTarget = Join-Path $absoluteDinoScr 'DinoCyberScreen.scr'
if (-not (Test-Path -LiteralPath $scrTarget)) {
    throw "Dino_SCR is missing DinoCyberScreen.scr"
}
$exeTarget = Join-Path $absoluteDinoScr 'DinoCyberScreen.exe'
if (-not (Test-Path -LiteralPath $exeTarget)) {
    throw "Dino_SCR is missing DinoCyberScreen.exe"
}

$scrFileCount = (Get-ChildItem -LiteralPath $absoluteDinoScr -Recurse -File).Count
Write-Host "Dino_SCR ready: $absoluteDinoScr ($scrFileCount files)" -ForegroundColor Cyan
