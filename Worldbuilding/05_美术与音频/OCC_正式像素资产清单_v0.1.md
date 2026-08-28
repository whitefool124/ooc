# OCC 姝ｅ紡鍍忕礌璧勪骇娓呭崟 v0.1

> 鐩殑锛氭妸姒傚康鍙傝€冦€佸師鍨嬪崰浣嶅拰鍙繘鍏ユ寮忕増鏈殑鍍忕礌璧勪骇鍒嗗紑绠＄悊銆?
> **登记表，不是规范源（2026-08-23）。** 本文件包含历史编码损坏、旧批次状态与已废弃生产路线，只用于追溯资产记录。尺寸、来源、风格、状态晋级与QA一律以 `OCC_美术规范_v0.1.md` 为唯一依据，并由 `Tools/OCCArt/occ_art_contract_v1.json`／`validate_occ_art_asset.py` 执行。旧条目不得反向覆盖现行合同。

## 2026-08-25 学院九图像素级终审地材（ART-ACADEMY-POLISH-39）

- `FORMAL`：再次返工 `academy_ground_macro_{earth|earth_b|ruin|ruin_b}_3x3`，共 4 张 `96×96 / 3×3` 宏块；夯土采用每格 16px 的三档近似明度安静材质，遗迹保留每格 32px 的规则切石／不规则卵石轮廓并收紧为 4 色。
- 运行时不再让夯土／遗迹 A、B 在同一地图按 `3×3` 棋盘交错；`rail_patrol`／`relay_raid` 与 `depot_wreck`／`elite_foundry` 分别使用稳定的地图级变体，既保留跨地图复用，也消除同图拼贴感。
- 4/4 机器合同、稳定 GUID、Importer、1×／4×／灰阶／棋盘格、九图总览及 1:1 战场细节接触 PASS；manifest 位于 `正式美术生产/M-A18/manifests/terrain_ground_macros_v19/`，实机证据位于 `UnityProject/Artifacts/Terrain39/NineMaps/`。

## 2026-08-25 学院九图二维体素结构与低噪底材（ART-ACADEMY-VOXEL-38）

- `FORMAL`：`academy_curb_{edge|corner|opposite|three|enclosed}`，共 5 张 `32×32` 硬 Alpha 四邻接压边；运行时以掩码与 90° 旋转覆盖石路／庭院／夯土／遗迹边界。
- `FORMAL`：`academy_wall_{straight|end|corner}` 与 `academy_stairs_2x1`，共 4 件模块化结构；允许连接边触及画布边界，非连接方向保留透明区，组合与永久阻挡一致。
- `FORMAL`：返工 `academy_ground_macro_{earth|earth_b|ruin|ruin_b}_3x3`，共 4 张 `96×96 / 3×3` 连续地面宏块；夯土 4 色、遗迹 5 色，无车辙带、黑洞和玩法格边框。
- `PROTOTYPE / REJECTED FOR UNITY`：`academy_gate_3x2` 的门柱轮廓与永久阻挡占格不一致，未部署；源图、规范化图和 QA 保留，Unity 正式路径副本已移出，可在重画占格后再评审。
- 13/13 机器合同、Importer、稳定 GUID 与九图实机接触 PASS；合同新增 `terrain_adjacency_overlay_32` 与 `modular_structure_32` 两项真实角色并通过审计。manifest 位于 `正式美术生产/M-A18/manifests/terrain_voxel_v17/` 与 `manifests/terrain_ground_macros_v18/`。

## 2026-08-25 学院九图连续地面宏块与结构层（ART-ACADEMY-TERRAIN-37）

- `FORMAL`：`academy_ground_macro_{court|road|ruin|earth}{|_b}_3x3`，共 8 张独立 `96×96 / 3×3` 地面宏块；Point、Clamp、PPU32、Uncompressed、无 mipmap，运行时按完整宏块 UV 子区采样，不登记切片资产。
- `FORMAL`：`academy_cloister_wall_4x1`（128×32）与 `academy_broken_wall_3x1`（96×32），透明硬 Alpha 跨格结构，复用于九图边界结构层且不参与射线。
- 10/10 机器合同、Importer、稳定 GUID 与双分辨率运行时复核 PASS；九图截图见 `UnityProject/Artifacts/Terrain37/NineMaps/`。
- 详细 manifest 位于 `Worldbuilding/05_美术与音频/正式美术生产/M-A18/manifests/terrain_ground_macros_v16/` 与 `manifests/terrain_structures_v15/`；登记表仅追踪状态，不覆盖 `OCC_美术规范_v0.1.md`。

## QA 闂ㄦ

- `32x32` 鍥炬爣/鍦板潡锛氬浐瀹氬昂瀵搞€佺偣杩囨护銆佹暣鏁扮缉鏀俱€佺‖杈圭晫閫忔槑銆佸彲璇昏疆寤撱€?- `64x64` 鍗曚綅锛氳剼搴曞熀绾跨害 `Y=58`銆佷腑蹇冪嚎绾?`X=32`銆佽疆寤撳彲鍖哄垎鍏电锛屼笉浣跨敤 AI 鎷兼澘纭垏銆?- 鍔ㄧ敾锛氱嫭绔嬪抚浼樺厛锛涘繀椤绘湁鍥哄畾 cell銆佸熀绾?涓績绾裤€侀€忔槑杈圭晫銆佽皟鑹叉澘鍜?QA 鎶ュ憡銆?- 鏈€氳繃 QA 鐨勮祫婧愬彧鑳芥爣璁颁负 `PROTOTYPE` 鎴?`CONCEPT`锛屼笉寰椾綔涓烘寮忚祫浜ч獙鏀躲€?
## 褰撳墠鐩樼偣

| 璧勪骇 | 灏哄 | 鐘舵€?| 璇存槑 |
| --- | --- | --- | --- |
| `attack` | 32x32 | PROTOTYPE | Unity 鍐呯疆鐐硅繃婊ゅ浘鏍囷紝寰呰疆寤?璇箟 QA |
| `interact` | 32x32 | PROTOTYPE | Unity 鍐呯疆鐐硅繃婊ゅ浘鏍囷紝寰呰疆寤?璇箟 QA |
| `loot` | 32x32 | PROTOTYPE | Unity 鍐呯疆鐐硅繃婊ゅ浘鏍囷紝寰呰疆寤?璇箟 QA |
| `move` | 32x32 | PROTOTYPE | Unity 鍐呯疆鐐硅繃婊ゅ浘鏍囷紝寰呰疆寤?璇箟 QA |
| `skillOne` | 32x32 | PROTOTYPE | Unity 鍐呯疆鐐硅繃婊ゅ浘鏍囷紝寰呰疆寤?璇箟 QA |
| `skillTwo` | 32x32 | PROTOTYPE | Unity 鍐呯疆鐐硅繃婊ゅ浘鏍囷紝寰呰疆寤?璇箟 QA |
| 鎴樻枟鍦板潡鍒囩墖 | 32x32 | MISSING | 灏氭棤姝ｅ紡 12x9 鍒囩墖闆?|
| 涓昏/姝ユ灙鍏?鐩惧崼/鐏湳甯?绮捐嫳 | 64x64 | MISSING | 灏氭棤鐙珛甯т笌鍩虹嚎 QA |

