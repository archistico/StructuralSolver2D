// Euler–Bernoulli Hermite interpolation is used between Frame2D nodes.
(() => {
  'use strict';
  const source = document.getElementById('modal-data');
  const data = JSON.parse(source.textContent);
  const svg = document.getElementById('modal-svg');
  const NS = 'http://www.w3.org/2000/svg';
  const baseline = document.getElementById('model-baseline');
  const deformed = document.getElementById('modal-shape');
  const nodeGroup = document.getElementById('model-nodes');
  const selector = document.getElementById('modal-mode');
  const speed = document.getElementById('speed');
  const amplitude = document.getElementById('amplitude');
  const nodes = new Map(data.nodes.map(n => [n.id, n]));
  const modelExtent = (() => {
    const xs = data.nodes.map(n => n.x), ys = data.nodes.map(n => -n.y);
    const xmin = Math.min(...xs), xmax = Math.max(...xs), ymin = Math.min(...ys), ymax = Math.max(...ys);
    const span = Math.max(xmax - xmin, ymax - ymin, 1e-5), margin = Math.max(span * .25, .2);
    return [xmin - margin, ymin - margin, xmax - xmin + 2 * margin, ymax - ymin + 2 * margin];
  })();
  function element(tag, parent, attrs) {
    const e = document.createElementNS(NS, tag);
    Object.entries(attrs || {}).forEach(([k, v]) => e.setAttribute(k, String(v)));
    parent.appendChild(e);
    return e;
  }
  function lineShape(member, t, shapes) {
    const a = nodes.get(member.a), b = nodes.get(member.b), sa = shapes.get(member.a), sb = shapes.get(member.b);
    const dx = b.x - a.x, dy = b.y - a.y, L = Math.hypot(dx, dy), c = dx / L, s = dy / L;
    let ux, uy;
    if (member.frame) {
      const au = c * sa.ux + s * sa.uy, av = -s * sa.ux + c * sa.uy;
      const bu = c * sb.ux + s * sb.uy, bv = -s * sb.ux + c * sb.uy;
      const u = (1 - t) * au + t * bu;
      const t2 = t * t, t3 = t2 * t;
      const v = (1 - 3*t2 + 2*t3) * av + (t - 2*t2 + t3) * L * sa.rz
              + (3*t2 - 2*t3) * bv + (-t2 + t3) * L * sb.rz;
      ux = c * u - s * v; uy = s * u + c * v;
    } else {
      ux = (1 - t) * sa.ux + t * sb.ux;
      uy = (1 - t) * sa.uy + t * sb.uy;
    }
    return { x: a.x + t * dx, y: a.y + t * dy, ux, uy };
  }
  const routes = data.members.map(m => ({ m, base: data.nodes.length ? nodes.get(m.a) : null }));
  routes.forEach(({m}) => {
    const a = nodes.get(m.a), b = nodes.get(m.b);
    element('line', baseline, {x1:a.x, y1:-a.y, x2:b.x, y2:-b.y});
  });
  data.nodes.forEach(n => element('circle', nodeGroup, {cx:n.x,cy:-n.y,r:Math.max(modelExtent[2],modelExtent[3])*.004}));
  data.modes.forEach((m,i) => {
    const option = document.createElement('option'); option.value = String(i);
    option.textContent = `Mode ${m.index} — ${m.hz.toPrecision(6)} Hz`; selector.appendChild(option);
  });
  const diagList = document.getElementById('warnings');
  data.warnings.forEach(msg => { const li=document.createElement('li');li.textContent=msg;diagList.appendChild(li); });
  let selected = 0, playing = false, lastTime = 0, phase = Math.PI / 2, frameId = null;
  let shapeMap = new Map(), maximum = 1;
  function loadMode() {
    const mode = data.modes[selected];
    shapeMap = new Map(mode.shape.map(s => [s.id,s]));
    maximum = 0;
    routes.forEach(({m}) => { for(let i=0;i<=20;i++) {
      const p=lineShape(m,i/20,shapeMap);maximum=Math.max(maximum,Math.hypot(p.ux,p.uy));
    }});
    if (!(maximum>0)) maximum=1;
    document.getElementById('mode-title').textContent=`Mode ${mode.index}`;
    const dl=document.getElementById('mode-metrics');dl.replaceChildren();
    [['Frequency',`${mode.hz.toPrecision(8)} Hz`],['Period',`${mode.period.toPrecision(8)} s`],
     ['Relative residual',mode.residual.toExponential(3)],['Normalization','Mass: φᵀMφ = 1']].forEach(([name,val]) => {
       const dt=document.createElement('dt'),dd=document.createElement('dd');dt.textContent=name;dd.textContent=val;dl.append(dt,dd);
     });
    const largest=Math.max(...mode.shape.map(s=>Math.hypot(s.ux,s.uy)),0);
    const table=document.getElementById('node-contributions');table.replaceChildren();
    mode.shape.forEach(s=>{const tr=document.createElement('tr'),a=document.createElement('td'),b=document.createElement('td');
      a.textContent=s.id;b.textContent=largest>0?`${(100*Math.hypot(s.ux,s.uy)/largest).toFixed(1)}%`:'0%';tr.append(a,b);table.appendChild(tr);
    });
    draw();
  }
  function draw() {
    const factor = .17 * Math.max(modelExtent[2],modelExtent[3]) / maximum
      * (Number(amplitude.value)/100) * Math.sin(phase);
    deformed.replaceChildren();
    routes.forEach(({m})=>{
      let d='';for(let i=0;i<=24;i++){
        const p=lineShape(m,i/24,shapeMap),x=p.x+factor*p.ux,y=-(p.y+factor*p.uy);
        d+=`${i===0?'M':' L'}${x.toPrecision(14)} ${y.toPrecision(14)}`;
      }
      element('path',deformed,{d,'data-member-id':m.id});
    });
  }
  function tick(time) {
    if(!playing) return;
    if(lastTime) phase += Math.min((time-lastTime)/1000,.1) * 2*Math.PI * Number(speed.value);
    lastTime=time;draw();frameId=requestAnimationFrame(tick);
  }
  function stop(){playing=false;lastTime=0;if(frameId!==null)cancelAnimationFrame(frameId);frameId=null;}
  document.getElementById('play').addEventListener('click',()=>{if(playing)return;playing=true;lastTime=0;frameId=requestAnimationFrame(tick);});
  document.getElementById('pause').addEventListener('click',stop);
  document.getElementById('reset').addEventListener('click',()=>{stop();phase=Math.PI/2;svg.setAttribute('viewBox',modelExtent.join(' '));draw();});
  selector.addEventListener('change',()=>{selected=Number(selector.value);phase=Math.PI/2;loadMode();});
  amplitude.addEventListener('input',()=>{document.getElementById('amplitude-label').textContent=amplitude.value+'%';draw();});
  speed.addEventListener('input',()=>{document.getElementById('speed-label').textContent=speed.value+' ×';});
  document.addEventListener('visibilitychange',()=>{if(document.hidden)stop();});
  // Wheel zoom and pointer drag pan the *view* only; never change modal values.
  let viewport = [...modelExtent], dragging = null;
  const applyViewport = () => svg.setAttribute('viewBox', viewport.join(' '));
  svg.addEventListener('wheel', event => {
    event.preventDefault();
    const rect = svg.getBoundingClientRect();
    if (!rect.width || !rect.height) return;
    const px = Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width));
    const py = Math.max(0, Math.min(1, (event.clientY - rect.top) / rect.height));
    const zoom = event.deltaY > 0 ? 1.15 : 1 / 1.15;
    const cx = viewport[0] + px * viewport[2];
    const cy = viewport[1] + py * viewport[3];
    viewport = [cx - px * viewport[2] * zoom, cy - py * viewport[3] * zoom,
                viewport[2] * zoom, viewport[3] * zoom];
    applyViewport();
  }, {passive:false});
  svg.addEventListener('pointerdown', event => {
    dragging = [event.pointerId, event.clientX, event.clientY];
    svg.setPointerCapture(event.pointerId);
  });
  svg.addEventListener('pointermove', event => {
    if (!dragging || dragging[0] !== event.pointerId) return;
    const rect = svg.getBoundingClientRect();
    if (!rect.width || !rect.height) return;
    viewport[0] -= (event.clientX - dragging[1]) * viewport[2] / rect.width;
    viewport[1] -= (event.clientY - dragging[2]) * viewport[3] / rect.height;
    dragging[1] = event.clientX; dragging[2] = event.clientY;
    applyViewport();
  });
  svg.addEventListener('pointerup', event => { if (dragging && dragging[0] === event.pointerId) dragging = null; });
  svg.addEventListener('pointercancel', () => { dragging = null; });
  document.getElementById('reset').addEventListener('click', () => {
    viewport = [...modelExtent]; applyViewport();
  });
  applyViewport();
  loadMode();
})();
