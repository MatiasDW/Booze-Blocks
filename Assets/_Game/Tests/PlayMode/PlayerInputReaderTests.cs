using System.Collections;
using BoozeBlocks.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace BoozeBlocks.Tests
{
    public sealed class PlayerInputReaderTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator KeyboardBindingsRecognizeInteractDefenseAndDrink()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            GameObject player = new GameObject("Input Test Player");
            PlayerInputReader input = player.AddComponent<PlayerInputReader>();

            Press(keyboard.eKey);
            yield return null;
            Assert.That(input.InteractPressed, Is.True);
            Release(keyboard.eKey);
            yield return null;

            Press(keyboard.fKey);
            yield return null;
            Assert.That(input.UseItemPressed, Is.True);
            Release(keyboard.fKey);
            yield return null;

            Press(keyboard.qKey);
            yield return null;
            Assert.That(input.DrinkPressed, Is.True);

            Object.Destroy(player);
        }
    }
}
