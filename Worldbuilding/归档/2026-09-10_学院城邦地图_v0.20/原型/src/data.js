const METERS_PER_LON_AT_EQUATOR = 111320;
const METERS_PER_LAT = 110540;
const WIDTH = 3200;
const HEIGHT = 2800;

export const mapDimensions = { width: WIDTH, height: HEIGHT, areaHectares: 896 };
export function toLngLat([x, y]) { return [x / METERS_PER_LON_AT_EQUATOR, -y / METERS_PER_LAT]; }
const close = (points) => [...points, points[0]];
const geometry = (type, coordinates) => ({ type, coordinates: type === "Polygon" ? [close(coordinates).map(toLngLat)] : type === "Point" ? toLngLat(coordinates) : coordinates.map(toLngLat) });
const makeFeature = (type, coordinates, properties) => ({ type: "Feature", properties, geometry: geometry(type, coordinates) });

const zoneDefs = [
  ["Z-N", "北部郊野外环", "郊野／外环", [[0,0],[3200,0],[3200,240],[0,240]], "北门、防灾林、检疫与生态维护"],
  ["Z-T", "教学区", "教学区", [[0,240],[1480,240],[1480,1350],[0,1350]], "历史学院街巷、教学庭院与扩建预留"],
  ["Z-L", "生活区", "生活区", [[0,1350],[1480,1350],[1480,2360],[0,2360]], "居住街坊、公共庭院、消防退界与河岸步道"],
  ["Z-C", "双核中庭带", "中庭区", [[1480,240],[1770,240],[1770,2360],[1480,2360]], "中央河湖、桥头广场、防洪退界与公共轴线"],
  ["Z-M", "城市市集区", "市集区", [[1770,240],[3200,240],[3200,1350],[1770,1350]], "市政、商业、文化、旅宿和公共交通"],
  ["Z-W", "工坊区", "工坊区", [[2200,1350],[3200,1350],[3200,2360],[2200,2360]], "制作、能源、货运、公共工程和消防间距"],
  ["Z-S", "封存区", "封存区", [[1770,1350],[2200,1350],[2200,2360],[1770,2360]], "许可边界、隔离缓冲、观察与受控档案"],
  ["Z-O", "南部郊野外环", "郊野／外环", [[0,2360],[3200,2360],[3200,2800],[0,2800]], "体育、战斗课程、水务与城市后勤"]
];
export const zones = { type: "FeatureCollection", features: zoneDefs.map(([id,name,zone,coords,openSpacePurpose]) => makeFeature("Polygon", coords, { id,name,zone,openSpacePurpose })) };

const waterShape = [[1515,0],[1490,300],[1510,620],[1450,850],[1330,1040],[1260,1220],[1280,1430],[1350,1600],[1430,1780],[1460,2100],[1480,2400],[1460,2800],[1710,2800],[1690,2400],[1715,2100],[1770,1780],[1870,1600],[1960,1430],[1980,1220],[1910,1040],[1780,850],[1700,620],[1720,300],[1690,0]];
const terrainDefs = [
  ["G-WATER", "学院城邦连续河湖", "water", waterShape, "北河—中央湖—南河连续水系"],
  ["G-A-SQ", "学院正厅门廊前庭", "paved", [[800,1235],[905,1235],[905,1240],[800,1240]], "紧贴学院正厅外墙并衔接双核主路的门廊前庭"],
  ["G-C-SQ", "市民议政厅门廊前庭", "paved", [[2385,1235],[2515,1235],[2515,1240],[2385,1240]], "紧贴议政厅外墙并衔接城市中央大道的门廊前庭"],
  ["G-W-PARK", "西岸公共步道", "green", [[1200,1350],[1240,1430],[1310,1600],[1390,1780],[1420,2050],[1360,2030],[1330,1800],[1250,1630],[1180,1450]], "河岸公园、防洪与步行连续带"],
  ["G-E-PARK", "东岸公共步道", "green", [[2020,1350],[2000,1430],[1930,1600],[1880,1680],[1940,1700],[2010,1630],[2060,1450]], "河岸公园、防洪与步行连续带"],
  ["G-T-PARK", "旧讲堂街坊公园", "park", [[820,880],[960,880],[960,980],[820,980]], "教学街坊的树荫、候课与短时休息空间"],
  ["G-T-WEST-PARK", "西门前街公园", "park", [[0,420],[130,420],[130,580],[0,580]], "西门入口侧的等候、遮荫与短时集散空间"],
  ["G-L-PARK", "灯庭生活公园", "park", [[600,1550],[720,1550],[720,1660],[600,1660]], "生活区居民与学生共用的小型公园"],
  ["G-M-PARK", "市集口袋公园", "park", [[2620,680],[2700,680],[2700,770],[2620,770]], "市集区步行休息、等候与小型摊位轮换空间"],
  ["G-WORK-PARK", "工坊午休公园", "park", [[2400,1790],[2500,1790],[2500,1860],[2400,1860]], "工坊工人午休、用餐与应急疏散空间"],
  ["G-STADIUM", "学院体育场", "sports", [[220,2480],[880,2480],[920,2720],[190,2720]], "体育课程、比赛与大型集会"],
  ["G-COMBAT", "护障对抗场", "combat", [[1010,2480],[1430,2480],[1430,2720],[1000,2700]], "非致命对抗课程与护障训练"],
  ["G-TACTICAL", "小队战术训练场", "training", [[1810,2480],[2390,2480],[2440,2720],[1780,2720]], "路线、协同与团队战术训练"],
  ["G-SEALED", "封存缓冲庭", "restricted", [[1840,1730],[2140,1730],[2140,2200],[1840,2200]], "封存隔离、观察与消防缓冲"],
  ["G-NORTH", "北门防灾林", "terrain", [[120,40],[720,40],[760,200],[100,200]], "防风、防火和北门集结"],
  ["G-SOUTH", "南部水务恢复地", "terrain", [[2550,2460],[3100,2460],[3060,2720],[2580,2720]], "下游防洪、生态恢复和水务维护"]
];
export const terrain = { type: "FeatureCollection", features: terrainDefs.map(([id,name,kind,coords,description]) => makeFeature("Polygon", coords, { id,name,kind,description })) };

