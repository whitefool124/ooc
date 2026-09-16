# 像素风类银河恶魔城参考收集

本轮只记录公开商店截图作为美术观察参考，不下载、不导入、不临摹原图。

## 参考样本

- [Blasphemous 商店页](https://store.steampowered.com/app/774361/Blasphemous/)：高对比轮廓、2–4px 连续材质簇、环境细节集中在可读形体上。
- [Bloodstained: Curse of the Moon 商店页](https://store.steampowered.com/app/838310/Bloodstained_Curse_of_the_Moon/)：粗颗粒角色与地形、有限色阶、台阶和边缘读法清楚。
- [Timespinner 商店页](https://store.steampowered.com/app/368620/Timespinner/)：角色、墙面、地板共享同一像素网格，装饰细节不抢主体。
- [Momodora: Reverie Under the Moonlight 商店页](https://store.steampowered.com/app/428550/Momodora_Reverie_Under_The_Moonlight/)：暗部层次和前后景遮挡适合借鉴，避免地板过度高频。
- [Axiom Verge 商店页](https://store.steampowered.com/app/332200/Axiom_Verge/)：低色数、强轮廓、功能性材质块面，适合做战棋读法参考。
- [Infernax 商店页](https://store.steampowered.com/app/374190/Infernax/)：最粗颗粒的一组参考，地面纹理克制，单位在场景中不会显得像另一套分辨率。
- [Gestalt: Steam & Cinder 商店页](https://store.steampowered.com/app/1231990/Gestalt_Steam__Cinder/)：本轮最接近 OCC 的主参考；工业建筑、人物和平台共享粗颗粒与清晰大块面，色彩丰富但不靠细噪点堆材质。

## 可直接对照的实机截图

以下均为 Steam 商店公开截图，仅作观察参考，不下载、不导入、不临摹原图：

- [Gestalt 场景截图 1](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1231990/ss_3cef17d4307f94bfe11750dd5eea218ceccb5396.1920x1080.jpg?t=1729786474)
- [Gestalt 场景截图 2](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1231990/ss_7ab0c3e39a06d047024aa9d83e9f8124794a83d0.1920x1080.jpg?t=1729786474)
- [Timespinner 场景截图](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/368620/ss_7d1fb2391de4098964de05c737076d86c14e2ce6.1920x1080.jpg?t=1783035176)
- [Axiom Verge 场景截图](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/332200/ss_978607583bf147d520f488bb9acdb1c00ea3349b.1920x1080.jpg?t=1645563574)
- [Bloodstained: Curse of the Moon 场景截图](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/838310/ss_8d45b68dbe1311161f115b721be7709dbf28465e.1920x1080.jpg?t=1727748384)
- [Infernax 场景截图](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/374190/ss_c4a659c6ebad1c55b8749b0485c07eadb6541be5.1920x1080.jpg?t=1780931133)
- [Momodora 场景截图](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/428550/ss_051cd90e0d4329259efe71cba64c439cdd607beb.1920x1080.jpg?t=1734415917)

## OCC 提取的统一规则

1. 64px 单格内以 2×2 至 4×4 的连续像素簇为主，1px 只用于少量轮廓、断口和高光。
2. 地板、人物、重掩体共享同一明暗阶和像素颗粒尺度；禁止地板使用高频噪点伪装材质。
3. 地块中心低对比，边缘和碰撞轮廓清晰；固定左上光源，暗缝保持统一。
4. 先完成“人物 + 一块地板 + 一个重掩体”的三件套，再扩展到主题批量。
5. 1024px 独立原料进入交付前先压到 16px 语义网格，再以整数倍放大到 64px；提示词明确禁止中心十字缝、四象限拼板和重复小砖格。

## 2026-09-14 组合复核

- 新版低频地面实机证据：[pixel_density_combo_v6_low_frequency.png](../../../UnityProject/Reports/CombatTestArena/pixel_density_combo_v6_low_frequency.png)
- 局部放大证据：[pixel_density_combo_v6_board_zoom_2x.png](../../../UnityProject/Reports/CombatTestArena/pixel_density_combo_v6_board_zoom_2x.png)
- 复核结论：地面应保持大色块和低频材质，人物继续作为高对比主体；书箱与重掩体沿用同一 64px 单格和透明安全边距。
- 青色角标属于运行时战术反馈，不纳入地面/人物/物品的像素密度审美判断。

## 人物像素语法候选

- 独立生成原料：[raw_raider_pixel_v2.png](../../../UnityProject/Reports/CombatTestArena/raw_raider_pixel_v2.png)
- 64px 规范化候选：[raider_pixel_candidate_64.png](../../../UnityProject/Reports/CombatTestArena/raider_pixel_candidate_64.png)
- 候选指标：9 色、边缘变化率约 0.49，已落入书箱/重掩体的中频区间。
- 该候选只用于验证人物像素语法，尚未替换正式敌人资源；批量替换前需先为主角和敌人族建立同一套独立原料与动画帧。
- 主角候选：[hero_pixel_candidate_64.png](../../../UnityProject/Reports/CombatTestArena/hero_pixel_candidate_64.png)；10 色、边缘变化率约 0.47，与 raider 候选处于同一区间。
- 批量规范化入口：[normalize_unit_pixel_family.py](../../../Tools/OCCArt/normalize_unit_pixel_family.py)，固定硬 Alpha、64px 画布、脚底接触线和有限色数，后续人物族统一走此入口。
