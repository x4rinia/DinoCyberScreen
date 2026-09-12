const messages = [
  ['INFO','telemetry stream initialized'],['SYS','node matrix synchronized'],['CORE','entropy pool refreshed'],
  ['INFO','local gateway verified'],['SYS','memory block integrity OK'],['CORE','DINO inference mesh nominal'],
  ['INFO','packet route optimized'],['SYS','security heuristics synchronized'],['CORE','compute channel standing by'],
  ['INFO','display topology verified'],['SYS','sensor envelope stable'],['CORE','neural lattice heartbeat received']
];
let terminal, hex, lastLine=0, lastHex=0, address=0x7ffa91c320;

export function initializeStreams() {
  terminal=document.querySelector('#terminal');hex=document.querySelector('#hexStream');
  for(let i=0;i<9;i++) appendLine(messages[i%messages.length][0],messages[i%messages.length][1]);
  renderHex();
}
function timestamp(){return new Date().toLocaleTimeString('de-DE',{hour12:false})}
function appendLine(level,message){
  if(!terminal)return;const line=document.createElement('div');line.className=`terminal-line ${level.toLowerCase()}`;
  line.innerHTML=`<span class="time">${timestamp()}</span><span class="level">[${level}]</span><span class="message">${message}</span>`;terminal.append(line);while(terminal.children.length>15)terminal.firstElementChild.remove();
}
function renderHex(){
  if(!hex)return;const rows=[];for(let r=0;r<8;r++){const bytes=Array.from({length:12},()=>Math.floor(Math.random()*256));const ascii=bytes.map(b=>b>31&&b<127?String.fromCharCode(b):'.').join('');rows.push(`${(address+r*16).toString(16).toUpperCase().padStart(12,'0')}  ${bytes.map(b=>b.toString(16).toUpperCase().padStart(2,'0')).join(' ')}  ${ascii}`)}address+=16;hex.textContent=rows.join('\n');document.querySelector('#memoryAddress').textContent='0x'+address.toString(16).toUpperCase();
}
export function updateStreams(data, now, settings){
  if(settings.showTerminal!==false && now-lastLine>1200+Math.random()*1500){lastLine=now;const source=messages[Math.floor(Math.random()*messages.length)];let message=source[1];const roll=Math.random();if(roll<.2)message=`CPU LOAD ${data.cpu.usage?.toFixed(1)??'N/A'}%`;else if(roll<.35)message=`GPU LOAD ${data.gpu.usage?.toFixed(1)??'N/A'}%`;else if(roll<.48)message=`VRAM ${data.gpu.vramUsed?.toFixed(1)??'N/A'} / ${data.gpu.vramTotal?.toFixed(1)??'N/A'} GB`;appendLine(source[0],message)}
  if(settings.showHexStream!==false&&now-lastHex>780){lastHex=now;renderHex();const mem=55+Math.random()*38,packet=25+Math.random()*65;setBuffer('memoryBuffer',mem);setBuffer('packetBuffer',packet)}
}
function setBuffer(id,value){const bar=document.querySelector('#'+id),label=document.querySelector('#'+id+'Value');if(bar)bar.style.width=value+'%';if(label)label.textContent=Math.round(value)+'%'}
export function logTerminal(level,message){appendLine(level,message)}
