using System;
using UnityEngine;
using UnityEngine.Video;

namespace OCC.Combat.Presentation
{
    public sealed class FirstExperiencePrototypeController : MonoBehaviour
    {
        public enum FlowStage
        {
            Branding, FirstSettings, WorldOpening, Landing, SaveSelection, SaveConfirmation,
            Configuration, AcademyIntro, Map,
            Battle1Preview, Battle1, Battle1Result, Reward1, Events12,
            Battle2Preview, Battle2, Battle2Result, Reward2, Event3Workshop,
            Battle3Preview, Battle3, Battle3Result, Reward3, Medical,
            ElitePreview, Elite, EliteResult, EliteReward, Shop, Complete, RunCreation
        }

        [Serializable]
        private sealed class SaveData
        {
            public int version = 2;
            public int slot = -1;
            public FlowStage stage;
            public int battleActions;
            public bool event1Done, event2Done, event3Done;
            public bool forged, specialized, battle3Completed;
            public bool healthChecked, healed, mealBought;
            public int coins = 8;
            public string reward1 = "", reward2 = "", reward3 = "";
            public bool shopPurchase, fixedExperienceComplete;
        }

        private const string LegacySaveKey = "OCC.FirstExperiencePrototype.v1";
        private const string SavePrefix = "OCC.FirstExperiencePrototype.v2.slot.";
        private const string SettingsSeenKey = "OCC.FirstExperiencePrototype.deviceSettings.v1";
        private const string OpeningSeenKey = "OCC.FirstExperiencePrototype.worldOpening.v1";
        private const string VolumeKey = "OCC.FirstExperiencePrototype.volume.v1";
        private const string ResolutionKey = "OCC.FirstExperiencePrototype.resolution.v1";
        private const string FullscreenKey = "OCC.FirstExperiencePrototype.fullscreen.v1";
        private const float DesignWidth = 1920f, DesignHeight = 1080f;
        private const float BrandDuration = 1f, FadeDuration = 0.2f;
        private static readonly Vector2Int[] Resolutions =
        {
            new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080)
        };
        private static readonly string[] RouteLabels =
        {
            "地图", "战1预览", "战斗1", "战果1", "奖励1", "事件1与2", "战2预览", "战斗2",
            "战果2", "奖励2", "事件3与工坊", "战3预览", "战斗3", "战果3", "奖励3", "医务室",
            "精英预览", "精英战", "精英战果", "精英奖励", "商店", "完成", "单轮创建"
        };

        private SaveData data;
        private VideoPlayer openingPlayer;
        private Texture2D[] brandMarks;
        private int brandIndex = -1;
        private float brandStartedAt;
        private bool openingStarted, videoPrepared;
        private bool selectingContinue, pendingOverwrite, settingsFromLanding, eliteArmed;
        private int selectedSlot = -1, pendingResolution;
        private float pendingVolume;
        private bool pendingFullscreen;
        private Texture2D panel, card, accent, done;
        private Texture2D buttonIdle, buttonHover, buttonPressed, cardFrame;
        private Texture2D landingBackdrop, startupBackdrop, settingsBackdrop, archiveBackdrop;
        private GUIStyle titleStyle, headingStyle, bodyStyle, smallStyle, buttonStyle, cardStyle;
        private CombatPrototypeBootstrap combatBootstrap;
        private string handoffError = string.Empty;
        private FormalFirstExperienceUi formalUi;

        public string DebugStage => data == null ? "Uninitialized" : data.stage.ToString();
        public int DebugBattleActions => data == null ? 0 : data.battleActions;
        public int DebugSelectedSlot => selectedSlot;
        public int DebugOpeningBrandMarkIndex => brandIndex;
        public bool HasSave => HasAnySaveRecord();
        public bool HasLegacySave => PlayerPrefs.HasKey(LegacySaveKey);
        public bool HasOpeningBrandMarks => brandMarks != null && brandMarks.Length == 2 && brandMarks[0] != null && brandMarks[1] != null;
        public FlowStage CurrentStage => data == null ? FlowStage.Branding : data.stage;
        public bool IsSelectingContinue => selectingContinue;
        public bool IsPendingOverwrite => pendingOverwrite;
        public bool IsSettingsFromLanding => settingsFromLanding;
        public int ResolutionIndex => pendingResolution;
        public int ResolutionCount => Resolutions.Length;
        public string ResolutionLabel => Resolutions[pendingResolution].x + " × " + Resolutions[pendingResolution].y;
        public float PendingVolume { get => pendingVolume; set { pendingVolume = Mathf.Clamp01(value); AudioListener.volume = pendingVolume; } }
        public bool PendingFullscreen { get => pendingFullscreen; set => pendingFullscreen = value; }
        public string HandoffError => handoffError;
        public float AnimationIntensity => combatBootstrap == null ? 1f : combatBootstrap.UiPreferences.AnimationIntensity;

