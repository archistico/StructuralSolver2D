using System.Text.Json;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Reporting.Transient;

/// <summary>Standalone browser viewer, playback is illustrative in time and scale, not a certified rendering.</summary>
public sealed class InteractiveHtmlTimeHistoryViewerExporter
{
    public string Export(StructuralModel model, StructuralTimeHistoryAnalysisOutput output, string title)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(output);
        int stride = Math.Max(1, (int)Math.Ceiling(output.Steps.Count / 500.0));
        var data = new
        {
            nodes = model.Nodes.Select(n => new { id = n.Id, x = n.X, y = n.Y }).ToArray(),
            members = model.Members.Select(m => new { start = m.StartNodeId, end = m.EndNodeId, type = m.Type.ToString() }).ToArray(),
            frames = output.Steps.Where((s, i) => i % stride == 0 || i == output.Steps.Count - 1)
                .Select(s => new { time = s.TimeSeconds, nodes = s.Nodes.Select(n => new {
                    id = n.NodeId, ux = n.Ux, uy = n.Uy, rz = n.Rz, vx = n.Vx, vy = n.Vy, vrz = n.VrZ,
                    ax = n.Ax, ay = n.Ay, arz = n.ArZ }).ToArray() }).ToArray(),
            maxDisplacement = output.Envelopes.Select(e => Math.Max(e.MaxAbsUx, e.MaxAbsUy)).DefaultIfEmpty(0).Max(),
            title
        };
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        // System.Text.Json escapes unsafe HTML characters by default; do not disable its encoder.
        return "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>M59 time history</title>" +
        "<style>body{font:14px system-ui;background:#101a2b;color:#e7edf9;margin:0;padding:22px}main{max-width:1000px;margin:auto}.panel{background:#18273d;border:1px solid #34455e;border-radius:12px;padding:16px;margin:12px 0}button,select,input{accent-color:#79b7fc}button,select{background:#233a55;color:#fff;border:1px solid #627a96;border-radius:6px;padding:7px}svg{width:100%;background:#101b2c;border-radius:6px}label{margin-right:16px}small{color:#bacce4}</style></head><body><main><h1>M59 · Time-history response</h1><p id=\"title\"></p><p><small>Physical motion reconstructed from complete linear modes; graphic displacement scale is illustrative. Viewer is downsampled to at most ~500 frames; CSV contains every computed step.</small></p>" +
        "<section class=\"panel\"><button id=\"play\">Play</button> <label>Time <input type=\"range\" id=\"step\" min=\"0\" value=\"0\" step=\"1\"></label> <span id=\"clock\"></span> <label>Visual scale <input id=\"scale\" type=\"range\" min=\"0\" max=\"100\" value=\"35\"></label><svg id=\"structure\" viewBox=\"0 0 800 340\" aria-label=\"Structure time animation\"></svg></section>" +
        "<section class=\"panel\"><label>Node <select id=\"node\"></select></label><label>Component <select id=\"component\"><option value=\"uy\">Uy [m]</option><option value=\"ux\">Ux [m]</option><option value=\"rz\">Rz [rad]</option><option value=\"vy\">Vy [m/s]</option><option value=\"vx\">Vx [m/s]</option><option value=\"vrz\">VrZ [rad/s]</option><option value=\"ay\">Ay [m/s²]</option><option value=\"ax\">Ax [m/s²]</option><option value=\"arz\">ArZ [rad/s²]</option></select></label><svg id=\"history\" viewBox=\"0 0 800 220\" aria-label=\"Selected time history\"></svg><p id=\"value\"></p></section>" +
        "<script type=\"application/json\" id=\"dataset\">" + json + "</script><script>" + Script + "</script></main></body></html>";
    }

    private const string Script = """
(() => {
'use strict';
const data = JSON.parse(document.getElementById('dataset').textContent);
const frames = data.frames, base = data.nodes, edges = data.members;
const $ = id => document.getElementById(id);
const svgNS='http://www.w3.org/2000/svg';
const step=$('step'), scale=$('scale'), node=$('node'), component=$('component');
step.max=Math.max(0,frames.length-1);
$('title').textContent=data.title;
for(const n of base){const o=document.createElement('option');o.value=n.id;o.textContent=n.id;node.appendChild(o);}
if (base.length) node.value=base[base.length-1].id;
const xs=base.map(n=>n.x), ys=base.map(n=>n.y);
const minX=Math.min(...xs), maxX=Math.max(...xs), minY=Math.min(...ys),maxY=Math.max(...ys);
const dim=Math.max(maxX-minX,maxY-minY,1);
const mul=285/dim;
const locate=(x,y)=>[400+(x-(minX+maxX)/2)*mul,170-(y-(minY+maxY)/2)*mul];
function line(svg,x1,y1,x2,y2,color,width){const x=document.createElementNS(svgNS,'line');x.setAttribute('x1',x1);x.setAttribute('y1',y1);x.setAttribute('x2',x2);x.setAttribute('y2',y2);x.setAttribute('stroke',color);x.setAttribute('stroke-width',width);svg.appendChild(x);}
function render(){
const i=Number(step.value),frame=frames[i];if(!frame)return;
$('clock').textContent=frame.time.toFixed(5)+' s';
const visual=(Number(scale.value)/100)*dim/(data.maxDisplacement||1)*0.4;
const original=new Map(base.map(n=>[n.id,locate(n.x,n.y)]));
const positions=new Map(base.map(n=>{const u=frame.nodes.find(v=>v.id===n.id);return [n.id,locate(n.x+(u?.ux||0)*visual,n.y+(u?.uy||0)*visual)];}));
const graphic=$('structure');graphic.replaceChildren();
for(const e of edges){const a=original.get(e.start),b=original.get(e.end);if(a&&b)line(graphic,...a,...b,'#647b94',3);}
// Frame shape is interpolated with Euler-Bernoulli cubic Hermite transverse fields.
// Rotational DOFs affect the shape between nodes, not just the nodal markers.
for(const e of edges){
 if(e.type==='Truss2D'){
  const a=positions.get(e.start),b=positions.get(e.end);
  if(a&&b)line(graphic,...a,...b,'#72d5b1',4);
  continue;
 }
 const na=base.find(x=>x.id===e.start),nb=base.find(x=>x.id===e.end);
 const ua=frame.nodes.find(x=>x.id===e.start),ub=frame.nodes.find(x=>x.id===e.end);
 if(!na||!nb||!ua||!ub)continue;
 const dx=nb.x-na.x,dy=nb.y-na.y,L=Math.hypot(dx,dy);
 if(L===0)continue;
 const ex=dx/L,ey=dy/L,nx=-ey,ny=ex;
 const au=ex*ua.ux+ey*ua.uy,bu=ex*ub.ux+ey*ub.uy;
 const av=nx*ua.ux+ny*ua.uy,bv=nx*ub.ux+ny*ub.uy;
 const points=[];
 for(let j=0;j<=16;j++){
  const r=j/16,r2=r*r,r3=r2*r;
  const n1=1-3*r2+2*r3,n2=r-2*r2+r3,n3=3*r2-2*r3,n4=-r2+r3;
  const uAx=au*(1-r)+bu*r;
  const uTrans=n1*av+n2*L*ua.rz+n3*bv+n4*L*ub.rz;
  const x=na.x+ex*(r*L+visual*uAx)+nx*visual*uTrans;
  const y=na.y+ey*(r*L+visual*uAx)+ny*visual*uTrans;
  points.push(locate(x,y).join(','));
 }
 const poly=document.createElementNS(svgNS,'polyline');poly.setAttribute('points',points.join(' '));
 poly.setAttribute('fill','none');poly.setAttribute('stroke','#72d5b1');poly.setAttribute('stroke-width','4');graphic.appendChild(poly);
}
for(const [id,p] of positions){const c=document.createElementNS(svgNS,'circle');c.setAttribute('cx',p[0]);c.setAttribute('cy',p[1]);c.setAttribute('r',3.5);c.setAttribute('fill','#fcd77c');graphic.appendChild(c);}
plot(i);
}
function plot(i){const key=component.value,id=node.value;
const samples=frames.map(f=>({t:f.time,v:f.nodes.find(n=>n.id===id)?.[key]||0}));
const minT=samples[0]?.t||0,maxT=samples[samples.length-1]?.t||1;
const maxV=Math.max(...samples.map(p=>Math.abs(p.v)),1e-16);
const x=t=>40+720*(t-minT)/Math.max(1e-12,maxT-minT),y=v=>110-90*v/maxV;
const s=$('history');s.replaceChildren();line(s,40,110,760,110,'#668199',1);
const path=document.createElementNS(svgNS,'polyline');path.setAttribute('fill','none');path.setAttribute('stroke','#72d5b1');path.setAttribute('stroke-width','2');path.setAttribute('points',samples.map(p=>x(p.t)+','+y(p.v)).join(' '));s.appendChild(path);
const point=samples[i];if(point){const marker=document.createElementNS(svgNS,'circle');marker.setAttribute('cx',x(point.t));marker.setAttribute('cy',y(point.v));marker.setAttribute('r','5');marker.setAttribute('fill','#fcd77c');s.appendChild(marker);$('value').textContent=id+' '+key+' = '+point.v.toExponential(6)+' @ '+point.t.toFixed(6)+' s';}
}
let timer=null;
$('play').addEventListener('click',()=>{if(timer){clearInterval(timer);timer=null;$('play').textContent='Play';return;}timer=setInterval(()=>{step.value=(Number(step.value)+1)%frames.length;render();},80);$('play').textContent='Pause';});
for(const el of [step,scale,node,component])el.addEventListener('input',render);
render();
})();
""";
}
