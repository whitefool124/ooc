# 九宫格纸页与紧凑整备

用户否定碎裂边框，要求容器更大、更紧凑，并批准使用可适配矩形的通用边框。此前portrait_frame裁切后分别建立1/2/3像素border，会截掉角部图形；本轮完全移除该代码和附加边框Image，使用独立内置生图的完整纸页卡。32×32原生、8色、硬Alpha，主体29×29，主体格网置信度1.9324。四角固定5px切片，边条与纸面平铺，保留一套Sprite，不为每种宽高生成资产；Image.Type.Tiled+fillCenter，Point。按世界Canvas尺度补偿pixelsPerUnitMultiplier，使各容器的物理边宽一致。仅支持大于双侧固定边宽的矩形，极小元素不强套该纸页。

正式资源仍需人工审美确认，本素材为QA_PENDING，源图不进入Unity；Editor只读取解码后的候选delivery。没有沿用此前连续三次拒收的空心边框来源，新的完整装订纸页为不同设计与新来源。

装备区放大20%、角色档案与背包15%、术式区域8%；装备格相对中心间距压缩10%。战术栏拆到桌面独立世界Canvas，避免原父区放大后落入桌面；档案册前移避免遮术式第七槽，装备页不摆无功能装饰盒。背包计数放入纸页标题，纸面略暗以显出亮格子。小按钮文字在内部居中并做12–16字号自适应，移除重复的按钮内槽位副文本，目标槽位仍由页内标题显示。

应用证据：UnityProject/ReviewEvidence/MainMenuE/compact-bound-equipment-20261009.png 与 compact-bound-spells-20261009.png。隔离运行，不等于全游戏界面迁移。真实InputSystem鼠标跨帧验证背包拖拽、亮度拖动保存、0关闭、跨页记忆、术式选择、档案册点击和返回地图；验证源代码另附。未保存场景或Prefab。

CLI检查无OCC可达实例，保持已确认的Funplay后备，不改其他项目。结束恢复原CombatTestArena Edit Mode与runInBackground=false。其余页面仍待逐页迁移，正式资源与飞书同步未完成。
