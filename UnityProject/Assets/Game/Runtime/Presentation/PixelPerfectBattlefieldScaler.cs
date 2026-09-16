using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>
    /// 战场画布的整数倍缩放器：保证"每个像素一样大 + 像素间严丝合缝"。
    ///
    /// 定案口径（2026-09-16 风格定案 v1 / occ-art-contract-v1.battlefield_display_policy）：
    /// 原生缓冲区 320×180，1920×1080 下整数放大 6 倍（1 原生像素 = 6 屏幕像素，
    /// 与主要参考《赛菲莉娅》同级）；画面不做小数倍缩放，分辨率非整数倍时按整数倍显示并留边。
    ///
    /// 本组件只改缩放模式，不碰布局：画布仍按 1920×1080 逻辑单位排布，
    /// 战场内容以 6 个画布单位 = 1 原生像素绘制（NativePixelUnits）。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class PixelPerfectBattlefieldScaler : MonoBehaviour
    {
        /// <summary>战场参考分辨率。1920/320 = 1080/180 = 6，整数对齐。</summary>
        public const int ReferenceWidth = 1920;
        public const int ReferenceHeight = 1080;

        /// <summary>1920×1080 下 1 个原生像素对应的画布单位数（6 倍整数放大）。</summary>
        public const int NativePixelUnitsAtReference = 6;

        /// <summary>玩法格的原生像素尺寸；一格 = NativePixelUnits × 本值 个画布单位。</summary>
        public const int GameplayCellNativePixels = 32;

        /// <summary>地面美术子网格的原生像素尺寸（每玩法格 2×2 张）。</summary>
        public const int GroundSubgridNativePixels = 16;

        /// <summary>
        /// 定案的默认玩法格画布尺寸：32 原生像素 × 6 = 192。
        /// 战场表现层的 cellSize 应取本常量——`CombatObjectLayerLayout` 用 `cellSize / 32f`
        /// 推导物件缩放，只有取 32 的整数倍时 1 个源像素才恰好是整数个画布单位。
        /// </summary>
        public const float GameplayCellCanvasUnitsAtReference = GameplayCellNativePixels * NativePixelUnitsAtReference;

        [SerializeField, Tooltip("是否在每帧分辨率变化时重算整数倍率。")]
        private bool _watchResolution = true;

        private CanvasScaler _scaler;
        private Canvas _canvas;
        private int _lastWidth;
        private int _lastHeight;

        /// <summary>当前画布缩放系数。≥1 且为整数时才是像素精确档。</summary>
        public float ScaleFactor => _scaler != null ? _scaler.scaleFactor : 1f;

        /// <summary>是否处于像素精确档（整数倍且 ≥1）。</summary>
        public bool IsPixelExact => ScaleFactor >= 1f && Mathf.Abs(ScaleFactor - Mathf.Round(ScaleFactor)) < .0001f;

        /// <summary>当前整数倍率（仅在像素精确档下有意义）。</summary>
        public int IntegerScale => Mathf.Max(1, Mathf.RoundToInt(ScaleFactor));

        /// <summary>当前 1 个原生像素对应的画布单位数。所有战场内容按它换算尺寸与位置。</summary>
        public float NativePixelUnits => NativePixelUnitsAtReference * ScaleFactor;

        /// <summary>当前玩法格对应的画布单位数。</summary>
        public float GameplayCellUnits => NativePixelUnits * GameplayCellNativePixels;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            _canvas = GetComponent<Canvas>();
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (!_watchResolution)
            {
                return;
            }

            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            {
                Apply();
            }
        }

        /// <summary>按屏幕尺寸取最大可用整数倍率；不能整除时留边，不做小数缩放。</summary>
        public void Apply()
        {
            if (_scaler == null)
            {
                _scaler = GetComponent<CanvasScaler>();
            }

            if (_scaler == null)
            {
                return;
            }

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            // 屏幕装得下参考分辨率时只允许整数倍（像素精确）；
            // 装不下（小屏）时退回比例缩放，宁可略糊也不裁切内容。
            float exact = Mathf.Min(Screen.width / (float)ReferenceWidth, Screen.height / (float)ReferenceHeight);
            float factor = exact >= 1f ? Mathf.Floor(exact) : exact;

            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _scaler.scaleFactor = Mathf.Max(.1f, factor);

            if (_canvas != null)
            {
                _canvas.pixelPerfect = true;
            }
        }

        /// <summary>把任意画布单位坐标吸附到原生像素栅格上（R 系列摆放规则的前置条件）。</summary>
        public float Snap(float canvasUnits)
        {
            float unit = NativePixelUnits;
            return unit <= 0f ? canvasUnits : Mathf.Round(canvasUnits / unit) * unit;
        }

        /// <summary>把画布单位坐标吸附到原生像素栅格上。</summary>
        public Vector2 Snap(Vector2 canvasUnits)
        {
            return new Vector2(Snap(canvasUnits.x), Snap(canvasUnits.y));
        }

        /// <summary>按原生像素尺寸换算画布单位尺寸（例：32×64 画布 → 192×384）。</summary>
        public Vector2 SizeForNativePixels(float nativeWidth, float nativeHeight)
        {
            float unit = NativePixelUnits;
            return new Vector2(nativeWidth * unit, nativeHeight * unit);
        }

        /// <summary>为战场画布挂上并立即应用整数倍缩放。</summary>
        public static PixelPerfectBattlefieldScaler Attach(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            if (canvas.GetComponent<CanvasScaler>() == null)
            {
                canvas.gameObject.AddComponent<CanvasScaler>();
            }

            PixelPerfectBattlefieldScaler scaler = canvas.GetComponent<PixelPerfectBattlefieldScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<PixelPerfectBattlefieldScaler>();
            }

            scaler.Apply();
            return scaler;
        }
    }
}
