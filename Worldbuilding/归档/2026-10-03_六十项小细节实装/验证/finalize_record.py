"""Update execution ledger and audit the latest buffered runtime frames."""
from pathlib import Path
import csv, json
import numpy as np
from PIL import Image

root = Path(__file__).resolve().parents[1]
updates = {
1: ('LOCAL_VERIFIED','实际1×／2×当前标记保持尺寸'),
2: ('LOCAL_VERIFIED','实际可达节点悬停路线及退出清理，无射线拦截'),
3: ('WIRED_PENDING','真实到达事件回弹已接线，尚未实际进入新节点验收'),
4: ('COMPONENT_VERIFIED','两项拖动测试覆盖6px上限、动画0、缩放后清理'),
7: ('LOCAL_VERIFIED','实际背包空槽淡印可见'),
8: ('PARTIAL_VERIFIED','护目镜匹配头部剪影及清理通过，不兼容分支未独立验证'),
10: ('WIRED_PENDING','金属亮度／粗糙度掩码已编译，待金属悬停像素采样'),
11: ('LOCAL_VERIFIED_PIXELS','18帧实机镜片采样，外框不变，动画0停止且购买可点击'),
13: ('ART_QA_PENDING','托盘独立原料网格与PNG合格，实际托座／应用缺失'),
14: ('APPROVED_PENDING','依赖13托座，不用卡片空白冒充货架'),
15: ('ART_QA_PENDING','硬币碟原料合格，尚未实际放置／交易演出'),
18: ('PARTIAL_VERIFIED','实际商店退出铃派发，环境交叉淡出接线，未做听感审核'),
23: ('ART_QA_PENDING','螺丝盒原料合格，抽屉摆放／交互未实装'),
24: ('LOCAL_VERIFIED','实际工具与纸页标签点击派发不同声音'),
26: ('ART_REJECTED_PENDING','医用仪表独立生成3轮置信度不足，停止未入库'),
27: ('ART_QA_PENDING','窗帘原料合格，真实窗边支持／下摆演出未完成'),
28: ('PARTIAL_VERIFIED','实际杯类餐食通过；盘／碗只有映射和资源检查'),
33: ('PARTIAL_VERIFIED','图鉴真实装备图标绑定材质并清理，待单独扫光像素验收'),
34: ('LOCAL_VERIFIED','实际分类切换返回恢复0.37滚动位置'),
35: ('LOCAL_VERIFIED','真实现有音量控件释放试听；未刻意改变玩家音量'),
36: ('LOCAL_VERIFIED','单试听声源，退出设置淡出后0.12秒内停止'),
37: ('ART_QA_PENDING','盆栽原料合格，独立叶层及实际背景放置未实现'),
38: ('ART_QA_PENDING','挂旗原料合格，真实挂点及共用风场未实现'),
42: ('WIRED_PENDING','房间环境交叉淡入淡出，医务室无机器声，待实际混音审核'),
55: ('AUDIO_READY_PENDING','推拉碰撞素材就绪，移除猜测播放，等待真实语义事件'),
57: ('WIRED_PENDING','部分碰撞声100ms内最多8次并递减，待多目标实机混音'),
58: ('LOCAL_VERIFIED','实际成交后PriorityGain降低断言通过；未做声学录音'),
59: ('LOCAL_VERIFIED','实际非法落位／外部取消反馈不同，无成功落位声'),
60: ('LOCAL_VERIFIED','商店／工坊／医务室实际入口派发不同声音，未做听感审核'),
}
ledger = root / '执行状态.csv'
with ledger.open(encoding='utf-8-sig', newline='') as f:
    rows = list(csv.DictReader(f))
for row in rows:
    number = int(row['编号'])
    if number in updates:
        row['状态'], row['说明'] = updates[number]
    if number == 34:
        row['模式'] = '共用'
assert len(rows) == 60 and len({r['编号'] for r in rows}) == 60
with ledger.open('w',encoding='utf-8-sig',newline='') as f:
    writer = csv.DictWriter(f,fieldnames=['编号','模式','状态','说明'])
    writer.writeheader(); writer.writerows(rows)

folder = root / '验证/inventory'
images = [Image.open(folder/f'lens-{i:02}.png').convert('RGB') for i in range(18)]
frames = np.stack([np.asarray(im)[292:356,1240:1304] for im in images])
y,x = np.mgrid[0:32,0:32]
u,v = (x+.5)/32, 1-(y+.5)/32
mask = (((u-.328125)/.0625)**2+((v-.421875)/.09375)**2 <= .70) | (((u-.671875)/.0625)**2+((v-.265625)/.09375)**2 <= .70)
mask = np.repeat(np.repeat(mask,2,axis=0),2,axis=1)
changed = np.any(frames != frames[-1],axis=(0,3))
count, outside = int(changed.sum()), int((changed & ~mask).sum())
assert count > 0 and outside == 0, (count,outside)
ys,xs = np.where(changed)
evidence = dict(crop=[1240,292,64,64], frames=18, changed_screen_pixels=count,
    changed_outside_lens_pixels=outside, change_bounds=[int(xs.min()),int(ys.min()),int(xs.max()),int(ys.max())],
    frame_and_alpha_contour_unchanged=True,visible_lens_effect=True,
    note='Actual buffered runtime screenshots. Content inset 4px; compare inactive final frame. Original 32px sprite shown at 2x.')
(folder/'lens-pixel-evidence.json').write_text(json.dumps(evidence,ensure_ascii=False,indent=2),encoding='utf-8')
times = [float(line.split()[0]) for line in (folder/'lens-times.txt').read_text().splitlines()]
durations = [max(20,round((times[i+1]-times[i])*1000)) for i in range(17)]+[1100]
gif = [im.crop((1216,268,1328,374)).resize((448,424),Image.Resampling.NEAREST) for im in images]
gif[0].save(folder/'lens-runtime.gif',save_all=True,append_images=gif[1:],duration=durations,loop=0)
print(json.dumps({'ledger_rows':len(rows),'lens_changed_pixels':count,'lens_outside_pixels':outside}))
