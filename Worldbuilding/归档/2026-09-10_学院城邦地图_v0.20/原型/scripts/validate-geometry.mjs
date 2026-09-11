import { buildings, mapDimensions, points, roads, statistics, terrain, zones } from "../src/data.js";

const METERS_PER_LON_AT_EQUATOR = 111320;
const METERS_PER_LAT = 110540;
const MAP_WIDTH = mapDimensions.width;
const MAP_HEIGHT = mapDimensions.height;
const ZONE_SAMPLE_STEP = 4;
const MAX_FRONTAGE_SETBACK = 14;
const ENDPOINT_PURPOSE_DISTANCE = 24;
const CARDINAL_TOLERANCE_DEGREES = 3;
const MIN_EXPLICIT_CORNER_DEGREES = 30;
const OPEN_SPACE_SAMPLE_STEP = 40;
const MAX_UNEXPLAINED_BUILDING_DISTANCE = 140;
const MAX_UNEXPLAINED_ROAD_DISTANCE = 110;

function localPoint([lng, lat]) {
  return [lng * METERS_PER_LON_AT_EQUATOR, -lat * METERS_PER_LAT];
}

function polygon(feature) {
  return feature.geometry.coordinates[0].slice(0, -1).map(localPoint);
}

function polyline(feature) {
  return feature.geometry.coordinates.map(localPoint);
}

function bbox(points) {
  return points.reduce((box, [x, y]) => ({
    minX: Math.min(box.minX, x),
    minY: Math.min(box.minY, y),
    maxX: Math.max(box.maxX, x),
    maxY: Math.max(box.maxY, y)
  }), { minX: Infinity, minY: Infinity, maxX: -Infinity, maxY: -Infinity });
}

function pointInPolygon([x, y], points) {
  let inside = false;
  for (let i = 0, j = points.length - 1; i < points.length; j = i++) {
    const [xi, yi] = points[i];
    const [xj, yj] = points[j];
    if ((yi > y) !== (yj > y) && x < ((xj - xi) * (y - yi)) / (yj - yi) + xi) inside = !inside;
  }
  return inside;
}

function orientation(a, b, c) {
  return Math.sign((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]));
}

function onSegment(a, b, p) {
  const epsilon = 0.001;
  return Math.abs((b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])) < epsilon
    && p[0] >= Math.min(a[0], b[0]) - epsilon && p[0] <= Math.max(a[0], b[0]) + epsilon
    && p[1] >= Math.min(a[1], b[1]) - epsilon && p[1] <= Math.max(a[1], b[1]) + epsilon;
}

function segmentsIntersect(a, b, c, d) {
  const o1 = orientation(a, b, c);
  const o2 = orientation(a, b, d);
  const o3 = orientation(c, d, a);
  const o4 = orientation(c, d, b);
  if (o1 !== o2 && o3 !== o4) return true;
  return (o1 === 0 && onSegment(a, b, c)) || (o2 === 0 && onSegment(a, b, d))
    || (o3 === 0 && onSegment(c, d, a)) || (o4 === 0 && onSegment(c, d, b));
}

function polygonEdges(points) {
  return points.map((point, index) => [point, points[(index + 1) % points.length]]);
}

function polygonArea(points) {
  return Math.abs(points.reduce((sum, [x, y], index) => {
    const [nextX, nextY] = points[(index + 1) % points.length];
    return sum + x * nextY - nextX * y;
  }, 0) / 2);
}

function polygonsOverlap(a, b) {
  const boxA = bbox(a);
  const boxB = bbox(b);
  if (boxA.maxX <= boxB.minX || boxB.maxX <= boxA.minX || boxA.maxY <= boxB.minY || boxB.maxY <= boxA.minY) return false;
  if (a.some((point) => pointInPolygon(point, b)) || b.some((point) => pointInPolygon(point, a))) return true;
  return polygonEdges(a).some(([a1, a2]) => polygonEdges(b).some(([b1, b2]) => {
    if (!segmentsIntersect(a1, a2, b1, b2)) return false;
    return orientation(a1, a2, b1) !== 0 || orientation(a1, a2, b2) !== 0;
  }));
}

