import { buildings, roads } from "../src/data.js";

const METERS_PER_LON_AT_EQUATOR = 111320;
const METERS_PER_LAT = 110540;

const localPoint = ([lng, lat]) => [lng * METERS_PER_LON_AT_EQUATOR, -lat * METERS_PER_LAT];
const buildingPolygon = (feature) => feature.geometry.coordinates[0].slice(0, -1).map(localPoint);
const roadLine = (feature) => feature.geometry.coordinates.map(localPoint);

function pointToSegmentDistance(point, a, b) {
  const dx = b[0] - a[0];
  const dy = b[1] - a[1];
  const lengthSquared = dx * dx + dy * dy;
  if (lengthSquared === 0) return Math.hypot(point[0] - a[0], point[1] - a[1]);
  const t = Math.max(0, Math.min(1, ((point[0] - a[0]) * dx + (point[1] - a[1]) * dy) / lengthSquared));
  return Math.hypot(point[0] - (a[0] + t * dx), point[1] - (a[1] + t * dy));
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

const publicRoads = roads.features.filter((feature) => !feature.properties.routeId);
const measurements = buildings.features.map((building) => {
  const shell = buildingPolygon(building);
  let closest = null;
  const assignedRoad = publicRoads.find((road) => road.properties.id === building.properties.frontageRoad);
  for (const road of assignedRoad ? [assignedRoad] : publicRoads) {
    const centerline = roadLine(road);
    for (let edgeIndex = 0; edgeIndex < shell.length; edgeIndex += 1) {
      const a = shell[edgeIndex];
      const b = shell[(edgeIndex + 1) % shell.length];
      for (let lineIndex = 0; lineIndex < centerline.length - 1; lineIndex += 1) {
        const centerlineDistance = segmentDistance(a, b, centerline[lineIndex], centerline[lineIndex + 1]);
        const clearDistance = centerlineDistance - Number(road.properties.width || 0) / 2;
        if (!closest || clearDistance < closest.clearDistance) closest = { road, clearDistance };
      }
    }
  }
  return {
    id: building.properties.id,
    name: building.properties.name,
    zone: building.properties.zone,
    assignedRoad: building.properties.frontageRoad,
    road: closest.road.properties.id,
    roadName: closest.road.properties.name,
    distance: Math.max(0, closest.clearDistance)
  };
}).sort((a, b) => b.distance - a.distance);

const buckets = { "0-6m": 0, "6-12m": 0, "12-20m": 0, ">20m": 0 };
for (const item of measurements) {
  if (item.distance <= 6) buckets["0-6m"] += 1;
  else if (item.distance <= 12) buckets["6-12m"] += 1;
  else if (item.distance <= 20) buckets["12-20m"] += 1;
  else buckets[">20m"] += 1;
}

console.log(JSON.stringify({ buckets, measurements }, null, 2));
