import { subscribe, subscribeSettings, getSettings, format } from './telemetry.js';
import { initializeCharts, updateCharts, chartStats } from './charts.js';
import { initializeNetwork, drawNetwork, setNetworkQuality } from './network.js';
import { initializeCore, drawCore, configureCore, setCoreIntensity } from './core3d.js';
import { initializeStreams, updateStreams } from './terminal.js';
import { initializeEvents, updateEvents } from './events.js';

const $ = selector => document.querySelector(selector);
const app=$('#app'), modes=['system','network','dino','data','overview'];
let currentMode=4,nextModeAt=performance.now()+120000,lastUi=0,lastProcesses=0;

const urlParams = new URLSearchParams(window.location.search);
if (urlParams.get('secondary') === '1') document.body.classList.add('secondary-monitor');
if (urlParams.get('singleMonitor') === '1') document.body.classList.add('single-monitor');
if (urlParams.get('primary') === '1') document.body.classList.add('primary-monitor');
if (urlParams.get('primaryMulti') === '1') document.body.classList.add('primary-multi');

initializeCharts();initializeNetwork();initializeCore();initializeStreams();initializeEvents();

const coreBars=$('#coreBars');for(let i=0;i<16;i++)coreBars.append(document.createElement('i'));

window.addEventListener('mousemove', () => document.body.classList.add('show-cursor'));
$('#modeNav').addEventListener('click',event=>{const button=event.target.closest('button[data-mode]');if(button)setMode(button.dataset.mode,true)});
$('#settingsButton').addEventListener('click',()=>window.chrome?.webview?.postMessage({command:'openSettings'}));

function setMode(mode,manual=false){
  const index=modes.indexOf(mode);if(index<0||app.dataset.mode===mode)return;currentMode=index;
  app.dataset.mode=mode;document.querySelectorAll('#modeNav button').forEach(button=>button.classList.toggle('active',button.dataset.mode===mode));
}

let nextEventAt = performance.now() + 10000 + Math.random() * 20000;
let nextScanlineAt = performance.now() + 20000 + Math.random() * 40000;

subscribeSettings(settings=>{
  const themeStr = String(settings.theme || 'blue').toLowerCase();
  const theme = ['green', 'red', 'white', 'pink', 'rosa'].includes(themeStr) ? (themeStr === 'rosa' ? 'pink' : themeStr) : 'blue';
  document.body.dataset.theme = theme;
  app.dataset.theme = theme;
  
  const background={"dark blue tint":"dark-blue","dark green tint":"dark-green"}[String(settings.backgroundStyle||'').toLowerCase()]||'pure-black';app.dataset.background=background;
  
  const specimen = String(settings.dinoSpecimen || 'ankylo').toLowerCase();
  const holoImg = $('.dino-hologram');
  const coreTitle = $('#coreTitle');
  if (specimen === 'triceratops') {
    if (holoImg) holoImg.src = 'assets/triceratops-hologram.png';
    if (coreTitle) coreTitle.textContent = 'TRICERA CORE';
  } else if (specimen === 'raptor') {
    if (holoImg) holoImg.src = 'assets/raptor-hologram.png';
    if (coreTitle) coreTitle.textContent = 'VELO CORE';
  } else if (specimen === 'stego') {
    if (holoImg) holoImg.src = 'assets/stego-hologram.png';
    if (coreTitle) coreTitle.textContent = 'STEGO CORE';
  } else if (specimen === 'ptero' || specimen === 'pterodactylus' || specimen === 'pterodax') {
    if (holoImg) holoImg.src = 'assets/ptero-hologram.png';
    if (coreTitle) coreTitle.textContent = 'PTERO CORE';
  } else {
    if (holoImg) holoImg.src = 'assets/ankylo-hologram.png';
    if (coreTitle) coreTitle.textContent = 'ANKYLO CORE';
  }

  const brandMark = $('#brandMark');
  const customName = (settings.customName || '').trim() || 'DINO';
  if (brandMark) brandMark.textContent = customName;

  configureCore(settings);setNetworkQuality(settings.animationQuality);$('#qualityState').textContent=`${settings.targetFps||60} FPS / ${(settings.animationQuality||'High').toUpperCase()}`;
  $('#terminalPanel').classList.toggle('module-disabled',settings.showTerminal===false);$('#hexPanel').classList.toggle('module-disabled',settings.showHexStream===false);$('.core-panel').classList.toggle('module-disabled',settings.showDinoCore===false);
  setMode('overview');
});

