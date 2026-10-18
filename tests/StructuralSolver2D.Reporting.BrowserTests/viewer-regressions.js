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
    window.requestAnimationFrame = callback => { queue.push(callback); return queue.length; };
    document.querySelector('[data-action=play-animation]').click();
    assert(queue.length > 0, 'Animation did not schedule a frame');
    queue.shift()(1000);
    queue.shift()(1750); // speed 1, negative peak at t = 0.75 s.
    checkAnnotation();
    document.querySelector('[data-action=pause-animation]').click();
  });
  const summary = { passed: results.filter(result => result.passed).length, failed: results.filter(result => !result.passed).length, results };
  const pre = document.createElement('pre');
  pre.id = 'browser-test-results';
  pre.textContent = btoa(JSON.stringify(summary));
  document.body.appendChild(pre);
})();
