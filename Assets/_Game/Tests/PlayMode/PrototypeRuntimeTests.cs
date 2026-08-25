using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BoozeBlocks.Distractions;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using BoozeBlocks.Prototype;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace BoozeBlocks.Tests
{
    public sealed class PrototypeRuntimeTests
    {
        [UnitySetUp]
        public IEnumerator ClearPreviousPrototypeRuntime()
        {
            Time.timeScale = 1f;
            NetworkManager[] managers = Object.FindObjectsByType<NetworkManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i].IsListening) managers[i].Shutdown();
            }

            HashSet<GameObject> roots = new HashSet<GameObject>();
            AddRoots(Object.FindObjectsByType<PrototypeBootstrap>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), roots);
            AddRoots(Object.FindObjectsByType<NetworkGameplayCoordinator>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), roots);
            AddRoots(Object.FindObjectsByType<PlayerVitals>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), roots);
            AddRoots(Object.FindObjectsByType<BoozeBlocks.Camera.ThirdPersonCamera>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), roots);
            AddRoots(Object.FindObjectsByType<HordeEntrance>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), roots);
            AddRoots(Object.FindObjectsByType<AttractionSource>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), roots);
            AddRoots(Object.FindObjectsByType<DrinkPreparationStation>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), roots);
            AddRoots(Object.FindObjectsByType<GrillTaskStation>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), roots);
            foreach (GameObject root in roots)
            {
                if (root != null) Object.Destroy(root);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator BootstrapCreatesSurvivalLoopWithoutRuntimeErrors()
        {
            GameObject bootstrapObject = new GameObject("Runtime Test Bootstrap");
            bootstrapObject.AddComponent<PrototypeBootstrap>();

            yield return null;

            PrototypeStartMenu startMenu = Object.FindAnyObjectByType<PrototypeStartMenu>();
            Assert.That(startMenu, Is.Not.Null);
            Assert.That(startMenu.IsStarted, Is.False);
            Assert.That(startMenu.CurrentPage, Is.EqualTo(StartMenuPage.Main));
            OnlineSessionController onlineSession = Object.FindAnyObjectByType<OnlineSessionController>();
            Assert.That(onlineSession, Is.Not.Null);
            Assert.That(onlineSession.HasActiveSession, Is.False);
            NetworkGameplayCoordinator networkGameplay = Object.FindAnyObjectByType<NetworkGameplayCoordinator>();
            Assert.That(networkGameplay, Is.Not.Null);
            Assert.That(networkGameplay.IsNetworkReady, Is.False);
            startMenu.OpenPage(StartMenuPage.Customization);
            Assert.That(startMenu.CurrentPage, Is.EqualTo(StartMenuPage.Customization));
            startMenu.OpenPage(StartMenuPage.Options);
            Assert.That(startMenu.CurrentPage, Is.EqualTo(StartMenuPage.Options));
            startMenu.OpenPage(StartMenuPage.Main);
            PlayerInventory inventory = Object.FindAnyObjectByType<PlayerInventory>();
            Assert.That(inventory, Is.Not.Null);
            Assert.That(inventory.GetComponent<PlayerVitals>().Model.MaxHealth, Is.EqualTo(150f));
            HordeDirector horde = Object.FindAnyObjectByType<HordeDirector>();
            Assert.That(horde, Is.Not.Null);
            Assert.That(horde.HasSimulationAuthority, Is.True);
            Assert.That(horde.enabled, Is.False);
            Assert.That(horde.ActiveUnitCount, Is.Zero);
            Assert.That(Time.timeScale, Is.Zero);

            PlayerAppearance appearance = inventory.GetComponent<PlayerAppearance>();
            string initialAppearance = appearance.CurrentDescription;
            appearance.CyclePreset();
            Assert.That(appearance.CurrentDescription, Is.Not.EqualTo(initialAppearance));
            Transform accessories = inventory.transform.Find("Wobbly Adult Visual/Accessories");
            Assert.That(accessories, Is.Not.Null);
            Assert.That(accessories.Find("Moustache").gameObject.activeSelf, Is.True);
            Assert.That(accessories.Find("Party Hat").gameObject.activeSelf, Is.True);
            Assert.That(accessories.Find("Beard"), Is.Not.Null);
            Assert.That(accessories.Find("Glasses"), Is.Not.Null);
            Assert.That(accessories.Find("Chef Hat"), Is.Not.Null);
            Assert.That(accessories.Find("Beer Helmet"), Is.Not.Null);
            appearance.CyclePants(1);
            appearance.CycleHair(1);
            appearance.CycleFacialHair(1);
            appearance.ToggleGlasses();
            Assert.That(appearance.HasGlasses, Is.True);
            GameSettingsController settings = Object.FindAnyObjectByType<GameSettingsController>();
            Assert.That(settings, Is.Not.Null);
            float originalSensitivity = settings.MouseSensitivity;
            float originalMasterVolume = settings.MasterVolume;
            settings.SetMouseSensitivity(0.18f);
            settings.SetMasterVolume(0.75f);
            Assert.That(settings.MouseSensitivity, Is.EqualTo(0.18f).Within(0.001f));
            Assert.That(settings.MasterVolume, Is.EqualTo(0.75f).Within(0.001f));
            settings.SetMouseSensitivity(originalSensitivity);
            settings.SetMasterVolume(originalMasterVolume);
            startMenu.StartGame();
            Assert.That(startMenu.IsStarted, Is.True);
            Assert.That(horde.enabled, Is.True);
            PrototypePauseMenu pauseMenu = Object.FindAnyObjectByType<PrototypePauseMenu>();
            Assert.That(pauseMenu, Is.Not.Null);
            PrototypeHud hud = Object.FindAnyObjectByType<PrototypeHud>();
            Assert.That(hud, Is.Not.Null);
            pauseMenu.Pause();
            Assert.That(pauseMenu.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(hud.IsSuppressed, Is.True);
            pauseMenu.Resume();
            Assert.That(pauseMenu.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(hud.IsSuppressed, Is.False);
            yield return null;
            Assert.That(horde.IsPreparing, Is.True);
            Assert.That(horde.ActiveUnitCount, Is.Zero);
            Assert.That(horde.SkipPreparation(), Is.True);
            yield return new WaitForSeconds(1.5f);

            PlayerDefenseController defense = Object.FindAnyObjectByType<PlayerDefenseController>();
            Assert.That(defense, Is.Not.Null);
            DrinkPreparationStation station = Object.FindAnyObjectByType<DrinkPreparationStation>();
            Assert.That(station, Is.Not.Null);
            GrillTaskStation grill = Object.FindAnyObjectByType<GrillTaskStation>();
            Assert.That(grill, Is.Not.Null);
            RunSessionDirector session = Object.FindAnyObjectByType<RunSessionDirector>();
            Assert.That(session, Is.Not.Null);
            Assert.That(session.enabled, Is.True);
            Assert.That(Object.FindAnyObjectByType<ContextObjectiveTracker>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<PrototypeEventFeedback>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<RoundResultsScreen>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<RuntimePerformanceMonitor>(), Is.Not.Null);
            PrototypeVfxDirector vfxDirector = Object.FindAnyObjectByType<PrototypeVfxDirector>();
            Assert.That(vfxDirector, Is.Not.Null);
            Assert.That(vfxDirector.MaximumParticles, Is.EqualTo(180));
            Assert.That(Object.FindAnyObjectByType<PlayerReviveTarget>(), Is.Not.Null);
            PrototypeAudioDirector audioDirector = Object.FindAnyObjectByType<PrototypeAudioDirector>();
            Assert.That(audioDirector, Is.Not.Null);
            Assert.That(audioDirector.GeneratedClipCount, Is.GreaterThanOrEqualTo(10));
            DefensePickup[] pickups = Object.FindObjectsByType<DefensePickup>();
            Assert.That(pickups.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(pickups.All(pickup => pickup.Uses >= 16), Is.True);
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
            PlayerDrinkAnimator drinkAnimator = inventory.GetComponent<PlayerDrinkAnimator>();
            Assert.That(drinkAnimator, Is.Not.Null);
            Assert.That(drinkAnimator.IsPlaying, Is.True);
            Assert.That(drinkAnimator.Bottle.name, Is.EqualTo("Animated BOOZE Bottle"));
            Assert.That(drinkAnimator.Bottle.GetComponentsInChildren<TextMesh>(true)
                .Any(label => label.text == "BOOZE"), Is.True);
            yield return CaptureDrinkFrameWhenRequested(inventory.transform);

            Renderer adultRenderer = inventory.GetComponentsInChildren<Renderer>(true)
                .First(renderer => renderer.gameObject.activeInHierarchy && renderer.enabled &&
                                   renderer.sharedMaterial.shader.name == "BoozeBlocks/StylizedCharacter");
            KidUnit kid = Object.FindAnyObjectByType<KidUnit>();
            Assert.That(kid.CurrentHealth, Is.EqualTo(3f));
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
            PlayerVitals localVitals = inventory.GetComponent<PlayerVitals>();
            entrances[0].SetConstructionDuration(0.1f);
            inventory.transform.position = entrances[0].transform.position + Vector3.back;
            Physics.SyncTransforms();
            entrances[0].Interact(localVitals);
            Assert.That(entrances[0].IsBuilding, Is.True);
            Assert.That(entrances[0].IsBlocked, Is.False);
            Assert.That(HordeEntranceRegistry.OpenCount, Is.EqualTo(4));
            yield return new WaitForSeconds(0.12f);
            Assert.That(entrances[0].IsBlocked, Is.True);
            StringAssert.Contains("Entrada bloqueada", feedback.CurrentMessage);
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
                if (i == 1)
                {
                    entrances[i].SetConstructionDuration(0.3f);
                    inventory.transform.position = entrances[i].transform.position + Vector3.back;
                    Physics.SyncTransforms();
                    entrances[i].Interact(localVitals);
                    Assert.That(entrances[i].IsBuilding, Is.True);
                    inventory.transform.position = Vector3.zero;
                    Physics.SyncTransforms();
                    yield return null;
                    Assert.That(entrances[i].IsBuilding, Is.False);
                    Assert.That(entrances[i].IsBlocked, Is.False);
                    StringAssert.Contains("Construccion cancelada", feedback.CurrentMessage);
                }
                entrances[i].SetConstructionDuration(0.1f);
                inventory.transform.position = entrances[i].transform.position + Vector3.back;
                Physics.SyncTransforms();
                entrances[i].Interact(localVitals);
                yield return new WaitForSeconds(0.12f);
                Assert.That(entrances[i].IsBlocked, Is.True);
            }
            Assert.That(HordeEntranceRegistry.HasEntrances, Is.True);
            Assert.That(HordeEntranceRegistry.OpenCount, Is.Zero);
            Assert.That(HordeEntranceRegistry.TotalFlow, Is.Zero);
            Assert.That(HordeEntranceRegistry.TryGetSpawnPoint(42, out _), Is.False);
            Assert.That(entrances.All(entrance => entrance.IntegrityRatio > 0.95f), Is.True);
            Assert.That(entrances.All(entrance =>
                entrance.transform.Find("Barricade Integrity Back").gameObject.activeSelf), Is.True);

            station.Configure(0.1f, 4);
            station.Interact(inventory.GetComponent<PlayerVitals>());
            Assert.That(station.State, Is.EqualTo(DrinkStationState.Preparing));
            yield return new WaitForSeconds(0.15f);
            Assert.That(station.State, Is.EqualTo(DrinkStationState.Ready));
            station.Interact(inventory.GetComponent<PlayerVitals>());
            Assert.That(inventory.Model.DrinkServings, Is.EqualTo(inventory.Model.DrinkCapacity));

            int scoreBeforeGrill = session.Score.Score;
            grill.Configure(0.1f, 0.1f, 0.5f,
                grill.transform.Find("Hot Coals").GetComponent<Renderer>(),
                new[]
                {
                    grill.transform.Find("Steak L").GetComponent<Renderer>(),
                    grill.transform.Find("Steak R").GetComponent<Renderer>()
                });
            grill.Interact(inventory.GetComponent<PlayerVitals>());
            yield return new WaitForSeconds(0.15f);
            Assert.That(grill.State, Is.EqualTo(GrillTaskState.ReadyToCook));
            grill.Interact(inventory.GetComponent<PlayerVitals>());
            yield return new WaitForSeconds(0.15f);
            Assert.That(grill.State, Is.EqualTo(GrillTaskState.ReadyToServe));
            localVitals.ApplyDamage(50f);
            float healthBeforeServing = localVitals.Model.Health;
            grill.Interact(inventory.GetComponent<PlayerVitals>());
            Assert.That(session.Score.Score, Is.EqualTo(scoreBeforeGrill + 250));
            Assert.That(localVitals.Model.Health,
                Is.EqualTo(healthBeforeServing + GrillTaskStation.ServingHealthRecovery).Within(0.001f));

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
            Assert.That(playerBody.constraints, Is.EqualTo(RigidbodyConstraints.None));
            Assert.That(Quaternion.Angle(Quaternion.identity, visualRoot.localRotation), Is.GreaterThan(15f));
            horde.enabled = false;
            HordeEntrance damagedEntrance = entrances[0];
            int sectionsBeforeDamage = damagedEntrance.RemainingSections;
            damagedEntrance.ApplyHordeAttack(
                damagedEntrance.MaximumIntegrity / sectionsBeforeDamage + 1f);
            Assert.That(damagedEntrance.RemainingSections, Is.LessThan(sectionsBeforeDamage));
            Assert.That(damagedEntrance.FlowRatio, Is.GreaterThan(0f).And.LessThan(1f));
            int partialPasses = Enumerable.Range(0, 60)
                .Count(sequence => damagedEntrance.TryResolveSpawn(sequence, 0f));
            Assert.That(partialPasses, Is.GreaterThan(0).And.LessThan(60));
            yield return CaptureBarrierFrameWhenRequested(damagedEntrance);
            damagedEntrance.ApplyHordeAttack(damagedEntrance.MaximumIntegrity);
            Assert.That(damagedEntrance.IsBlocked, Is.False);
            Assert.That(damagedEntrance.FlowRatio, Is.EqualTo(1f));
            Assert.That(HordeEntranceRegistry.OpenCount, Is.EqualTo(1));
            yield return new WaitForSeconds(1.7f);
            Assert.That(inventory.GetComponent<PlayerStateMachine>().State, Is.EqualTo(PlayerState.Normal));
            Assert.That(playerBody.constraints,
                Is.EqualTo(RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ));

            PrototypeRound round = Object.FindAnyObjectByType<PrototypeRound>();
            RoundResultsScreen results = Object.FindAnyObjectByType<RoundResultsScreen>();
            round.EndRound(true);
            yield return null;
            Assert.That(round.State, Is.EqualTo(PrototypeRoundState.Won));
            Assert.That(results.IsVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator KnockedDownPlayerCanBeRescuedByTeammate()
        {
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            targetObject.name = "Revive Test Target";
            PlayerStateMachine state = targetObject.AddComponent<PlayerStateMachine>();
            PlayerVitals target = targetObject.GetComponent<PlayerVitals>();
            PlayerReviveTarget revive = targetObject.AddComponent<PlayerReviveTarget>();

            GameObject rescuerObject = new GameObject("Revive Test Rescuer");
            PlayerVitals rescuer = rescuerObject.AddComponent<PlayerVitals>();
            bool eventRaised = false;
            revive.Revived += () => eventRaised = true;
            yield return null;

            target.ApplyPressure(200f);
            Assert.That(state.State, Is.EqualTo(PlayerState.KnockedDown));
            Assert.That(revive.CanInteract(rescuer), Is.True);
            revive.Interact(rescuer);

            Assert.That(eventRaised, Is.True);
            Assert.That(state.State, Is.EqualTo(PlayerState.Normal));
            Assert.That(revive.CanInteract(rescuer), Is.False);

            Object.Destroy(targetObject);
            Object.Destroy(rescuerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LocalHost_SerializesAuthoritativeWorldWithoutErrors()
        {
            GameObject bootstrapObject = new GameObject("Local Host Test Bootstrap");
            bootstrapObject.AddComponent<PrototypeBootstrap>();
            yield return null;

            NetworkManager networkManager = Object.FindAnyObjectByType<NetworkManager>();
            NetworkGameplayCoordinator coordinator = Object.FindAnyObjectByType<NetworkGameplayCoordinator>();
            Assert.That(networkManager, Is.Not.Null);
            Assert.That(coordinator, Is.Not.Null);
            UnityTransport transport = networkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", 0, "127.0.0.1");
            Assert.That(networkManager.StartHost(), Is.True);

            float timeout = Time.realtimeSinceStartup + 3f;
            while (!coordinator.IsNetworkReady && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(coordinator.IsNetworkReady, Is.True);
            Assert.That(coordinator.CanHostStart, Is.True);
            coordinator.StartOnlineMatch();
            Assert.That(coordinator.IsMatchStarted, Is.True);
            HordeDirector horde = Object.FindAnyObjectByType<HordeDirector>();
            float preparationTimeout = Time.realtimeSinceStartup + 1f;
            while (!horde.IsPreparing && Time.realtimeSinceStartup < preparationTimeout) yield return null;
            Assert.That(horde.IsPreparing, Is.True,
                $"enabled={horde.enabled}, authority={horde.HasSimulationAuthority}, " +
                $"active={horde.IsWaveActive}, remaining={horde.WaveRemainingTime:0.00}, " +
                $"units={horde.ActiveUnitCount}/{horde.WaveUnitLimit}");
            Assert.That(horde.SkipPreparation(), Is.True);
            float populationTimeout = Time.realtimeSinceStartup + 1.5f;
            while (horde.ActiveUnitCount == 0 && Time.realtimeSinceStartup < populationTimeout) yield return null;

            Assert.That(horde.ActiveUnitCount, Is.GreaterThan(0));
            networkManager.Shutdown();
            yield return null;
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

        private static IEnumerator CaptureDrinkFrameWhenRequested(Transform player)
        {
            string path = System.Environment.GetEnvironmentVariable("BOOZE_DRINK_CAPTURE_PATH");
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            if (string.IsNullOrWhiteSpace(path) || camera == null || player == null) yield break;

            BoozeBlocks.Camera.ThirdPersonCamera controller =
                camera.GetComponent<BoozeBlocks.Camera.ThirdPersonCamera>();
            if (controller != null) controller.enabled = false;
            camera.transform.position = player.position + player.forward * 4f + Vector3.up * 1.9f +
                                        player.right * 0.35f;
            camera.transform.LookAt(player.position + Vector3.up * 0.75f);
            yield return new WaitForSeconds(0.40f);
            CaptureFrame(path);
        }

        private static IEnumerator CaptureBarrierFrameWhenRequested(HordeEntrance entrance)
        {
            string path = System.Environment.GetEnvironmentVariable("BOOZE_BARRIER_CAPTURE_PATH");
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            if (string.IsNullOrWhiteSpace(path) || camera == null || entrance == null) yield break;

            BoozeBlocks.Camera.ThirdPersonCamera controller =
                camera.GetComponent<BoozeBlocks.Camera.ThirdPersonCamera>();
            if (controller != null) controller.enabled = false;
            camera.transform.position = entrance.transform.position - entrance.transform.forward * 6f +
                                        Vector3.up * 3.2f;
            camera.transform.LookAt(entrance.transform.position + Vector3.up * 1.4f);
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

        private static void AddRoots<T>(T[] components, HashSet<GameObject> roots) where T : Component
        {
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null) roots.Add(components[i].transform.root.gameObject);
            }
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