## V2-04 鍘熸枡杩涘害锛?026-07-24锛?
| 璧勪骇 | 灏哄 | 鐘舵€?| 瀹℃煡缁撹 |
| --- | --- | --- | --- |
| 5 鎸囦护鍥炬爣 | 32x32锛堣鑼冨寲瀹℃煡鍓湰锛?| `FORMAL` | 5 椤圭嫭绔嬪師鍥俱€?x 缃戞牸 QA銆?6 鑹茶皟鑹叉澘棰勮鍜?JSON 鎶ュ憡鍧囬€氳繃锛涗互绉诲姩鏂瑰悜銆佹敾鍑汇€佹妧鑳姐€佹悳鍒€佷簰鍔ㄧ殑琛ㄩ潰璇箟鎵瑰噯銆?|
| 涓昏/姝ユ灙鍏?鐩惧崼/鐏湳甯?绮捐嫳 | 64x64锛堣鑼冨寲瀹℃煡鍓湰锛?| `FORMAL` | 5 椤圭嫭绔嬪師鍥俱€乣X=32` / `Y=58` QA 棰勮銆?4 鑹茶皟鑹叉澘鍜屾姤鍛婂潎閫氳繃锛涗互涓昏疆寤撳拰瑁呭鐗瑰緛鐨勮〃闈㈣涔夋壒鍑嗐€?|
| 鍦板潡/杞绘帺浣?閲嶆帺浣?涓户鍣?鎴樺埄鍝佺 | 32x32锛堣鑼冨寲瀹℃煡鍓湰锛?| `FORMAL` | 鐙珛鍘熷浘銆?x QA銆?6 鑹茶皟鑹叉澘涓庢姤鍛婇綈鍏紱浠ュ湴闈€佹帺浣撱€佽澶囥€佺浣撶殑琛ㄩ潰璇箟鎵瑰噯銆?|

闆嗕腑瀹℃煡缁撴灉浣嶄簬 `鍍忕礌璧勪骇鍘熸枡/V2-04/QA/OCC_V2-04_闆嗕腑瀹℃煡.md`銆備笂杩?`FORMAL` 鏄凡鎵瑰噯鐨勫師鏂欏簱璧勪骇锛屼笉浠ｈ〃 Unity 宸插鍏ワ紱Unity 杩愯鏃舵浛鎹㈤』鐢?V2-05 鍗曠嫭璇勫銆?
## V2-05 棣栨壒 Unity 瀵煎叆锛?026-07-24锛?
| 璧勪骇 | Unity 璺緞 | 杩愯鏃剁敤閫?| 瀵煎叆/杩愯鏃跺鏍?|
| --- | --- | --- | --- |
| `move`銆乣attack`銆乣skill`銆乣loot`銆乣interact` | `Assets/Game/Resources/Art/FormalIcons32/` | `FormalCombatHud` 鎸囦护鎸夐挳锛沗skill` 渚涗袱鏍兼妧鑳藉叡鐢?| `32脳32`銆丼prite銆丳oint銆丆lamp銆佹棤 mipmap锛汸lay Mode 涓?6 涓寜閽缓绔嬫寮忓浘鏍囧眰 |
| `floor`銆乣light_cover`銆乣heavy_cover`銆乣relay`銆乣loot_crate` | `Assets/Game/Resources/Art/FormalRelay32/` | 杩愯鏃?`鍦板浘鍙鍖朻 鍦板潡/鎺╀綋/涓户鍣紱鎴樺埄鍝佺鐢ㄤ簬鏃㈡湁 IMGUI 鎴樺埄鍝佸鍣?| `32脳32`銆丼prite銆丳oint銆丆lamp銆佹棤 mipmap锛汸lay Mode 澶嶆牳 108 鍦板潡銆? 杞绘帺浣撱€? 閲嶆帺浣撱€? 涓户鍣紝鍥為€€涓?0 |
| `hero`銆乣rifleman`銆乣shieldguard`銆乣pyromancer`銆乣elite` | `Assets/Game/Resources/Art/FormalUnits64/` | 鎴樻枟缃戞牸鐨勬寮忓崟浣嶇粯鍒讹紱鍚岀郴鍏电澶嶇敤鐩歌繎闈欏抚 | `64脳64`銆丼prite銆丳oint銆丆lamp銆佹棤 mipmap锛汸lay Mode 澶嶆牳 5/5 鍙姞杞斤紝涓昏/姝ユ灙鍏?鐩惧崼/鐏湳甯?绮捐嫳鍙婄洃宸ユ槧灏勬湁鏁?|
| `raider` | `Assets/Game/Resources/Art/FormalUnits64/raider.png` | 绐佽鑰呯殑涓撶敤姝ｅ紡闈欏抚 | `64脳64`銆丼prite銆丳oint銆丆lamp銆佹棤 mipmap锛汸lay Mode 纭 `绐佽鑰卄 鏄犲皠鍒?`raider` |

鏃?`Assets/Game/Art/UI/Icons32/*.asset` 缁х画鏄?`PROTOTYPE`锛屾湭琚鐩栥€備腑缁х珯鎴樺埄鍝佺宸插畬鎴愭棦鏈?IMGUI 鎴樺埄鍝佸鍣ㄧ粯鍒舵帴鍏ワ紱绐佽鑰呯幇鍦ㄤ娇鐢ㄧ嫭绔嬮潤甯с€傛墍鏈変笂杩板崟浣嶄粛鍙叿澶囬潤甯э紝鍔ㄧ敾椤讳綔涓哄悗缁嫭绔嬩换鍔″埗浣溿€?
## V2-06 绐佽鑰呯嫭绔嬮潤甯э紙2026-07-25锛?
| 璧勪骇 | 鍘熸枡/QA 璺緞 | 灏哄涓庣姸鎬?| 瀹℃煡缁撹 |
| --- | --- | --- | --- |
| `raider` | `鍍忕礌璧勪骇鍘熸枡/V2-06/Units64/occ_unit_raider_v01.png` 涓?`QA/occ_unit_raider_v01/` | `64脳64`锛屽師鏂欏簱 `FORMAL` | 鐙珛鍗曞浘鍘熸枡缁忕‖ alpha銆?4 鑹茶皟鑹叉澘銆乣X=32` / `Y=58`銆?x 瀹℃煡鍥惧拰 JSON 鎶ュ憡澶嶆牳閫氳繃锛涘凡瀵煎叆 `FormalUnits64/raider.png` 骞堕€氳繃杩愯鏃舵槧灏勫鏍革紝鍔ㄧ敾浠嶇己澶便€?|

## V2-07 涓昏寰呮満鍔ㄧ敾锛?026-07-25锛?
| 璧勪骇 | 鍘熸枡/QA 璺緞 | 灏哄涓庣姸鎬?| 瀹℃煡缁撹 |
| --- | --- | --- | --- |
| `hero_idle_4f` | `鍍忕礌璧勪骇鍘熸枡/V2-07/QA/occ_hero_idle_4f/` | `4脳64脳64`锛屽師鏂欏簱 `FORMAL` | 4 寮犲熀浜庡悓涓€涓昏鍙傝€冨崟鐙敓鎴愮殑杈撳叆锛屽潎缁忕‖ alpha銆?4 鑹茶皟鑹叉澘銆乣X=32` / `Y=58`銆丟IF銆?x QA 涓?JSON 鎶ュ憡澶嶆牳閫氳繃锛涘彲瀵煎叆涓?`256脳64` 鍥哄畾寰幆 strip銆?|

