# 学院庭院遮蔽烘焙与性能优化

目标：在当前近景样板改善建筑、台阶、栏杆与墙脚承载感，同时减少渲染与每帧分配。仍不替换正式入口、不生成或改色源贴图、不保存场景。

实现：Hd2DAcademyEnvironment 建立8方向半球可见度，距离半径1.4世界单位；静态建筑表面按最多32段细分，遮蔽写入第二UV。学院永久墙加入遮蔽物集合，地表读取同一可见度。新 Hd2DBakedArchitecture shader 保留URP实际日光、阴影、补光、像素纹理UV与硬Alpha，在最终光照乘有限遮蔽因子。此项是静态遮蔽烘焙，并不是完整Lightmapper全局光照烘焙；不得宣称已完成全场景GI。

38个静态建筑模块按材质合并为5组；临时细分Mesh合并后释放，运行时Mesh与Material在OnDestroy清理。原Cube渲染器关闭。单位、独立物件和逻辑状态仍独立更新。Hd2DBattlefieldSample 每帧只生成一套真实呈现模型供战术层复用；战术层不再每帧先关掉全部信息框后重开，死亡与离场仍清理。

性能采样：先测旧样板，再测优化结果；同一电脑、1920×1080、当前近景、正常入口B1首场、静止状态、录屏前。通过ProfilerRecorder采集，预热15帧、采集80帧；绘制为Standard Draw Calls Count与SRP Batcher Draw Calls Count合计，CPU和GPU为Render分类帧时间，GC为Memory/GC Allocated In Frame。这是整帧的编辑器证据，含其他游戏UI和编辑器开销，不等于发布版结果，也不足以证明达到固定帧率。

|指标|旧样板|最终含Alpha裁切版本|
|---|---:|---:|
|主线程采样 ms|16.97|14.21|
|GPU采样 ms|0.70|0.73|
|GC字节/帧|2273001|1533892|
|绘制次数/帧|238.0|172.8|
|HD-2D根节点下可见MeshRenderer|121|88|

早期复测主线程13.56ms与12.15ms，说明编辑器计时存在波动；不以最优采样宣传稳定收益。稳定的绘制计数下降约27%，分配下降约32%。首次静态遮蔽与合批建立240.8ms，仅建立时执行；战场小面片遮蔽另在首次创建或形状变化时更新。该建立时间尚未做离线缓存。剩余约1.53MB/帧分配仍偏高，未来应针对共享呈现适配器继续测量，而不是宣称性能优化全部完成。

验证：主工作树E:/数据库/OCC_Codex，Application.dataPath为主工程。CLI无Pipeline实例，按项目允许的Funplay补充。prepare hd2d-baked-alpha-v3 READY；ShaderUtil.ShaderHasError=False。正常入口实际输入审计 PASS commands=14 moves=3 attacks=8 projection=30 context=PASS victory=PASS saved=True。40秒静音实机录制与连续帧审阅透明地表、技能反馈、移动、路线、意图、伤害与死亡；最近Console错误为零。退出Play，活动场景干净，隔离验证作用域恢复且无玩家原档变化报告。

交付：ArtSource/MainMenuE_20261008/hd2d-courtyard-review.html?v=baked-v3、hd2d-baked-v3.png与hd2d-baked-v3.mp4；保留同镜头旧版。证据UnityProject/ReviewEvidence/MainMenuE/hd2d-baked-v3.png、baked-v3-contact-01.png、baked-v3-contact-02.png。
