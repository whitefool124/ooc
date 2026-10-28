# 原作数据重建研究：2026-10-10

状态：QA_PENDING。可在 Unity Editor 中浏览城镇静态场景，尚未达到用户要求的 100% 质感还原，也不是 OCC 正式美术验收或可发布试玩包。

## 实际完成

- 输入为用户提供的 `E:/SteamLibrary/steamapps/common/OCTOPATH TRAVELER`。只读解析原始 pak v4；索引未加密，未修改游戏文件。
- 选择原作地图 `T_Plain_L_A`，配合官方 Steam 蓝屋顶城镇截图研究。未验证该地图的中文城镇名称。
- 导出 235 项原始网格、88 项材质引用、纹理及地图变换。三项地图内动态 Godray 材质没有作为独立包导出成功。
- Unity 重建实际使用 948 个网格组件、72 项材质及 138 个 Texture2D 加载实例；已应用材质引用缺失数为 0。这里的“0”不表示整个原作场景无缺失：光束、角色、粒子与运行时逻辑尚未重建。
- 68 项已使用材质启用独立法线。建筑使用原作底色/顶色及高度渐变参数；门使用 DoorTexture，地表使用基础纹理、合并遮罩与坡面纹理。
- 环境贴图为原作 `EnTX_Sky_Plain_Town` 的 128×768 DXT1 六面条带。反射强度取原作 .3；立方体面方向和漫反射卷积转换仍需核对。
- Shader 和 C# 已实际导入编译；最后视觉检查 Shader 消息数为 0。已在 Play Mode 执行重建并检查真实 Game View。

## 浏览入口

工程：`E:/数据库/OCC_Codex/UnityProject`。

进入 Play Mode 后选择 `OCC / Research / Build Octopath town study (Play Mode)`。WASD 移动观察位置，Q/E 改变距离，F 切换后处理。此工具在 Play Mode 中停用原场景根对象；退出 Play 后恢复原场景，不保存研究对象或替换正式美术。

源代码为 `Assets/Game/Editor/OctopathTownStudy.cs` 和 `OctopathStudySurface.shader`。原作资产从本归档加载，没有导入正式 Resources。研究脚本与原作纹理没有接入正式游戏入口。

## 验证范围和限制

- `unity_evidence/build.json` 为运行时实际重建统计。
- `unity_evidence/final_1080.png` 为 1920×1080 实际渲染截图；`final_preview.png` 是同一 1080 渲染的 960×540 缩略预览，不能据此声称已验收 540p 渲染性能。
- 已检查建筑、道路、阶梯、法线、反射贴图接入与前后景景深。镜头和 Unity BRDF、色调映射、景深焦距是重建校准值，不是全部从原作恢复的参数。
- 原作太阳强度为 30，本样板暂映射为 Unity 3；原作室外环境强度 .3、高度雾起始 4000cm、DOF 焦距位置 3500cm 有数据依据。Unity 中的线性雾终点、曝光、暗角及物理景深参数仍是近似转换。
- 原作完整材质函数、云影、AO/接触阴影、动态灯与光束、运行时角色和交互没有完全恢复。无截图逐像素一致性或 100% 质感验收结果。
- Console 在进入原 EpicToonVfxShowcase 场景时有粒子 duration 错误和 Editor 字符转换错误；此次没有修改这些既有系统，也不将“Shader 无报错”表述为整台 Editor 的 Console 完全无错误。
- 60 FPS、P95≤16.7ms 尚未性能验收；没有独立试玩包测量。

下一步是在固定原图镜头下恢复材质函数、云影与接触阴影，再核对环境反射方向和 UE 后处理，完成相同镜头对照后才可判断研究效果是否达到用户认可的标准。

## 第二轮：材质函数、接触遮蔽、云影与后处理

用户认可第一轮城镇观感，并明确要求下一轮继续研究上述四项。此次没有迁回 OCC 书桌，没有替换正式资产。

