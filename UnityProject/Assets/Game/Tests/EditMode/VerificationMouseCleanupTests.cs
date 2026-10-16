using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OCC.Combat.Tests
{
    public sealed class VerificationMouseCleanupTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void EndRestoresNativeDeviceEvenAfterManagedReferencesAreLost(bool loseReferences)
        {
            var original=Mouse.current;
            if(original==null || !original.enabled)Assert.Ignore("Requires an enabled native editor mouse.");
            var fixture=AppDomain.CurrentDomain.GetAssemblies()
                .Select(a=>a.GetType("OCC.Combat.EditorTools.DetailsInputFixture"))
                .FirstOrDefault(t=>t!=null);
            Assert.That(fixture,Is.Not.Null);
            var begin=fixture.GetMethod("Begin");var end=fixture.GetMethod("End");
            bool background=Application.runInBackground;
            try
            {
                begin.Invoke(null,new object[]{background});
                Assert.That(original.enabled,Is.False);
                Assert.That(Mouse.current.name,Is.EqualTo("OCCDetailsVerificationMouse"));
                if(loseReferences)
                {
                    foreach(var name in new[]{"native","simulated"})
                        fixture.GetField(name,BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
                    fixture.GetField("configured",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,false);
                }
                end.Invoke(null,null);end.Invoke(null,null);
                Assert.That(original.enabled,Is.True);
                Assert.That(Mouse.current,Is.SameAs(original));
                Assert.That(InputSystem.devices.Any(d=>d.name=="OCCDetailsVerificationMouse"),Is.False);
                Assert.That(Application.runInBackground,Is.EqualTo(background));
            }
            finally { end.Invoke(null,null); }
        }
    }
}
