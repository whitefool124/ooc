import { mkdir, writeFile } from "node:fs/promises";
import { join } from "node:path";
import { formalIds, mapDirectory, rebuildAllDerivedData, validateMap } from "./map-build.mjs";

const P = (x, y) => ({ x, y });
const E = (archetypeId, x, y) => ({ archetypeId, x, y });
const T = (kind, x, y, mechanismKind = 0) => ({ kind, x, y, mechanismKind });
const C = (id, type, label, timing, trigger, result, duration, source, positions = []) =>
  ({ id, type, label, timing, trigger, result, duration, source, positions });
const walls = (...coords) => coords.map(([x, y]) => T("PermanentWall", x, y));
const battleType = options => options.isBoss ? "Boss战斗" : options.isElite ? "精英战斗" : "普通战斗";
const battleSequence = { "普通战斗": 0, "精英战斗": 0, "Boss战斗": 0 };
const numberedName = (displayName, options) => {
  const type = battleType(options);
  battleSequence[type] += 1;
  return `${type} ${String(battleSequence[type]).padStart(2, "0")} · ${displayName}`;
};
const map = (id, displayName, width, height, options) => ({
  schemaVersion: "occ-battle-map-v1", id, displayName: numberedName(displayName, options), width, height,
  objectiveSummary: options.objectiveSummary, objectiveType: options.objectiveType || "Elimination", tier: options.tier,
  floorTheme: options.floorTheme || "Courtyard", isElite: !!options.isElite, isBoss: !!options.isBoss,
  complexMechanics: width * height >= 40, prerequisites: options.prerequisites || [], heroSpawn: options.heroSpawn,
  enemies: options.enemies, terrain: options.terrain, blockedPositions: [], changeAnnotations: options.changeAnnotations || [],
  spaceContract: { grammar: options.grammar, publicRisk: options.publicRisk, counterplayWindow: options.counterplayWindow },
  notes: options.notes || "2026-09-22 按 25–50 格战场合同重置；永久重物块用于塑造有效战区。", updatedAt: new Date().toISOString()
});

