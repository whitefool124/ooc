# 持续目标：六类战场整体光影

用户2026-10-14更新目标：树影采样纹理／接触阴影 → 主体与外围明暗层次 → 夜景原像素发光及局部照明 → 六类环境光色统一。兼顾减少大面积重复与块感，保留32PPU、格边、逻辑地图、玩法与清晰战斗信息。每轮只必要编译、局部运行和少量实际图，除明显性能问题外不例行全套验收。目标active，所有新视觉仍QA_PENDING。

## 连续遮挡统计候选

上一版12个点采样的有效遮挡数量与均深会随texel边界跳变，导致滤波半径随片元跳动，树影出现颗粒。本轮搜索位置和25点tent核不变，改用每个位置四个原始深度邻居，先分类有效遮挡，再按双线性权重累计距离及有效权重；没有把天空清除深度与树叶深度混成虚假遮挡。

使用URP锁定core的GATHER_RED_TEXTURE2D，一次收集四邻居。邻居顺序按Microsoft HLSL文档核对：(-,+),(+,+),(+,-),(-,-)；https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/gather4--sm5---asm- 。搜索指令仍为12次Gather，读取的texel更多，不声称零成本或性能预算通过。D3D11为当前验证设备，其他平台未验收。

只修改所属战场的阴影候选，不改原图、PPU、相机、格边、玩法或全局书桌。修改前代码在source_before/，上一版实际白天图previous_candidate_day.png；新三时段及同帧原URP阴影对照在continuous_blocker_shadows/。仍需实际图判断收益，不由算法名称宣称完成。

## 继续修正滤波条纹

Gather有效遮挡统计的第一次实际图仍有条纹，已保存gather_only_trial/，只作为中间研究，不作为最终验收。最后5×5核在最大半径时相邻采样间隔达到2个atlas texel；即使坐标连续，某些相位仍会漏取一部分原生深度值。

本轮改为URP原生7×7高质量tent（16比较fetch）的完整像素覆盖，按测得遮挡距离在单次双线性接触比较和原生tent之间连续混合，最终比较fetch 17次，替代25次自定义网格。仍是局部几何阴影候选，不增加全屏模糊、阴影贴图或shadow camera；不是精确面积光源积分，按实际效果评审。

修正前HLSL在gather_only_before_native_filter.hlsl。后台Editor停止并确认自建进程38800退出后重新载入，确保本轮源代码、Shader依赖和截图归档路径全部重新导入；不关闭用户的ZaoShen Editor，不聚焦窗口。

## 本轮局部结果

新启动后台Editor已完成导入编译，庭院白天、夕阳和雨夜三张实际运行截图及同帧原阴影对照位于continuous_blocker_shadows/。本次27个材质启用候选；Shader依赖指纹9700a3c78c178a8984893a7ac5ac8baf。Console无新增错误，退出Play后CombatPrototype场景isDirty=false，未保存场景或替换正式原图。相关代码git diff --check通过。

shadow_before_after.png为同区域最近邻放大展示：上一版树影内部条纹在本轮静帧中已不再明显，长椅脚边接触仍清晰。仅证明本轮庭院静帧局部改善；六类环境、运动、性能预算及完整旅程没有在本轮重新验收，人工审美仍待确认，保持QA_PENDING。

持续目标保持active。下一步优先拉开战斗主体、外围和远景的明暗层次，再推进夜景局部照明与六类环境统一。

## 战场明暗层次候选

问题依据：continuous_blocker_shadows/day.png中远立面、外围铺地与战斗区域亮度接近；两路均匀补光与环境漫反射叠加，削弱朝向与深度层次。本轮降低大范围填充，保留站立纸板的水平轮廓补光和主光，以保住单位阅读；非交互外围原有空气衰减上限从20%调整至32%（室内14%至20%），完整逻辑格及0.75m安全边仍排除衰减。

默认室外Fill .45→.30，温室.42→.28，室内.48→.36；天空线性辐照系数室外.24→.18／室内.16→.14，地反射.065→.05；夕阳Fill .55→.32，夜景.65→.38。主光、人物水平轮廓补光、源发光与局部灯不变。涉及Hd2DBattlefieldProfile.cs、Hd2DBattleAtmosphere.cs；前版源文件在light_hierarchy_source_before/。截图菜单只增加本轮归档入口，不保存场景、改源图或影响书桌。

