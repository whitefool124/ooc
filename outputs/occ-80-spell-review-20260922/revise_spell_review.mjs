import fs from 'node:fs/promises';
import path from 'node:path';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const root = 'E:/数据库/OCC_Codex';
const source = path.join(root, 'outputs/occ-80-spell-review-20260922/OCC_80张术式候选清单_审核版.xlsx');
const outDir = path.join(root, 'outputs/01a0d110-3b96-72d2-8093-4145f9bafd89');
const output = path.join(outDir, 'OCC_80张术式候选清单_审核通过_待实装.xlsx');
const previewPath = path.join(outDir, 'OCC_80张术式候选清单_审核预览.png');

await fs.mkdir(outDir, { recursive: true });
const wb = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const ws = wb.worksheets.getItem('术式清单');
const table = ws.tables.getItem('SpellReviewTable');
const rows = ws.getRange('A4:X84').values;
const headers = rows[0].map(String);
const index = Object.fromEntries(headers.map((h, i) => [h, i]));
if (rows.length !== 81 || index['候选效果'] === undefined) throw new Error('清单结构异常，停止写入');

const clean = (value) => String(value ?? '')
  .replaceAll('迟缓', '敏捷负值')
  .replace(/平衡职责：[^；。]*；\s*/g, '')
  .replace(/状态：待固定种子验证；\s*/g, '')
  .replace(/状态：待固定种子验证/g, '')
  .replace(/保留现有效果；需审核其在小图中的覆盖率与可回避窗口。/g, '小图中的覆盖率与可回避窗口待战斗实测。')
  .replace(/需审核其在小图中的覆盖率与可回避窗口。/g, '小图中的覆盖率与可回避窗口待战斗实测。')
  .replace(/；\s*；/g, '；')
  .replace(/；\s*。/g, '。')
  .replace(/^；|；$/g, '')
  .trim();

const normalizeEffect = (text) => {
  let s = clean(text);
  s = s.replace(/本行动移动\+(\d+)/g, '施加行进强化+$1（持续至下次主动移动或本回合结束）');
  s = s.replace(/下次(?:相邻近战|近战命中|武器命中)(?:追加|额外造成)(\d+)点火焰伤害/g,
    '施加火焰附着+$1（下次武器命中额外造成+$1点火焰伤害，至命中或本回合结束）');
  s = s.replace(/下次武器命中伤害\+(\d+)/g,
    '施加武器蓄力+$1（下次武器命中伤害+$1，至命中或本回合结束）');
  s = s.replace(/下回合获得(\d+)行动点/g, '施加待行动点+$1（下一次自身回合开始结算）');
  s = s.replace(/下回合恢复(\d+)魔力/g, '施加待回魔+$1（下一次自身回合开始结算）');
  return s;
};

const rowSpecific = {
  'F-P-M01': { effect: '施加行进强化+2（下次主动移动最多增加2格，至移动或本回合结束）；施加火焰附着+8（下次武器命中额外造成8点火焰伤害，至命中或本回合结束）。', rule: '束缚期间不能主动移动；本次不解除束缚。' },
  'F-P-M05': { effect: '目标在本次战斗中首次主动移动后，施法者立即追移1格。', rule: '每次施放最多触发1次；追移须落在合法空格，不视为主动移动。' },
  'F-P-M11': { rule: '姿态持续至下一次自身回合开始；期间首次受到远程伤害后，获得8护盾并移除此姿态。' },
  'F-P-M12': { effect: '立即获得12护盾；下一次近战命中后额外获得4护盾。', rule: '额外护盾触发一次；至触发或本回合结束移除。' },
  'F-P-M13': { rule: '姿态持续至下一次自身回合开始；期间首次受到相邻攻击后，对攻击者造成12点武器伤害并移除此姿态。' },
  'F-P-M24': { rule: '姿态持续至下次突进或本回合结束；下次突进后向后移动1格，落点须合法。' },
  'F-P-M26': { rule: '姿态持续至下一次自身回合开始；期间首次受到相邻攻击后，对攻击者造成12点武器伤害并移除此姿态。' },
  'F-P-R34': { effect: '范围内敌方单位施加燃烧8，持续1回合；友方单位受到8点火焰伤害，不施加燃烧。' },
  'F-P-U09': { effect: '清除自身敏捷负值，并恢复本次行动的基础移动次数。', rule: '仅当自身敏捷为负时可施放；不清除束缚。' },
  'F-P-U28': { effect: '选择清除自身束缚，或清除自身敏捷负值。', rule: '自身没有束缚且敏捷不为负时不可施放；一次只清除一种。' },
};

