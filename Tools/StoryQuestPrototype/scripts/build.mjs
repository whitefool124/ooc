import { cp, mkdir, readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const out = path.join(root, 'dist');
await mkdir(out, { recursive: true });
for (const item of ['index.html', 'src', 'assets']) await cp(path.join(root, item), path.join(out, item), { recursive: true });
const source = await readFile(path.join(root, 'index.html'), 'utf8');
if (!source.includes('./src/app.mjs')) throw new Error('Missing application entry');
await writeFile(path.join(out, 'build-info.json'), JSON.stringify({ name: 'OCC Story Quest Prototype', builtAt: new Date().toISOString(), type: 'standalone-static-web', entry: 'index.html' }, null, 2));
console.log(`OCC_STORY_BUILD ${out}`);
