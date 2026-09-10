using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    /// <summary>One bit per source texel. This is input data, not a replacement art texture.</summary>
    public sealed class CombatTextureAlphaMask
    {
        private readonly byte[] bits;
        public int Width { get; }
        public int Height { get; }

        public CombatTextureAlphaMask(int width, int height, Color32[] pixels)
        {
            if (width <= 0 || height <= 0 || pixels == null || pixels.Length != width * height)
                throw new ArgumentException("Alpha mask dimensions must match the source pixels.");
            Width = width; Height = height;
            bits = new byte[(pixels.Length + 7) / 8];
            for (int i = 0; i < pixels.Length; i++)
                if (pixels[i].a >= 128) bits[i >> 3] |= (byte)(1 << (i & 7));
        }

        public bool Sample(float u, float v)
        {
            if (float.IsNaN(u) || float.IsNaN(v) || float.IsInfinity(u) || float.IsInfinity(v)) return false;
            int x = Mathf.Clamp(Mathf.FloorToInt(u * Width), 0, Width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(v * Height), 0, Height - 1);
            int index = y * Width + x;
            return (bits[index >> 3] & (1 << (index & 7))) != 0;
        }
    }

    /// <summary>Captured at texture binding, never while processing a pointer event.</summary>
    public sealed class CombatTextureAlphaMaskCache
    {
        private readonly Dictionary<Texture2D, CombatTextureAlphaMask> masks = new Dictionary<Texture2D, CombatTextureAlphaMask>();
        public int CaptureCount { get; private set; }
        public int FailedCaptureCount { get; private set; }
        public double CaptureMilliseconds { get; private set; }

        public CombatTextureAlphaMask Get(Texture2D source)
        {
            if (source == null) return null;
            if (masks.TryGetValue(source, out CombatTextureAlphaMask mask)) return mask;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            try { mask = Capture(source); CaptureCount++; }
            catch (Exception error)
            {
                FailedCaptureCount++;
                Debug.LogWarning("单位像素命中掩码不可用，保留格位操作：" + source.name + " / " + error.Message);
            }
            finally { timer.Stop(); CaptureMilliseconds += timer.Elapsed.TotalMilliseconds; }
            masks[source] = mask;
            return mask;
        }

        private static CombatTextureAlphaMask Capture(Texture2D source)
        {
            if (source.isReadable) return new CombatTextureAlphaMask(source.width, source.height, source.GetPixels32());
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = null;
            Texture2D readable = null;
            try
            {
                temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                readable.Apply(false, false);
                return new CombatTextureAlphaMask(source.width, source.height, readable.GetPixels32());
            }
            finally
            {
                RenderTexture.active = previous;
                if (temporary != null) RenderTexture.ReleaseTemporary(temporary);
                if (readable != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(readable);
                    else UnityEngine.Object.DestroyImmediate(readable);
                }
            }
        }
    }
}