function segmentIntersectsBox(a, b, box) {
  if (a[0] > box.minX && a[0] < box.maxX && a[1] > box.minY && a[1] < box.maxY) return true;
  if (b[0] > box.minX && b[0] < box.maxX && b[1] > box.minY && b[1] < box.maxY) return true;
  const corners = [[box.minX, box.minY], [box.maxX, box.minY], [box.maxX, box.maxY], [box.minX, box.maxY]];
  return polygonEdges(corners).some(([c, d]) => segmentsIntersect(a, b, c, d));
}

function lineIntersectsPolygon(linePoints, polyPoints) {
  return linePoints.some((point) => pointInPolygon(point, polyPoints))
    || linePoints.slice(0, -1).some((point, index) => polygonEdges(polyPoints)
      .some(([a, b]) => segmentsIntersect(point, linePoints[index + 1], a, b)));
}

function linesIntersect(a, b) {
  return a.slice(0, -1).some((point, index) => b.slice(0, -1)
    .some((other, otherIndex) => segmentsIntersect(point, a[index + 1], other, b[otherIndex + 1])));
}

function pointDistance(a, b) {
  return Math.hypot(a[0] - b[0], a[1] - b[1]);
}

function pointToSegmentDistance(point, a, b) {
  const dx = b[0] - a[0];
  const dy = b[1] - a[1];
  const lengthSquared = dx * dx + dy * dy;
  if (lengthSquared === 0) return pointDistance(point, a);
  const t = Math.max(0, Math.min(1, ((point[0] - a[0]) * dx + (point[1] - a[1]) * dy) / lengthSquared));
  return pointDistance(point, [a[0] + t * dx, a[1] + t * dy]);
}

function segmentDistance(a, b, c, d) {
  const length = Math.hypot(b[0] - a[0], b[1] - a[1]);
  let minimum = Infinity;
  for (let distance = 0; distance <= length; distance += 1) {
    const t = length === 0 ? 0 : distance / length;
    minimum = Math.min(minimum, pointToSegmentDistance([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t], c, d));
  }
  return Math.min(minimum, pointToSegmentDistance(b, c, d));
}

function polygonToRoadClearDistance(buildingPoints, road) {
  const centerline = polyline(road);
  let minimum = Infinity;
  for (let edgeIndex = 0; edgeIndex < buildingPoints.length; edgeIndex += 1) {
    const a = buildingPoints[edgeIndex];
    const b = buildingPoints[(edgeIndex + 1) % buildingPoints.length];
    for (let lineIndex = 0; lineIndex < centerline.length - 1; lineIndex += 1) {
      minimum = Math.min(minimum, segmentDistance(a, b, centerline[lineIndex], centerline[lineIndex + 1]));
    }
  }
  return minimum - Number(road.properties.width || 0) / 2;
}

function pointToPolygonDistance(point, points) {
  if (pointInPolygon(point, points)) return 0;
  return Math.min(...polygonEdges(points).map(([a, b]) => pointToSegmentDistance(point, a, b)));
}

function lineEndpointHasPurpose(endpoint, owner, roadFeatures, buildingEntries, pointEntries, surfaceEntries) {
  const boundaryAccessMargin = 2;
  if (endpoint[0] <= boundaryAccessMargin || endpoint[0] >= MAP_WIDTH - boundaryAccessMargin || endpoint[1] <= boundaryAccessMargin || endpoint[1] >= MAP_HEIGHT - boundaryAccessMargin) return true;
  const reachesOtherRoad = roadFeatures.some((feature) => {
    if (feature === owner) return false;
    const points = polyline(feature);
    return points.slice(0, -1).some((point, index) => pointToSegmentDistance(endpoint, point, points[index + 1]) <= 0.5);
  });
  if (reachesOtherRoad) return true;
  if (pointEntries.some((point) => pointDistance(endpoint, point) <= ENDPOINT_PURPOSE_DISTANCE)) return true;
  if (buildingEntries.some((points) => {
    const box = bbox(points);
    const dx = Math.max(box.minX - endpoint[0], 0, endpoint[0] - box.maxX);
    const dy = Math.max(box.minY - endpoint[1], 0, endpoint[1] - box.maxY);
    return Math.hypot(dx, dy) <= ENDPOINT_PURPOSE_DISTANCE;
  })) return true;
  return surfaceEntries.some((points) => pointInPolygon(endpoint, points));
}