局部完成标准：编译通过，庭院三时段实际静帧中单位和战术信息仍可读、外围明度有所退让；人工审美仍待审阅，不晋升APPROVED_BASELINE。此步之后继续夜景可见光源照明与六类统一。

### 调整依据与实际静帧

第一轮仍采用6m外围衰减过渡，可见背景变化较弱；已保留light_hierarchy_first_trial/。改为3m连续过渡，仍以世界坐标逻辑格边+0.75m为起点，不沿屏幕或单位位置制造阴影。Hd2DBattleAerialPerspective.shader修改前存于light_hierarchy_source_before/。

本轮最终庭院三时段截图在light_hierarchy/，灯光记录包含实际运行Light.intensity，白天主光2.3／广域补光0.30／水平轮廓0.55。源文件已实际重编译completed、failed=false。静帧同位置采样亮度（RGB Rec709系数，仅显示值比较，非物理照度或严格同动画帧）：白天后方铺地110.7→92.4、右侧外围131.9→120.2、中央地面133.3→125.7；夕阳后方58.7→47.7；雨夜后方55.2→42.6；所采UI块183.6→183.6。截图中主体轮廓、战术预览及格边仍可读；不把细微像素差异全部归因于本改动，云影、雨和角色帧可能不同。

六类静帧菜单Six light hierarchy families仅采代表性真实关卡／配置，不运行全量品质、完整旅程或性能套件；证据在light_hierarchy_families/，仍为人工待审候选。

### 本轮六类局部结果

六份capture.json均已生成：AcademyCourt、Conservatory、StoneRoad、RuinSite、InteriorHall、AetherTower；已查看light_hierarchy_families_overview.png（仅展示用缩略拼图，原始1920×1080图未改）。庭院／温室主体保持亮，石路和遗址外围退暗，室内及塔的格边、单位、预览和数值仍可读。尚不证明六类夜景／运动／性能或正式正常旅程验收；品质保持QA_PENDING。

最后Console error=0、compilationFailed=false、compiling=false；Play已停止，CombatPrototype isDirty=false。仍有已有的额外点光阴影atlas降采样提示，不据此宣称性能通过。本轮没有新增灯、阴影camera或全屏pass，修改现有参数与现有空气pass。相关源文件git diff --check通过。后台OCC Editor进程31416保留为下一轮持续开发的已核验实例，状态ready，不操作用户的其他工程。

接下来优先改善夜景人物受光与可见发光源周围的照明融合，并继续六类光色差异；整目标保持active。

## 原像素发光与夜景照明融合

light_hierarchy/rain_night.png中晶簇底部蓝光较平、较亮；运行绑定同时存在tile晶簇光与Hd2DPixelEmission第二点光。法杖的已绘制火焰原像素发光与暖灯已接入，但暖灯未登记时段变化。

本轮移除原像素组件重复蓝灯，保留tile-owned晶簇灯及其真实存活／耐久启停；位置由地面上0.5m抬至0.85m，默认1.8／2.3m不变，夜间目标3.8／3.2m调整为2.7／2.9m。晶体发光区域／原始颜色／gain 8、5、6.5不变，没有新增发光图。法杖暖灯仍随原像素组件移动和启停；登记独立昼夜端点，白天1.2／1.9m，夜间1.8／2.4m，沿已有时段过渡连续插值。RegisterLocalLight现在保存每盏灯的夜间目标，避免把不同光源都拉到同一亮度与半径。

前版源码在night_light_source_before/；新的庭院三时段局部截图night_light_integration/。仅编译、实际静帧、灯强记录与Console检查，不触发完整旅程或性能套件。PPU、源图、源发光区域、棋盘、格边、玩法和UI不变，未保存场景。QA_PENDING，整体目标active。

### 夜景照明局部结果

night_light_integration/day.png、sunset.png、rain_night.png均为本次实际Editor Play静帧；雨夜灯记录仅有一盏晶簇Point，2.7／2.9m，以及一盏法杖Point，1.8／2.4m，证明重复晶簇灯已移除并独立夜间端点已生效。已查看实际雨夜图：晶簇底部过亮蓝色池收敛，法杖对邻近墙面和脚边的暖光更明显；原生发光区域仍清晰。英雄最暗处细节仍弱，后续人物受光继续打磨，不宣称本目标完成。

