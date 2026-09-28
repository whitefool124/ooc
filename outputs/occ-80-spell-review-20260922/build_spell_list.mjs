import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const root = "E:/数据库/OCC_Codex";
const outDir = path.join(root, "outputs/occ-80-spell-review-20260922");
const spellCsv = path.join(root, "Worldbuilding/数据表/OCC_技能配置表_v1.0.csv");
const rarityCsv = path.join(root, "Worldbuilding/数据表/OCC_术式稀有度表_v1.0.csv");
const outFile = path.join(outDir, "OCC_80张术式候选清单_审核版.xlsx");
const previewFile = path.join(outDir, "术式清单_预览.png");

function parseCsv(text) {
  const rows = [];
  let row = [], field = "", quoted = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (quoted) {
      if (c === '"' && text[i + 1] === '"') { field += '"'; i++; }
      else if (c === '"') quoted = false;
      else field += c;
    } else if (c === '"') quoted = true;
    else if (c === ',') { row.push(field); field = ""; }
    else if (c === '\n') { row.push(field.replace(/\r$/, "")); rows.push(row); row = []; field = ""; }
    else field += c;
  }
  if (field.length || row.length) { row.push(field.replace(/\r$/, "")); rows.push(row); }
  const headers = rows.shift();
  return rows.filter(r => r.some(v => v !== "")).map(r => Object.fromEntries(headers.map((h, i) => [h, r[i] ?? ""])));
}

const poolMap = {
  "F-P-M01":"DC","F-P-M02":"DC","F-P-M03":"DC","F-P-M04":"DC","F-P-M05":"DC",
  "F-P-M06":"BC","F-P-M07":"DC","F-P-M08":"BC","F-P-M09":"BC","F-P-M10":"UT",
  "F-P-M11":"UT","F-P-M12":"UT","F-P-M13":"UT","F-P-M14":"UT","F-P-M15":"DC",
  "F-P-M16":"FC","F-P-M17":"FC","F-P-M18":"DF","F-P-M19":"DC","F-P-M20":"BF",
  "F-P-R01":"FC","F-P-R02":"FC","F-P-R03":"FC","F-P-R04":"FC","F-P-R05":"FC",
  "F-P-R06":"BC","F-P-R07":"BC","F-P-R08":"FC","F-P-R09":"FC","F-P-R10":"BC",
  "F-P-R11":"FC","F-P-R12":"FC","F-P-R13":"BF","F-P-R14":"FC","F-P-R15":"FC",
  "F-P-R16":"FC","F-P-R17":"BF","F-P-R18":"FC","F-P-R19":"BC","F-P-R20":"BF",
  "F-P-U01":"DC","F-P-U02":"FC","F-P-U03":"BC","F-P-U04":"BC","F-P-U05":"BC",
  "F-P-U06":"DC","F-P-U07":"UT","F-P-U08":"BF","F-P-U09":"DC","F-P-U10":"DB",
  "F-P-U11":"FC","F-P-U12":"DF","F-P-U13":"DF","F-P-U14":"DF","F-P-U15":"DF",
  "F-P-U16":"DC","F-P-U17":"FC","F-P-U18":"DB","F-P-U19":"DF","F-P-U20":"BF"
};

const poolInfo = {
  DC:["突进穿刺核心","—"], BC:["地块破坏回流核心","—"], FC:["燃烧火场核心","—"],
  DB:["突进穿刺","地块破坏回流"], DF:["突进穿刺","燃烧火场"], BF:["地块破坏回流","燃烧火场"],
  UT:["通用战术","—"]
};