function triggerDinoScanline() {
  if (document.body.classList.contains('primary-multi')) return;
  const scanline = $('#dinoScanline');
  if (!scanline) return;
  scanline.classList.remove('run');
  void scanline.offsetWidth;
  scanline.classList.add('run');
}

subscribe((data,delta)=>{
  const now=performance.now(),settings=getSettings();
  if(settings.enableEvents !== false && now > nextEventAt) { triggerRandomAlarm(); nextEventAt = now + 120000 + Math.random() * 360000; }
  if(now > nextScanlineAt) { triggerDinoScanline(); nextScanlineAt = now + 60000 + Math.random() * 120000; }
  drawNetwork(now,data);setCoreIntensity(Math.max(data.cpu.usage||0,data.gpu.usage||0));drawCore(now);updateCharts(data,now);updateStreams(data,now,settings);updateEvents(data,now,settings);
  if(now-lastUi>100){lastUi=now;renderTelemetry(data)}
  if(now-lastProcesses>900){lastProcesses=now;renderProcesses(data.processes||[])}
});

const EVENTS = [
  { class: '', html: `<div class="event-card"><small>SYSTEM EVENT // PRIORITY ANALYSIS</small><h2>ANOMALY DETECTED</h2><p>NODE 03</p><div class="event-progress"><i></i></div><strong>ANALYZING...</strong></div>` },
  { class: 'primary', html: `<div class="event-card"><small>GHOST SIGNAL DETECTED</small><h2>UNREGISTERED DATA STREAM</h2><p>ORIGIN: UNKNOWN<br>ENCRYPTION: 4096-BIT<br>PACKET LOSS: 17%</p><div class="event-progress"><i></i></div><strong>DECODING...</strong><p style="margin-top:10px;font-size:0.8em;color:var(--text-medium)">SIGNATURE MATCH: NONE<br>CLASSIFICATION: NON-HOSTILE<br>ARCHIVING SIGNAL...</p></div>` },
  { class: 'secondary', html: `<div class="event-card"><small>BLACK ICE PROTOCOL</small><h2>UNAUTHORIZED PATTERN DETECTED</h2><p>ISOLATION MODE ACTIVE<br>NODE MATRIX LOCKED</p><div class="event-progress"><i></i></div><strong>ANALYZING...</strong><p style="margin-top:10px;font-size:0.8em;color:var(--text-medium)">STATUS: NEUTRALIZED<br>SYSTEM SECURE</p></div>` }
];

function triggerRandomAlarm() {
  if (document.body.classList.contains('secondary-monitor')) return;
  const overlay = $('#eventOverlay');
  if(!overlay) return;
  const evt = EVENTS[Math.floor(Math.random() * EVENTS.length)];
  overlay.innerHTML = evt.html;
  overlay.className = 'event-overlay visible ' + evt.class;
  const progress = overlay.querySelector('.event-progress i');
  if(progress) setTimeout(() => progress.style.width = '100%', 50);
  setTimeout(() => {
    overlay.classList.remove('visible');
    setTimeout(() => overlay.innerHTML = '', 400);
  }, 9000 + Math.random() * 4000);
}

