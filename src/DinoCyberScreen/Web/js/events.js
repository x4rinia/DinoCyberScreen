let log,lastRoutine=0;
const routine=[['INFO','SCAN','Threat scan complete — no threats'],['CORE','DINO','AI modules online (12/12)'],['INFO','NODE','Heartbeat received'],['SYS','SYSTEM','Configuration envelope verified'],['INFO','NETWORK','Route optimized'],['CORE','CORE','Node sync completed'],['INFO','UPDATE','Signatures current']];

export function initializeEvents(){log=document.querySelector('#eventLog');for(let i=0;i<8;i++){const r=routine[i%routine.length];addEvent(r[0],r[1],r[2])}}
function addEvent(level,source,message){if(!log)return;const row=document.createElement('div');row.className=`event-row ${level.toLowerCase()}`;row.innerHTML=`<span>${new Date().toLocaleTimeString('de-DE',{hour12:false})}</span><span class="level">${level}</span><span>${source} // ${message}</span>`;log.prepend(row);while(log.children.length>12)log.lastElementChild.remove()}
export function updateEvents(data,now,settings){
  if(now-lastRoutine>4700+Math.random()*2200){lastRoutine=now;const r=routine[Math.floor(Math.random()*routine.length)];addEvent(r[0],r[1],r[2])}
}
