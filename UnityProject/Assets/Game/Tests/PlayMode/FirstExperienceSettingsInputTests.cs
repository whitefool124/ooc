using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class FirstExperienceSettingsInputTests
    {
        private GameObject owner;
        private Canvas canvas;
        private Mouse mouse;
        private FirstExperiencePrototypeController flow;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode originalInputBehavior;
#endif

        [UnityTest]
        public IEnumerator SettingsAndConfirmationAcceptMousePressAndReleaseAcrossFrames()
        {
#if UNITY_EDITOR
            originalInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            owner = new GameObject("Settings input test");
            flow = owner.AddComponent<FirstExperiencePrototypeController>();
            var ui = owner.GetComponent<FormalFirstExperienceUi>();
            Assert.That(ui, Is.Not.Null);
            canvas = (Canvas)typeof(FormalFirstExperienceUi).GetField("canvas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            Assert.That(canvas, Is.Not.Null);
            flow.ShowLanding();
            flow.OpenSettings();
            mouse = InputSystem.AddDevice<Mouse>();
            yield return null;
            yield return null;

            int resolution = flow.ResolutionIndex;
            yield return Click("按钮_设置_分辨率_左");
            Assert.That(flow.ResolutionIndex, Is.EqualTo((resolution + 1) % flow.ResolutionCount));
            string mode = flow.DisplayModeLabel;
            yield return Click("按钮_设置_显示模式_左");
            Assert.That(flow.DisplayModeLabel, Is.Not.EqualTo(mode));
            bool sync = flow.PendingVSync;
            yield return Click("按钮_设置_垂直同步_左");
            Assert.That(flow.PendingVSync, Is.EqualTo(!sync));
            if (flow.PendingVSync) yield return Click("按钮_设置_垂直同步_左");
            string frameRate = flow.FrameRateLabel;
            yield return Click("按钮_设置_帧率上限_左");
            Assert.That(flow.FrameRateLabel, Is.Not.EqualTo(frameRate));

            yield return Click("按钮_应用设置");
            Assert.That(flow.IsConfirmingDisplay, Is.True);
            Button keep = FindButton("按钮_保留画面设置");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(keep != null, Is.True, "Countdown updates must preserve the pressed control.");
            yield return Click("按钮_恢复画面设置");
            Assert.That(flow.IsConfirmingDisplay, Is.False);
            yield return Click("按钮_取消");
            Assert.That(flow.CurrentStage, Is.EqualTo(FirstExperiencePrototypeController.FlowStage.Landing));
        }

        private Button FindButton(string name) => canvas.GetComponentsInChildren<Button>()
            .Single(button => button.name == name && button.gameObject.activeInHierarchy);

        private IEnumerator Click(string name)
        {
            Button button = FindButton(name);
            Assert.That(button.interactable, Is.True, name);
            RectTransform rect = (RectTransform)button.transform;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            Assert.That(button != null, Is.True, "A pointer-down target must survive until pointer-up: " + name);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = originalInputBehavior;
#endif
            if (flow != null) flow.RevertDisplaySettings();
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (owner != null) Object.Destroy(owner);
            if (canvas != null) Object.Destroy(canvas.gameObject);
            yield return null;
        }
    }
}