function renderTelemetry(data){
  const date=data.timestamp?new Date(data.timestamp):new Date();
  const timeStr = date.toLocaleTimeString('de-DE',{hour12:false});
  $('#clock').textContent = timeStr;
  $('#coreClock').textContent = timeStr;
  $('#date').textContent=date.toLocaleDateString('de-DE',{weekday:'short',day:'2-digit',month:'short',year:'numeric'}).toUpperCase();$('#uptime').textContent=data.uptime||'00:00:00';$('#hostName').textContent=(data.computerName||'LOCAL NODE').toUpperCase();
  const cpu=data.cpu.usage,gpu=data.gpu.usage,ram=data.ram.usage;setValue('#cpuValue',format(cpu,0,'%'));setWidth('#cpuBar',cpu);$('#cpuName').textContent=data.cpu.name||'PROCESSOR ARRAY';setValue('#gpuValue',format(gpu,0,'%'));setWidth('#gpuBar',gpu);$('#gpuName').textContent=data.gpu.name||'GPU NOT DETECTED';setValue('#ramValue',format(ram,0,'%'));setWidth('#ramBar',ram);$('#ramUsed').textContent=format(data.ram.used,1,' GB');$('#ramTotal').textContent=format(data.ram.total,1,' GB');
  const cs=chartStats('cpu'),gs=chartStats('gpu');$('#cpuPeak').textContent=format(cs.peak,0,'%');$('#cpuAvg').textContent=format(cs.average,0,'%');$('#gpuPeak').textContent=format(gs.peak,0,'%');$('#gpuAvg').textContent=format(gs.average,0,'%');
  const temp=Math.max(data.cpu.temperature||0,data.gpu.temperature||0)||null;$('#temperature').textContent=format(temp,0,'°C');$('#coreTemp').textContent=format(temp,0,'°C');$('#vramValue').textContent=data.gpu.vramUsed==null||data.gpu.vramTotal==null?'N/A':`${data.gpu.vramUsed.toFixed(1)} / ${data.gpu.vramTotal.toFixed(1)} GB`;
  $('#download').textContent=format(data.network.download,1);$('#upload').textContent=format(data.network.upload,1);$('#netRx').textContent=format(data.network.download,2,' Mbps');$('#netTx').textContent=format(data.network.upload,2,' Mbps');$('#adapter').textContent=data.network.adapter||'NO ACTIVE ADAPTER';
  $('#diskUsage').textContent=format(data.disk.usage,0);$('#diskName').textContent=data.disk.name||'SYSTEM DISK';$('#diskRead').textContent=format(data.disk.read,1,' MB/s');$('#diskWrite').textContent=format(data.disk.write,1,' MB/s');
  const cores=data.cpu.cores||[];[...coreBars.children].forEach((bar,i)=>bar.classList.toggle('on',(cores[i%Math.max(1,cores.length)]||0)>22));
  document.documentElement.style.setProperty('--system-load',Math.max(cpu||0,gpu||0)/100);
}

function renderProcesses(processes){
  $('#processCount').textContent=processes.length;const body=$('#processRows');body.replaceChildren(...processes.slice(0,8).map(process=>{const row=document.createElement('tr');row.innerHTML=`<td>${process.id}</td><td>${escapeHtml(process.name)}</td><td>${format(process.cpu,1,'%')}</td><td>${format(process.memory,0,' MB')}</td><td>${process.status||'RUNNING'}</td>`;return row}));
}
function setValue(selector,value){$(selector).textContent=value}
function setWidth(selector,value){$(selector).style.width=Math.max(0,Math.min(100,value||0))+'%'}
function escapeHtml(value){return String(value??'').replace(/[&<>"]/g,char=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[char]))}

window.addEventListener('keydown',event=>{
  if(event.key==='Enter' || event.key==='Escape') window.chrome?.webview?.postMessage({command:'exit'});
  if(event.key==='ArrowRight')setMode(modes[(modes.indexOf(app.dataset.mode)+1)%modes.length],true);
  if(event.key==='ArrowLeft')setMode(modes[(modes.indexOf(app.dataset.mode)+modes.length-1)%modes.length],true);
});

// Matrix Binary Rain for Secondary Monitor
function initBinaryRain() {
    const canvas = document.getElementById('binaryCanvas');
    if (!canvas || !document.body.classList.contains('secondary-monitor')) return;
    const ctx = canvas.getContext('2d');
    
    let width, height, columns;
    const drops = [];
    const fontSize = 16;
    
    function resize() {
        width = canvas.width = canvas.offsetWidth;
        height = canvas.height = canvas.offsetHeight;
        columns = Math.floor(width / fontSize);
        while(drops.length < columns) drops.push(Math.random() * -100);
    }
    
    window.addEventListener('resize', resize);
    resize();
    
    function draw() {
        ctx.fillStyle = 'rgba(0, 0, 0, 0.08)';
        ctx.fillRect(0, 0, width, height);
        
        const isGreen = document.body.dataset.theme === 'green' || document.querySelector('.app').dataset.theme === 'green';
        ctx.fillStyle = isGreen ? '#5cff9c' : '#26d9ff';
        ctx.font = fontSize + 'px monospace';
        
        for (let i = 0; i < drops.length; i++) {
            const text = Math.random() > 0.5 ? '1' : '0';
            ctx.fillText(text, i * fontSize, drops[i] * fontSize);
            
            if (drops[i] * fontSize > height && Math.random() > 0.975) {
                drops[i] = 0;
            }
            drops[i]++;
        }
        setTimeout(() => requestAnimationFrame(draw), 45);
    }
    
    draw();
}
initBinaryRain();
