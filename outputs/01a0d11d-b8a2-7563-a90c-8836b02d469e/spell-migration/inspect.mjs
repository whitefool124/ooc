import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const source = 'E:/数据库/OCC_Codex/outputs/01a0d110-3b96-72d2-8093-4145f9bafd89/OCC_80张术式候选清单_审核通过_待实装.xlsx';
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
console.log((await workbook.inspect({ kind: 'sheet', include: 'id,name', maxChars: 2000 })).ndjson);
console.log(workbook.help('SpreadsheetFile.exportCsv', { include: 'index,examples,notes', maxChars: 3000 }).ndjson);
console.log(workbook.help('Workbook.toCSV', { include: 'index,examples,notes', maxChars: 3000 }).ndjson);