const overrides = {
  "F-P-M03": { change:"改写", target:"空格路径；直线射程3", effect:"沿四向主轴突进最多3格；实际路径正交邻接的单位与可破坏物各受到8点伤害，每个目标每次至多结算1次；只留下视觉焦痕，不生成燃烧地格。", rule:"终点必须可站立；不可穿单位、重掩体或永久墙；单位与物件同值。", interaction:"突进经过窄路可擦伤堵路者与轻掩体；不挤占火场构筑的持续地格份额。", risk:"小图中最多命中3个路径侧目标；路径、命中对象与终点必须完整预览。" },
  "F-P-M06": { change:"改写", effect:"附着：下次近战命中单位时施加破势；命中可破坏物时改为施加裂痕（其下次受到伤害+8后移除）。", interaction:"同一张牌兼容攻坚单位与拆除物件，不要求地图必有物件。", risk:"裂痕仅影响一次伤害，不触发自身连锁。" },
  "F-P-M07": { change:"修正口径", target:"相邻敌单体；射程1", effect:"造成20点武器伤害＋8点火焰伤害并施加破势；施法者行动条+4。", rule:"不要求目标燃烧，也不消费燃烧；同次命中合并为一个伤害包。", interaction:"作为突进后的稳定攻坚终点，不绑火场前置。", risk:"高伤害以2AP、5魔力、3回合冷却及行动延后支付。" },
  "F-P-M08": { change:"改写", effect:"前方3格锥形内单位与可破坏物各受到12点武器伤害＋4点火焰伤害；会伤及友军。", interaction:"可同时清理堵路单位、轻掩体与脆弱机关。", risk:"重掩体逐射线截断；范围内友军和宝箱同样可能受损。" },
  "F-P-M09": { change:"改归属", interaction:"推动单位撞向物块时产生碰撞；也可把敌人推出交叉攻击覆盖。", risk:"非法落点只取消位移；撞可破坏物时双方各受4点碰撞伤害。" },
  "F-P-M13": { change:"保留", interaction:"反击作为被动效果：敌人在相邻范围主动攻击后即时触发。", risk:"每次姿态只触发1次；被动反击不触发另一被动反击。" },
  "F-P-M18": { change:"改写", target:"空格路径；普通路径2格／连续火场路径3格", effect:"移动至合法空格；若全程沿连续燃烧地格移动，射程提高至3且不触发该火场伤害。", interaction:"无火场时仍是短位移；有火场时成为进攻走廊。", risk:"不可穿单位、重掩体或永久墙；不能借敌方火场免疫其他伤害。" },
  "F-P-M20": { change:"改写", target:"相邻燃烧敌，或与裂痕物件相邻的敌人；射程1", effect:"造成28点武器伤害；若目标燃烧，追加12点火焰伤害并消费燃烧；若目标与裂痕物件相邻，该物件同时受到20点结构伤害并移除裂痕。", interaction:"可用火场终结单位，也可借结构破口同时拆障。", risk:"两个条件同时满足也不再追加第三段单位伤害；会误伤宝箱。" },
  "F-P-R06": { change:"改写", effect:"直线4格内单位与可破坏物各受到8点火焰伤害；首个重掩体截断后续格。", interaction:"能烧穿散页、页幕与轻掩体，同时形成单位压线。", risk:"伤及友军和宝箱；每个对象每次只结算1次。" },
  "F-P-R07": { change:"改写", effect:"前方3格锥形内单位与可破坏物各受到8点火焰伤害；对物件额外施加裂痕。", interaction:"近距扇面同时压单位与拆除物；可点燃可燃材质但不自动生成火场。", risk:"会伤友军；重掩体逐射线截断。" },
  "F-P-R10": { change:"改写", target:"可见单位或可破坏物；射程3", effect:"对单位造成12点火焰伤害并施加破势；对物件造成12点结构伤害并施加裂痕；施法者行动条+4。", interaction:"单位攻坚与结构攻坚共用入口。", risk:"破势/裂痕不回溯增强本次伤害。" },
  "F-P-R13": { change:"改写", effect:"在可站立空格生成1格每次造成12点伤害、持续4公共回合的火场；其正交相邻可破坏物获得裂痕。", interaction:"用火钉守点，并为后续拆除创造结构弱点。", risk:"不能直接指定被占据格；浅水会覆盖火场。" },
  "F-P-R17": { change:"改写", target:"4格内燃烧地格；格上可有单位或物件", effect:"消费该格火场；格上单位受到12点火焰伤害，格上可破坏物受到16点结构伤害并获得裂痕。", interaction:"把持续封锁兑换为即时拆除或补刀。", risk:"0AP/0魔力但必须消费真实火场；不会连锁引爆相邻物件。" },
  "F-P-R20": { change:"改写", effect:"中心及正交相邻格内单位与可破坏物各受到20点火焰伤害；结算后在可站立空格生成每次12点伤害、持续3公共回合的火场；施法者行动条+8。", interaction:"同时终结密集单位、拆障并重塑路径。", risk:"会伤友军、宝箱与中立机关；重掩体截断投放线。" },
  "F-P-U03": { change:"改写", effect:"附着：下次武器命中单位时施加破势；命中可破坏物时施加裂痕。", interaction:"武器型角色可自由选择压单位或拆结构。", risk:"只强化一次命中；不直接增加伤害。" },
  "F-P-U08": { change:"改写", effect:"清除自身燃烧并获得20护盾；若自身未燃烧但本回合已有裂痕物件被摧毁，则获得16护盾；否则获得12护盾。", interaction:"火场与破坏两条资源都能转为防守。", risk:"每次只取最高一档，不叠加。" },
  "F-P-U10": { change:"改写", effect:"姿态：下一次突进落点若与可破坏物相邻，立即获得8护盾；若该物件在本回合被摧毁，可再向其空出的格移动1格。", interaction:"把拆障产生的空位转化为突进续步。", risk:"续步不是新行动，不触发第二次攻击；宝箱被摧毁仍会销毁内容。" },
  "F-P-U12": { change:"改写", effect:"附着：下一次完成突进后的武器命中使自身获得8护盾；若目标处于燃烧，再消费燃烧并额外获得4护盾。", interaction:"突进提供基础收益，燃烧提供加成。", risk:"本行动结束未触发则失效；只触发一次。" },
  "F-P-U13": { change:"改写", effect:"附着：本回合完成突进后，下一次武器命中恢复2魔力；若目标燃烧，再恢复1魔力且保留燃烧。", interaction:"突进负责启动，燃烧提高回收效率。", risk:"每次最多恢复3魔力；不超过12魔力上限。" },
  "F-P-U14": { change:"改写", effect:"附着：本回合完成突进后，下一次攻击追加8点火焰伤害；目标燃烧时改为追加12点。", interaction:"没有火场也可用，有燃烧时达到原有上限。", risk:"只强化一次攻击；被动反击不能消耗此附着。" },
  "F-P-U15": { change:"改写", effect:"附着：本回合完成突进后的下一次命中施加燃烧8持续1回合；目标已燃烧则追加4点火焰伤害并延长至至少2回合。", interaction:"突进角色可自行建立或维持燃烧。", risk:"不提高燃烧强度；只触发一次。" },
  "F-P-U18": { change:"改归属", interaction:"落点冲击单位与物件，是突进和拆障的天然桥接牌。", risk:"范围会伤友军；推动失败不取消伤害。" },
  "F-P-U19": { change:"修正口径", target:"4格内可见敌单体", effect:"标记目标：友军对其下一次武器命中后将其推1格；若施法者本回合完成过突进，该次命中追加8点火焰伤害；若目标已燃烧，则其离开的原格生成8伤害、持续2回合的火场。", rule:"每个标记最多触发1次；推动先结算，随后在原格生成火场；被动反击不可触发。", interaction:"协同攻击可把敌人推入交叉火力，同时用燃烧留下封路格。", risk:"目标无法推动时仍可结算伤害，但不生成“离开原格”火场。" },
  "F-P-U20": { change:"改写", effect:"附着：下一次武器命中若目标燃烧或与本回合被摧毁物件的原格相邻，追加20点火焰伤害；两个条件同时满足时追加28点并清除燃烧；施法者行动条+8。", interaction:"火场终结与拆障开口共享同一终结牌。", risk:"须保留武器攻击AP；被动反击不可消耗。" }
};

