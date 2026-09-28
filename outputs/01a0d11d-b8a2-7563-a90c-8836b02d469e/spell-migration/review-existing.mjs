import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const path = 'E:/数据库/OCC_Codex/outputs/01a0d110-3b96-72d2-8093-4145f9bafd89/OCC_80张术式候选清单_审核通过_待实装.xlsx';
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(path));
for (const row of workbook.worksheets.getItemAt(0).getRange('A1:Y84').values) {
  if (typeof row[0] !== 'number' || row[4] === '新增' || !row[4]) continue;
  console.log(JSON.stringify({id:row[1], name:row[2], change:row[4], ap:row[10], mana:row[11], health:row[12], cooldown:row[13], target:row[15], copy:row[16], rule:row[17], note:row[21]}));
}