function parallelCorridorOverlap(a1, a2, b1, b2) {
  const ax = a2[0] - a1[0];
  const ay = a2[1] - a1[1];
  const bx = b2[0] - b1[0];
  const by = b2[1] - b1[1];
  const lengthA = Math.hypot(ax, ay);
  const lengthB = Math.hypot(bx, by);
  if (lengthA < 0.01 || lengthB < 0.01) return null;
  const parallelError = Math.abs(ax * by - ay * bx) / (lengthA * lengthB);
  if (parallelError > Math.sin(5 * Math.PI / 180)) return null;
  const ux = ax / lengthA;
  const uy = ay / lengthA;
  const b0 = (b1[0] - a1[0]) * ux + (b1[1] - a1[1]) * uy;
  const b1Projection = (b2[0] - a1[0]) * ux + (b2[1] - a1[1]) * uy;
  const overlap = Math.min(lengthA, Math.max(b0, b1Projection)) - Math.max(0, Math.min(b0, b1Projection));
  if (overlap <= 8) return null;
  const separation = Math.abs((b1[0] - a1[0]) * uy - (b1[1] - a1[1]) * ux);
  return { overlap, separation };
}

function disconnectedLineGroups(features) {
  if (!features.length) return [];
  const parents = features.map((_, index) => index);
  const find = (index) => parents[index] === index ? index : (parents[index] = find(parents[index]));
  const join = (a, b) => {
    const rootA = find(a);
    const rootB = find(b);
    if (rootA !== rootB) parents[rootB] = rootA;
  };
  const lines = features.map(polyline);
  for (let i = 0; i < features.length; i += 1) {
    for (let j = i + 1; j < features.length; j += 1) {
      if (linesIntersect(lines[i], lines[j])) join(i, j);
    }
  }
  const groups = new Map();
  features.forEach((feature, index) => {
    const root = find(index);
    if (!groups.has(root)) groups.set(root, []);
    groups.get(root).push(feature.properties.id);
  });
  return [...groups.values()];
}

const errors = [];
const warnings = [];
const allFeatures = [...zones.features, ...terrain.features, ...roads.features, ...buildings.features, ...points.features];
const ids = new Set();
for (const feature of allFeatures) {
  const id = feature.properties.id;
  if (!id) errors.push("存在缺少 id 的几何对象。");
  else if (ids.has(id)) errors.push(`重复 id：${id}`);
  ids.add(id);
}

let zoneGapSamples = 0;
let zoneOverlapSamples = 0;
const zoneGapExamples = [];
const zoneOverlapExamples = [];
const zonePolygons = zones.features.map((feature) => [feature, polygon(feature)]);
for (const zone of zones.features) {
  if (!zone.properties.openSpacePurpose) errors.push(`功能分区缺少未绘制空间用途：${zone.properties.id}`);
}
for (let y = ZONE_SAMPLE_STEP / 2; y < MAP_HEIGHT; y += ZONE_SAMPLE_STEP) {
  for (let x = ZONE_SAMPLE_STEP / 2; x < MAP_WIDTH; x += ZONE_SAMPLE_STEP) {
    const count = zonePolygons.filter(([, points]) => pointInPolygon([x, y], points)).length;
    if (count === 0) {
      zoneGapSamples += 1;
      if (zoneGapExamples.length < 5) zoneGapExamples.push(`(${x}, ${y})`);
    }
    if (count > 1) {
      zoneOverlapSamples += 1;
      if (zoneOverlapExamples.length < 5) zoneOverlapExamples.push(`(${x}, ${y})`);
    }
  }
}
if (zoneGapSamples) errors.push(`功能分区存在约 ${zoneGapSamples * ZONE_SAMPLE_STEP ** 2} m² 未归属缝隙；示例 ${zoneGapExamples.join("、")}。`);
if (zoneOverlapSamples) errors.push(`功能分区存在约 ${zoneOverlapSamples * ZONE_SAMPLE_STEP ** 2} m² 重叠；示例 ${zoneOverlapExamples.join("、")}。`);

