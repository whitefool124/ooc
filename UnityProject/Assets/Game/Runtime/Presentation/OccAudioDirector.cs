using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Presentation only: follows the existing flow without changing game state.
    public sealed class OccAudioDirector : MonoBehaviour
    {
        private CombatPrototypeBootstrap host;
        private FirstExperiencePrototypeController front;
        private readonly AudioSource[] voices = new AudioSource[2];
        private readonly float[] gains = new float[2];
        private int active;
        private string requested;
        private CombatFlowPhase lastPhase;
        public string CurrentMusic => requested ?? string.Empty;

        internal static void Attach(CombatPrototypeBootstrap owner)
        {
            if (owner.GetComponent<OccAudioDirector>() != null) return;
            owner.gameObject.AddComponent<OccAudioDirector>();
        }

        private void Awake()
        {
            host = GetComponent<CombatPrototypeBootstrap>();
            for (int i = 0; i < voices.Length; i++)
            {
                // Dedicated children allow safe teardown without touching other audio.
                var child = new GameObject("OCC 配乐 " + i);
                child.transform.SetParent(transform, false);
                voices[i] = child.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].loop = true;
                voices[i].spatialBlend = 0;
                voices[i].volume = 0;
            }
        }

        private void OnEnable()
        {
            if (host != null) host.UiVisualEvents.Published += OnVisualEvent;
        }

        private void OnDisable()
        {
            if (host != null) host.UiVisualEvents.Published -= OnVisualEvent;
            foreach (var voice in voices) if (voice != null) voice.Stop();
            requested = null;
            gains[0] = gains[1] = 0;
        }

        private void OnDestroy()
        {
            foreach (var voice in voices) if (voice != null) Destroy(voice.gameObject);
        }

        public static string SelectMusic(bool video, bool archive, bool battle, bool boss, bool outcome, bool academy, bool map)
        {
            if (video) return string.Empty;
            if (archive || outcome) return "archive_piano";
            if (battle) return boss ? "boss_exam" : "jrpg_practicum";
            if (academy) return "magic_town";
            return map ? "academy_town" : "archive_piano";
        }

        private void Update()
        {
            if (host == null) return;
            if (front == null) front = GetComponent<FirstExperiencePrototypeController>();
            bool opening = front != null && front.enabled;
            var stage = opening ? front.CurrentStage : FirstExperiencePrototypeController.FlowStage.Map;
            bool academy = opening && (stage == FirstExperiencePrototypeController.FlowStage.AcademyIntro ||
                stage == FirstExperiencePrototypeController.FlowStage.Configuration);
            bool battle = !opening && host.IsBattlefieldVisible && !host.IsMapMenuOpen && !host.IsRogueliteMenuOpen;
            var run = host.CurrentMapRun;
            bool boss = battle && run != null && run.MapNode(run.CurrentNodeId)?.Type == RogueliteMapNodeType.Finale;
            SetMusic(SelectMusic(opening && stage == FirstExperiencePrototypeController.FlowStage.WorldOpening,
                host.IsEncyclopediaOpen, battle, boss, !host.IsMapMenuOpen && host.IsCombatOutcomeVisible, academy,
                !opening && run != null));
            var phase = host.CurrentFlowPhase;
            if (phase != lastPhase)
            {
                if (phase == CombatFlowPhase.Victory) OccSoundEffects.Complete();
                else if (phase == CombatFlowPhase.Defeat) OccSoundEffects.Retry();
                lastPhase = phase;
            }
            for (int i = 0; i < voices.Length; i++)
            {
                float target = i == active && !string.IsNullOrEmpty(requested) ? 1 : 0;
                gains[i] = Mathf.MoveTowards(gains[i], target, Time.unscaledDeltaTime / 1.5f);
                voices[i].volume = gains[i] * MusicGain(voices[i].clip == null ? "" : voices[i].clip.name);
                if (target == 0 && gains[i] == 0 && voices[i].isPlaying) voices[i].Stop();
            }
        }

        // Measured source RMS differs by ~14 dB. Keep menu/map at ~-30 dBFS,
        // combat at ~-27 dBFS before the player's existing master-volume control.
        private static float MusicGain(string track)
        {
            switch (track)
            {
                case "archive_piano": return .60f;
                case "academy_town": return .19f;
                case "magic_town": return .135f;
                case "jrpg_practicum": return .18f;
                case "boss_exam": return .24f;
                default: return 0;
            }
        }

        private void SetMusic(string track)
        {
            if (requested == track) return;
            requested = track;
            if (string.IsNullOrEmpty(track)) return;
            var clip = Resources.Load<AudioClip>("Audio/OCC/Music/" + track);
            if (clip == null) { Debug.LogWarning("OCC missing music: " + track); return; }
            active = 1 - active;
            voices[active].Stop();
            voices[active].clip = clip;
            voices[active].volume = gains[active] = 0;
            voices[active].Play();
        }

        private void OnVisualEvent(UiVisualEvent signal)
        {
            switch (signal.Kind)
            {
                case UiVisualEventKind.MapLocationChanged: OccSoundEffects.PageFlip(); break;
                case UiVisualEventKind.BriefingOpened: OccSoundEffects.Ready(); break;
                case UiVisualEventKind.RewardClaimed: OccSoundEffects.Reward(); break;
            }
        }
    }
}
