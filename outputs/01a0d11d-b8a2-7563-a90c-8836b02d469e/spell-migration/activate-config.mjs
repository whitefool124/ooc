import fs from 'node:fs/promises';
import { Workbook } from '@oai/artifact-tool';

const path = 'E:/数据库/OCC_Codex/Worldbuilding/数据表/OCC_技能配置表_v1.0.csv';
const ids = new Set(['F-P-M26']);
const workbook = await Workbook.fromCSV(await fs.readFile(path, 'utf8'), { sheetName: '技能配置' });
const data = workbook.worksheets.getItemAt(0).getUsedRange().values;
const header = data[0];
const idColumn = header.indexOf('技能ID');
const enabledColumn = header.indexOf('启用');
const ruleColumn = header.indexOf('结算规则');
if ([idColumn, enabledColumn, ruleColumn].some(value => value < 0)) throw new Error('技能表列结构变更');
const matched = data.slice(1).filter(row => ids.has(row[idColumn]));
if (matched.length !== ids.size || matched.some(row => row[enabledColumn] !== '否' && row[enabledColumn] !== '是'))
  throw new Error('待启用术式集合与预期不符');
for (const row of matched) {
  row[enabledColumn] = '是';
  row[ruleColumn] = String(row[ruleColumn]).replace('审核通过，待战斗实装与实测', '审核通过，已接入战斗结算，待正常流程实机验收');
}
const csv = data.map(row => row.map(value => '"' + String(value ?? '').replaceAll('"', '""') + '"').join(',')).join('\r\n') + '\r\n';
await fs.writeFile(path, csv, 'utf8');
console.log('已启用', [...ids].join(','));
