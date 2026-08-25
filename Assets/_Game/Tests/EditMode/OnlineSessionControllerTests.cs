using BoozeBlocks.Prototype;
using NUnit.Framework;

namespace BoozeBlocks.Tests.EditMode
{
    public sealed class OnlineSessionControllerTests
    {
        [Test]
        public void NormalizeJoinCode_RemovesWhitespaceAndUsesUppercase()
        {
            Assert.That(OnlineSessionController.NormalizeJoinCode(null), Is.Empty);
            Assert.That(OnlineSessionController.NormalizeJoinCode(string.Empty), Is.Empty);
            Assert.That(OnlineSessionController.NormalizeJoinCode("  ab c12  "), Is.EqualTo("ABC12"));
            Assert.That(OnlineSessionController.NormalizeJoinCode("relay99"), Is.EqualTo("RELAY99"));
        }
    }
}
