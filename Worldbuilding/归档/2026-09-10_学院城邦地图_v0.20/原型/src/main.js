import maplibregl from "maplibre-gl";
import "maplibre-gl/dist/maplibre-gl.css";
import { bounds, buildings, points, roads, statistics, terrain, zones } from "./data.js";
import "./style.css";

const app = document.querySelector("#app");

app.innerHTML = `
  <main class="map-shell">
    <section class="map-canvas" aria-label="学院城邦总图">
      <div id="map"></div>
      <div class="map-title">
        <p>OCC · 学院城邦</p>
        <h1>学院全域平面图</h1>
        <span>v0.20 · 道路名称与全量门牌</span>
      </div>
      <div class="map-scale">本图为虚构地点的本地米制坐标总图</div>
    </section>
    <aside class="side-panel">
      <header>
        <p class="eyebrow">学院城邦全域</p>
        <h2>地图图层</h2>
        <p class="subtle">西岸学院核心与东岸城市核心隔河相望，主桥和巨蚓专用桥各自独立。</p>
      </header>
      <dl class="stats">
        <div><dt>总用地</dt><dd>${statistics.areaHectares} ha</dd></div>
        <div><dt>可识别建筑</dt><dd>${statistics.buildings}</dd></div>
        <div><dt>道路与步道</dt><dd>${statistics.roads}</dd></div>
        <div><dt>生物通勤环线</dt><dd>${statistics.transitLines}</dd></div>
        <div><dt>功能区域</dt><dd>${statistics.zones}</dd></div>
      </dl>
      <fieldset class="layers">
        <legend>显示</legend>
        <label><input type="checkbox" data-layer="zones" checked /> 功能分区</label>
        <label><input type="checkbox" data-layer="terrain" checked /> 庭院、郊野与水系</label>
        <label><input type="checkbox" data-layer="roads" checked /> 道路与步行线</label>
        <label><input type="checkbox" data-layer="buildings" checked /> 建筑轮廓</label>
        <label><input type="checkbox" data-layer="labels" checked /> 名称标注</label>
      </fieldset>
      <section class="detail" aria-live="polite">
        <p class="eyebrow">选中对象</p>
        <h3 id="detail-title">拖动、缩放或点选建筑</h3>
        <p id="detail-body">高缩放级别会展开建筑名称。点击建筑、道路、入口或区域可查看其地图资料。</p>
        <dl id="detail-meta"></dl>
      </section>
      <footer>
        <span>道路与建筑均为矢量对象</span>
        <span>不含 Unity 关卡格图</span>
      </footer>
    </aside>
  </main>
`;

const map = new maplibregl.Map({
  container: "map",
  style: {
    version: 8,
    glyphs: "https://demotiles.maplibre.org/font/{fontstack}/{range}.pbf",
    sources: {},
    layers: [{ id: "background", type: "background", paint: { "background-color": "#e4e2d7" } }]
  },
  bounds,
  fitBoundsOptions: { padding: 58, duration: 0 },
  maxBounds: bounds,
  maxPitch: 0,
  pitchWithRotate: false,
  dragRotate: false,
  touchPitch: false,
  bearing: 0,
  minZoom: 12,
  maxZoom: 18
});

map.addControl(new maplibregl.NavigationControl({ showCompass: false }), "bottom-right");
map.addControl(new maplibregl.ScaleControl({ maxWidth: 130, unit: "metric" }), "bottom-left");

const zoneColor = [
  "match", ["get", "zone"],
  "教学区", "#d7ddd1",
  "中庭区", "#ddd3be",
  "工坊区", "#d5d9db",
  "生活区", "#dcd3d0",
  "市集区", "#e1d5b9",
  "封存区", "#d9caca",
  "郊野／外环", "#cad7bd",
  "#d7d7d0"
];

const majorRoadFilter = ["in", ["get", "roadClass"], ["literal", ["主步道", "维护道", "桥梁", "生物列车轨道", "生物列车桥梁"]]];
const minorRoadFilter = ["in", ["get", "roadClass"], ["literal", ["支路", "受控路"]]];
const roadNames = new Map(roads.features.map((feature) => [feature.properties.id, feature.properties.name]));

function setDetail(feature) {
  const { name, zone, description, footprint, frontageRoad, shapeType, roadClass, width, kind, openSpacePurpose, blockId, scaleClass, buildingType, sizeSpec, widthMeters, depthMeters, alignment, streetGapMeters, roadSetbackMeters, address, doorPlate } = feature.properties;
  document.querySelector("#detail-title").textContent = name;
  document.querySelector("#detail-body").textContent = description || "暂无补充说明。";
  const rows = [
    zone && ["区域", zone],
    footprint && ["首层占地", `${Number(footprint).toLocaleString("zh-CN")} m²`],
    shapeType && ["平面原型", shapeType],
    frontageRoad && ["正面道路", roadNames.get(frontageRoad) || frontageRoad],
    address && ["门牌地址", address],
    doorPlate && ["门牌号", doorPlate],
    blockId && ["所属街坊", blockId],
    scaleClass && ["建筑级别", scaleClass],
    buildingType && ["建筑用途", buildingType],
    sizeSpec && ["规格", sizeSpec],
    widthMeters && depthMeters && ["标准尺寸", `${widthMeters} × ${depthMeters} m`],
    alignment && ["排列基线", alignment],
    streetGapMeters && ["标准房间距", `${streetGapMeters} m`],
    roadSetbackMeters && ["临街退距", `${roadSetbackMeters} m`],
    roadClass && ["道路等级", roadClass],
    width && ["规划宽度", `${width} m`],
    openSpacePurpose && ["未绘制空间用途", openSpacePurpose],
    kind && ["对象类型", kind]
  ].filter(Boolean);
  document.querySelector("#detail-meta").innerHTML = rows.map(([key, value]) => `<div><dt>${key}</dt><dd>${value}</dd></div>`).join("");
}

