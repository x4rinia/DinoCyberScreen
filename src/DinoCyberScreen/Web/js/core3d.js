let canvas,gl,program,pointBuffer,lineBuffer,pointCount=0,lineCount=0,start=performance.now(),quality='High',fps=60,lastFrame=0,intensity=.4,lineColor=[.15,.85,1],pointColor=[.25,1,.75],activeTheme='blue',rainbowColors=[];
const vertex=`attribute vec3 p;uniform float t;uniform float size;varying float depth;void main(){float cy=cos(t),sy=sin(t),cx=cos(t*.37),sx=sin(t*.37);vec3 q=vec3(cy*p.x+sy*p.z,p.y,-sy*p.x+cy*p.z);q=vec3(q.x,cx*q.y-sx*q.z,sx*q.y+cx*q.z);float z=q.z+3.7;gl_Position=vec4(q.x/z*2.35,q.y/z*2.35,q.z/4.0,1.0);gl_PointSize=size*(1.0+q.z*.16);depth=(q.z+1.0)/2.0;}`;
const fragment=`precision mediump float;uniform vec4 color;uniform float points;varying float depth;void main(){if(points>0.5){vec2 d=gl_PointCoord-.5;if(length(d)>.5)discard;}gl_FragColor=vec4(color.rgb,color.a*(.45+depth*.55));}`;

export function initializeCore(){canvas=document.querySelector('#coreCanvas');gl=canvas.getContext('webgl',{alpha:true,antialias:true,preserveDrawingBuffer:false});if(!gl){canvas.dataset.fallback='true';return}program=createProgram(vertex,fragment);pointBuffer=gl.createBuffer();lineBuffer=gl.createBuffer();rebuild();gl.enable(gl.BLEND);gl.blendFunc(gl.SRC_ALPHA,gl.ONE);}
function createProgram(vs,fs){const compile=(type,source)=>{const s=gl.createShader(type);gl.shaderSource(s,source);gl.compileShader(s);return s};const p=gl.createProgram();gl.attachShader(p,compile(gl.VERTEX_SHADER,vs));gl.attachShader(p,compile(gl.FRAGMENT_SHADER,fs));gl.linkProgram(p);return p}
function rebuild(){if(!gl)return;const count=quality==='Low'?110:quality==='Medium'?180:280,nodes=[];for(let i=0;i<count;i++){const y=1-(i/(count-1))*2,r=Math.sqrt(1-y*y),theta=Math.PI*(3-Math.sqrt(5))*i;nodes.push([Math.cos(theta)*r,y,Math.sin(theta)*r])}const lines=[];const step=quality==='High'?7:11;for(let i=0;i<count;i++){for(const off of [1,step]){const a=nodes[i],b=nodes[(i+off)%count];lines.push(...a,...b)}}gl.bindBuffer(gl.ARRAY_BUFFER,pointBuffer);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(nodes.flat()),gl.STATIC_DRAW);pointCount=count;gl.bindBuffer(gl.ARRAY_BUFFER,lineBuffer);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(lines),gl.STATIC_DRAW);lineCount=lines.length/3}
const THEME_COLORS = {
  blue:    { primary: [.15, .85, 1.0],  secondary: [.00, .67, 1.0]  },  // cyan + sky-blue
  green:   { primary: [.30, 1.0, .53],  secondary: [.77, 1.0, .20]  },  // green + lime
  red:     { primary: [1.0, .23, .36],  secondary: [1.0, .52, .20]  },  // red + orange
  white:   { primary: [1.0, 1.0, 1.0],  secondary: [.78, .85, .88]  },  // white + silver-grey
  pink:    { primary: [1.0, .40, .80],  secondary: [.65, .30, 1.0]  },  // pink + purple
  rainbow: { primary: [.16, .75, 1.0],  secondary: [1.0, .31, .85]  },
};

function parseHex(value){const hex=String(value||'').trim().replace('#','');if(!/^[0-9a-f]{6}$/i.test(hex))return null;return [0,2,4].map(index=>parseInt(hex.slice(index,index+2),16)/255)}
function rainbowColor(now,offset=0){if(!rainbowColors.length)return offset?pointColor:lineColor;const position=((now-start)/2600+offset)%rainbowColors.length,index=Math.floor(position),mix=position-index,a=rainbowColors[index],b=rainbowColors[(index+1)%rainbowColors.length];return a.map((value,i)=>value+(b[i]-value)*mix)}

export function configureCore(settings){
  const isEco = settings.energySavingMode === true;
  quality=isEco ? 'Low' : (settings.animationQuality||'High');
  fps=isEco ? 24 : (settings.targetFps||60);
  const theme = String(settings.theme || 'blue').toLowerCase();
  activeTheme=theme in THEME_COLORS?theme:'blue';
  const c = THEME_COLORS[activeTheme];
  lineColor = c.primary;
  pointColor = c.secondary;
  const style=getComputedStyle(document.querySelector('#app'));rainbowColors=activeTheme==='rainbow'?Array.from({length:7},(_,i)=>parseHex(style.getPropertyValue(`--rainbow-${i+1}`))).filter(Boolean):[];
  rebuild();
}
export function setCoreIntensity(value){intensity=Math.max(.15,Math.min(1,(value||0)/100))}
function resize(){const box=canvas.getBoundingClientRect(),scale=quality==='Low'?1:Math.min(devicePixelRatio||1,1.65),w=Math.max(2,Math.floor(box.width*scale)),h=Math.max(2,Math.floor(box.height*scale));if(canvas.width!==w||canvas.height!==h){canvas.width=w;canvas.height=h;gl.viewport(0,0,w,h)}}
export function drawCore(now){if(!gl||now-lastFrame<1000/fps)return;lastFrame=now;resize();gl.clearColor(0,0,0,0);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);gl.useProgram(program);const pos=gl.getAttribLocation(program,'p'),time=(now-start)*(.000045+intensity*.000035),activeLine=activeTheme==='rainbow'?rainbowColor(now):lineColor,activePoint=activeTheme==='rainbow'?rainbowColor(now,2.4):pointColor;gl.uniform1f(gl.getUniformLocation(program,'t'),time);gl.enableVertexAttribArray(pos);
  gl.bindBuffer(gl.ARRAY_BUFFER,lineBuffer);gl.vertexAttribPointer(pos,3,gl.FLOAT,false,0,0);gl.uniform4f(gl.getUniformLocation(program,'color'),activeLine[0],activeLine[1],activeLine[2],.11+intensity*.08);gl.uniform1f(gl.getUniformLocation(program,'points'),0);gl.uniform1f(gl.getUniformLocation(program,'size'),1);gl.drawArrays(gl.LINES,0,lineCount);
  gl.bindBuffer(gl.ARRAY_BUFFER,pointBuffer);gl.vertexAttribPointer(pos,3,gl.FLOAT,false,0,0);gl.uniform4f(gl.getUniformLocation(program,'color'),activePoint[0],activePoint[1],activePoint[2],.58);gl.uniform1f(gl.getUniformLocation(program,'points'),1);gl.uniform1f(gl.getUniformLocation(program,'size'),quality==='Low'?2.1:2.8+intensity*1.8);gl.drawArrays(gl.POINTS,0,pointCount);
}
