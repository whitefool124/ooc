const fs = require('fs');
const path = require('path');

const outputPath = path.resolve(__dirname, '../../Artifacts/OCC_术式范围形状预制表_2026-09-09.svg');
const shapes = [
  ['直线', (n) => cellsWhere((x, y) => y === 0 && x >= 1 && x <= n)],
  ['十字', (n) => cellsWhere((x, y) => (x === 0 || y === 0) && Math.abs(x) + Math.abs(y) >= 1 && Math.abs(x) + Math.abs(y) <= n)],
  ['X形', (n) => cellsWhere((x, y) => Math.abs(x) === Math.abs(y) && Math.abs(x) >= 1 && Math.abs(x) <= n)],
  ['菱形', (n) => cellsWhere((x, y) => Math.abs(x) + Math.abs(y) >= 1 && Math.abs(x) + Math.abs(y) <= n)],
  ['方形', (n) => cellsWhere((x, y) => Math.max(Math.abs(x), Math.abs(y)) >= 1 && Math.max(Math.abs(x), Math.abs(y)) <= n)],
  ['锥形', (n) => cellsWhere((x, y) => x >= 1 && x <= n && Math.abs(y) <= x - 1)],
  ['环形', (n) => cellsWhere((x, y) => Math.abs(x) + Math.abs(y) === n)],
];

function cellsWhere(predicate) {
  const result = new Set();
  for (let y = -5; y <= 5; y += 1) {
    for (let x = -5; x <= 5; x += 1) {
      if (predicate(x, y)) result.add(`${x},${y}`);
    }
  }
  return result;
}

const width = 1900;
const height = 1280;
const margin = 36;
const titleHeight = 96;
const headerHeight = 76;
const rowHeight = 196;
const labelWidth = 118;
const columnWidth = (width - margin * 2 - labelWidth) / shapes.length;
const gridCell = 14;
const gridSize = gridCell * 11;
const tableTop = margin + titleHeight;
const tableBottom = tableTop + headerHeight + rowHeight * 5;

const esc = (value) => String(value).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const parts = [];
parts.push(`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">`);
parts.push(`<rect width="${width}" height="${height}" fill="#efe2c4"/>`);
parts.push(`<rect x="18" y="18" width="${width - 36}" height="${height - 36}" rx="4" fill="none" stroke="#302b2a" stroke-width="3"/>`);
parts.push(`<text x="${width / 2}" y="72" text-anchor="middle" font-family="Microsoft YaHei, Noto Sans CJK SC, sans-serif" font-size="46" font-weight="700" fill="#241f1e">术式范围形状预制表</text>`);

parts.push(`<rect x="${margin}" y="${tableTop}" width="${width - margin * 2}" height="${headerHeight + rowHeight * 5}" fill="#f6edd8" stroke="#302b2a" stroke-width="3"/>`);
for (let r = 0; r <= 5; r += 1) {
  const y = tableTop + headerHeight + r * rowHeight;
  parts.push(`<line x1="${margin}" y1="${y}" x2="${width - margin}" y2="${y}" stroke="#302b2a" stroke-width="2"/>`);
}
parts.push(`<line x1="${margin + labelWidth}" y1="${tableTop}" x2="${margin + labelWidth}" y2="${tableBottom}" stroke="#302b2a" stroke-width="2"/>`);
for (let c = 1; c < shapes.length; c += 1) {
  const x = margin + labelWidth + c * columnWidth;
  parts.push(`<line x1="${x}" y1="${tableTop}" x2="${x}" y2="${tableBottom}" stroke="#302b2a" stroke-width="2"/>`);
}

parts.push(`<text x="${margin + labelWidth / 2}" y="${tableTop + 51}" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="28" font-weight="700" fill="#241f1e">距离</text>`);
for (let c = 0; c < shapes.length; c += 1) {
  const x = margin + labelWidth + c * columnWidth + columnWidth / 2;
  parts.push(`<text x="${x}" y="${tableTop + 51}" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="30" font-weight="700" fill="#241f1e">${esc(shapes[c][0])}</text>`);
}

for (let n = 1; n <= 5; n += 1) {
  const rowY = tableTop + headerHeight + (n - 1) * rowHeight;
  parts.push(`<text x="${margin + labelWidth / 2}" y="${rowY + rowHeight / 2 + 10}" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="31" font-weight="700" fill="#241f1e">${n}格</text>`);
  for (let c = 0; c < shapes.length; c += 1) {
    const active = shapes[c][1](n);
    const startX = margin + labelWidth + c * columnWidth + (columnWidth - gridSize) / 2;
    const startY = rowY + (rowHeight - gridSize) / 2;
    parts.push(`<rect x="${startX}" y="${startY}" width="${gridSize}" height="${gridSize}" fill="#263746" stroke="#1b252d" stroke-width="1.5"/>`);
    for (let gy = 0; gy < 11; gy += 1) {
      for (let gx = 0; gx < 11; gx += 1) {
        const rx = gx - 5;
        const ry = gy - 5;
        let fill = '#263746';
        if (active.has(`${rx},${ry}`)) fill = '#ef6d32';
        if (rx === 0 && ry === 0) fill = '#f3ca3f';
        parts.push(`<rect x="${startX + gx * gridCell}" y="${startY + gy * gridCell}" width="${gridCell}" height="${gridCell}" fill="${fill}" stroke="#8fa0aa" stroke-width="0.65"/>`);
      }
    }
  }
}

const legendY = tableBottom + 54;
const legend = [
  ['#f3ca3f', '锚点'],
  ['#ef6d32', '作用格'],
  ['#263746', '非作用格'],
];
let legendX = 650;
for (const [color, label] of legend) {
  parts.push(`<rect x="${legendX}" y="${legendY - 24}" width="34" height="34" fill="${color}" stroke="#302b2a" stroke-width="2"/>`);
  parts.push(`<text x="${legendX + 48}" y="${legendY + 3}" font-family="Microsoft YaHei, sans-serif" font-size="25" font-weight="600" fill="#241f1e">${label}</text>`);
  legendX += 210;
}
parts.push(`<text x="${width / 2}" y="${legendY + 55}" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="22" fill="#5b504a">默认不含锚点；直线与锥形统一朝右展示，实战按所选方向旋转。</text>`);
parts.push(`</svg>`);

fs.writeFileSync(outputPath, parts.join('\n'), 'utf8');
console.log(outputPath);
