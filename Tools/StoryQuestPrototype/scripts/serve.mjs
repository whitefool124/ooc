import http from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const port = Number(process.env.OCC_STORY_PORT || 4178);
const mime = { '.html': 'text/html; charset=utf-8', '.mjs': 'text/javascript; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8', '.png': 'image/png', '.json': 'application/json; charset=utf-8', '.ttf': 'font/ttf' };
const server = http.createServer(async (request, response) => {
  try {
    const pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
    if (pathname === '/favicon.ico') { response.writeHead(204); response.end(); return; }
    const relative = pathname === '/' ? 'index.html' : pathname.replace(/^\/+/, '');
    if (relative !== 'index.html' && !/^(src|assets)\//.test(relative)) throw new Error('Not found');
    const target = path.resolve(root, relative);
    if (!target.startsWith(root + path.sep) || !(await stat(target)).isFile()) throw new Error('Not found');
    const data = await readFile(target);
    response.writeHead(200, { 'Content-Type': mime[path.extname(target)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
    response.end(data);
  } catch {
    response.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
    response.end('页面不存在');
  }
});
server.listen(port, '127.0.0.1', () => console.log(`OCC_STORY_READY http://127.0.0.1:${port}`));
