# 学院庭院 HD-2D 样板验证 2026-10-08

用户目标：永久墙降低、高一点的正面俯视、组合式学院背景。

本次：墙高 1.3→0.65、俯角35→45、yaw0；主菜单35度保持。新增独立立面与树木，经内置生图和原生解码：立面R1置信度1.0414拒绝，R2置信度1.2632通过；树木R1置信度1.2392通过。原料、失败记录与最终原生图位于 ArtSource/HD2DAcademyCourtyard_20261008。两个manifest通过，人工美术审核PENDING，REVIEW_READY，未提升正式美术。

Unity导入与脚本编译通过。Point/Clamp、无MipMap无压缩、32PPU，立面224x128、树96x96；GUID分别05cd96ad3c4b7b14fb0548bcc9430a98、e2b667ee7d89cb7409a0fbd40e9408d0。组合庭院无碰撞、不新增逻辑格。

正常开局与真实InputSystem鼠标流程：PASS commands=14 moves=3 attacks=8 projection=30 context=PASS victory=PASS saved=True。验证使用唯一隔离存档，退出Play恢复。录制40秒686帧只用于视觉证据，不是性能测量。Console无新增错误。8项合同测试通过。

当前限制：只完善可玩迁移样板，正式战斗入口未替换；屋顶、水面、围栏材质仍是几何样板，需要后续独立原料与审美验收。总案、本地规格表、机器合同同步此技术样板口径，飞书未写入。
