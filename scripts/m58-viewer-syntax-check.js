'use strict';
// Optional Node.js syntax check of generated standalone HTML viewer.
const fs = require('node:fs');
const vm = require('node:vm');
const path = process.argv[2];
if (!path) throw new Error('Usage: node scripts/m58-viewer-syntax-check.js response.html');
const html = fs.readFileSync(path, 'utf8');
const matches = [...html.matchAll(/<script>([\s\S]*?)<\/script>/g)];
if (matches.length !== 1) throw new Error(`Expected exactly one inline viewer script, got ${matches.length}`);
new vm.Script(matches[0][1], {filename:'m58-generated-viewer.js'});
if (!html.includes('id="amplitude"') || !html.includes('id="phase"')) throw new Error('Missing both harmonic plots.');
console.log('M58 inline viewer JavaScript syntax OK.');
