using System;
using System.Linq;
using BoozeBlocks.Prototype;
using NUnit.Framework;

namespace BoozeBlocks.Tests
{
    public sealed class RunVariantModelTests
    {
        [Test]
        public void UpgradeChoices_AreDeterministicAndUnique()
        {
            RunUpgradeType[] first = RunVariantModels.CreateUpgradeChoices(1234, 2);
            RunUpgradeType[] second = RunVariantModels.CreateUpgradeChoices(1234, 2);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first.Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void CrisisSelection_AlwaysReturnsKnownCrisis()
        {
            for (int seed = -20; seed <= 20; seed++)
            {
                Assert.That(Enum.IsDefined(typeof(RunCrisisType), RunVariantModels.SelectCrisis(seed)), Is.True);
            }
        }

        [Test]
        public void PartyScore_TracksCooperativeActionsAndPenalty()
        {
            PartyScoreModel score = new PartyScoreModel();

            score.AddServing();
            score.AddBarricade();
            score.AddDistraction();
            score.AddWaveBonus(2);
            score.AddBurnedMeal();
            score.AddRescue();

            Assert.That(score.Score, Is.EqualTo(575));
            Assert.That(score.Servings, Is.EqualTo(1));
            Assert.That(score.Barricades, Is.EqualTo(1));
            Assert.That(score.Distractions, Is.EqualTo(1));
            Assert.That(score.BurnedMeals, Is.EqualTo(1));
            Assert.That(score.Rescues, Is.EqualTo(1));
        }
    }
}
