# HD-2D 战斗信息适配验证（2026-10-08）

范围：当前学院庭院可玩迁移样板。没有改变玩法规则、正式入口、源图或正式资产状态。

实现：新增 Hd2DBattlefieldTactics，复用 CombatBattlefieldRangeGraphic、CombatEnemyMoveRouteGraphic 与真实 BattlefieldCellPresentation。水平世界 Canvas 将预览、威胁和公开路线放在地面上；屏幕信息框复用 CombatIntentLayout 避让身体及其他框。真实意图变化复用 CombatIntentChangeTracker。悬停经真实投影选格驱动预览，退出清理。命令结果使用统一预览格反馈。

跳字：CombatVisualFeedback 以实际单位头部投影起始，保持原有数值、时序和避让；地面命中反馈以逻辑格投影为锚点。单位移除后保留最后面片锚点以显示致命伤害。

实际验证：主工程 Application.dataPath 为 E:/数据库/OCC_Codex/UnityProject/Assets。Unity CLI 连接不可用，使用允许的 Funplay 补充。hd2d-tactics-play-v4 导入编译 READY，无编译错误。通过正常主菜单、角色选择、地图进入 B1 后启用迁移样板；Hd2DCombatReviewAudit 使用真实 Input System 指针事件。最终结果：PASS commands=14 moves=3 attacks=8 projection=30 context=PASS victory=PASS saved=True。审阅静音40秒实机视频和命中连续帧，确认信息框两行容量、伤害跳字及死亡后的意图清除。最终无最近运行错误，退出后 play=False、dirty=False。隔离验证存档作用域在退出时恢复，未报告玩家原档变化。

证据：UnityProject/ReviewEvidence/MainMenuE/hd2d-tactics-latest.png、hd2d-tactics-hit.png、tactics-final-contact-01.png、tactics-final-contact-02.png。交付预览为 ArtSource/MainMenuE_20261008/hd2d-courtyard-review.html，实机视频 hd2d-tactics.mp4。

限制：首场战斗样板验证；不是全部地图、全部状态及多单位密集情境验收。信息框仍为功能样板，未获正式美术认可。尚未替换正式入口。