function visibility(layerIds, visible) {
  layerIds.forEach((layerId) => map.setLayoutProperty(layerId, "visibility", visible ? "visible" : "none"));
}

map.on("load", () => {
  map.addSource("zones", { type: "geojson", data: zones });
  map.addSource("terrain", { type: "geojson", data: terrain });
  map.addSource("roads", { type: "geojson", data: roads });
  map.addSource("buildings", { type: "geojson", data: buildings });
  map.addSource("points", { type: "geojson", data: points });

  map.addLayer({ id: "zones-fill", type: "fill", source: "zones", paint: { "fill-color": zoneColor, "fill-opacity": 0.78 } });
  map.addLayer({ id: "zones-line", type: "line", source: "zones", paint: { "line-color": "#7b806f", "line-width": 0.7, "line-opacity": 0.35 } });
  map.addLayer({ id: "terrain-fill", type: "fill", source: "terrain", paint: { "fill-color": ["match", ["get", "kind"], "water", "#8daeb3", "green", "#b7c89e", "park", "#acc58f", "courtyard", "#d9d0b9", "paved", "#d4d0c5", "restricted", "#cfc6c3", "terrain", "#c8d4bb", "sports", "#aebf91", "combat", "#b8a29a", "training", "#aab69d", "#b7c89e"], "fill-opacity": 0.82 } });
  map.addLayer({ id: "terrain-line", type: "line", source: "terrain", paint: { "line-color": ["match", ["get", "kind"], "restricted", "#8c7775", "water", "#6b9197", "#6d8067"], "line-width": 0.6, "line-opacity": 0.55 } });
  map.addLayer({ id: "roads-shadow", type: "line", source: "roads", filter: majorRoadFilter, layout: { "line-cap": "round", "line-join": "round" }, paint: { "line-color": "#aaa493", "line-width": ["interpolate", ["linear"], ["zoom"], 13, 2.4, 18, 11], "line-opacity": 0.62 } });
  map.addLayer({ id: "roads-line", type: "line", source: "roads", filter: majorRoadFilter, layout: { "line-cap": "round", "line-join": "round" }, paint: { "line-color": ["match", ["get", "roadClass"], "主步道", "#f2ede2", "维护道", "#c8c8b7", "桥梁", "#f3ead2", "生物列车轨道", "#7b6954", "生物列车桥梁", "#8d7457", "#ddd7cb"], "line-width": ["interpolate", ["linear"], ["zoom"], 13, ["match", ["get", "roadClass"], "主步道", 1.6, "维护道", 1.2, "桥梁", 1.5, "生物列车轨道", 1.4, "生物列车桥梁", 1.5, 1], 18, ["match", ["get", "roadClass"], "主步道", 10, "维护道", 7, "桥梁", 8, "生物列车轨道", 7, "生物列车桥梁", 8, 6]] } });
  map.addLayer({ id: "roads-minor-shadow", type: "line", source: "roads", filter: minorRoadFilter, minzoom: 12, layout: { "line-cap": "round", "line-join": "round" }, paint: { "line-color": "#9e988b", "line-width": ["interpolate", ["linear"], ["zoom"], 12, 0.9, 14.4, 1.7, 18, 6], "line-opacity": 0.62 } });
  map.addLayer({ id: "roads-minor", type: "line", source: "roads", filter: minorRoadFilter, minzoom: 12, layout: { "line-cap": "round", "line-join": "round" }, paint: { "line-color": ["match", ["get", "roadClass"], "受控路", "#b8aaaa", "#eee8dc"], "line-width": ["interpolate", ["linear"], ["zoom"], 12, 0.55, 14.4, 1.05, 18, ["match", ["get", "roadClass"], "受控路", 5, 4]], "line-opacity": 0.95 } });
  map.addLayer({ id: "transit-rail-sleepers", type: "line", source: "roads", filter: ["has", "routeId"], layout: { "line-cap": "butt", "line-join": "round" }, paint: { "line-color": "#e4c782", "line-width": ["interpolate", ["linear"], ["zoom"], 13, 0.5, 18, 2], "line-dasharray": [1, 1.4] } });
  map.addLayer({ id: "road-labels", type: "symbol", source: "roads", minzoom: 14.6, layout: { "symbol-placement": "line-center", "text-field": ["get", "name"], "text-font": ["Open Sans Semibold"], "text-size": ["interpolate", ["linear"], ["zoom"], 14.6, 9, 18, 12], "text-letter-spacing": 0.04, "text-allow-overlap": true, "text-ignore-placement": false, "text-rotation-alignment": "map" }, paint: { "text-color": ["case", ["has", "routeId"], "#6d5435", "#474741"], "text-halo-color": "#f7f3e9", "text-halo-width": 1.2 } });
  map.addLayer({ id: "building-fill", type: "fill", source: "buildings", paint: { "fill-color": ["case", ["has", "landmarkTier"], "#ead8ac", ["==", ["get", "scaleClass"], "日常服务设施"], "#e8e1c7", "#f4f1e8"], "fill-outline-color": "#6f6e66", "fill-opacity": 0.98 } });
  map.addLayer({ id: "building-line", type: "line", source: "buildings", paint: { "line-color": "#595851", "line-width": ["interpolate", ["linear"], ["zoom"], 12, 0.8, 18, 1.6] } });
  map.addLayer({ id: "points-circle", type: "circle", source: "points", paint: { "circle-color": "#5e5c51", "circle-radius": 4, "circle-stroke-color": "#f7f3e9", "circle-stroke-width": 1.5 } });
  map.addLayer({ id: "zone-labels", type: "symbol", source: "zones", layout: { "text-field": ["get", "name"], "text-font": ["Open Sans Semibold"], "text-size": ["interpolate", ["linear"], ["zoom"], 13, 12, 16, 18], "text-max-width": 8, "text-letter-spacing": 0.04 }, paint: { "text-color": "#4f554d", "text-halo-color": "#ebe9de", "text-halo-width": 1.2 } });
  map.addLayer({ id: "landmark-labels", type: "symbol", source: "buildings", filter: ["has", "landmarkTier"], minzoom: 12, layout: { "text-field": ["get", "name"], "text-font": ["Open Sans Semibold"], "text-size": ["interpolate", ["linear"], ["zoom"], 12, 9, 16, 13], "text-max-width": 8, "text-offset": [0, 1.15], "text-anchor": "top", "text-optional": true }, paint: { "text-color": "#4b3e2a", "text-halo-color": "#faf4e5", "text-halo-width": 1.2 } });
  map.addLayer({ id: "building-labels", type: "symbol", source: "buildings", filter: ["!=", ["get", "scaleClass"], "普通建筑"], minzoom: 16.3, layout: { "text-field": ["get", "name"], "text-font": ["Open Sans Regular"], "text-size": ["interpolate", ["linear"], ["zoom"], 16, 10, 18, 13], "text-max-width": 10, "text-offset": [0, 1.05], "text-anchor": "top", "text-optional": true }, paint: { "text-color": "#393934", "text-halo-color": "#faf7ef", "text-halo-width": 1 } });
  map.addLayer({ id: "house-number-labels", type: "symbol", source: "buildings", minzoom: 16.2, layout: { "text-field": ["get", "doorPlate"], "text-font": ["Open Sans Semibold"], "text-size": ["interpolate", ["linear"], ["zoom"], 16.2, 8, 18, 11], "text-allow-overlap": true, "text-ignore-placement": false }, paint: { "text-color": "#34342f", "text-halo-color": "#faf7ef", "text-halo-width": 0.8 } });
  map.addLayer({ id: "point-labels", type: "symbol", source: "points", minzoom: 15.8, layout: { "text-field": ["get", "name"], "text-font": ["Open Sans Semibold"], "text-size": 11, "text-offset": [0, 1.1], "text-anchor": "top", "text-optional": true }, paint: { "text-color": "#30332e", "text-halo-color": "#f7f4eb", "text-halo-width": 1.1 } });

  const interactiveLayers = ["building-fill", "points-circle", "roads-line", "roads-minor", "terrain-fill", "zones-fill"];
  map.on("mousemove", (event) => {
    const hit = map.queryRenderedFeatures(event.point, { layers: interactiveLayers });
    map.getCanvas().style.cursor = hit.length > 0 ? "pointer" : "";
  });
  map.on("click", (event) => {
    const hit = map.queryRenderedFeatures(event.point, { layers: interactiveLayers });
    const selected = interactiveLayers
      .map((layerId) => hit.find((feature) => feature.layer.id === layerId))
      .find(Boolean);
    if (selected) setDetail(selected);
  });

  document.querySelectorAll("[data-layer]").forEach((checkbox) => {
    checkbox.addEventListener("change", (event) => {
      const enabled = event.currentTarget.checked;
      const groups = {
        zones: ["zones-fill", "zones-line"],
        terrain: ["terrain-fill", "terrain-line"],
        roads: ["roads-shadow", "roads-line", "roads-minor-shadow", "roads-minor", "transit-rail-sleepers"],
        buildings: ["building-fill", "building-line"],
        labels: ["zone-labels", "landmark-labels", "road-labels", "building-labels", "house-number-labels", "point-labels", "points-circle"]
      };
      visibility(groups[event.currentTarget.dataset.layer], enabled);
    });
  });
});
