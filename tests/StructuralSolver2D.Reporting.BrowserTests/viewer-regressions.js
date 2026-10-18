(() => {
  const results = [];
  function test(name, action) {
    try { action(); results.push({ name, passed: true }); }
    catch (error) { results.push({ name, passed: false, error: String(error.message || error) }); }
  }
  function assert(condition, message) { if (!condition) throw new Error(message); }
  function close(actual, expected, tolerance, message) {
    assert(Number.isFinite(actual) && Math.abs(actual - expected) <= tolerance, `${message}: ${actual} vs ${expected}`);
  }
  const svg = document.querySelector('#viewerCanvas svg');
  const canvas = document.getElementById('viewerCanvas');
  const initial = svg.getAttribute('viewBox');
  const reset = () => document.querySelector('[data-action=reset]').click();
  const toSvg = (x, y) => new DOMPoint(x, y).matrixTransform(svg.getScreenCTM().inverse());

  test('Accepted fractional initial controls survive browser initialization', () => {
    close(Number(document.querySelector('[data-scale=deformed]').value),102,1e-12,'Initial deformation rounded');
    close(Number(document.querySelector('[data-text-scale=labels]').value),117,1e-12,'Initial text rounded');
    close(Number(document.querySelector('[data-animation=speed]').value),1.1,1e-12,'Initial speed rounded');
  });
  document.querySelector('[data-animation=speed]').value='1';
  document.querySelector('[data-animation=speed]').dispatchEvent(new Event('input'));

  if (window.simpleTestFixture) {

  test('Reset restores original viewBox after zoom', () => {
    document.querySelector('[data-action=zoom-in]').click();
    assert(svg.getAttribute('viewBox') !== initial, 'Zoom did not change the view');
    reset();
    assert(svg.getAttribute('viewBox') === initial, 'Reset did not restore the initial view');
  });

  for (const [width, height] of [[1200, 350], [500, 800], [700, 450]]) {
    canvas.style.width = `${width}px`;
    canvas.style.height = `${height}px`;
    for (const fraction of [.25, .75]) {
      test(`Wheel keeps cursor anchor in ${width}x${height} at ${fraction}`, () => {
        reset();
        const rect = svg.getBoundingClientRect();
        const x = Math.round(rect.left + rect.width * fraction), y = Math.round(rect.top + rect.height * fraction);
        const before = toSvg(x, y);
        canvas.dispatchEvent(new WheelEvent('wheel', { clientX: x, clientY: y, deltaY: -100, cancelable: true }));
        const after = toSvg(x, y);
        close(after.x, before.x, 1e-4, 'Cursor X moved');
        close(after.y, before.y, 1e-4, 'Cursor Y moved');
      });
    }
    test(`Pan preserves drag anchor in ${width}x${height}`, () => {
      reset();
      // Synthetic pointer events have no active hardware pointer to capture. Only capture is stubbed;
      // event handlers, SVG layout, viewBox and coordinate transforms run in the real browser.
      canvas.setPointerCapture = () => {};
      canvas.releasePointerCapture = () => {};
      canvas.hasPointerCapture = () => false;
      const rect = svg.getBoundingClientRect();
      const x = Math.round(rect.left + rect.width * .4), y = Math.round(rect.top + rect.height * .4);
      const before = toSvg(x, y);
      canvas.dispatchEvent(new PointerEvent('pointerdown', { pointerId: 1, clientX: x, clientY: y }));
      canvas.dispatchEvent(new PointerEvent('pointermove', { pointerId: 1, clientX: x + 40, clientY: y + 20 }));
      canvas.dispatchEvent(new PointerEvent('pointerup', { pointerId: 1, clientX: x + 40, clientY: y + 20 }));
      const after = toSvg(x + 40, y + 20);
      close(after.x, before.x, 1e-4, 'Drag X anchor moved');
      close(after.y, before.y, 1e-4, 'Drag Y anchor moved');
    });
  }
  test('Reset restores original viewBox after pan', () => {
    reset();
    assert(svg.getAttribute('viewBox') === initial, 'Reset retained the panned rectangle');
  });

  const scale = document.querySelector('[data-scale=deformed]');
  const annotation = svg.querySelector('#deformed-shape .annotation-point');
  const line = svg.querySelector('#deformed-shape .annotation-line');
  const labels = [...svg.querySelectorAll('#deformed-shape text.annotation-label')];
  const physicalText = labels.map(label => label.textContent).join('\n');
  function setScale(value) { scale.value = String(value); scale.dispatchEvent(new Event('input')); }
  function checkAnnotation() {
    const polyline = svg.querySelector('[data-deformed-polyline=true]');
    const points = polyline.getAttribute('points').trim().split(/\s+/).map(pair => pair.split(',').map(Number));
    const tip = points[points.length - 1];
    close(Number(annotation.getAttribute('cx')), tip[0], .003, 'Maximum marker X misses deformed tip');
    close(Number(annotation.getAttribute('cy')), tip[1], .003, 'Maximum marker Y misses deformed tip');
    close(Number(line.getAttribute('x2')), tip[0], .003, 'Maximum line X misses tip');
    close(Number(line.getAttribute('y2')), tip[1], .003, 'Maximum line Y misses tip');
    const base = polyline.dataset.basePoints.trim().split(/\s+/).pop().split(',').map(Number);
    close(Number(line.getAttribute('x1')), base[0], .003, 'Maximum line base X moved');
    close(Number(line.getAttribute('y1')), base[1], .003, 'Maximum line base Y moved');
    close(Number(labels[0].getAttribute('x')), (base[0] + tip[0]) / 2 + 6, .003, 'Maximum label X misses midpoint');
    close(Number(labels[0].getAttribute('y')), (base[1] + tip[1]) / 2 - 8, .003, 'Maximum label Y misses midpoint');
    assert(labels.map(label => label.textContent).join('\n') === physicalText, 'Visual scaling changed physical values');
  }
  for (const value of [0, 100, 200]) test(`Maximum annotation follows deformation at ${value}%`, () => {
    setScale(value);
    checkAnnotation();
  });
  test('Maximum annotation follows negative animation phase', () => {
    setScale(100);
    const queue = [];
    const originalRequest = window.requestAnimationFrame;
    window.requestAnimationFrame = callback => { queue.push(callback); return queue.length; };
    document.querySelector('[data-action=play-animation]').click();
    assert(queue.length > 0, 'Animation did not schedule a frame');
    queue.shift()(1000);
    queue.shift()(1750); // speed 1, negative peak at t = 0.75 s.
    checkAnnotation();
    document.querySelector('[data-action=pause-animation]').click();
    window.requestAnimationFrame = originalRequest;
  });
  }

  const view=document.querySelector('[data-view]');
  const changeView=value => {view.value=value;view.dispatchEvent(new Event('change'));};
  const layer=selector => [...document.querySelectorAll('[data-layer]')].find(input => input.dataset.layer===selector);
  const toggle=(selector,value) => {const input=layer(selector);input.checked=value;input.dispatchEvent(new Event('change'));};
  function visible(element) {
    if(!element) return false;
    for(let current=element;current && current!==canvas;current=current.parentElement)
      if(getComputedStyle(current).display==='none') return false;
    return true;
  }
  canvas.style.width='';canvas.style.height='';changeView('model');
  test('Default model view stays uncluttered and secondary controls are collapsed',() => {
    assert(!document.querySelector('.viewer-details').open,'Secondary controls expanded');
    assert(!visible(svg.querySelector('[data-deformed-polyline]')),'Deformation initially visible');
    assert([...svg.querySelectorAll('.diagram')].every(element=>!visible(element)),'Diagram initially visible');
    assert([...svg.querySelectorAll('text')].filter(visible).length<=3,'Too many initial labels');
    assert(document.querySelectorAll('.primary-controls button,.primary-controls input,.primary-controls select').length<=6,'Too many primary controls');
  });
  test('Responsive layout keeps the diagram in the first viewport without document overflow',() => {
    assert(document.documentElement.scrollWidth<=innerWidth+1,`Document overflows: ${document.documentElement.scrollWidth} > ${innerWidth}`);
    const rect=canvas.getBoundingClientRect();
    assert(rect.top<innerHeight*.5,`Diagram starts too low: ${rect.top}`);
    assert(Math.min(rect.bottom,innerHeight)-Math.max(rect.top,0)>innerHeight*.4,'Too little diagram area visible');
  });
  for(const [name,kind] of [['n','normal-force'],['v','shear-force'],['m','bending-moment']]) test(`Mode ${name} shows one result quantity`,() => {
    changeView(name);
    assert(!visible(svg.querySelector('[data-deformed-polyline]')),'Deformation overlaps force mode');
    for(const diagram of svg.querySelectorAll('.diagram')) assert(visible(diagram)===diagram.classList.contains(kind),'Other quantity remains visible');
  });
  test('Context layers remain visible when the undeformed model is hidden',() => {
    changeView('model');toggle('#supports',true);toggle('#reactions',true);toggle('#dimensions',true);toggle('#undeformed-model',false);
    for(const selector of ['#supports','#reactions','#dimensions']) {
      assert(layer(selector).checked,'Context checkbox was cleared');
      assert(visible(svg.querySelector(selector)),'Context layer hidden: '+selector);
      assert(svg.querySelector(selector).parentElement===svg,'Context group is nested');
    }
    toggle('#undeformed-model',true);toggle('#reactions',false);toggle('#dimensions',false);
  });
  test('All layers remains available on request',() => {
    changeView('all');assert([...svg.querySelectorAll('.diagram')].every(visible),'Full view omits diagrams');
    assert(visible(svg.querySelector('[data-deformed-polyline]')),'Full view omits deformation');
    // Return to the quiet defaults for the remaining checks.
    for(const input of document.querySelectorAll('[data-layer]')) input.checked=['#undeformed-model','#supports','#loads'].includes(input.dataset.layer);
    changeView('model');
  });
  test('Amplification feedback and Fit view follow the current deformation',() => {
    changeView('displacements');const input=document.querySelector('[data-scale=deformed]');
    input.value='200';input.dispatchEvent(new Event('input'));
    const data=JSON.parse(document.getElementById('viewer-data').textContent);
    assert(document.querySelector('[data-effective-scale]').textContent==='×'+(data.baseScale*2),'Feedback still shows base amplification');
    document.querySelector('[data-action=fit]').click();const box=svg.viewBox.baseVal;
    for(const polyline of svg.querySelectorAll('[data-deformed-polyline]')) for(const pair of polyline.getAttribute('points').split(' ')) {
      const [x,y]=pair.split(',').map(Number);assert(x>=box.x-.01 && x<=box.x+box.width+.01 && y>=box.y-.01 && y<=box.y+box.height+.01,'Fit clips deformation');
    }
    input.value='100';input.dispatchEvent(new Event('input'));
  });
  test('Play is idempotent and Pause cancels every pending frame',() => {
    const request=window.requestAnimationFrame,cancel=window.cancelAnimationFrame;
    const callbacks=new Map();let id=0;
    window.requestAnimationFrame=callback=>{callbacks.set(++id,callback);return id;};window.cancelAnimationFrame=key=>callbacks.delete(key);
    const play=document.querySelector('[data-action=play-animation]'),pause=document.querySelector('[data-action=pause-animation]');
    try {
      for(let i=0;i<10;i++) play.click();assert(callbacks.size===1,'More than one animation chain');
      const [key,callback]=[...callbacks][0];callbacks.delete(key);callback(0);assert(callbacks.size===1,'Frame multiplies callbacks');
      pause.click();assert(callbacks.size===0,'Pause leaves a pending frame');
      play.click();assert(callbacks.size===1,'Pause/Play duplicates callbacks');pause.click();assert(callbacks.size===0,'Second pause leaves callback');
    } finally {pause.click();window.requestAnimationFrame=request;window.cancelAnimationFrame=cancel;}
  });
  test('Keyboard can pan and fit the scene',() => {
    changeView('model');canvas.focus();assert(document.activeElement===canvas,'Canvas cannot receive focus');
    const before=svg.viewBox.baseVal.x;canvas.dispatchEvent(new KeyboardEvent('keydown',{key:'ArrowRight',bubbles:true,cancelable:true}));
    assert(svg.viewBox.baseVal.x!==before,'Keyboard pan did not change view');
    canvas.dispatchEvent(new KeyboardEvent('keydown',{key:'Home',bubbles:true,cancelable:true}));
    close(svg.viewBox.baseVal.x,before,.01,'Home did not fit the scene');
  });
  test('Node values are available through keyboard selection',() => {
    changeView('model');const data=JSON.parse(document.getElementById('viewer-data').textContent);
    const node=Object.values(data.nodes).sort((a,b)=>(Math.abs(b.ux)+Math.abs(b.uy))-(Math.abs(a.ux)+Math.abs(a.uy)))[0];
    const element=[...svg.querySelectorAll('[data-node-id][tabindex]')].find(item=>item.dataset.nodeId===node.id);
    element.focus();element.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',bubbles:true,cancelable:true}));
    const text=document.getElementById('selection-details').textContent;
    assert(!document.getElementById('inspector').hidden && text.includes(node.id),'Node details unavailable');
    assert(text.includes('Ux') && text.includes('Uy') && text.includes('mm') && text.includes('Rz'),'Node details omit physical values/units');
    document.querySelector('[data-action=close-inspector]').click();
  });
  test('Member values and stations are available without global labels',() => {
    const element=svg.querySelector('.member-hit');element.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',bubbles:true,cancelable:true}));
    const text=document.getElementById('selection-details').textContent;
    assert(text.includes(element.dataset.memberId) && text.includes('Length') && text.includes('Max |N|'),'Member details omit results');
    const data=JSON.parse(document.getElementById('viewer-data').textContent);
    if(data.members[element.dataset.memberId].stations.length) assert(text.includes('L/4') && text.includes('L/2') && text.includes('3L/4'),'Station values missing');
    document.querySelector('[data-action=close-inspector]').click();
  });
  test('Pointer selection opens details and a drag does not select',() => {
    changeView('model');
    const capture=canvas.setPointerCapture,release=canvas.releasePointerCapture,has=canvas.hasPointerCapture;
    canvas.setPointerCapture=()=>{};canvas.releasePointerCapture=()=>{};canvas.hasPointerCapture=()=>false;
    const element=svg.querySelector('.member-hit');
    try {
      element.dispatchEvent(new PointerEvent('pointerdown',{pointerId:7,clientX:100,clientY:100,bubbles:true}));
      canvas.dispatchEvent(new PointerEvent('pointerup',{pointerId:7,clientX:100,clientY:100,bubbles:true}));
      assert(!document.getElementById('inspector').hidden,'Pointer capture prevents selection');
      document.querySelector('[data-action=close-inspector]').click();
      element.dispatchEvent(new PointerEvent('pointerdown',{pointerId:7,clientX:100,clientY:100,bubbles:true}));
      for(let delta=1;delta<=12;delta++) canvas.dispatchEvent(new PointerEvent('pointermove',{pointerId:7,clientX:100+delta,clientY:100,bubbles:true}));
      canvas.dispatchEvent(new PointerEvent('pointerup',{pointerId:7,clientX:112,clientY:100,bubbles:true}));
      assert(document.getElementById('inspector').hidden,'Dragging selected a member');
    } finally {canvas.setPointerCapture=capture;canvas.releasePointerCapture=release;canvas.hasPointerCapture=has;}
  });
  test('Load and support graphics do not block node hit targets',() => {
    changeView('model');document.querySelector('[data-action=fit]').click();
    for(const node of svg.querySelectorAll('.node[data-node-id]')) {
      const rect=node.getBoundingClientRect(),x=(rect.left+rect.right)/2,y=(rect.top+rect.bottom)/2;
      if(x<0 || x>=innerWidth || y<0 || y>=innerHeight) continue;
      const hit=document.elementFromPoint(x,y)?.closest('[data-node-id]');
      assert(hit?.dataset.nodeId===node.dataset.nodeId,'Node hit target is covered: '+node.dataset.nodeId);
    }
  });
  const summary = { passed: results.filter(result => result.passed).length, failed: results.filter(result => !result.passed).length, results };
  const pre = document.createElement('pre');
  pre.id = 'browser-test-results';
  pre.textContent = btoa(JSON.stringify(summary));
  document.body.appendChild(pre);
})();