const statuses = [];
const reviewNotes = [];
for (let r = 1; r < rows.length; r++) {
  const values = rows[r];
  const id = String(values[index['技能ID']]);
  for (const key of ['目标与范围', '候选效果', '结算规则', '地形/单位交互', '发动方式', '生效时点', '设计风险/约束']) {
    values[index[key]] = clean(values[index[key]]);
  }
  values[index['候选效果']] = normalizeEffect(values[index['候选效果']]);
  if (rowSpecific[id]) {
    for (const [key, value] of Object.entries(rowSpecific[id])) values[index[key]] = value;
  }
  values[index['审核结论']] = '通过';
  const risk = String(values[index['设计风险/约束']] ?? '');
  const riskText = risk.replace(/[。；;\s]+$/g, '');
  values[index['审核意见']] = `通过：效果与适用范围已审；${riskText ? `约束：${riskText}` : '无额外约束'}；数值与战斗节奏待实测。`;
  statuses.push([id, String(values[index['名称']]), String(values[index['候选效果']]), String(values[index['结算规则']])]);
  reviewNotes.push(String(values[index['审核意见']]));
}

// Keep the existing formatted table and extend it with a dedicated implementation-status column.
table.delete();
ws.getRange('A4:X84').values = rows;
ws.getRange('A2').values = [['80张候选术式均已完成策划审核并通过；所有条目待实装。数值与战斗节奏待实测；尚未写入正式技能配置或Unity。']];
ws.getRange('Y4:Y84').values = [['实装状态'], ...statuses.map(() => ['待实装'])];
ws.getRange('Y4:Y4').format = ws.getRange('X4:X4').format;
ws.getRange('Y5:Y84').format = ws.getRange('X5:X84').format;
ws.getRange('Y:Y').format.columnWidth = 12;
ws.tables.add('A4:Y84', true, 'SpellReviewTable');
const extendedTable = ws.tables.getItem('SpellReviewTable');
extendedTable.style = 'TableStyleMedium2';
extendedTable.showBandedRows = true;
ws.getRange('W5:W84').dataValidation = { rule: { type: 'list', values: ['待定', '通过', '修改', '删除'] } };

const staleTerms = [];
for (const row of statuses) {
  if (/迟缓|待固定种子验证|平衡职责/.test(row.join(' '))) staleTerms.push(row[0]);
}
if (staleTerms.length) throw new Error(`仍有旧口径未清理：${staleTerms.join(', ')}`);
const approvalCount = rows.slice(1).filter(row => row[index['审核结论']] === '通过').length;
if (approvalCount !== 80 || statuses.length !== 80) throw new Error('80项审核状态校验失败');

const xlsx = await SpreadsheetFile.exportXlsx(wb);
await xlsx.save(output);
const preview = await wb.render({ sheetName: '术式清单', range: 'A1:Y14', scale: 0.8, format: 'png' });
await fs.writeFile(previewPath, new Uint8Array(await preview.arrayBuffer()));
const verification = await wb.inspect({ kind: 'region', sheetId: '术式清单', range: 'W4:Y12', maxChars: 4000, tableMaxRows: 10, tableMaxCols: 3 });
console.log(JSON.stringify({ output, previewPath, rows: statuses.length, approvals: approvalCount, implementationStatuses: 80, staleTerms, verification: verification.ndjson }));