const buildingPolygons = buildings.features.map((feature) => [feature, polygon(feature)]);
const roadsById = new Map(roads.features.map((feature) => [feature.properties.id, feature]));
const addresses = new Set();
for (const road of roads.features) if (!road.properties.name?.trim()) errors.push(`道路缺少名称：${road.properties.id}`);
for (const [building, buildingPoints] of buildingPolygons) {
  const center = buildingPoints.reduce(([x, y], point) => [x + point[0] / buildingPoints.length, y + point[1] / buildingPoints.length], [0, 0]);
  const claimedZones = zonePolygons.filter(([zone]) => zone.properties.zone === building.properties.zone);
  if (!claimedZones.some(([, zonePoints]) => pointInPolygon(center, zonePoints))) {
    errors.push(`建筑中心不在声明分区：${building.properties.id}（${building.properties.zone}）`);
  }
  const modeledFootprint = polygonArea(buildingPoints);
  if (Math.abs(modeledFootprint - Number(building.properties.footprint || 0)) > 0.1) {
    errors.push(`建筑占地元数据与真实轮廓不一致：${building.properties.id}`);
  }
  if (building.properties.shapeType && buildingPoints.length <= 4) {
    errors.push(`特殊建筑仍是普通四边矩形：${building.properties.id}（${building.properties.shapeType}）`);
  }
  if (!Number.isInteger(building.properties.houseNumber) || building.properties.houseNumber < 1 || !building.properties.doorPlate || !building.properties.address) {
    errors.push(`建筑缺少有效门牌：${building.properties.id}`);
  } else {
    const expectedAddress = `${roadsById.get(building.properties.frontageRoad)?.properties.name || ""} ${building.properties.houseNumber}号`;
    if (building.properties.doorPlate !== `${building.properties.houseNumber}号` || building.properties.address !== expectedAddress) errors.push(`建筑门牌与正面道路不一致：${building.properties.id}`);
    if (addresses.has(building.properties.address)) errors.push(`门牌地址重复：${building.properties.address}`);
    addresses.add(building.properties.address);
  }
  const frontageRoad = roadsById.get(building.properties.frontageRoad);
  if (!building.properties.frontageRoad) {
    errors.push(`建筑未声明正面道路：${building.properties.id}`);
  } else if (!frontageRoad) {
    errors.push(`建筑正面道路不存在：${building.properties.id} → ${building.properties.frontageRoad}`);
  } else if (frontageRoad.properties.routeId) {
    errors.push(`建筑不得以通勤轨道作为正面道路：${building.properties.id} → ${building.properties.frontageRoad}`);
  } else {
    const setback = polygonToRoadClearDistance(buildingPoints, frontageRoad);
    if (setback > MAX_FRONTAGE_SETBACK + 0.1) {
      errors.push(`建筑离正面道路过远：${building.properties.id} → ${building.properties.frontageRoad}（${setback.toFixed(1)} m，最大 ${MAX_FRONTAGE_SETBACK} m）`);
    }
    if (building.properties.scaleClass === "普通建筑") {
      if (Math.abs(setback - 5) > 0.2) errors.push(`普通建筑未按5 m统一临街退距：${building.properties.id} 当前 ${setback.toFixed(2)} m。`);
      if (building.properties.alignment !== "分区街段基线" || Number(building.properties.streetGapMeters) !== 8 || Number(building.properties.roadSetbackMeters) !== 5) {
        errors.push(`普通建筑缺少街段对齐元数据：${building.properties.id}`);
      }
      const centerline = polyline(frontageRoad);
      let nearestSegment = null;
      for (let index = 0; index < centerline.length - 1; index += 1) {
        const distance = pointToSegmentDistance(center, centerline[index], centerline[index + 1]);
        if (!nearestSegment || distance < nearestSegment.distance) nearestSegment = { index, distance, a: centerline[index], b: centerline[index + 1] };
      }
      if (nearestSegment) {
        const horizontal = Math.abs(nearestSegment.b[0] - nearestSegment.a[0]) >= Math.abs(nearestSegment.b[1] - nearestSegment.a[1]);
        const pitch = Number(building.properties.widthMeters) + Number(building.properties.streetGapMeters);
        const phase = building.properties.zone === "封存区" ? pitch / 2 : 0;
        const axis = horizontal ? center[0] : center[1];
        const latticeError = Math.abs(((axis - phase + pitch / 2) % pitch + pitch) % pitch - pitch / 2);
        if (latticeError > 0.2) errors.push(`普通建筑未落在街段等距基线：${building.properties.id} 偏差 ${latticeError.toFixed(2)} m。`);
      }
      for (const otherRoad of roads.features.filter((feature) => feature !== frontageRoad)) {
        const otherClearance = polygonToRoadClearDistance(buildingPoints, otherRoad);
        if (otherClearance < 3.4) errors.push(`普通建筑在路口侧向退距不足：${building.properties.id} 与 ${otherRoad.properties.id} 仅 ${otherClearance.toFixed(2)} m。`);
      }
    }
  }
}
for (let i = 0; i < buildingPolygons.length; i += 1) {
  for (let j = i + 1; j < buildingPolygons.length; j += 1) {
    const [aFeature, a] = buildingPolygons[i];
    const [bFeature, b] = buildingPolygons[j];
    if (polygonsOverlap(a, b)) errors.push(`建筑重叠：${aFeature.properties.id} 与 ${bFeature.properties.id}`);
  }
}

