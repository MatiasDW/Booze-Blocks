using BoozeBlocks.Prototype;
using NUnit.Framework;

namespace BoozeBlocks.Tests
{
    public sealed class RunLaunchOptionsTests
    {
        [SetUp]
        public void ResetOptions()
        {
            RunLaunchOptions.ConsumeSeed();
            RunLaunchOptions.ConsumeAutoStart();
        }

        [Test]
        public void PreparedSeedAndAutoStartAreConsumedOnce()
        {
            RunLaunchOptions.Prepare(4815, true);

            Assert.That(RunLaunchOptions.ConsumeSeed(), Is.EqualTo(4815));
            Assert.That(RunLaunchOptions.ConsumeSeed(), Is.Zero);
            Assert.That(RunLaunchOptions.ConsumeAutoStart(), Is.True);
            Assert.That(RunLaunchOptions.ConsumeAutoStart(), Is.False);
        }
    }
}