const newCards = [
  ["F-P-M21","折线突步","普通","个人术式-M近战","DC","稳定骨架",1,3,0,"2自身回合","无限","空格路径；总长3，允许一次直角转向","沿可通行路径移动最多3格，路径可有一次直角转向；不造成伤害。","逐格检查；不可穿单位、重掩体或永久墙。","小图内绕开直线堵路与单一警戒线。","路径移动","即时","完整显示折线路径与每格敌方反应范围。"],
  ["F-P-M22","越障跃步","罕见","个人术式-M近战","DC","空间转化",1,4,0,"2自身回合","无限","2格内可站立空格","跳跃至目标格；可越过恰好1个单位或轻掩体，但不可越过重掩体与永久墙。","只检查起点、越过格与落点；不触发越过格的进入效果。","针对堵路但不否定重掩体价值。","跳跃移动","即时","落点仍会触发进入效果和公开反应。"],
  ["F-P-M23","擦锋穿行","罕见","个人术式-M近战","DC","空间转化",2,4,0,"2自身回合","无限","直线空格路径；射程3","直线突进最多3格；实际路径正交邻接的敌人各受到8点武器伤害。","每个敌人每次至多受伤1次；不伤友军和物件。","在狭窄队列侧面穿行，反制密集堵路。","突进攻击","即时","路径相邻伤害不能绕过永久墙。"],
  ["F-P-M24","余势回身","普通","个人术式-M近战","DC","独立收益",1,2,0,"1自身回合","无限","自身附着","下一次突进完整结算后，可沿实际路径向起点方向回退1格。","回退格须合法；不重新触发路径伤害。","让突进可试探反击区后撤出。","附着触发","下次突进后","回退仍属于移动，会触发落点进入效果。"],
  ["F-P-M25","侧压落步","普通","个人术式-M近战","DC","稳定骨架",1,3,0,"2自身回合","无限","直线空格路径；射程2","直线突进最多2格；落点正交相邻单位向远离落点方向推1格，不造成伤害。","敌我同样推动；非法落点只取消对应位移。","拆散交叉覆盖或把堵路者推出关键格。","突进控制","即时","四向同时结算，先预览每个单位的落点。"],
  ["F-P-M26","迎锋架势","稀有","个人术式-M近战","DC","构筑终结",1,3,0,"3自身回合","无限","自身姿态","下一次完成突进后获得被动反击：直到下次自身回合开始，首次受到相邻敌人的主动攻击后，对攻击者造成12点武器伤害。","反击在攻击完整结算后触发；姿态触发1次。","突进压入敌阵后建立近身威慑。","姿态/被动反击","受击后即时","被动反击不触发另一被动反击，也不消耗攻击附着。"],
  ["F-P-R21","裂痕投射","普通","个人术式-R远程","BC","稳定骨架",1,3,0,"1自身回合","无限","直线4格；首个单位或物件","首个目标受到6点伤害；若为物件则施加裂痕，若为单位则推1格。","重掩体截断；裂痕使物件下次受伤+8后移除。","无物件时仍可作为低伤推动。","投射攻击","即时","推动不得穿单位或物块。"],
  ["F-P-R22","楔形震裂","罕见","个人术式-R远程","BC","空间转化",2,4,0,"2自身回合","无限","前方2格锥形","范围内单位与可破坏物各受到8点伤害；对物件视为破障，伤害翻倍。","敌我同值；重掩体逐射线截断。","短距扇面清障并压缩单位站位。","范围攻击","即时","会摧毁宝箱并销毁未领取内容。"],
  ["F-P-R23","崩面震波","罕见","个人术式-R远程","BC","空间转化",2,4,0,"2自身回合","无限","3格内目标格；中心与正交相邻","范围内单位与可破坏物各受到6点伤害；若有物件被摧毁，相邻单位向外推1格。","先统一伤害，再按被摧毁物件逐一处理推动；同一单位最多推1次。","拆障会直接改变单位阵形。","延后位移","伤害后即时","必须预览哪些物件会被摧毁及最终推位。"],
  ["F-P-R24","回流护持","普通","个人术式-R远程","BC","独立收益",1,3,0,"2自身回合","无限","自身附着","下一次裂痕物件被己方摧毁时，在标准摧毁回流之外获得4护盾。","持续至下次自身回合开始；每次只触发1次。","把拆障转换为前排续航。","附着触发","物件摧毁后","不因环境自行摧毁而触发。"],
  ["F-P-R25","借障折射","罕见","个人术式-R远程","BC","空间转化",1,3,0,"1自身回合","无限","3格内与重掩体正交相邻的可见空格；折射线2格","以该重掩体为折射点，沿另一侧直线2格；首个单位或物件受到10点伤害。","不能选择永久墙；折射路径仍受第二个重掩体截断。","把固定掩体转化为侧射角度。","折射攻击","即时","目标格与折射方向必须在施放前完整显示。"],
  ["F-P-R26","断线爆破","稀有","个人术式-R远程","BC","构筑终结",2,5,0,"3自身回合","无限","4格内可见目标格；中心与正交相邻","放置公开爆破标记；本回合结束时范围内单位与可破坏物各受到12点伤害，物件视为破障。","标记持续可见；施法者倒下也照常结算。","迫使敌人离开掩体簇或承受结构坍塌。","蓄时标记","本回合结束","单位可主动撤离；宝箱在爆区内会被摧毁。"],
  ["F-P-U21","结构测绘","普通","个人术式-U通用","BC","稳定骨架",1,2,0,"2自身回合","无限","直线4格；首个单位或物件","不造成伤害；若首个目标为物件则施加裂痕，若为单位则施加破势。","重掩体截断；状态持续至被消耗或目标下次行动结束。","保证无物件地图仍有合法用途。","状态施加","即时","不能穿过前方单位去标记后方物件。"],
  ["F-P-U22","余料筑障","罕见","个人术式-U通用","BC","独立收益",1,3,0,"2自身回合","无限","本回合被己方摧毁物件的原格，或其相邻空格；射程3","在合法空格生成8耐久轻掩体，持续至战斗结束或被摧毁。","每个被摧毁物件只可作为1次材料来源；不能建在宝箱原格。","拆除旧堵点后重建己方掩体。","条件造物","即时","不得封死唯一任务路径；生成前预览新的通行与视线。"],
  ["F-P-U23","破口突入","普通","个人术式-U通用","DB","稳定骨架",2,4,0,"2自身回合","无限","直线3格；终点前方首个单位或物件","突进至目标前一格并对其造成12点武器伤害；若目标为物件且被摧毁，则继续进入其空出的格。","继续移动不是新行动；若空出格仍非法则停在原落点。","用拆障直接制造并占领突破口。","突进攻击","伤害后续步","路径和可能的二段落点都必须预览。"],
  ["F-P-U24","裂路穿刺","罕见","个人术式-U通用","DB","空间转化",1,3,0,"2自身回合","无限","本回合被摧毁物件原格串联的路径；总长3","沿合法路径移动最多3格；路径必须经过至少1个本回合被摧毁物件的原格；路径正交邻接敌人受到6点武器伤害。","每个敌人每次至多受伤1次。","把破坏留下的空洞转化为机动线路。","条件突进","即时","没有当回合破口时不可施放。"],
  ["F-P-U25","震楔落步","稀有","个人术式-U通用","DB","构筑终结",2,4,0,"3自身回合","无限","直线空格路径2格；落点十字","突进最多2格；落点十字内单位与可破坏物各受到8点伤害，物件视为破障；单位向外推1格。","先伤害、再摧毁、后推位。","一次完成进场、拆障和阵形打散。","突进范围攻击","即时","伤友军；被摧毁宝箱内容丢失。"],
  ["F-P-U26","碎障回身","普通","个人术式-U通用","DB","独立收益",1,3,0,"2自身回合","无限","自身附着","下一次由自身摧毁可破坏物后，可立即移动至其空出的格。","空出格必须可站立且距自身不超过3；每次只触发1次。","让远近程破坏都能转换为占位。","附着触发","物件摧毁后","移动会触发进入效果与敌方公开反应。"],
  ["F-P-U27","缓冲护幕","普通","个人术式-U通用","UT","独立收益",1,3,0,"2自身回合","无限","自身单体","获得8护盾；下次在自身下回合前受到的强制位移距离减少1格。","最低减至0；不影响主动移动。","对抗推动、拉动与撞击链。","主动防御","即时/下次受推时","只减少一次强制位移。"],
  ["F-P-U28","断势调息","普通","个人术式-U通用","UT","稳定骨架",1,2,0,"2自身回合","无限","自身单体","选择清除束缚或迟缓之一。","自身没有这两种状态时不可施放。","提供不依赖流派的基础反控制。","状态清除","即时","不能同时清除两种状态。"]
];

