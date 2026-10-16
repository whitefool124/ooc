from pathlib import Path
import shutil,json,xml.etree.ElementTree as ET
from html import escape

root=Path(__file__).resolve().parents[3]
folder=Path(__file__).resolve().parent
paths=['Worldbuilding/策划案/OCC_项目总策划案_v1.0.md','Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv','Tools/OCCArt/occ_art_contract_v1.json','Tools/OCCArt/native32_generation_profiles.json','UnityProject/Assets/Game/Runtime/Presentation/FormalCombatHud.cs','UnityProject/Assets/Game/Runtime/Presentation/FormalBattlefieldView.cs','UnityProject/Assets/Game/Runtime/Combat/FormalArtRegistry.cs','UnityProject/Assets/Game/Resources/Config/OccPixelUiV02.json','UnityProject/Assets/Game/Resources/UI/Prefabs/FormalCombatHudShell.prefab','UnityProject/Assets/Game/Tests/EditMode/FormalCombatHudShellPrefabTests.cs']
for name in paths:
    dest=folder/'before'/name;dest.parent.mkdir(parents=True,exist_ok=True)
    if not dest.exists():shutil.copy2(root/name,dest)
data=json.loads((folder/'feishu-before.json').read_text(encoding='utf-8-sig'))
fragment=ET.fromstring(data['data']['document']['content'])
block=next(p for p in fragment if '战场参考视口' in ''.join(p.itertext()))
old=''.join(block.itertext())
new=old.replace('正面416×248，背面按状态内容展开至416×280，使用不透明底色和1px细框','正面480×288，背面480×280，采用学院象牙档案底板、少量黄铜结构件、独立头像框和不透明读数区').replace('顶部偏右（1032，80，824，96）常驻行动顺序','顶部靠右常驻预计行动顺序，右边界1904、顶部80、高192；根据单位数收短，最多七名头像卡及剩余数量，不用大块空面板').replace('正面显示生命、护盾、个人魔力和状态图标','正面使用独立立绘头像、角色名，三项资源在头像右侧对齐，底部显示状态图标；显示生命、护盾、个人魔力和状态图标').replace('悬停头像定位单位、点击聚焦','头像使用专门头肩立绘，不使用战棋纹理；优先读取独立64×64头像，其他角色从已有展示立绘读取头像区域，资源缺失时显示名称占位而不回退战棋。64原生头像在1920与960分别以2倍与1倍显示。当前行动者以青色标记和“当前”文字突出，敌我标记与当前、选中和预览变化分别显示；同类单位使用顺位号区分。悬停头像显示单位名、当前行动值、速度和预计顺位并定位单位，左键或键盘确认聚焦，右键不执行；键盘焦点与鼠标悬停独立。重排按单位身份保留卡片，以整数像素短位移过渡，动画关闭时立即更新；合法预览显示绿色提前或红色延后箭头，取消恢复；死亡和离场清除定位，阻塞模态禁止头像操作').replace('肉鸽无上限护盾只显示当前值','肉鸽无上限护盾只显示当前值；资源归零、满值和超出常规条长时保持读数完整，状态溢出使用数量提示，悬停查看全部，详情翻面保留状态与被动说明')
(folder/'layout-new.txt').write_text(new,encoding='utf-8')
(folder/'layout.xml').write_text('<p>'+escape(new)+'</p>',encoding='utf-8')
(folder/'patch.json').write_text(json.dumps({'id':block.attrib['id'],'old':old},ensure_ascii=False),encoding='utf-8')
print(block.attrib['id'])
