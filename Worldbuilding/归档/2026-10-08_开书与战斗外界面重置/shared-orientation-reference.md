# 当前庭院的面片取向

参考：Acquire 与 Square Enix 的《Octopath Traveler II》开发者访谈，Unreal Engine，2023-08-22。
https://www.unrealengine.com/developer-interviews/octopath-traveler-ii-builds-a-bigger-bolder-world-in-its-stunning-hd-2d-style?lang=en-US

访谈明确角色使用2D像素面片、背景几乎完全使用3D，需要专门处理两者的自然结合。探索采用固定相机。访谈未公布默认相机俯角或面片夹角。本轮没有测得原作截图中的精确角度。

OCC实现选择：当前45°正面相机；单位、独立物件、战利品、树木使用同相机取向，面片地面夹角由90°减俯角得出45°。地面与范围水平，建筑立面附着楼体，屋顶与墙等使用真实几何。可见面片保留源图比例，直立面仅投射阴影。URP材质关闭可见面片的阴影接收，防止自身投影产生黑块。

覆盖当前庭院战斗样板，不宣称其他页面完成迁移。书页与书封继续由装订模型和实际动画承担。正式入口仍未切换。总案、本地规格表与机器镜像同步，飞书未写入。

实现验证：17个活跃独立面片与相机一致，17个直立投影面，30个水平地面格子，3组贴合楼体立面；30格投影与右键菜单通过。树冠自遮挡通过URP阴影接收材质关键字修复，墙边横向切断通过正向颜色与深度通道的直立代理深度修复。新着色器当前平台supported=True，无编译诊断。
最终深度修正后的正常开局与实际鼠标输入战斗：PASS commands=14 moves=3 attacks=8 projection=30 context=PASS victory=PASS saved=True。Console无新增错误。32秒录像和动作接触图已保存；录像不构成性能测试。
