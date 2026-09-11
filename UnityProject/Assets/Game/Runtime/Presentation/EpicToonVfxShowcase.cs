using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace OCC.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EpicToonVfxShowcase : MonoBehaviour
    {
        private const string TextureRoot = "Art/PrototypeToonParticles/EpicToonFxSource/";
        private readonly Dictionary<string, Material> runtimeMaterials = new Dictionary<string, Material>();
        private Camera targetCamera;
        private Vector3 cameraHome;
        private float shakeUntil;
        private float shakeStrength;
        private GUIStyle titleStyle;
        private GUIStyle hintStyle;

        public int LastSystemCount { get; private set; }
        public int LastLightningSystemCount { get; private set; }

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null) cameraHome = targetCamera.transform.localPosition;
            BuildAmbientMotes();
        }

        public void Configure(Camera camera)
        {
            targetCamera = camera;
            if (targetCamera != null) cameraHome = targetCamera.transform.localPosition;
        }

        private void Update()
        {
            if (targetCamera == null) return;
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            bool leftPressed = mouse.leftButton.wasPressedThisFrame;
            bool rightPressed = mouse.rightButton.wasPressedThisFrame;
            if (!leftPressed && !rightPressed) return;
            Vector2 screen = mouse.position.ReadValue();
            float depth = Mathf.Abs(targetCamera.transform.position.z);
            Vector3 world = targetCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            world.z = 0f;
            if (rightPressed) PlayLightningAt(world);
            else PlayAt(world);
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;
            if (Time.unscaledTime < shakeUntil)
            {
                float remaining = Mathf.Clamp01((shakeUntil - Time.unscaledTime) / .32f);
                Vector2 jitter = Random.insideUnitCircle * shakeStrength * remaining;
                targetCamera.transform.localPosition = cameraHome + new Vector3(jitter.x, jitter.y, 0f);
            }
            else targetCamera.transform.localPosition = cameraHome;
        }

        public GameObject PlayAt(Vector3 worldPosition)
        {
            GameObject root = new GameObject("Epic Toon 超新星");
            root.transform.position = worldPosition;

            AddAfterglow(root.transform);
            AddCloudHalo(root.transform);
            AddRune(root.transform, "外环符文", 4.4f, 95f, new Color(.18f, .8f, 1f, .85f), 4);
            AddRune(root.transform, "逆旋符文", 3.25f, -145f, new Color(1f, .16f, .86f, .78f), 5);
            AddShockwave(root.transform, "冲击环 A", .03f, 3.3f, new Color(.1f, .9f, 1f, .95f), 8);
            AddShockwave(root.transform, "冲击环 B", .13f, 4.25f, new Color(1f, .12f, .72f, .86f), 7);
            AddShockwave(root.transform, "冲击环 C", .24f, 5.15f, new Color(.48f, .32f, 1f, .72f), 6);
            AddCoreFlash(root.transform);
            AddRays(root.transform);
            AddExplosion(root.transform);
            AddLightning(root.transform);
            AddFlameCrown(root.transform);
            AddShards(root.transform);
            AddSparks(root.transform);

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            LastSystemCount = systems.Length;
            foreach (ParticleSystem system in systems) system.Play(true);
            if (Application.isPlaying) Destroy(root, 3.4f);
            shakeUntil = Time.unscaledTime + .32f;
            shakeStrength = .12f;
            return root;
        }

        public GameObject PlayLightningAt(Vector3 worldPosition)
        {
            GameObject root = new GameObject("Epic Toon 雷霆裁决");
            root.transform.position = worldPosition;

            AddStormFront(root.transform);
            AddLightningRune(root.transform, "雷纹外阵", 4.9f, 118f, new Color(.08f, .72f, 1f, .76f), 2);
            AddLightningRune(root.transform, "雷纹内阵", 3.45f, -172f, new Color(.52f, .18f, 1f, .88f), 3);
            AddConvergenceRing(root.transform);
            AddThunderColumn(root.transform);
            AddBoltBranches(root.transform);
            AddOrbitingArcs(root.transform);
            AddLightningCore(root.transform);
            AddLightningShockwave(root.transform, "雷击波纹 A", .24f, 3.1f, new Color(.2f, .9f, 1f, .96f), 12);
            AddLightningShockwave(root.transform, "雷击波纹 B", .31f, 4.65f, new Color(.55f, .2f, 1f, .82f), 10);
            AddGroundDischarge(root.transform);
            AddLightningSparks(root.transform);
            AddLightningDebris(root.transform);
            AddLightningAfterglow(root.transform);

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            LastLightningSystemCount = systems.Length;
            foreach (ParticleSystem system in systems) system.Play(true);
            if (Application.isPlaying) Destroy(root, 3.6f);
            shakeUntil = Time.unscaledTime + .48f;
            shakeStrength = .18f;
            return root;
        }

        private void AddStormFront(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "雷云压境", "cloud_magic", false,
                new ParticleSystem.MinMaxCurve(1.4f, 2f), new ParticleSystem.MinMaxCurve(.15f, .55f),
                new ParticleSystem.MinMaxCurve(2.8f, 4.6f),
                new ParticleSystem.MinMaxGradient(new Color(.02f, .12f, .34f, .48f), new Color(.28f, .05f, .48f, .4f)), 0);
            SetBurst(system, 0f, 12);
            SetCircleShape(system, .65f);
            SetSizeCurve(system, new AnimationCurve(new Keyframe(0f, .55f), new Keyframe(.25f, 1f), new Keyframe(1f, 1.45f)));
            SetFade(system, .08f, .58f);
            var noise = system.noise;
            noise.enabled = true;
            noise.strength = .42f;
            noise.frequency = .52f;
        }

        private void AddLightningRune(Transform parent, string name, float size, float rotationSpeed, Color color, int order)
        {
            ParticleSystem system = CreateSystem(parent, name, "magic_runecircle", true, 1.15f, 0f, size, color, order);
            SetBurst(system, 0f, 1);
            SetSizeCurve(system, new AnimationCurve(
                new Keyframe(0f, .12f), new Keyframe(.14f, 1f), new Keyframe(.58f, .92f), new Keyframe(1f, .15f)));
            SetFade(system, .04f, .74f);
            var rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = rotationSpeed * Mathf.Deg2Rad;
        }

        private void AddConvergenceRing(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "聚雷瞄准环", "ring", true, .62f, 0f, 5.7f,
                new Color(.42f, .84f, 1f, .92f), 6);
            SetBurst(system, .02f, 2);
            SetSizeCurve(system, new AnimationCurve(
                new Keyframe(0f, 1.35f), new Keyframe(.52f, .7f), new Keyframe(.88f, .16f), new Keyframe(1f, 0f)));
            SetFade(system, .02f, .72f);
        }

        private void AddThunderColumn(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "天穹主雷柱", "lightray1", true, .38f, 0f, 1f,
                new ParticleSystem.MinMaxGradient(Color.white, new Color(.1f, .78f, 1f, 1f)), 20);
            system.transform.localPosition = new Vector3(0f, 3.15f, 0f);
            SetBurst(system, .22f, 3);
            var main = system.main;
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(.42f, .8f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(7.4f, 9.2f);
            main.startSizeZ = 1f;
            main.startRotation = new ParticleSystem.MinMaxCurve(-.035f, .035f);
            SetSizeCurve(system, new AnimationCurve(
                new Keyframe(0f, .05f), new Keyframe(.08f, 1f), new Keyframe(.42f, .68f), new Keyframe(1f, 0f)));
            SetFade(system, .01f, .68f);
        }

        private void AddBoltBranches(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "分叉雷脉", "lightning_v2_3x3", true,
                new ParticleSystem.MinMaxCurve(.34f, .58f), new ParticleSystem.MinMaxCurve(.15f, .75f),
                new ParticleSystem.MinMaxCurve(2.8f, 5.2f),
                new ParticleSystem.MinMaxGradient(new Color(.18f, .86f, 1f, 1f), new Color(.62f, .18f, 1f, .96f)), 18);
            system.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            SetBurst(system, .2f, 8);
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.35f, 2.7f, .1f);
            var main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(1.22f, 1.92f);
            SetSizeCurve(system, new AnimationCurve(new Keyframe(0f, .18f), new Keyframe(.12f, 1f), new Keyframe(1f, .58f)));
            SetFade(system, .01f, .76f);
            SetThreeByThreeAnimation(system);
        }

        private void AddOrbitingArcs(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "环绕电弧", "lightning_v2_3x3", true,
                new ParticleSystem.MinMaxCurve(.42f, .72f), new ParticleSystem.MinMaxCurve(.25f, .85f),
                new ParticleSystem.MinMaxCurve(1.1f, 2.1f),
                new ParticleSystem.MinMaxGradient(new Color(.06f, .62f, 1f, .94f), new Color(.72f, .24f, 1f, .9f)), 14);
            SetBurst(system, .12f, 15);
            SetCircleShape(system, 2.05f);
            var main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            SetSizeCurve(system, new AnimationCurve(new Keyframe(0f, .12f), new Keyframe(.16f, 1f), new Keyframe(1f, .35f)));
            SetFade(system, .02f, .72f);
            SetThreeByThreeAnimation(system);
        }

        private void AddLightningCore(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "雷核过曝", "glow1", true, .42f, 0f, 1.6f,
                new ParticleSystem.MinMaxGradient(Color.white, new Color(.24f, .76f, 1f, 1f)), 24);
            SetBurst(system, .235f, 4);
            SetSizeCurve(system, new AnimationCurve(
                new Keyframe(0f, .08f), new Keyframe(.08f, 3.6f), new Keyframe(.34f, 2.35f), new Keyframe(1f, 0f)));
            SetFade(system, .01f, .56f);
        }

        private void AddLightningShockwave(Transform parent, string name, float delay, float size, Color color, int order)
        {
            ParticleSystem system = CreateSystem(parent, name, "ring_shockwave", true, .68f, 0f, size, color, order);
            SetBurst(system, delay, 1);
            SetSizeCurve(system, new AnimationCurve(
                new Keyframe(0f, .06f), new Keyframe(.16f, .5f), new Keyframe(1f, 2.2f)));
            SetFade(system, .015f, .48f);
        }

        private void AddGroundDischarge(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "贴地放电", "lightray1", true, .48f, 0f, 1f,
                new ParticleSystem.MinMaxGradient(new Color(.08f, .82f, 1f, .96f), new Color(.5f, .22f, 1f, .9f)), 16);
            SetBurst(system, .255f, 18);
            var main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(2.2f, 5.8f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(.1f, .24f);
            main.startSizeZ = 1f;
            SetSizeCurve(system, new AnimationCurve(new Keyframe(0f, .08f), new Keyframe(.14f, 1f), new Keyframe(1f, 0f)));
            SetFade(system, .01f, .66f);
        }

        private void AddLightningSparks(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "雷火星群", "sparkle", true,
                new ParticleSystem.MinMaxCurve(.55f, 1.05f), new ParticleSystem.MinMaxCurve(3.8f, 9.6f),
                new ParticleSystem.MinMaxCurve(.1f, .38f),
                new ParticleSystem.MinMaxGradient(new Color(.45f, .95f, 1f, 1f), new Color(.68f, .32f, 1f, 1f)), 22);
            SetBurst(system, .245f, 72);
            SetCircleShape(system, .12f);
            SetSizeCurve(system, new AnimationCurve(new Keyframe(0f, .16f), new Keyframe(.1f, 1f), new Keyframe(1f, .04f)));
            SetFade(system, .01f, .75f);
            var noise = system.noise;
            noise.enabled = true;
            noise.strength = .52f;
            noise.frequency = 1.8f;
        }

        private void AddLightningDebris(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "电离晶片", "triangle_curve", false,
                new ParticleSystem.MinMaxCurve(.7f, 1.18f), new ParticleSystem.MinMaxCurve(3.1f, 6.8f),
                new ParticleSystem.MinMaxCurve(.18f, .46f),
                new ParticleSystem.MinMaxGradient(new Color(.18f, .72f, 1f, .95f), new Color(.58f, .2f, 1f, .9f)), 11);
            SetBurst(system, .27f, 28);
            SetCircleShape(system, .16f);
            var main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            SetSizeCurve(system, AnimationCurve.EaseInOut(0f, .18f, 1f, 1.25f));
            SetFade(system, .02f, .68f);
            var rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-7f, 7f);
        }

        private void AddLightningAfterglow(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "雷暴余辉", "glow1", true, 1.35f, 0f, 2.5f,
                new ParticleSystem.MinMaxGradient(new Color(.06f, .48f, 1f, .52f), new Color(.48f, .08f, 1f, .42f)), 1);
            SetBurst(system, .24f, 3);
            SetSizeCurve(system, new AnimationCurve(new Keyframe(0f, .35f), new Keyframe(.18f, 1f), new Keyframe(1f, 2.4f)));
            SetFade(system, .04f, .38f);
        }

        private void AddCoreFlash(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "白热核心", "glow1", true, .34f, 0f, 1.4f,
                new ParticleSystem.MinMaxGradient(Color.white, new Color(.15f, .9f, 1f, 1f)), 20);
            SetBurst(system, 0f, 3);
            SetSizeCurve(system, new AnimationCurve(
                new Keyframe(0f, .2f), new Keyframe(.12f, 3.8f), new Keyframe(.45f, 2.2f), new Keyframe(1f, 0f)));
            SetFade(system, .02f, .6f);
        }

        private void AddAfterglow(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "双色余辉", "glow1", true, 1.65f, 0f, 2.2f,
                new ParticleSystem.MinMaxGradient(new Color(.05f, .65f, 1f, .64f), new Color(1f, .05f, .62f, .58f)), 0);
            SetBurst(system, .18f, 4);
            SetSizeCurve(system, AnimationCurve.EaseInOut(0f, .6f, 1f, 2.7f));
            SetFade(system, .05f, .42f);
        }

        private void AddRune(Transform parent, string name, float size, float rotationSpeed, Color color, int order)
        {
            ParticleSystem system = CreateSystem(parent, name, "magic_runecircle", true, 1.45f, 0f, size,
                color, order);
            SetBurst(system, .02f, 1);
            SetSizeCurve(system, new AnimationCurve(
                new Keyframe(0f, .05f), new Keyframe(.12f, 1f), new Keyframe(.72f, 1.08f), new Keyframe(1f, 1.45f)));
            SetFade(system, .08f, .68f);
            var rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = rotationSpeed * Mathf.Deg2Rad;
        }

        private void AddShockwave(Transform parent, string name, float delay, float size, Color color, int order)
        {
            ParticleSystem system = CreateSystem(parent, name, "ring_shockwave", true, .72f, 0f, size,
                color, order);
            SetBurst(system, delay, 1);
            SetSizeCurve(system, new AnimationCurve(
                new Keyframe(0f, .08f), new Keyframe(.22f, .82f), new Keyframe(1f, 2.15f)));
            SetFade(system, .03f, .5f);
        }

        private void AddExplosion(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "九帧锐爆", "explosion_sharp_soft_3x3", false,
                .62f, 0f, new ParticleSystem.MinMaxCurve(4.6f, 6.2f),
                new ParticleSystem.MinMaxGradient(new Color(1f, .16f, .03f, 1f), new Color(1f, .82f, .08f, 1f)), 14);
            SetBurst(system, .06f, 4);
            SetSizeCurve(system, new AnimationCurve(new Keyframe(0f, .45f), new Keyframe(.35f, 1f), new Keyframe(1f, 1.28f)));
            SetFade(system, .01f, .86f);
            var textureSheet = system.textureSheetAnimation;
            textureSheet.enabled = true;
            textureSheet.mode = ParticleSystemAnimationMode.Grid;
            textureSheet.numTilesX = 3;
            textureSheet.numTilesY = 3;
            textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
            textureSheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            textureSheet.cycleCount = 1;
            var main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        }

        private void AddRays(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "放射光刃", "lightray1", true, .55f, 0f, 1f,
                new ParticleSystem.MinMaxGradient(new Color(.05f, .9f, 1f, .96f), new Color(1f, .1f, .74f, .9f)), 11);
            SetBurst(system, .015f, 22);
            var main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(3.8f, 7.4f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(.18f, .42f);
            main.startSizeZ = 1f;
            SetSizeCurve(system, new AnimationCurve(
                new Keyframe(0f, .08f), new Keyframe(.18f, 1f), new Keyframe(.72f, .74f), new Keyframe(1f, 0f)));
            SetFade(system, .02f, .72f);
        }

        private void AddLightning(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "闪电裂片", "lightning_v2_3x3", true,
                new ParticleSystem.MinMaxCurve(.42f, .68f), new ParticleSystem.MinMaxCurve(.4f, 1.2f),
                new ParticleSystem.MinMaxCurve(2.2f, 4.1f),
                new ParticleSystem.MinMaxGradient(new Color(.18f, .7f, 1f, 1f), new Color(.9f, .22f, 1f, 1f)), 13);
            SetBurst(system, .1f, 9);
            SetCircleShape(system, .45f);
            var main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            SetSizeCurve(system, new AnimationCurve(new Keyframe(0f, .25f), new Keyframe(.16f, 1f), new Keyframe(1f, .7f)));
            SetFade(system, .01f, .8f);
            var textureSheet = system.textureSheetAnimation;
            textureSheet.enabled = true;
            textureSheet.mode = ParticleSystemAnimationMode.Grid;
            textureSheet.numTilesX = 3;
            textureSheet.numTilesY = 3;
            textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
            textureSheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
        }

        private void AddFlameCrown(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "火焰冠", "fire_soft_blank", true,
                new ParticleSystem.MinMaxCurve(.58f, 1.05f), new ParticleSystem.MinMaxCurve(2.1f, 4.8f),
                new ParticleSystem.MinMaxCurve(.45f, 1.05f),
                new ParticleSystem.MinMaxGradient(new Color(1f, .12f, .02f, .95f), new Color(1f, .72f, .05f, .95f)), 10);
            SetBurst(system, .08f, 28);
            SetCircleShape(system, .32f);
            var main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(-.5f, .5f);
            SetSizeCurve(system, new AnimationCurve(new Keyframe(0f, .35f), new Keyframe(.18f, 1f), new Keyframe(1f, .12f)));
            SetFade(system, .02f, .7f);
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(1.2f);
        }

        private void AddShards(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "弧形碎片", "triangle_curve", false,
                new ParticleSystem.MinMaxCurve(.7f, 1.15f), new ParticleSystem.MinMaxCurve(4.5f, 8.5f),
                new ParticleSystem.MinMaxCurve(.22f, .62f),
                new ParticleSystem.MinMaxGradient(new Color(.12f, .85f, 1f, 1f), new Color(1f, .18f, .75f, 1f)), 12);
            SetBurst(system, .055f, 34);
            SetCircleShape(system, .12f);
            var main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            SetSizeCurve(system, AnimationCurve.EaseInOut(0f, .2f, 1f, 1.4f));
            SetFade(system, .03f, .72f);
            var rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
        }

        private void AddSparks(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "星芒火花", "sparkle", true,
                new ParticleSystem.MinMaxCurve(.65f, 1.35f), new ParticleSystem.MinMaxCurve(2.5f, 8.8f),
                new ParticleSystem.MinMaxCurve(.13f, .52f),
                new ParticleSystem.MinMaxGradient(new Color(.35f, .92f, 1f, 1f), new Color(1f, .5f, .08f, 1f)), 15);
            SetBurst(system, .03f, 62);
            SetCircleShape(system, .18f);
            SetSizeCurve(system, new AnimationCurve(new Keyframe(0f, .15f), new Keyframe(.12f, 1f), new Keyframe(1f, .05f)));
            SetFade(system, .01f, .82f);
            var noise = system.noise;
            noise.enabled = true;
            noise.strength = .38f;
            noise.frequency = 1.1f;
            noise.scrollSpeed = .8f;
        }

        private void AddCloudHalo(Transform parent)
        {
            ParticleSystem system = CreateSystem(parent, "魔力云环", "cloud_magic", false,
                new ParticleSystem.MinMaxCurve(1.35f, 2.2f), new ParticleSystem.MinMaxCurve(.15f, .7f),
                new ParticleSystem.MinMaxCurve(1.2f, 2.7f),
                new ParticleSystem.MinMaxGradient(new Color(.08f, .42f, 1f, .42f), new Color(.7f, .08f, 1f, .38f)), 1);
            SetBurst(system, .12f, 14);
            SetCircleShape(system, .5f);
            var main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            SetSizeCurve(system, AnimationCurve.EaseInOut(0f, .35f, 1f, 1.8f));
            SetFade(system, .08f, .45f);
            var noise = system.noise;
            noise.enabled = true;
            noise.strength = .55f;
            noise.frequency = .45f;
        }

        private void BuildAmbientMotes()
        {
            if (!Application.isPlaying) return;
            ParticleSystem system = CreateSystem(transform, "背景星尘", "circle", true,
                new ParticleSystem.MinMaxCurve(5f, 9f), new ParticleSystem.MinMaxCurve(.08f, .35f),
                new ParticleSystem.MinMaxCurve(.025f, .085f),
                new ParticleSystem.MinMaxGradient(new Color(.1f, .7f, 1f, .28f), new Color(.8f, .12f, 1f, .22f)), -20);
            var main = system.main;
            main.loop = true;
            main.duration = 8f;
            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 6f;
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(18f, 10f, .1f);
            SetFade(system, .18f, .62f);
            var noise = system.noise;
            noise.enabled = true;
            noise.strength = .3f;
            noise.frequency = .2f;
            system.Play();
        }

        private ParticleSystem CreateSystem(Transform parent, string name, string texture, bool additive,
            float lifetime, float speed, float size, Color color, int sortingOrder)
        {
            return CreateSystem(parent, name, texture, additive, new ParticleSystem.MinMaxCurve(lifetime),
                new ParticleSystem.MinMaxCurve(speed), new ParticleSystem.MinMaxCurve(size),
                new ParticleSystem.MinMaxGradient(color), sortingOrder);
        }

        private ParticleSystem CreateSystem(Transform parent, string name, string texture, bool additive,
            ParticleSystem.MinMaxCurve lifetime, ParticleSystem.MinMaxCurve speed, ParticleSystem.MinMaxCurve size,
            ParticleSystem.MinMaxGradient color, int sortingOrder)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            ParticleSystem system = child.AddComponent<ParticleSystem>();
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 2.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 128;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = MaterialFor(texture, additive);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.YoungestInFront;
            renderer.sortingOrder = sortingOrder;
            return system;
        }

        private Material MaterialFor(string textureName, bool additive)
        {
            string key = textureName + (additive ? ":add" : ":alpha");
            if (runtimeMaterials.TryGetValue(key, out Material cached) && cached != null) return cached;
            Texture2D texture = Resources.Load<Texture2D>(TextureRoot + textureName);
            if (texture == null) throw new KeyNotFoundException("Missing Epic Toon texture: " + textureName);
            Shader shader = Shader.Find("OCC/Epic Toon Particle");
            if (shader == null) throw new MissingReferenceException("OCC/Epic Toon Particle shader was not imported.");
            Material material = new Material(shader) { name = "Runtime_ETFX_" + key, hideFlags = HideFlags.DontSave };
            material.SetTexture("_MainTex", texture);
            material.SetColor("_Color", Color.white);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.renderQueue = 3000;
            material.SetShaderPassEnabled("ShadowCaster", false);
            runtimeMaterials[key] = material;
            return material;
        }

        private static void SetBurst(ParticleSystem system, float time, int count)
        {
            var emission = system.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(time, (short)count) });
        }

        private static void SetCircleShape(ParticleSystem system, float radius)
        {
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
        }

        private static void SetThreeByThreeAnimation(ParticleSystem system)
        {
            var textureSheet = system.textureSheetAnimation;
            textureSheet.enabled = true;
            textureSheet.mode = ParticleSystemAnimationMode.Grid;
            textureSheet.numTilesX = 3;
            textureSheet.numTilesY = 3;
            textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
            textureSheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            textureSheet.cycleCount = 1;
        }

        private static void SetSizeCurve(ParticleSystem system, AnimationCurve curve)
        {
            var module = system.sizeOverLifetime;
            module.enabled = true;
            module.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static void SetFade(ParticleSystem system, float fadeIn, float hold)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, Mathf.Clamp01(fadeIn)),
                    new GradientAlphaKey(1f, Mathf.Clamp01(hold)),
                    new GradientAlphaKey(0f, 1f)
                });
            var module = system.colorOverLifetime;
            module.enabled = true;
            module.color = gradient;
        }

        private void OnGUI()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 30,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter
                };
                titleStyle.normal.textColor = new Color(.45f, .9f, 1f, .96f);
                hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    alignment = TextAnchor.UpperCenter
                };
                hintStyle.normal.textColor = new Color(.78f, .72f, 1f, .86f);
            }
            GUI.Label(new Rect(0f, 26f, Screen.width, 46f), "EPIC TOON　AETHER ARSENAL", titleStyle);
            GUI.Label(new Rect(0f, 70f, Screen.width, 34f), "左键：以太超新星　右键：雷霆裁决", hintStyle);
        }

        private void OnDestroy()
        {
            foreach (Material material in runtimeMaterials.Values)
            {
                if (material == null) continue;
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
            runtimeMaterials.Clear();
        }
    }
}