const maps = [
  map("rain_lantern_court", "雨后灯庭", 6, 5, {
    objectiveSummary:"让寻迹兽失去行动能力，并让高年级火矢生认输。", tier:1, heroSpawn:P(1,4),
    enemies:[E("tether_hound",4,3),E("pyromancer",5,0)],
    terrain:[T("Water",2,3),T("Water",3,3),T("Water",4,3),T("LampVine",3,0),T("LampVine",4,0),T("LampVine",3,1),T("LampVine",4,1),T("LampVine",3,2),T("LampVine",4,2),T("LightCover",1,2),T("LightCover",5,2),T("LightCover",2,4),...walls([0,0],[1,0],[0,1],[5,4])],
    grammar:"雨后石庭、短浅水横带、双列灯藤与西侧安全入口",
    publicRisk:"寻迹兽封住近侧，火矢生沿干地与藤列形成公开射线。", counterplayWindow:"可入藤延迟搜查、借浅水灭火，或沿西侧掩体先处理寻迹兽。"
  }),
  map("first_battle_greenhouse_collection_room", "温室藏品间", 7, 6, {
    objectiveSummary:"击倒替身偶与侧锋生；中央备件箱可搜刮苗床回流芯。", tier:2, prerequisites:["rain_lantern_court"], heroSpawn:P(1,3),
    enemies:[E("raider",6,1),E("sigil_mauler",6,4)],
    terrain:[T("LampVine",2,2),T("LampVine",3,2),T("LampVine",4,2),T("LampVine",2,3),T("LampVine",4,3),T("LampVine",2,4),T("LampVine",3,4),T("LampVine",4,4),T("AetherCrystal",5,1),T("AetherCrystal",5,4),...walls([0,0],[0,5],[3,0],[6,5])],
    grammar:"中央藤圈宝箱、东侧双晶簇、南侧普通长路",
    publicRisk:"侧锋与替身偶分守两枚晶簇，藤圈遮断中央攻击线。", counterplayWindow:"可跃进入藤搜刮、等待敌人贴晶后引爆，或沿南侧干路稳健推进。"
  }),
  map("first_b3_rain_prism_court", "雨痕晶庭", 8, 5, {
    objectiveSummary:"击倒替身偶与火矢生；可破坏封门晶簇搜刮学院储能芯。", tier:3, prerequisites:["first_battle_greenhouse_collection_room"], heroSpawn:P(1,2),
    enemies:[E("sigil_mauler",4,2),E("pyromancer",5,0)],
    terrain:[T("Water",2,0),T("Water",2,1),T("Water",2,2),T("Water",2,3),T("Water",3,0),T("Water",4,0),T("AetherCrystal",5,2),T("LightCover",1,0),T("LightCover",1,4),T("LightCover",7,0),T("LightCover",7,4),...walls([6,1],[6,3],[7,2])],
    grammar:"纵向冷却沟、封门晶簇与南侧干路",
    publicRisk:"火矢生从干地施加燃烧，封门晶簇与永久重物块围出器材匣。", counterplayWindow:"可破晶开匣并爆伤替身偶、退入浅水熄火，或放弃搜刮走北侧普通路线。"
  }),
  map("first_elite_three_material_pressure", "三材承压场", 8, 6, {
    objectiveSummary:"击倒楔角。", tier:4, isElite:true, prerequisites:["first_b3_rain_prism_court"], heroSpawn:P(1,3),
    enemies:[E("breach_ram",6,3)],
    terrain:[T("Water",2,1),T("Water",2,2),T("Water",2,3),T("Water",2,4),T("LampVine",3,1),T("LampVine",4,1),T("LampVine",5,1),T("LampVine",3,4),T("LampVine",4,4),T("LampVine",5,4),T("AetherCrystal",5,3),...walls([0,0],[0,5],[7,0],[7,5])],
    grammar:"冷却沟、双灯藤带、中央稳压晶簇与两侧干路",
    publicRisk:"楔角公开锁定冲压线，并会撞击晶簇、单位或灯藤。", counterplayWindow:"可诱导撞晶、藏入灯藤、用浅水缩短冲压，或走南侧干路等待卸压。"
  }),
  map("rail_patrol", "石路巡哨", 6, 5, {
    objectiveSummary:"清除石路巡哨队", tier:1, floorTheme:"StoneRoad", heroSpawn:P(2,4), enemies:[E("shieldguard",2,2),E("pyromancer",0,0),E("raider",5,0)],
    terrain:[T("LightCover",0,2),T("LightCover",5,2),T("LightCover",1,4),T("LightCover",4,4),T("HeavyCover",1,2),T("HeavyCover",4,2),...walls([2,0],[3,0])],
    grammar:"短石路交叉线与底边门墩", publicRisk:"压中会进入两翼交叉影响区。", counterplayWindow:"左右翼均可回到底边换线，中央盾位不封死两侧。"
  }),
  map("depot_wreck", "废弃驿站", 6, 5, {
    objectiveSummary:"清除占据驿站的敌人", tier:1, floorTheme:"Ruins", heroSpawn:P(2,2), enemies:[E("tether_hound",0,0),E("sigil_mauler",5,4),E("stone_snare",5,0)],
    terrain:[T("HeavyCover",1,1),T("HeavyCover",1,3),T("HeavyCover",4,1),T("HeavyCover",4,3),T("LightCover",0,2),T("LightCover",5,2),...walls([2,0],[3,4])],
    grammar:"三口收束的小驿站废墟", publicRisk:"三股追击从不同方向抵达，中线停留会叠加控制。", counterplayWindow:"上下宽口都能拆开三名敌人的接触节奏。"
  }),
  map("relay_raid", "野外导能柱", 6, 6, {
    objectiveSummary:"破坏被敌军占用的导能柱", objectiveType:"Destruction", tier:2, floorTheme:"AetherMarked", prerequisites:["rail_patrol"], heroSpawn:P(4,0), enemies:[E("raider",4,2),E("rune_arbalist",5,4),E("tether_hound",1,4)],
    terrain:[T("LightCover",2,2),T("LightCover",4,4),T("HeavyCover",3,1),T("HeavyCover",3,3),T("AetherObjective",1,2),...walls([0,0],[0,5],[5,0])],
    grammar:"偏心目标与折线掩体", publicRisk:"直切目标会进入背弩生公开射线。", counterplayWindow:"可先压制背弩生，也可由下侧掩体接近目标。"
  }),
  map("signal_hub", "传讯石庭", 6, 6, {
    objectiveSummary:"清除传讯石庭守军", tier:2, prerequisites:["depot_wreck"], heroSpawn:P(0,4), enemies:[E("barrier_mender",4,1),E("lantern_revealer",1,1),E("shieldguard",4,4)],
    terrain:[T("LightCover",3,1),T("LightCover",1,3),T("LightCover",5,4),T("HeavyCover",2,2),T("HeavyCover",4,3),T("HeavyCover",2,4),...walls([0,0],[3,5],[4,5])],
    grammar:"三角维护网与北侧石台", publicRisk:"切任一支援点都会留下另外两条公开维护关系。", counterplayWindow:"外围保持连通，可在看见显影与护障意图后换边。"
  }),
  map("gatehouse", "石闸关口", 6, 6, {
    objectiveSummary:"夺取石闸关口", tier:3, prerequisites:["signal_hub","relay_raid"], heroSpawn:P(3,3), enemies:[E("shieldguard",0,3),E("sigil_mauler",5,1),E("rune_arbalist",5,4)],
    terrain:[T("LightCover",2,2),T("LightCover",4,3),T("HeavyCover",1,1),T("HeavyCover",1,4),T("HeavyCover",4,1),T("HeavyCover",4,4),...walls([2,0],[3,0],[2,5],[3,5])],
    grammar:"双向门厅与上下门楣", publicRisk:"两条门道分别暴露于近战截击与背弩射线。", counterplayWindow:"两路均保持两格宽，可在门厅中央改变方向。"
  }),
  map("transmission_tower", "传讯塔楼", 6, 6, {
    objectiveSummary:"破坏塔楼内的敌方导能柱", objectiveType:"Destruction", tier:3, floorTheme:"AetherMarked", prerequisites:["signal_hub"], heroSpawn:P(0,2), enemies:[E("pyromancer",5,1),E("stone_snare",5,4),E("lantern_revealer",1,4)],
    terrain:[T("LightCover",2,2),T("LightCover",2,4),T("LightCover",4,3),T("HeavyCover",3,2),T("HeavyCover",4,4),T("AetherObjective",3,3),...walls([0,0],[5,0],[0,5])],
    grammar:"中央装置与三个短扇区", publicRisk:"接近中心只能遮蔽一部分远程压力。", counterplayWindow:"目标破坏即胜；可快拆或先从左上处理显影。"
  }),
  map("elite_foundry", "刻阵工坊", 6, 6, {
    objectiveSummary:"摧毁工坊内的敌方导能柱", objectiveType:"Destruction", tier:3, floorTheme:"Ruins", isElite:true, prerequisites:["signal_hub"], heroSpawn:P(2,5), enemies:[E("elite_vanguard",2,2),E("barrier_mender",0,0),E("sigil_mauler",0,4)],
    terrain:[T("LightCover",4,4),T("HeavyCover",1,1),T("HeavyCover",3,1),T("HeavyCover",1,3),T("HeavyCover",4,3),T("AetherObjective",5,0),...walls([2,0],[3,5])],
    changeAnnotations:[C("vanguard_temp_heavy_cover","temporary","划线教官生成临时重掩体","划线教官回合开始","场上没有可倚靠的完整重掩体且本场生成次数未满","在划线教官正交相邻合法空格生成耐久12的重掩体","直到被摧毁；每场最多2次","elite_vanguard")],
    grammar:"编织狭口与双短路", publicRisk:"左路承受破势，右路更快接近目标但会被教官横移拦截。", counterplayWindow:"两路在中部连通，单个敌人无法封死双路。"
  }),
  map("core_approach", "塔前石庭", 6, 6, {
    objectiveSummary:"清除古塔前庭守军", tier:4, isElite:true, prerequisites:["transmission_tower","elite_foundry"], heroSpawn:P(0,4), enemies:[E("elite_vanguard",2,3),E("rune_arbalist",5,1),E("stone_snare",5,4)],
    terrain:[T("LightCover",0,2),T("LightCover",3,5),T("LightCover",5,3),T("HeavyCover",1,1),T("HeavyCover",2,2),T("HeavyCover",4,4),...walls([2,0],[3,0],[2,5])],
    changeAnnotations:[C("vanguard_temp_heavy_cover","temporary","划线教官生成临时重掩体","划线教官回合开始","场上没有可倚靠的完整重掩体且本场生成次数未满","在划线教官正交相邻合法空格生成耐久12的重掩体","直到被摧毁；每场最多2次","elite_vanguard")],
    grammar:"对角封线与中央缺口", publicRisk:"背弩与约束控制相反对角，教官惩罚直穿。", counterplayWindow:"外围路线较慢，中心两侧缺口允许在意图公开后换线。"
  }),
  map("core_finale", "古塔核心", 8, 6, {
    objectiveSummary:"击败拦在必经之路上的塔之守卫", tier:5, floorTheme:"AetherMarked", isBoss:true, prerequisites:["core_approach"], heroSpawn:P(0,3), enemies:[E("core_overseer",4,3)],
    terrain:[T("LightCover",2,2),T("LightCover",2,4),T("LightCover",6,3),T("HeavyCover",3,2),T("HeavyCover",5,2),T("HeavyCover",5,4),T("HeavyCover",3,4),T("TowerMechanism",6,1,1),T("TowerMechanism",2,1,2),T("TowerMechanism",4,5,3),...walls([0,0],[0,5],[7,0],[7,5])],
    changeAnnotations:[
      C("phase0_turn1_ward","phase","第1回合放行护障维护","阶段〇·第1自身回合","塔之守卫回合开始","放行护障维护；核心开始按存活维护链获得护盾","机关持续到被摧毁", "core_overseer", [P(6,1)]),
      C("ward_temp_heavy_cover","temporary","护障维护生成临时重掩体","第1回合机关放行时","护障维护成功放行","在机关正交相邻合法空格生成最多2面耐久12的重掩体","直到被摧毁","core_overseer", [P(6,1)]),
      C("phase0_turn2_reveal","phase","第2回合放行显影巡查","阶段〇·第2自身回合","塔之守卫回合开始","从机关朝主角方向生成5格光带并清除线上护盾","机关持续到被摧毁；光带随方向刷新","core_overseer",[P(2,1)]),
      C("phase0_turn3_press","phase","第3回合放行冲压隔离","阶段〇·第3自身回合","塔之守卫回合开始","公开4格冲压线；核心回合结束时线上单位受到6点以太伤害","机关持续到被摧毁","core_overseer",[P(4,5)]),
      C("phase2_chain","phase","阶段二并链","生命不高于30%","塔之守卫每次出手","所有存活且已放行机关同时结算一次","直到战斗结束或机关被摧毁","core_overseer",[P(6,1),P(2,1),P(4,5)])
    ],
    grammar:"中心核心、三道机关与四角塔墙", publicRisk:"核心居中，三组机关按阶段逐组放行。", counterplayWindow:"机关耐久与阶段公开，可先拆已放行机关切断对应加成。"
  }),
  map("calibration_lockdown", "失控校准室封锁", 7, 6, {
    objectiveSummary:"击倒试制员、背弩生与拴索助教。", tier:3, floorTheme:"Ruins", isElite:true, prerequisites:["rail_patrol","depot_wreck"], heroSpawn:P(0,3), enemies:[E("prototype_hand",4,3),E("rune_arbalist",6,1),E("stone_snare",6,4)],
    terrain:[T("HeavyCover",2,1),T("HeavyCover",2,4),T("HeavyCover",5,2),T("HeavyCover",5,4),T("HeavyCover",3,3),T("HeavyCover",4,2),T("WardGenerator",1,1),T("OverloadDevice",1,4),T("Water",2,2),T("Water",2,3),...walls([3,0],[4,0],[3,5],[4,5])],
    changeAnnotations:[C("prototype_temp_devices","temporary","试制员布放临时装置","试制员回合开始","试制箱仍有库存且存在正交相邻合法空格","依次布放耐久8的护罩发生器或过载装置","直到被摧毁或引爆；受库存限制","prototype_hand")],
    grammar:"设备间短走廊、冷却沟与双装置区", publicRisk:"试制件逐段堵路，背弩射线覆盖主廊。", counterplayWindow:"可先拆试制件、反用过载区，或从外侧短路绕行。"
  }),
  map("cliff_relay_survey", "断崖导能柱考察", 7, 6, {
    objectiveSummary:"击倒老寻、灯台值守与补盾助教。", tier:3, floorTheme:"Ruins", isElite:true, prerequisites:["rail_patrol","depot_wreck"], heroSpawn:P(0,4), enemies:[E("elder_tracker_hound",4,4),E("signal_keeper",6,3),E("barrier_mender",6,1)],
    terrain:[T("Water",1,3),T("Water",2,3),T("Water",3,3),T("LampVine",4,1),T("LampVine",5,1),T("LampVine",4,2),T("LampVine",5,2),T("HeavyCover",5,3),T("HeavyCover",1,2),T("HeavyCover",3,1),T("LoosePaper",3,4),T("LoosePaper",4,4),T("HeavyCover",2,4),...walls([0,0],[1,0],[2,0],[6,5])],
    grammar:"断崖坡面、积水带、灯藤暗段与基座", publicRisk:"光柱封住中廊，老寻沿痕提速。", counterplayWindow:"走积水清痕，或借重物块与基座留下的暗段换线。"
  }),
  map("sealed_vault_certification", "封存库权限核验", 7, 6, {
    objectiveSummary:"击倒老库管、提灯巡查与替身偶。", tier:3, floorTheme:"AetherMarked", isElite:true, prerequisites:["relay_raid","signal_hub"], heroSpawn:P(0,4), enemies:[E("legacy_storekeeper",5,3),E("lantern_revealer",6,1),E("sigil_mauler",4,4)],
    terrain:[T("HeavyCover",3,1),T("HeavyCover",3,4),T("HeavyCover",6,2),T("HeavyCover",2,1),T("HeavyCover",1,0),T("WardGenerator",4,3),T("LoosePaper",1,2),T("LoosePaper",1,3),T("Water",0,1),T("Water",1,1),...walls([3,0],[4,0],[3,5],[4,5])],
    grammar:"货架短墙与消防积水构成的库房通道", publicRisk:"老库管会退掉新场地，提灯巡查沿直线施加破势。", counterplayWindow:"利用货架短墙切断旧脉冲，或抢在登记生效前从积水侧推进。"
  }),
  map("library_discipline", "图书馆纠察", 7, 6, {
    objectiveSummary:"击倒小铃、补盾助教与替身偶。", tier:3, isElite:true, prerequisites:["rail_patrol","relay_raid"], heroSpawn:P(1,3), enemies:[E("wind_librarian",6,1),E("barrier_mender",6,4),E("sigil_mauler",6,3)],
    terrain:[T("LightCover",0,3),T("HeavyCover",2,1),T("HeavyCover",2,4),T("HeavyCover",4,1),T("HeavyCover",4,4),T("HeavyCover",3,2),T("HeavyCover",3,4),T("LoosePaper",1,1),T("LoosePaper",1,4),T("LoosePaper",5,1),T("LoosePaper",5,4),T("BindingMark",5,3),...walls([3,0],[3,5])],
    grammar:"书架双廊、阅览桌与散页堆", publicRisk:"页幕和书架会截断攻击线，补盾助教依附长桌。", counterplayWindow:"烧掉或打湿散页，再从另一条走廊压上。"
  }),
  map("outer_ring_clearance", "高塔外环清障", 7, 6, {
    objectiveSummary:"击倒划线教官、提灯巡查与背弩生。", tier:3, floorTheme:"AetherMarked", isElite:true, prerequisites:["relay_raid","signal_hub"], heroSpawn:P(0,3), enemies:[E("elite_vanguard",4,3),E("lantern_revealer",6,4),E("rune_arbalist",6,1)],
    terrain:[T("HeavyCover",2,2),T("HeavyCover",4,2),T("HeavyCover",3,1),T("HeavyCover",3,4),T("HeavyCover",1,3),T("HeavyCover",5,2),T("LoosePaper",4,4),T("LoosePaper",4,1),T("Water",2,4),T("HeavyCover",3,3),T("WardGenerator",1,1),...walls([1,0],[2,0],[5,5],[6,5])],
    changeAnnotations:[C("vanguard_temp_heavy_cover","temporary","划线教官生成临时重掩体","划线教官回合开始","场上没有可倚靠的完整重掩体且本场生成次数未满","在划线教官正交相邻合法空格生成耐久12的重掩体","直到被摧毁；每场最多2次","elite_vanguard")],
    grammar:"外环直廊、中央物块堆与两处重掩体", publicRisk:"背弩重矢与显影沿同一廊道叠加。", counterplayWindow:"借永久重物块遮断直线，或先拆重掩体再压上。"
  }),
  map("weak_layout_w1", "弱遭遇布局 W1", 6, 5, {
    objectiveSummary:"弱遭遇双敌演练布局", tier:1, floorTheme:"StoneRoad", heroSpawn:P(2,4), enemies:[E("raider",1,0),E("shieldguard",4,0)],
    terrain:[T("LightCover",1,1),T("LightCover",4,1),T("HeavyCover",2,2),T("LightCover",0,3),T("LightCover",5,3),...walls([2,0],[3,0])],
    grammar:"半开放双翼演练场", publicRisk:"中央盾位与单侧接近形成公开夹角。", counterplayWindow:"出生点后方保留换翼退路。"
  }),
  map("weak_layout_w2", "弱遭遇布局 W2", 6, 5, {
    objectiveSummary:"弱遭遇双敌折角布局", tier:1, floorTheme:"Courtyard", heroSpawn:P(1,4), enemies:[E("rune_arbalist",5,0),E("raider",5,3)],
    terrain:[T("HeavyCover",3,0),T("HeavyCover",3,1),T("LightCover",4,1),T("HeavyCover",1,2),T("HeavyCover",2,2),T("LightCover",1,3),...walls([3,3],[3,4])],
    grammar:"回廊折角与双出口", publicRisk:"远端射线与下方通道接近同时公开。", counterplayWindow:"折角墙后可换线并切断远射。"
  }),
  map("weak_layout_w3", "弱遭遇布局 W3", 6, 5, {
    objectiveSummary:"弱遭遇双敌环形布局", tier:1, floorTheme:"Ruins", heroSpawn:P(2,4), enemies:[E("tether_hound",1,0),E("shieldguard",4,0)],
    terrain:[T("HeavyCover",2,1),T("HeavyCover",3,1),T("HeavyCover",1,2),T("HeavyCover",4,2),T("HeavyCover",2,3),T("HeavyCover",3,3),T("LightCover",0,3),T("LightCover",5,3),...walls([0,0],[5,0])],
    grammar:"六块结构围出的短环路", publicRisk:"双敌分守环路两侧。", counterplayWindow:"环路上下均连通，可主动拆开接触顺序。"
  }),
  map("weak_layout_w4", "弱遭遇布局 W4", 6, 5, {
    objectiveSummary:"弱遭遇双敌错位布局", tier:1, floorTheme:"AetherMarked", heroSpawn:P(0,4), enemies:[E("pyromancer",4,0),E("raider",5,3)],
    terrain:[T("LightCover",4,1),T("HeavyCover",1,1),T("HeavyCover",2,1),T("LightCover",4,2),T("LightCover",2,3),T("HeavyCover",4,3),...walls([0,0],[5,0])],
    grammar:"错位掩体与远近双线", publicRisk:"火矢远压与侧锋接近线错位。", counterplayWindow:"中段掩体允许先挡远程或先截侧锋。"
  })
];

if (maps.length !== formalIds.length || maps.some(item => !formalIds.includes(item.id))) throw new Error("正式地图清单与生成脚本不一致");
for (const item of maps) {
  const errors = validateMap(item, { formal: true });
  if (errors.length) throw new Error(`${item.id}: ${errors.join("；")}`);
}
await mkdir(mapDirectory, { recursive: true });
for (const item of maps) await writeFile(join(mapDirectory, `${item.id}.occ-map.json`), JSON.stringify(item, null, 2) + "\n", "utf8");
await rebuildAllDerivedData();
console.log(`已重置 ${maps.length} 张正式战斗地图。`);
