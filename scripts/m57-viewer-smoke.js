// Optional, dependency-free smoke test for the embedded modal viewer JS.
// Runs under Node 18+ with a deliberately small mock DOM; a browser review is
// still mandatory for layout, accessibility, pan/zoom, and playback smoothness.
'use strict';
const fs = require('fs'), path = require('path'), assert = require('assert');
const js = fs.readFileSync(path.join(__dirname,
  '../src/StructuralSolver2D.Reporting/Visualization/Assets/modal-viewer.js'), 'utf8');
class FakeElement {
  constructor() { this.children = []; this.listeners = {}; this.attrs = {}; this.textContent = ''; this.value = '1'; }
  append(...nodes) { this.children.push(...nodes); }
  appendChild(node) { this.children.push(node); return node; }
  replaceChildren(...nodes) { this.children = [...nodes]; }
  setAttribute(k,v) { this.attrs[k] = String(v); }
  addEventListener(k,f) { this.listeners[k] = f; }
  getBoundingClientRect() { return {width:800,height:500,left:0,top:0}; }
  setPointerCapture() { }
}
const names = ['modal-data','modal-svg','model-baseline','modal-shape','model-nodes',
  'modal-mode','speed','amplitude','amplitude-label','speed-label','mode-title',
  'mode-metrics','node-contributions','warnings','play','pause','reset'];
const nodes = Object.fromEntries(names.map(n => [n,new FakeElement()]));
const input = {
  nodes:[{id:'A',x:0,y:0},{id:'B',x:4,y:0}],
  members:[{id:'E',a:'A',b:'B',frame:true}],
  modes:[
    {index:1,hz:12.5,period:.08,residual:1e-12,shape:[
      {id:'A',ux:0,uy:0,rz:0},{id:'B',ux:0,uy:1,rz:.1}]},
    {index:2,hz:75,period:1/75,residual:2e-12,shape:[
      {id:'A',ux:0,uy:0,rz:0},{id:'B',ux:0,uy:-.5,rz:-.4}]}
  ], warnings:['Illustrative only']
};
nodes['modal-data'].textContent = JSON.stringify(input);
nodes.amplitude.value='75';nodes.speed.value='1';nodes['modal-mode'].value='0';
const document = {
  getElementById: id => {assert(nodes[id], `missing mock ${id}`);return nodes[id];},
  createElementNS: () => new FakeElement(),
  createElement: () => new FakeElement(),
  addEventListener: () => {},
  hidden: false
};
(new Function('document','requestAnimationFrame','cancelAnimationFrame',js))(document,()=>1,()=>{});
assert.equal(nodes['modal-mode'].children.length,2);
assert.equal(nodes['model-baseline'].children.length,1);
assert.equal(nodes['modal-shape'].children.length,1);
assert.equal(nodes['node-contributions'].children.length,2);
assert.equal(nodes['warnings'].children.length,1);
const firstPath = nodes['modal-shape'].children[0].attrs.d;
nodes['modal-mode'].value='1';nodes['modal-mode'].listeners.change();
assert.notEqual(nodes['modal-shape'].children[0].attrs.d,firstPath);
nodes['amplitude'].value='0';nodes['amplitude'].listeners.input();
assert.equal(nodes['amplitude-label'].textContent,'0%');
nodes['reset'].listeners.click();
console.log('M57 modal viewer JS mock-DOM smoke: PASSED');
