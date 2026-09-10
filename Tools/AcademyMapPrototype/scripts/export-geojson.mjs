import { mkdir, writeFile } from "node:fs/promises";
import { buildings, points, roads, terrain, zones } from "../src/data.js";

const METERS_PER_LON_AT_EQUATOR = 111320;
const METERS_PER_LAT = 110540;

function toLocalCoordinates(value) {
  if (typeof value[0] === "number") {
    return [
      Math.round(value[0] * METERS_PER_LON_AT_EQUATOR * 1000) / 1000,
      Math.round(-value[1] * METERS_PER_LAT * 1000) / 1000
    ];
  }
  return value.map(toLocalCoordinates);
}

function withLayer(layer, collection) {
  return collection.features.map((feature) => ({
    ...feature,
    properties: { ...feature.properties, layer },
    geometry: { ...feature.geometry, coordinates: toLocalCoordinates(feature.geometry.coordinates) }
  }));
}

const data = {
  type: "FeatureCollection",
  metadata: {
    title: "OCC 学院城邦全域地图 v0.20",
    coordinateSystem: "学院本地平面坐标：西北角为 (0,0)，x 向东、y 向南，单位 m。",
    source: "Tools/AcademyMapPrototype/src/data.js",
    generatedBy: "npm run export:geojson",
    scope: "地图精修中的矢量母版交换格式；不包含 Unity 战斗格图。"
  },
  features: [
    ...withLayer("zones", zones),
    ...withLayer("terrain", terrain),
    ...withLayer("roads", roads),
    ...withLayer("buildings", buildings),
    ...withLayer("points", points)
  ]
};

await mkdir(new URL("../data/", import.meta.url), { recursive: true });
await writeFile(new URL("../data/academy-campus-v0.20.geojson", import.meta.url), `${JSON.stringify(data, null, 2)}\n`);
console.log(`Exported ${data.features.length} features to data/academy-campus-v0.20.geojson`);