const spells = parseCsv(await fs.readFile(spellCsv, "utf8")).filter(r => /^F-P-[MRU]\d{2}$/.test(r.技能ID));
const rarityById = Object.fromEntries(parseCsv(await fs.readFile(rarityCsv, "utf8")).map(r => [r.技能ID, r.稀有度]));

function timing(effect) {
  if (/本回合结束/.test(effect)) return ["蓄时/公开标记","本回合结束"];
  if (/下次|首次|姿态|附着/.test(effect)) return ["主动设置状态","满足条件时"];
  if (!/不生成/.test(effect) && /生成.*(?:火场|燃烧地格)/.test(effect)) return ["主动施放","即时生成，后续进格/回合开始触发"];
  return ["主动施放","即时"];
}

const existingRows = spells.map((s) => {
  const code = poolMap[s.技能ID];
  const ov = overrides[s.技能ID] ?? {};
  const effect = ov.effect ?? s.效果链;
  const [castTime, resolveTime] = timing(effect);
  return {
    id:s.技能ID, name:s.名称, source:"现有60张", change:ov.change ?? "保留", rarity:rarityById[s.技能ID] ?? "普通",
    type:s.类别, main:poolInfo[code][0], secondary:poolInfo[code][1], role:s.职责 || "待归类",
    ap:Number(s.AP)||0, mana:Number(s.魔力)||0, hp:Number(s.生命)||0, cooldown:s.冷却, uses:s.次数,
    target:ov.target ?? s.目标与范围, effect, rule:ov.rule ?? s.结算规则,
    interaction:ov.interaction ?? "按目标、范围与结算规则参与单位/地形交互。", castTime, resolveTime,
    risk:ov.risk ?? "保留现有效果；需审核其在小图中的覆盖率与可回避窗口。"
  };
});