编译completed failed=false，最终Console error=0、compiling=false，Play已停止。只证明庭院三时段局部运行；六类其它关卡本轮未重新采图，移动、切换、完整旅程、性能预算与人工审美未验收，维持QA_PENDING。源码差异检查通过，没有场景保存或正式原图替换。

## 夜景人物阅读补光

night_light_integration/rain_night.png中主角深色衣服的层次较弱。材质沿用源图和UnitTint，补光主因是夜景背侧主光与较低正面光能。沿已有水平轮廓灯调整夜间端点：颜色(.68,.77,.94)→(.78,.84,.97)，强度.65→.95，仍低于主光1.4；地面水平法线与该光方向正交，不直接给水平铺地加照度。没有提高全局曝光、改变贴图或增加单位自发光；朝向真实光方向的物件和远立面也会受影响，因此需实际图检查主体／远景层次。白天／夕阳不变。

变更Hd2DBattleAtmosphere.cs；旧代码及截图菜单在night_unit_source_before/。局部完成标准为编译／Console正常、实际雨夜图人物衣服／脸部较清楚并保留夜景地面层次；night_unit_readability/提供实际三时段原图和运行灯值，仍待人工审美。

第一轮night_unit_first_trial/仅提高水平补光，静帧主角层次提升仍小；未把这次效果描述为完成。进一步纠正夜景主光方位：俯角48°及强度1.4保留，方位125°→85°，从背侧调整为侧前方。水平地面与主光的夹角未变，但阴影平面方向随真实光源旋转，人物面片正面的入射角更合适。实际运行灯Euler一起登记到*_lighting.json，不只依赖代码声明。雨夜仍冷色，曝光、原像素发光、附近点光、PPU与棋盘不变。

### 夜景人物局部结果

已查看最终night_unit_readability/rain_night.png及night_unit_comparison.png：主角脸部、手臂及深色服装的亮面比night_light_integration更清楚，保留深色轮廓与夜景层次。对比图为真实静帧同区域2×最近邻展示，原始PNG未改；角色动画相位不完全相同，不冒称逐像素同姿态对照。实际主光Euler约(48,85,0)、I1.4，水平轮廓I.95，点光仍晶簇2.7／法杖1.8。整体雨夜阴影方向按月光真实旋转。

白天、夕阳、雨夜三时段截图已生成，编译completed failed=false，最终Console error=0、compiling=false、场景isDirty=false、Play停止；相关源码git diff --check通过。人工审美未确认，六类其它夜景／运动／性能／完整旅程未在本轮验收，QA_PENDING。下一步继续六类光色与空间氛围、外围重复和块感，整体目标active。

## 六类环境与外围土面

在本轮检查发现：左侧花圃实体种植土顶面仍沿用完整32×32 earth_b含格框；之前外围无框铺地未覆盖该实体顶面。导致土床连续出现方形边缘与同一个纹样。修正保留该薄实体的侧面及雨接触／遮蔽Bounds，移除旧顶面，改为同一y=.135m的水平原生土面；复用4张已入库earth变体的28×28内区及相同裁切的配套法线／粗糙度，世界UV32/28，密度32PPU。按28/32m周期确定变体，不在周期中途换图；外围已有GardenCell也接入相同4变体，仍是原逻辑之外的环境，未添加占格或碰撞。

不变更源图、Importer、GUID或正式资产，不把组合复用冒称独立生图。前版Hd2DAcademyEnvironment／Profile／菜单在environment_finish_source_before/。本轮Runtime新增一个花圃土面mesh、四个小材质组，复用已有外围渲染；原顶面从合并建筑中移除，避免重叠Z-fighting。实际CPU/GPU性能预算未测，不能宣称性能通过。

共用光色覆盖：庭院既有暖日光保留；温室主光偏中性暖且环境补光稍偏植被冷绿；石路主光偏冷中性；遗址主光更暖、补光冷灰；室内象牙暖／以太塔冷蓝沿用。只改变真实灯色与对应环境辐照，保持共用曝光／主光方向和主体阅读层级，不改贴图重着色。六类当轮实际静帧environment_finish_families/，只局部编译与静帧检查，QA_PENDING。

### 六类环境当轮结果

六类capture.json均生成，实际关卡依次为first_b3_rain_prism_court／first_battle_greenhouse_collection_room／rail_patrol／depot_wreck／calibration_lockdown／transmission_tower；注册土壤变体4、外围铺地8（登记数不等于每类都显示种植土）。已查看实际庭院全图与environment_finish_overview.png：左侧土床原先明显的整格框消失，原生团块由四变体组合，树／长椅／花箱接触仍正常；六类战斗格边、单位与预览清楚，石路偏冷、遗址偏暖、室内／塔仍维持各自光色。soil_bed_comparison.png仅裁取同区域实际截图用于展示，原图未变。