for (const road of roads.features) {
  const roadPoints = polyline(road);
  const clearance = Math.max(2, Number(road.properties.width || 0) / 2 + 2);
  for (const [building, buildingPoints] of buildingPolygons) {
    const centerlineDistance = polygonToRoadClearDistance(buildingPoints, road) + Number(road.properties.width || 0) / 2;
    if (lineIntersectsPolygon(roadPoints, buildingPoints) || centerlineDistance < clearance - 0.1) {
      errors.push(`通行廊道侵入建筑退界：${road.properties.id} 与 ${building.properties.id}`);
    }
  }
}

const nonTransitRoads = roads.features.filter((feature) => !feature.properties.routeId);
let ordinaryRoadLength = 0;
let cardinalRoadLength = 0;
for (const road of nonTransitRoads) {
  const points = polyline(road);
  const headings = [];
  for (let index = 0; index < points.length - 1; index += 1) {
    const dx = points[index + 1][0] - points[index][0];
    const dy = points[index + 1][1] - points[index][1];
    const length = Math.hypot(dx, dy);
    if (length < 0.01) {
      errors.push(`道路包含零长度线段：${road.properties.id}`);
      continue;
    }
    const heading = ((Math.atan2(dy, dx) * 180 / Math.PI) + 360) % 180;
    const cardinalError = Math.min(heading, Math.abs(90 - heading), Math.abs(180 - heading));
    ordinaryRoadLength += length;
    if (cardinalError <= CARDINAL_TOLERANCE_DEGREES) cardinalRoadLength += length;
    headings.push(heading);
  }
  for (let index = 1; index < headings.length; index += 1) {
    const rawChange = Math.abs(headings[index] - headings[index - 1]);
    const turn = Math.min(rawChange, 180 - rawChange);
    if (turn > CARDINAL_TOLERANCE_DEGREES && turn < MIN_EXPLICIT_CORNER_DEGREES) {
      errors.push(`道路以小角度漂移而未形成明确拐角：${road.properties.id}（${turn.toFixed(1)}°）`);
    }
  }
}
if (ordinaryRoadLength > 0 && cardinalRoadLength / ordinaryRoadLength < 0.9) {
  errors.push(`普通道路未形成以水平／垂直为主的街网：正交线段仅占 ${(cardinalRoadLength / ordinaryRoadLength * 100).toFixed(1)}%。`);
}
const pointEntries = points.features.map((feature) => localPoint(feature.geometry.coordinates));
const surfaceEntries = terrain.features.filter((feature) => feature.properties.kind !== "water").map(polygon);
for (const road of nonTransitRoads) {
  const roadPoints = polyline(road);
  for (const endpoint of [roadPoints[0], roadPoints.at(-1)]) {
    if (!lineEndpointHasPurpose(endpoint, road, nonTransitRoads, buildingPolygons.map(([, points]) => points), pointEntries, surfaceEntries)) {
      errors.push(`道路出现无入口、无节点且无场地用途的尽端：${road.properties.id} @ (${endpoint[0].toFixed(0)}, ${endpoint[1].toFixed(0)})`);
    }
  }
}

