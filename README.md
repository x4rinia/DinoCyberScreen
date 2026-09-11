# DinoCyberScreen

Ein futuristischer technischer Windows-Bildschirmschoner und Echtzeit-Systemmonitor. Die Anwendung kombiniert einen nativen .NET-8/WPF-Host mit einer hardwarebeschleunigten WebView2-Oberfläche aus HTML, CSS, Canvas und WebGL.

Konzept und Projekt: **by X4RiNiA**.

## Installieren

Die fertige Version liegt unter `release/`. Für die saubere Windows-Installation:

1. `release\Install-Screensaver.bat` per Doppelklick starten.
2. Die Abfrage der Benutzerkontensteuerung bestätigen.
3. Auf Wunsch danach direkt die Windows-Bildschirmschoner-Einstellungen öffnen.

Der Installer kopiert die vollständige Anwendung nach `%SystemRoot%\Dino_SCR\` (normalerweise `C:\Windows\Dino_SCR\`) und den kleinen eigenständigen Launcher nach `%SystemRoot%\System32\DinoCyberScreen.scr`. Dieser Launcher startet immer die installierte Anwendung und hat keine Abhängigkeit vom Entwicklungs- oder Release-Ordner.

Zum Entfernen ausschließlich dieser beiden Ziele `release\Uninstall-Screensaver.bat` ausführen.

## Direkt starten und Screensaver-Modi

```powershell
# normale Fensteransicht
.\release\DinoCyberScreen.exe

# Vollbild auf den gewählten Monitoren
.\release\DinoCyberScreen.exe /s

# Konfigurationsfenster
.\release\DinoCyberScreen.exe /c

# Windows-Vorschaufenster (normalerweise von Windows aufgerufen)
.\release\DinoCyberScreen.exe /p 123456
```

Nach der Installation funktionieren dieselben Argumente über:

```powershell
C:\Windows\System32\DinoCyberScreen.scr /s
C:\Windows\System32\DinoCyberScreen.scr /c
```

Im Screensaver-Modus ist das Fenster rahmenlos, im Vordergrund und der Cursor unsichtbar. Tastendruck, Mausklick oder eine deutliche Mausbewegung beendet alle Monitorfenster. Kleine Bewegungen während der ersten 1,5 Sekunden werden ignoriert.

## Einstellungen und Themes

Start mit `/c` oder über das Zahnrad in der normalen Ansicht. Konfigurierbar sind:

- echte oder simulierte Systemdaten
- GPU-, Netzwerk-, Terminal-, Hex-, Event- und DINO-Module
- alle Monitore oder nur der Hauptmonitor
- 30/60 FPS und Low/Medium/High-Qualität
- automatischer Moduswechsel nach 30/60/120/180 Sekunden
- Akzent-Theme Blue (futuristisch/clean) oder Green (taktisch/technisch)
- Hintergrundstil Pure Black (Standard), Dark Blue Tint oder Dark Green Tint

Theme und Hintergrundstil sind getrennt: Auch Blue oder Green kann mit Pure Black verwendet werden. Änderungen über das Zahnrad werden ohne Neustart auf das laufende HUD angewendet. Ältere oder ungültige Theme-Werte werden sicher auf Blue/Pure Black normalisiert.

Einstellungen und WebView2-Benutzerdaten werden ohne Administratorrechte gespeichert:

```text
%LOCALAPPDATA%\DinoCyberScreen\settings.json
%LOCALAPPDATA%\DinoCyberScreen\WebView2\
```

Unter `C:\Windows\` werden keine veränderlichen Benutzerdaten angelegt.

## Telemetrie und Datenschutz

Die Anwendung liest ausschließlich lokale Systemwerte:

- CPU-Gesamtlast, CPU-Kerne, CPU-Name und verfügbare Temperatursensoren
- RAM-Nutzung
- GPU-Last, VRAM und Temperatur, soweit Treiber und Sensoren dies anbieten
- lokaler Netzwerkdurchsatz aktiver Adapter
- Systemlaufwerk, freier Speicher sowie Lese-/Schreibrate
- lokale Prozessnamen, CPU- und Speichernutzung
- Rechnername, Windows-Version, Uhrzeit und Uptime

Die NETWORK-GRID-Ansicht ist rein visuell. Es gibt keine Portscans, Angriffe, Passwortfunktionen, Fremdsystemabfragen oder Prozessspeicherzugriffe. Nicht verfügbare Sensorwerte erscheinen als `N/A`.

## Build

Voraussetzungen: Windows 10/11, .NET 8 SDK und Microsoft Edge WebView2 Runtime.

```powershell
.\build-release.ps1
```

Das Skript baut beide Projekte, veröffentlicht die vollständige win-x64-Anwendung, erzeugt den eigenständigen System32-Launcher und legt Installer sowie Uninstaller in `release\` ab.

## Projektstruktur

```text
DinoCyberScreen/
├── DinoCyberScreen.sln
├── build-release.ps1
├── installer/                         Installer-Vorlagen
├── docs/                              Designreferenzen und QA-Screenshots
├── release/                           installierbare win-x64-Ausgabe
└── src/
    ├── DinoCyberScreen/               WPF-App, Telemetrie und Web-HUD
    └── DinoCyberScreen.Launcher/      eigenständiger System32-.scr-Launcher
```

## Bekannte Einschränkungen

- Die Microsoft Edge WebView2 Runtime ist auch beim Self-contained-.NET-Publish erforderlich. Windows 11 bringt sie üblicherweise bereits mit; auf älteren Windows-10-Systemen muss sie eventuell installiert werden.
- Hardwaretemperaturen, GPU-Auslastung und VRAM hängen von GPU, Treiber, Sensorfreigabe und Berechtigungen ab. Fehlende Werte beeinträchtigen die übrige Anzeige nicht.
- Manche Windows-Systeme stellen lokalisierte oder gesperrte Performance Counter für Datenträger nicht bereit. Dann bleiben Lese-/Schreibraten auf `N/A`, während Kapazität und freier Speicher funktionieren.
- Die klassische `/p`-Vorschau ist implementiert, kann aber abhängig von Windows-Dialog und DPI-Modus kleiner skaliert erscheinen.

## Design- und QA-Artefakte

- `docs/design-concept-black.png` – generierte Referenz für den schwarzen High-Tech-Cockpit-Look
- `Web/assets/ankylo-hologram.png` – aus der bereitgestellten Ankylosaurus-Referenz entwickeltes Core-Signet
- `docs/implementation-black-1920x1080.png` – geprüfter Blue/Pure-Black-Render
- `docs/implementation-black-green-1920x1080.png` – geprüfter Green/Pure-Black-Render
