# OCC M-A36 生产与评审记录 v0.1

> 状态：`4/4 REVIEW_READY / PRODUCT REVIEW PENDING`，2026-08-30。  
> 总接触表：`QA/contacts/m_a36_production_contact.png`。  
> 应用接触：`QA/contacts/m_a36_prop_footprint_contact.png`。

## 生产与验证

- 4 件均在 manifest 建立后由内置图像生成能力独立生产；未调用本地工作台、localhost、私有 relay 或 CLI 回退。
- 单格港务物件 2、多格港务物件 2，共 4/4 通过当前机器合同；合同单测 6/6、合同审计 PASS。
- 每件具备原始输出、规范化交付、哈希、1×／4×／灰阶／棋盘格证据；单格严格为 `32×32`，2×1 严格为 `64×32`。
- 占格接触将资产铺在 M-A33 港区地面并叠加评审格线：绳卷与灯柱保持一格内，货秤与晾修架跨两格且未被缩入单格。
- 未修改或导入 `UnityProject/Assets/`；无 GUID、Importer 和运行时验证，不标记 `FORMAL_CANDIDATE` 或 `FORMAL`。

## 当前评审结论

| 资产 | 机器 | 内部规范复核 | 产品 |
| --- | --- | --- | --- |
| `harbor_coiled_mooring_rope` | PASS | 低矮绳卷轮廓、麻绳材质与一格占地成立 | 待审 |
| `harbor_oil_lantern_post` | PASS | 未点燃油灯、木／石／铁材质与一格占地成立 | 待审 |
| `harbor_cargo_balance_2x1` | PASS | 人力杠杆秤、货盘／配重与两格占地成立 | 待审 |
| `harbor_canvas_drying_frame_2x1` | PASS | 帆布晾修架、补片与两格支脚成立 | 待审 |

## 状态边界

本批将港区／仓栈通用物件库存由 6 件补至 10 件，达到需求表建议的 8–16 件区间；不为资产附加碰撞、交互、任务或掉落规则。产品可以按资产或整批给出通过／返工；即使全部通过，也因“不实装”要求保持非 Unity 状态。
