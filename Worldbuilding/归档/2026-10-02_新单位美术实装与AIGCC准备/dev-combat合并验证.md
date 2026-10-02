# dev/combat 合并验证（2026-10-02）

用户确认合入 dev/combat。先在 dev/ui 提交本轮新立绘、静态战棋、WebGL 构建与六步首战教学（bf42b03f），再将 origin/dev/combat 的 e3e5d984 合入本机 dev/ui，验证同一结果后同步推送两个分支；本机工作分支保留 dev/ui。

四处冲突均已解决。AGENTS 保留两台电脑各自的主工程路径，并合入远端 Unity CLI 优先规则；单位注册和加载保留本轮新静态资源、全部独立立绘及停用帧动画决定，同时接入远端 prototype_hand 美术。远端学院地表、地图布局及独立像素特效展示场也完整保留。

主工程与活动 CombatPrototype 场景已核对，场景未保存。重新导入编译后 Editor ready，Console 无 error。针对教程、首轮、存档、命令、资源注册、战场格子、物块布局、信息展示及移动反馈的 121 项 EditMode 测试全部通过。Play Mode 正常启动，经继续档案02恢复 B1 战斗，人物、地表和 HUD 正常加载，Console 无 error，退出后回到 Edit Mode。

本机 CLI 为1.0.0-beta.8，完成帮助和命令发现；当前帮助未提供新版 caller/skill 调用标签，编译、测试与运行验证由已核对主工程的 Funplay 补充。测试结果及启动截图见 evidence/combat-tutorial/merge-*。

此轮只合并和推送源码，已交付 H5 仍为合并前经浏览器完整验证的教学包，未重新构建为本次合并结果；尚未上传 AIGCC 比赛。第一阶段既有完整单轮验收、三项失败测试和24/28时序口径待办不因本轮相关测试通过而结案。