const roadDefs = [];
const addRoad = (id,name,roadClass,width,coords,extra={}) => roadDefs.push({ id,name,roadClass,width,coords,...extra });
// 城市采用正交街网：同组道路保持水平或垂直，仅在水岸、设施边界和受控入口处设置明确直角折点。
// 三条东西城市轴线，跨水段独立建桥。
addRoad("R-NW", "北部学府大道", "主步道", 16, [[0,620],[1506,620]]);
addRoad("B-N", "北部公共桥", "桥梁", 16, [[1506,620],[1704,620]]);
addRoad("R-NE", "北部城市大道", "主步道", 16, [[1704,620],[3200,620]]);
addRoad("R-CW", "学院桥头大道", "主步道", 20, [[0,1250],[1258,1250]]);
addRoad("B-MAIN", "双核主桥", "桥梁", 20, [[1258,1250],[1982,1250]]);
addRoad("R-CE", "城市中央大道", "主步道", 20, [[1982,1250],[3200,1250]]);
addRoad("R-SW", "南部学院大道", "主步道", 16, [[0,2050],[1452,2050]]);
addRoad("B-S", "南部公共桥", "桥梁", 16, [[1452,2050],[1740,2050]]);
addRoad("R-SE", "南部工坊大道", "主步道", 16, [[1740,2050],[3200,2050]]);
// 南北收集路。与东西轴线保持垂直，以真实十字或丁字路口连接。
addRoad("R-W1", "西门学院路", "主步道", 14, [[360,0],[360,2380]]);
addRoad("R-W2", "学院中路", "支路", 9, [[760,0],[760,2380]]);
addRoad("R-W3", "钟楼西路", "支路", 9, [[1120,0],[1120,2380]]);
addRoad("R-E1", "市民东路", "主步道", 14, [[2760,0],[2760,2380]]);
addRoad("R-E2", "议政东路", "支路", 9, [[2350,0],[2350,2380]]);
addRoad("M-V0", "河港纵路", "支路", 9, [[2020,0],[2020,1250]]);
addRoad("W-V0", "封存工坊界路", "支路", 9, [[2180,1250],[2180,2380]]);
// 教学区：平行横街配垂直短巷。
addRoad("T-01", "古讲堂街", "支路", 8, [[0,360],[1440,360]]);
addRoad("T-02", "图书馆街", "支路", 8, [[0,820],[1438,820]]);
addRoad("T-03", "学院南街", "支路", 8, [[0,1050],[1120,1050]]);
addRoad("T-04", "学院北巷", "支路", 7, [[360,500],[1120,500]]);
addRoad("T-V1", "讲堂西巷", "支路", 7, [[560,360],[560,1250]]);
addRoad("T-V2", "河岸教学巷", "支路", 7, [[1250,360],[1250,1250]]);
addRoad("T-V3", "西侧教学巷", "支路", 7, [[180,360],[180,1250]]);
// 生活区：短街坊与公共庭院均服从正交骨架。
addRoad("L-01", "灯庭街", "支路", 8, [[0,1460],[1260,1460]]);
addRoad("L-02", "宿舍街", "支路", 8, [[0,1760],[1260,1760]]);
addRoad("L-03", "生活服务街", "支路", 8, [[0,2200],[1430,2200]]);
addRoad("L-04", "生活南街", "支路", 7, [[360,1900],[1430,1900]]);
addRoad("L-V0", "西门生活巷", "支路", 7, [[120,1460],[120,1760]]);
addRoad("L-V1", "西岸医馆路", "支路", 7, [[520,1250],[520,2050]]);
addRoad("L-V2", "生活中巷", "支路", 7, [[920,1250],[920,2200]]);
addRoad("L-V3", "河岸生活巷", "支路", 7, [[1260,1250],[1260,2200]]);
// 市集区：城市街坊以东西商业街和南北服务街组成。
addRoad("M-00", "北河门街", "支路", 8, [[1730,280],[3200,280]]);
addRoad("M-01", "市政北街", "支路", 8, [[1715,390],[3200,390]]);
addRoad("M-02", "覆顶市集街", "支路", 9, [[1785,830],[3200,830]]);
addRoad("M-03", "公共剧场街", "支路", 8, [[1925,1050],[2760,1050]]);
addRoad("M-04", "旅舍街", "支路", 7, [[2350,1090],[3200,1090]]);
addRoad("M-V1", "市集西路", "支路", 7, [[2180,390],[2180,1250]]);
addRoad("M-V2", "市集东路", "支路", 7, [[2550,390],[2550,1250]]);
addRoad("M-V3", "旅舍纵巷", "支路", 7, [[3040,390],[3040,1250]]);
// 工坊区：较宽且规则，但仍由明确街坊而非整片网格构成。
addRoad("W-01", "总工坊街", "主步道", 14, [[2180,1450],[3200,1450]]);
addRoad("W-02", "材料交换街", "支路", 9, [[2180,1730],[3200,1730]]);
addRoad("W-03", "工坊中街", "支路", 9, [[2180,1900],[3200,1900]]);
addRoad("W-04", "机务南街", "支路", 9, [[2180,2200],[3200,2200]]);
addRoad("W-V1", "工坊纵路", "支路", 8, [[2550,1250],[2550,2380]]);
addRoad("W-V2", "货运纵路", "维护道", 12, [[2910,1250],[2910,2380]]);
addRoad("W-V3", "东侧机务路", "支路", 8, [[3040,1250],[3040,2380]]);
// 封存区：入口数量少，使用直角折线和受控边路。
addRoad("S-01", "封存许可路", "受控路", 8, [[1985,1450],[2180,1450]]);
addRoad("S-02", "隔离北路", "受控路", 7, [[1885,1580],[1885,1600],[2180,1600]]);
addRoad("S-03", "封存值守路", "受控路", 7, [[1770,2260],[2180,2260]]);
addRoad("S-04", "档案北街", "受控路", 7, [[2020,1600],[2020,1700],[2180,1700]]);
// 南部课程与后勤设施：平行横路配垂直入口路。
addRoad("O-01", "南部课程联络路", "主步道", 14, [[0,2380],[1450,2380]]);
addRoad("O-02", "南部后勤联络路", "主步道", 14, [[1720,2380],[3200,2380]]);
addRoad("O-03", "课程设施南路", "支路", 8, [[0,2760],[1450,2760]]);
addRoad("O-04", "战术水务南路", "支路", 8, [[1720,2760],[3200,2760]]);
addRoad("O-V1", "课程场西联络路", "支路", 8, [[960,2380],[960,2760]]);
addRoad("O-V2", "课程场东联络路", "支路", 8, [[2500,2380],[2500,2760]]);
addRoad("O-V3", "西南入口路", "支路", 8, [[100,2380],[100,2800]]);
addRoad("O-V4", "东南入口路", "支路", 8, [[3160,2380],[3160,2800]]);
// 巨蚓外环与内环。跨水段均单列桥槽。
addRoad("TR-OUT-W", "外环巨蚓西线", "生物列车轨道", 10, [[80,180],[1496,180]], { routeId:"TR-OUT" });
addRoad("TR-OUT-NB", "外环巨蚓北桥槽", "生物列车桥梁", 10, [[1496,180],[1715,180]], { routeId:"TR-OUT" });
addRoad("TR-OUT-E", "外环巨蚓东线", "生物列车轨道", 10, [[1715,180],[3120,180],[3120,2620],[1715,2620]], { routeId:"TR-OUT" });
addRoad("TR-OUT-SB", "外环巨蚓南桥槽", "生物列车桥梁", 10, [[1715,2620],[1455,2620]], { routeId:"TR-OUT" });
addRoad("TR-OUT-W2", "外环巨蚓西线", "生物列车轨道", 10, [[1455,2620],[80,2620],[80,180]], { routeId:"TR-OUT" });
addRoad("TR-IN-W", "内环巨蚓学院线", "生物列车轨道", 9, [[560,680],[1180,680],[1180,980],[1050,1250],[1190,1600],[1190,1900],[620,1820],[600,1280],[560,680]], { routeId:"TR-IN" });
addRoad("TR-IN-WB", "内环巨蚓学院桥引线", "生物列车轨道", 9, [[1120,1600],[1315,1520]], { routeId:"TR-IN" });
addRoad("TR-IN-B", "双核巨蚓专用桥", "生物列车桥梁", 9, [[1315,1520],[1920,1520]], { routeId:"TR-IN" });
addRoad("TR-IN-EB", "内环巨蚓城市桥引线", "生物列车轨道", 9, [[1920,1520],[2100,1600]], { routeId:"TR-IN" });
addRoad("TR-IN-E", "内环巨蚓城市线", "生物列车轨道", 9, [[2100,1600],[2050,850],[2650,700],[2920,1120],[2760,1780],[2240,1900],[2100,1600]], { routeId:"TR-IN" });

