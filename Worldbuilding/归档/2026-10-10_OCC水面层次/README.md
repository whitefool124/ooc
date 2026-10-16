# 雨后庭院水面候选

目标：从正常入口进入 B1 战斗，改善右侧水庭与现有三格积水的水体层次，保留战斗规则、拾取位置和既有像素资产。

观察到的原问题：右侧水庭使用纯色材料，无池底、岸边深浅或波纹响应。

实现：复用现有32×32水色与学院石材纹理，以32 texels/world unit的同一世界网格采样；水庭用水平纸板承载。独立解析波纹法线驱动URP非金属镜面光和既有局部环境反射，岸边及三块踏石距离决定浅水过渡。没有从颜色明暗推断高度或粗糙度，也没有新增发光补亮。

运行读回：四处水面采用同一Shader，纹理Point采样；池深参数0.5，战术积水0.12，均为美术设置的光学近似。Shader supported且无编译消息，30格投影回查通过。1920×1080、960×540均为实际帧缓冲截图；8秒波纹录像只用于运动观察，不是性能计时。

首轮战斗运行完成14次指令、3次移动、8次攻击，到达胜利并释放全部水Renderer与材料；验证脚本在胜利后误访问已销毁的战场，产生MissingReferenceException。修正检查仅在Active战斗阶段执行，保留first_combat_failure.json，重跑结果见combat_outcome.json。

限制：水底是opaque吸收混合近似，尚非真实屏幕空间折射；反射来自既有局部Cubemap，尚非精确平面倒影，亮点仍偏尖锐。全场像素密度清单、人工审美和正式Player 60FPS/P95预算未验收，状态QA_PENDING。工具操作日志有文件替换警告；QuickInstaller存在已观察到的非OCC程序集路径错误，Console不能宣称完全干净。

重跑入口时，Hd2DCombatReviewAudit.Enter的主菜单按钮中心射线未命中；记录为入口验证辅助器失败，随后使用既有AcademyJourneyAudit正常UI入口完成新游戏与B1选择，没有内部构造战斗状态。不能将这次失败省略或计为通过。

最终重跑：PASS commands=14 moves=3 attacks=8 victory=PASS saved=True。结束Play后主工程路径正确、未编译中，原GameView索引3（1080p）已恢复，唯一CombatPrototype场景clean，水材料0。审美总门槛返回失败是QA_PENDING预期结果，不代表完整品质获批。Console仍保留入口辅助器失败和QuickInstaller异常；本次源码导入编译成功。

本轮只修改渲染源码及对应局部验证脚本，未保存场景、替换正式图像或变更玩法口径。完成后解锁水面亮部、折射和倒影的进一步打磨。
