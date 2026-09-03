# OCC M-A35 生产与评审记录 v0.1

> 状态：`12/12 REVIEW_READY / PRODUCT REVIEW PENDING`，2026-08-30。  
> 总接触表：`QA/contacts/m_a35_production_contact.png`。  
> 组级接触：`m_a35_adjacency_contact.png`、`m_a35_semantic_material_contact.png`。

## 生产与验证

- 12 件均在 manifest 建立后由内置图像生成能力独立生产；未调用本地工作台、localhost、私有 relay 或 CLI 回退。
- 港区石岸四向直边 4、四向外角 4、通用材料 4，共 12/12 通过当前机器合同。
- 合同单测 6/6、合同审计 PASS；每件具备原始输出、规范化交付、哈希、1×／4×／灰阶／棋盘格证据。
- 邻接件按固定左上光分别生产，归一化只执行机械缩放、硬透明和按命名方向触边，不旋转或镜像原料；3×3 地面接触确认外圈连续、中央地面保持无遮挡。
- 材料按原生 `24×24` 生产并机械嵌入 `32×32` 交付画布；灯油与蜂蜡的暖色只表达真实物质，不作为以太高亮语义。
- 未修改或导入 `UnityProject/Assets/`；无 GUID、Importer 和运行时验证，不标记 `FORMAL_CANDIDATE` 或 `FORMAL`。

## 当前评审结论

| 组 | 资产 | 机器 | 内部规范复核 | 产品 |
| --- | --- | --- | --- | --- |
| 港区邻接 | `harbor_quay_edge_north/east/south/west` | 4/4 PASS | 直边方向、触边与低矮压边成立 | 待审 |
| 港区邻接 | `harbor_quay_corner_ne/se/sw/nw` | 4/4 PASS | L 形外角与相邻直边接触成立 | 待审 |
| 通用材料 | `sailcloth_patch` | PASS | 帆布补片与折叠厚度可读 | 待审 |
| 通用材料 | `filter_grit_pouch` | PASS | 过滤砂袋与散粒结构可读 | 待审 |
| 通用材料 | `lamp_oil_bottle` | PASS | 陶质灯油容器可读，无现代瓶形 | 待审 |
| 通用材料 | `beeswax_seal_sticks` | PASS | 蜂蜡封条束可读，暖色不冒充法术 | 待审 |

## 拒绝版本

本批首轮 12 件均通过组级接触与规范复核，没有生成替换版本；如产品退回，后续版本仍按逐件原料留档规则进入 `rejected/`。

## 状态边界

本批只补足港区地面邻接语法与通用物质库存，不冻结真实地图、任务物、配方或掉落内容。产品可以按资产或整批给出通过／返工；即使全部通过，也因“不实装”要求保持非 Unity 状态。