export const roads = { type: "FeatureCollection", features: roadDefs.map(({coords,...properties}) => makeFeature("LineString", coords, properties)) };

function polygonArea(points) { return Math.abs(points.reduce((sum,[x,y],i) => { const [nx,ny] = points[(i+1)%points.length]; return sum + x*ny - nx*y; },0)/2); }
function pointInPolygon([x,y], points) { let inside=false; for(let i=0,j=points.length-1;i<points.length;j=i++){const [xi,yi]=points[i];const [xj,yj]=points[j];if((yi>y)!==(yj>y)&&x<((xj-xi)*(y-yi))/(yj-yi)+xi)inside=!inside;} return inside; }
function segmentsIntersect(a,b,c,d){const cross=(p,q,r)=>(q[0]-p[0])*(r[1]-p[1])-(q[1]-p[1])*(r[0]-p[0]);const a1=cross(a,b,c),a2=cross(a,b,d),b1=cross(c,d,a),b2=cross(c,d,b);return ((a1>0&&a2<0)||(a1<0&&a2>0))&&((b1>0&&b2<0)||(b1<0&&b2>0));}
function polygonsOverlap(a,b){if(a.some(p=>pointInPolygon(p,b))||b.some(p=>pointInPolygon(p,a)))return true;return a.some((p,i)=>b.some((q,j)=>segmentsIntersect(p,a[(i+1)%a.length],q,b[(j+1)%b.length])));}
function pointSegmentDistance(p,a,b){const dx=b[0]-a[0],dy=b[1]-a[1],l=dx*dx+dy*dy;if(!l)return Math.hypot(p[0]-a[0],p[1]-a[1]);const t=Math.max(0,Math.min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dy)/l));return Math.hypot(p[0]-a[0]-t*dx,p[1]-a[1]-t*dy);}
function segmentDistanceExact(a,b,c,d){if(segmentsIntersect(a,b,c,d))return 0;return Math.min(pointSegmentDistance(a,c,d),pointSegmentDistance(b,c,d),pointSegmentDistance(c,a,b),pointSegmentDistance(d,a,b));}
function polylineDistance(points,line){let min=Infinity;for(let p=0;p<points.length;p++)for(let i=0;i<line.length-1;i++)min=Math.min(min,segmentDistanceExact(points[p],points[(p+1)%points.length],line[i],line[i+1]));return min;}
function zoneAt([x,y]){if(y<240||y>=2360)return "郊野／外环";if(x<1480)return y<1350?"教学区":"生活区";if(x<1770)return "中庭区";if(y<1350)return "市集区";if(x<2200)return "封存区";return "工坊区";}
function orientedRect(cx,cy,w,d,angle=0){const c=Math.cos(angle),s=Math.sin(angle);return [[-w/2,-d/2],[w/2,-d/2],[w/2,d/2],[-w/2,d/2]].map(([x,y])=>[cx+x*c-y*s,cy+x*s+y*c]);}
function chamfered(cx,cy,w,d,cut=14){return [[cx-w/2+cut,cy-d/2],[cx+w/2-cut,cy-d/2],[cx+w/2,cy-d/2+cut],[cx+w/2,cy+d/2-cut],[cx+w/2-cut,cy+d/2],[cx-w/2+cut,cy+d/2],[cx-w/2,cy+d/2-cut],[cx-w/2,cy-d/2+cut]];}

