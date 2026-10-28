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
        private static int footVariant;
        private static float lastFootstep = -1;
        private static bool lastFootstepWater;
        private static float burstStarted=-1;
        private static int burstCount;
        public static event System.Action<string> Played;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Clips.Clear();
            LastPlayed.Clear();
            source = null;
            pageVariant = magicVariant = strikeVariant = 0;
            footVariant=0;lastFootstep=-1;lastFootstepWater=false;burstStarted=-1;burstCount=0;Played=null;
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
            float gain=volume;
            bool impact=path.Contains("/FreeProcessed/") && !path.Contains("/book_") || path.EndsWith("/object_hit") || path.EndsWith("/object_break") || path.EndsWith("/defeat");
            if(impact)
            {
                if(Time.unscaledTime-burstStarted>.10f){burstStarted=Time.unscaledTime;burstCount=0;}
                if(++burstCount>8)return;
                gain/=Mathf.Sqrt(Mathf.Max(1,burstCount));
            }
            source.PlayOneShot(clip,gain);
            Played?.Invoke(path);
        }

        internal static void Detail(string name,float gain=.20f)=>Foley(name,gain);

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
        internal static void Rejected() => Foley("reject", .30f);
        internal static void Footstep() => Play("Kenney/impact-sounds/footstep_concrete_000", .75f);
        internal static void Strike() => Free("blade_0" + (1 + strikeVariant++ % 3), .60f);
        internal static void MetalImpact() => Play("Kenney/impact-sounds/impactMetal_medium_000", .8f);
        internal static void ShieldBlock() => Free("metal_02", .56f);
        internal static void BreakObject() => Foley("object_break", .50f);
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
        private static void Foley(string name,float volume)=>PlayResource("Audio/OCC/SFX/Foley/"+name,volume);
        internal static string InventoryMaterial(string definitionId,string materialId)
        {
            if(materialId=="FORGE-CIRCUIT" || definitionId=="ACA-EQ-HD01")return "glass";
            if(materialId=="FORGE-LOAD" || definitionId=="ACA-EQ-CH03")return "metal";
            if(!string.IsNullOrEmpty(materialId) && materialId.StartsWith("SPEC-"))return "paper";
            if(string.IsNullOrEmpty(definitionId))return "neutral";
            if(definitionId.StartsWith("ACA-EQ-BP") || definitionId=="ACA-EQ-LG01")return "leather";
            if(definitionId.StartsWith("ACA-EQ-CH"))return "cloth";
            if(definitionId.StartsWith("ACA-EQ-"))return "metal";
            if(definitionId.StartsWith("G-"))return "device";
            return "neutral";
        }
        internal static string InventoryClip(string definitionId,string materialId,bool dropped,int weight=2)
        {
            string kind=InventoryMaterial(definitionId,materialId);
            if(kind=="neutral")return "neutral_drop";
            if(kind=="device")return "latch";
            if(kind=="metal")return !dropped?"metal_pick":weight>=3?"metal_heavy":"metal_light";
            return kind+(dropped?"_drop":"_pick");
        }
        internal static void Inventory(string definitionId,string materialId,bool dropped,int weight=2)
            => Foley(InventoryClip(definitionId,materialId,dropped,weight),dropped?.42f:.26f);
        internal static void GroundStep(bool water)
        {
            // Preserve the first wet contact after stone on short, fast routes.
            if(Time.unscaledTime-lastFootstep<.04f && (!water || lastFootstepWater))return;
            lastFootstep=Time.unscaledTime;lastFootstepWater=water;
            Foley((water?"water_":"stone_")+(footVariant++%(water?2:3)),water?.36f:.42f);
        }
        internal static string ServiceClip(string subject)
        {
            switch(subject)
            {
                case "purchase":return "coins";
                case "forge":return "forge";
                case "specialize":return "paper_drop";
                case "heal":return "heal";
                case "meal":return "meal";
                case "check":return "latch";
                default:return null;
            }
        }
        internal static void Meal(string id)=>Foley(id=="MEAL-AETHER" ? "meal_cup" : id=="MEAL-POWER" ? "meal_plate" : "meal_bowl",.32f);
        internal static void ServiceCommitted(string subject)
        {
            string clip=ServiceClip(subject);
            if(clip=="heal")Free("magical_4",.38f);
            else if(clip!=null)Foley(clip,subject=="forge"?.46f:.38f);
        }

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
            if(feedback.Kind==CombatFeedbackKind.UnitDefeated){Foley("defeat",.40f);return;}
            if(feedback.Kind==CombatFeedbackKind.DestructibleDamaged){Foley("object_hit",.42f);return;}
            if(feedback.Kind==CombatFeedbackKind.DestructibleDestroyed){Foley("object_break",.50f);return;}
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
            else if (title.Contains("购买") || title.Contains("商店")) Button();
            else if (title.Contains("锁定") || title.Contains("解锁")) Free("lock_01", .45f);
            else if (title.Contains("确认") || title.Contains("选择") || title.Contains("领取") || title.Contains("奖励") || title.Contains("装入")) NotePlace();
            else Button();
        }
    }
}
