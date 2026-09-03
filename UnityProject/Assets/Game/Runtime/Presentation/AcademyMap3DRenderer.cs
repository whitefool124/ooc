using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class AcademyMapDistrictSpec
    {
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<Vector2> MapPolygon { get; }
        public Color Fill { get; }
        public Color Accent { get; }

        public AcademyMapDistrictSpec(string id, string displayName, Color fill, Color accent, params Vector2[] mapPolygon)
        {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentNullException(nameof(id)) : id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentNullException(nameof(displayName)) : displayName;
            if (mapPolygon == null || mapPolygon.Length < 3) throw new ArgumentException("A district needs at least three map points.", nameof(mapPolygon));
            Fill = fill;
            Accent = accent;
            MapPolygon = mapPolygon;
        }
    }

    public sealed class AcademyMapRoadSpec
    {
        public string Id { get; }
        public float Width { get; }
        public IReadOnlyList<Vector2> ControlPoints { get; }

        public AcademyMapRoadSpec(string id, float width, params Vector2[] controlPoints)
        {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentNullException(nameof(id)) : id;
            if (width <= 0f) throw new ArgumentOutOfRangeException(nameof(width));
            if (controlPoints == null || controlPoints.Length < 4)
                throw new ArgumentException("A curved orientation road needs at least four control points.", nameof(controlPoints));
            Width = width;
            ControlPoints = controlPoints;
        }
    }

    // Fixed coarse backdrop. Nodes, routes, states and click targets stay in the separate UGUI layer.
    public static class AcademyMap3DLayout
    {
        public const int RenderWidth = 1536;
        public const int RenderHeight = 864;
        public const int RenderAntiAliasing = 2;
        public const float WorldWidth = 24f;
        public const float WorldDepth = 13.5f;
        public const float CameraPitch = 90f;
        public const float CameraYaw = 0f;
        public const float CameraOrthographicSize = 6.75f;
        public const int OrientationRoadCount = 7;
        public const int DefenceSegmentCount = 8;

        private static Color C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

        public static readonly IReadOnlyList<AcademyMapDistrictSpec> Districts = new[]
        {
            new AcademyMapDistrictSpec("teaching_archive", "教学与档案区", C(224, 214, 193), C(124, 103, 73),
                new Vector2(100, 100), new Vector2(520, 80), new Vector2(550, 330), new Vector2(100, 330)),
            new AcademyMapDistrictSpec("workshop", "工坊与训练区", C(205, 190, 163), C(126, 90, 49),
                new Vector2(520, 80), new Vector2(1030, 80), new Vector2(1060, 330), new Vector2(550, 330)),
            new AcademyMapDistrictSpec("sealed_tower", "封存高塔区", C(173, 161, 150), C(99, 67, 62),
                new Vector2(1030, 80), new Vector2(1440, 130), new Vector2(1460, 340), new Vector2(1060, 330)),
            new AcademyMapDistrictSpec("entrance", "学院入口区", C(236, 226, 207), C(136, 109, 68),
                new Vector2(100, 330), new Vector2(550, 330), new Vector2(540, 590), new Vector2(100, 600)),
            new AcademyMapDistrictSpec("dormitory", "宿舍庭院区", C(210, 216, 194), C(88, 109, 76),
                new Vector2(100, 600), new Vector2(540, 590), new Vector2(520, 810), new Vector2(120, 760)),
            new AcademyMapDistrictSpec("wilds", "郊野与实训区", C(184, 202, 173), C(78, 105, 72),
                new Vector2(1060, 330), new Vector2(1460, 340), new Vector2(1460, 580), new Vector2(1040, 590)),
            new AcademyMapDistrictSpec("market_medical", "市集与医务区", C(226, 220, 196), C(118, 105, 70),
                new Vector2(540, 590), new Vector2(1040, 590), new Vector2(1030, 810), new Vector2(520, 810)),
            new AcademyMapDistrictSpec("harbour_logistics", "港口与物流区", C(188, 202, 196), C(72, 103, 100),
                new Vector2(1040, 590), new Vector2(1460, 580), new Vector2(1420, 800), new Vector2(1030, 810)),
            new AcademyMapDistrictSpec("central_public", "中央图书馆公共区", C(246, 239, 221), C(126, 93, 42),
                new Vector2(550, 330), new Vector2(1060, 330), new Vector2(1040, 590), new Vector2(540, 590))
        };

        public static readonly IReadOnlyList<AcademyMapRoadSpec> Roads = new[]
        {
            new AcademyMapRoadSpec("entrance_to_central", 16f, new Vector2(110, 465), new Vector2(260, 445), new Vector2(420, 470), new Vector2(610, 445), new Vector2(770, 430)),
            new AcademyMapRoadSpec("academic_curve", 13f, new Vector2(245, 180), new Vector2(420, 210), new Vector2(570, 280), new Vector2(690, 350), new Vector2(770, 430)),
            new AcademyMapRoadSpec("workshop_curve", 13f, new Vector2(770, 430), new Vector2(875, 365), new Vector2(995, 292), new Vector2(1165, 250), new Vector2(1320, 280)),
            new AcademyMapRoadSpec("dormitory_curve", 12f, new Vector2(760, 440), new Vector2(690, 520), new Vector2(610, 605), new Vector2(505, 680), new Vector2(355, 720)),
            new AcademyMapRoadSpec("market_curve", 12f, new Vector2(790, 440), new Vector2(815, 525), new Vector2(820, 620), new Vector2(790, 710), new Vector2(720, 780)),
            new AcademyMapRoadSpec("eastern_commons", 13f, new Vector2(790, 430), new Vector2(930, 445), new Vector2(1080, 480), new Vector2(1230, 500), new Vector2(1390, 470)),
            new AcademyMapRoadSpec("port_curve", 14f, new Vector2(1125, 470), new Vector2(1175, 540), new Vector2(1220, 615), new Vector2(1280, 690), new Vector2(1370, 750))
        };

        public static Vector3 MapToWorld(Vector2 mapPosition, float height = 0f)
        {
            return new Vector3((mapPosition.x / AcademyMapVisualLayout.SourceSize.x - .5f) * WorldWidth, height,
                (.5f - mapPosition.y / AcademyMapVisualLayout.SourceSize.y) * WorldDepth);
        }

        public static Vector2 MapSizeToWorld(Vector2 mapSize)
        {
            return new Vector2(mapSize.x / AcademyMapVisualLayout.SourceSize.x * WorldWidth,
                mapSize.y / AcademyMapVisualLayout.SourceSize.y * WorldDepth);
        }

        public static Vector2 ProjectMapToCanvas(Vector2 mapPosition, float worldHeight = .5f)
        {
            Quaternion rotation = Quaternion.Euler(CameraPitch, CameraYaw, 0);
            Vector3 cameraPosition = -(rotation * Vector3.forward) * 24f + new Vector3(0, .5f, 0);
            Vector3 cameraLocal = Quaternion.Inverse(rotation) * (MapToWorld(mapPosition, worldHeight) - cameraPosition);
            float halfHeight = CameraOrthographicSize;
            float halfWidth = halfHeight * RenderWidth / RenderHeight;
            float viewportX = .5f + cameraLocal.x / (halfWidth * 2f);
            float viewportY = .5f + cameraLocal.y / (halfHeight * 2f);
            return new Vector2((viewportX - .5f) * AcademyMapVisualLayout.LogicalCanvasSize.x,
                (viewportY - .5f) * AcademyMapVisualLayout.LogicalCanvasSize.y);
        }

        public static bool PointInPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 3) return false;
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                bool crosses = a.y > point.y != b.y > point.y &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (crosses) inside = !inside;
            }
            return inside;
        }

        public static bool PolygonsOverlap(IReadOnlyList<Vector2> left, IReadOnlyList<Vector2> right)
        {
            if (left == null || right == null || left.Count < 3 || right.Count < 3) return false;
            for (int i = 0; i < left.Count; i++)
                if (PointInPolygonStrict(left[i], right)) return true;
            for (int i = 0; i < right.Count; i++)
                if (PointInPolygonStrict(right[i], left)) return true;
            for (int i = 0; i < left.Count; i++)
            for (int j = 0; j < right.Count; j++)
                if (SegmentsCrossProperly(left[i], left[(i + 1) % left.Count], right[j], right[(j + 1) % right.Count])) return true;
            return false;
        }

        private static bool PointInPolygonStrict(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            for (int i = 0; i < polygon.Count; i++)
                if (PointOnSegment(point, polygon[i], polygon[(i + 1) % polygon.Count])) return false;
            return PointInPolygon(point, polygon);
        }

        private static bool PointOnSegment(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 edge = to - from;
            Vector2 offset = point - from;
            if (Mathf.Abs(edge.x * offset.y - edge.y * offset.x) > .01f) return false;
            float dot = Vector2.Dot(offset, edge);
            return dot >= -.01f && dot <= edge.sqrMagnitude + .01f;
        }

        private static bool SegmentsCrossProperly(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float abC = Cross(b - a, c - a);
            float abD = Cross(b - a, d - a);
            float cdA = Cross(d - c, a - c);
            float cdB = Cross(d - c, b - c);
            return abC * abD < -.01f && cdA * cdB < -.01f;
        }

        private static float Cross(Vector2 left, Vector2 right) => left.x * right.y - left.y * right.x;
    }

    // Existing component name is preserved so the runtime composition and scene remain untouched.
    public sealed class AcademyMap3DRenderer : MonoBehaviour
    {
        private const int MapLayer = 30;
        private static readonly Color Water = new Color32(91, 116, 117, 255);
        private static readonly Color Land = new Color32(202, 194, 174, 255);
        private static readonly Color Boundary = new Color32(55, 59, 54, 255);
        private static readonly Color RoadEdge = new Color32(76, 71, 61, 255);
        private static readonly Color Road = new Color32(239, 229, 207, 255);
        private static readonly Color Defence = new Color32(105, 86, 62, 255);
        private static readonly Color Library = new Color32(250, 245, 231, 255);
        private static readonly Color Aether = new Color32(46, 123, 130, 255);

        private GameObject renderRoot;
        private RenderTexture target;
        private Camera mapCamera;
        private readonly List<Material> materials = new List<Material>();

        public Texture Output => target;
        public Camera RenderCamera => mapCamera;

        public void Initialize(RawImage targetImage)
        {
            if (targetImage == null) throw new ArgumentNullException(nameof(targetImage));
            Release();
            target = new RenderTexture(AcademyMap3DLayout.RenderWidth, AcademyMap3DLayout.RenderHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = "OCC_学院地块地图_1536x864",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = AcademyMap3DLayout.RenderAntiAliasing,
                useMipMap = false,
                autoGenerateMips = false
            };
            target.Create();
            renderRoot = new GameObject("学院地块地图_运行时");
            renderRoot.layer = MapLayer;
            BuildBoard(renderRoot.transform);
            CreateCamera(renderRoot.transform);
            targetImage.texture = target;
            targetImage.color = Color.white;
            targetImage.raycastTarget = true;
            mapCamera.Render();
        }

        private void BuildBoard(Transform root)
        {
            CreateBlock(root, "水域底色", Vector3.down * .12f, new Vector3(30f, .12f, 18f), Water);
            Vector2[] landOutline =
            {
                new Vector2(70, 180), new Vector2(190, 80), new Vector2(580, 45), new Vector2(950, 65),
                new Vector2(1300, 80), new Vector2(1460, 170), new Vector2(1490, 580), new Vector2(1430, 805),
                new Vector2(1020, 830), new Vector2(620, 825), new Vector2(240, 790), new Vector2(70, 650)
            };
            CreatePolygon(root, "学院地块外缘", landOutline, .01f, Boundary, 1.018f);
            CreatePolygon(root, "学院地块基底", landOutline, .02f, Land, 1f);

            foreach (AcademyMapDistrictSpec district in AcademyMap3DLayout.Districts)
            {
                CreatePolygon(root, district.Id + "_地块", district.MapPolygon, .045f, district.Fill, 1f);
                CreateDistrictMarker(root, district);
            }

            CreateDistrictBoundaries(root);
            foreach (AcademyMapRoadSpec roadSpec in AcademyMap3DLayout.Roads) CreateCurvedRoad(root, roadSpec);

            CreateCentralLibrarySymbol(root);
            CreateDefencePerimeter(root);
        }

        private void CreateDistrictBoundaries(Transform root)
        {
            var drawn = new HashSet<string>();
            foreach (AcademyMapDistrictSpec district in AcademyMap3DLayout.Districts)
            for (int i = 0; i < district.MapPolygon.Count; i++)
            {
                Vector2 from = district.MapPolygon[i];
                Vector2 to = district.MapPolygon[(i + 1) % district.MapPolygon.Count];
                string forward = from.x + "," + from.y + ":" + to.x + "," + to.y;
                string reverse = to.x + "," + to.y + ":" + from.x + "," + from.y;
                string key = string.CompareOrdinal(forward, reverse) < 0 ? forward : reverse;
                if (!drawn.Add(key)) continue;
                CreateRoad(root, "分区边界_" + drawn.Count, from, to, 5f, Boundary, .061f);
            }
        }

        private void CreateCurvedRoad(Transform root, AcademyMapRoadSpec spec)
        {
            IReadOnlyList<Vector2> samples = SmoothRoad(spec.ControlPoints, 8);
            CreateRibbon(root, spec.Id + "_边", samples, spec.Width + 7f, .069f, RoadEdge);
            CreateRibbon(root, spec.Id, samples, spec.Width, .075f, Road);
        }

        private static IReadOnlyList<Vector2> SmoothRoad(IReadOnlyList<Vector2> controlPoints, int samplesPerSpan)
        {
            var result = new List<Vector2>((controlPoints.Count - 1) * samplesPerSpan + 1);
            for (int span = 0; span < controlPoints.Count - 1; span++)
            {
                Vector2 p0 = controlPoints[Mathf.Max(0, span - 1)];
                Vector2 p1 = controlPoints[span];
                Vector2 p2 = controlPoints[span + 1];
                Vector2 p3 = controlPoints[Mathf.Min(controlPoints.Count - 1, span + 2)];
                for (int step = 0; step < samplesPerSpan; step++)
                {
                    float t = step / (float)samplesPerSpan;
                    float t2 = t * t;
                    float t3 = t2 * t;
                    result.Add(.5f * ((2f * p1) + (-p0 + p2) * t +
                        (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                        (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            result.Add(controlPoints[controlPoints.Count - 1]);
            return result;
        }

        private void CreateRibbon(Transform root, string name, IReadOnlyList<Vector2> mapPoints, float mapWidth, float y, Color color)
        {
            float halfWidth = AcademyMap3DLayout.MapSizeToWorld(new Vector2(mapWidth, mapWidth)).x * .5f;
            var centers = new Vector3[mapPoints.Count];
            var vertices = new Vector3[mapPoints.Count * 2];
            for (int i = 0; i < mapPoints.Count; i++) centers[i] = AcademyMap3DLayout.MapToWorld(mapPoints[i], y);
            for (int i = 0; i < centers.Length; i++)
            {
                Vector3 previous = centers[Mathf.Max(0, i - 1)];
                Vector3 next = centers[Mathf.Min(centers.Length - 1, i + 1)];
                Vector3 tangent = (next - previous).normalized;
                Vector3 normal = new Vector3(-tangent.z, 0, tangent.x) * halfWidth;
                vertices[i * 2] = centers[i] + normal;
                vertices[i * 2 + 1] = centers[i] - normal;
            }
            var triangles = new int[(mapPoints.Count - 1) * 6];
            for (int i = 0; i < mapPoints.Count - 1; i++)
            {
                int offset = i * 6;
                int current = i * 2;
                int next = current + 2;
                triangles[offset] = current;
                triangles[offset + 1] = next;
                triangles[offset + 2] = current + 1;
                triangles[offset + 3] = current + 1;
                triangles[offset + 4] = next;
                triangles[offset + 5] = next + 1;
            }
            var mesh = new Mesh { name = name + "_Mesh", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            GameObject value = new GameObject(name);
            value.layer = MapLayer;
            value.transform.SetParent(root, false);
            value.AddComponent<MeshFilter>().sharedMesh = mesh;
            value.AddComponent<MeshRenderer>().sharedMaterial = Material(color);
        }

        private void CreateDistrictMarker(Transform root, AcademyMapDistrictSpec district)
        {
            Vector2 center = Vector2.zero;
            for (int i = 0; i < district.MapPolygon.Count; i++) center += district.MapPolygon[i];
            center /= district.MapPolygon.Count;
            Vector3 world = AcademyMap3DLayout.MapToWorld(center, .075f);
            Vector2 size = AcademyMap3DLayout.MapSizeToWorld(new Vector2(42, 42));
            CreateBlock(root, district.Id + "_定位标", world, new Vector3(size.x, .045f, size.y), district.Accent);
        }

        private void CreateCentralLibrarySymbol(Transform root)
        {
            Vector3 center = AcademyMap3DLayout.MapToWorld(AcademyMapVisualLayout.SourceSize * .5f, .105f);
            Vector2 hall = AcademyMap3DLayout.MapSizeToWorld(new Vector2(170, 92));
            CreateBlock(root, "中央图书馆_轮廓", center + Vector3.down * .015f,
                new Vector3(hall.x + .10f, .055f, hall.y + .10f), Boundary);
            CreateBlock(root, "中央图书馆_阅览厅", center, new Vector3(hall.x, .09f, hall.y), Library);
            Vector2 rotunda = AcademyMap3DLayout.MapSizeToWorld(new Vector2(70, 70));
            CreateBlock(root, "中央图书馆_档案圆厅", center + new Vector3(-hall.x * .45f, .03f, 0),
                new Vector3(rotunda.x, .12f, rotunda.y), Aether);
        }

        private void CreateDefencePerimeter(Transform root)
        {
            CreateRoad(root, "防线_西北", new Vector2(105, 210), new Vector2(235, 105), 12, Defence, .085f);
            CreateRoad(root, "防线_北一", new Vector2(235, 105), new Vector2(690, 72), 12, Defence, .085f);
            CreateRoad(root, "防线_北二", new Vector2(690, 72), new Vector2(1250, 105), 12, Defence, .085f);
            CreateRoad(root, "防线_东北", new Vector2(1250, 105), new Vector2(1445, 205), 12, Defence, .085f);
            CreateRoad(root, "防线_东", new Vector2(1445, 205), new Vector2(1460, 555), 12, Defence, .085f);
            CreateRoad(root, "防线_东南", new Vector2(1460, 555), new Vector2(1400, 760), 12, Defence, .085f);
            CreateRoad(root, "防线_南", new Vector2(1010, 802), new Vector2(590, 798), 12, Defence, .085f);
            CreateRoad(root, "防线_西南", new Vector2(590, 798), new Vector2(170, 725), 12, Defence, .085f);
        }

        private void CreateRoad(Transform root, string name, Vector2 fromMap, Vector2 toMap, float mapWidth,
            Color? color = null, float y = .075f)
        {
            Vector3 from = AcademyMap3DLayout.MapToWorld(fromMap, y);
            Vector3 to = AcademyMap3DLayout.MapToWorld(toMap, y);
            Vector3 delta = to - from;
            float width = AcademyMap3DLayout.MapSizeToWorld(new Vector2(mapWidth, mapWidth)).x;
            float yaw = -Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg;
            if (!color.HasValue)
                CreateBlock(root, name + "_边", (from + to) * .5f + Vector3.down * .006f,
                    new Vector3(delta.magnitude + .05f, .035f, width + .08f), RoadEdge, yaw);
            CreateBlock(root, name, (from + to) * .5f,
                new Vector3(delta.magnitude, .04f, width), color ?? Road, yaw);
        }

        private void CreateCamera(Transform root)
        {
            GameObject cameraObject = new GameObject("学院地块地图相机");
            cameraObject.layer = MapLayer;
            cameraObject.transform.SetParent(root, false);
            mapCamera = cameraObject.AddComponent<Camera>();
            mapCamera.orthographic = true;
            mapCamera.orthographicSize = AcademyMap3DLayout.CameraOrthographicSize;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = Water;
            mapCamera.cullingMask = 1 << MapLayer;
            mapCamera.allowHDR = false;
            mapCamera.allowMSAA = true;
            mapCamera.useOcclusionCulling = false;
            mapCamera.targetTexture = target;
            Quaternion rotation = Quaternion.Euler(AcademyMap3DLayout.CameraPitch, AcademyMap3DLayout.CameraYaw, 0);
            mapCamera.transform.rotation = rotation;
            mapCamera.transform.position = -(rotation * Vector3.forward) * 24f + new Vector3(0, .5f, 0);
        }

        private void CreatePolygon(Transform root, string name, IReadOnlyList<Vector2> mapPoints, float y, Color color, float scale)
        {
            Vector2 center = Vector2.zero;
            for (int i = 0; i < mapPoints.Count; i++) center += mapPoints[i];
            center /= mapPoints.Count;
            var vertices = new Vector3[mapPoints.Count];
            for (int i = 0; i < mapPoints.Count; i++)
                vertices[i] = AcademyMap3DLayout.MapToWorld(center + (mapPoints[i] - center) * scale, y);
            var triangles = new int[(mapPoints.Count - 2) * 3];
            for (int i = 0; i < mapPoints.Count - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            var mesh = new Mesh { name = name + "_Mesh", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            GameObject value = new GameObject(name);
            value.layer = MapLayer;
            value.transform.SetParent(root, false);
            value.AddComponent<MeshFilter>().sharedMesh = mesh;
            value.AddComponent<MeshRenderer>().sharedMaterial = Material(color);
        }

        private void CreateBlock(Transform root, string name, Vector3 position, Vector3 size, Color color, float yaw = 0f)
        {
            GameObject value = GameObject.CreatePrimitive(PrimitiveType.Cube);
            value.name = name;
            value.layer = MapLayer;
            value.transform.SetParent(root, false);
            value.transform.localPosition = position;
            value.transform.localScale = size;
            value.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            value.GetComponent<MeshRenderer>().sharedMaterial = Material(color);
            Destroy(value.GetComponent<Collider>());
        }

        private Material Material(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material material = new Material(shader) { name = "地块材质_" + ColorUtility.ToHtmlStringRGB(color) };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            materials.Add(material);
            return material;
        }

        private void OnDestroy() => Release();

        private void Release()
        {
            if (renderRoot != null) Destroy(renderRoot);
            if (target != null)
            {
                target.Release();
                Destroy(target);
            }
            foreach (Material material in materials)
                if (material != null) Destroy(material);
            materials.Clear();
            renderRoot = null;
            target = null;
            mapCamera = null;
        }
    }
}