for (let i = 0; i < roads.features.length; i += 1) {
  for (let j = i + 1; j < roads.features.length; j += 1) {
    const aFeature = roads.features[i];
    const bFeature = roads.features[j];
    const a = polyline(aFeature);
    const b = polyline(bFeature);
    const minimumSeparation = (Number(aFeature.properties.width || 0) + Number(bFeature.properties.width || 0)) / 2;
    let reported = false;
    for (let ai = 0; ai < a.length - 1 && !reported; ai += 1) {
      for (let bi = 0; bi < b.length - 1; bi += 1) {
        const relation = parallelCorridorOverlap(a[ai], a[ai + 1], b[bi], b[bi + 1]);
        if (relation && relation.separation < minimumSeparation - 0.1) {
          errors.push(`道路廊道重复并行：${aFeature.properties.id} 与 ${bFeature.properties.id}（约 ${relation.overlap.toFixed(0)} m）`);
          reported = true;
          break;
        }
      }
    }
  }
}

const pedestrianGroups = disconnectedLineGroups(roads.features.filter((feature) => !feature.properties.routeId));
if (pedestrianGroups.length > 1) {
  errors.push(`道路／步道网络分成 ${pedestrianGroups.length} 个不连通组：${pedestrianGroups.map((group) => `[${group.join(", ")}]`).join("；")}`);
}
for (const routeId of new Set(roads.features.filter((feature) => feature.properties.routeId).map((feature) => feature.properties.routeId))) {
  const groups = disconnectedLineGroups(roads.features.filter((feature) => feature.properties.routeId === routeId));
  if (groups.length > 1) errors.push(`通勤线路 ${routeId} 存在断口：${groups.map((group) => `[${group.join(", ")}]`).join("；")}`);
}

const waterPolygons = terrain.features.filter((feature) => feature.properties.kind === "water").map((feature) => [feature, polygon(feature)]);
for (const [water, waterPoints] of waterPolygons) {
  for (const [building, buildingPoints] of buildingPolygons) {
    if (polygonsOverlap(waterPoints, buildingPoints)) errors.push(`水体侵入建筑：${water.properties.id} 与 ${building.properties.id}`);
  }
  for (const road of roads.features.filter((feature) => !feature.properties.roadClass.includes("桥梁"))) {
    if (lineIntersectsPolygon(polyline(road), waterPoints)) errors.push(`非桥梁通行线跨越水体：${road.properties.id} 与 ${water.properties.id}`);
  }
}