| Unity 璧勪骇 | Unity 璺緞 | 杩愯鏃剁敤閫?| 瀵煎叆/杩愯鏃跺鏍?|
| --- | --- | --- | --- |
| `hero_idle_4f` | `Assets/Game/Resources/Art/FormalAnimations64/hero_idle_4f.png` | 宸叉壒鍑嗙殑鐙珛甯у鏌ユ牱鏈紝褰撳墠涓嶄綔涓鸿繍琛屾椂渚濊禆 | `256脳64`銆丼prite銆丳oint銆丆lamp銆佹棤 mipmap锛涘洜鏈湴澶氬抚涓€鑷存€т粛寰呮垚鐔燂紝杩愯鏃舵殏缁熶竴閲囩敤闈欏抚涓庢暣鍍忕礌寰綅绉汇€?|

## V2-09 涓户绔欓潤鎬佸湴鍧楀彉浣擄紙2026-07-25锛?
| 璧勪骇 | 鍘熸枡/QA 璺緞 | 灏哄涓庣姸鎬?| 瀹℃煡缁撹 |
| --- | --- | --- | --- |
| `floor_industrial`銆乣floor_rail`銆乣floor_warning` | `鍍忕礌璧勪骇鍘熸枡/V2-09/` | `32脳32`锛屽師鏂欏簱 `FORMAL` | 3 寮犵嫭绔嬭緭鍏ュ潎缁忕‖ alpha銆?6 鑹茶皟鑹叉澘銆?x QA 鍜?JSON 鎶ュ憡澶嶆牳閫氳繃锛涘彲瀵煎叆涓户绔欏湴鍥剧殑鏃㈡湁鍦板潡 SpriteRenderer銆?|

| Unity 璧勪骇 | Unity 璺緞 | 杩愯鏃剁敤閫?| 瀵煎叆/杩愯鏃跺鏍?|
| --- | --- | --- | --- |
| `floor_industrial`銆乣floor_rail`銆乣floor_warning` | `Assets/Game/Resources/Art/FormalRelay32/` | 鏅€氬湴闈€佽竟鐣岃建閬撱€佷腑蹇冭鎴掑尯 | 涓夎€呭潎 `32脳32`銆丼prite銆丳oint銆丆lamp銆佹棤 mipmap锛汸lay Mode 楠岃瘉 78 / 24 / 6 涓棦鏈夋牸瀹炰緥锛屽洖閫€ 0銆?|

## V2-10 涓户鍣ㄧ牬鎹熼潤鎬佸弽棣堬紙2026-07-25锛?
| 璧勪骇 | 鍘熸枡/QA 璺緞 | 灏哄涓庣姸鎬?| 瀹℃煡缁撹 |
| --- | --- | --- | --- |
| `relay_destroyed` | `鍍忕礌璧勪骇鍘熸枡/V2-10/Relay32/` 涓?`QA/relay_destroyed/` | `32脳32`锛屽師鏂欏簱 `FORMAL` | 鐙珛鍘熸枡缁忕‖ alpha銆?6 鑹层€?x QA 涓?JSON 鎶ュ憡澶嶆牳锛涗粎鐢ㄤ簬 `TileState.IsDestroyed` 鐨勭洰鏍囧弽棣堛€?|

## V2-11 杞绘帺浣撶牬鎹熼潤鎬佸弽棣堬紙2026-07-25锛?
| 璧勪骇 | 鍘熸枡/QA 璺緞 | 灏哄涓庣姸鎬?| 瀹℃煡缁撹 |
| --- | --- | --- | --- |
| `light_cover_destroyed` | `鍍忕礌璧勪骇鍘熸枡/V2-11/Covers32/` 涓?`QA/light_cover_destroyed/` | `32脳32`锛屽師鏂欏簱 `FORMAL` | 鐙珛鍘熸枡缁忕‖ alpha銆?6 鑹层€?x QA 涓?JSON 鎶ュ憡澶嶆牳锛涗粎鐢ㄤ簬杞绘帺浣?`TileState.IsDestroyed` 鐨勯潤鎬佸弽棣堛€?|

## V2-12 閲嶆帺浣撶牬鎹熼潤鎬佸弽棣堬紙2026-07-25锛?
| 璧勪骇 | 鍘熸枡/QA 璺緞 | 灏哄涓庣姸鎬?| 瀹℃煡缁撹 |
| --- | --- | --- | --- |
| `heavy_cover_destroyed` | `鍍忕礌璧勪骇鍘熸枡/V2-12/Covers32/` 涓?`QA/heavy_cover_destroyed/` | `32脳32`锛屽師鏂欏簱 `FORMAL` | 鐙珛鍘熸枡缁忕‖ alpha銆?6 鑹层€?x QA 涓?JSON 鎶ュ憡澶嶆牳锛涗粎鐢ㄤ簬閲嶆帺浣?`TileState.IsDestroyed` 鐨勯潤鎬佸弽棣堛€?|

## V2-13 鎴樺埄鍝佺寮€鍚潤鎬佸弽棣堬紙2026-07-25锛?
| 璧勪骇 | 鍘熸枡/QA 璺緞 | 灏哄涓庣姸鎬?| 瀹℃煡缁撹 |
| --- | --- | --- | --- |
| `loot_crate_open` | `鍍忕礌璧勪骇鍘熸枡/V2-13/Relay32/` 涓?`QA/loot_crate_open/` | `32脳32`锛屽師鏂欏簱 `FORMAL` | 鐙珛鍘熸枡缁忕‖ alpha銆?6 鑹层€?x QA 涓?JSON 鎶ュ憡澶嶆牳锛涗粎鐢ㄤ簬 `LootContainer.IsLooted` 鐨勯潤鎬佸弽棣堛€?|

