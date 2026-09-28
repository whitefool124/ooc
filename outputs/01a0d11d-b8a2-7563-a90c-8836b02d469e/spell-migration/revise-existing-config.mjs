import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile, Workbook } from '@oai/artifact-tool';

const approvedPath = 'E:/数据库/OCC_Codex/outputs/01a0d110-3b96-72d2-8093-4145f9bafd89/OCC_80张术式候选清单_审核通过_待实装.xlsx';
const configPath = 'E:/数据库/OCC_Codex/Worldbuilding/数据表/OCC_技能配置表_v1.0.csv';
const approved = await SpreadsheetFile.importXlsx(await FileBlob.load(approvedPath));
const reviewed = approved.worksheets.getItemAt(0).getRange('A1:Y84').values;
const current = await Workbook.fromCSV(await fs.readFile(configPath, 'utf8'), { sheetName: '技能配置' });
const data = current.worksheets.getItemAt(0).getUsedRange().values;
const columns = Object.fromEntries(data[0].map((name, index) => [name, index]));
const ids = new Set(['F-P-R06']);
for (const row of data.slice(1).filter(row => ids.has(row[columns['技能ID']]))) {
  const source = reviewed.find(item => item[1] === row[columns['技能ID']]);
  if (!source || source[22] !== '通过') throw new Error('未找到审核通过的术式');
  row[columns['名称']] = source[2];
  row[columns['卡面文案']] = source[16];
  row[columns['目标与范围']] = source[15];
  row[columns['效果链']] = source[16];
  row[columns['结算规则']] = source[17] + '；' + source[21] + '；已接入审核稿结算，待正常流程实机验收';
}
const csv = data.map(row => row.map(value => '"' + String(value ?? '').replaceAll('"', '""') + '"').join(',')).join('\r\n') + '\r\n';
await fs.writeFile(configPath, csv, 'utf8');
console.log('已同步审核稿效果', [...ids].join(','));