const buildingsLocal = [];
function addBuilding(id,name,zone,coords,frontageRoad,description,extra={}){buildingsLocal.push({coords,properties:{id,name,zone,frontageRoad,description,footprint:Math.round(polygonArea(coords)*10)/10,...extra}});}
const landmarks = [
  ["LM-A1","中庭总钟楼","教学区",1080,1015,72,72,"R-W3","学院时间、调度和公共仪式中心"],
  ["LM-A2","国立学院正厅","教学区",850,1180,150,90,"R-CW","学院治理、典礼和跨国学术事务"],
  ["LM-A3","七系总图书馆","教学区",980,700,170,100,"T-02","教学核心与全城知识档案入口"],
  ["LM-A4","以太观测院","教学区",1260,460,115,115,"T-01","高塔、测量庭院与城市护障观测"],
  ["LM-C1","市民议政厅","市集区",2220,1195,160,90,"R-CE","城市公共治理和市民集会中心"],
  ["LM-C2","中央覆顶市集","市集区",2450,850,190,110,"M-02","商业、物资和日常生活中心"],
  ["LM-C3","双核中央换乘站","市集区",2050,1318,150,90,"R-CE","内外环巨蚓换乘与城市门户"],
  ["LM-C4","湖岸大医馆","生活区",1040,1420,150,85,"L-01","公共医疗、灾害响应和学院教学医院"],
  ["LM-R1","学院大礼堂","教学区",620,560,135,82,"T-04","课程典礼、演说和公共考试"],
  ["LM-R2","河岸公共剧场","市集区",2050,720,130,90,"M-03","面向两岸的公共剧场"],
  ["LM-R3","总校准工坊","工坊区",2460,1380,165,95,"W-01","全城以太仪器校准中心"],
  ["LM-R4","材料与货运交换厅","工坊区",2860,1640,180,100,"W-02","材料交割、货运与装卸调度"],
  ["LM-R5","受控总档案库","封存区",1880,2260,120,60,"S-03","受控文书、遗物与许可记录"],
  ["LM-R6","封存隔离塔","封存区",2120,1450,70,70,"S-01","高风险对象隔离与观察"],
  ["LM-R7","学院体育场主看台","郊野／外环",560,2385,190,55,"O-01","体育场看台、更衣和器材服务"],
  ["LM-R8","护障对抗场控制馆","郊野／外环",1260,2385,170,55,"O-01","护障控制、裁判记录与急救"],
  ["LM-R9","小队战术场指挥馆","郊野／外环",2080,2385,170,55,"O-02","战术课程指挥、器材与观察"],
  ["LM-R10","下游水务与防洪塔","郊野／外环",2830,2385,120,65,"O-02","下游防洪、水务和生态监测"]
];
const roadMap = new Map(roadDefs.map(r=>[r.id,r]));
const forbidden = terrainDefs.map(([, , , coords])=>coords);
function validBuilding(coords, frontageId){
  if(coords.some(([x,y])=>x<24||x>WIDTH-24||y<24||y>HEIGHT-24))return false;
  const z=zoneAt(coords.reduce(([x,y],p)=>[x+p[0]/coords.length,y+p[1]/coords.length],[0,0]));
  if(coords.some(p=>zoneAt(p)!==z))return false;
  if(forbidden.some(poly=>polygonsOverlap(coords,poly)))return false;
  if(buildingsLocal.some(b=>polygonsOverlap(coords,b.coords)))return false;
  for(const road of roadDefs){
    const distance=polylineDistance(coords,road.coords);
    const clearance=road.width/2+2;
    if(distance<clearance-0.1)return false;
  }
  return roadMap.has(frontageId);
}
function validAlignedBuilding(coords,frontageId){
  if(!validBuilding(coords,frontageId))return false;
  return roadDefs.every(road=>polylineDistance(coords,road.coords)>=road.width/2+(road.id===frontageId?4.9:3.5));
}