        public void Bind(CombatPrototypeBootstrap bootstrap) => combatBootstrap = bootstrap;

        public bool IsEncyclopediaOpen => combatBootstrap != null && combatBootstrap.IsEncyclopediaOpen;
        public void OpenEncyclopedia() => combatBootstrap?.OpenEncyclopedia();

        public static bool IsRuntimeStage(FlowStage stage) => stage >= FlowStage.Map;

        private void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            data = new SaveData { stage = FlowStage.Branding };
            pendingVolume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            pendingResolution = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, 2), 0, Resolutions.Length - 1);
            pendingFullscreen = PlayerPrefs.GetInt(FullscreenKey, 0) != 0;
            AudioListener.volume = pendingVolume;
            BuildTextures();
            brandMarks = new[]
            {
                Resources.Load<Texture2D>("Art/PrototypeOpening/oc_aether_industry_mark"),
                Resources.Load<Texture2D>("Art/PrototypeOpening/occ_life_archive_mark")
            };
            PrepareVideo();
            PlayBranding();
            formalUi = GetComponent<FormalFirstExperienceUi>();
            if (formalUi == null) formalUi = gameObject.AddComponent<FormalFirstExperienceUi>();
            formalUi.Initialize(this);
        }

        private void Update()
        {
            if (data.stage == FlowStage.Branding && brandIndex >= 0 && Time.unscaledTime - brandStartedAt >= BrandDuration)
                AdvanceBrand();
        }

        private void OnDestroy()
        {
            if (panel != null) Destroy(panel);
            if (card != null) Destroy(card);
            if (accent != null) Destroy(accent);
            if (done != null) Destroy(done);
            if (buttonIdle != null) Destroy(buttonIdle);
            if (buttonHover != null) Destroy(buttonHover);
            if (buttonPressed != null) Destroy(buttonPressed);
            if (cardFrame != null) Destroy(cardFrame);
        }

        private void PrepareVideo()
        {
            openingPlayer = GetComponent<VideoPlayer>();
            if (openingPlayer == null) openingPlayer = gameObject.AddComponent<VideoPlayer>();
            openingPlayer.playOnAwake = false;
            openingPlayer.isLooping = false;
            openingPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            openingPlayer.targetCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            openingPlayer.aspectRatio = VideoAspectRatio.FitInside;
            openingPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            openingPlayer.source = VideoSource.VideoClip;
            openingPlayer.clip = Resources.Load<VideoClip>("Video/OccOpening45s_UnityWebm");
            openingPlayer.prepareCompleted += OnOpeningPrepared;
            openingPlayer.loopPointReached += _ => FinishWorldOpening();
            openingPlayer.errorReceived += (_, message) => Debug.LogWarning("Opening video unavailable: " + message);
            videoPrepared = openingPlayer.clip != null;
        }

        private void BuildTextures()
        {
            panel = Solid(FormalUiTheme.Ink);
            card = Solid(FormalUiTheme.SurfaceRaised);
            accent = Solid(FormalUiTheme.Interactive);
            done = Solid(Color.Lerp(FormalUiTheme.SurfaceRaised, FormalUiTheme.Safe, .18f));
            buttonIdle = Framed(FormalUiTheme.Interactive, FormalUiTheme.Rule, FormalUiTheme.Amber);
            buttonHover = Framed(Color.Lerp(FormalUiTheme.Interactive, FormalUiTheme.Cyan, .14f), FormalUiTheme.Cyan, FormalUiTheme.Cyan);
            buttonPressed = Framed(FormalUiTheme.InteractivePressed, FormalUiTheme.Ink, FormalUiTheme.Cyan);
            cardFrame = Framed(FormalUiTheme.SurfaceRaised, FormalUiTheme.Rule, FormalUiTheme.Amber);
            landingBackdrop = BackdropTexture("landing");
            startupBackdrop = BackdropTexture("startup");
            settingsBackdrop = BackdropTexture("settings");
            archiveBackdrop = BackdropTexture("archive");
        }

        private static Texture2D BackdropTexture(string id)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.BackdropPath(id));
            return sprite == null ? null : sprite.texture;
        }

        private static Texture2D Framed(Color fill, Color outer, Color inner)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool edge = x < 2 || x >= size - 2 || y < 2 || y >= size - 2;
                bool inset = x == 2 || x == size - 3 || y == 2 || y == size - 3;
                texture.SetPixel(x, y, edge ? outer : inset ? inner : fill);
            }
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            Font font = FormalUiKit.Font;
            titleStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 48, fontStyle = FontStyle.Normal, normal = { textColor = FormalUiTheme.Text } };
            headingStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 32, fontStyle = FontStyle.Normal, wordWrap = true, normal = { textColor = FormalUiTheme.Cyan } };
            bodyStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 24, wordWrap = true, richText = true, normal = { textColor = FormalUiTheme.Text } };
            smallStyle = new GUIStyle(bodyStyle) { fontSize = 20, normal = { textColor = FormalUiTheme.Muted } };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = font, fontSize = 24, fontStyle = FontStyle.Normal, wordWrap = true,
                border = new RectOffset(4, 4, 4, 4), padding = new RectOffset(20, 20, 12, 12),
                normal = { textColor = FormalUiTheme.Text, background = buttonIdle },
                hover = { textColor = FormalUiTheme.Text, background = buttonHover },
                active = { textColor = FormalUiTheme.Text, background = buttonPressed },
                focused = { textColor = FormalUiTheme.Text, background = buttonHover },
                onNormal = { textColor = FormalUiTheme.Text, background = buttonIdle },
                onHover = { textColor = FormalUiTheme.Text, background = buttonHover },
                onActive = { textColor = FormalUiTheme.Text, background = buttonPressed }
            };
            cardStyle = new GUIStyle(GUI.skin.box)
            {
                font = font, fontSize = 24, alignment = TextAnchor.MiddleLeft,
                border = new RectOffset(4, 4, 4, 4), padding = new RectOffset(20, 20, 12, 12),
                normal = { textColor = FormalUiTheme.Text, background = cardFrame }
            };
        }

        private void OnGUI()
        {
            if (formalUi != null) return;
            EnsureStyles();
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / DesignWidth, Screen.height / DesignHeight, 1f));
            switch (data.stage)
            {
                case FlowStage.Branding: DrawBranding(); break;
                case FlowStage.FirstSettings: DrawSettings(); break;
                case FlowStage.WorldOpening: DrawWorldOpening(); break;
                case FlowStage.Landing: DrawLanding(); break;
                case FlowStage.SaveSelection: DrawSaveSelection(); break;
                case FlowStage.SaveConfirmation: DrawSaveConfirmation(); break;
                case FlowStage.Configuration: DrawConfiguration(); break;
                case FlowStage.AcademyIntro: DrawAcademyIntro(); break;
                default: DrawRuntimeHandoff(); break;
            }
            GUI.matrix = old;
        }

        private void DrawBranding()
        {
            GUI.DrawTexture(new Rect(0, 0, DesignWidth, DesignHeight), panel);
            if (brandIndex >= 0 && brandMarks[brandIndex] != null)
            {
                float elapsed = Time.unscaledTime - brandStartedAt;
                float alpha = Mathf.Min(Mathf.Clamp01(elapsed / FadeDuration), Mathf.Clamp01((BrandDuration - elapsed) / FadeDuration));
                Color old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(new Rect(0, 0, DesignWidth, DesignHeight), brandMarks[brandIndex], ScaleMode.StretchToFill, false);
                GUI.color = old;
            }
            if (GUI.Button(new Rect(1570, 45, 280, 70), "跳过品牌", buttonStyle)) FinishBranding();
        }

        private void DrawSettings()
        {
            Background(settingsFromLanding ? "显示与声音设置" : "首次设备设置");
            GUI.Label(new Rect(170, 225, 600, 55), "主音量  " + Mathf.RoundToInt(pendingVolume * 100f) + "%", headingStyle);
            pendingVolume = GUI.HorizontalSlider(new Rect(170, 300, 900, 35), pendingVolume, 0f, 1f);
            AudioListener.volume = pendingVolume;
            GUI.Label(new Rect(170, 390, 850, 55), "分辨率  " + Resolutions[pendingResolution].x + " × " + Resolutions[pendingResolution].y, headingStyle);
            Button(new Rect(170, 470, 420, 85), "切换分辨率", () => pendingResolution = (pendingResolution + 1) % Resolutions.Length);
            Button(new Rect(650, 470, 420, 85), pendingFullscreen ? "显示模式：全屏" : "显示模式：窗口", () => pendingFullscreen = !pendingFullscreen);
            GUI.Label(new Rect(170, 620, 1100, 90), "应用后保存到本机；以后仍可从以太主界面打开设置。", bodyStyle);
            Button(new Rect(1290, 820, 480, 100), "应用设置", ApplySettings);
        }

        private void DrawWorldOpening()
        {
            if (!videoPrepared) Background("世界观动画");
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, .88f);
            GUI.DrawTexture(new Rect(32, 28, 980, 104), panel);
            GUI.color = previous;
            GUI.Label(new Rect(55, 45, 900, 70), videoPrepared ? "世界观动画" : "世界观动画文件未找到（仍可继续）", headingStyle);
            Button(new Rect(1570, 45, 280, 70), "跳过动画", FinishWorldOpening);
        }

        private void DrawLanding()
        {
            Background("OCC　以太主界面");
            GUI.Label(new Rect(170, 245, 1100, 100), "固定短图使用三个独立存档位；覆盖前会再次确认。", bodyStyle);
            Button(new Rect(170, 405, 620, 95), "新游戏", () => BeginSaveSelection(false));
            GUI.enabled = HasAnySaveRecord();
            Button(new Rect(170, 525, 620, 95), "继续游戏", () => BeginSaveSelection(true));
            GUI.enabled = true;
            Button(new Rect(170, 645, 620, 95), "设置", OpenSettingsFromLanding);
            Button(new Rect(870, 645, 620, 95), "重看开场 CG　45 秒", ReplayWorldOpening);
            GUI.Label(new Rect(170, 805, 1120, 90), "双品牌印记与开场 CG 可跳过；重看不会修改任何存档。", smallStyle);
        }

        private void DrawSaveSelection()
        {
            Background(selectingContinue ? "选择要继续的存档" : "选择新游戏存档位");
            for (int i = 0; i < 3; i++)
            {
                bool exists = PlayerPrefs.HasKey(SlotKey(i));
                Rect r = new Rect(150 + i * 570, 300, 480, 300);
                GUI.DrawTexture(r, exists ? done : card);
                GUI.Label(new Rect(r.x + 28, r.y + 28, 420, 55), "存档位 " + (i + 1), headingStyle);
                GUI.Label(new Rect(r.x + 28, r.y + 105, 420, 75), exists ? SlotSummary(i) : "空存档位", bodyStyle);
                GUI.enabled = !selectingContinue || exists;
                int slot = i;
                Button(new Rect(r.x + 28, r.y + 205, 424, 68), selectingContinue ? "继续" : exists ? "选择并覆盖" : "选择并创建", () => SelectSlot(slot));
                GUI.enabled = true;
            }
            Button(new Rect(150, 820, 360, 90), "返回以太主界面", () => data.stage = FlowStage.Landing);
        }

        private void DrawSaveConfirmation()
        {
            Background(pendingOverwrite ? "确认覆盖存档" : "确认创建存档");
            GUI.Label(new Rect(220, 320, 1200, 150), pendingOverwrite
                ? "存档位 " + (selectedSlot + 1) + " 已有进度。确认后将覆盖该槽位。"
                : "将在存档位 " + (selectedSlot + 1) + " 创建固定首次体验。", bodyStyle);
            Button(new Rect(220, 620, 520, 100), "取消", () => data.stage = FlowStage.SaveSelection);
            Button(new Rect(880, 620, 620, 100), pendingOverwrite ? "确认覆盖" : "确认创建", ConfirmCreateSlot);
        }

        private void DrawConfiguration()
        {
            Background("固定学生基础配置");
            GUI.Label(new Rect(150, 190, 1500, 90), "三项固定在同一页展示，不做三选一。", bodyStyle);
            DrawCard(new Rect(150, 330, 480, 310), "公开考核与工读入学", "学生背景");
            DrawCard(new Rect(720, 330, 480, 310), "就地接线", "固定天赋");
            DrawCard(new Rect(1290, 330, 480, 310), "借障导流", "固定专属术式");
            Button(new Rect(1290, 820, 480, 100), "确认基础配置", () => SetStage(FlowStage.AcademyIntro));
        }

        private void DrawAcademyIntro()
        {
            Background("学院阶段　入学实操");
            GUI.Label(new Rect(170, 280, 1250, 220), "完成三场普通战、三个事件、工坊加工和健康确认后，才能进入不可逆的精英挑战。\n\n本段可跳过；首次地图仅开放战斗1。", bodyStyle);
            Button(new Rect(1290, 820, 480, 100), "进入首次地图", () => EnterAcademyMap(false));
        }

        private void DrawRuntimeHandoff()
        {
            Background("进入学院地图");
            GUI.Label(new Rect(170, 280, 1250, 180), string.IsNullOrEmpty(handoffError)
                ? "正在连接主场景中的学院地图与真实战斗流程。"
                : handoffError, bodyStyle);
            Button(new Rect(170, 700, 520, 90), "返回以太主界面", ReturnToLanding);
            Button(new Rect(1250, 700, 520, 90), "重试进入地图",
                () => EnterAcademyMap(FirstExperienceSaveRouting.HasMapRun(data.slot)));
        }

        private void DrawFlowShell()
        {
            GUI.DrawTexture(new Rect(0, 0, DesignWidth, DesignHeight), panel);
            GUI.Label(new Rect(48, 28, 1320, 55), "首次体验路线　存档位 " + (selectedSlot + 1), titleStyle);
            DrawRoute();
            GUI.DrawTexture(new Rect(1440, 0, 480, 1080), card);
            DrawCurrentPanel();
        }

        private void DrawRoute()
        {
            int current = Mathf.Clamp((int)data.stage - (int)FlowStage.Map, 0, RouteLabels.Length - 1);
            for (int i = 0; i < RouteLabels.Length; i++)
            {
                int col = i % 3, row = i / 3;
                Rect r = new Rect(48 + col * 448, 112 + row * 112, 400, 76);
                if (i < current) GUI.DrawTexture(r, done); else if (i == current) GUI.DrawTexture(r, accent);
                GUI.Box(r, (i < current ? "✓ " : i == current ? "▶ " : "  ") + RouteLabels[i], cardStyle);
            }
        }

        private void DrawCurrentPanel()
        {
            switch (data.stage)
            {
                case FlowStage.Map: Panel("首次地图", "当前只有战斗1可达。先查看任务、敌情、地形与奖励类型。", "查看战斗1", FlowStage.Battle1Preview); break;
                case FlowStage.Battle1Preview: Preview("战斗1预览", "学院实地对抗课程；尚未确认时可返回地图。", FlowStage.Battle1); break;
                case FlowStage.Battle1: Battle("战斗1", 3, FlowStage.Battle1Result); break;
                case FlowStage.Battle1Result: Panel("战斗1战果", "战损与节点完成状态只读。", "确认战果", FlowStage.Reward1); break;
                case FlowStage.Reward1: Reward("奖励1", 1, FlowStage.Events12); break;
                case FlowStage.Events12: Events12(); break;
                case FlowStage.Battle2Preview: Preview("战斗2预览", "查看上一奖励与事件所得法宝的作用。", FlowStage.Battle2); break;
                case FlowStage.Battle2: Battle("战斗2", 3, FlowStage.Battle2Result); break;
                case FlowStage.Battle2Result: Panel("战斗2战果", "本页只读，确认后显示第二组奖励。", "确认战果", FlowStage.Reward2); break;
                case FlowStage.Reward2: Reward("奖励2", 2, FlowStage.Event3Workshop); break;
                case FlowStage.Event3Workshop: Event3Workshop(); break;
                case FlowStage.Battle3Preview: Preview("战斗3预览", "事件3已完成；工坊可在本战前后完成。", FlowStage.Battle3); break;
                case FlowStage.Battle3: Battle("战斗3", 3, FlowStage.Battle3Result); break;
                case FlowStage.Battle3Result: Panel("战斗3战果", "本页只读，确认后显示第三组奖励。", "确认战果", FlowStage.Reward3); break;
                case FlowStage.Reward3: Reward("奖励3", 3, FlowStage.Medical); break;
                case FlowStage.Medical: Medical(); break;
                case FlowStage.ElitePreview: ElitePreview(); break;
                case FlowStage.Elite: Battle("精英战", 5, FlowStage.EliteResult); break;
                case FlowStage.EliteResult: Panel("精英战果", "胜利已保存；本页只读。", "确认战果", FlowStage.EliteReward); break;
                case FlowStage.EliteReward: Panel("精英奖励", "领取后开放并首次进入商店。", "领取并打开商店", FlowStage.Shop); break;
                case FlowStage.Shop: Shop(); break;
                case FlowStage.Complete: Panel("固定引导完成", "完成标记已写入；下一步进入常规单轮创建。", "继续", FlowStage.RunCreation); break;
                case FlowStage.RunCreation: CustomPanel("常规单轮创建", "固定短图不再生成。正式版本将在这里写入模式、种子、版本、40节点内容和初始资源。", "返回以太主界面", ReturnToLanding); break;
            }
        }

        private void Preview(string title, string detail, FlowStage battle) => CustomPanel(title, detail + "\n\n确认后创建战斗入口快照。", "确认出发", () => BeginBattle(battle));

        private void Battle(string label, int required, FlowStage result)
        {
            Heading(label + "　占位战斗");
            Body("执行移动、攻击或施术累计行动。\n\n行动已完成 " + data.battleActions + "　要求 " + required);
            Small(330, "移动", AddBattleAction); Small(430, "攻击", AddBattleAction); Small(530, "施术", AddBattleAction);
            GUI.enabled = data.battleActions >= required;
            Primary("完成战斗", () => CompleteBattle(result));
            GUI.enabled = true;
        }

        private void Reward(string label, int group, FlowStage next)
        {
            Heading(label + "　三选一");
            Body("候选固定，返回或继续游戏不会刷新。");
            Small(330, "候选 A　稳定", () => ChooseReward(group, "A", next));
            Small(440, "候选 B　输出", () => ChooseReward(group, "B", next));
            Small(550, "候选 C　机动", () => ChooseReward(group, "C", next));
        }

        private void Events12()
        {
            Heading("事件1 与事件2");
            Body("两项都必须完成，先后不限。\n\n事件1：" + Mark(data.event1Done) + "\n事件2：" + Mark(data.event2Done));
            GUI.enabled = !data.event1Done; Small(360, "完成事件1", () => { data.event1Done = true; Save(); });
            GUI.enabled = !data.event2Done; Small(470, "完成事件2", () => { data.event2Done = true; Save(); });
            GUI.enabled = data.event1Done && data.event2Done; Primary("查看战斗2", () => SetStage(FlowStage.Battle2Preview));
            GUI.enabled = true;
        }

        private void Event3Workshop()
        {
            Heading("事件3 与工坊");
            Body("事件3开放战斗3；锻造与专精可在战斗3前后完成，但都是精英门槛。\n\n事件3：" + Mark(data.event3Done) + "\n锻造：" + Mark(data.forged) + "\n专精：" + Mark(data.specialized));
            GUI.enabled = !data.event3Done; Small(350, "完成事件3", () => { data.event3Done = true; Save(); });
            GUI.enabled = !data.forged; Small(450, "确认一次锻造", () => { data.forged = true; Save(); });
            GUI.enabled = !data.specialized; Small(550, "确认一次专精", () => { data.specialized = true; Save(); });
            GUI.enabled = data.event3Done;
            Primary(data.battle3Completed ? "返回医务室" : "查看战斗3", () => SetStage(data.battle3Completed ? FlowStage.Medical : FlowStage.Battle3Preview));
            GUI.enabled = true;
        }

        private void Medical()
        {
            Heading("医务室与精英门槛");
            Body("锻造、专精、健康确认全部完成后才能查看精英。\n\n锻造：" + Mark(data.forged) + "\n专精：" + Mark(data.specialized) + "\n健康：" + Mark(data.healthChecked));
            GUI.enabled = !data.healthChecked; Small(405, "完成免费健康确认", () => { data.healthChecked = true; Save(); });
            GUI.enabled = !data.healed; Small(500, "可选：治疗", () => { data.healed = true; Save(); });
            GUI.enabled = !data.mealBought; Small(595, "可选：购买餐食", () => { data.mealBought = true; Save(); });
            GUI.enabled = true;
            if (!data.forged || !data.specialized) Small(700, "返回工坊补全加工", () => SetStage(FlowStage.Event3Workshop));
            GUI.enabled = data.healthChecked && data.forged && data.specialized; Primary("查看精英挑战", () => SetStage(FlowStage.ElitePreview));
            GUI.enabled = true;
        }

        private void ElitePreview()
        {
            Heading("精英挑战预览");
            Body("目标、敌情和构筑已公开。进入后不可返回本轮地图；失败不开放商店。\n\n" + (eliteArmed ? "已阅读风险，请再次确认。" : "尚未确认不可逆风险。"));
            if (!eliteArmed) Primary("阅读并接受风险", () => eliteArmed = true);
            else Primary("确认不可逆挑战", () => { eliteArmed = false; BeginBattle(FlowStage.Elite); });
        }

        private void Shop()
        {
            Heading("商店　已首次打开");
            Body("首次打开已满足强制条件；购买可选。\n\n余额：" + data.coins + "\n购买：" + Mark(data.shopPurchase));
            GUI.enabled = !data.shopPurchase && data.coins >= 3;
            Small(400, "购买占位商品　价格 3", () => { data.coins -= 3; data.shopPurchase = true; Save(); });
            GUI.enabled = true;
            Primary("离开商店", CompleteFixedExperience);
        }

        private void Background(string title)
        {
            Texture2D backdrop = CurrentBackdrop();
            GUI.DrawTexture(new Rect(0, 0, DesignWidth, DesignHeight), backdrop == null ? panel : backdrop, ScaleMode.StretchToFill, false);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, .94f);
            GUI.DrawTexture(new Rect(112, 74, 1696, 912), card);
            GUI.color = previous;
            DrawFrame(new Rect(112, 74, 1696, 912));
            GUI.Label(new Rect(170, 110, 1500, 80), title, titleStyle);
        }

        private Texture2D CurrentBackdrop()
        {
            if (data.stage == FlowStage.FirstSettings) return settingsBackdrop;
            if (data.stage == FlowStage.SaveSelection || data.stage == FlowStage.SaveConfirmation || data.stage == FlowStage.Configuration) return archiveBackdrop;
            if (data.stage == FlowStage.AcademyIntro) return startupBackdrop;
            return landingBackdrop;
        }

        private void DrawFrame(Rect rect)
        {
            const float thickness = 6f;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), panel);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), panel);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), panel);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), panel);
        }
        private void DrawCard(Rect r, string title, string detail)
        {
            GUI.DrawTexture(r, card); GUI.Label(new Rect(r.x + 25, r.y + 25, r.width - 50, 55), title, headingStyle);
            GUI.Label(new Rect(r.x + 25, r.y + 100, r.width - 50, r.height - 120), detail, bodyStyle);
        }
        private void Panel(string title, string detail, string action, FlowStage next) => CustomPanel(title, detail, action, () => SetStage(next));
        private void CustomPanel(string title, string detail, string action, Action callback) { Heading(title); Body(detail); Primary(action, callback); }
        private void Heading(string text) => GUI.Label(new Rect(1480, 45, 400, 90), text, headingStyle);
        private void Body(string text) => GUI.Label(new Rect(1480, 145, 390, 240), text, bodyStyle);
        private void Primary(string label, Action action) => Button(new Rect(1480, 900, 390, 100), label, action);
        private void Small(float y, string label, Action action) => Button(new Rect(1480, y, 390, 78), label, action);
        private void Button(Rect r, string label, Action action) { if (GUI.Button(r, label, buttonStyle)) action(); }
        private static string Mark(bool value) => value ? "已完成" : "未完成";
        private static string SlotKey(int slot) => SavePrefix + slot;

        public bool SlotExists(int slot) => slot >= 0 && slot < 3 && PlayerPrefs.HasKey(SlotKey(slot));
        public string SlotDisplaySummary(int slot) => SlotSummary(slot);

        public static bool HasAnySaveRecord()
        {
            for (int i = 0; i < 3; i++) if (PlayerPrefs.HasKey(SlotKey(i))) return true;
            return false;
        }

        private string SlotSummary(int slot)
        {
            SaveData loaded = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SlotKey(slot), ""));
            return loaded == null ? "存档不可读" : "进度：" + loaded.stage + (loaded.fixedExperienceComplete ? "\n固定引导已完成" : "\n固定引导进行中");
        }

        private void PlayBranding() { StopOpening(); data.stage = FlowStage.Branding; brandIndex = -1; AdvanceBrand(); }
        private void AdvanceBrand()
        {
            int next = brandIndex + 1;
            while (brandMarks != null && next < brandMarks.Length && brandMarks[next] == null) next++;
            if (brandMarks != null && next < brandMarks.Length) { brandIndex = next; brandStartedAt = Time.unscaledTime; return; }
            FinishBranding();
        }

        public void FinishBranding()
        {
            brandIndex = -1;
            if (PlayerPrefs.GetInt(SettingsSeenKey, 0) == 0) { settingsFromLanding = false; data.stage = FlowStage.FirstSettings; }
            else ContinueAfterSettings();
        }
        private void ContinueAfterSettings() { if (PlayerPrefs.GetInt(OpeningSeenKey, 0) == 0) PlayWorldOpening(); else EnterFormalMainMenu(); }
        private void PlayWorldOpening()
        {
            StopOpening(); data.stage = FlowStage.WorldOpening; openingStarted = true;
            if (!videoPrepared) return;
            if (openingPlayer.isPrepared) openingPlayer.Play(); else openingPlayer.Prepare();
        }
        private void OnOpeningPrepared(VideoPlayer player) { if (data.stage == FlowStage.WorldOpening && openingStarted) player.Play(); }
        private void StopOpening() { if (openingPlayer != null && openingPlayer.isPlaying) openingPlayer.Stop(); openingStarted = false; brandIndex = -1; }
        public void FinishWorldOpening()
        {
            if (data.stage != FlowStage.WorldOpening) return;
            StopOpening(); PlayerPrefs.SetInt(OpeningSeenKey, 1); PlayerPrefs.Save(); EnterFormalMainMenu();
        }

        private void EnterFormalMainMenu()
        {
            if (combatBootstrap == null)
            {
                ShowLanding();
                return;
            }
            combatBootstrap.EnterMainMenuFromOpening();
        }

        private void OpenSettingsFromLanding() { settingsFromLanding = true; data.stage = FlowStage.FirstSettings; }
        private void ApplySettings()
        {
            Vector2Int r = Resolutions[pendingResolution];
            AudioListener.volume = pendingVolume;
            Screen.SetResolution(r.x, r.y, pendingFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            PlayerPrefs.SetFloat(VolumeKey, pendingVolume); PlayerPrefs.SetInt(ResolutionKey, pendingResolution);
            PlayerPrefs.SetInt(FullscreenKey, pendingFullscreen ? 1 : 0); PlayerPrefs.SetInt(SettingsSeenKey, 1); PlayerPrefs.Save();
            if (settingsFromLanding) { settingsFromLanding = false; data.stage = FlowStage.Landing; } else ContinueAfterSettings();
        }

        private void BeginSaveSelection(bool forContinue) { selectingContinue = forContinue; selectedSlot = -1; data.stage = FlowStage.SaveSelection; }
        private void SelectSlot(int slot)
        {
            selectedSlot = slot;
            if (selectingContinue) { ContinueSlot(slot); return; }
            pendingOverwrite = PlayerPrefs.HasKey(SlotKey(slot)); data.stage = FlowStage.SaveConfirmation;
        }
        private void ConfirmCreateSlot()
        {
            data = new SaveData { slot = selectedSlot, stage = FlowStage.Configuration };
        }
        private void ContinueSlot(int slot)
        {
            SaveData loaded = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SlotKey(slot), ""));
            if (loaded == null)
            {
                // Keep the player in slot selection and explain why continue
                // did not proceed; a silent return looked like a dead button.
                selectedSlot = slot;
                handoffError = "该存档无法读取，数据仍保留。请选择其他槽位，或返回后重新开始。";
                data.stage = FlowStage.SaveSelection;
                return;
            }
            handoffError = string.Empty;
            data = loaded; selectedSlot = slot; data.slot = slot;
            if (IsRuntimeStage(data.stage)) EnterAcademyMap(FirstExperienceSaveRouting.HasMapRun(slot));
        }
        public void StartNewRun() => BeginSaveSelection(false);
        public void ContinueRun() => BeginSaveSelection(true);
        public void ShowLanding()
        {
            StopOpening();
            selectingContinue = false;
            pendingOverwrite = false;
            settingsFromLanding = false;
            selectedSlot = -1;
            data.stage = FlowStage.Landing;
            enabled = true;
        }
        public void ReplayWorldOpening() => PlayWorldOpening();
        public void OpenSettings() => OpenSettingsFromLanding();
        public void ApplyPendingSettings() => ApplySettings();
        public void CycleResolution() => pendingResolution = (pendingResolution + 1) % Resolutions.Length;
        public void ChooseSlot(int slot) => SelectSlot(slot);
        public void ConfirmSelectedSlot() => ConfirmCreateSlot();
        public void ConfirmConfiguration()
        {
            FirstExperienceSaveRouting.DeleteMapRun(selectedSlot);
            data.slot = selectedSlot;
            data.stage = FlowStage.AcademyIntro;
            Save();
        }
        public void EnterAcademy() => EnterAcademyMap(false);
        public void GoBack()
        {
            switch (CurrentStage)
            {
                case FlowStage.FirstSettings:
                    if (settingsFromLanding) { settingsFromLanding = false; data.stage = FlowStage.Landing; }
                    break;
                case FlowStage.SaveSelection: data.stage = FlowStage.Landing; break;
                case FlowStage.SaveConfirmation: data.stage = FlowStage.SaveSelection; break;
                case FlowStage.Configuration: data.stage = FlowStage.SaveSelection; break;
                default: break;
            }
        }

        private void EnterAcademyMap(bool continueSave)
        {
            if (combatBootstrap == null)
            {
                Debug.LogError("首次体验无法进入学院地图：主场景 CombatPrototypeBootstrap 未绑定。");
                return;
            }
            data.stage = FlowStage.Map;
            Save();
            FirstExperienceSaveRouting.SelectSlot(data.slot);
            if (!combatBootstrap.EnterFirstExperienceFromOpening(continueSave))
            {
                handoffError = "真实首次体验运行态未能创建或读取。当前槽位数据仍保留，请返回入口或重试。";
                return;
            }
            handoffError = string.Empty;
            enabled = false;
        }

        public void ResetPrototype()
        {
            StopOpening();
            for (int i = 0; i < 3; i++) PlayerPrefs.DeleteKey(SlotKey(i));
            PlayerPrefs.DeleteKey(SettingsSeenKey); PlayerPrefs.DeleteKey(OpeningSeenKey); PlayerPrefs.Save();
            selectedSlot = -1; data = new SaveData { stage = FlowStage.Branding }; PlayBranding();
        }

        private void BeginBattle(FlowStage stage) { data.battleActions = 0; SetStage(stage); }
        private void CompleteBattle(FlowStage result) { if (data.stage == FlowStage.Battle3) data.battle3Completed = true; SetStage(result); }
        public void AddBattleAction()
        {
            if (data.stage != FlowStage.Battle1 && data.stage != FlowStage.Battle2 && data.stage != FlowStage.Battle3 && data.stage != FlowStage.Elite) return;
            data.battleActions++; Save();
        }
        private void ChooseReward(int group, string choice, FlowStage next)
        {
            if (group == 1) data.reward1 = choice; else if (group == 2) data.reward2 = choice; else data.reward3 = choice;
            SetStage(next);
        }
        private void CompleteFixedExperience() { data.fixedExperienceComplete = true; SetStage(FlowStage.Complete); }
        private void SetStage(FlowStage stage) { data.stage = stage; data.battleActions = 0; Save(); }
        private void ReturnToLanding() { StopOpening(); data.stage = FlowStage.Landing; }
        private void Save()
        {
            if (data.slot < 0 || data.slot > 2) return;
            PlayerPrefs.SetString(SlotKey(data.slot), JsonUtility.ToJson(data)); PlayerPrefs.Save();
        }

        public void DebugAdvance()
        {
            switch (data.stage)
            {
                case FlowStage.Branding: FinishBranding(); break;
                case FlowStage.FirstSettings: ApplySettings(); break;
                case FlowStage.WorldOpening: FinishWorldOpening(); break;
                case FlowStage.Landing: BeginSaveSelection(false); break;
                case FlowStage.SaveSelection: SelectSlot(0); break;
                case FlowStage.SaveConfirmation: ConfirmCreateSlot(); break;
                case FlowStage.Configuration: SetStage(FlowStage.AcademyIntro); break;
                case FlowStage.AcademyIntro: EnterAcademyMap(false); break;
                default: EnterAcademyMap(FirstExperienceSaveRouting.HasMapRun(data.slot)); break;
            }
        }
    }
}
