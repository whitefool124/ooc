# OCC ART-ARTIFACT-FOOTPRINTS-54 法宝 20 件背包占格素材重制验证（2026-08-26）

## 结论

PASS。20 件既有法宝背包占格图已在不改变任何法宝定义、玩法逻辑、占格或存档的前提下完成独立重制并晋级 `FORMAL`。

## 生产与审美

- 20 份 `occ-art-manifest-v1` 在生成前建立；20 次 Codex 内建 imagegen 均为单件器物调用。一次并发网络失败未产生可用原料，缺失项只沿原渠道重试。
- 没有本地生图工作台、localhost API、私有 relay、自动回退、生成拼板切图或 M-A19 32px 内容图拉伸。
- 六种交付尺寸严格覆盖 `32×32／32×64／32×96／64×32／64×64／96×64`；全部硬 Alpha、5–12 色、至少 2px 透明安全边、无固定投影与方向阴影。
- 人工审美确认 20 件在 1× 下由真实器物轮廓区分；火、护幕、相位、复元、暗相位和冷凝分别使用不同功能色，玻璃／水晶只在真实光学或储能结构出现。

## Unity 与回归

- Funplay 身份门禁：`Application.dataPath=E:/数据库/OCC_Codex/UnityProject/Assets`；活动场景 `Assets/Scenes/CombatPrototype.unity`，操作前后均 clean。
- 正式目录 `Assets/Game/Resources/Art/FormalInventoryFootprints`：20/20 Sprite、Point、Clamp、PPU32、Uncompressed、无 mipmap；Resources 运行时加载 20/20。
- `.meta` 未替换，GUID 前后稳定 20/20，唯一 GUID 20/20。
- 两只真实 6×10 背包容纳全部 64 格法宝占格；Unity Resources 双分辨率接触为 `unity_inventory_contact_1920x1080.png` 与 `unity_inventory_contact_960x540.png`。
- `validate_occ_art_asset.py`：20/20 PASS；美术合同 unittest：6/6 PASS；Unity 编译错误／警告 0；全量 EditMode 649/649；Console error 0；dirty scenes 0。未进入 Play Mode，未保存场景。

## 证据

- 生产目录与 manifest：`Worldbuilding/05_美术与音频/正式美术生产/M-A22/`
- 1×／4×／灰阶／棋盘格：`UnityProject/Artifacts/ArtifactFootprints20/<slug>/`
- 前后对照：`UnityProject/Artifacts/ArtifactFootprints20/contacts/before_after_1920x620.png`
- Unity 接触：`UnityProject/Artifacts/ArtifactFootprints20/contacts/unity_inventory_contact_1920x1080.png`、`unity_inventory_contact_960x540.png`
