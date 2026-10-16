using System;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class AcademyEconomyAcceptanceTests
    {
        // Settlement/service regression. Combat difficulty is measured separately by real combat commands.
        [TestCase(701)] [TestCase(702)] [TestCase(703)]
        [TestCase(704)] [TestCase(705)] [TestCase(706)]
        public void FixedSeedTutorialIncome_CoversDeclaredServicesWithoutDuplicatingRewards(int seed)
        {
            var run=RogueliteMapRun.CreateFirstRunV1(seed);
            run.AcknowledgeFirstRunOrigin();
            Reward(run,"B1","F-P-R19");
            Event(run,"EV1","FIRST-EV1-ACCEPT-DELIVERY");
            Event(run,"EV2","FIRST-EV2-GOLD");
            Reward(run,"B2","F-P-U04");
            Event(run,"EV3","FIRST-EV3-CONTRIBUTION");
            Reward(run,"B3","F-P-M12");
            run.SelectNode("M");
            int contribution=run.StageContribution;
            run.ChooseFirstRunMeal("MEAL-AETHER");
            Assert.That(run.StageContribution,Is.EqualTo(contribution-1));
            string beforeRejected=Rogue11Serializer.Serialize(run.RogueRunState);
            Assert.Throws<InvalidOperationException>(()=>run.ChooseFirstRunMeal("MEAL-AETHER"));
            Assert.That(Rogue11Serializer.Serialize(run.RogueRunState),Is.EqualTo(beforeRejected));
            run.RogueRunState.CurrentHealth=40; // Isolated treatment fixture; difficulty uses real combat elsewhere.
            run.UseFirstRunHeal();
            Assert.That(run.CurrentHealth,Is.EqualTo(50));
            beforeRejected=Rogue11Serializer.Serialize(run.RogueRunState);
            Assert.Throws<InvalidOperationException>(()=>run.UseFirstRunHeal());
            Assert.That(Rogue11Serializer.Serialize(run.RogueRunState),Is.EqualTo(beforeRejected));
            Reward(run,"X","PASSIVE-ELITE-01");
            Assert.That(run.Gold,Is.EqualTo(26),"8 starting + 9 tutorial + 3 event + 6 elite");
            Assert.That(run.StageContribution,Is.EqualTo(4),"3 tutorial + 1 event - 1 meal - 1 treatment + 2 elite");
            run.SelectNode("S");
            foreach(var offer in run.CurrentShopService.Offers.ToArray())
            {
                int gold=run.Gold;
                run.PurchaseFirstRunOffer(offer.OfferId);
                Assert.That(run.Gold,Is.EqualTo(gold-offer.Price));
                Assert.Throws<InvalidOperationException>(()=>run.PurchaseFirstRunOffer(offer.OfferId));
            }
            Assert.That(run.Gold,Is.EqualTo(13),"3 + 6 + 4 shop prices");
            run=Restore(run);
            Assert.That(run.CurrentShopService.Offers.All(o=>o.Sold),Is.True);
            var mastered=run.RogueRunState.MasteredSpellIds.ToArray();
            run.CompleteFirstRunExperience();
            run=Restore(run);
            Assert.That(run.IsInAcademyLayer,Is.True);
            Assert.That(run.Gold,Is.EqualTo(13));
            Assert.That(run.StageContribution,Is.EqualTo(4));
            Assert.That(run.RogueRunState.MasteredSpellIds,Is.EqualTo(mastered));
        }
        private static void Event(RogueliteMapRun run,string node,string choice)
        {run.SelectNode(node);run.ChooseCurrentNodeContent(choice);}
        private static void Reward(RogueliteMapRun run,string node,string choice)
        {
            run.SelectNode(node);run.CompleteCurrentCombat();run.ClaimReward(choice);
            Assert.That(run.AwaitingReward,Is.False,node);
            Assert.That(Restore(run).Gold,Is.EqualTo(run.Gold));
        }
        private static RogueliteMapRun Restore(RogueliteMapRun run)=>
            RogueliteMapRun.FromRogue11(Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(run.RogueRunState)));
    }
}