function orientedChamfered(cx,cy,w,d,angle){const base=chamfered(0,0,w,d,Math.min(14,w/6,d/6));const c=Math.cos(angle),s=Math.sin(angle);return base.map(([x,y])=>[cx+x*c-y*s,cy+x*s+y*c]);}
function placeLandmark(def){
  const [id,name,zone,targetX,targetY,w,d,frontage,description]=def;
  const road=roadMap.get(frontage);let best=null;
  for(let si=0;si<road.coords.length-1;si++){
    const a=road.coords[si],b=road.coords[si+1],dx=b[0]-a[0],dy=b[1]-a[1],len=Math.hypot(dx,dy);if(len<20)continue;
    const ux=dx/len,uy=dy/len,nx=-uy,ny=ux,angle=Math.atan2(dy,dx);
    for(let dist=20;dist<len-20;dist+=16)for(const side of [1,-1]){
      const offset=road.width/2+5+d/2,cx=a[0]+ux*dist+nx*offset*side,cy=a[1]+uy*dist+ny*offset*side;
      if(zoneAt([cx,cy])!==zone)continue;
      const coords=orientedChamfered(cx,cy,w,d,angle);
      if(!validBuilding(coords,frontage))continue;
      const score=Math.hypot(cx-targetX,cy-targetY);
      if(!best||score<best.score)best={coords,score};
    }
  }
  if(!best)throw new Error(`无法为地标找到合法位置：${id} ${name}`);
  addBuilding(id,name,zone,best.coords,frontage,description,{shapeType:"地标切角轮廓",scaleClass:id.startsWith("LM-A")||id.startsWith("LM-C")?"全城级地标":"区域级地标",landmarkTier:id.startsWith("LM-A")||id.startsWith("LM-C")?"city":"regional"});
}
for(const landmark of landmarks)placeLandmark(landmark);