const expectedWalkways = roads.features.filter((feature) => !feature.properties.routeId).length;
const expectedTransitLines = new Set(roads.features.filter((feature) => feature.properties.routeId).map((feature) => feature.properties.routeId)).size;
const expectedFootprint = buildings.features.reduce((sum, feature) => sum + Number(feature.properties.footprint || 0), 0);
if (statistics.buildings !== buildings.features.length) errors.push("统计中的建筑数量与几何源不一致。");
if (statistics.roads !== expectedWalkways) errors.push("统计中的道路数量与几何源不一致。");
if (statistics.transitLines !== expectedTransitLines) errors.push("统计中的通勤线路数量与几何源不一致。");
if (statistics.buildingFootprint !== expectedFootprint) errors.push("统计中的建筑占地与几何源不一致。");
if (MAP_WIDTH !== 3200 || MAP_HEIGHT !== 2800 || statistics.areaHectares !== 896) errors.push("当前城邦尺度必须保持 3200 m × 2800 m（896 ha）。");
if (buildings.features.length !== 600) errors.push(`当前精修版建筑数量必须保持 600 栋，当前为 ${buildings.features.length} 栋。`);
const coverageRate = expectedFootprint / (MAP_WIDTH * MAP_HEIGHT);
if (coverageRate < 0.14 || coverageRate > 0.16) errors.push(`建筑覆盖率应保持在减少无效空地后的精修区间（14%—16%）：当前 ${(coverageRate * 100).toFixed(3)}%。`);
const landmarkBuildings = buildings.features.filter((feature) => feature.properties.shapeType);
const cityLandmarks = landmarkBuildings.filter((feature) => feature.properties.landmarkTier === "city");
const regionalLandmarks = landmarkBuildings.filter((feature) => feature.properties.landmarkTier === "regional");
if (cityLandmarks.length !== 8 || regionalLandmarks.length !== 10) errors.push(`地标体系必须为8座全城级与10座区域级，当前为 ${cityLandmarks.length}／${regionalLandmarks.length}。`);
const dailyServiceBuildings = buildings.features.filter((feature) => feature.properties.scaleClass === "日常服务设施");
if (dailyServiceBuildings.length !== 24) errors.push(`日常服务设施必须保持24栋，当前为 ${dailyServiceBuildings.length} 栋。`);
for (const requiredType of ["餐饮", "饭店", "商店", "车站", "医疗", "生活服务", "旅店", "公共服务"]) {
  if (!dailyServiceBuildings.some((feature) => feature.properties.buildingType === requiredType)) errors.push(`日常服务缺少类型：${requiredType}`);
}
if (dailyServiceBuildings.filter((feature) => feature.properties.buildingType === "车站").length < 6) errors.push("日常交通至少需要6座分区站房。");
const dimensionBySpec = new Map();
const ordinaryDimensionsByZone = new Map();
for (const building of buildings.features.filter((feature) => ["普通建筑", "日常服务设施"].includes(feature.properties.scaleClass))) {
  const { buildingType, sizeSpec, widthMeters, depthMeters, zone } = building.properties;
  if (!buildingType || !sizeSpec || !widthMeters || !depthMeters) {
    errors.push(`建筑缺少类型或规格元数据：${building.properties.id}`);
    continue;
  }
  const key = `${zone}:${sizeSpec}`;
  const dimensions = `${Number(widthMeters).toFixed(1)}×${Number(depthMeters).toFixed(1)}`;
  if (dimensionBySpec.has(key) && dimensionBySpec.get(key) !== dimensions) errors.push(`同区域同规格建筑尺寸不一致：${key}`);
  else dimensionBySpec.set(key, dimensions);
  if (building.properties.scaleClass === "普通建筑") {
    if (!ordinaryDimensionsByZone.has(zone)) ordinaryDimensionsByZone.set(zone, new Set());
    ordinaryDimensionsByZone.get(zone).add(dimensions);
  }
}
for (const [zone, dimensions] of ordinaryDimensionsByZone) {
  if (dimensions.size !== 1) errors.push(`同一区域的普通建筑必须共用一个可见宽深模数：${zone} 当前为 ${[...dimensions].join("、")}`);
}
const neighborhoodParks = terrain.features.filter((feature) => feature.properties.kind === "park");
if (neighborhoodParks.length < 4) errors.push(`城区街坊公园不足：当前 ${neighborhoodParks.length} 处，至少需要4处。`);
const densityBands = { near: 0, middle: 0, far: 0 };
for (const [, buildingPoints] of buildingPolygons) {
  const center = buildingPoints.reduce(([x, y], point) => [x + point[0] / buildingPoints.length, y + point[1] / buildingPoints.length], [0, 0]);
  const distance = Math.min(Math.hypot(center[0] - 1080, center[1] - 1200), Math.hypot(center[0] - 2220, center[1] - 1200));
  densityBands[distance < 650 ? "near" : distance < 1100 ? "middle" : "far"] += 1;
}
if (densityBands.near < buildings.features.length * 0.25 || densityBands.far > buildings.features.length * 0.22) errors.push(`建筑没有形成双核心向外围递减的分布：近／中／远为 ${densityBands.near}／${densityBands.middle}／${densityBands.far}。`);
for (const routeId of ["TR-IN", "TR-OUT"]) if (!roads.features.some((feature) => feature.properties.routeId === routeId)) errors.push(`缺少巨蚓线路：${routeId}`);
for (const requiredId of ["B-MAIN", "TR-IN-B"]) if (!roadsById.has(requiredId)) errors.push(`缺少双核心必要桥梁：${requiredId}`);

