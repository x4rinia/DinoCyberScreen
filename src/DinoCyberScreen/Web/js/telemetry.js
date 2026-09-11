const fallback = {
  timestamp: new Date().toISOString(), computerName: 'LOCAL NODE', windowsVersion: 'WINDOWS', uptime: '00:00:00',
  cpu: { name: 'PROCESSOR ARRAY', usage: 38, temperature: null, cores: [30,42,36,51,32,28,48,39] },
  gpu: { name: 'GRAPHICS ARRAY', usage: 52, temperature: 58, vramUsed: 6.4, vramTotal: 16 },
  ram: { used: 13.8, total: 32, usage: 43.1 },
  network: { adapter: 'AWAITING TELEMETRY', download: 0, upload: 0 },
  disk: { name: 'SYSTEM DISK', read: 0, write: 0, free: 0, total: 0, usage: 0 },
  processes: [
    { id: 4720, name: 'DinoCore', cpu: 12.4, memory: 1228, status: 'RUNNING' },
    { id: 3184, name: 'System', cpu: 3.1, memory: 330, status: 'RUNNING' },
    { id: 9048, name: 'WebView2', cpu: 6.7, memory: 512, status: 'RUNNING' },
    { id: 7612, name: 'telemetry', cpu: .9, memory: 76, status: 'RUNNING' }
  ]
};

let target = structuredClone(fallback);
let display = structuredClone(fallback);
let settings = { targetFps: 60, animationQuality: 'High', modeIntervalSeconds: 120, theme: 'Blue', backgroundStyle: 'Pure Black', showTerminal: true, showHexStream: true, enableEvents: true, showDinoCore: true };
const listeners = new Set();
const settingsListeners = new Set();

const numericKeys = [
  ['cpu','usage'], ['cpu','temperature'], ['gpu','usage'], ['gpu','temperature'], ['gpu','vramUsed'], ['gpu','vramTotal'],
  ['ram','used'], ['ram','total'], ['ram','usage'], ['network','download'], ['network','upload'],
  ['disk','read'], ['disk','write'], ['disk','free'], ['disk','total'], ['disk','usage']
];

function onMessage(message) {
  if (!message || !message.type) return;
  if (message.type === 'telemetry' && message.payload) {
    target = { ...fallback, ...message.payload };
    for (const key of ['cpu','gpu','ram','network','disk']) target[key] = { ...fallback[key], ...(message.payload[key] || {}) };
  }
  if (message.type === 'settings' && message.payload) {
    settings = { ...settings, ...message.payload };
    settingsListeners.forEach(fn => fn(settings));
  }
}

if (window.chrome?.webview) window.chrome.webview.addEventListener('message', event => onMessage(event.data));

let last = performance.now();
function interpolate(now) {
  const delta = Math.min(2, (now - last) / 1000); last = now;
  const factor = 1 - Math.pow(.09, delta);
  display.timestamp = target.timestamp; display.computerName = target.computerName; display.windowsVersion = target.windowsVersion; display.uptime = target.uptime;
  display.processes = target.processes || [];
  display.cpu.name = target.cpu.name; display.cpu.cores = target.cpu.cores || [];
  display.gpu.name = target.gpu.name; display.network.adapter = target.network.adapter; display.disk.name = target.disk.name;
  for (const [group,key] of numericKeys) {
    const value = target[group]?.[key];
    if (value == null || !Number.isFinite(Number(value))) display[group][key] = null;
    else {
      const current = Number.isFinite(Number(display[group][key])) ? Number(display[group][key]) : Number(value);
      display[group][key] = current + (Number(value) - current) * factor;
    }
  }
  listeners.forEach(fn => fn(display, delta));
  requestAnimationFrame(interpolate);
}
requestAnimationFrame(interpolate);

export function subscribe(listener) { listeners.add(listener); return () => listeners.delete(listener); }
export function subscribeSettings(listener) { settingsListeners.add(listener); listener(settings); return () => settingsListeners.delete(listener); }
export function getTelemetry() { return display; }
export function getSettings() { return settings; }
export function format(value, digits = 1, suffix = '') { return value == null || !Number.isFinite(value) ? 'N/A' : `${value.toFixed(digits)}${suffix}`; }
