# ART-ACADEMY-POLISH-39 学院九图像素级视觉终审验证

日期：2026-08-25

## 结论

- 九图继续使用可跨关卡复用的地材宏块、四邻接压边与模块化墙／端头／转角／楼梯，没有导入关卡专属整图。
- 夯土由四档高频碎纹降为每格 16px、三档近似明度的安静材质；人物、范围框和道路优先级明显高于底纹。
- 遗迹第一次降频因丢失石块语义而被实机否决；最终保留每格 32px 的规则切石／不规则卵石轮廓，只收紧到四档明度。
- 夯土与遗迹不再在同一张地图按 3×3 宏块棋盘交错：A、B 改为地图级稳定变体，消除驿站和精英工坊的拼贴感。
- 九图证据标签移到画面外，不再遮挡顶边单位、墙体或角落；每图另输出 1:1 战场细节裁切。

## 资产与运行时门禁

- `terrain_ground_macros_v19` 四件 `96×96 / 3×3` 正式资产：4/4 `validate_occ_art_asset.py` PASS。
- 机器合同审计：PASS。
- Unity Importer：Point／Clamp／PPU32／Uncompressed／无 mipmap；四件均保持原稳定 GUID。
- 运行时：1920×1080 九图、960×540 九图、1:1 战场细节接触全部复核；墙／楼梯／压边不参与射线，单位与临时范围覆盖保持上层可读。

## 自动化与编辑器状态

- Unity 编译：0 error。
- EditMode：644/644 passed。
- PlayMode：1/1 passed。
- Console：无 cached error。
- Dirty scene：0；未保存 `CombatPrototype.unity`。

## 证据

- 九图总览：`UnityProject/Artifacts/Terrain39/NineMaps/terrain39_nine_maps_contact.png`
- 各图双分辨率与细节：`UnityProject/Artifacts/Terrain39/NineMaps/`
- 资产 QA：`Worldbuilding/05_美术与音频/正式美术生产/M-A18/QA/terrain_ground_macros_v19/`
- 正式 manifest：`Worldbuilding/05_美术与音频/正式美术生产/M-A18/manifests/terrain_ground_macros_v19/`