编译completed failed=false，最终Console error=0、compiling=false；Play停止、CombatPrototype isDirty=false。本轮未测运动／性能／完整旅程或六类夜景，仍有历史点光阴影atlas警告，不声称性能通过。所有新视觉QA_PENDING，未保存场景或替换原图；整体目标保持active，后续继续外围建筑重复、光影融合和未覆盖时段。

## 局部灯的结构遮挡

用户最新优先级：重复感最后处理，后续回到光线、阴影与六类空间氛围；已完成土面保留，本轮不继续调整重复素材或布局。当前法杖／晶簇点光没有阴影，BakedArchitecture／WetCourtyard缺少_ADDITIONAL_LIGHT_SHADOWS分支，不能遮挡墙体后方的局部照明。

本轮接入锁定URP原生点光阴影，256px每面、Low软滤波、bias .035／normalBias .035、near .05m，保留光色／亮度／范围。场景-owned pipeline开启RenderingLayers；结构接收体保留默认bit1并加bit2，局部灯照明层仍默认1、shadowRenderingLayers=2，仅墙体等真实几何遮挡；单位／物件直立投影代理仍仅bit1，继续投主光阴影，不从不同几何遮挡自己的贴片发光源。Shader新增局部关键词_OCC_LOCAL_LIGHT_SHADOWS，只在所属战斗材料启用，关闭关键词仍为旧附加灯行为；未改桌面渲染配置或源图。

前版源码local_shadow_source_before/；local_structural_shadows/三时段有实际开启与同帧关闭局部阴影对照。每点光6张原生shadow face，增加真实阴影渲染成本，未测性能预算，不冒称性能通过。此轮以局部编译、实际静帧、Console与遮挡对照判定候选收益，QA_PENDING，整体目标active。

### 局部结构阴影实际结果

已查看local_structural_shadows/rain_night.png及local_shadow_comparison.png。同帧关／开局部阴影，变化集中在墙后暖光遮挡：墙脚出现跟随几何的冷色三角阴影，晶簇与法杖原像素发光不变。雨夜RGB最大通道差>4的像素13961，bbox(1035,452)..(1362,821)，HUD区域差=0；法杖邻域平均绝对差约2.991／晶簇约0.163（仅显示值对比，不是光度）。对比图是实际同帧局部裁图，原始PNG未改。

当轮庭院白天／夕阳／雨夜和各自关局部阴影对照已生成；灯记录Soft、custom256，编译completed failed=false，Console error=0，Play停止、CombatPrototype isDirty=false。新视觉仍QA_PENDING，未测性能预算／运动／六类其它关卡或正常完整旅程；不由技术分支或帧差宣称全目标完成。后续继续六类光影空间氛围，重复感按用户最新要求最后处理。

## 六类环境的时段照明

现状检查发现Hd2DBattleAtmosphere.Preset把夕阳、夜景主光／补光强度写成同一个绝对值，覆盖了Profile中的环境角色。夜景天空／地反射也统一绝对颜色，温室、石路、遗址、室内、塔的差异易丢失。

本轮Profile集中声明NightMainColour／SunsetMainColour。时段主光保留环境相对白天强度（主光ratio defaults/2.3，广域填充defaults/.30，人物水平补光defaults/.55）；共用俯角／方位、阴影强度、曝光与Bloom保持既有候选。夜间天空及地面线性辐照跟随同环境NightMainColour.linear及主光ratio（天空.13、地反射.052系数）。不改Source像素、地块、玩法／占格或重复感。

首轮编译发现Initialize字段赋值使用了错误参数名，已修正为this.visualProfile=visualProfile并重新编译completed failed=false。源码前版在family_atmosphere_source_before/。

本轮菜单Six night atmosphere families采6张实际夜景：室外profile.OutdoorClouds对应Rain，其余Clear；这是局部展示配置，不是新增游戏天气规则。切换后等待新局部环境捕获完成，不用白天旧辐照充当夜景证据。night_family_atmosphere/记录实际时段／天气／灯强，人工待审、QA_PENDING，整体目标active，重复感最后处理。

### 六类夜景当轮结果

