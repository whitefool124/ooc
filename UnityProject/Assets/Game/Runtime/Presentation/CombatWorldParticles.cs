using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OCC.Combat.Presentation
{
    // Owned by the battlefield world; sampled from the existing action clock, never a new action timer.
    public sealed class CombatWorldParticles : MonoBehaviour
    {
        public const int MaxTransientSlots = 48, MaxFieldSlots = 24, MaxParticlesPerSlot = 96, MaxLights = 4;
        private sealed class Slot
        {
            public ParticleSystem System;
            public string Effect;
            public float Started, Age;
            public bool Seen;
            public int Emitted;
        }
        private readonly Dictionary<CombatVfxSlot, Slot> active = new Dictionary<CombatVfxSlot, Slot>();
        private readonly Dictionary<GridPosition, Slot> fields = new Dictionary<GridPosition, Slot>();
        private readonly Stack<Slot> pool = new Stack<Slot>();
        private readonly List<CombatVfxSlot> expired = new List<CombatVfxSlot>();
        private readonly List<GridPosition> expiredFields = new List<GridPosition>();
        private readonly List<Light> lights = new List<Light>();
        private Mesh cube;
        private Material material;
        public int ActiveSlots => active.Count;
        public int FieldSlots => fields.Count;
        public int PooledSlots => pool.Count;
        public int ActiveParticles { get { int n = 0; foreach (var s in active.Values) n += s.System.particleCount; foreach (var s in fields.Values) n += s.System.particleCount; return n; } }
        public int ActiveLights { get { int n = 0; foreach (var l in lights) if (l.enabled) n++; return n; } }
        public int PeakParticles { get; private set; }
        public Bounds MotionBounds { get; private set; }

        public static CombatWorldParticles Create(Transform world)
        {
            var node = new GameObject("战斗空间粒子池") { layer = world.gameObject.layer };
            node.transform.SetParent(world, false);
            var result = node.AddComponent<CombatWorldParticles>();
            result.Initialize();
            return result;
        }
        private void Initialize()
        {
            // The only mesh is a unit cube. Nonuniform particle size makes one-pixel-section columns.
            cube = new Mesh { name = "战斗粒子共用立方体" };
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var triangles = new List<int>();
            var axes = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var normal in axes)
            {
                Vector3 u = Vector3.Cross(normal, Mathf.Abs(normal.y) > .5f ? Vector3.forward : Vector3.up) * .5f;
                Vector3 v = Vector3.Cross(normal, u.normalized) * .5f; int b = vertices.Count;
                vertices.Add(normal * .5f - u - v); vertices.Add(normal * .5f + u - v);
                vertices.Add(normal * .5f + u + v); vertices.Add(normal * .5f - u + v);
                for (int i = 0; i < 4; i++) normals.Add(normal);
                triangles.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            cube.SetVertices(vertices); cube.SetNormals(normals); cube.SetTriangles(triangles, 0); cube.RecalculateBounds();
            var shader = Resources.Load<Shader>("Shaders/SkyFurnaceSolidPixel");
            if (shader == null) { enabled = false; Debug.LogError("Battle particle shader missing"); return; }
            material = new Material(shader) { name = "战斗粒子共用材质" };
            material.SetColor("_Tint", Color.white); material.SetFloat("_Energy", .85f); material.SetFloat("_LightResponse", .6f);
            for (int i = 0; i < MaxLights; i++)
            {
                var go = new GameObject("战斗瞬态光_" + i) { layer = gameObject.layer }; go.transform.SetParent(transform, false);
                var light = go.AddComponent<Light>(); light.type = LightType.Point; light.shadows = LightShadows.None;
                light.cullingMask = 1 << gameObject.layer; light.range = 2.5f; light.enabled = false; lights.Add(light);
            }
        }
        private Slot Acquire()
        {
            if (pool.Count > 0) { var reused = pool.Pop(); reused.System.gameObject.SetActive(true); return reused; }
            var ps = SkyFurnaceVoxelEmitter.Create(transform, gameObject.layer, "原生战斗粒子", transform.position, cube, material, 7819, MaxParticlesPerSlot);
            var main = ps.main; main.simulationSpace = ParticleSystemSimulationSpace.Custom; main.customSimulationSpace = transform;
            main.startSize3D = true;
            var shape = ps.shape; shape.enabled = false;
            return new Slot { System = ps };
        }
        private void Release(Slot slot)
        {
            slot.System.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            slot.System.gameObject.SetActive(false); slot.Effect = null; slot.Age = 0; slot.Emitted = 0; pool.Push(slot);
        }
        public void Clear()
        {
            foreach (var slot in active.Values) Release(slot); active.Clear();
            foreach (var slot in fields.Values) Release(slot); fields.Clear();
            foreach (var light in lights) light.enabled = false;
        }
        private void OnDisable() => Clear();
        private void OnDestroy()
        {
            if (cube != null) DestroyOwned(cube); if (material != null) DestroyOwned(material);
        }
        private static void DestroyOwned(Object obj) { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }

        public void Sample(IReadOnlyList<CombatVfxSample> samples, float intensity)
        {
            if (!enabled || intensity <= .01f) { Clear(); return; }
            foreach (var slot in active.Values) slot.Seen = false;
            foreach (var light in lights) light.enabled = false;
            int lightIndex = 0;
            foreach (var sample in samples)
            {
                if (!CombatParticleRecipe.TryGet(sample.Cue.Effect, out var recipe)) continue;
                if (!active.TryGetValue(sample.Slot, out var slot))
                {
                    if (active.Count >= MaxTransientSlots) continue;
                    slot = Acquire(); active.Add(sample.Slot, slot);
                }
                float age = sample.Progress * sample.Cue.Duration;
                bool restart = slot.Effect != sample.Cue.Effect || slot.Started != sample.StartedAt || age < slot.Age;
                if (restart)
                {
                    slot.System.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear); slot.Emitted = 0; slot.Age = 0;
                    var main = slot.System.main;
                    main.gravityModifier = recipe.Family == CombatParticleFamily.Debris || recipe.Family == CombatParticleFamily.Break ? .5f : 0f;
                }
                slot.Effect = sample.Cue.Effect; slot.Started = sample.StartedAt; slot.Seen = true;
                Vector3 origin = Anchor(sample.Cue.Origin, .9f), target = Anchor(sample.Cue.Position, .9f);
                Vector3 direction = target - origin; direction.y = 0; if (direction.sqrMagnitude < .001f) direction = Vector3.forward;
                direction.Normalize();
                bool stream = recipe.Family == CombatParticleFamily.Projectile || recipe.Family == CombatParticleFamily.Line || recipe.Family == CombatParticleFamily.Spray;
                int count = Mathf.Max(4, Mathf.RoundToInt(recipe.Count * Mathf.Clamp01(intensity)));
                int desired = stream ? Mathf.Max(1, Mathf.CeilToInt(count * sample.Progress)) : count;
                // Advance old particles first; newly emitted particles start at the current delivery point.
                slot.System.Simulate(Mathf.Max(0, age - slot.Age), false, false, false);
                for (int i = slot.Emitted; i < desired; i++)
                {
                    float delivery = stream ? i / (float)count : sample.Progress;
                    Vector3 anchor = sample.Cue.Travels || recipe.Family == CombatParticleFamily.Line || recipe.Family == CombatParticleFamily.Spray
                        ? Vector3.Lerp(origin, target, delivery) : target;
                    Emit(slot, recipe, i, count, anchor, direction, sample.Cue.Duration, false);
                }
                slot.Emitted = desired; slot.Age = age;
                if (recipe.Light > 0 && lightIndex < lights.Count)
                {
                    var light = lights[lightIndex++]; light.transform.localPosition = target + Vector3.up * .35f;
                    light.color = recipe.Color; light.intensity = recipe.Light * Mathf.Sin(sample.Progress * Mathf.PI) * Mathf.Clamp01(intensity); light.enabled = true;
                }
            }
            expired.Clear(); foreach (var pair in active) if (!pair.Value.Seen) expired.Add(pair.Key);
            foreach (var key in expired) { Release(active[key]); active.Remove(key); }
            Observe();
        }
        public void SampleFields(IReadOnlyList<GridPosition> positions, float now, float intensity)
        {
            foreach (var slot in fields.Values) slot.Seen = false;
            if (intensity > .01f && enabled)
                foreach (var position in positions)
                {
                    if (!fields.TryGetValue(position, out var slot))
                    {
                        if (fields.Count >= MaxFieldSlots) continue; slot = Acquire(); slot.Started = now; slot.Age = 0;
                        var main = slot.System.main; main.gravityModifier = 0f; fields.Add(position, slot);
                    }
                    slot.Seen = true; float age = Mathf.Max(0, now - slot.Started);
                    slot.System.Simulate(Mathf.Max(0, age - slot.Age), false, false, false);
                    // Bounded catch-up prevents a paused frame emitting a storm.
                    int tick = Mathf.FloorToInt(age * 12f);
                    if (tick > slot.Emitted)
                    {
                        CombatParticleRecipe.TryGet("fire_burning_ground", out var recipe);
                        int count = Mathf.Max(1, Mathf.RoundToInt(3 * Mathf.Clamp01(intensity)));
                        for (int i = 0; i < count; i++) Emit(slot, recipe, tick * 3 + i, 24, Anchor(position, .06f), Vector3.forward, .6f, true);
                    }
                    slot.Emitted = tick; slot.Age = age;
                }
            expiredFields.Clear(); foreach (var pair in fields) if (!pair.Value.Seen) expiredFields.Add(pair.Key);
            foreach (var key in expiredFields) { Release(fields[key]); fields.Remove(key); }
            Observe();
        }
        private static Vector3 Anchor(GridPosition position, float height) => new Vector3(position.X + .5f, height, position.Y + .5f);
        private static void Emit(Slot slot, CombatParticleRecipe r, int i, int count, Vector3 anchor, Vector3 direction, float duration, bool field)
        {
            // Arithmetic variation never consumes UnityEngine.Random or the combat RNG.
            float angle = i * 2.399963f, f = .35f + .65f * ((i * 37 % 101) / 100f);
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 position = radial * r.Radius * f, velocity = radial * r.Speed * f + Vector3.up * (.25f + f);
            var family = r.Family;
            if (family == CombatParticleFamily.Cast || family == CombatParticleFamily.Recovery)
            { position = radial * (.24f + .2f * f) + Vector3.up * (.2f + .3f * f); velocity = -position / Mathf.Max(.1f, duration); }
            if (family == CombatParticleFamily.Cleanse) { position = radial * .2f; velocity = radial * .7f + Vector3.up * 1.4f; }
            if (family == CombatParticleFamily.Bind || family == CombatParticleFamily.Slow)
            { anchor.y = .1f; position = radial * (.25f + .1f * f); velocity = family == CombatParticleFamily.Bind ? -radial * .3f + Vector3.up * .7f : radial * .15f; }
            if (family == CombatParticleFamily.Attribute) { position = radial * .2f; velocity = Vector3.up * (.7f + f); }
            if (family == CombatParticleFamily.Movement) { anchor.y = .05f; position *= .6f; velocity = radial * .6f + Vector3.up * .2f; }
            if (family == CombatParticleFamily.Arc)
            {
                float arc = (i / (float)count - .5f) * 2.5f;
                radial = Quaternion.AngleAxis(arc * Mathf.Rad2Deg, Vector3.up) * direction;
                position = radial * .32f; velocity = radial * 1.8f;
            }
            if (family == CombatParticleFamily.Projectile || family == CombatParticleFamily.Line || family == CombatParticleFamily.Spray)
            { position *= .3f; velocity = -direction * .4f + radial * (family == CombatParticleFamily.Spray ? .8f : .15f); }
            if (family == CombatParticleFamily.Fireground || family == CombatParticleFamily.Burning)
            { if (!field) anchor.y = .08f; position = radial * .3f * f; velocity = Vector3.up * (.5f + f * .6f); }
            bool column = family == CombatParticleFamily.Projectile || family == CombatParticleFamily.Line || family == CombatParticleFamily.Arc;
            Color color = r.Color * (i % 5 == 0 ? .42f : 1f); color.a = 1;
            var parameters = new ParticleSystem.EmitParams {
                position = anchor + position, velocity = velocity, startColor = color,
                startLifetime = field ? .45f + f * .15f : Mathf.Max(.08f, duration * (.55f + .45f * f)),
                startSize3D = new Vector3(r.PixelMetres, r.PixelMetres, r.PixelMetres * (column ? 4f : 1f)),
                rotation3D = column ? Quaternion.LookRotation(direction).eulerAngles : Vector3.zero,
                randomSeed = (uint)(i + 1)
            };
            slot.System.Emit(parameters, 1);
        }
        private readonly ParticleSystem.Particle[] readback = new ParticleSystem.Particle[MaxParticlesPerSlot];
        private void Observe()
        {
            PeakParticles = Mathf.Max(PeakParticles, ActiveParticles);
            Bounds bounds = default; bool any = false;
            foreach (var slot in active.Values) Include(slot, ref bounds, ref any);
            foreach (var slot in fields.Values) Include(slot, ref bounds, ref any);
            MotionBounds = bounds;
        }
        private void Include(Slot slot, ref Bounds bounds, ref bool any)
        {
            int count = slot.System.GetParticles(readback);
            for (int i = 0; i < count; i++)
            { Vector3 point = readback[i].position; if (!any) { bounds = new Bounds(point, Vector3.one / 8f); any = true; } else bounds.Encapsulate(point); }
        }
    }
}
