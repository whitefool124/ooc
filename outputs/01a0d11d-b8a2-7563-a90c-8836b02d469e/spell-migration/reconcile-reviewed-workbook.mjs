import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const source = 'E:/数据库/OCC_Codex/outputs/01a0d110-3b96-72d2-8093-4145f9bafd89/OCC_80张术式候选清单_审核通过_待实装.xlsx';
const outputDir = 'E:/数据库/OCC_Codex/outputs/01a0d11d-b8a2-7563-a90c-8836b02d469e';
const destination = `${outputDir}/OCC_80张术式审核清单_统一火场与教程裁定.xlsx`;
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const sheet = workbook.worksheets.getItemAt(0);
const rows = sheet.getRange('A1:Y84').values;
const byId = new Map(rows.map((row, index) => [row[1], index + 1]));
for (const id of ['F-P-M03', 'F-P-R13', 'F-P-R20']) {
  if (!byId.has(id) || rows[byId.get(id) - 1][22] !== '通过') throw new Error(`审核条目缺失：${id}`);
}
if (process.argv[2] === 'preview') {
  const preview = await workbook.render({ sheetName: '术式清单', range: 'B4:R7', scale: 1, format: 'png' });
  await fs.writeFile(`${outputDir}/spell-source-before.png`, new Uint8Array(await preview.arrayBuffer()));
  console.log('source preview ready');
  process.exit(0);
}
const set = (id, column, value) => { sheet.getCell(byId.get(id) - 1, column).values = [[value]]; };
set('F-P-M03', 4, '教程版保留');
set('F-P-M03', 16, '沿四向主轴突进最多3格；起点生成8点基础伤害、持续2公共回合的火场；实际路径正交邻接的敌方单位与可破坏物各受到8点武器伤害，每个目标每次至多结算1次。');
set('F-P-M03', 17, '终点必须可站立且与敌人相邻；不可穿单位、重掩体或永久墙；起点火场遵守统一8点基础伤害。');
set('F-P-M03', 18, '固定教程借助起点火场、灯藤与突进路径建立可见反制；不新增隐藏地格。');
set('F-P-M03', 21, '保留总案第6章固定教程效果；路径、命中对象、起点火场与终点必须完整预览。');
set('F-P-M03', 23, '通过：依据总案第6章及用户裁定保留教程效果；数值与节奏待实测。');
set('F-P-R13', 16, '在可站立空格生成1格每次造成8点基础伤害、持续4公共回合的火场；其正交相邻可破坏物获得裂痕。');
set('F-P-R13', 23, '通过：火场统一8点基础伤害；相邻物件裂痕待实装与实测。');
set('F-P-R20', 16, '中心及正交相邻格内单位与可破坏物各受到20点火焰伤害；结算后在可站立空格生成每次8点基础伤害、持续3公共回合的火场；施法者行动条+8。');
set('F-P-R20', 23, '通过：火场统一8点基础伤害；范围伤害与火场待实装验收。');
sheet.getCell(1, 0).values = [['80张候选术式均已完成策划审核；焦痕跃进保留教程效果，火场统一8点基础伤害。数值与战斗节奏仍需实测，实装状态以正式配置和运行时为准。']];
const result = await SpreadsheetFile.exportXlsx(workbook);
await result.save(destination);
const check = await SpreadsheetFile.importXlsx(await FileBlob.load(destination));
const out = check.worksheets.getItemAt(0).getRange('A1:Y84').values;
if (out.length !== 84 || out.filter(row => typeof row[0] === 'number').length !== 80 ||
    !out[6][16].includes('起点生成8点基础伤害') || !out[42][16].includes('每次造成8点基础伤害') ||
    !out[49][16].includes('每次8点基础伤害')) throw new Error('修订后工作簿回读不符');
console.log((await check.inspect({ kind: 'region', sheetId: '术式清单', range: 'B5:R7', maxChars: 1100 })).ndjson);
console.log(destination);
