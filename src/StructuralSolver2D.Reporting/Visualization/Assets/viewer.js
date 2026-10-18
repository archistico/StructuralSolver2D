(() => {
  const canvas = document.getElementById('viewerCanvas');
  const svg = canvas.querySelector('svg');
  const data = JSON.parse(document.getElementById('viewer-data').textContent);
  const mode = document.querySelector('[data-view]');
  const controls = [...document.querySelectorAll('[data-layer]')];
  const scaleInput = document.querySelector('[data-scale=deformed]');
  const textInput = document.querySelector('[data-text-scale=labels]');
  const speedInput = document.querySelector('[data-animation=speed]');
  const play = document.querySelector('[data-action=play-animation]');
  const pause = document.querySelector('[data-action=pause-animation]');
  const rect = svg.viewBox.baseVal;
  let viewBox = { x: rect.x, y: rect.y, width: rect.width, height: rect.height };
  let original;
  let dragging = false, pointerId = null, moved = false, last = { x: 0, y: 0 }, pointerStart, pressedElement;
  let animationRunning = false, animationTimestamp = null, animationPhase = 0, frameId = null;
  const number = value => new Intl.NumberFormat('en-GB', { maximumSignificantDigits: 6 }).format(value);
  const layer = selector => controls.find(control => control.dataset.layer === selector);
  const diagramControls = ['normal-force','shear-force','bending-moment'].map(kind => controls.find(control => control.dataset.layer.startsWith(`.diagram.${kind},`)));
  svg.setAttribute('role', 'group');

  function applyViewBox() { svg.setAttribute('viewBox', `${viewBox.x} ${viewBox.y} ${viewBox.width} ${viewBox.height}`); }
  function screenToSvg(x, y) { const matrix = svg.getScreenCTM(); return matrix ? new DOMPoint(x, y).matrixTransform(matrix.inverse()) : null; }
  function zoom(factor, x, y) {
    const bounds = svg.getBoundingClientRect();
    const anchor = screenToSvg(x ?? bounds.left + bounds.width / 2, y ?? bounds.top + bounds.height / 2);
    if (!anchor || viewBox.width * factor < .01 || viewBox.width * factor > 1e9) return;
    viewBox = { x: anchor.x - (anchor.x - viewBox.x) * factor, y: anchor.y - (anchor.y - viewBox.y) * factor,
      width: viewBox.width * factor, height: viewBox.height * factor };
    applyViewBox();
  }
  function visible(element) {
    for (let parent = element; parent && parent !== canvas; parent = parent.parentElement)
      if (getComputedStyle(parent).display === 'none') return false;
    return true;
  }
  function fitView() {
    const points = [];
    svg.querySelectorAll('.member:not(.member-hit),.support-symbol,.reaction,.load-arrow,.load-moment,.distributed-load-shape,.diagram,.dimension').forEach(element => {
      if (!visible(element)) return;
      if (animationRunning && element.matches('[data-deformed-polyline]')) return;
      const bounds = element.getBoundingClientRect();
      if (!bounds.width && !bounds.height) return;
      const a = screenToSvg(bounds.left,bounds.top), b = screenToSvg(bounds.right,bounds.bottom);
      if (a && b) points.push(a,b);
    });
    // Fit the whole loading/unloading cycle once, keeping the camera fixed between frames.
    if (animationRunning) svg.querySelectorAll('[data-deformed-polyline]').forEach(polyline => {
      if (!visible(polyline)) return;
      const base=parsePointList(polyline.dataset.basePoints),displacement=parsePointList(polyline.dataset.displacementPoints);
      if (base.length!==displacement.length) return;
      const amplitude=Number(scaleInput.value)/100;
      base.forEach((point,index)=>{
        for (const fraction of [0,1]) points.push({x:point[0]+fraction*amplitude*displacement[index][0],y:point[1]+fraction*amplitude*displacement[index][1]});
      });
    });
    // The maximum callout follows the midpoint of its moving leader. Include its text
    // at both extrema too, so fitting the curves does not leave the values outside the canvas.
    svg.querySelectorAll('[data-maximum-displacement]').forEach(annotation => {
      if (!visible(annotation)) return;
      annotation.querySelectorAll('text.annotation-label').forEach((label,index) => {
        if (!visible(label)) return;
        const bounds=label.getBBox(),x=Number(label.getAttribute('x')),y=Number(label.getAttribute('y'));
        const anchors=animationRunning ? [0,1].map(fraction=>({
          x:Number(annotation.dataset.baseX)+Number(annotation.dataset.dx)*Number(scaleInput.value)/100*fraction/2+6,
          y:Number(annotation.dataset.baseY)+Number(annotation.dataset.dy)*Number(scaleInput.value)/100*fraction/2-8+index*14
        })) : [{x,y}];
        anchors.forEach(anchor=>points.push({x:anchor.x+bounds.x-x,y:anchor.y+bounds.y-y},
          {x:anchor.x+bounds.x-x+bounds.width,y:anchor.y+bounds.y-y+bounds.height}));
      });
    });
    if (!points.length) return;
    const minX = Math.min(...points.map(point => point.x)), maxX = Math.max(...points.map(point => point.x));
    const minY = Math.min(...points.map(point => point.y)), maxY = Math.max(...points.map(point => point.y));
    let width = Math.max(30,maxX-minX)*1.18, height = Math.max(30,maxY-minY)*1.18;
    const aspect = canvas.clientWidth / Math.max(1,canvas.clientHeight);
    if (width / height < aspect) width = height * aspect; else height = width / aspect;
    viewBox = { x:(minX+maxX-width)/2, y:(minY+maxY-height)/2, width, height };
    applyViewBox();
  }
  function applyLayers() {
    const allowed = new Map();
    controls.forEach(control => svg.querySelectorAll(control.dataset.layer).forEach(element =>
      allowed.set(element,(allowed.get(element) ?? true) && control.checked)));
    allowed.forEach((show,element) => element.classList.toggle('hidden-layer',!show));
  }
  function applyMode(fit = true) {
    const value = mode.value;
    if (value === 'all') controls.forEach(control => { control.checked = true; });
    else if (value !== 'custom') {
      layer('#deformed-shape').checked = value === 'displacements';
      diagramControls.forEach((control,index) => { control.checked = value === ['n','v','m'][index]; });
    }
    applyLayers();
    if (animationRunning && !layer('#deformed-shape').checked) stopAnimation();
    document.querySelector('[data-view-status]').textContent = mode.selectedOptions[0].textContent;
    if (fit) fitView();
  }
  const parsePointList = value => (value || '').trim().split(/\s+/).filter(Boolean).map(pair => pair.split(',').map(Number));
  function applyDeformation(factor) {
    svg.querySelectorAll('[data-deformed-polyline=true]').forEach(polyline => {
      const base = parsePointList(polyline.dataset.basePoints), displacement = parsePointList(polyline.dataset.displacementPoints);
      if (!base.length || base.length !== displacement.length) return;
      polyline.setAttribute('points',base.map((point,index) => `${(point[0]+displacement[index][0]*factor).toFixed(3)},${(point[1]+displacement[index][1]*factor).toFixed(3)}`).join(' '));
    });
    svg.querySelectorAll('[data-deformation-label=true]').forEach(label =>
      label.setAttribute('transform',`translate(${(Number(label.dataset.dx || 0)*(factor-1)).toFixed(3)} ${(Number(label.dataset.dy || 0)*(factor-1)).toFixed(3)})`));
    svg.querySelectorAll('[data-maximum-displacement=true]').forEach(annotation => {
      const x1=Number(annotation.dataset.baseX),y1=Number(annotation.dataset.baseY);
      const x2=x1+Number(annotation.dataset.dx)*factor,y2=y1+Number(annotation.dataset.dy)*factor;
      const line=annotation.querySelector('.annotation-line'),point=annotation.querySelector('.annotation-point');
      line.setAttribute('x2',x2.toFixed(3));line.setAttribute('y2',y2.toFixed(3));
      point.setAttribute('cx',x2.toFixed(3));point.setAttribute('cy',y2.toFixed(3));
      annotation.querySelectorAll('text.annotation-label').forEach((label,index) => {
        label.setAttribute('x',((x1+x2)/2+6).toFixed(3));label.setAttribute('y',((y1+y2)/2-8+index*14).toFixed(3));
      });
    });
  }
  function applyVisualScales(factor = Number(scaleInput.value)/100) {
    applyDeformation(factor);
    document.querySelector('[data-effective-scale]').textContent = '×'+number(data.baseScale*factor);
  }
  // Smoothly reveal the static result and return to zero without reversing its displacement.
  const animationFactor = () => (1-Math.cos(animationPhase*Math.PI*2))/2;
  function animationLoop(timestamp) {
    frameId = null;
    if (!animationRunning) return;
    // Integrate frame intervals: changing speed must not rephase the elapsed history.
    if (animationTimestamp !== null)
      animationPhase=(animationPhase+Math.max(0,timestamp-animationTimestamp)/1000*Number(speedInput.value))%1;
    animationTimestamp=timestamp;
    applyVisualScales(Number(scaleInput.value)/100 * animationFactor());
    frameId = requestAnimationFrame(animationLoop);
  }
  play.addEventListener('click',() => {
    if (animationRunning) return;
    mode.value='displacements';applyMode(false);
    animationRunning=true;animationTimestamp=null;animationPhase=0;play.disabled=true;pause.disabled=false;
    fitView();
    frameId=requestAnimationFrame(animationLoop);
  });
  function stopAnimation() {
    animationRunning=false;animationTimestamp=null;animationPhase=0;
    if (frameId !== null) cancelAnimationFrame(frameId);
    frameId=null;play.disabled=false;pause.disabled=true;applyVisualScales();
  }
  pause.addEventListener('click',stopAnimation);
  scaleInput.addEventListener('input',() => {
    document.querySelector('[data-scale-output=deformed]').textContent=scaleInput.value+'%';
    if (animationRunning) {
      applyVisualScales(Number(scaleInput.value)/100*animationFactor());
      fitView();
    } else applyVisualScales();
  });
  textInput.addEventListener('input',() => {
    svg.style.setProperty('--viewer-label-scale',String(Number(textInput.value)/100));
    document.querySelector('[data-text-scale-output=labels]').textContent=textInput.value+'%';
  });
  speedInput.addEventListener('input',() => { document.querySelector('[data-animation-output=speed]').textContent=speedInput.value+'×'; });
  mode.addEventListener('change',() => applyMode());
  controls.forEach(control => control.addEventListener('change',() => {
    if (diagramControls.includes(control) || control === layer('#deformed-shape')) mode.value='custom';
    applyMode(false);
  }));
  canvas.addEventListener('wheel',event => { event.preventDefault();zoom(event.deltaY<0?.9:1.1,event.clientX,event.clientY); },{passive:false});
  canvas.addEventListener('pointerdown',event => {
    if (pointerId !== null || event.button !== 0) return;
    dragging=true;pointerId=event.pointerId;moved=false;last={x:event.clientX,y:event.clientY};pointerStart={...last};
    pressedElement=event.target.closest('[data-node-id],[data-member-id]');
    canvas.classList.add('dragging');canvas.setPointerCapture(event.pointerId);
  });
  canvas.addEventListener('pointermove',event => {
    if (!dragging || event.pointerId !== pointerId) return;
    const previous=screenToSvg(last.x,last.y),current=screenToSvg(event.clientX,event.clientY);
    if (!previous || !current) return;
    if (Math.abs(event.clientX-pointerStart.x)+Math.abs(event.clientY-pointerStart.y)>4) moved=true;
    viewBox.x-=current.x-previous.x;viewBox.y-=current.y-previous.y;last={x:event.clientX,y:event.clientY};applyViewBox();
  });
  function endDrag(event) {
    if (event.pointerId !== pointerId) return;
    dragging=false;pointerId=null;canvas.classList.remove('dragging');
    if (canvas.hasPointerCapture(event.pointerId)) canvas.releasePointerCapture(event.pointerId);
    if (event.type==='pointerup' && !moved && pressedElement) inspect(pressedElement);
    pressedElement=null;
  }
  canvas.addEventListener('pointerup',endDrag);canvas.addEventListener('pointercancel',endDrag);
  document.querySelector('[data-action=zoom-in]').addEventListener('click',() => zoom(.85));
  document.querySelector('[data-action=zoom-out]').addEventListener('click',() => zoom(1.15));
  document.querySelector('[data-action=fit]').addEventListener('click',fitView);
  document.querySelector('[data-action=reset]').addEventListener('click',() => { viewBox={...original};applyViewBox(); });
  canvas.addEventListener('keydown',event => {
    const shifts={ArrowLeft:[-.05,0],ArrowRight:[.05,0],ArrowUp:[0,-.05],ArrowDown:[0,.05]};
    if (shifts[event.key]) {event.preventDefault();viewBox.x+=shifts[event.key][0]*viewBox.width;viewBox.y+=shifts[event.key][1]*viewBox.height;applyViewBox();}
    else if (event.key==='+' || event.key==='=') {event.preventDefault();zoom(.85);}
    else if (event.key==='-') {event.preventDefault();zoom(1.15);}
    else if (event.key==='Home') {event.preventDefault();fitView();}
  });
  const inspector=document.getElementById('inspector'),details=document.getElementById('selection-details');
  function inspect(element) {
    svg.querySelectorAll('[data-selected]').forEach(item => item.removeAttribute('data-selected'));
    const node=element.dataset.nodeId ? data.nodes[element.dataset.nodeId] : null;
    const member=node?null:data.members[element.dataset.memberId];
    if (!node && !member) return;
    element.dataset.selected='true';details.replaceChildren();
    const heading=document.createElement('h2');heading.textContent=(node?'Node ':'Member ')+(node?.id || member.id);details.append(heading);
    const list=document.createElement('dl');details.append(list);
    const row=(name,value) => {const dt=document.createElement('dt'),dd=document.createElement('dd');dt.textContent=name;dd.textContent=value;list.append(dt,dd);};
    if (node) {
      if (node.label) row('Label',node.label);
      row('Coordinates',`${number(node.x)}, ${number(node.y)} m`);row('Ux',number(node.ux*1000)+' mm');row('Uy',number(node.uy*1000)+' mm');row('Rz',number(node.rz)+' rad');
    } else {
      row('Type',member.type);row('Nodes',member.start+' → '+member.end);row('Length',number(member.length)+' m');
      for (const [key,label,unit] of [['NormalForce','Max |N|','kN'],['ShearForce','Max |V|','kN'],['BendingMoment','Max |M|','kNm']])
        if (Object.hasOwn(member.forces,key)) row(label,number(member.forces[key])+' '+unit);
      if (member.stations.length) {
        const table=document.createElement('table'),caption=document.createElement('caption');caption.textContent='Station displacements';table.append(caption);
        const header=document.createElement('tr');for (const value of ['Station','Ux [mm]','Uy [mm]','Rz [rad]']) {const th=document.createElement('th');th.textContent=value;header.append(th);} table.append(header);
        member.stations.forEach(station => {const tr=document.createElement('tr');for (const value of [station.label,number(station.ux*1000),number(station.uy*1000),number(station.rz)]) {const td=document.createElement('td');td.textContent=value;tr.append(td);}table.append(tr);});details.append(table);
      }
    }
    inspector.hidden=false;
  }
  // Wide transparent hit areas keep thin member axes usable with touch and mouse.
  svg.querySelectorAll('#undeformed-model .member[data-member-id]').forEach(line => {
    const hit=line.cloneNode();hit.setAttribute('class','member-hit');line.parentElement.insertBefore(hit,line);
  });
  svg.querySelectorAll('#undeformed-model .node[data-node-id]').forEach(node => {
    const hit=node.cloneNode();hit.setAttribute('class','node-hit');node.parentElement.insertBefore(hit,node);
  });
  svg.querySelectorAll('[data-member-id],[data-node-id]').forEach(element => {
    if (element.classList.contains('node-hit')) return;
    if (element.classList.contains('member') && element.classList.contains('undeformed')) return;
    element.setAttribute('tabindex','0');element.setAttribute('role','button');
    element.setAttribute('aria-label',(element.dataset.nodeId?'Inspect node ':'Inspect member ')+(element.dataset.nodeId || element.dataset.memberId));
    element.addEventListener('click',() => {if (!moved) inspect(element);});
    element.addEventListener('keydown',event => {if (event.key==='Enter' || event.key===' ') {event.preventDefault();inspect(element);}});
  });
  document.querySelector('[data-action=close-inspector]').addEventListener('click',() => {inspector.hidden=true;canvas.focus();});
  [scaleInput,textInput,speedInput].forEach(input => input.dispatchEvent(new Event('input')));
  mode.value=data.initialView;applyMode(false);applyVisualScales();fitView();original={...viewBox};
})();
