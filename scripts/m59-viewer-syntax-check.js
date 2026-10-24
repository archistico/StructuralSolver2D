// Node-only syntax verification; does not require a browser or external packages.
const fs = require('fs');
const vm = require('vm');
const path = process.argv[2];
if (!path) throw new Error('Expected HTML path');
const html = fs.readFileSync(path, 'utf8');
const matches = [...html.matchAll(/<script(?:\s[^>]*)?>([\s\S]*?)<\/script>/gi)];
if (matches.length < 2) throw new Error('M59 viewer JS or embedded data missing');
const script = matches[matches.length-1][1];
new vm.Script(script,{filename:'m59-viewer.js'});
for (const id of ['id="step"','id="history"','id="structure"','id="scale"'])
  if (!html.includes(id)) throw new Error('Viewer missing '+id);
console.log('M59 inline viewer JavaScript syntax OK.');
