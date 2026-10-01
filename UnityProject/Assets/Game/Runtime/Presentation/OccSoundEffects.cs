using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    internal static class OccSoundEffects
    {
        private const string Root = "Audio/SFX/Processed/";
        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        private static AudioSource source;
        private static readonly Dictionary<string, float> LastPlayed = new Dictionary<string, float>();
        private static int pageVariant;
        private static int magicVariant;
        private static int strikeVariant;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Clips.Clear();
            LastPlayed.Clear();
            source = null;
            pageVariant = magicVariant = strikeVariant = 0;
        }

        private static void Play(string path, float volume)
            => PlayResource(Root + path, volume);

        private static void PlayResource(string path, float volume)
        {
            if (!Application.isPlaying || AudioListener.volume <= 0f) return;
            if (LastPlayed.TryGetValue(path, out float previous) && Time.unscaledTime - previous < .075f) return;
            if (!Clips.TryGetValue(path, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>(path);
                Clips[path] = clip;
            }
            if (clip == null) return;
            LastPlayed[path] = Time.unscaledTime;
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
        internal static void PageFlip() => Free("book_0" + (1 + pageVariant++ % 4), .48f);
        internal static void PaperPlace() => Play("OpenGameArt/paper/paper_01", .60f);
        internal static void NotePlace() => Play("Kenney/rpg-audio/bookPlace1", .42f);
        internal static void ScrollAction() => Play("Kenney/rpg-audio/bookFlip2", .64f);
        internal static void Button() => Play("Kenney/interface-sounds/click_001", .32f);
        internal static void Rejected() => Play("Kenney/rpg-audio/bookClose", .28f);
        internal static void Footstep() => Play("Kenney/impact-sounds/footstep_concrete_000", .75f);
        internal static void Strike() => Free("blade_0" + (1 + strikeVariant++ % 3), .60f);
        internal static void MetalImpact() => Play("Kenney/impact-sounds/impactMetal_medium_000", .8f);
        internal static void ShieldBlock() => Free("metal_02", .56f);
        internal static void BreakObject() => Free("wood_01", .65f);
        internal static void Device() => Free("metal_01", .65f);
        internal static void Magic() => Free("magical_" + (1 + magicVariant++ % 7), .62f);
        internal static void Arcane() => Free("spell_0" + (1 + magicVariant++ % 2), .70f);
        internal static void Fire() => Free("spell_fire_01", .70f);
        internal static void Teleport() => Play("OpenGameArt/magic/teleport", .8f);
        private static void Free(string name, float volume) => PlayResource("Audio/OCC/SFX/FreeProcessed/" + name, volume);
        private static void Cue(string name, float volume) => PlayResource("Audio/OCC/SFX/" + name, volume);
        internal static void Ready() => Cue("aether_ready", .48f);
        internal static void Reward() { Free("item_gem_01", .32f); Cue("archive_reward", .50f); }
        internal static void Complete() => Cue("practicum_complete", .55f);
        internal static void Retry() => Cue("practicum_retry", .50f);

        internal static void Skill(SkillDefinition skill)
        {
            if (skill == null) return;
            if (skill.DamageType == DamageType.Fire) Fire();
            else if (skill.DamageType == DamageType.Arcane) Arcane();
            else Magic();
        }

        // Called only when a captured result is dispatched, alongside its visual impact.
        internal static void CombatFeedback(CombatFeedbackEvent feedback)
        {
            string clip = CombatFeedbackClip(feedback.Kind);
            if (clip != null) Free(clip, feedback.Kind == CombatFeedbackKind.UnitDefeated ? .48f : .52f);
        }

        internal static string CombatFeedbackClip(CombatFeedbackKind kind)
        {
            switch (kind)
            {
                case CombatFeedbackKind.Damage: return "impact_hit";
                case CombatFeedbackKind.ShieldAbsorb: return "metal_02";
                case CombatFeedbackKind.ArmorBreak:
                case CombatFeedbackKind.BreakStance: return "metal_03";
                case CombatFeedbackKind.Burning: return "spell_fire_02";
                case CombatFeedbackKind.Bound:
                case CombatFeedbackKind.Slow: return "chain_01";
                case CombatFeedbackKind.Healing: return "magical_4";
                case CombatFeedbackKind.ShieldRestore:
                case CombatFeedbackKind.ShieldTransferredIn: return "magical_5";
                case CombatFeedbackKind.ManaRestore: return "magical_6";
                case CombatFeedbackKind.StatusCleared: return "magical_7";
                case CombatFeedbackKind.DestructibleDamaged: return "stones_01";
                case CombatFeedbackKind.DestructibleDestroyed:
                case CombatFeedbackKind.UnitDefeated: return "wood_01";
                default: return null;
            }
        }

        internal static void UiButton(string title)
        {
            title = title ?? string.Empty;
            if (title.Contains("出发") || title.Contains("开始新游戏")) ScrollAction();
            else if (title.Contains("返回") || title.Contains("关闭") || title.Contains("先不")) PageClose();
            else if (title.Contains("上一") || title.Contains("下一")) PageFlip();
            else if (title.Contains("百科") || title.Contains("档案") || title.Contains("背包") || title.Contains("设置") || title.Contains("整备")) PageOpen();
            else if (title.Contains("购买") || title.Contains("商店")) Free("item_coins_01", .45f);
            else if (title.Contains("锁定") || title.Contains("解锁")) Free("lock_01", .45f);
            else if (title.Contains("确认") || title.Contains("选择") || title.Contains("领取") || title.Contains("奖励") || title.Contains("装入")) NotePlace();
            else Button();
        }
    }
}
