using BoozeBlocks.Interaction;
using NUnit.Framework;

namespace BoozeBlocks.Tests
{
    public sealed class GrillTaskModelTests
    {
        [Test]
        public void FullCycle_RequiresFuelCookingAndServing()
        {
            GrillTaskModel model = new GrillTaskModel(5f, 8f, 6f);

            Assert.That(model.TryInteract(), Is.True);
            Assert.That(model.State, Is.EqualTo(GrillTaskState.Heating));
            model.Tick(5f);
            Assert.That(model.State, Is.EqualTo(GrillTaskState.ReadyToCook));

            Assert.That(model.TryInteract(), Is.True);
            model.Tick(8f);
            Assert.That(model.State, Is.EqualTo(GrillTaskState.ReadyToServe));
            Assert.That(model.TryInteract(), Is.True);
            Assert.That(model.State, Is.EqualTo(GrillTaskState.ReadyToCook));
        }

        [Test]
        public void ReadyFood_BurnsWhenTeamIgnoresIt()
        {
            GrillTaskModel model = new GrillTaskModel(1f, 1f, 2f);

            model.TryInteract();
            model.Tick(1f);
            model.TryInteract();
            model.Tick(1f);
            model.Tick(2f);

            Assert.That(model.State, Is.EqualTo(GrillTaskState.Burned));
            Assert.That(model.TryInteract(), Is.True);
            Assert.That(model.State, Is.EqualTo(GrillTaskState.ReadyToCook));
        }

        [Test]
        public void BusyStates_RejectExtraInteractions()
        {
            GrillTaskModel model = new GrillTaskModel(5f, 8f, 6f);

            model.TryInteract();

            Assert.That(model.TryInteract(), Is.False);
            Assert.That(model.State, Is.EqualTo(GrillTaskState.Heating));
        }
    }
}