| Unity 璧勪骇 | Unity 璺緞 | 杩愯鏃剁敤閫?| 瀵煎叆/杩愯鏃跺鏍?|
| --- | --- | --- | --- |
| `loot_crate_open` | `Assets/Game/Resources/Art/FormalRelay32/loot_crate_open.png` | 宸叉悳鍒垬鍒╁搧绠辩殑寮€鍚姸鎬?| `32脳32`銆丼prite銆丳oint銆丆lamp銆佹棤 mipmap锛汸lay Mode 纭鍏抽棴/寮€鍚袱绉嶈创鍥惧潎鍙姞杞藉苟鎸?`IsLooted` 鍒嗘敮銆?|

| Unity 璧勪骇 | Unity 璺緞 | 杩愯鏃剁敤閫?| 瀵煎叆/杩愯鏃跺鏍?|
| --- | --- | --- | --- |
| `light_cover_destroyed`銆乣heavy_cover_destroyed` | `Assets/Game/Resources/Art/FormalRelay32/` | 杞?閲嶆帺浣撹€愪箙褰掗浂鍚庣殑闈欐€佸弽棣?| 鍧?`32脳32`銆丼prite銆丳oint銆丆lamp銆佹棤 mipmap锛汸lay Mode 纭姝ｅ父鐘舵€佷粛浣跨敤鍘熸帺浣撳浘锛岀牬鎹熻祫婧愬潎鍙姞杞姐€?|

## V2-03 瀹炴祴瀵煎叆瀹℃煡锛?026-07-24锛?
| 璧勪骇 | 灏哄 | Unity 瀵煎叆瀹炴祴 | QA 缁撹 |
| --- | --- | --- | --- |
| `attack`銆乣interact`銆乣loot`銆乣move`銆乣skillOne`銆乣skillTwo` | 32x32 | `RGBA32`銆乣Point`銆乣Clamp`銆乣mipmapCount=1` | 灏哄涓庡儚绱犺繃婊ゅ悎鏍硷紱缂哄皯鐙珛鍘熸枡銆佽疆寤?璇箟銆侀€忔槑杈圭晫鍜岃皟鑹叉澘瀹℃煡锛屽叏閮ㄧ淮鎸?`PROTOTYPE` |
| 鎴樻枟鍦板潡鍒囩墖 | 32x32 | 鏈彂鐜板彲瀹℃煡鐨勬寮忓垏鐗?| `MISSING` |
| 涓昏/姝ユ灙鍏?鐩惧崼/鐏湳甯?绮捐嫳 | 64x64 | 鏈彂鐜扮嫭绔嬪抚銆佷腑蹇冪嚎鎴栬剼搴曞熀绾挎姤鍛?| `MISSING` |

瀹℃煡鏂规硶銆佺姸鎬佸畾涔夈€佹彁浜ょ墿鍜?V2-04 閫愰」娓呭崟瑙?`OCC_鍍忕礌璧勪骇_QA娴佺▼_v0.1.md`銆俙mipmapCount=1` 鏄崟灞傜汗鐞嗘湰韬殑缁撴灉锛屼笉琛ㄧず鐢熸垚浜?mipmap锛涘鍏ユ寮?PNG 鍚庝粛椤诲湪 Unity 涓鏍稿叧闂?mipmap銆?
## 涓嬩竴鎵规寮忓埗浣滀紭鍏堢骇

1. 鎴樻枟鎸囦护鍥炬爣锛氱Щ鍔ㄣ€佹敾鍑汇€佹妧鑳姐€佹悳鍒€佷簰鍔ㄣ€?2. 鍗曚綅闈欏抚锛氫富瑙掋€佹鏋叺銆佺浘鍗€佺伀鏈笀銆佺簿鑻便€?3. 12x9 涓户绔欏湴鍧楀垏鐗囦笌涓户鍣ㄣ€佽交鎺╀綋銆侀噸鎺╀綋銆佹垬鍒╁搧绠便€?4. 鍛戒腑銆佸彈鍑汇€佸嚮鐮寸殑 4--10 甯т綆鎴愭湰鍍忕礌鍔ㄧ敾銆?
## 闈欏抚浼樺厛琛ュ厖锛?026-07-25锛?
- 鏈湴澶氬抚鐢熸垚涓€鑷存€ф殏涓嶄綔涓烘寮忚繍琛屾椂渚濊禆锛涘姩鐢绘潯鐩繚鎸?`DEFERRED`锛岄櫎闈炲悗缁敱浜у搧閲嶆柊纭銆?- 姝ｅ紡杩愯鏃堕粯璁や娇鐢ㄥ凡閫氳繃 QA 鐨勭嫭绔嬮潤甯э紱寰呮満浣跨敤鏁村儚绱犲井浣嶇Щ锛屽彈鍑讳娇鐢ㄧ煭鏃舵暣鍍忕礌鎶栧姩锛屽繀瑕佹椂浣跨敤棰滆壊/浜害鑴夊啿銆?- Aseprite 宸插畨瑁呬簬 `E:\SteamLibrary\steamapps\common\Aseprite\Aseprite.exe`锛屼粎鐢ㄤ簬鍚庣画浜哄伐娓呯悊銆佸鍑轰笌 QA锛屼笉鏀瑰彉鈥滅嫭绔嬪崟鍥惧師鏂欎紭鍏堚€濈殑娴佺▼銆?
## V2-15 skill_two 静态图标（2026-07-25）

| 资产 | 尺寸 | 状态 | 审查结论 |
| --- | --- | --- | --- |
| skill_two | 32×32 | FORMAL 原料 / Unity 待修正采样 | 独立单图经硬 alpha、16 色、4x QA 与 JSON 报告通过；表面语义为第二技能的火焰/奥术闪击；禁止作为多帧动画来源。 |

原料与 QA 位于 像素资产原料/V2-15/；Unity 运行时复核发现当前导入采样为 Bilinear，需下一步改为 Point 后再纳入正式导入清单。
## V2-16 floor_hazard static tile (2026-07-25)

| Asset | Size | Status | QA result |
| --- | --- | --- | --- |
| `floor_hazard` | 32x32 | `FORMAL` | Independent single-image source, hard alpha, 16-color palette, 4x QA and JSON report PASS; Unity import verified Point/Clamp. |

Source and QA outputs are under `像素资产原料/V2-16/`. This is a static tile only and is not an animation source.

## V2-17 本地 image2 规范化路线样本（2026-07-27）

| 资产 | 尺寸 | 状态 | 审查结论 |
| --- | --- | --- | --- |
| `steel_floor` | 32×32 | `QA_SAMPLE` | 本地工作台 `gpt-image-2` 单图初稿；关闭色键保留完整不透明钢铁表面，16 色规范化、硬 alpha、128×128 4x QA 和 JSON 报告均通过。 |
| `riflewoman` | 64×64 | `QA_SAMPLE` | 本地工作台 `gpt-image-2` 独立绿幕单图；色键去背后锁定 `X=32`、脚底 `Y=58`，24 色规范化、硬 alpha、256×256 4x QA 和 JSON 报告均通过。 |

