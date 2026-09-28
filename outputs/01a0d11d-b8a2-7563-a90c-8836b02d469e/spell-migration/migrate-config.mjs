import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile, Workbook } from '@oai/artifact-tool';

const approvedPath = 'E:/数据库/OCC_Codex/outputs/01a0d110-3b96-72d2-8093-4145f9bafd89/OCC_80张术式候选清单_审核通过_待实装.xlsx';
const configPath = 'E:/数据库/OCC_Codex/Worldbuilding/数据表/OCC_技能配置表_v1.0.csv';
const approved = await SpreadsheetFile.importXlsx(await FileBlob.load(approvedPath));
const reviewed = approved.worksheets.getItemAt(0).getRange('A1:Y84').values
  .filter(row => typeof row[0] === 'number');
if (reviewed.length !== 80 || reviewed.some(row => row[22] !== '通过'))
  throw new Error('审核工作簿不是 80 张全部通过的版本');

const currentText = await fs.readFile(configPath, 'utf8');
const current = await Workbook.fromCSV(currentText, { sheetName: '技能配置' });
const data = current.worksheets.getItemAt(0).getUsedRange().values;
const header = data[0];
const index = Object.fromEntries(header.map((name, column) => [name, column]));
const idColumn = index['技能ID'];
const existingIds = new Set(data.slice(1).map(row => row[idColumn]));
if (reviewed.filter(row => existingIds.has(row[1])).length !== 60)
  throw new Error('正式技能表的 60 张基线与审核清单不匹配');

const additions = reviewed.filter(row => !existingIds.has(row[1]));
if (additions.length !== 20 || additions.some(row => row[4] !== '新增'))
  throw new Error('新增术式与审核工作簿不一致');

function set(row, name, value) { row[index[name]] = value == null ? '' : String(value); }
function newRecord(review) {
  const row = Array(header.length).fill('');
  const id = review[1];
  set(row, '技能ID', id);
  set(row, '名称', review[2]);
  set(row, '卡面文案', review[16]);
  set(row, '类别', review[6]);
  set(row, '来源', '火系术式:' + id);
  set(row, 'AP', review[10]);
  set(row, '魔力', review[11]);
  set(row, '生命', review[12]);
  set(row, '冷却', review[13]);
  set(row, '次数', review[14]);
  set(row, '目标与范围', review[15]);
  set(row, '效果链', review[16]);
  set(row, '结算规则', review[17] + '；' + review[21] + '；审核通过，待战斗实装与实测');
  set(row, '启用', '否');
  set(row, '版本', '1');
  set(row, '主构筑', String(review[7]).replace(/核心$/, ''));
  set(row, '职责', review[9]);
  return row;
}

const rows = data.slice(1);
for (const group of ['M', 'R', 'U']) {
  const after = rows.findIndex(row => row[idColumn] === `F-P-${group}20`);
  if (after < 0) throw new Error(`缺少 F-P-${group}20 插入点`);
  rows.splice(after + 1, 0, ...additions.filter(row => row[1].startsWith(`F-P-${group}`)).map(newRecord));
}
const out = [header, ...rows].map(row => row.map(value => '"' + String(value ?? '').replaceAll('"', '""') + '"').join(',')).join('\r\n') + '\r\n';
await fs.writeFile(configPath, out, 'utf8');
console.log(`正式技能配置表已增补 ${additions.length} 张审核通过但未启用的术式。`);
