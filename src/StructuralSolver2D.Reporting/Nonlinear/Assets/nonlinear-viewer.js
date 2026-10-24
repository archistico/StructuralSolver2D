(() => {
'use strict';
const $=id=>document.getElementById(id);
const data=JSON.parse($('dataset').textContent);
const frames=data.steps||[], nodes=data.nodes||[], members=data.members||[];
const svgNS='http://www.w3.org/2000/svg';
const step=$('step'), scale=$('scale'), selectedNode=$('node'), selectedDof=$('dof'), selectedMember=$('member');
let playback=null;
const svgElement=(name,attrs,text)=>{const el=document.createElementNS(svgNS,name);for(const [k,v] of Object.entries(attrs||{}))el.setAttribute(k,String(v));if(text!==undefined)el.textContent=String(text);return el;};
const number=v=>Number.isFinite(v)?Number(v).toExponential(5):'—';
const point=(a,b)=>`${a},${b}`;
const pushOpt=(select,value,label)=>{const option=document.createElement('option');option.value=value;option.textContent=label;select.appendChild(option);};
$('title').textContent=data.title;
$('status').textContent=data.completed?'Analisi completata':`RISULTATO PARZIALE — ${data.failure||'arresto della convergenza'}. Solo passi accettati.`;
if(!data.completed)$('status').classList.add('failed');
$('footer-note').textContent=data.mode==='time'?'Ascissa delle serie: tempo fisico [s]. Non sono interpolati passi di Newton rifiutati; le deformate amplificate sono illustrative.':'Ascissa delle serie: fattore di carico adimensionale. Carico–spostamento usa U sull’asse orizzontale e il fattore di carico sull’asse verticale.';
for(const n of nodes)pushOpt(selectedNode,n.id,n.id);
for(const m of members)pushOpt(selectedMember,m.id,m.id);
if(nodes.length)selectedNode.value=nodes[nodes.length-1].id;
step.max=Math.max(0,frames.length-1);
const positions=new Map(nodes.map((n,i)=>[n.id,i]));
const memberIndex=new Map(members.map((m,i)=>[m.id,i]));
const bounds=()=>{const xs=nodes.map(n=>n.x),ys=nodes.map(n=>n.y);const minX=Math.min(...xs),maxX=Math.max(...xs),minY=Math.min(...ys),maxY=Math.max(...ys);return {minX,maxX,minY,maxY,range:Math.max(maxX-minX,maxY-minY,1e-9)};};
const b=nodes.length?bounds():{minX:0,maxX:1,minY:0,maxY:1,range:1};
const midX=(b.minX+b.maxX)/2,midY=(b.minY+b.maxY)/2;
const xy=(x,y)=>[450+(x-midX)*340/b.range,200-(y-midY)*340/b.range];
let maxU=0;for(const f of frames)for(const v of f.displacements)maxU=Math.max(maxU,Math.abs(v));
// A fixed maximal visual deflection is used across the entire playback; not a physical load factor.
const graphicGain=()=>maxU>0?b.range*0.35/maxU*(Number(scale.value)/100):0;
function renderStructure(frame){
 const scene=$('structure');scene.replaceChildren();
 const gain=graphicGain();
 const original=nodes.map(n=>xy(n.x,n.y));
 const moved=nodes.map((n,i)=>xy(n.x+gain*frame.displacements[2*i],n.y+gain*frame.displacements[2*i+1]));
 for(let i=0;i<members.length;i++){
  const m=members[i],a=positions.get(m.startNodeId),z=positions.get(m.endNodeId);
  if(a===undefined||z===undefined)continue;
  const line0=svgElement('line',{x1:original[a][0],y1:original[a][1],x2:original[z][0],y2:original[z][1],stroke:'#71849d','stroke-width':2,'stroke-dasharray':'5 4'});
  scene.appendChild(line0);
  const plastic=frame.accumulatedPlasticStrain[i]>1e-13;
  const isSelected=m.id===selectedMember.value;
  const main=svgElement('line',{x1:moved[a][0],y1:moved[a][1],x2:moved[z][0],y2:moved[z][1],stroke:isSelected?'#ff73a5':plastic?'#ffbd69':'#57d9ad','stroke-width':isSelected?6:4,'stroke-linecap':'round','data-member-id':m.id});
  const tooltip=svgElement('title',{},`${m.id}: N = ${number(frame.axialForces[i])} kN; εp = ${number(frame.plasticStrain[i])}; p = ${number(frame.accumulatedPlasticStrain[i])}`);
  main.appendChild(tooltip);
  main.addEventListener('click',()=>{selectedMember.value=m.id;render();});
  scene.appendChild(main);
 }
 for(let i=0;i<nodes.length;i++){
  scene.appendChild(svgElement('circle',{cx:moved[i][0],cy:moved[i][1],r:4,fill:'#dceaff'}));
  scene.appendChild(svgElement('text',{x:moved[i][0]+8,y:moved[i][1]-9,fill:'#e6f0ff','font-size':12},nodes[i].id));
 }
 const j=memberIndex.get(selectedMember.value);
 $('member-value').textContent=j===undefined?'Nessuna asta':`${members[j].id} · N = ${number(frame.axialForces[j])} kN · εp = ${number(frame.plasticStrain[j])} · p = ${number(frame.accumulatedPlasticStrain[j])} · ${frame.accumulatedPlasticStrain[j]>1e-13?'plasticizzata':'senza plasticità accumulata'}`;
}
function plot(id,values,marker){
 const chart=$(id);chart.replaceChildren();
 if(!values.length)return;
 const xs=values.map(v=>v[0]),ys=values.map(v=>v[1]);
 let x0=Math.min(...xs),x1=Math.max(...xs),y0=Math.min(...ys),y1=Math.max(...ys);
 if(x1-x0<1e-14){const bump=Math.max(1e-8,Math.abs(x0)*0.1);x0-=bump;x1+=bump;}
 if(y1-y0<1e-14){const bump=Math.max(1e-8,Math.abs(y0)*0.1);y0-=bump;y1+=bump;}
 const mapx=x=>56+515*(x-x0)/(x1-x0),mapy=y=>220-178*(y-y0)/(y1-y0);
 chart.appendChild(svgElement('line',{x1:56,y1:220,x2:572,y2:220,stroke:'#8798ae','stroke-width':1}));
 chart.appendChild(svgElement('line',{x1:56,y1:40,x2:56,y2:220,stroke:'#8798ae','stroke-width':1}));
 chart.appendChild(svgElement('polyline',{points:values.map(v=>point(mapx(v[0]),mapy(v[1]))).join(' '),stroke:'#66d5bd','stroke-width':2,fill:'none','stroke-linejoin':'round'}));
 const row=values[marker];if(row)chart.appendChild(svgElement('circle',{cx:mapx(row[0]),cy:mapy(row[1]),r:5,fill:'#ffcc78'}));
 chart.appendChild(svgElement('text',{x:57,y:245,fill:'#baccdf','font-size':12},number(x0)));
 chart.appendChild(svgElement('text',{x:571,y:245,fill:'#baccdf','font-size':12,'text-anchor':'end'},number(x1)));
 chart.appendChild(svgElement('text',{x:10,y:30,fill:'#baccdf','font-size':12},number(y1)));
 chart.appendChild(svgElement('text',{x:10,y:217,fill:'#baccdf','font-size':12},number(y0)));
}
function render(){
 if(!frames.length){$('step-label').textContent='Nessun passo accettato';$('node-value').textContent='Nessun risultato visualizzabile.';return;}
 let i=Math.max(0,Math.min(frames.length-1,Number(step.value)||0));const frame=frames[i];
 const ni=positions.get(selectedNode.value)??0,mi=memberIndex.get(selectedMember.value)??0,component=Number(selectedDof.value);
 $('step-label').textContent=`Passo accettato ${frame.index} / ${frames[frames.length-1].index} · ${data.mode==='time'?'t = '+number(frame.position)+' s':'λ = '+number(frame.loadFactor)} · Newton: ${frame.iterations}`;
 $('node-value').textContent=`${nodes[ni]?.id||'—'}: U${component===0?'x':'y'} = ${number(frame.displacements[2*ni+component])} m; R${component===0?'x':'y'} = ${number(frame.reactions[2*ni+component])} kN`;
 renderStructure(frame);
 const values=frames.map(f=>[f.displacements[2*ni+component],f.loadFactor]);
 plot('load-chart',values,i);
 plot('force-chart',frames.map(f=>[f.position,f.axialForces[mi]]),i);
 plot('plastic-chart',frames.map(f=>[f.position,f.accumulatedPlasticStrain[mi]]),i);
 plot('energy-chart',frames.map(f=>[f.position,f.plasticDissipation]),i);
}
function pause(){if(playback!==null){clearInterval(playback);playback=null;}$('play').textContent='Riproduci';}
$('play').disabled=frames.length<2;step.disabled=frames.length<2;
$('play').addEventListener('click',()=>{
 if(playback!==null){pause();return;}
 if(frames.length<2)return;
 playback=setInterval(()=>{step.value=(Number(step.value)+1)%frames.length;render();},180);
 $('play').textContent='Pausa';
});
for(const el of [step,scale,selectedNode,selectedDof,selectedMember])el.addEventListener('input',render);
window.addEventListener('pagehide',pause);
render();
})();
