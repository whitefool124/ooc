# OCC M-A41 生产与评审记录 v0.1

> 状态：`6/6 REVIEW_READY / PRODUCT REVIEW PENDING`，2026-08-30。  
> 总接触表：`QA/contacts/m_a41_production_contact.png`。  
> 材料接触：`QA/contacts/m_a41_semantic_material_contact.png`。

## 生产与验证

- 6 件均在 manifest 建立后由 Codex 内置图像生成能力独立生产；未调用本地工作台、localhost、私有 relay 或自动回退。
- 硬皂块、明矾袋、干海藻束、纸包熏鱼、黄铜线圈和陶瓷密封环共 6/6 通过单资产机器合同；合同单测 6/6、规范镜像审计 PASS。
- 每件保留原生 `24×24` 源图，并以 `(4,4)` 无缩放嵌入 `32×32` 交付画布；均具备源图／交付哈希、1×／4×／灰阶／棋盘格和组级应用接触。
- 24px 组级接触确认六件分别读取为清洁材料、净水矿物、港区干货、保存口粮、维修导线与陶质密封件；没有药水、完整工具、文字、魔法光或黑色背景。
- 未修改或导入 `UnityProject/Assets/`；不冻结食用效果、净水公式、配方、价格、掉落、任务用途或正式内容 ID，不标记 `FORMAL_CANDIDATE` 或 `FORMAL`。

## 拒绝版本

- `lard_soap_blocks v01`：三个浅色块在 24px 下更像奶酪／食物，清洁材料特征不足；已进入 `rejected/lard_soap_blocks/v01/`。现行 v02 使用单块硬皂、直切槽、缺角和三枚皂屑建立语义。
- `brass_wire_coil v01`：深橄榄色粗环与布条在 24px 下更像绳卷；已进入 `rejected/brass_wire_coil/v01/`。现行 v02 使用分离细金属环、冷灰夹具和短断头建立导线语义。

## 状态边界

本批只把可复用通用材料库存补到需求表建议上限 20 件，不创建 gameplay 内容。产品仍可按资产或整批给出通过／返工；即使通过，也因“不实装”要求保持非 Unity 状态。
