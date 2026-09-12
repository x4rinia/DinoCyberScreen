const charts = new Map();

class Sparkline {
  constructor(canvas, color) {
    this.canvas = canvas; this.ctx = canvas.getContext('2d'); this.color = color; this.values = Array(60).fill(0); this.peak = 0; this.average = 0;
    this.lastSample = 0;
  }
  push(value, now) {
    if (value == null || now - this.lastSample < 700) return;
    this.lastSample = now; this.values.push(Math.max(0, Math.min(100, value))); this.values.shift();
    this.peak = Math.max(...this.values); this.average = this.values.reduce((a,b)=>a+b,0) / this.values.length;
  }
  draw() {
    const dpr = Math.min(devicePixelRatio || 1, 2); const box = this.canvas.getBoundingClientRect();
    const w = Math.max(2, Math.floor(box.width*dpr)), h = Math.max(2, Math.floor(box.height*dpr));
    if (this.canvas.width !== w || this.canvas.height !== h) { this.canvas.width=w;this.canvas.height=h; }
    const c=this.ctx;c.clearRect(0,0,w,h); const app=document.querySelector('#app'),style=getComputedStyle(app),color=style.getPropertyValue(this.color).trim();
    const isRainbow=app.dataset.theme==='rainbow',rainbow=Array.from({length:7},(_,i)=>style.getPropertyValue(`--rainbow-${i+1}`).trim());
    const stroke=isRainbow?c.createLinearGradient(0,0,w,0):color;
    if(isRainbow)rainbow.forEach((value,index)=>stroke.addColorStop(index/(rainbow.length-1),value));
    const gradient=c.createLinearGradient(0,0,0,h);gradient.addColorStop(0,isRainbow?'rgba(41,191,255,.16)':color+'24');gradient.addColorStop(1,'rgba(0,0,0,0)');
    c.beginPath();this.values.forEach((v,i)=>{const x=i/(this.values.length-1)*w;const y=h-(v/100*h*.88+h*.06);i?c.lineTo(x,y):c.moveTo(x,y)});c.lineTo(w,h);c.lineTo(0,h);c.closePath();c.fillStyle=gradient;c.fill();
    c.beginPath();this.values.forEach((v,i)=>{const x=i/(this.values.length-1)*w;const y=h-(v/100*h*.88+h*.06);i?c.lineTo(x,y):c.moveTo(x,y)});c.strokeStyle=stroke;c.lineWidth=dpr;c.shadowColor=isRainbow?'#29bfff':color;c.shadowBlur=1.5*dpr;c.stroke();c.shadowBlur=0;
  }
}

export function initializeCharts() {
  charts.set('cpu',new Sparkline(document.querySelector('#cpuChart'),'--primary'));
  charts.set('gpu',new Sparkline(document.querySelector('#gpuChart'),'--tertiary'));
  charts.set('ram',new Sparkline(document.querySelector('#ramChart'),'--primary'));
}
export function updateCharts(data, now) { charts.get('cpu')?.push(data.cpu.usage,now);charts.get('gpu')?.push(data.gpu.usage,now);charts.get('ram')?.push(data.ram.usage,now);charts.forEach(c=>c.draw()); }
export function chartStats(name) { const c=charts.get(name);return c?{peak:c.peak,average:c.average}:{peak:0,average:0}; }