const dailyServices = [
  ["SV-T01","学生第一食堂","教学区",620,980,64,40,"T-03","餐饮","教学服务-大型","面向课程密集时段的学生食堂"],
  ["SV-T02","课本文具店","教学区",300,790,48,30,"T-02","商店","教学服务-小型","教材、纸张、记录工具与普通文具"],
  ["SV-T03","学院日用品店","教学区",900,500,48,30,"T-04","商店","教学服务-小型","学生日用品与基础维护用品"],
  ["SV-T04","学院北站候车厅","教学区",1250,620,64,36,"R-NW","车站","学院站房-标准","连接北部学府大道与巨蚓外环"],
  ["SV-L01","雨灯饭店","生活区",300,1460,56,36,"L-01","饭店","生活餐饮-标准","面向学生、居民与访客的街角饭店"],
  ["SV-L02","河岸面包房","生活区",1180,1460,48,30,"L-01","商店","生活店铺-小型","面包、热食与清晨外带"],
  ["SV-L03","西门杂货店","生活区",120,1760,48,30,"L-02","商店","生活店铺-小型","家庭杂货与临时补给"],
  ["SV-L04","学生用品合作社","生活区",780,1760,56,36,"L-02","商店","生活店铺-标准","学生共同采购与二手用品交换"],
  ["SV-L05","公共浴室与洗衣房","生活区",520,1900,64,40,"L-04","生活服务","生活公共服务-大型","公共洗浴、洗衣与织物烘干"],
  ["SV-L06","灯庭诊所","生活区",920,1900,56,36,"L-04","医疗","生活公共服务-标准","日常诊疗、药品与夜间值守"],
  ["SV-L07","南生活食堂","生活区",760,2200,64,40,"L-03","餐饮","生活餐饮-大型","服务南部宿舍和课程设施"],
  ["SV-L08","生活区站房","生活区",1260,1760,64,36,"L-02","车站","生活站房-标准","生活区与巨蚓内环换乘入口"],
  ["SV-M01","河岸饭店","市集区",2050,830,60,38,"M-02","饭店","市集餐饮-标准","面向河岸剧场与市集客流"],
  ["SV-M02","中央食品店","市集区",2550,830,52,32,"M-02","商店","市集店铺-标准","食品、调味品与家庭补给"],
  ["SV-M03","市民百货店","市集区",3040,830,60,38,"M-02","商店","市集店铺-大型","衣物、器具和常用商品"],
  ["SV-M04","北街旅店","市集区",2350,390,64,40,"M-01","旅店","市集旅宿-标准","短期访客、商旅与家属住宿"],
  ["SV-M05","邮政与票务所","市集区",2760,1050,56,36,"M-03","公共服务","市集公共服务-标准","邮政、寄存、票务与路线问询"],
  ["SV-M06","市集站房","市集区",2180,620,64,36,"R-NE","车站","市集站房-标准","城市核心与巨蚓内环换乘入口"],
  ["SV-W01","工坊食堂","工坊区",2350,1450,64,40,"W-01","餐饮","工坊餐饮-大型","工坊轮班人员集中用餐"],
  ["SV-W02","工具与护具店","工坊区",2760,1450,60,38,"W-01","商店","工坊店铺-标准","常用工具、护具与替换零件"],
  ["SV-W03","工坊医务站","工坊区",3040,1900,56,36,"W-03","医疗","工坊公共服务-标准","工伤处置、洗消与转运"],
  ["SV-W04","工坊换乘站房","工坊区",2760,1730,64,40,"W-02","车站","工坊站房-大型","客货分流和巨蚓外环换乘"],
  ["SV-O01","北门外环站房","郊野／外环",760,90,64,36,"R-W2","车站","外环站房-标准","北门检疫与巨蚓外环换乘"],
  ["SV-O02","南门外环站房","郊野／外环",3160,2720,64,36,"O-V4","车站","外环站房-标准","南部课程设施与巨蚓外环换乘"]
];
function placeDailyService(def){
  const [id,name,zone,targetX,targetY,w,d,frontage,buildingType,sizeSpec,description]=def;
  const road=roadMap.get(frontage);let best=null;
  for(let si=0;si<road.coords.length-1;si++){
    const a=road.coords[si],b=road.coords[si+1],dx=b[0]-a[0],dy=b[1]-a[1],len=Math.hypot(dx,dy);if(len<20)continue;
    const ux=dx/len,uy=dy/len,nx=-uy,ny=ux,angle=Math.atan2(dy,dx);
    for(let dist=18;dist<len-18;dist+=8)for(const side of [1,-1]){
      const offset=road.width/2+5+d/2,cx=a[0]+ux*dist+nx*offset*side,cy=a[1]+uy*dist+ny*offset*side;
      if(zoneAt([cx,cy])!==zone)continue;
      const coords=orientedRect(cx,cy,w,d,angle);
      if(!validBuilding(coords,frontage))continue;
      const score=Math.hypot(cx-targetX,cy-targetY);
      if(!best||score<best.score)best={coords,score};
    }
  }
  if(!best)throw new Error(`无法为日常设施找到合法位置：${id} ${name}`);
  addBuilding(id,name,zone,best.coords,frontage,description,{scaleClass:"日常服务设施",buildingType,sizeSpec,widthMeters:w,depthMeters:d});
}
for(const service of dailyServices)placeDailyService(service);

