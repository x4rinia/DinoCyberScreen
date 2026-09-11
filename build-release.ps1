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
Copy-Item -LiteralPath (Join-Path $installerDirectory 'Install-Screensaver.bat') -Destination $releaseDirectory -Force
Copy-Item -LiteralPath (Join-Path $installerDirectory 'Uninstall-Screensaver.bat') -Destination $releaseDirectory -Force
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
    (Join-Path $dinoScrDir 'Web\assets\ankylo-hologram.png')
)
foreach ($requiredFile in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $requiredFile)) {
        throw "Release file missing: $requiredFile"
    }
}
$scrFileCount = (Get-ChildItem -LiteralPath $dinoScrDir -Recurse -File).Count
Write-Host "Release ready in: $absoluteRelease ($scrFileCount payload files)" -ForegroundColor Cyan