const surfacePolygons = terrain.features.map((feature) => [feature, polygon(feature)]);
for (const [surface, surfacePoints] of surfacePolygons) {
  for (const [building, buildingPoints] of buildingPolygons) {
    if (polygonsOverlap(surfacePoints, buildingPoints)) errors.push(`地表用途侵入建筑：${surface.properties.id} 与 ${building.properties.id}`);
  }
}
for (let i = 0; i < surfacePolygons.length; i += 1) {
  for (let j = i + 1; j < surfacePolygons.length; j += 1) {
    const [aFeature, a] = surfacePolygons[i];
    const [bFeature, b] = surfacePolygons[j];
    if (polygonsOverlap(a, b)) errors.push(`地表用途重叠：${aFeature.properties.id} 与 ${bFeature.properties.id}`);
  }
}

const urbanZoneNames = new Set(["教学区", "生活区", "市集区", "工坊区"]);
const ordinaryRoadLines = nonTransitRoads.map(polyline);
const unexplainedOpenSamples = [];
for (let y = OPEN_SPACE_SAMPLE_STEP / 2; y < MAP_HEIGHT; y += OPEN_SPACE_SAMPLE_STEP) {
  for (let x = OPEN_SPACE_SAMPLE_STEP / 2; x < MAP_WIDTH; x += OPEN_SPACE_SAMPLE_STEP) {
    const point = [x, y];
    const zone = zonePolygons.find(([, points]) => pointInPolygon(point, points))?.[0];
    if (!zone || !urbanZoneNames.has(zone.properties.zone)) continue;
    if (surfacePolygons.some(([, points]) => pointInPolygon(point, points))) continue;
    if (buildingPolygons.some(([, points]) => pointInPolygon(point, points))) continue;
    const buildingDistance = Math.min(...buildingPolygons.map(([, points]) => pointToPolygonDistance(point, points)));
    const roadDistance = Math.min(...ordinaryRoadLines.flatMap((line) => line.slice(0, -1).map((a, index) => pointToSegmentDistance(point, a, line[index + 1]))));
    if (buildingDistance > MAX_UNEXPLAINED_BUILDING_DISTANCE && roadDistance > MAX_UNEXPLAINED_ROAD_DISTANCE) {
      unexplainedOpenSamples.push(`(${x}, ${y})`);
    }
  }
}
if (unexplainedOpenSamples.length) {
  errors.push(`城市片区仍有未由建筑、道路或命名地表解释的大块空地：${unexplainedOpenSamples.slice(0, 8).join("、")} 等 ${unexplainedOpenSamples.length} 个采样点。`);
}

if (warnings.length) {
  console.warn(`Geometry warnings (${warnings.length}):`);
  warnings.forEach((warning) => console.warn(`- ${warning}`));
}
if (errors.length) {
  console.error(`Geometry validation failed (${errors.length}):`);
  errors.forEach((error) => console.error(`- ${error}`));
  process.exit(1);
}

console.log(`Geometry validation passed: ${zones.features.length} zone polygons, ${buildings.features.length} buildings, ${roads.features.length} network lines, ${terrain.features.length} terrain parcels; ${(cardinalRoadLength / ordinaryRoadLength * 100).toFixed(1)}% of ordinary road length is cardinal.`);