night_family_atmosphere/六张1920×1080实际夜景均生成。运行记录Night/Rain为庭院、石路、遗址；Night/Clear为温室、室内、以太塔。实际主光/广域填充/人物补光：庭院1.4/.38/.95，温室1.339/.355/.95，石路与遗址1.4/.38/.95，室内1.065/.456/.777，以太塔1.309/.38/.95。已查看night_family_atmosphere_overview.png：温室保留偏绿色环境，室内较柔和、塔偏冷蓝，必要格边、单位与战术信息仍可读；局部灯阴影在当轮场景中正常接入，无新增运行错误。

Console事件历史仍有本轮已修正的CS0103（05:39:26），当前compilationFailed=false、compiling=false、Console groundTruth consoleErrors=0；不能把历史错误条目假称从未存在。Play已停止、CombatPrototype isDirty=false，源码diff --check通过。六类夕阳未在本轮重新采图，运动／性能／完整旅程未验收；候选QA_PENDING、人工审美待定，不宣称全目标完成。重复感仍按用户最新要求最后处理。

## 六类夕阳局部收尾

按剩余光影范围查看同六类夕阳真实静帧；本轮扩展已有手动截图菜单支持Sunset/Clear，等待时段切换后的新环境采集完成。未改运行时灯光值、原图、格边、场地或重复素材。前版截图工具保存在family_atmosphere_source_before/Hd2DManualFamilyStills_before_sunset.cs，输出sunset_family_atmosphere/。只6张局部实图／Console检查，不跑全量品质、性能、运动或旅程；审美仍待用户查看。

### 六类夕阳实际结果

sunset_family_atmosphere/六份capture.json均为Sunset/Clear，局部漫反射更新后才截取。实际主光／填充／人物补光：庭院2.7/.32/.65，温室2.583/.299/.65，石路及遗址2.7/.32/.65，室内2.054/.384/.532，以太塔2.524/.32/.65。已查看sunset_family_atmosphere_overview.png：暖色受光面与冷色阴影可区分、主角及敌人和战术格边保持可读，没有发现需立即调参的明显问题；没有为了变化而继续改运行时值。

最后编译completed failed=false、当前compiling=false／compilationFailed=false，运行没有新增错误（Console历史仍有前轮已修正的CS0103）；Play停止、CombatPrototype isDirty=false；截图工具diff --check通过。当前夕阳与night_family_atmosphere夜景供集中人工审阅。该证据只为静帧局部呈现，不冒称运动、性能、完整旅程或APPROVED_BASELINE通过。强弱光可读性尚未形成专门对照，人工审美待定，重复感最后处理，整体目标仍active。

## 局部照明强弱收尾

本轮以庭院雨夜同帧对照改变两盏直射点灯为当前100%、40%、0%，固定原像素自发光、主光／补光、现有环境采集、曝光、机位、HUD及动画相位。仅检查邻近墙面／地面在直射点灯弱化后的可读性，不把局部0%称为关闭全部照明或关闭原像素发光，不替代总品质0%/40%/100%完整验收。

只扩展Editor截图工具；当前运行配置没有调参，正式资产不变、未保存场景、不继续处理重复感。源码前版local_shadow_source_before/Hd2DManualBattleScreenshots_before_strength.cs，证据local_light_strength/。局部灯恢复于finally，三时段基础截图仍使用既有候选。QA_PENDING、整体目标active。

### 本阶段局部收尾与人工方向确认

已查看local_light_strength_comparison.png，直射点灯0／40／100%下人物、墙体及战术信息仍可分辨，原像素发光保持；当前最大强度未见明显吞轮廓的问题。此对照冻结局部环境采集，不能当成完整关灯／全场景光强验收。当前局部运行没有新增错误，编译completed failed=false，Play已停止、场景isDirty=false。

用户2026-10-14直接回复“保留当前光影方向”（问题关联六类夕阳、夜景及本轮局部照明对照）。据此保留当前光影参数与方向，不继续盲目增强或柔化。这是人工方向认可，未声称性能、运动、同场景全资产密度清单、完整流程或APPROVED_BASELINE通过；机器批准状态仍QA_PENDING。

光影技术接入及用户要求的局部静帧收尾已完成；整体目标尚有最后的重复感／块感处理，维持active。接下来的工作在保留已认可光影与逐格战斗边缘的前提下，处理非交互外围的重复感，不另开新目标、不扩大玩法范围。