- 新增只读递归导出：17 个起始父材质/云影引用，共读取 59 个包，失败 0。包含 24 个材质函数；导出数据中存在 375 个空表达式槽位和 64 个无法在同包导出中找到的具名运算引用。包读取成功不代表完整图已恢复。`functions/audit_functions.py` 与 `functions/audit.json` 保留清单、参数、源文件哈希和缺口。
- `MPC_Object` 原始默认值为 Roughness=1、Specular=.2、Metallic=.1、NormalPower=1。对引用该集合的 68 个已使用物件材质加入可切换候选，未强套给背景等其他材质。UE dielectric Specular 转 F0 使用 `.08 × Specular`；粗糙度转 Unity Smoothness 使用 `1-Roughness`。集合默认值有原始依据，缺失算术连接和蓝图运行时覆盖仍未验证，所以不是完整函数的等价恢复。
- 加入运行时隔离的 URP SSAO：原作 AO 半径 45cm 转 .45m，全分辨率，从深度重建法线。作用为物件落地、墙根、台阶等接触环境遮蔽。它不是已恢复的 UE 接触阴影光线步进；原作 AO Power=8/Bias=.5 也没有等价转换。
- 云影使用原作 `envtx_cloudshadow` 线性遮罩，作为太阳灯 cookie，保留投影尺度 26.87m × 38.367756m。候选采用遮罩六次幂，对应保留的 EmissivePower=6 和末端 Power 节点名称，但其完整接线缺失，仍为假设。XY 平移速度 .08/.05m 每秒为明确近似；源 Speed=1 无法恢复已缺失的 Panner XY 系数。100m 淡出及 UE 投影坐标约定仍待转换。
- 加入 Gamma/Gain 转换候选。URP 轨迹球 W 是偏移，因此 UE Gamma=.6 对应 W=-.4、Gain=1.2 对应 W=.2；Contrast=.8 暂映射 -20。实际画面明显偏暗，保持该候选默认关闭。曝光、阴影分段调色、UE Film/ACES、DOF 与 Bloom 核仍需继续研究；不能声称后处理转换已完成。

浏览入口不变，新增数字键：`1` 物件参数、`2` 接触遮蔽、`3` 云影、`4` 后处理转换候选。前三项默认开，第四项默认关。原有 WASD/Q/E/F 保留。首次切换新 Shader 变体可能短暂异步编译，应待画面稳定后比较。

已通过实际按键验证：1 为 True→False→True；2/3/4 切换后的运行时状态分别为 False/False/True。冻结相机与云影阶段后采集对照。`unity_evidence/v2/` 保存第二轮最终 1080p、实际 540p、旧基线与各项研究图；各单项图为 1080 渲染的缩略预览，不能当作 540 性能证据。

必要验证：C# 导入编译完成；最终 Shader 编译队列为空、消息数 0；重建仍为 948 个网格组件/72 项使用材质/已应用引用缺失 0；两个实际渲染分辨率已查看。退出 Play 后研究根对象数 0、场景无脏状态、原 UniversalRP 和 Flat 环境模式恢复。未测 Editor/Player 性能，未获得第二轮人工美术认可，状态仍为 QA_PENDING。

下一轮聚焦缺失运算连接的进一步解析，以及 UE Basic 曝光、分段调色和 Film 曲线的同镜头校准；随后才评估向 OCC 迁移。

## 可复核数据

`pak_inspect.py`：索引、按过滤条件只读提取。

`parser/Program.cs`：CUE4Parse 1.2.2 / Conversion 1.2.1 解析地图和导出网格、材质、纹理。`stage` 导出地图引用；`cube` 导出环境六面条带。

`pak_index.json`：107227 项索引，SHA1 为 `2946e51c60dcdf6ab15550415d9f6dc66417cd15`。

参考：[官方 Steam 图库](https://store.steampowered.com/app/921570/OCTOPATH_TRAVELER/)、[开发者 Unreal Engine 访谈](https://www.unrealengine.com/spotlights/octopath-traveler-s-hd-2d-art-style-and-story-make-for-a-jrpg-dream-come-true)。原始截图见 `reference_0.jpg`。
