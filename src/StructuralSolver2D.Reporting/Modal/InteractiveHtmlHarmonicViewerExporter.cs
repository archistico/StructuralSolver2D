using System.Text.Json;
using StructuralSolver2D.Analysis.PublicApi;

namespace StructuralSolver2D.Reporting.Modal;

/// <summary>Dependency-free standalone amplitude/phase frequency-response plots.
/// Steady-state displacement response, not a time-history or seismic-design viewer.</summary>
public sealed class InteractiveHtmlHarmonicViewerExporter
{
    public string Export(StructuralHarmonicAnalysisOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (output.Samples.Count == 0 || output.Samples[0].Nodes.Count == 0)
            throw new ArgumentException("M58_VIEWER_NO_SAMPLES: No frequency responses.");
        var data = output.Samples.Select(sample => new {
            frequency = sample.FrequencyHertz,
            nodes = sample.Nodes.Select(node => new { id = node.NodeId,
                ux = new[] { node.Ux.Amplitude, node.Ux.PhaseDegrees },
                uy = new[] { node.Uy.Amplitude, node.Uy.PhaseDegrees },
                rz = new[] { node.Rz.Amplitude, node.Rz.PhaseDegrees } }).ToArray()
        }).ToArray();
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        // System.Text.Json escapes <, > and & by default: prevent script injection via user node IDs.
        const string template = """
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>StructuralSolver2D — M58 harmonic frequency response</title>
<style>body{font:15px system-ui, sans-serif;max-width:1020px;margin:24px auto;padding:0 16px;color:#17212b}h1{font-size:1.5rem}label{display:inline-block;margin:8px 14px 8px 0}select{padding:7px}svg{display:block;width:100%;height:auto;border:1px solid #ccd3da;border-radius:6px;margin:16px 0}.plot{fill:none;stroke:#2469a3;stroke-width:2.5}.axis{stroke:#626f7d;stroke-width:1.2}text{font:12px system-ui;fill:#293847}.hint{font-size:.9rem;color:#455a68}</style></head>
<body><h1>Steady-state harmonic frequency response</h1>
<p class="hint">All forces use F₀ sin(Ωt). Amplitudes are physical displacement/rotation phasors; phases are relative to that sine input. Graphs connect supplied samples only: a narrow resonance between frequencies may be missed. Not a seismic design assessment.</p>
<label>Node <select id="node"></select></label><label>DOF <select id="dof"><option value="ux">Ux [m]</option><option value="uy">Uy [m]</option><option value="rz">Rz [rad]</option></select></label>
<h2>Amplitude</h2><svg id="amplitude" role="img" aria-label="Amplitude versus excitation frequency" viewBox="0 0 900 300"></svg>
<h2>Phase [degrees]</h2><svg id="phase" role="img" aria-label="Phase versus excitation frequency" viewBox="0 0 900 300"></svg>
<p class="hint" id="details"></p>
<script>
'use strict';
const samples=__M58_DATA__;
const $=id=>document.getElementById(id);
const nodes=Array.from(new Set(samples.flatMap(s=>s.nodes.map(n=>n.id))));
for(const id of nodes){const opt=document.createElement('option'); opt.value=id;opt.textContent=id;$('node').appendChild(opt);}
function render(){
  const node=$('node').value, dof=$('dof').value;
  const series=samples.map(s=>({frequency:s.frequency, pair:s.nodes.find(n=>n.id===node)?.[dof]})).filter(s=>s.pair);
  if(!series.length)return;
  plot('amplitude',series.map(s=>[s.frequency,s.pair[0]]),true);
  plot('phase',series.map(s=>[s.frequency,s.pair[1]]),false);
  $('details').textContent=`${node} / ${dof.toUpperCase()}: ${series.length} samples, maximum amplitude ${Math.max(...series.map(s=>s.pair[0])).toExponential(5)} ${dof==='rz'?'rad':'m'}.`;
}
function plot(element,points,amplitude){
 const svg=$(element);while(svg.firstChild)svg.removeChild(svg.firstChild);
 const minX=Math.min(...points.map(p=>p[0])),maxX=Math.max(...points.map(p=>p[0]));
 let minY=amplitude?0:Math.min(...points.map(p=>p[1]));
 let maxY=Math.max(...points.map(p=>p[1]));
 if(maxY===minY) {maxY+=1; if(!amplitude)minY-=1;}
 const dx=maxX-minX||1, dy=maxY-minY;const x=v=>75+780*(v-minX)/dx,y=v=>255-215*(v-minY)/dy;
 const ns='http://www.w3.org/2000/svg';
 function el(tag,attrs,content){const e=document.createElementNS(ns,tag);for(const [k,v]of Object.entries(attrs))e.setAttribute(k,String(v));if(content!==undefined)e.textContent=content;svg.appendChild(e);return e;}
 el('line',{class:'axis',x1:75,y1:255,x2:855,y2:255});el('line',{class:'axis',x1:75,y1:40,x2:75,y2:255});
 el('text',{x:75,y:275},minX.toPrecision(4)+' Hz');el('text',{x:800,y:275},maxX.toPrecision(4)+' Hz');
 el('text',{x:7,y:45},maxY.toPrecision(5));el('text',{x:7,y:255},minY.toPrecision(5));
 el('polyline',{class:'plot',points:points.map(p=>`${x(p[0])},${y(p[1])}`).join(' ')});
 for(const p of points)el('circle',{cx:x(p[0]),cy:y(p[1]),r:3.2,fill:'#2469a3'});
}
$('node').addEventListener('change',render);$('dof').addEventListener('change',render);render();
</script></body></html>
""";
        return template.Replace("__M58_DATA__", json, StringComparison.Ordinal);
    }
}