const addedRows = newCards.map(c => {
  const [id,name,rarity,type,code,role,ap,mana,hp,cooldown,uses,target,effect,rule,interaction,castTime,resolveTime,risk] = c;
  return {id,name,source:"新增20张",change:"新增",rarity,type,main:poolInfo[code][0],secondary:poolInfo[code][1],role,ap,mana,hp,cooldown,uses,target,effect,rule,interaction,castTime,resolveTime,risk};
});

const allRows = [...existingRows, ...addedRows].sort((a,b) => a.id.localeCompare(b.id, "en"));
if (allRows.length !== 80) throw new Error(`术式数量错误：${allRows.length}`);

const headers = ["序号","技能ID","名称","来源状态","改动类型","稀有度","类别","主构筑/池","副构筑","职责","AP","魔力","生命","冷却","次数","目标与范围","候选效果","结算规则","地形/单位交互","发动方式","生效时点","设计风险/约束","审核结论","审核意见"];
const matrix = allRows.map((r,i) => [i+1,r.id,r.name,r.source,r.change,r.rarity,r.type,r.main,r.secondary,r.role,r.ap,r.mana,r.hp,r.cooldown,r.uses,r.target,r.effect,r.rule,r.interaction,r.castTime,r.resolveTime,r.risk,"待定",""]);

