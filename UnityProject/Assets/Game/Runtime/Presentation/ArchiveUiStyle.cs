using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Archive controls and large dossier pages each use one complete nine-sliced sprite.
    internal static class ArchiveUiStyle
    {
        internal static readonly Color Paper = new Color(.96f, .925f, .845f, 1f);
        internal static readonly Color LightPaper = new Color(1f, .976f, .918f, 1f);
        internal static readonly Color Ink = new Color(.19f, .15f, .105f, 1f);
        internal static readonly Color QuietInk = new Color(.39f, .33f, .255f, 1f);
        internal static readonly Color Brass = new Color(.59f, .43f, .205f, 1f);
        internal static readonly Color Rule = new Color(.43f, .35f, .255f, 1f);
        private const string ScrollPath = "Art/ArchiveUiFormal/primary_scroll";
        private const string TabPath = "Art/ArchiveUiFormal/nav_tab";
        private const string PagePath = "Art/ArchiveUiFormal/archive_page_panel";
        private const string NotePath = "Art/ArchiveUiFormal/archive_memo_note";

        internal static void PaperPanel(GameObject panel, Color fill, bool frame)
        {
            if (panel == null) return;
            Image image = panel.GetComponent<Image>();
            if (image == null) return;
            Image skin = FormalUiKit.SkinOverlay(image);
            if (skin != null) skin.gameObject.SetActive(false);
            HideOldChrome(panel.transform);
            image.color=fill;
            if(frame)
            {
                bool hit=image.raycastTarget;
                FormalUiChrome.Apply(image,UiChromeRole.Panel);
                image.raycastTarget=hit;
            }
            else {image.sprite=null;image.type=Image.Type.Simple;}

        }

        // Content written on an archive page should not look like a second sheet pasted on top.
        internal static void PaperContent(GameObject panel)
        {
            PaperPanel(panel, Color.clear, false);
            if (panel != null && panel.TryGetComponent(out Image image)) image.raycastTarget = false;
        }

        // A loose archival note is a secondary information surface, never a spell/item card.
        internal static void NotePanel(GameObject panel)
        {
            if (panel == null || !panel.TryGetComponent(out Image image)) return;
            Sprite note = Resources.Load<Sprite>(NotePath);
            if (note == null)
            {
                Debug.LogError("Archive note art is missing: " + NotePath);
                return;
            }
            Image skin = FormalUiKit.SkinOverlay(image);
            if (skin != null) skin.gameObject.SetActive(false);
            HideOldChrome(panel.transform);
            image.sprite = note;
            image.type = Image.Type.Sliced;
            FormalUiChrome.Apply(image,UiChromeRole.Button);
            image.color = Color.white;
            image.raycastTarget = false;
        }

        internal static void TrimPanel(GameObject panel)
        {
            if (panel == null) return;
            Image image = panel.GetComponent<Image>();
            if (image == null) return;
            HideOldChrome(panel.transform);
            image.sprite = null;
            image.type = Image.Type.Simple;
        }

        internal static void TrimTab(Button button)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            Sprite tab = FormalUiChrome.Frame;
            if (image == null || tab == null) return;
            HideOldChrome(button.transform);
            image.sprite = tab;
            image.type = Image.Type.Sliced;
            FormalUiChrome.Apply(image,UiChromeRole.Button);
            image.color = Color.white;
        }

        internal static void ScrollButton(Button button, Func<UiMotionProfile> motion,
            Action<UiActionFeedback> feedback = null, string disabledReason = null)
        {
            if (button == null) return;
            Sprite scroll = FormalUiChrome.Frame;
            Image image = button.GetComponent<Image>();
            if (scroll == null || image == null)
            {
                Debug.LogError("Archive scroll art is missing: " + ScrollPath);
                return;
            }
            Image oldSkin = FormalUiKit.SkinOverlay(image);
            if (oldSkin != null) oldSkin.gameObject.SetActive(false);
            HideOldChrome(button.transform);
            image.sprite = scroll;
            image.type = Image.Type.Sliced;
            FormalUiChrome.Apply(image,UiChromeRole.Button);
            image.color = Color.white;
            button.targetGraphic = image;
            FormalUiKit.ConfigureButtonFeedback(button,
                new FormalUiButtonPalette(Color.white, new Color(1f, .96f, .86f, 1f),
                    new Color(.82f, .72f, .57f, 1f), Color.white, new Color(.69f, .67f, .63f, .7f)),
                motion, feedback, disabledReason);
            Transform label = button.transform.Find("文字");
            if (label != null && label.TryGetComponent(out Text text)) text.color = Ink;
            FocusRule(button.transform);
        }

        internal static void TabButton(Button button, bool selected, Func<UiMotionProfile> motion,
            Action<UiActionFeedback> feedback = null, string disabledReason = null)
        {
            if (button == null) return;
            Sprite tab = FormalUiChrome.Frame;
            Image image = button.GetComponent<Image>();
            if (tab == null || image == null)
            {
                Debug.LogError("Archive tab art is missing: " + TabPath);
                return;
            }
            Image oldSkin = FormalUiKit.SkinOverlay(image);
            if (oldSkin != null) oldSkin.gameObject.SetActive(false);
            HideOldChrome(button.transform);
            image.sprite = tab;
            image.type = Image.Type.Sliced;
            button.targetGraphic = image;
            Color normal = selected ? new Color(.92f, .87f, .76f, 1f) : Color.white;
            FormalUiKit.ConfigureButtonFeedback(button,
                new FormalUiButtonPalette(normal, new Color(1f, .97f, .9f, 1f),
                    new Color(.78f, .70f, .57f, 1f), normal, new Color(.75f, .72f, .66f, 1f)),
                motion, feedback, disabledReason);
            foreach (Text label in button.GetComponentsInChildren<Text>()) label.color = Ink;
            button.GetComponent<UiButtonFeedback>()?.SetSelectedState(selected);
            FocusRule(button.transform);
        }

        private static void FocusRule(Transform button)
        {
            Transform frame = button.Find("像素焦点框");
            if (frame == null) return;
            Image image = frame.GetComponent<Image>();
            if (image == null) return;
            Image skin = FormalUiKit.SkinOverlay(image);
            if (skin != null) skin.gameObject.SetActive(false);
            HideThinBorder(frame);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMin = new Vector2(20f, 4f);
            rect.offsetMax = new Vector2(-20f, 6f);
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = FormalUiTheme.Cyan;
        }

        internal static void HideOldChrome(Transform parent)
        {
            HideThinBorder(parent);
            HideLayer(parent, "纸页视觉层");
            HideLayer(parent, "签条纸面层");
            HideLayer(parent, "卷轴纸面");
        }

        private static void HideThinBorder(Transform parent)
        {
            foreach (string edge in new[]
            {
                "细边_上", "细边_下", "细边_左", "细边_右",
                "签条细框_上", "签条细框_下", "签条细框_左", "签条细框_右",
                "档案细框_上", "档案细框_下", "档案细框_左", "档案细框_右"
            })
            {
                Transform child = parent.Find(edge);
                if (child != null) child.gameObject.SetActive(false);
            }
        }

        private static void HideLayer(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) child.gameObject.SetActive(false);
        }
    }
}
