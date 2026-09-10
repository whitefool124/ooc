using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public enum ToonParticleRecipe
    {
        Impact,
        Ember,
        Ripple,
        Spark
    }

    /// <summary>
    /// Uses Unity's ParticleSystem for deterministic simulation, then draws the particles through
    /// a MaskableGraphic so they remain visible and clipped inside the overlay-canvas battlefield.
    /// Source textures are a small, traceable subset of Epic Toon FX v1.8.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer), typeof(ParticleSystem))]
    public sealed class ToonParticleUiEffect : MaskableGraphic
    {
        private const string TextureRoot = "Art/PrototypeToonParticles/EpicToonFxSource/";
        private const int Capacity = 32;

        private readonly ParticleSystem.Particle[] particles = new ParticleSystem.Particle[Capacity];
        private ParticleSystem simulation;
        private Texture2D particleTexture;
        private string activeEffect;
        private ToonParticleRecipe recipe;
        private int tilesX = 1;
        private int tilesY = 1;
        private float quadAspect = 1f;

        public string ActiveEffect => activeEffect;
        public ToonParticleRecipe ActiveRecipe => recipe;
        public override Texture mainTexture => particleTexture != null ? particleTexture : s_WhiteTexture;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            EnsureSimulation();
        }

        public static ToonParticleRecipe RecipeFor(string effect)
        {
            if (string.IsNullOrEmpty(effect)) return ToonParticleRecipe.Spark;
            if (effect.StartsWith("fire_", StringComparison.Ordinal) || effect == "burning")
            {
                if (effect.Contains("impact") || effect.Contains("detonate") || effect.Contains("blast") ||
                    effect.Contains("break")) return ToonParticleRecipe.Impact;
                return ToonParticleRecipe.Ember;
            }
            if (effect.Contains("shield") || effect.Contains("restore") || effect == "cleanse" ||
                effect == "revealed") return ToonParticleRecipe.Ripple;
            if (effect.Contains("hit") || effect.Contains("break") || effect.Contains("damage"))
                return ToonParticleRecipe.Impact;
            return ToonParticleRecipe.Spark;
        }

        public void PlayIfChanged(string effect, float cellSize)
        {
            EnsureSimulation();
            if (activeEffect == effect && simulation.IsAlive(true)) return;

            activeEffect = effect;
            recipe = RecipeFor(effect);
            ConfigureRecipe(recipe);
            RectTransform rect = rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.one * Mathf.Max(96f, cellSize * 3f);

            simulation.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            simulation.randomSeed = StableSeed(effect);
            simulation.Play(false);
            EmitRecipe(recipe, Mathf.Max(24f, cellSize));
            SetMaterialDirty();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (simulation == null || particleTexture == null) return;

            int count = simulation.GetParticles(particles);
            for (int i = 0; i < count; i++)
            {
                ParticleSystem.Particle particle = particles[i];
                float size = particle.GetCurrentSize(simulation);
                float halfWidth = size * quadAspect * .5f;
                float halfHeight = size * .5f;
                float radians = -particle.rotation * Mathf.Deg2Rad;
                float sin = Mathf.Sin(radians);
                float cos = Mathf.Cos(radians);
                Vector2 center = particle.position;
                Vector2 right = new Vector2(cos, sin) * halfWidth;
                Vector2 up = new Vector2(-sin, cos) * halfHeight;
                Color32 tint = particle.GetCurrentColor(simulation);
                ResolveUvs(particle, out Vector2 uvMin, out Vector2 uvMax);

                int first = vertexHelper.currentVertCount;
                vertexHelper.AddVert(center - right - up, tint, new Vector2(uvMin.x, uvMin.y));
                vertexHelper.AddVert(center - right + up, tint, new Vector2(uvMin.x, uvMax.y));
                vertexHelper.AddVert(center + right + up, tint, new Vector2(uvMax.x, uvMax.y));
                vertexHelper.AddVert(center + right - up, tint, new Vector2(uvMax.x, uvMin.y));
                vertexHelper.AddTriangle(first, first + 1, first + 2);
                vertexHelper.AddTriangle(first, first + 2, first + 3);
            }
        }

        private void LateUpdate()
        {
            if (simulation == null) return;
            if (simulation.IsAlive(true)) SetVerticesDirty();
            else if (canvasRenderer != null) canvasRenderer.Clear();
        }

        private void EnsureSimulation()
        {
            if (simulation != null) return;
            simulation = GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
            if (renderer != null) renderer.enabled = false;
            var main = simulation.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = Capacity;
            main.startSpeed = 0f;
            var emission = simulation.emission;
            emission.enabled = false;
            var shape = simulation.shape;
            shape.enabled = false;
        }

        private void ConfigureRecipe(ToonParticleRecipe next)
        {
            string textureName;
            switch (next)
            {
                case ToonParticleRecipe.Impact:
                    textureName = "explosion_sharp_soft_3x3";
                    tilesX = 3; tilesY = 3; quadAspect = 1.72f;
                    break;
                case ToonParticleRecipe.Ember:
                    textureName = "fire_soft_blank";
                    tilesX = tilesY = 1; quadAspect = 1f;
                    break;
                case ToonParticleRecipe.Ripple:
                    textureName = "ring";
                    tilesX = tilesY = 1; quadAspect = 1f;
                    break;
                default:
                    textureName = "circle";
                    tilesX = tilesY = 1; quadAspect = 1f;
                    break;
            }
            particleTexture = Resources.Load<Texture2D>(TextureRoot + textureName);
            if (particleTexture == null)
                Debug.LogError("Missing prototype toon particle texture: " + TextureRoot + textureName, this);

            var size = simulation.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, SizeCurve(next));
            var colorOverLifetime = simulation.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(FadeGradient());
        }

        private void EmitRecipe(ToonParticleRecipe next, float cellSize)
        {
            var random = new System.Random((int)simulation.randomSeed);
            Color tint = TintFor(activeEffect);
            if (next == ToonParticleRecipe.Impact)
            {
                Emit(Vector2.zero, Vector2.zero, cellSize * 1.45f, .34f, tint, 0f);
                return;
            }
            if (next == ToonParticleRecipe.Ripple)
            {
                Emit(Vector2.zero, Vector2.zero, cellSize * .55f, .42f, tint, 0f);
                Emit(Vector2.zero, Vector2.zero, cellSize * .78f, .34f, WithAlpha(tint, .72f), 18f);
                return;
            }

            int count = next == ToonParticleRecipe.Ember ? 9 : 14;
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(random.NextDouble() * Mathf.PI * 2f);
                float speed = Mathf.Lerp(cellSize * .55f, cellSize * 1.25f, (float)random.NextDouble());
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 velocity = next == ToonParticleRecipe.Ember
                    ? new Vector2(direction.x * speed * .35f, Mathf.Abs(direction.y) * speed + cellSize * .25f)
                    : direction * speed;
                Vector2 position = next == ToonParticleRecipe.Ember
                    ? new Vector2(Mathf.Lerp(-cellSize * .28f, cellSize * .28f, (float)random.NextDouble()), -cellSize * .28f)
                    : direction * cellSize * .08f;
                float particleSize = next == ToonParticleRecipe.Ember
                    ? Mathf.Lerp(cellSize * .18f, cellSize * .34f, (float)random.NextDouble())
                    : Mathf.Lerp(cellSize * .08f, cellSize * .18f, (float)random.NextDouble());
                Emit(position, velocity, particleSize, next == ToonParticleRecipe.Ember ? .44f : .36f,
                    WithAlpha(tint, Mathf.Lerp(.65f, 1f, (float)random.NextDouble())), (float)random.NextDouble() * 360f);
            }
        }

        private void Emit(Vector2 position, Vector2 velocity, float size, float lifetime, Color tint, float rotation)
        {
            var emission = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity,
                startSize = size,
                startLifetime = lifetime,
                startColor = tint,
                rotation = rotation * Mathf.Deg2Rad
            };
            simulation.Emit(emission, 1);
        }

        private void ResolveUvs(ParticleSystem.Particle particle, out Vector2 uvMin, out Vector2 uvMax)
        {
            if (tilesX == 1 && tilesY == 1)
            {
                uvMin = Vector2.zero;
                uvMax = Vector2.one;
                return;
            }
            float progress = 1f - particle.remainingLifetime / Mathf.Max(.001f, particle.startLifetime);
            int frame = Mathf.Clamp(Mathf.FloorToInt(progress * tilesX * tilesY), 0, tilesX * tilesY - 1);
            int x = frame % tilesX;
            int y = tilesY - 1 - frame / tilesX;
            uvMin = new Vector2((float)x / tilesX, (float)y / tilesY);
            uvMax = new Vector2((float)(x + 1) / tilesX, (float)(y + 1) / tilesY);
        }

        private static AnimationCurve SizeCurve(ToonParticleRecipe value)
        {
            switch (value)
            {
                case ToonParticleRecipe.Impact:
                    return new AnimationCurve(new Keyframe(0f, .72f), new Keyframe(.25f, 1.05f), new Keyframe(1f, 1.18f));
                case ToonParticleRecipe.Ripple:
                    return AnimationCurve.EaseInOut(0f, .55f, 1f, 1.55f);
                case ToonParticleRecipe.Ember:
                    return AnimationCurve.EaseInOut(0f, .72f, 1f, .18f);
                default:
                    return AnimationCurve.EaseInOut(0f, 1f, 1f, .2f);
            }
        }

        private static Gradient FadeGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .08f), new GradientAlphaKey(.92f, .68f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        private static Color TintFor(string effect)
        {
            if (effect != null && (effect.StartsWith("fire", StringComparison.Ordinal) || effect == "burning"))
                return new Color(1f, .34f, .07f, .92f);
            if (effect != null && (effect.Contains("shield") || effect.Contains("mana")))
                return new Color(.18f, .78f, 1f, .88f);
            if (effect != null && (effect.Contains("restore") || effect == "cleanse"))
                return new Color(.34f, 1f, .72f, .88f);
            if (effect != null && (effect.Contains("hit") || effect.Contains("damage") || effect.Contains("break")))
                return new Color(1f, .45f, .18f, .9f);
            return new Color(.46f, .86f, 1f, .82f);
        }

        private static Color WithAlpha(Color value, float alpha)
        {
            value.a *= alpha;
            return value;
        }

        private static uint StableSeed(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                return hash == 0 ? 1u : hash;
            }
        }
    }
}
