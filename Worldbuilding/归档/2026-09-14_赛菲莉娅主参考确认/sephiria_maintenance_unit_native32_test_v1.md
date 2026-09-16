# 32px纯侧视人物流程测试 v1

- 状态：视觉流程测试，不进入正式Unity资产目录。
- 身份：学院战场维护人员。
- 视角：人物身体严格纯侧视，面向屏幕右侧；地图地面仍使用斜俯视。
- 流程：Image阶段直接按32×32逻辑像素密度设计；淘汰附件过多的第一稿；第二稿移除扳手、背包和挂件后，只做背景剥离、整格采样、统一调色板、硬Alpha与单像素外轮廓。
- 输出：32×32画布；可见边界 `(9,2)-(21,29)`；人物高28px；13色；硬Alpha；外轮廓边界全部纯黑。
- 同屏证据：已与批准的32px宝箱放在相邻地块验证，人物与物件的原生像素块尺度一致。
- 限制：当前活动合同只有64px战棋单位角色，尚无32px战棋单位正式角色类型；本稿不误标为正式候选。

## 追溯

- Image参考：`Worldbuilding/归档/2026-09-14_赛菲莉娅主参考确认/source/sephiria_maintenance_unit_native32_reference_v2.png`
- Image参考 SHA256：`6fd1fb22873c292d3f756b9e56562c89ab1c63205cc764ed8722cc728af035cd`
- 规范化脚本：`Tools/OCCArt/normalize_image_native32_character.py`
- 脚本 SHA256：`8febd4c781f22e3857ef639761210837460ab0341e3c97704fe4a40641afc9c9`
- 32px输出 SHA256：`9dd6be21e9dd32e652ee8cd881ba0ca8cfebdec92edfc6f6de921eb94730aa45`