两项原料、固定规格、规范化 PNG 与报告均位于 `像素资产原料/V2-17/`，仅用于验证“本地 image2 初稿 → 本地像素规范化 → QA”的生产路线，尚未导入 Unity，也不构成正式运行时替换。

## V2-18 原生逻辑像素格提示词验证（2026-07-27）

| 资产 | 尺寸 | 状态 | 审查结论 |
| --- | --- | --- | --- |
| `steel_floor_native32_v02` | 32×32 | `QA_SAMPLE` | 提示词要求 32×32 逻辑像素母版、每格等宽、无小于一格的细节与不超过 16 色；本地只做最近邻取样和无抖动调色板压缩，输出为 15 色，报告 `PASS`。 |
| `riflewoman_native64_v02` | 64×64 | `QA_SAMPLE` | 提示词要求 64×64 逻辑像素母版、每格等宽、最多 24 色、绿幕与脚底固定于逻辑 `Y=58`；本地只做最近邻取样、绿幕去背和无抖动调色板压缩，输出为 23 色，透明边界末行为 59，报告 `PASS`。 |

该路线禁止按轮廓裁切、缩放拟合或重定位。`V2-18/direct_pixel_master_qa.py` 仅允许整画布最近邻取样、硬 alpha/绿幕去背、调色板压缩和 QA 叠线；素材仍未导入 Unity。

## V2-19 Codex 单图生成与规范化样本（2026-08-02）

| 资产 | 尺寸 | 状态 | 审查结论 |
| --- | --- | --- | --- |
| `aether_supply_crate_v02` | 32×32 | `FORMAL` 原料 / Unity 未导入 | 使用 Codex 内建 ImageGen 生成两张独立绿幕单图；第二版经 chroma-key 去背、整画布最近邻采样和无抖动压缩后为 14 色、硬 alpha，4× QA、调色板和 JSON 规格报告均为 `PASS`，边界为 `[6,8,26,23]`。按产品决定接受其低饱和冷青/安全黄表现；不替换现有 `loot_crate`。 |

原图、去背中间文件、规范化 PNG 与 QA 位于 `像素资产原料/V2-19/`。此条目验证 Codex 生成可纳入 OCC 的“单图原料 → 本地规范化 → QA”路径，并已通过本项人工目视门禁；在专门导入任务完成前不得复制到 Unity 正式资源目录。

## R-F5-02 战斗反馈静态图标复用基线（2026-08-04）

本项未生成、复制或替换像素资产，仅将已经导入 `Assets/Game/Resources/Art/FormalIcons32/` 的六张正式静态图标收束为基础效果语义入口。

| Unity 资源键 | 复用语义 | 导入复核 |
| --- | --- | --- |
| `attack` | 伤害、破甲、物件摧毁、目标击破 | 32×32 / Sprite / Point / Clamp / 透明 / 无 mipmap |
| `skill` | 护盾吸收、束缚、护盾恢复 | 32×32 / Sprite / Point / Clamp / 透明 / 无 mipmap |
| `skill_two` | 燃烧、以太恢复 | 32×32 / Sprite / Point / Clamp / 透明 / 无 mipmap |
| `move` | 迟缓、位移 | 32×32 / Sprite / Point / Clamp / 透明 / 无 mipmap |
| `loot` | 生命修复 | 32×32 / Sprite / Point / Clamp / 透明 / 无 mipmap |
| `interact` | 状态净化、物件受损 | 32×32 / Sprite / Point / Clamp / 透明 / 无 mipmap |

14 类反馈通过“资源键 + 固定颜色 + 中文短标签”形成唯一组合；27 个验证技能只引用基础表现语义，不为单个技能创建专属图标或多帧动画。后续资产扩容必须继续经过独立原料、硬 alpha、调色板与 4× QA 门禁，不能把本次语义复用视为未审资产的批准入口。
# 2026-08-08 类塔科夫背包与搜索图标增量

- `FormalItemIcons32/fire_scroll`、`demolition_canister`：F-S01/F-T01 专属内容图标。
- `FormalItemIcons32/category_*`：武器、防具、消耗品、卷轴、法宝、材料、任务物、容器 8 类。
- `FormalItemIcons32/inventory_*`：搜索、筛选、排序、自动放入、快捷栏、使用、拆解、丢弃、旋转、清除条件、负重 11 项。
- `FormalItemIcons32/loot_*`：未知、搜索中、空容器 3 项；与容器状态文字共同呈现，不依赖颜色单独识别。
- 合计 18 张，全部 32×32、硬 Alpha、3–7 色；Sprite / Point / Clamp / 无 mipmap，18/18 PASS。
- QA：`UnityProject/Artifacts/Inventory/inventory_icons_contact_sheet.png` 与 `inventory_icons_qa.json`。
- 2026-08-08 新系统补缺 QA：`inventory_missing_icons_contact_sheet.png`、`inventory_missing_icons_qa.json`；6/6 为 32×32、硬 Alpha、3–5 个不透明色，Unity Importer 6/6 为 Sprite/Point/Clamp/无 mipmap。

## ENEMY-PACK-01 当前时代特色敌人（2026-08-08）