const quotas={"教学区":111,"生活区":137,"中庭区":0,"市集区":145,"工坊区":104,"封存区":15,"郊野／外环":46};
const counts=Object.fromEntries(Object.keys(quotas).map(k=>[k,0]));
const regularSpecs={
  "教学区":{sizeSpec:"教学区普通模数",w:54,d:38,types:["普通教学楼","小型讲堂","实验准备楼"]},
  "生活区":{sizeSpec:"生活区普通模数",w:50,d:36,types:["标准生活街屋","学生宿舍","家庭公寓"]},
  "市集区":{sizeSpec:"市集区普通模数",w:52,d:36,types:["临街商住楼","标准街铺","旅舍住宅"]},
  "工坊区":{sizeSpec:"工坊区普通模数",w:60,d:42,types:["标准工坊楼","材料库房","维修作业楼"]},
  "封存区":{sizeSpec:"封存区普通模数",w:48,d:30,types:["封存值守楼","记录库房"]},
  "郊野／外环":{sizeSpec:"外环普通模数",w:50,d:34,types:["外围维护舍","检疫服务楼"]}
};
const typeCounts={};
const coreRoadIds=new Set(["R-CW","R-CE","R-W3","R-E2","R-E3","T-02","L-01","M-02","M-03","W-01","S-01"]);
const developmentRoads=roadDefs.filter(r=>!r.routeId&&!r.roadClass.includes("桥梁")).sort((a,b)=>{
  const priority=(road)=>coreRoadIds.has(road.id)?0:/^[TLMWS]-/.test(road.id)?1:/^R-/.test(road.id)?2:3;
  return priority(a)-priority(b);
});
const roadPriority=(road)=>coreRoadIds.has(road.id)?0:/^[TLMWS]-/.test(road.id)?1:/^R-/.test(road.id)?2:3;
const zoneCore={"教学区":[1080,1050],"生活区":[1040,1550],"市集区":[2220,1120],"工坊区":[2460,1550],"封存区":[2020,1550],"郊野／外环":[1600,2400]};
const alignedCandidates=Object.fromEntries(Object.keys(quotas).map(zone=>[zone,[]]));
for(const road of developmentRoads){
  for(let si=0;si<road.coords.length-1;si++){
    const a=road.coords[si],b=road.coords[si+1];
    const dx=b[0]-a[0],dy=b[1]-a[1],len=Math.hypot(dx,dy);if(len<60)continue;
    const ux=dx/len,uy=dy/len,nx=-uy,ny=ux,angle=Math.atan2(dy,dx);
    const horizontal=Math.abs(dx)>=Math.abs(dy);
    for(const candidateZone of Object.keys(quotas)){
      if(!quotas[candidateZone])continue;
      const spec=regularSpecs[candidateZone];
      const pitch=spec.w+8;
      const axisStart=horizontal?a[0]:a[1],axisEnd=horizontal?b[0]:b[1];
      const low=Math.min(axisStart,axisEnd)+spec.w/2+12,high=Math.max(axisStart,axisEnd)-spec.w/2-12;
      const phase=candidateZone==="封存区"?pitch/2:0;
      const first=Math.ceil((low-phase)/pitch)*pitch+phase;
      for(let axis=first;axis<=high+0.01;axis+=pitch){
        const t=horizontal?(axis-a[0])/dx:(axis-a[1])/dy;
        if(t<0||t>1)continue;
        for(const side of [1,-1]){
          const offset=road.width/2+5+spec.d/2;
          const cx=a[0]+dx*t+nx*offset*side,cy=a[1]+dy*t+ny*offset*side;
          if(zoneAt([cx,cy])!==candidateZone)continue;
          const coords=orientedRect(cx,cy,spec.w,spec.d,angle);
          if(coords.some(p=>zoneAt(p)!==candidateZone))continue;
          const [zx,zy]=zoneCore[candidateZone];
          const coreDistance=Math.hypot(cx-zx,cy-zy);
          alignedCandidates[candidateZone].push({coords,road,score:roadPriority(road)*220+coreDistance,cx,cy});
        }
      }
    }
  }
}
let serial=1;
for(const zone of Object.keys(quotas)){
  const spec=regularSpecs[zone];if(!spec)continue;
  alignedCandidates[zone].sort((a,b)=>a.score-b.score||a.cy-b.cy||a.cx-b.cx||a.road.id.localeCompare(b.road.id));
  for(const candidate of alignedCandidates[zone]){
    if(counts[zone]>=quotas[zone])break;
    if(!validAlignedBuilding(candidate.coords,candidate.road.id))continue;
    const buildingType=spec.types[counts[zone]%spec.types.length];
    const typeKey=`${zone}:${buildingType}`;typeCounts[typeKey]=(typeCounts[typeKey]||0)+1;
    addBuilding(`B-${String(serial).padStart(3,"0")}`,`${buildingType} ${typeCounts[typeKey]}`,zone,candidate.coords,candidate.road.id,`${zone}的${buildingType}，按${spec.sizeSpec}统一规格并沿街坊基线对齐。`,{scaleClass:"普通建筑",buildingType,sizeSpec:spec.sizeSpec,widthMeters:spec.w,depthMeters:spec.d,alignment:"分区街段基线",streetGapMeters:8,roadSetbackMeters:5});
    counts[zone]++;serial++;
  }
}

