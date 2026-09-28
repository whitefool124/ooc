import fs from 'node:fs/promises';
import { Workbook } from '@oai/artifact-tool';

const configPath = 'E:/数据库/OCC_Codex/Worldbuilding/数据表/OCC_技能配置表_v1.0.csv';
const backupPath = 'E:/数据库/OCC_Codex/outputs/01a0d11d-b8a2-7563-a90c-8836b02d469e/spell-migration/before.csv';
const current = await Workbook.fromCSV(await fs.readFile(configPath, 'utf8'), { sheetName: '技能配置' });
const backup = await Workbook.fromCSV(await fs.readFile(backupPath, 'utf8'), { sheetName: '原配置' });
const rows = current.worksheets.getItemAt(0).getUsedRange().values;
const oldRows = backup.worksheets.getItemAt(0).getUsedRange().values;
const idColumn = rows[0].indexOf('技能ID');
const id = 'F-P-M03';
const index = rows.findIndex(row => row[idColumn] === id);
const original = oldRows.find(row => row[idColumn] === id);
if (index < 0 || !original || rows[index].length !== original.length) throw new Error('冲突条目无法安全还原');
rows[index] = original;
const csv = rows.map(row => row.map(value => '"' + String(value ?? '').replaceAll('"', '""') + '"').join(',')).join('\r\n') + '\r\n';
await fs.writeFile(configPath, csv, 'utf8');
console.log('已还原冲突术式', id);