| 资产 | Unity 路径 | 最终像素门禁 | 运行时语义 |
| --- | --- | --- | --- |
| `sigil_mauler` | `Assets/Game/Resources/Art/FormalUnits64/sigil_mauler.png` | 64×64、38 px 高、20 色、硬 Alpha、中心 X=31、接地点 Y=57，PASS | 刻印锤手；大锤近战与接触破甲，不含爆破箱、弹体、枪械或现代工兵语义 |
| `barrier_mender` | `Assets/Game/Resources/Art/FormalUnits64/barrier_mender.png` | 64×64、38 px 高、20 色、硬 Alpha、中心 X=32、接地点 Y=57，PASS | 屏障修补师；手杖、符片与线轴施术，不含现代工程兵轮廓 |
| `tether_hound` | `Assets/Game/Resources/Art/FormalUnits64/tether_hound.png` | 64×64、34 px 高、18 色、硬 Alpha、中心 X=31、接地点 Y=57，PASS | 缚环猎兽；原生魔法生物与项圈定式，不是机械犬、污染变异或智慧异族 |
| `shieldguard` | `Assets/Game/Resources/Art/FormalUnits64/shieldguard.png` | 64×64、46 px 高、24 色、硬 Alpha、中心 X=31.5、接地点 Y=57，PASS | 铭盾卫；宽盾正面轮廓与接触迟缓 |
| `pyromancer` | `Assets/Game/Resources/Art/FormalUnits64/pyromancer.png` | 64×64、46 px 高、24 色、硬 Alpha、中心 X=31.5、接地点 Y=57，PASS | 火矢术师；手杖与以太火纹，不含火器轮廓 |
| `raider` | `Assets/Game/Resources/Art/FormalUnits64/raider.png` | 64×64、46 px 高、24 色、硬 Alpha、中心 X=32、接地点 Y=57，PASS | 钩刃突袭者；轻装前倾与钩刃牵制 |
| `elite_vanguard` | `Assets/Game/Resources/Art/FormalUnits64/elite.png` | 64×64、46 px 高、24 色、硬 Alpha、中心 X=31.5、接地点 Y=57，PASS | 刻阵先锋；重甲、重锤和刻阵肩甲 |
| `stone_snare` | `Assets/Game/Resources/Art/FormalUnits64/stone_snare.png` | 64×64、38 px 高、20 色、硬 Alpha、中心 X=32、接地点 Y=57，PASS | 石索缚师；绳索与双石坠轮廓，不读作爆炸物 |
| `lantern_revealer` | `Assets/Game/Resources/Art/FormalUnits64/lantern_revealer.png` | 64×64、38 px 高、20 色、硬 Alpha、中心 X=32、接地点 Y=57，PASS | 显影灯使；冷青刻纹铜灯，不是电灯/现代侦察器材 |
| `rune_arbalist` | `Assets/Game/Resources/Art/FormalUnits64/rune_arbalist.png` | 64×64、38 px 高、20 色、硬 Alpha、中心 X=31、接地点 Y=57，PASS | 重弩手；宽弩臂、弩弦与短矢槽，无枪管/枪托/瞄准镜 |

- 旧 `aether_sapper`、`barrier_engineer`、`relay_hound` 母图、规范化输出与报告保留在 `Tools/Art/EnemyPack01/` 作为修订证据；旧 Unity 导入副本已移出正式资源目录并归档到 `retired_unity_drafts/`，不再可由 `FormalArtRegistry` 访问。
- 最终母图追溯、规范化输出、GIF 与报告位于 `Tools/Art/EnemyPack01/raw/` 和 `normalized/`；1×/4×、灰阶、轮廓、中心/基线、调色板与汇总报告位于 `final_qa/`。
- 十张扩展包单位图均经最终像素 QA；新增三张 Unity Importer 经 Funplay 实测为 Sprite / Point / Clamp / PPU32 / 无 mipmap，既有七张由全量门禁复核。运行时以 10 个稳定 ID/ArtId 注册且本包内不复用轮廓。

## M-A11 肉鸽 UI 正式语义资产（2026-08-17）

| 资产组 | 数量 | 尺寸／导入 | 状态 |
| --- | ---: | --- | --- |
| 资源与指标 | 9 | 32×32、硬 Alpha、Point、Clamp、PPU32 | FORMAL / QA_PASS |
| 11 装备位 | 11 | 32×32、硬 Alpha、Point、Clamp、PPU32 | FORMAL / QA_PASS |
| 地图节点状态 | 7 | 独立 16×16、硬 Alpha、Point、Clamp、PPU16 | FORMAL / QA_PASS |

- 资源组覆盖金币、阶段贡献、公开时序、已探索、核心许可、风险、重量、以太负荷与剩余次数。
- 装备位组覆盖主手、副手、头部、胸甲、手部、腿部、背架、以太核心、导器与两个饰品位；只表达槽位语义，不冒充逐件装备内容图标。
- 地图状态组替换旧“现／可／清／锁／？”字符标记；运行时通过 `FormalArtRegistry` 严格加载，不存在通用占位回退。
- 简报、4× QA 图和 JSON 报告位于 `正式美术生产/M-A11/`，`27/27 PASS`；Unity 导入与全量 EditMode 门禁通过。

## M-A12 学院装备双分辨率正式资产（2026-08-17）

| 资产组 | 数量 | 尺寸／导入 | 状态 |
| --- | ---: | --- | --- |
| 32 件装备内容图标 | 32 | 独立 32×32、硬 Alpha、Point、Clamp、PPU32 | FORMAL / QA_PASS |
| 32 件装备背包占格图 | 32 | 严格 `Width×32 × Height×32`、硬 Alpha、Point、Clamp、PPU32 | FORMAL / QA_PASS |

- 内容图标覆盖学院目录 `ACA-EQ-MH01` 至 `ACA-EQ-AC04` 全部 32 个稳定 ID，主轮廓分别表达武器、防具、背架、核心、导器与饰品的具体结构。
- 占格图在每件装备自己的 `Width×16 × Height×16` 逻辑画布上绘制，不由 32×32 图标拉伸；战外整备与战斗背包共用同一正式路径。
- 空装备槽继续使用 M-A11 槽位语义图；已有装备、选中详情与结算奖励使用本批逐件内容图标，从视觉合同上区分“槽位”和“物品”。
- 简报、4× QA 图和 JSON 报告位于 `正式美术生产/M-A12/`，`32 件 / 64 资产 / 64 PASS`；Unity 逐件尺寸与导入门禁通过。

## M-A20 学院装备专属内容图标重制（2026-08-26）

| 资产组 | 数量 | 尺寸／导入 | 状态 |
| --- | ---: | --- | --- |
| 学院装备内容图标 | 32 | 独立原生 32×32、硬 Alpha、5–10 色、Point、Clamp、PPU32 | FORMAL / QA_PASS |

- M-A20 只替换 M-A12 的 32 件内容图标 PNG，M-A12 的严格背包占格图保持不变；装备 ID、槽位、占格、数值和运行时注册不变。
- 逐件轮廓改为真实剑／枪／锤／弓弩／导杖、盾具、穿戴服装、背架、核心、导具与饰品；不再使用程序化矩形符号或同槽模板换色。
- 水晶只保留在真实储能／聚焦语义：三枚核心分别使用烟紫、橙红余烬与琥珀金介质；其余装备以锻铁、旧铜、木、皮、粗布、陶瓷、玻璃和刻线为主，不默认蓝晶或冷青能量。
- 32 份 manifest、验证、Importer／GUID 报告和生产目录位于 `正式美术生产/M-A20/`；Unity 双分辨率接触与前后对照位于 `UnityProject/Artifacts/AcademyEquipment32/contacts/`。机器验证 `32/32 PASS`、合同审计 PASS、运行时加载与全量 EditMode 门禁通过。

## M-A21 学院装备多格占格素材重制（2026-08-26）

| 资产组 | 数量 | 尺寸／导入 | 状态 |
| --- | ---: | --- | --- |
| 学院装备背包占格图 | 32 | `32×32` 至 `64×96`、严格 `(W×32)×(H×32)`、Point、Clamp、PPU32 | FORMAL / QA_PASS |

