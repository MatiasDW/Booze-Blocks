using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using BoozeBlocks.Distractions;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using BoozeBlocks.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BoozeBlocks.Tests
{
    public sealed class PrototypeRuntimeTests
    {
        [UnityTest]
        public IEnumerator BootstrapCreatesSurvivalLoopWithoutRuntimeErrors()
        {
            GameObject bootstrapObject = new GameObject("Runtime Test Bootstrap");
            bootstrapObject.AddComponent<PrototypeBootstrap>();

            yield return null;

            PrototypeStartMenu startMenu = Object.FindAnyObjectByType<PrototypeStartMenu>();
            Assert.That(startMenu, Is.Not.Null);
            Assert.That(startMenu.IsStarted, Is.False);
            PlayerInventory inventory = Object.FindAnyObjectByType<PlayerInventory>();
            Assert.That(inventory, Is.Not.Null);
            HordeDirector horde = Object.FindAnyObjectByType<HordeDirector>();
            Assert.That(horde, Is.Not.Null);
            Assert.That(horde.enabled, Is.False);

            PlayerAppearance appearance = inventory.GetComponent<PlayerAppearance>();
            string initialAppearance = appearance.CurrentDescription;
            appearance.CyclePreset();
            Assert.That(appearance.CurrentDescription, Is.Not.EqualTo(initialAppearance));
            Transform accessories = inventory.transform.Find("Wobbly Adult Visual/Accessories");
            Assert.That(accessories, Is.Not.Null);
            Assert.That(accessories.Find("Moustache").gameObject.activeSelf, Is.True);
            Assert.That(accessories.Find("Party Hat").gameObject.activeSelf, Is.True);
            startMenu.StartGame();
            Assert.That(startMenu.IsStarted, Is.True);
            Assert.That(horde.enabled, Is.True);
            yield return new WaitForSeconds(1.5f);

            PlayerDefenseController defense = Object.FindAnyObjectByType<PlayerDefenseController>();
            Assert.That(defense, Is.Not.Null);
            DrinkPreparationStation station = Object.FindAnyObjectByType<DrinkPreparationStation>();
            Assert.That(station, Is.Not.Null);
            DefensePickup[] pickups = Object.FindObjectsByType<DefensePickup>();
            Assert.That(pickups.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(horde.ActiveUnitCount, Is.GreaterThan(0));

            PlayerActionFeedback feedback = inventory.GetComponent<PlayerActionFeedback>();
            Assert.That(defense.TryUseEquippedItem(), Is.False);
            StringAssert.Contains("No tienes defensa", feedback.CurrentMessage);
            inventory.GetComponent<PlayerVitals>().RefillBuzz(100f);
            Assert.That(inventory.TryDrink(), Is.False);
            StringAssert.Contains("BUZZ lleno", feedback.CurrentMessage);
            inventory.GetComponent<PlayerVitals>().Model.Tick(1f, 10f, 0f, 0f);
            Assert.That(inventory.TryDrink(), Is.True);
            StringAssert.Contains("Bebiste booze", feedback.CurrentMessage);

            Renderer adultRenderer = inventory.GetComponentsInChildren<Renderer>(true)
                .First(renderer => renderer.gameObject.activeInHierarchy && renderer.enabled &&
                                   renderer.sharedMaterial.shader.name == "BoozeBlocks/StylizedCharacter");
            KidUnit kid = Object.FindAnyObjectByType<KidUnit>();
            Renderer kidRenderer = kid.GetComponentInChildren<Renderer>(true);
            Assert.That(adultRenderer.bounds.size.y, Is.GreaterThan(0.5f));
            Assert.That(kidRenderer.bounds.size.y, Is.GreaterThan(0.3f));
            Assert.That(adultRenderer.bounds.min.y, Is.GreaterThan(-0.3f));
            Assert.That(kidRenderer.bounds.min.y, Is.GreaterThan(-0.2f));
            Assert.That(adultRenderer.sharedMaterial.shader.isSupported, Is.True);
            Assert.That(kidRenderer.sharedMaterial.shader.isSupported, Is.True);
            Assert.That(GameObject.Find("BOOZE LAB - Estacion de mezcla"), Is.Not.Null);
            Assert.That(GameObject.Find("Booze Bottle 1"), Is.Not.Null);
            HordeEntrance[] entrances = Object.FindObjectsByType<HordeEntrance>();
            Assert.That(entrances.Length, Is.EqualTo(4));
            Assert.That(HordeEntranceRegistry.OpenCount, Is.EqualTo(4));
            entrances[0].Interact(inventory.GetComponent<PlayerVitals>());
            Assert.That(entrances[0].IsBlocked, Is.True);
            Assert.That(HordeEntranceRegistry.OpenCount, Is.EqualTo(3));

            CaptureFrameWhenRequested();
            yield return CaptureStationFrameWhenRequested(station);

            AttractionSource balloons = Object.FindObjectsByType<AttractionSource>()
                .First(source => source.name == "Balloon Bundle");
            inventory.transform.position = balloons.transform.position + Vector3.left * 1.5f;
            Physics.SyncTransforms();
            PlayerInteraction playerInteraction = inventory.GetComponent<PlayerInteraction>();
            Assert.That(playerInteraction.TryInteract(), Is.True);
            Assert.That(balloons.IsActive, Is.True);
            StringAssert.Contains("Soltar globos", feedback.CurrentMessage);

            for (int i = 1; i < entrances.Length; i++)
            {
                entrances[i].Interact(inventory.GetComponent<PlayerVitals>());
            }
            Assert.That(HordeEntranceRegistry.HasEntrances, Is.True);
            Assert.That(HordeEntranceRegistry.OpenCount, Is.Zero);
            Assert.That(HordeEntranceRegistry.TryGetSpawnPoint(42, out _), Is.False);

            station.Configure(0.1f, 4);
            station.Interact(inventory.GetComponent<PlayerVitals>());
            Assert.That(station.State, Is.EqualTo(DrinkStationState.Preparing));
            yield return new WaitForSeconds(0.15f);
            Assert.That(station.State, Is.EqualTo(DrinkStationState.Ready));
            station.Interact(inventory.GetComponent<PlayerVitals>());
            Assert.That(inventory.Model.DrinkServings, Is.EqualTo(inventory.Model.DrinkCapacity));

            pickups[0].Interact(inventory.GetComponent<PlayerVitals>());
            Assert.That(inventory.Model.DefenseItem, Is.Not.EqualTo(DefenseItemType.None));
            Assert.That(defense.TryUseEquippedItem(), Is.True);
            AttractionSource truck = Object.FindObjectsByType<AttractionSource>()
                .First(source => source.name == "Ice Cream Truck");
            Assert.That(truck.IsInteractionEnabled, Is.False);

            Transform visualRoot = inventory.transform.Find("Wobbly Adult Visual");
            Transform leftArm = visualRoot.Find("Arm Pivot L");
            Transform rightArm = visualRoot.Find("Arm Pivot R");
            Transform leftLeg = visualRoot.Find("Leg Pivot L");
            Transform rightLeg = visualRoot.Find("Leg Pivot R");
            Assert.That(leftArm, Is.Not.Null);
            Assert.That(rightArm, Is.Not.Null);
            Assert.That(leftLeg, Is.Not.Null);
            Assert.That(rightLeg, Is.Not.Null);

            Rigidbody playerBody = inventory.GetComponent<Rigidbody>();
            playerBody.angularVelocity = Vector3.up * 12f;
            yield return new WaitForFixedUpdate();
            Assert.That(Mathf.Abs(playerBody.angularVelocity.y), Is.LessThan(0.1f));
            playerBody.linearVelocity = inventory.transform.forward * 4f;
            yield return null;
            Assert.That(Quaternion.Angle(Quaternion.identity, leftLeg.localRotation), Is.GreaterThan(1f));
            Assert.That(Quaternion.Angle(leftLeg.localRotation, rightLeg.localRotation), Is.GreaterThan(1f));

            inventory.GetComponent<PlayerVitals>().ApplyPressure(200f);
            yield return new WaitForSeconds(0.2f);
            Assert.That(inventory.GetComponent<PlayerStateMachine>().State, Is.EqualTo(PlayerState.KnockedDown));
            Assert.That(Quaternion.Angle(Quaternion.identity, visualRoot.localRotation), Is.GreaterThan(15f));
        }

        private static void CaptureFrameWhenRequested()
        {
            string path = System.Environment.GetEnvironmentVariable("BOOZE_CAPTURE_PATH");
            CaptureFrame(path);
        }

        private static IEnumerator CaptureStationFrameWhenRequested(DrinkPreparationStation station)
        {
            string path = System.Environment.GetEnvironmentVariable("BOOZE_STATION_CAPTURE_PATH");
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            if (string.IsNullOrWhiteSpace(path) || camera == null) yield break;

            BoozeBlocks.Camera.ThirdPersonCamera controller =
                camera.GetComponent<BoozeBlocks.Camera.ThirdPersonCamera>();
            if (controller != null) controller.enabled = false;
            camera.transform.position = station.transform.position + new Vector3(0f, 4.8f, -8f);
            camera.transform.LookAt(station.transform.position + Vector3.up * 2f);
            yield return null;
            CaptureFrame(path);
        }

        private static void CaptureFrame(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || UnityEngine.Camera.main == null) return;

            RenderTexture target = new RenderTexture(1280, 720, 24);
            Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            UnityEngine.Camera.main.targetTexture = target;
            UnityEngine.Camera.main.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            WritePpm(path, image);
            UnityEngine.Camera.main.targetTexture = null;
            RenderTexture.active = previous;
            Object.Destroy(target);
            Object.Destroy(image);
        }

        private static void WritePpm(string path, Texture2D image)
        {
            Color32[] pixels = image.GetPixels32();
            using FileStream stream = File.Create(path);
            using BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes($"P6\n{image.width} {image.height}\n255\n"));
            for (int y = image.height - 1; y >= 0; y--)
            {
                int row = y * image.width;
                for (int x = 0; x < image.width; x++)
                {
                    Color32 color = pixels[row + x];
                    writer.Write(color.r);
                    writer.Write(color.g);
                    writer.Write(color.b);
                }
            }
        }
    }
}
