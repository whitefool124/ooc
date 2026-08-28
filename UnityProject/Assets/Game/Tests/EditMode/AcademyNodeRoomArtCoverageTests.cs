using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class AcademyNodeRoomArtCoverageTests
    {
        [Test]
        public void EveryAcademyNodeTypeHasAnIndependentFormalIdentityIcon()
        {
            RogueliteMapNodeType[] types = RogueliteMapCatalog.Nodes.Select(value => value.Type).Distinct().ToArray();
            Assert.That(types, Is.EquivalentTo(Enum.GetValues(typeof(RogueliteMapNodeType))));
            foreach (RogueliteMapNodeType type in types)
            {
                string runtimeId = type.ToString().ToLowerInvariant();
                Assert.That(FormalArtRegistry.NodeTypes.Any(value => value.RuntimeId == runtimeId), Is.True, runtimeId);
                Assert.That(Resources.Load<Sprite>(FormalArtRegistry.NodeTypePath(runtimeId)), Is.Not.Null, runtimeId);
            }
        }

        [Test]
        public void NavigationArtCoversEnterConfirmBackAndCloseActions()
        {
            foreach (string id in new[] { "continue", "confirm", "back", "close" })
                Assert.That(Resources.Load<Sprite>(FormalArtRegistry.NavigationPath(id)), Is.Not.Null, id);
        }
    }
}