- 逐件独立重制既有 `1×1／1×2／1×3／2×1／2×2／2×3` 占格素材；没有拉伸 M-A20 内容图标，也没有改变 `RogueContentCatalog` 的宽×高。
- 长柄武器沿 1×3 纵轴展开，2×3 战锤／弓弩／服装／背架使用完整大画布，2×1 成对穿戴件横向组织，2×2 盾／靴／核心使用宽厚主体。
- 资产采用无固定投影的旋转安全构图；材质和功能色延续 M-A20，三类核心分别为烟紫、余烬橙红和琥珀金，不使用统一蓝晶模板。
- 32 份 manifest、验证及 Importer／GUID 报告位于 `正式美术生产/M-A21/`；两张正式 6×10 背包的双分辨率接触与前后对照位于 `UnityProject/Artifacts/AcademyEquipmentFootprints32/contacts/`。机器验证 `32/32 PASS`，合同、运行时和全量 EditMode 门禁通过。

## M-A22 法宝背包占格素材重制（2026-08-26）

| 资产组 | 数量 | 尺寸／导入 | 状态 |
| --- | ---: | --- | --- |
| 法宝背包占格图 | 20 | `32×32` 至 `96×64`、严格 `(W×32)×(H×32)`、Point、Clamp、PPU32 | FORMAL / QA_PASS |

- 逐件独立重制 `F-T01` 与 `G-T01` 至 `G-T19` 的既有占格素材，不放大 M-A19 内容图标，不改变 `ArtifactCatalog` 的稳定 ID、宽×高、旋转、重量、次数、效果或内容池。
- 器物轮廓覆盖陶铜炎脉筒、折盾匣、线轴、缚位框、测镜、手压泵、编架、压模、楔、罗盘、铃、冷凝器、行程簿、锚架、棱镜调节器、诱导灯、均衡阀、铅锤、静默幕与封签；不再使用旧几何符号占格。
- 功能色拆分为陶红／橙、乳白／灰绿／旧金、紫罗兰、草绿／象牙、烟紫／黑金与淡水绿；水晶只用于真实光学结构，全部无固定投影并支持既有背包旋转。
- 20 份 manifest、验证及 Importer／GUID 报告位于 `正式美术生产/M-A22/`；Unity 双分辨率 6×10 背包接触与前后对照位于 `UnityProject/Artifacts/ArtifactFootprints20/contacts/`。机器验证 `20/20 PASS`、合同测试 `6/6`、Resources／Importer／GUID `20/20`、全量 EditMode `649/649`。

## M-A23 通用物品背包占格素材重制（2026-08-26）

| 资产组 | 数量 | 尺寸／导入 | 状态 |
| --- | ---: | --- | --- |
| 通用物品背包占格图 | 4 | `64×32／32×64／64×32／64×64`、Point、Clamp、PPU32 | FORMAL / QA_PASS |

- 独立重制医疗包、护盾单元、火线卷轴和任务以太核心；不拉伸内容图标，不改变稳定 ID、占格、旋转、次数、效果、重量或任务属性。
- 医疗包改为象牙陶瓷医务箱与灰绿布扣，护盾单元改为乳白介质／灰绿绝缘／旧金阀帽，火线卷轴使用耐热皮纸与陶红／橙线路，以太核心使用深陶六瓣护壳和琥珀金内部储能窗；不再复用统一蓝晶或冷青电池模板。
- 4 份 manifest、验证与 Importer／GUID 报告位于 `正式美术生产/M-A23/`；Unity 6×10 背包双分辨率接触及前后对照位于 `UnityProject/Artifacts/ItemFootprints4/contacts/`。机器验证 `4/4 PASS`、合同测试 `6/6`、Resources／Importer／GUID `4/4`、全量 EditMode `649/649`。

## M-A24 战斗核心语义图标重制（2026-08-27）

| 资产组 | 数量 | 尺寸／导入 | 状态 |
| --- | ---: | --- | --- |
| 玩家指令 | 6 | `16×16`、Point、Clamp、PPU16 | FORMAL / QA_PASS |
| 敌方意图 | 5 | `16×16`、Point、Clamp、PPU16 | FORMAL / QA_PASS |
| 持续状态 | 6 | `32×32`、Point、Clamp、PPU32 | FORMAL / QA_PASS |
| 即时反馈 | 14 | `32×32`、Point、Clamp、PPU32 | FORMAL / QA_PASS |

- 指令、意图、状态与反馈建立“按钮动作→敌人计划→持续状态→结算瞬间”的四层视觉语法；旧细剑、闪电框、眼睛、同盾换色和通用蓝晶语言退出核心战斗显示。
- 色彩按功能分配：攻击／破坏用锈红，火焰用陶红／橙，防护用乳白／旧金，治疗用灰绿，法力用紫罗兰，束缚／减速用棕／赭黄，冷青仅保留给主动位移。
- 31 份 manifest 与生产简报位于 `正式美术生产/M-A24/`；Unity 双分辨率实际战斗接触、离线清单与前后对照位于 `UnityProject/Artifacts/CombatSemantics31/contacts/`。机器验证 `31/31 PASS`、合同测试 `6/6`、Resources／Importer／GUID `31/31`、旧 GUID 稳定 `25/25`、全量 EditMode `650/650`。

## M-A25 战斗语义弱项定点打磨（2026-08-27）

| 返修组 | 数量 | 尺寸／导入 | 状态 |
| --- | ---: | --- | --- |
| 敌方意图 | 3 | `16×16`、Point、Clamp、PPU16 | FORMAL / QA_PASS |
| 持续状态 | 2 | `32×32`、Point、Clamp、PPU32 | FORMAL / QA_PASS |
| 即时反馈 | 5 | `32×32`、Point、Clamp、PPU32 | FORMAL / QA_PASS |

- 返修对象仅限 M-A24 的 10 个 1× 弱项；将同形三叉意图、装饰花结、地面杂物、治疗罐、翅膀盾和黄色彗星替换为重斩楔、校准环、短锤断角、压靴重块、测绘锁定、夹紧、止动、缝合、护片合拢和扫除动作。
- 10 份 manifest 与简报位于 `正式美术生产/M-A25/`；旧新对照和 Unity 双分辨率接触位于 `UnityProject/Artifacts/CombatSemanticPolish10/contacts/`。机器验证 `10/10 PASS`、合同测试 `6/6`、Resources／Importer／稳定 GUID `10/10`、全量 EditMode `650/650`。

## M-A13 学院背景设定图探索（2026-08-17）

| 概念图 | 视觉职责 | 状态 |
| --- | --- | --- |
| 学院中庭总览 | 开局／地图背景；开放中心、四向连接与受控中继器 | CONCEPT REFERENCE |
| 校准工坊 | 整备背景；武器、装备、核心与导器同源校准 | CONCEPT REFERENCE |
| 封存高塔外环 | 收束／首领背景；权限门、维护链与异常升级 | CONCEPT REFERENCE |

