import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const root = fileURLToPath(new URL('../', import.meta.url));
const directory = resolve(root, 'out/reporting-browser-tests');
mkdirSync(directory, { recursive: true });
const html = join(directory, 'viewer-tests.html');
const generated = spawnSync('dotnet', ['run', '--project',
  resolve(root, 'tests/StructuralSolver2D.Reporting.BrowserTests/StructuralSolver2D.Reporting.BrowserTests.csproj'),
  '--verbosity', 'quiet', '--', html], { cwd: root, encoding: 'utf8', windowsHide: true, timeout: 60000 });
if (generated.status !== 0) throw new Error(`Cannot generate viewer test fixture: ${generated.error?.message || generated.stderr || generated.stdout}`);
const browser = [process.env.CHROME_PATH,
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
  '/usr/bin/chromium', '/usr/bin/chromium-browser', '/usr/bin/google-chrome',
  '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome']
  .find(candidate => candidate && existsSync(candidate));
if (!browser) throw new Error('Chrome/Chromium not found. Set CHROME_PATH to the browser executable.');
const result = spawnSync(browser, ['--headless', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
  '--disable-background-networking', '--disable-extensions',
  `--user-data-dir=${mkdtempSync(join(directory, 'profile-'))}`, '--dump-dom', pathToFileURL(html).href],
  { cwd: root, encoding: 'utf8', windowsHide: true, timeout: 20000, maxBuffer: 8 * 1024 * 1024 });
const encoded = result.stdout?.match(/<pre id="browser-test-results">([^<]+)<\/pre>/)?.[1];
if (!encoded) throw new Error(`Browser tests did not complete: ${result.error?.message || result.stderr}`);
const summary = JSON.parse(Buffer.from(encoded, 'base64').toString('utf8'));
writeFileSync(join(directory, 'results.json'), JSON.stringify(summary, null, 2));
for (const test of summary.results) console.log(`${test.passed ? 'PASS' : 'FAIL'} ${test.name}${test.error ? ': ' + test.error : ''}`);
console.log(`Browser tests: ${summary.passed} passed, ${summary.failed} failed`);
if (summary.failed || result.status !== 0) process.exitCode = 1;