if(buildingsLocal.length!==600){throw new Error(`新城建筑生成未达到600栋：当前 ${buildingsLocal.length}；分区 ${JSON.stringify(counts)}`);}
function roadPosition(point,road){
  let accumulated=0,best=null;
  for(let index=0;index<road.coords.length-1;index++){
    const a=road.coords[index],b=road.coords[index+1],dx=b[0]-a[0],dy=b[1]-a[1],lengthSquared=dx*dx+dy*dy;
    const length=Math.sqrt(lengthSquared);if(!length)continue;
    const t=Math.max(0,Math.min(1,((point[0]-a[0])*dx+(point[1]-a[1])*dy)/lengthSquared));
    const projected=[a[0]+t*dx,a[1]+t*dy],distance=Math.hypot(point[0]-projected[0],point[1]-projected[1]);
    const side=Math.sign(dx*(point[1]-a[1])-dy*(point[0]-a[0]))||1;
    if(!best||distance<best.distance)best={distance,measure:accumulated+t*length,side};
    accumulated+=length;
  }
  return best;
}
const addressGroups=new Map();
for(const building of buildingsLocal){
  const road=roadMap.get(building.properties.frontageRoad);
  const center=building.coords.reduce(([x,y],point)=>[x+point[0]/building.coords.length,y+point[1]/building.coords.length],[0,0]);
  const position=roadPosition(center,road);
  if(!addressGroups.has(road.id))addressGroups.set(road.id,{road,positive:[],negative:[]});
  addressGroups.get(road.id)[position.side>0?"positive":"negative"].push({building,measure:position.measure});
}
for(const {road,positive,negative} of addressGroups.values()){
  for(const [entries,firstNumber] of [[positive,1],[negative,2]]){
    entries.sort((a,b)=>a.measure-b.measure||a.building.properties.id.localeCompare(b.building.properties.id));
    entries.forEach(({building},index)=>{
      const houseNumber=firstNumber+index*2;
      building.properties.houseNumber=houseNumber;
      building.properties.doorPlate=`${houseNumber}号`;
      building.properties.address=`${road.name} ${houseNumber}号`;
    });
  }
}
export const buildings={type:"FeatureCollection",features:buildingsLocal.map(({coords,properties})=>makeFeature("Polygon",coords,properties))};

const pointDefs=[
  ["P-A","学院核心","landmark",[1080,1050],"西岸学院核心"],["P-C","城市核心","landmark",[2220,1120],"东岸城市公共核心"],
  ["P-M-NORTH","北河门","entrance",[1730,280],"北岸市集区河门与巡检入口"],
  ["P-M-RIVER1","市政北街河门","entrance",[1715,390],"市政北街河岸入口"],["P-M-RIVER2","市集街河门","entrance",[1785,830],"覆顶市集街河岸入口"],
  ["P-M-RIVER3","剧场街河门","entrance",[1925,1050],"公共剧场街河岸入口"],
  ["P-MB","双核主桥西端","entrance",[1260,1250],"步行与普通车辆主桥"],["P-TR","中央巨蚓换乘站","station",[1900,1520],"内环与跨河桥槽换乘"],
  ["P-WTR","工坊客货换乘站","station",[2760,1780],"客运与货运分流"],["P-ST","学院体育场入口","entrance",[560,2410],"体育课程入口"],
  ["P-CO","护障对抗场入口","entrance",[1260,2420],"护障课程入口"],["P-TA","小队战术场入口","entrance",[2080,2420],"战术课程入口"],
  ["P-T-RIVER","教学河岸门","entrance",[1440,360],"教学区河岸维护入口"],["P-T-CLOCK","学院正厅门廊入口","entrance",[900,1238],"学院正厅贴路门廊入口"],
  ["P-L-RIVER","生活区河岸门","entrance",[1430,2200],"生活区河岸维护入口"],["P-S-N1","封存许可门","entrance",[1985,1450],"封存受控入口"],
  ["P-L-RIVER-MID","生活南街河门","entrance",[1430,1900],"生活南街河岸入口"],
  ["P-S-N2","隔离北门","entrance",[1885,1580],"隔离设施入口"],["P-S-E","隔离东门","entrance",[2178,1600],"隔离设施东入口"],
  ["P-O-WATER","南部课程水岸门","entrance",[1450,2380],"课程场水岸维护入口"],["P-O-SW","西南课程门","entrance",[1450,2740],"课程设施南门"],
  ["P-O-SE","东南水务门","entrance",[1720,2760],"水务设施南门"],["P-T-EAST","图书馆街河岸门","entrance",[1438,820],"教学区东侧河岸入口"],
  ["P-S-SOUTH","封存南门","entrance",[1770,2260],"封存区南部受控入口"],["P-O-EAST","南部后勤水岸门","entrance",[1720,2380],"南部后勤区水岸入口"]
];
for(const building of buildingsLocal.filter(({properties})=>properties.buildingType==="车站")){
  const center=building.coords.reduce(([x,y],point)=>[x+point[0]/building.coords.length,y+point[1]/building.coords.length],[0,0]);
  pointDefs.push([`P-${building.properties.id}`,building.properties.name,"station",center,building.properties.description]);
}
export const points={type:"FeatureCollection",features:pointDefs.map(([id,name,kind,coord,description])=>makeFeature("Point",coord,{id,name,kind,description}))};

export const statistics={areaHectares:896,buildings:buildings.features.length,buildingFootprint:buildingsLocal.reduce((sum,b)=>sum+b.properties.footprint,0),roads:roads.features.filter(f=>!f.properties.routeId).length,transitLines:new Set(roads.features.filter(f=>f.properties.routeId).map(f=>f.properties.routeId)).size,zones:7};
export const bounds=[toLngLat([-120,HEIGHT+120]),toLngLat([WIDTH+120,-120])];