- 三图位于 `正式美术生产/M-A13/`，共享煤灰／铁黑主体、受控冷青以太、安全黄维护和锈红危险语义。
- 本批是宽画幅像素背景的构图、材质和光值探索，不进入 `FormalArtRegistry`，不得直接切作地图地块、UI、设备或角色正式资产。
- 后续若选定具体画面进入游戏，必须另做目标分辨率、遮挡安全区、整数缩放、分层／静态方案和 Unity 导入 QA。

## M-A17 学院 3D 沙盘地图（2026-08-18）

| 运行时层 | 数量／规格 | 状态 |
| --- | --- | --- |
| 可编辑建筑地标 | 13 个稳定 ID；坐标、尺寸、高度、色彩独立 | RUNTIME FOUNDATION |
| 道路与地理地标 | 6 条主路、岸线、中央石庭、封存塔、以太渠 | RUNTIME FOUNDATION |
| 像素化输出 | 768×432 RenderTexture、Point、无 AA／Mip | QA_PASS |
| 节点状态牌 | 7 张独立 32×32、硬 Alpha、Point／Clamp、PPU32 | FORMAL / QA_PASS |

- M-A17 不使用静态美术底图；3D 模型经正交相机输出到 UGUI，节点和路线使用同一投影合同。
- M-A16 的平面试产图是 `PRODUCT VISUAL REJECTED` 证据，不进入 `FormalArtRegistry`。
- 运行截图和合同位于 `正式美术生产/M-A17/`；最终全量 EditMode 545/545、PlayMode 1/1、Console clean、场景未脏。

## M-A18 学院阶段战斗美术生产包（2026-08-20）

| 资产组 | 规划规模 | 当前状态 |
| --- | ---: | --- |
| 学院战斗地块与物件 | 48 张 32×32：27 张地面／连接件、18 张掩体／任务物三态、3 张战利品箱状态 | `FORMAL / QA_PASS`；庭院四图恢复已确认的 clean32 v07；21 张透明场地物以应用环境为 Gate 0 逐张独立生产为 v09，实际运行映射项为 `RUNTIME_COMPLETE`，无直接地图状态入口的草边与封印台保留正式导入 |
| 学院敌人静帧 | 12 张独立 64×64：10 个学院敌人／役兽／构造体及 2 名首领 | `RUNTIME_COMPLETE / QA_PASS`；v05 全员以质朴西幻魔法侧身份重制，标准人形约 46px、精英／首领约 52px、四足按 52–62px 宽例外 |
| 学院敌人标志动作 | 12 组 × 2 张 64×64 双状态端点 | `RUNTIME_COMPLETE / QA_PASS`；每组只保留 `frame_00` 预备态与 `frame_05` 峰值态，运行时硬切并使用 0–2px 整数抖动 |
| 首发火系 VFX | 14 组 × 6 张 32×32 帧，共 84 帧 | `RUNTIME_COMPLETE`；十四模块覆盖 60 项术式并具有同格组合优先级 |

- 正式生产总包、逐族 CSV 清单、P0 六帧分镜与审核门槛位于 `正式美术生产/M-A18/`。
- 本包把旧 `FormalRelayV01` 视为尺寸和玩法语义参考，不延续铁路、黄黑警示漆、现代工业钢板与现代中继设备外形；新地块目标族为 `FormalAcademyCombat32`。
- P0 先完成并验证庭院、两名首领、三种代表动作与 `fire_cast`；产品随后明确授权放宽锚点／规范化约束并要求完成全包。早期失败稿继续保持 `REWORK`，不与最终正式资产混淆。
- 2026-08-22 产品把 OCC 全项目人物动画改为双状态端点；旧十二组六帧脚本部件动画已降为 `REWORK` 审计证据并移出 Unity 正式资源区。v05 的 12 张身份静帧与 24 张端点均有独立原料、规范化报告、逐项接触表和全员比例接触表；运行时只严格加载两个端点。完整证据见 `正式美术生产/M-A18/OCC_M-A18_双状态端点首批生产简报_v0.1.md` 与 `Worldbuilding/03_开发管理/OCC_M-A18_质朴双状态端点正式生产验证_2026-08-22.md`。
- 2026-08-23 产品实机否决 v08：其中 21 张场地物／掩体三态属于脚本几何部件稿，不能因机械规格通过而登记为正式美术；该批已归档为 `PRODUCT_REJECTED / PROTOTYPE`。21 张场地物改为直接图像渠道的逐图独立来源并以实际战斗格占用先行定尺寸：1920×1080 默认 128px 格按 4×显示，960×540 按 2×显示。v09 全部 21/21 通过 1×／4×／灰阶／透明棋盘格、三态接触表、12×9 混合场景、Importer 与双分辨率 Play Mode 复核；详见 `正式美术生产/M-A18/OCC_M-A18_学院战场物件独立像素重制_v0.1.md`。
- 2026-08-23 庭院 materialscale v10：clean32 v07 的“一格一板”与下／右边缘归属在实机中再次否决。正式 `academy_courtyard_a-d` 保持原路径、GUID、32×32 与 PPU32，改为每格 2×4 个 16×8px 小切石模块、1px 石缝；A–D 任意有向拼接横／纵边缘均 0 mismatch，4 个独立哈希、平均明度差 0.15、孤立像素 0。同步把 `move_range`／`attack_range`／`selected` 改为 1px 内缩方角语义框，旧粗边缺口稿归档为应用否决。直接图像候选因斜向噪点未进入 Unity，正式图来自逐像素明确源稿；详见 `正式美术生产/M-A18/OCC_M-A18_学院庭院材质尺度无缝返修_v0.1.md`。
- 2026-08-24 学院连续底材 v11：`PRODUCT_REJECTED / PROTOTYPE`。本批四张 384×288 连续材质场虽然消除逐格接缝且通过机械门禁，但产品实机复核确认其斑驳、噪声化、缺少正交像素战棋 tileset 的地形语义与人工构图，原 `FORMAL` 结论撤销。四张底材只保留拒收审计；透明线路的分层原则进入新版邻接套件重新审核，不能沿用本批审美放行。
- 2026-08-25 学院模块化地面 v14：19 张 `FORMAL` 独立模块进入 `FormalAcademyCombat32`，包括庭院／道路／遗迹／夯土四族 A–D 和道路直边／转角／端头；逐项 manifest、1×／4×／灰阶／棋盘格、九图接触、稳定 GUID、Importer 与九图双分辨率运行证据齐全。`academy_north_dais_6x2` 同步以 `FORMAL` 进入 `FormalAcademyStructures32`，复用于档案庭与校门门厅；九图不使用整张背景图。详见 `正式美术生产/M-A18/OCC_M-A18_学院九图模块化战场完成_v0.1.md`。
