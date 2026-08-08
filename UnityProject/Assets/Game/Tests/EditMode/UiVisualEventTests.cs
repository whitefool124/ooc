using System;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class UiVisualEventTests
    {
        [Test]
        public void Event_PreservesSignedResourceDelta()
        {
            var visualEvent = new UiVisualEvent(UiVisualEventKind.ResourceChanged, "零件", -2, "消费");

            Assert.That(visualEvent.Kind, Is.EqualTo(UiVisualEventKind.ResourceChanged));
            Assert.That(visualEvent.Subject, Is.EqualTo("零件"));
            Assert.That(visualEvent.Delta, Is.EqualTo(-2));
            Assert.That(visualEvent.Message, Is.EqualTo("消费"));
        }

        [Test]
        public void Event_RejectsMissingSubject()
        {
            Assert.Throws<ArgumentException>(() => new UiVisualEvent(UiVisualEventKind.MapNodeSelected, ""));
        }

        [Test]
        public void Stream_PublishesExactlyOnceWithoutOwningState()
        {
            var stream = new UiVisualEventStream();
            int count = 0;
            UiVisualEvent received = default;
            stream.Published += item => { count++; received = item; };

            stream.Publish(new UiVisualEvent(UiVisualEventKind.CombatCommandSubmitted, "Attack"));

            Assert.That(count, Is.EqualTo(1));
            Assert.That(received.Subject, Is.EqualTo("Attack"));
        }
    }
}
