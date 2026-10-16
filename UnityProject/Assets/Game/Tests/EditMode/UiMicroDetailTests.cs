using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class UiMicroDetailTests
    {
        private GameObject root;
        private UiButtonFeedback feedback;
        private Button button;
        private float intensity;

        [SetUp] public void SetUp()
        {
            intensity = 0f;
            root = new GameObject("Micro detail test", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 64f);
            button = root.GetComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            feedback = root.AddComponent<UiButtonFeedback>();
            feedback.Configure(button, Color.white, Color.white, Color.white, Color.white, Color.gray,
                () => UiMotionProfile.FromIntensity(intensity), null);
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void KeyboardThenPointer_UsesOneTrace_AndClearsOnlyAfterBothLeave()
        {
            intensity = 1f;
            feedback.OnSelect(null);
            feedback.OnPointerEnter(null);
            Assert.That(root.GetComponentsInChildren<PeripheralPixelVfx>().Length, Is.EqualTo(1));
            feedback.OnPointerExit(null);
            Assert.That(root.GetComponentsInChildren<PeripheralPixelVfx>().Length, Is.EqualTo(1));
            feedback.OnDeselect(null);
            Assert.That(root.GetComponentsInChildren<PeripheralPixelVfx>().Length, Is.Zero);
        }

        [Test]
        public void RapidPointerReentry_DoesNotAccumulateTraces()
        {
            intensity = 1f;
            for (int i = 0; i < 12; i++)
            {
                feedback.OnPointerEnter(null);
                feedback.OnPointerExit(null);
            }
            feedback.OnPointerEnter(null);
            Assert.That(root.GetComponentsInChildren<PeripheralPixelVfx>(true).Length, Is.EqualTo(1));
        }

        [Test]
        public void SelectedBookmark_RemainsAtZeroMotion_WithoutInterceptingInput()
        {
            feedback.SetSelectedState(true);
            feedback.SetSelectedState(true);
            feedback.OnSelect(null);
            var marks = root.GetComponentsInChildren<UiSelectionBookmark>();
            Assert.That(marks.Length, Is.EqualTo(1));
            Assert.That(marks[0].raycastTarget, Is.False);
            Assert.That(root.GetComponentsInChildren<PeripheralPixelVfx>().Length, Is.Zero);
            feedback.SetSelectedState(false);
            Assert.That(root.GetComponentsInChildren<UiSelectionBookmark>().Length, Is.Zero);
        }

        [Test]
        public void FocusReentry_AfterDeselectStartsOneFreshTrace()
        {
            intensity = 1f;
            feedback.OnSelect(null);
            feedback.OnDeselect(null);
            Assert.That(root.GetComponentsInChildren<PeripheralPixelVfx>(true).Length, Is.Zero);
            Assert.That(root.transform.Find("像素焦点框").gameObject.activeSelf, Is.False);
            feedback.OnSelect(null);
            feedback.OnSelect(null);
            Assert.That(root.GetComponentsInChildren<PeripheralPixelVfx>(true).Length, Is.EqualTo(1));
        }

        [Test]
        public void UnavailableButton_DoesNotProduceExecutableFocusTrace()
        {
            intensity = 1f;
            feedback.OnPointerEnter(null);
            feedback.SetAvailability(false, "不可执行");
            feedback.OnSelect(null);
            Assert.That(root.GetComponentsInChildren<PeripheralPixelVfx>(true).Length, Is.Zero);
        }
    }
}