const wb = Workbook.create();
const ws = wb.worksheets.add("术式清单");
ws.showGridLines = false;
ws.tabColor = "#C5522F";
ws.getRange("A1:X1").merge();
ws.getRange("A1").values = [["OCC 80张术式候选清单｜审核版"]];
ws.getRange("A2:X2").merge();
ws.getRange("A2").values = [["候选结构：突进穿刺核心18｜地块破坏回流核心18｜燃烧火场核心18｜三类双流派桥接各6｜通用战术8。仅供审核，尚未写回正式数据与Unity。"]];
ws.getRange("A4:X4").values = [headers];
ws.getRange(`A5:X${4+matrix.length}`).values = matrix;

ws.getRange("A1:X1").format = { fill:"#5A2418", font:{bold:true,color:"#FFFFFF",size:18}, horizontalAlignment:"left", verticalAlignment:"center" };
ws.getRange("A1:X1").format.rowHeight = 34;
ws.getRange("A2:X2").format = { fill:"#F3E7D8", font:{color:"#5A2418",italic:true,size:10}, wrapText:true, verticalAlignment:"center" };
ws.getRange("A2:X2").format.rowHeight = 32;
ws.getRange("A4:X4").format = { fill:"#8E3E2C", font:{bold:true,color:"#FFFFFF",size:10}, horizontalAlignment:"center", verticalAlignment:"center", wrapText:true, borders:{preset:"all",style:"thin",color:"#D7B7A7"} };
ws.getRange("A4:X4").format.rowHeight = 30;
ws.getRange(`A5:X${4+matrix.length}`).format = { font:{size:9,color:"#26221F"}, verticalAlignment:"top", wrapText:true, borders:{preset:"all",style:"thin",color:"#E2D8CF"} };
ws.getRange(`A5:A${4+matrix.length}`).format.horizontalAlignment = "center";
ws.getRange(`D5:F${4+matrix.length}`).format.horizontalAlignment = "center";
ws.getRange(`K5:O${4+matrix.length}`).format.horizontalAlignment = "center";
ws.getRange(`T5:W${4+matrix.length}`).format.horizontalAlignment = "center";
ws.getRange(`A5:X${4+matrix.length}`).format.rowHeight = 62;

