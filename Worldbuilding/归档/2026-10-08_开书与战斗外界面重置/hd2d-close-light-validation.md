# 学院庭院近景与光照验证

用户要求：相机更近，照明延续八方旅人／歧路旅人的 HD-2D 方向。

调整限于迁移样板运行时，不改正式入口、原生源贴图、逻辑格或场景文件。Hd2DBattlefieldSample 相机适配的最小可见高度从9.8改为7.8，距离约缩短20.4%；中心从地图中线后1格改为后0.5格。45°俯角、0°水平朝向与24°视野保持。全部关联信息沿用真实相机投影。

日光强度2.15、暖色，冷补光强度0.48；投影强度0.82并使用URP高质量软阴影。样板独立Volume包含ACES、适度对比／饱和度、冷影暖高光、阈值1.25强度0.18的Bloom、轻暗角，以及仅在相机距离加3.5之后开始的Gaussian景深。互动地面深度位于清晰区。水面运行时材质提高平滑度到0.72。没有用贴图重着色代替新美术，也没有宣称复刻原作参数。UI不经过场景Volume。

验证：主工作树与Application.dataPath确认一致；活动场景CombatPrototype.unity无脏状态。CLI报告无Pipeline实例，按项目允许的Funplay补充。prepare hd2d-close-light-v1为READY且无编译错误。运行时相机局部位置(3,13.47,-9.97)，后处理开启、独立layer15、6个Volume组件。正常开局进B1后的实际输入审计结果PASS commands=14 moves=3 attacks=8 projection=30 context=PASS victory=PASS saved=True。完整40秒静音录制与连续帧已审阅。退出后play=False、dirty=False；无最近运行错误与原存档变化报告。

交付：ArtSource/MainMenuE_20261008/hd2d-courtyard-review.html?v=close-light-v1、hd2d-close-light.png、hd2d-close-light.mp4。证据：UnityProject/ReviewEvidence/MainMenuE/hd2d-close-light-first.png、close-light-contact-01.png、close-light-contact-02.png。页面保留上一版较远镜头供对比。

限制：仍为首场战斗迁移样板；信息框美术、庭院材质细节与其他地图未完成正式验收。
