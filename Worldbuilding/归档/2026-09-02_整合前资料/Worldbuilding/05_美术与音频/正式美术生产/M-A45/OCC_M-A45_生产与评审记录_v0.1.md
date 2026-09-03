# OCC M-A45 生产与评审记录 v0.1

> 状态：`8/8 REVIEW_READY / PRODUCT REVIEW PENDING`，2026-08-31。  
> 总接触表：`QA/contacts/m_a45_production_contact.png`。  
> 地板族接触：`QA/contacts/m_a45_floor_family_contact.png`。  
> 格位接触：`QA/contacts/m_a45_prop_footprint_contact.png`。  
> 地区扩充接触：`QA/contacts/m_a45_outer_ring_expansion_contact.png`。

## 生产与验证

- 已完成翻耕田土 A／B、编篱田门、空载手推车、排水木板跨桥、无字界石、幼树护架与覆布干草垛，共 8 件独立原料。
- 每件均在 manifest 建立后由 Codex 内置图像生成能力独立生产；没有拼板切图、本地工作台、localhost、私有 relay 或自动回退。
- 8/8 通过尺寸、格位、透明边界、硬 alpha 与调色板机器合同；合同单测 6/6、规范镜像审计 PASS。
- M-A43＋M-A45 的 12×9 地区接触确认地板家族、编篱与田门邻接、1×1／2×1／2×2 格位及猎人驿亭／干草垛两个锚点共存；无交互或战利品叠层。
- 外环地图库存累计 20 件，其中地材 6、物件 12、锚点 2，位于需求表 12–24 件及 1–2 锚点建议区间内。
- 未修改或导入 `UnityProject/Assets/`，不创建 GUID，不标记 `FORMAL_CANDIDATE` 或 `FORMAL`。

## 拒绝版本

- `outer_ring_turned_field_a v01` 与 `outer_ring_turned_field_b v01`：随机混铺出现强烈重复的波纹／斜纹，压过单位；现行 v02 改为低对比整格土面。
- `outer_ring_field_boundary_stone v01`：圆钝轮廓和地表碎屑使其读取为自然巨石；现行 v02 使用人工切割的直立矩形块。
- `outer_ring_sapling_guard v01`：护架过细，在 32px 地图中近乎消失；`v02`：对称三叉轮廓读取为图腾标记。现行 `v03` 使用方形木护框、中央树干和非对称橄榄叶簇。

## 状态边界

本批只扩充非 Unity 地区语汇，不为田门、手推车、跨桥、界石、幼树或干草垛配置碰撞、破坏、采集、藏匿、火灾、任务或掉落规则。产品仍可按资产或整批给出通过／返工。