const widths = [6,14,13,10,10,8,17,17,16,12,6,7,6,12,8,24,44,42,34,14,18,34,10,28];
widths.forEach((w,i) => ws.getRangeByIndexes(0,i,85,1).format.columnWidth = w);
ws.freezePanes.freezeRows(4);
ws.freezePanes.freezeColumns(3);
ws.tables.add(`A4:X${4+matrix.length}`, true, "SpellReviewTable");
ws.getRange(`W5:W${4+matrix.length}`).dataValidation = { rule:{ type:"list", values:["待定","通过","修改","删除"] } };

ws.getRange(`D5:D${4+matrix.length}`).conditionalFormats.add("containsText", { text:"新增20张", format:{fill:"#E8F2E1",font:{color:"#2F6B2F",bold:true}} });
ws.getRange(`E5:E${4+matrix.length}`).conditionalFormats.add("containsText", { text:"改写", format:{fill:"#FFF1CC",font:{color:"#8A5A00",bold:true}} });
ws.getRange(`E5:E${4+matrix.length}`).conditionalFormats.add("containsText", { text:"修正口径", format:{fill:"#FADBD8",font:{color:"#922B21",bold:true}} });
ws.getRange(`W5:W${4+matrix.length}`).conditionalFormats.add("containsText", { text:"通过", format:{fill:"#DFF0D8",font:{color:"#2F6B2F",bold:true}} });
ws.getRange(`W5:W${4+matrix.length}`).conditionalFormats.add("containsText", { text:"修改", format:{fill:"#FFF1CC",font:{color:"#8A5A00",bold:true}} });
ws.getRange(`W5:W${4+matrix.length}`).conditionalFormats.add("containsText", { text:"删除", format:{fill:"#FADBD8",font:{color:"#922B21",bold:true}} });

await fs.mkdir(outDir, {recursive:true});
const xlsx = await SpreadsheetFile.exportXlsx(wb);
await xlsx.save(outFile);
const preview = await wb.render({sheetName:"术式清单", range:"A1:X18", scale:0.8, format:"png"});
await fs.writeFile(previewFile, new Uint8Array(await preview.arrayBuffer()));

const inspect = await wb.inspect({kind:"region",sheetId:"术式清单",range:"A1:X12",maxChars:6000,tableMaxRows:12,tableMaxCols:24,tableMaxCellChars:120});
const errors = await wb.inspect({kind:"match",sheetId:"术式清单",searchTerm:"#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",options:{useRegex:true,maxResults:50},maxChars:2000});
console.log(JSON.stringify({outFile,previewFile,count:allRows.length,inspect:inspect.ndjson,errors:errors.ndjson}));
