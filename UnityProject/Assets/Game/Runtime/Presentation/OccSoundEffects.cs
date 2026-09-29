using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    internal static class OccSoundEffects
    {
        private const string Root = "Audio/SFX/Processed/";
        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        private static AudioSource source;

        private static void Play(string path, float volume)
        {
            if (!Application.isPlaying || AudioListener.volume <= 0f) return;
            if (!Clips.TryGetValue(path, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>(Root + path);
                Clips[path] = clip;
            }
            if (clip == null) return;
            if (source == null)
            {
                var objectWithSource = new GameObject("OCC 音效播放器");
                Object.DontDestroyOnLoad(objectWithSource);
                source = objectWithSource.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
            }
            source.PlayOneShot(clip, volume);
        }

        internal static void PageOpen()
        {
            Play("Kenney/rpg-audio/bookOpen", .60f);
        }
        internal static void PageClose() => Play("Kenney/rpg-audio/bookClose", .53f);
        internal static void PageFlip() => Play("Kenney/rpg-audio/bookFlip2", .64f);
        internal static void PaperPlace() => Play("OpenGameArt/paper/paper_01", .60f);
        internal static void NotePlace() => Play("Kenney/rpg-audio/bookPlace1", .60f);
        internal static void ScrollAction() => Play("Kenney/rpg-audio/bookFlip2", .64f);
        internal static void Button() => Play("Kenney/interface-sounds/click_001", .50f);
        internal static void Rejected() => Play("Kenney/interface-sounds/error_001", .53f);
        internal static void Footstep() => Play("Kenney/impact-sounds/footstep_concrete_000", .75f);
        internal static void Strike() => Play("Kenney/rpg-audio/knifeSlice", .85f);
        internal static void MetalImpact() => Play("Kenney/impact-sounds/impactMetal_medium_000", .8f);
        internal static void ShieldBlock() => Play("OpenGameArt/combat/impact.1", .8f);
        internal static void BreakObject() => Play("Kenney/impact-sounds/impactWood_heavy_000", .8f);
        internal static void Device() => Play("Kenney/rpg-audio/metalClick", .9f);
        internal static void Magic() => Play("OpenGameArt/magic/magic1", .85f);
        internal static void Arcane() => Play("OpenGameArt/magic/magic2", .85f);
        internal static void Fire() => Play("OpenGameArt/magic/fire", .85f);
        internal static void Teleport() => Play("OpenGameArt/magic/teleport", .8f);

        internal static void UiButton(string title)
        {
            title = title ?? string.Empty;
            if (title.Contains("出发") || title.Contains("开始新游戏")) ScrollAction();
            else if (title.Contains("返回") || title.Contains("关闭") || title.Contains("先不")) PageClose();
            else if (title.Contains("上一") || title.Contains("下一")) PageFlip();
            else if (title.Contains("百科") || title.Contains("档案") || title.Contains("背包") || title.Contains("设置") || title.Contains("整备")) PageOpen();
            else if (title.Contains("确认") || title.Contains("选择") || title.Contains("领取") || title.Contains("奖励") || title.Contains("装入")) NotePlace();
            else Button();
        }
    }
}
