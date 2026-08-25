using System.Collections.Generic;
using BoozeBlocks.Camera;
using BoozeBlocks.Distractions;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Rendering;

namespace BoozeBlocks.Prototype
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private const int ArenaObstacleLayer = 7;

        [SerializeField, Range(1, 8)] private int simulatedPlayerCount = 1;

        private readonly List<Material> runtimeMaterials = new List<Material>(16);
        private Material floorMaterial;
        private Material fenceMaterial;
        private Material woodMaterial;
        private Material darkMaterial;
        private Material playerMaterial;
        private Material skinMaterial;
        private Material pantsMaterial;
        private Material whiteMaterial;
        private Material drinkMaterial;
        private Material attractionMaterial;
        private Material yellowMaterial;
        private Material truckMaterial;
        private Material glassMaterial;
        private Material hedgeMaterial;
        private Material patioMaterial;
        private Material characterMaterial;

        private void Awake()
        {
            if (FindAnyObjectByType<PlayerVitals>() != null) return;

            simulatedPlayerCount = RunLaunchOptions.ResolveSimulatedPlayerCount(simulatedPlayerCount);
            CreateMaterials();
            BuildLighting();
            GrillTaskStation grillStation = BuildArena();
            PlayerVitals player = null;
            for (int i = 0; i < simulatedPlayerCount; i++)
            {
                PlayerVitals createdPlayer = BuildPlayer(i, i == 0);
                if (i == 0) player = createdPlayer;
            }
            ThirdPersonCamera cameraController = BuildCamera(player.transform);
            AttractionSource truck = BuildInteractables(out DrinkPreparationStation drinkStation);

            GameObject systems = new GameObject("GameplaySystems");
            UnityTransport transport = systems.AddComponent<UnityTransport>();
            NetworkManager networkManager = systems.AddComponent<NetworkManager>();
            networkManager.NetworkConfig ??= new NetworkConfig();
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.EnableSceneManagement = false;
            networkManager.NetworkConfig.TickRate = 30;
            OnlineSessionController onlineSession = systems.AddComponent<OnlineSessionController>();
            HordeDirector horde = systems.AddComponent<HordeDirector>();

            PrototypeRound round = systems.AddComponent<PrototypeRound>();
            round.Configure(480f);

            RunProgression progression = systems.AddComponent<RunProgression>();
            progression.Configure(horde, truck, drinkStation);

            AttractionSource[] attractions = FindObjectsByType<AttractionSource>();
            HordeEntrance[] entrances = FindObjectsByType<HordeEntrance>();
            PlayerReviveTarget[] reviveTargets = FindObjectsByType<PlayerReviveTarget>();
            RunSessionDirector session = systems.AddComponent<RunSessionDirector>();
            session.Configure(horde, drinkStation, grillStation, attractions, RunLaunchOptions.ConsumeSeed());

            GameSettingsController settings = systems.AddComponent<GameSettingsController>();
            settings.Configure(cameraController);

            ContextObjectiveTracker objectives = systems.AddComponent<ContextObjectiveTracker>();
            objectives.Configure(player, player.GetComponent<PlayerInventory>(), drinkStation, grillStation, horde);

            PrototypeHud hud = systems.AddComponent<PrototypeHud>();
            hud.Configure(player, player.GetComponent<PlayerStateMachine>(),
                player.GetComponent<PlayerInteraction>(), round, horde,
                player.GetComponent<PlayerInventory>(), progression, drinkStation, grillStation, session, objectives);

            PrototypeAudioDirector audioDirector = systems.AddComponent<PrototypeAudioDirector>();
            audioDirector.Configure(settings, horde, grillStation, player, attractions, entrances,
                player.GetComponent<PlayerMotor>(), player.GetComponent<PlayerInventory>(),
                player.GetComponent<PlayerDefenseController>(), reviveTargets);

            PrototypeEventFeedback eventFeedback = systems.AddComponent<PrototypeEventFeedback>();
            eventFeedback.Configure(horde, grillStation, player, attractions, entrances, reviveTargets, settings);

            PrototypeVfxDirector vfxDirector = systems.AddComponent<PrototypeVfxDirector>();
            vfxDirector.Configure(cameraController, horde, grillStation, player,
                player.GetComponent<PlayerMotor>(), player.GetComponent<PlayerInventory>(),
                player.GetComponent<PlayerDefenseController>(), attractions, entrances, reviveTargets);

            RoundResultsScreen results = systems.AddComponent<RoundResultsScreen>();

            systems.AddComponent<RuntimePerformanceMonitor>();

            PrototypeStartMenu startMenu = systems.AddComponent<PrototypeStartMenu>();
            startMenu.Configure(player, cameraController, horde, round, progression, session, hud, settings,
                onlineSession, RunLaunchOptions.ConsumeAutoStart() || RunLaunchOptions.HasAutoStartArgument());

            NetworkGameplayCoordinator networkGameplay = systems.AddComponent<NetworkGameplayCoordinator>();
            networkGameplay.Configure(networkManager, this, player, horde, round, session, startMenu,
                drinkStation, grillStation, attractions, entrances, onlineSession);
            startMenu.SetNetworkGameplay(networkGameplay);
            results.Configure(round, session, horde, player.GetComponent<PlayerMotor>(), cameraController,
                networkGameplay, onlineSession);

            PrototypePauseMenu pauseMenu = systems.AddComponent<PrototypePauseMenu>();
            pauseMenu.Configure(startMenu, results, session, settings, player.GetComponent<PlayerMotor>(),
                player.GetComponent<PlayerStateMachine>(), cameraController, networkGameplay, onlineSession);
            hud.SetPauseMenu(pauseMenu);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimeMaterials.Count; i++)
            {
                if (runtimeMaterials[i] != null) Destroy(runtimeMaterials[i]);
            }
        }

        private void CreateMaterials()
        {
            floorMaterial = CreateMaterial(new Color(0.20f, 0.46f, 0.29f));
            fenceMaterial = CreateMaterial(new Color(0.88f, 0.68f, 0.38f));
            woodMaterial = CreateMaterial(new Color(0.43f, 0.22f, 0.12f));
            darkMaterial = CreateMaterial(new Color(0.10f, 0.13f, 0.16f));
            playerMaterial = CreateMaterial(new Color(0.10f, 0.50f, 0.72f));
            skinMaterial = CreateMaterial(new Color(1f, 0.70f, 0.50f));
            pantsMaterial = CreateMaterial(new Color(0.12f, 0.22f, 0.31f));
            whiteMaterial = CreateMaterial(new Color(0.94f, 0.91f, 0.79f));
            drinkMaterial = CreateMaterial(new Color(0.95f, 0.52f, 0.10f));
            attractionMaterial = CreateMaterial(new Color(0.90f, 0.25f, 0.28f));
            yellowMaterial = CreateMaterial(new Color(0.96f, 0.72f, 0.16f));
            truckMaterial = CreateMaterial(new Color(0.10f, 0.67f, 0.67f));
            glassMaterial = CreateMaterial(new Color(0.20f, 0.38f, 0.46f));
            hedgeMaterial = CreateMaterial(new Color(0.10f, 0.35f, 0.17f));
            patioMaterial = CreateMaterial(new Color(0.64f, 0.59f, 0.50f));
            Shader characterShader = Shader.Find("BoozeBlocks/StylizedCharacter");
            if (characterShader != null)
            {
                characterMaterial = new Material(characterShader) { enableInstancing = true };
                runtimeMaterials.Add(characterMaterial);
            }
        }

        private Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = color, enableInstancing = true };
            material.SetFloat("_Glossiness", 0.18f);
            material.SetFloat("_Metallic", 0f);
            runtimeMaterials.Add(material);
            return material;
        }

        private void BuildLighting()
        {
            GameObject lightObject = new GameObject("Warm Afternoon Sun");
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.90f, 0.76f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.54f, 0.68f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.53f, 0.49f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.30f, 0.24f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.68f, 0.80f, 0.84f);
            RenderSettings.fogDensity = 0.008f;

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material sky = new Material(skyShader);
                sky.SetColor("_SkyTint", new Color(0.48f, 0.70f, 0.82f));
                sky.SetColor("_GroundColor", new Color(0.34f, 0.48f, 0.34f));
                sky.SetFloat("_AtmosphereThickness", 0.85f);
                sky.SetFloat("_SunSize", 0.025f);
                sky.SetFloat("_Exposure", 1.05f);
                runtimeMaterials.Add(sky);
                RenderSettings.skybox = sky;
            }
        }

        private GrillTaskStation BuildArena()
        {
            GameObject environment = new GameObject("Backyard Party");
            CreatePart("Distant Lawn", PrimitiveType.Cube, environment.transform,
                new Vector3(0f, -0.72f, 0f), new Vector3(120f, 0.40f, 120f),
                floorMaterial, false);
            CreateBlock("Grass", new Vector3(0f, -0.5f, 0f), new Vector3(42f, 1f, 36f), floorMaterial, environment.transform);
            BuildFence(environment.transform);
            BuildHordeEntrances(environment.transform);
            BuildPatio(environment.transform);
            BuildPicnicTable(new Vector3(-6.5f, 0f, 5.2f), 12f, environment.transform);
            BuildPicnicTable(new Vector3(6.5f, 0f, -5.5f), -18f, environment.transform);
            BuildPicnicTable(new Vector3(13f, 0f, 8.5f), -32f, environment.transform);
            GrillTaskStation grill = BuildGrill(new Vector3(-11.5f, 0f, -11.5f), environment.transform);
            BuildCooler(new Vector3(16f, 0f, -11.5f), environment.transform);
            BuildObstacleLayout(environment.transform);
            BuildGardenDetails(environment.transform);
            BuildFoodTent(new Vector3(-14f, 0f, 7.5f), environment.transform);
            BuildBarCounter(new Vector3(14.5f, 0f, -6f), environment.transform);
            BuildBeerPongTable(new Vector3(7f, 0f, 10.5f), environment.transform);
            BuildAdultLounge(new Vector3(14.5f, 0f, 3.5f), environment.transform);
            BuildSpeakerStack(new Vector3(-8.5f, 0f, 12.5f), environment.transform);
            BuildGardenShed(new Vector3(-17.5f, 0f, 1f), environment.transform);
            BuildPartyLights(environment.transform);
            BuildUmbrella(new Vector3(-5.5f, 0f, 3.5f), environment.transform);
            BuildTree(new Vector3(-19f, 0f, 15.5f), environment.transform);
            BuildTree(new Vector3(18.5f, 0f, 14.5f), environment.transform);
            BuildTree(new Vector3(-19f, 0f, -15f), environment.transform);
            BuildTree(new Vector3(18.5f, 0f, -14.5f), environment.transform);
            for (int i = 0; i < 10; i++)
            {
                float wobble = Mathf.Sin(i * 1.4f);
                CreatePart($"Path Stone {i + 1}", PrimitiveType.Sphere, environment.transform,
                    new Vector3(wobble * 0.55f, 0.05f, 2f + i * 1.35f),
                    new Vector3(1.18f + (i % 3) * 0.10f, 0.10f, 0.72f + (i % 2) * 0.08f),
                    whiteMaterial, false, new Vector3(0f, wobble * 13f, 0f));
            }
            BuildAmbientDetails(environment.transform);
            return grill;
        }

        private void BuildFence(Transform parent)
        {
            BuildFenceSegment("Fence North L", new Vector3(-11.5f, 0.8f, 18f),
                new Vector3(19f, 1.6f, 0.35f), true, parent);
            BuildFenceSegment("Fence North R", new Vector3(11.5f, 0.8f, 18f),
                new Vector3(19f, 1.6f, 0.35f), true, parent);
            BuildFenceSegment("Fence South L", new Vector3(-11.5f, 0.8f, -18f),
                new Vector3(19f, 1.6f, 0.35f), true, parent);
            BuildFenceSegment("Fence South R", new Vector3(11.5f, 0.8f, -18f),
                new Vector3(19f, 1.6f, 0.35f), true, parent);
            BuildFenceSegment("Fence East S", new Vector3(21f, 0.8f, -10f),
                new Vector3(0.35f, 1.6f, 16f), false, parent);
            BuildFenceSegment("Fence East N", new Vector3(21f, 0.8f, 10f),
                new Vector3(0.35f, 1.6f, 16f), false, parent);
            BuildFenceSegment("Fence West S", new Vector3(-21f, 0.8f, -10f),
                new Vector3(0.35f, 1.6f, 16f), false, parent);
            BuildFenceSegment("Fence West N", new Vector3(-21f, 0.8f, 10f),
                new Vector3(0.35f, 1.6f, 16f), false, parent);
        }

        private void BuildFenceSegment(string name, Vector3 position, Vector3 size, bool horizontal,
            Transform parent)
        {
            GameObject segment = new GameObject(name);
            segment.transform.SetParent(parent);
            segment.transform.position = position;
            segment.layer = ArenaObstacleLayer;
            BoxCollider boundary = segment.AddComponent<BoxCollider>();
            boundary.size = size;

            Vector3 railScale = horizontal
                ? new Vector3(0.24f, size.x * 0.5f, 0.24f)
                : new Vector3(0.24f, size.z * 0.5f, 0.24f);
            Vector3 railRotation = horizontal ? new Vector3(0f, 0f, 90f) : new Vector3(90f, 0f, 0f);
            CreatePart("Lower Rail", PrimitiveType.Cylinder, segment.transform,
                new Vector3(0f, -0.38f, 0f), railScale, fenceMaterial, false, railRotation);
            CreatePart("Upper Rail", PrimitiveType.Cylinder, segment.transform,
                new Vector3(0f, 0.38f, 0f), railScale, fenceMaterial, false, railRotation);

            float length = horizontal ? size.x : size.z;
            int postCount = Mathf.Max(2, Mathf.CeilToInt(length / 3.8f) + 1);
            for (int i = 0; i < postCount; i++)
            {
                float offset = Mathf.Lerp(-length * 0.5f, length * 0.5f, i / (postCount - 1f));
                Vector3 localPosition = horizontal
                    ? new Vector3(offset, 0f, 0f)
                    : new Vector3(0f, 0f, offset);
                CreatePart($"Post {i + 1}", PrimitiveType.Capsule, segment.transform, localPosition,
                    new Vector3(0.34f, 0.98f, 0.34f), fenceMaterial, false);
            }
        }

        private void BuildHordeEntrances(Transform parent)
        {
            BuildHordeEntrance("ENTRADA NORTE", "pallets", new Vector3(0f, 0f, 17f), 180f, woodMaterial, parent);
            BuildHordeEntrance("ENTRADA SUR", "mesas plegables", new Vector3(0f, 0f, -17f), 0f, patioMaterial, parent);
            BuildHordeEntrance("ENTRADA ESTE", "coolers", new Vector3(20f, 0f, 0f), -90f, playerMaterial, parent);
            BuildHordeEntrance("ENTRADA OESTE", "basureros", new Vector3(-20f, 0f, 0f), 90f, darkMaterial, parent);
        }

        private void BuildHordeEntrance(string label, string method, Vector3 position, float yaw,
            Material barricadeMaterial, Transform parent)
        {
            GameObject entrance = new GameObject(label);
            entrance.transform.SetParent(parent);
            entrance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            SphereCollider interactionArea = entrance.AddComponent<SphereCollider>();
            interactionArea.radius = 2.2f;
            interactionArea.isTrigger = true;
            CreatePart("Gate Post L", PrimitiveType.Capsule, entrance.transform, new Vector3(-2f, 1.3f, 0f),
                new Vector3(0.38f, 1.3f, 0.38f), fenceMaterial, true);
            CreatePart("Gate Post R", PrimitiveType.Capsule, entrance.transform, new Vector3(2f, 1.3f, 0f),
                new Vector3(0.38f, 1.3f, 0.38f), fenceMaterial, true);
            CreatePart("Gate Header", PrimitiveType.Cube, entrance.transform, new Vector3(0f, 2.75f, 0f),
                new Vector3(2.8f, 0.38f, 0.22f), darkMaterial, false);
            CreateWorldLabel(label.Replace("ENTRADA ", string.Empty), entrance.transform,
                new Vector3(0f, 2.77f, -0.14f), 0.030f);

            GameObject barricade = new GameObject($"Barricada: {method}");
            barricade.transform.SetParent(entrance.transform, false);
            if (method == "coolers")
            {
                for (int i = 0; i < 3; i++)
                {
                    CreatePart($"Cooler {i + 1}", PrimitiveType.Cube, barricade.transform,
                        new Vector3(-1.2f + i * 1.2f, 0.65f, 0f), new Vector3(1.1f, 1.25f, 0.8f),
                        barricadeMaterial, false);
                }
            }
            else if (method == "basureros")
            {
                for (int i = 0; i < 3; i++)
                {
                    CreatePart($"Basurero {i + 1}", PrimitiveType.Cylinder, barricade.transform,
                        new Vector3(-1.15f + i * 1.15f, 0.75f, 0f), new Vector3(0.48f, 0.75f, 0.48f),
                        barricadeMaterial, false);
                }
            }
            else if (method == "mesas plegables")
            {
                CreatePart("Mesa bloqueo L", PrimitiveType.Cube, barricade.transform,
                    new Vector3(-0.9f, 0.85f, 0f), new Vector3(2.1f, 1.15f, 0.42f), barricadeMaterial, false,
                    new Vector3(0f, 0f, 9f));
                CreatePart("Mesa bloqueo R", PrimitiveType.Cube, barricade.transform,
                    new Vector3(0.9f, 0.85f, 0f), new Vector3(2.1f, 1.15f, 0.42f), barricadeMaterial, false,
                    new Vector3(0f, 0f, -9f));
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    CreatePart($"Pallet {i + 1}", PrimitiveType.Cube, barricade.transform,
                        new Vector3(0f, 0.55f + i * 0.48f, 0f), new Vector3(3.7f, 0.28f, 0.55f),
                        barricadeMaterial, false, new Vector3(0f, 0f, i % 2 == 0 ? 4f : -4f));
                }
            }
            GameObject integrityBack = CreatePart("Barricade Integrity Back", PrimitiveType.Cube,
                entrance.transform, new Vector3(0f, 2.35f, -0.16f), new Vector3(2.5f, 0.18f, 0.10f),
                darkMaterial, false);
            GameObject integrityFill = CreatePart("Barricade Integrity Fill", PrimitiveType.Cube,
                entrance.transform, new Vector3(0f, 2.35f, -0.22f), new Vector3(2.32f, 0.10f, 0.11f),
                truckMaterial, false);
            HordeEntrance component = entrance.AddComponent<HordeEntrance>();
            component.Configure(method, 14f, 9f, barricade);
            component.ConfigureIntegrityVisual(integrityBack, integrityFill);
        }

        private void BuildPatio(Transform parent)
        {
            CreatePart("Central Patio", PrimitiveType.Cube, parent, new Vector3(0f, 0.03f, 0f),
                new Vector3(10f, 0.06f, 8f), patioMaterial, false);
            for (int x = -2; x <= 2; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Material tile = (x + z) % 2 == 0 ? whiteMaterial : patioMaterial;
                    CreatePart($"Patio Tile {x}_{z}", PrimitiveType.Cube, parent,
                        new Vector3(x * 1.8f, 0.07f, z * 2f), new Vector3(1.65f, 0.04f, 1.85f), tile, false);
                }
            }
        }

        private void BuildObstacleLayout(Transform parent)
        {
            BuildHedge("West Hedge", new Vector3(-9.5f, 0.75f, -0.5f), new Vector3(1.2f, 1.5f, 8f), parent);
            BuildHedge("East Hedge", new Vector3(9.5f, 0.75f, 1.5f), new Vector3(1.2f, 1.5f, 8f), parent);
            BuildHedge("North West Hedge", new Vector3(-13.5f, 0.75f, 12f), new Vector3(7f, 1.5f, 1.2f), parent);
            BuildHedge("South East Hedge", new Vector3(13.5f, 0.75f, -13f), new Vector3(7f, 1.5f, 1.2f), parent);
            BuildCrateStack(new Vector3(-4.5f, 0f, -10f), parent);
            BuildCrateStack(new Vector3(4.5f, 0f, 10f), parent);
            BuildCrateStack(new Vector3(0f, 0f, -13.5f), parent);
        }

        private void BuildGardenDetails(Transform parent)
        {
            Vector3[] patches =
            {
                new Vector3(-17f, 0.22f, -6f), new Vector3(-15f, 0.22f, 14f),
                new Vector3(-11f, 0.22f, -15f), new Vector3(-5f, 0.22f, 15.5f),
                new Vector3(5f, 0.22f, -15.5f), new Vector3(11f, 0.22f, 15f),
                new Vector3(16.5f, 0.22f, 10f), new Vector3(17f, 0.22f, -7f)
            };
            for (int i = 0; i < patches.Length; i++)
            {
                GameObject patch = new GameObject($"Garden Patch {i + 1}");
                patch.transform.SetParent(parent);
                patch.transform.position = patches[i];
                patch.transform.rotation = Quaternion.Euler(0f, i * 37f, 0f);
                CreatePart("Leaf Cluster A", PrimitiveType.Sphere, patch.transform,
                    new Vector3(-0.35f, 0f, 0f), new Vector3(0.75f, 0.28f, 0.52f),
                    hedgeMaterial, false);
                CreatePart("Leaf Cluster B", PrimitiveType.Sphere, patch.transform,
                    new Vector3(0.35f, 0.04f, 0.10f), new Vector3(0.65f, 0.32f, 0.48f),
                    floorMaterial, false);
                Material flowerColor = i % 3 == 0 ? attractionMaterial : i % 3 == 1
                    ? yellowMaterial
                    : whiteMaterial;
                CreatePart("Flower", PrimitiveType.Sphere, patch.transform,
                    new Vector3(0f, 0.30f, -0.05f), Vector3.one * 0.16f, flowerColor, false);
            }

            BuildPlanter(new Vector3(-12.8f, 0f, -8.8f), parent);
            BuildPlanter(new Vector3(12.4f, 0f, 7.4f), parent);
        }

        private void BuildPlanter(Vector3 position, Transform parent)
        {
            GameObject planter = new GameObject("Party Planter");
            planter.transform.SetParent(parent);
            planter.transform.position = position;
            CreatePart("Pot", PrimitiveType.Cylinder, planter.transform,
                new Vector3(0f, 0.42f, 0f), new Vector3(0.58f, 0.42f, 0.58f),
                woodMaterial, false);
            CreatePart("Foliage L", PrimitiveType.Sphere, planter.transform,
                new Vector3(-0.32f, 1.05f, 0f), new Vector3(0.62f, 0.72f, 0.58f),
                hedgeMaterial, false);
            CreatePart("Foliage R", PrimitiveType.Sphere, planter.transform,
                new Vector3(0.32f, 1.10f, 0.05f), new Vector3(0.58f, 0.78f, 0.62f),
                floorMaterial, false);
        }

        private void BuildHedge(string name, Vector3 position, Vector3 scale, Transform parent)
        {
            GameObject hedge = new GameObject(name);
            hedge.transform.SetParent(parent);
            hedge.transform.position = position;
            hedge.layer = ArenaObstacleLayer;
            BoxCollider boundary = hedge.AddComponent<BoxCollider>();
            boundary.size = scale;

            bool alongX = scale.x > scale.z;
            float length = alongX ? scale.x : scale.z;
            int clusterCount = Mathf.Max(2, Mathf.CeilToInt(length / 1.65f));
            for (int i = 0; i < clusterCount; i++)
            {
                float offset = Mathf.Lerp(-length * 0.5f, length * 0.5f,
                    clusterCount == 1 ? 0.5f : i / (clusterCount - 1f));
                Vector3 localPosition = alongX
                    ? new Vector3(offset, Mathf.Sin(i * 1.7f) * 0.08f, 0f)
                    : new Vector3(0f, Mathf.Sin(i * 1.7f) * 0.08f, offset);
                Vector3 clusterScale = alongX
                    ? new Vector3(1.9f, scale.y * 0.92f, scale.z * 1.12f)
                    : new Vector3(scale.x * 1.12f, scale.y * 0.92f, 1.9f);
                CreatePart($"Leaf Cluster {i + 1}", PrimitiveType.Sphere, hedge.transform,
                    localPosition, clusterScale, i % 2 == 0 ? hedgeMaterial : floorMaterial, false);
            }
        }

        private void BuildCrateStack(Vector3 position, Transform parent)
        {
            GameObject stack = new GameObject("Party Supply Crates");
            stack.transform.SetParent(parent);
            stack.transform.position = position;
            CreatePart("Crate A", PrimitiveType.Cube, stack.transform, new Vector3(-0.6f, 0.55f, 0f), Vector3.one * 1.1f, woodMaterial, true);
            CreatePart("Crate B", PrimitiveType.Cube, stack.transform, new Vector3(0.6f, 0.55f, 0.1f), Vector3.one * 1.1f, woodMaterial, true);
            CreatePart("Crate C", PrimitiveType.Cube, stack.transform, new Vector3(0f, 1.65f, 0f), Vector3.one * 1.1f, yellowMaterial, true);
        }

        private void BuildFoodTent(Vector3 position, Transform parent)
        {
            GameObject tent = new GameObject("Food Tent");
            tent.transform.SetParent(parent);
            tent.transform.position = position;
            Vector3[] poles =
            {
                new Vector3(-2f, 1.5f, -1.5f), new Vector3(2f, 1.5f, -1.5f),
                new Vector3(-2f, 1.5f, 1.5f), new Vector3(2f, 1.5f, 1.5f)
            };
            for (int i = 0; i < poles.Length; i++)
            {
                CreatePart($"Tent Pole {i + 1}", PrimitiveType.Cylinder, tent.transform, poles[i],
                    new Vector3(0.11f, 1.5f, 0.11f), darkMaterial, true);
            }
            CreatePart("Striped Canopy", PrimitiveType.Cube, tent.transform, new Vector3(0f, 3.05f, 0f),
                new Vector3(4.8f, 0.18f, 3.8f), attractionMaterial, false, new Vector3(0f, 0f, 4f));
            CreatePart("Serving Counter", PrimitiveType.Cube, tent.transform, new Vector3(0f, 0.75f, 0f),
                new Vector3(3.4f, 1.5f, 1.1f), woodMaterial, true);
            CreatePart("Meat Tray", PrimitiveType.Cube, tent.transform, new Vector3(-0.85f, 1.58f, 0f),
                new Vector3(1.2f, 0.08f, 0.7f), darkMaterial, false);
            CreatePart("Bread Tray", PrimitiveType.Cube, tent.transform, new Vector3(0.85f, 1.58f, 0f),
                new Vector3(1.2f, 0.08f, 0.7f), yellowMaterial, false);
            for (int i = 0; i < 3; i++)
            {
                CreatePart($"Bread {i + 1}", PrimitiveType.Sphere, tent.transform,
                    new Vector3(0.55f + i * 0.30f, 1.72f, 0f), new Vector3(0.18f, 0.10f, 0.26f),
                    fenceMaterial, false);
            }
        }

        private void BuildBarCounter(Vector3 position, Transform parent)
        {
            GameObject bar = new GameObject("Adult Drinks Bar");
            bar.transform.SetParent(parent);
            bar.transform.position = position;
            CreatePart("Bar Counter", PrimitiveType.Cube, bar.transform, new Vector3(0f, 0.85f, 0f),
                new Vector3(5f, 1.7f, 1.25f), woodMaterial, true);
            CreatePart("Counter Top", PrimitiveType.Cube, bar.transform, new Vector3(0f, 1.78f, 0f),
                new Vector3(5.4f, 0.16f, 1.5f), darkMaterial, false);
            for (int i = 0; i < 5; i++)
            {
                float x = -1.5f + i * 0.75f;
                CreatePart($"Bar Bottle {i + 1}", PrimitiveType.Cylinder, bar.transform,
                    new Vector3(x, 2.15f, 0f), new Vector3(0.16f, 0.38f, 0.16f),
                    i % 2 == 0 ? drinkMaterial : truckMaterial, false);
            }
            CreatePart("Tap Tower", PrimitiveType.Cylinder, bar.transform, new Vector3(1.85f, 2.2f, 0f),
                new Vector3(0.14f, 0.42f, 0.14f), yellowMaterial, false);
        }

        private void BuildBeerPongTable(Vector3 position, Transform parent)
        {
            GameObject table = new GameObject("Beer Pong Table");
            table.transform.SetParent(parent);
            table.transform.position = position;
            CreatePart("Table", PrimitiveType.Cube, table.transform, new Vector3(0f, 0.9f, 0f),
                new Vector3(4.2f, 0.18f, 2f), playerMaterial, true);
            CreatePart("Leg L", PrimitiveType.Cube, table.transform, new Vector3(-1.5f, 0.42f, 0f),
                new Vector3(0.18f, 0.9f, 1.4f), darkMaterial, true);
            CreatePart("Leg R", PrimitiveType.Cube, table.transform, new Vector3(1.5f, 0.42f, 0f),
                new Vector3(0.18f, 0.9f, 1.4f), darkMaterial, true);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 3; row++)
                {
                    for (int cup = 0; cup <= row; cup++)
                    {
                        CreatePart($"Cup {side}_{row}_{cup}", PrimitiveType.Cylinder, table.transform,
                            new Vector3(side * (1.35f - row * 0.25f), 1.1f, (cup - row * 0.5f) * 0.32f),
                            new Vector3(0.12f, 0.18f, 0.12f), attractionMaterial, false);
                    }
                }
            }
        }

        private void BuildAdultLounge(Vector3 position, Transform parent)
        {
            GameObject lounge = new GameObject("Adult Lounge");
            lounge.transform.SetParent(parent);
            lounge.transform.position = position;
            CreatePart("Sofa Seat", PrimitiveType.Cube, lounge.transform, new Vector3(0f, 0.55f, 0f),
                new Vector3(4.2f, 0.7f, 1.4f), attractionMaterial, true);
            CreatePart("Sofa Back", PrimitiveType.Cube, lounge.transform, new Vector3(0f, 1.25f, 0.58f),
                new Vector3(4.2f, 1.1f, 0.28f), attractionMaterial, true);
            CreatePart("Coffee Table", PrimitiveType.Cube, lounge.transform, new Vector3(0f, 0.48f, -2f),
                new Vector3(2.8f, 0.22f, 1.5f), woodMaterial, true);
            CreatePart("Snack Bowl", PrimitiveType.Sphere, lounge.transform, new Vector3(0f, 0.75f, -2f),
                new Vector3(0.5f, 0.18f, 0.5f), yellowMaterial, false);
        }

        private void BuildSpeakerStack(Vector3 position, Transform parent)
        {
            GameObject sound = new GameObject("Party Sound System");
            sound.transform.SetParent(parent);
            sound.transform.position = position;
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.75f : 0.75f;
                CreatePart($"Speaker {i + 1}", PrimitiveType.Cube, sound.transform,
                    new Vector3(x, 1.15f, 0f), new Vector3(1.25f, 2.3f, 1f), darkMaterial, true);
                CreatePart($"Woofer {i + 1}", PrimitiveType.Cylinder, sound.transform,
                    new Vector3(x, 1.05f, -0.53f), new Vector3(0.38f, 0.08f, 0.38f),
                    playerMaterial, false, new Vector3(90f, 0f, 0f));
            }
        }

        private void BuildGardenShed(Vector3 position, Transform parent)
        {
            GameObject shed = CreateBlock("Garden Shed", position + Vector3.up * 1.5f,
                new Vector3(4.5f, 3f, 5f), woodMaterial, parent);
            CreatePart("Shed Door", PrimitiveType.Cube, shed.transform, new Vector3(0f, -0.1f, -0.51f),
                new Vector3(0.45f, 0.75f, 0.04f), darkMaterial, false);
            CreatePart("Door Knob", PrimitiveType.Sphere, shed.transform,
                new Vector3(0.15f, -0.08f, -0.54f), Vector3.one * 0.035f, yellowMaterial, false);
            CreatePart("Window", PrimitiveType.Cube, shed.transform, new Vector3(-0.32f, 0.18f, -0.54f),
                new Vector3(0.22f, 0.25f, 0.025f), glassMaterial, false);
            CreatePart("Roof Left", PrimitiveType.Cube, shed.transform, new Vector3(-0.28f, 0.60f, 0f),
                new Vector3(0.66f, 0.12f, 1.15f), attractionMaterial, false,
                new Vector3(0f, 0f, -15f));
            CreatePart("Roof Right", PrimitiveType.Cube, shed.transform, new Vector3(0.28f, 0.60f, 0f),
                new Vector3(0.66f, 0.12f, 1.15f), attractionMaterial, false,
                new Vector3(0f, 0f, 15f));
        }

        private void BuildPicnicTable(Vector3 position, float yaw, Transform parent)
        {
            GameObject table = new GameObject("Picnic Table");
            table.transform.SetParent(parent);
            table.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            CreatePart("Table Top", PrimitiveType.Cube, table.transform, new Vector3(0f, 1f, 0f), new Vector3(3.8f, 0.18f, 1.35f), woodMaterial, true);
            CreatePart("Bench L", PrimitiveType.Cube, table.transform, new Vector3(0f, 0.58f, -1.05f), new Vector3(3.7f, 0.16f, 0.42f), woodMaterial, true);
            CreatePart("Bench R", PrimitiveType.Cube, table.transform, new Vector3(0f, 0.58f, 1.05f), new Vector3(3.7f, 0.16f, 0.42f), woodMaterial, true);
            CreatePart("Leg L", PrimitiveType.Cube, table.transform, new Vector3(-1.3f, 0.48f, 0f), new Vector3(0.18f, 1f, 1.8f), darkMaterial, true);
            CreatePart("Leg R", PrimitiveType.Cube, table.transform, new Vector3(1.3f, 0.48f, 0f), new Vector3(0.18f, 1f, 1.8f), darkMaterial, true);
        }

        private GrillTaskStation BuildGrill(Vector3 position, Transform parent)
        {
            GameObject grill = new GameObject("Barbecue Grill");
            grill.transform.SetParent(parent);
            grill.transform.position = position;
            CreatePart("Grill Body", PrimitiveType.Cube, grill.transform, new Vector3(0f, 1.1f, 0f), new Vector3(2.4f, 0.65f, 1.3f), darkMaterial, true);
            GameObject coals = CreatePart("Hot Coals", PrimitiveType.Cube, grill.transform,
                new Vector3(0f, 1.48f, 0f), new Vector3(2.05f, 0.08f, 1.05f), attractionMaterial, false);
            for (int i = 0; i < 7; i++)
            {
                CreatePart($"Grill Bar {i + 1}", PrimitiveType.Cube, grill.transform,
                    new Vector3(-0.88f + i * 0.29f, 1.58f, 0f), new Vector3(0.035f, 0.035f, 1.12f),
                    darkMaterial, false);
            }
            GameObject steakL = CreatePart("Steak L", PrimitiveType.Cube, grill.transform,
                new Vector3(-0.5f, 1.64f, 0f), new Vector3(0.65f, 0.07f, 0.45f), woodMaterial, false,
                new Vector3(0f, 14f, 0f));
            GameObject steakR = CreatePart("Steak R", PrimitiveType.Cube, grill.transform,
                new Vector3(0.5f, 1.64f, 0f), new Vector3(0.65f, 0.07f, 0.45f), woodMaterial, false,
                new Vector3(0f, -12f, 0f));
            CreatePart("Lid", PrimitiveType.Cube, grill.transform, new Vector3(0f, 1.85f, 0.45f), new Vector3(2.4f, 0.55f, 0.15f), darkMaterial, false);
            CreatePart("Lid Handle", PrimitiveType.Cylinder, grill.transform, new Vector3(0f, 2.12f, 0.42f),
                new Vector3(0.08f, 0.55f, 0.08f), whiteMaterial, false, new Vector3(0f, 0f, 90f));
            CreatePart("Thermometer", PrimitiveType.Sphere, grill.transform, new Vector3(0f, 1.92f, 0.32f),
                Vector3.one * 0.13f, whiteMaterial, false);
            CreatePart("Side Shelf", PrimitiveType.Cube, grill.transform, new Vector3(1.8f, 1.15f, 0f), new Vector3(1.2f, 0.12f, 1.1f), woodMaterial, false);
            CreatePart("Leg 1", PrimitiveType.Cylinder, grill.transform, new Vector3(-0.85f, 0.45f, 0f), new Vector3(0.13f, 0.55f, 0.13f), darkMaterial, false);
            CreatePart("Leg 2", PrimitiveType.Cylinder, grill.transform, new Vector3(0.85f, 0.45f, 0f), new Vector3(0.13f, 0.55f, 0.13f), darkMaterial, false);
            CreatePart("Wheel L", PrimitiveType.Cylinder, grill.transform, new Vector3(-0.85f, 0.18f, 0f),
                new Vector3(0.28f, 0.10f, 0.28f), darkMaterial, false, new Vector3(90f, 0f, 0f));
            CreatePart("Wheel R", PrimitiveType.Cylinder, grill.transform, new Vector3(0.85f, 0.18f, 0f),
                new Vector3(0.28f, 0.10f, 0.28f), darkMaterial, false, new Vector3(90f, 0f, 0f));
            CreatePart("Tongs", PrimitiveType.Cube, grill.transform, new Vector3(1.75f, 1.32f, 0f),
                new Vector3(0.10f, 0.04f, 0.85f), whiteMaterial, false, new Vector3(0f, 18f, 0f));
            CreatePart("Smoke A", PrimitiveType.Sphere, grill.transform, new Vector3(-0.25f, 2.35f, 0f),
                new Vector3(0.35f, 0.22f, 0.35f), whiteMaterial, false);
            CreatePart("Smoke B", PrimitiveType.Sphere, grill.transform, new Vector3(0.12f, 2.75f, 0f),
                new Vector3(0.48f, 0.28f, 0.48f), whiteMaterial, false);
            CreateWorldLabel("PARRILLA\nCOCINA Y SIRVE", grill.transform, new Vector3(0f, 3.35f, 0f), 0.044f);
            GrillTaskStation station = grill.AddComponent<GrillTaskStation>();
            station.Configure(5f, 8f, 6f, coals.GetComponent<Renderer>(),
                new[] { steakL.GetComponent<Renderer>(), steakR.GetComponent<Renderer>() });
            return station;
        }

        private void BuildCooler(Vector3 position, Transform parent)
        {
            GameObject cooler = CreateBlock("Drink Cooler", position + Vector3.up * 0.55f,
                new Vector3(2.2f, 1.1f, 1.45f), playerMaterial, parent);
            CreatePart("Cooler Lid", PrimitiveType.Cube, cooler.transform, new Vector3(0f, 0.58f, 0f), new Vector3(1.04f, 0.12f, 1.04f), whiteMaterial, false);
            CreatePart("Cooler Handle", PrimitiveType.Cube, cooler.transform, new Vector3(0f, 0f, 0.78f), new Vector3(0.8f, 0.12f, 0.1f), whiteMaterial, false);
            for (int i = 0; i < 3; i++)
            {
                CreatePart($"Cooler Can {i + 1}", PrimitiveType.Cylinder, cooler.transform,
                    new Vector3(-0.28f + i * 0.28f, 0.78f, 0f), new Vector3(0.09f, 0.22f, 0.09f),
                    i % 2 == 0 ? attractionMaterial : yellowMaterial, false);
            }
        }

        private void BuildPartyLights(Transform parent)
        {
            CreatePart("Light Pole L", PrimitiveType.Cylinder, parent, new Vector3(-16f, 2.2f, 14f), new Vector3(0.12f, 2.2f, 0.12f), darkMaterial, false);
            CreatePart("Light Pole R", PrimitiveType.Cylinder, parent, new Vector3(16f, 2.2f, 14f), new Vector3(0.12f, 2.2f, 0.12f), darkMaterial, false);
            Material[] colors = { attractionMaterial, yellowMaterial, playerMaterial, truckMaterial };
            for (int i = 0; i < 11; i++)
            {
                float x = Mathf.Lerp(-16f, 16f, i / 10f);
                float sag = Mathf.Abs(i - 5) * 0.08f;
                CreatePart($"Party Light {i + 1}", PrimitiveType.Sphere, parent,
                    new Vector3(x, 4.3f + sag, 14f), Vector3.one * 0.25f, colors[i % colors.Length], false);
            }
        }

        private void BuildUmbrella(Vector3 position, Transform parent)
        {
            GameObject umbrella = new GameObject("Patio Umbrella");
            umbrella.transform.SetParent(parent);
            umbrella.transform.position = position;
            CreatePart("Umbrella Pole", PrimitiveType.Cylinder, umbrella.transform,
                new Vector3(0f, 2.15f, 0f), new Vector3(0.10f, 2.15f, 0.10f), darkMaterial, false);
            CreatePart("Umbrella Canopy", PrimitiveType.Sphere, umbrella.transform,
                new Vector3(0f, 4.15f, 0f), new Vector3(2.4f, 0.28f, 2.4f), attractionMaterial, false);
            CreatePart("Canopy Top", PrimitiveType.Sphere, umbrella.transform,
                new Vector3(0f, 4.32f, 0f), Vector3.one * 0.18f, yellowMaterial, false);
        }

        private void BuildTree(Vector3 position, Transform parent)
        {
            GameObject tree = new GameObject("Stylized Tree");
            tree.transform.SetParent(parent);
            tree.transform.position = position;
            CreatePart("Trunk", PrimitiveType.Cylinder, tree.transform,
                new Vector3(0f, 1.8f, 0f), new Vector3(0.55f, 1.8f, 0.55f), woodMaterial, false);
            CreatePart("Crown A", PrimitiveType.Sphere, tree.transform,
                new Vector3(0f, 4.1f, 0f), new Vector3(2.2f, 2f, 2.2f), floorMaterial, false);
            CreatePart("Crown B", PrimitiveType.Sphere, tree.transform,
                new Vector3(-1.1f, 3.7f, 0.2f), new Vector3(1.45f, 1.35f, 1.45f), floorMaterial, false);
            CreatePart("Crown C", PrimitiveType.Sphere, tree.transform,
                new Vector3(1.05f, 3.8f, -0.25f), new Vector3(1.5f, 1.45f, 1.5f), floorMaterial, false);
        }

        private void BuildAmbientDetails(Transform parent)
        {
            GameObject ambient = new GameObject("Ambient Details");
            ambient.transform.SetParent(parent);

            Material[] bulbColors =
            {
                CreateMaterial(new Color(1f, 0.55f, 0.20f)),
                CreateMaterial(new Color(0.30f, 0.85f, 0.95f)),
                CreateMaterial(new Color(0.95f, 0.85f, 0.30f)),
                CreateMaterial(new Color(0.95f, 0.35f, 0.55f))
            };
            BuildStringLights(ambient.transform,
                new Vector3(-19f, 5.6f, 15f), new Vector3(18.5f, 5.6f, 14.5f), 12, 0.85f, bulbColors);
            BuildStringLights(ambient.transform,
                new Vector3(-19f, 5.4f, -15f), new Vector3(18.5f, 5.4f, -14.5f), 12, 0.75f, bulbColors);

            BuildBarrelStack(ambient.transform, new Vector3(15.8f, 0f, -8.6f));
            BuildBarrelStack(ambient.transform, new Vector3(-16.2f, 0f, -3.2f));

            Vector3[] canPositions =
            {
                new Vector3(13.2f, 0.10f, -4.6f),
                new Vector3(13.8f, 0.10f, -5.2f),
                new Vector3(-8.9f, 0.10f, 12.6f),
                new Vector3(-7.5f, 0.10f, 11.2f),
                new Vector3(6.4f, 0.10f, 9.2f),
                new Vector3(-3.2f, 0.10f, -7.8f),
                new Vector3(0.9f, 0.10f, -6.6f),
                new Vector3(4.8f, 0.10f, -9.4f),
                new Vector3(-13.1f, 0.10f, -9.6f),
                new Vector3(15.4f, 0.10f, 2.1f)
            };
            for (int i = 0; i < canPositions.Length; i++)
            {
                Material canMaterial = (i % 3) == 0 ? drinkMaterial
                    : (i % 3) == 1 ? truckMaterial
                    : yellowMaterial;
                BuildFallenCan(ambient.transform, canPositions[i], i * 47f, canMaterial);
            }

            BuildFoldingChair(ambient.transform, new Vector3(13.1f, 0f, 5.1f), 158f);
            BuildFoldingChair(ambient.transform, new Vector3(15.9f, 0f, 5.4f), -172f);

            BuildBucket(ambient.transform, new Vector3(16.6f, 0f, -10.8f));

            BuildDecoSign(ambient.transform, new Vector3(0f, 0f, 17.4f), 0f, "BOOZE PARTY");

            BuildLightPost(ambient.transform, new Vector3(-19.5f, 0f, 8.5f));
            BuildLightPost(ambient.transform, new Vector3(19.5f, 0f, 8.5f));
            BuildLightPost(ambient.transform, new Vector3(-19.5f, 0f, -8.5f));
            BuildLightPost(ambient.transform, new Vector3(19.5f, 0f, -8.5f));

            Color[] confettiColors =
            {
                new Color(0.95f, 0.35f, 0.45f),
                new Color(0.30f, 0.75f, 0.95f),
                new Color(0.96f, 0.72f, 0.16f),
                new Color(0.55f, 0.90f, 0.45f),
                new Color(0.90f, 0.55f, 0.90f)
            };
            Material[] confettiMaterials = new Material[confettiColors.Length];
            for (int i = 0; i < confettiMaterials.Length; i++)
            {
                confettiMaterials[i] = CreateMaterial(confettiColors[i]);
            }
            for (int i = 0; i < 24; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad;
                float radius = 2.5f + (i % 5) * 1.35f;
                Vector3 confPos = new Vector3(Mathf.Cos(angle) * radius, 0.045f, Mathf.Sin(angle) * radius + 2f);
                CreatePart($"Confetti {i + 1}", PrimitiveType.Cube, ambient.transform,
                    confPos, new Vector3(0.28f, 0.03f, 0.16f),
                    confettiMaterials[i % confettiMaterials.Length], false,
                    new Vector3(0f, i * 31f, 0f));
            }

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 basePos = i < 4
                    ? new Vector3(-19f + Mathf.Cos(angle) * 1.6f, 0.06f, 15.5f + Mathf.Sin(angle) * 1.6f)
                    : new Vector3(18.5f + Mathf.Cos(angle) * 1.6f, 0.06f, -14.5f + Mathf.Sin(angle) * 1.6f);
                CreatePart($"Deco Stone {i + 1}", PrimitiveType.Sphere, ambient.transform,
                    basePos, new Vector3(0.38f + (i % 3) * 0.08f, 0.14f, 0.32f + (i % 2) * 0.06f),
                    fenceMaterial, false, new Vector3(0f, i * 27f, 0f));
            }
        }

        private void BuildStringLights(Transform parent, Vector3 start, Vector3 end,
            int bulbCount, float sagAmount, Material[] bulbColors)
        {
            GameObject garland = new GameObject("Party Garland");
            garland.transform.SetParent(parent);
            garland.transform.position = (start + end) * 0.5f;
            for (int i = 0; i < bulbCount; i++)
            {
                float t = (i + 1f) / (bulbCount + 1f);
                Vector3 point = Vector3.Lerp(start, end, t);
                point.y -= Mathf.Sin(t * Mathf.PI) * sagAmount;
                Vector3 localPos = point - garland.transform.position;
                CreatePart($"Bulb {i + 1}", PrimitiveType.Sphere, garland.transform,
                    localPos + Vector3.down * 0.18f, Vector3.one * 0.16f,
                    bulbColors[i % bulbColors.Length], false);
                CreatePart($"Bulb Wire {i + 1}", PrimitiveType.Cylinder, garland.transform,
                    localPos + Vector3.down * 0.08f, new Vector3(0.03f, 0.10f, 0.03f),
                    darkMaterial, false);
            }
        }

        private void BuildBarrelStack(Transform parent, Vector3 position)
        {
            GameObject stack = new GameObject("Barrel Stack");
            stack.transform.SetParent(parent);
            stack.transform.position = position;
            CreatePart("Barrel Base L", PrimitiveType.Cylinder, stack.transform,
                new Vector3(-0.45f, 0.55f, 0f), new Vector3(0.62f, 0.55f, 0.62f), woodMaterial, false);
            CreatePart("Barrel Base R", PrimitiveType.Cylinder, stack.transform,
                new Vector3(0.45f, 0.55f, 0.10f), new Vector3(0.62f, 0.55f, 0.62f), woodMaterial, false);
            CreatePart("Barrel Top", PrimitiveType.Cylinder, stack.transform,
                new Vector3(0f, 1.55f, 0.05f), new Vector3(0.62f, 0.55f, 0.62f), drinkMaterial, false);
            CreatePart("Ring Base L", PrimitiveType.Cylinder, stack.transform,
                new Vector3(-0.45f, 0.55f, 0f), new Vector3(0.68f, 0.08f, 0.68f), darkMaterial, false);
            CreatePart("Ring Base R", PrimitiveType.Cylinder, stack.transform,
                new Vector3(0.45f, 0.55f, 0.10f), new Vector3(0.68f, 0.08f, 0.68f), darkMaterial, false);
            CreatePart("Ring Top", PrimitiveType.Cylinder, stack.transform,
                new Vector3(0f, 1.55f, 0.05f), new Vector3(0.68f, 0.08f, 0.68f), darkMaterial, false);
        }

        private void BuildFallenCan(Transform parent, Vector3 position, float yaw, Material canMaterial)
        {
            GameObject can = new GameObject("Fallen Can");
            can.transform.SetParent(parent);
            can.transform.position = position;
            can.transform.rotation = Quaternion.Euler(0f, yaw, 90f);
            CreatePart("Body", PrimitiveType.Cylinder, can.transform,
                Vector3.zero, new Vector3(0.14f, 0.13f, 0.14f), canMaterial, false);
            CreatePart("Rim Top", PrimitiveType.Cylinder, can.transform,
                new Vector3(0f, 0.13f, 0f), new Vector3(0.15f, 0.02f, 0.15f), darkMaterial, false);
            CreatePart("Rim Bottom", PrimitiveType.Cylinder, can.transform,
                new Vector3(0f, -0.13f, 0f), new Vector3(0.15f, 0.02f, 0.15f), darkMaterial, false);
        }

        private void BuildFoldingChair(Transform parent, Vector3 position, float yaw)
        {
            GameObject chair = new GameObject("Folding Chair");
            chair.transform.SetParent(parent);
            chair.transform.position = position;
            chair.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            CreatePart("Seat", PrimitiveType.Cube, chair.transform,
                new Vector3(0f, 0.42f, 0f), new Vector3(0.75f, 0.06f, 0.72f),
                attractionMaterial, false);
            CreatePart("Backrest", PrimitiveType.Cube, chair.transform,
                new Vector3(0f, 0.90f, -0.32f), new Vector3(0.75f, 0.90f, 0.06f),
                attractionMaterial, false);
            CreatePart("Leg FL", PrimitiveType.Cylinder, chair.transform,
                new Vector3(-0.32f, 0.20f, 0.32f), new Vector3(0.06f, 0.22f, 0.06f), darkMaterial, false);
            CreatePart("Leg FR", PrimitiveType.Cylinder, chair.transform,
                new Vector3(0.32f, 0.20f, 0.32f), new Vector3(0.06f, 0.22f, 0.06f), darkMaterial, false);
            CreatePart("Leg BL", PrimitiveType.Cylinder, chair.transform,
                new Vector3(-0.32f, 0.20f, -0.32f), new Vector3(0.06f, 0.22f, 0.06f), darkMaterial, false);
            CreatePart("Leg BR", PrimitiveType.Cylinder, chair.transform,
                new Vector3(0.32f, 0.20f, -0.32f), new Vector3(0.06f, 0.22f, 0.06f), darkMaterial, false);
        }

        private void BuildBucket(Transform parent, Vector3 position)
        {
            GameObject bucket = new GameObject("Bucket");
            bucket.transform.SetParent(parent);
            bucket.transform.position = position;
            CreatePart("Body", PrimitiveType.Cylinder, bucket.transform,
                new Vector3(0f, 0.28f, 0f), new Vector3(0.38f, 0.28f, 0.38f), fenceMaterial, false);
            CreatePart("Rim", PrimitiveType.Cylinder, bucket.transform,
                new Vector3(0f, 0.56f, 0f), new Vector3(0.42f, 0.03f, 0.42f), darkMaterial, false);
            CreatePart("Handle", PrimitiveType.Cylinder, bucket.transform,
                new Vector3(0f, 0.70f, 0f), new Vector3(0.05f, 0.28f, 0.42f), darkMaterial, false,
                new Vector3(90f, 0f, 0f));
        }

        private void BuildDecoSign(Transform parent, Vector3 position, float yaw, string _label)
        {
            GameObject sign = new GameObject("Deco Sign");
            sign.transform.SetParent(parent);
            sign.transform.position = position;
            sign.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            CreatePart("Post L", PrimitiveType.Cylinder, sign.transform,
                new Vector3(-1.35f, 1.35f, 0f), new Vector3(0.15f, 1.35f, 0.15f), woodMaterial, false);
            CreatePart("Post R", PrimitiveType.Cylinder, sign.transform,
                new Vector3(1.35f, 1.35f, 0f), new Vector3(0.15f, 1.35f, 0.15f), woodMaterial, false);
            CreatePart("Board", PrimitiveType.Cube, sign.transform,
                new Vector3(0f, 2.35f, 0f), new Vector3(3.4f, 0.95f, 0.14f), whiteMaterial, false);
            CreatePart("Board Trim Top", PrimitiveType.Cube, sign.transform,
                new Vector3(0f, 2.85f, 0f), new Vector3(3.5f, 0.10f, 0.20f), attractionMaterial, false);
            CreatePart("Board Trim Bottom", PrimitiveType.Cube, sign.transform,
                new Vector3(0f, 1.85f, 0f), new Vector3(3.5f, 0.10f, 0.20f), attractionMaterial, false);
            CreatePart("Letter B", PrimitiveType.Cube, sign.transform,
                new Vector3(-1.20f, 2.35f, -0.08f), new Vector3(0.30f, 0.55f, 0.05f), darkMaterial, false);
            CreatePart("Letter O1", PrimitiveType.Sphere, sign.transform,
                new Vector3(-0.72f, 2.35f, -0.08f), new Vector3(0.36f, 0.55f, 0.05f), darkMaterial, false);
            CreatePart("Letter O2", PrimitiveType.Sphere, sign.transform,
                new Vector3(-0.24f, 2.35f, -0.08f), new Vector3(0.36f, 0.55f, 0.05f), darkMaterial, false);
            CreatePart("Letter Z", PrimitiveType.Cube, sign.transform,
                new Vector3(0.24f, 2.35f, -0.08f), new Vector3(0.30f, 0.55f, 0.05f), darkMaterial, false,
                new Vector3(0f, 0f, 12f));
            CreatePart("Letter E", PrimitiveType.Cube, sign.transform,
                new Vector3(0.72f, 2.35f, -0.08f), new Vector3(0.30f, 0.55f, 0.05f), darkMaterial, false);
            CreatePart("Star", PrimitiveType.Sphere, sign.transform,
                new Vector3(1.30f, 2.55f, -0.08f), Vector3.one * 0.28f, yellowMaterial, false);
        }

        private void BuildLightPost(Transform parent, Vector3 position)
        {
            GameObject post = new GameObject("Warm Light Post");
            post.transform.SetParent(parent);
            post.transform.position = position;
            CreatePart("Pole", PrimitiveType.Cylinder, post.transform,
                new Vector3(0f, 1.6f, 0f), new Vector3(0.12f, 1.6f, 0.12f), darkMaterial, false);
            CreatePart("Head", PrimitiveType.Sphere, post.transform,
                new Vector3(0f, 3.35f, 0f), new Vector3(0.42f, 0.42f, 0.42f), yellowMaterial, false);
            CreatePart("Hood", PrimitiveType.Cylinder, post.transform,
                new Vector3(0f, 3.55f, 0f), new Vector3(0.34f, 0.10f, 0.34f), darkMaterial, false);
            GameObject lightObject = new GameObject("Point Light");
            lightObject.transform.SetParent(post.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 3.35f, 0f);
            Light warmLight = lightObject.AddComponent<Light>();
            warmLight.type = LightType.Point;
            warmLight.color = new Color(1f, 0.72f, 0.35f);
            warmLight.intensity = 0.85f;
            warmLight.range = 6.5f;
            warmLight.shadows = LightShadows.None;
        }

        private PlayerVitals BuildPlayer(int playerIndex, bool controllable)
        {
            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = controllable ? "Player_Adult_Local" : $"Player_Adult_Simulated_{playerIndex + 1}";
            float angle = playerIndex * Mathf.PI * 2f / Mathf.Max(1, simulatedPlayerCount);
            playerObject.transform.position = new Vector3(Mathf.Cos(angle) * 3f, 1.1f, Mathf.Sin(angle) * 3f);
            playerObject.GetComponent<Renderer>().enabled = false;

            Rigidbody body = playerObject.AddComponent<Rigidbody>();
            body.mass = 3f;
            body.linearDamping = 1.2f;
            body.angularDamping = 5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            BuildAdultVisual(playerObject.transform, playerIndex);

            PlayerInputReader input = playerObject.AddComponent<PlayerInputReader>();
            PlayerMotor motor = playerObject.AddComponent<PlayerMotor>();
            PlayerVitals vitals = playerObject.AddComponent<PlayerVitals>();
            playerObject.AddComponent<PlayerActionFeedback>();
            PlayerAppearance appearance = playerObject.AddComponent<PlayerAppearance>();
            appearance.Initialize(playerObject.transform.Find("Wobbly Adult Visual"), playerIndex);
            playerObject.AddComponent<PlayerInventory>();
            Transform visual = playerObject.transform.Find("Wobbly Adult Visual");
            PlayerDrinkAnimator drinkAnimator = playerObject.AddComponent<PlayerDrinkAnimator>();
            drinkAnimator.Initialize(BuildPlayerBoozeBottle(visual),
                visual != null ? visual.Find("Arm Pivot R") ?? visual.Find("Arm R") : null);
            playerObject.AddComponent<PlayerDefenseController>();
            playerObject.AddComponent<PlayerStateMachine>();
            playerObject.AddComponent<PlayerReviveTarget>();
            playerObject.AddComponent<BlobShadow>();
            if (controllable) playerObject.AddComponent<PlayerInteraction>();
            else
            {
                input.enabled = false;
                motor.SetControlEnabled(false);
            }
            return vitals;
        }

        private Transform BuildPlayerBoozeBottle(Transform visual)
        {
            if (visual == null) return null;
            GameObject bottle = new GameObject("Animated BOOZE Bottle");
            bottle.transform.SetParent(visual, false);
            bottle.transform.localPosition = new Vector3(0.76f, -0.25f, 0.36f);
            bottle.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            CreatePart("Bottle Body", PrimitiveType.Cylinder, bottle.transform, Vector3.zero,
                new Vector3(0.18f, 0.36f, 0.18f), drinkMaterial, false);
            CreatePart("Bottle Shoulder", PrimitiveType.Sphere, bottle.transform,
                new Vector3(0f, 0.34f, 0f), new Vector3(0.19f, 0.12f, 0.19f), drinkMaterial, false);
            CreatePart("Bottle Neck", PrimitiveType.Cylinder, bottle.transform,
                new Vector3(0f, 0.48f, 0f), new Vector3(0.09f, 0.14f, 0.09f), drinkMaterial, false);
            CreatePart("Bottle Cap", PrimitiveType.Cylinder, bottle.transform,
                new Vector3(0f, 0.64f, 0f), new Vector3(0.10f, 0.035f, 0.10f), darkMaterial, false);
            CreatePart("BOOZE Label", PrimitiveType.Cube, bottle.transform,
                new Vector3(0f, -0.02f, -0.185f), new Vector3(0.30f, 0.25f, 0.025f),
                whiteMaterial, false);
            CreateBottleText(bottle.transform);
            bottle.SetActive(false);
            return bottle.transform;
        }

        private static void CreateBottleText(Transform parent)
        {
            GameObject label = new GameObject("BOOZE Floating Text");
            label.transform.SetParent(parent, false);
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = "BOOZE";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.characterSize = 0.018f;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(0.12f, 0.10f, 0.08f);
        }

        public PlayerVitals CreateNetworkPlayer(ulong clientId, bool simulateOnThisPeer)
        {
            int visualIndex = (int)(clientId % 8UL) + 1;
            PlayerVitals vitals = BuildPlayer(visualIndex, false);
            GameObject playerObject = vitals.gameObject;
            playerObject.name = $"Player_Adult_Network_{clientId}";
            float spawnAngle = (clientId % 8UL) * Mathf.PI * 2f / 8f;
            playerObject.transform.position = new Vector3(Mathf.Cos(spawnAngle) * 3f, 1.1f,
                Mathf.Sin(spawnAngle) * 3f);
            PlayerInputReader input = playerObject.GetComponent<PlayerInputReader>();
            PlayerMotor motor = playerObject.GetComponent<PlayerMotor>();
            PlayerInventory inventory = playerObject.GetComponent<PlayerInventory>();
            PlayerDefenseController defense = playerObject.GetComponent<PlayerDefenseController>();
            PlayerStateMachine state = playerObject.GetComponent<PlayerStateMachine>();
            vitals.SetSimulationAuthority(simulateOnThisPeer);
            inventory.SetExecutionAuthority(simulateOnThisPeer);
            defense.SetExecutionAuthority(simulateOnThisPeer);
            state.SetSimulationAuthority(simulateOnThisPeer);

            if (simulateOnThisPeer)
            {
                input.enabled = true;
                input.SetRemoteWorldInput(Vector2.zero);
                motor.SetControlEnabled(true);
                PlayerInteraction remoteInteraction = playerObject.AddComponent<PlayerInteraction>();
                remoteInteraction.SetExecutionAuthority(true);
            }
            else
            {
                input.enabled = false;
                motor.enabled = false;
                Rigidbody body = playerObject.GetComponent<Rigidbody>();
                body.isKinematic = true;
                body.detectCollisions = false;
            }
            return vitals;
        }

        private void BuildAdultVisual(Transform player, int playerIndex)
        {
            GameObject visual = new GameObject("Wobbly Adult Visual");
            visual.transform.SetParent(player, false);
            BuildAdultAccessories(visual.transform);
            GameObject characterPrefab = Resources.Load<GameObject>("AdultCharacter");
            if (characterPrefab != null)
            {
                GameObject model = Instantiate(characterPrefab, visual.transform);
                model.name = "Low Poly Adult";
                model.transform.localPosition = Vector3.down * 1.1f;
                model.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                model.transform.localScale = Vector3.one * 0.90f;
                Collider[] modelColliders = model.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < modelColliders.Length; i++) modelColliders[i].enabled = false;
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    Destroy(model);
                }
                else
                {
                    Color clothingColor = playerIndex % 2 == 0
                        ? new Color(0.05f, 0.48f, 0.76f)
                        : new Color(0.93f, 0.20f, 0.26f);
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (characterMaterial != null) renderers[i].sharedMaterial = characterMaterial;
                        ApplyCharacterPalette(renderers[i], clothingColor);
                    }
                    BuildAnimatedAdultLimbs(visual.transform, playerIndex,
                        out Transform leftArm, out Transform rightArm,
                        out Transform leftLeg, out Transform rightLeg);
                    DrunkVisualAnimator modelWobble = player.gameObject.AddComponent<DrunkVisualAnimator>();
                    modelWobble.Initialize(visual.transform, playerIndex, leftArm, rightArm, leftLeg, rightLeg);
                    return;
                }
            }

            Material shirt = playerIndex % 2 == 0 ? playerMaterial : attractionMaterial;
            CreatePart("Torso", PrimitiveType.Capsule, visual.transform, Vector3.zero, new Vector3(0.95f, 0.82f, 0.82f), shirt, false);
            CreatePart("Belly", PrimitiveType.Sphere, visual.transform, new Vector3(0f, -0.1f, 0.28f), new Vector3(1.05f, 0.82f, 0.78f), shirt, false);
            CreatePart("Head", PrimitiveType.Sphere, visual.transform, new Vector3(0f, 1.15f, 0f), Vector3.one * 0.72f, skinMaterial, false);
            CreatePart("Nose", PrimitiveType.Sphere, visual.transform, new Vector3(0f, 1.12f, 0.58f), Vector3.one * 0.18f, skinMaterial, false);
            CreatePart("Hair", PrimitiveType.Sphere, visual.transform, new Vector3(0f, 1.49f, -0.02f), new Vector3(0.76f, 0.22f, 0.72f), darkMaterial, false);
            CreatePart("Arm L", PrimitiveType.Capsule, visual.transform, new Vector3(-0.78f, 0.15f, 0f), new Vector3(0.24f, 0.60f, 0.24f), shirt, false, new Vector3(0f, 0f, -15f));
            CreatePart("Arm R", PrimitiveType.Capsule, visual.transform, new Vector3(0.78f, 0.15f, 0f), new Vector3(0.24f, 0.60f, 0.24f), shirt, false, new Vector3(0f, 0f, 15f));
            CreatePart("Leg L", PrimitiveType.Cylinder, visual.transform, new Vector3(-0.32f, -1.0f, 0f), new Vector3(0.28f, 0.45f, 0.28f), pantsMaterial, false);
            CreatePart("Leg R", PrimitiveType.Cylinder, visual.transform, new Vector3(0.32f, -1.0f, 0f), new Vector3(0.28f, 0.45f, 0.28f), pantsMaterial, false);
            CreatePart("Shoe L", PrimitiveType.Cube, visual.transform, new Vector3(-0.32f, -1.43f, 0.18f), new Vector3(0.42f, 0.24f, 0.68f), darkMaterial, false);
            CreatePart("Shoe R", PrimitiveType.Cube, visual.transform, new Vector3(0.32f, -1.43f, 0.18f), new Vector3(0.42f, 0.24f, 0.68f), darkMaterial, false);
            DrunkVisualAnimator wobble = player.gameObject.AddComponent<DrunkVisualAnimator>();
            wobble.Initialize(visual.transform, playerIndex);
        }

        private void BuildAnimatedAdultLimbs(Transform parent, int playerIndex,
            out Transform leftArm, out Transform rightArm, out Transform leftLeg, out Transform rightLeg)
        {
            Material shirt = playerIndex % 2 == 0 ? playerMaterial : attractionMaterial;
            leftArm = CreateLimbPivot("Arm Pivot L", parent, new Vector3(-0.56f, 0.28f, 0f));
            rightArm = CreateLimbPivot("Arm Pivot R", parent, new Vector3(0.56f, 0.28f, 0f));
            leftLeg = CreateLimbPivot("Leg Pivot L", parent, new Vector3(-0.24f, -0.28f, 0f));
            rightLeg = CreateLimbPivot("Leg Pivot R", parent, new Vector3(0.24f, -0.28f, 0f));

            BuildArm(leftArm, shirt);
            BuildArm(rightArm, shirt);
            BuildLeg(leftLeg);
            BuildLeg(rightLeg);
        }

        private void BuildArm(Transform pivot, Material shirt)
        {
            CreatePart("Sleeve", PrimitiveType.Capsule, pivot, new Vector3(0f, -0.28f, 0f),
                new Vector3(0.18f, 0.34f, 0.18f), shirt, false);
            CreatePart("Hand", PrimitiveType.Sphere, pivot, new Vector3(0f, -0.67f, 0f),
                new Vector3(0.21f, 0.20f, 0.21f), skinMaterial, false);
        }

        private void BuildLeg(Transform pivot)
        {
            CreatePart("Shorts", PrimitiveType.Sphere, pivot, new Vector3(0f, -0.08f, 0f),
                new Vector3(0.27f, 0.29f, 0.28f), pantsMaterial, false);
            CreatePart("Leg", PrimitiveType.Cylinder, pivot, new Vector3(0f, -0.43f, 0f),
                new Vector3(0.14f, 0.30f, 0.14f), skinMaterial, false);
            CreatePart("Shoe", PrimitiveType.Cube, pivot, new Vector3(0f, -0.78f, 0.13f),
                new Vector3(0.30f, 0.16f, 0.52f), darkMaterial, false);
        }

        private static Transform CreateLimbPivot(string name, Transform parent, Vector3 localPosition)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPosition;
            return pivot.transform;
        }

        private void BuildAdultAccessories(Transform visual)
        {
            GameObject accessories = new GameObject("Accessories");
            accessories.transform.SetParent(visual, false);

            GameObject moustache = new GameObject("Moustache");
            moustache.transform.SetParent(accessories.transform, false);
            CreatePart("Moustache L", PrimitiveType.Cube, moustache.transform, new Vector3(-0.10f, 0.48f, 0.39f),
                new Vector3(0.22f, 0.07f, 0.07f), darkMaterial, false, new Vector3(0f, 0f, -14f));
            CreatePart("Moustache R", PrimitiveType.Cube, moustache.transform, new Vector3(0.10f, 0.48f, 0.39f),
                new Vector3(0.22f, 0.07f, 0.07f), darkMaterial, false, new Vector3(0f, 0f, 14f));

            GameObject beard = new GameObject("Beard");
            beard.transform.SetParent(accessories.transform, false);
            CreatePart("Beard Chin", PrimitiveType.Sphere, beard.transform, new Vector3(0f, 0.20f, 0.36f),
                new Vector3(0.38f, 0.28f, 0.16f), darkMaterial, false);
            CreatePart("Beard L", PrimitiveType.Cube, beard.transform, new Vector3(-0.27f, 0.29f, 0.34f),
                new Vector3(0.12f, 0.28f, 0.10f), darkMaterial, false, new Vector3(0f, 0f, -12f));
            CreatePart("Beard R", PrimitiveType.Cube, beard.transform, new Vector3(0.27f, 0.29f, 0.34f),
                new Vector3(0.12f, 0.28f, 0.10f), darkMaterial, false, new Vector3(0f, 0f, 12f));

            GameObject glasses = new GameObject("Glasses");
            glasses.transform.SetParent(accessories.transform, false);
            CreatePart("Glasses L", PrimitiveType.Cube, glasses.transform, new Vector3(-0.25f, 0.68f, 0.43f),
                new Vector3(0.34f, 0.18f, 0.06f), glassMaterial, false);
            CreatePart("Glasses R", PrimitiveType.Cube, glasses.transform, new Vector3(0.25f, 0.68f, 0.43f),
                new Vector3(0.34f, 0.18f, 0.06f), glassMaterial, false);
            CreatePart("Glasses Bridge", PrimitiveType.Cube, glasses.transform, new Vector3(0f, 0.68f, 0.43f),
                new Vector3(0.18f, 0.05f, 0.05f), darkMaterial, false);

            GameObject partyHat = new GameObject("Party Hat");
            partyHat.transform.SetParent(accessories.transform, false);
            CreatePart("Hat Color Party Base", PrimitiveType.Cylinder, partyHat.transform,
                new Vector3(0f, 1.11f, 0f), new Vector3(0.34f, 0.11f, 0.34f), attractionMaterial, false);
            CreatePart("Hat Color Party Middle", PrimitiveType.Cylinder, partyHat.transform,
                new Vector3(0f, 1.31f, 0f), new Vector3(0.24f, 0.10f, 0.24f), attractionMaterial, false);
            CreatePart("Hat Color Party Tip", PrimitiveType.Cylinder, partyHat.transform,
                new Vector3(0f, 1.49f, 0f), new Vector3(0.13f, 0.09f, 0.13f), attractionMaterial, false);
            CreatePart("Party Pom", PrimitiveType.Sphere, partyHat.transform, new Vector3(0f, 1.68f, 0f),
                Vector3.one * 0.11f, yellowMaterial, false);

            GameObject cowboy = new GameObject("Cowboy Hat");
            cowboy.transform.SetParent(accessories.transform, false);
            CreatePart("Cowboy Brim", PrimitiveType.Cylinder, cowboy.transform, new Vector3(0f, 1.05f, 0f),
                new Vector3(0.55f, 0.07f, 0.48f), woodMaterial, false);
            CreatePart("Hat Color Cowboy", PrimitiveType.Cylinder, cowboy.transform, new Vector3(0f, 1.28f, 0f),
                new Vector3(0.31f, 0.25f, 0.31f), woodMaterial, false);

            GameObject capObject = new GameObject("Cap");
            capObject.transform.SetParent(accessories.transform, false);
            CreatePart("Hat Color Cap", PrimitiveType.Sphere, capObject.transform, new Vector3(0f, 1.10f, 0f),
                new Vector3(0.42f, 0.20f, 0.40f), playerMaterial, false);
            CreatePart("Cap Visor", PrimitiveType.Cube, capObject.transform, new Vector3(0f, 1.02f, 0.40f),
                new Vector3(0.48f, 0.07f, 0.32f), playerMaterial, false);

            GameObject chef = new GameObject("Chef Hat");
            chef.transform.SetParent(accessories.transform, false);
            CreatePart("Chef Band", PrimitiveType.Cylinder, chef.transform, new Vector3(0f, 1.13f, 0f),
                new Vector3(0.40f, 0.18f, 0.40f), whiteMaterial, false);
            CreatePart("Chef Puff L", PrimitiveType.Sphere, chef.transform, new Vector3(-0.22f, 1.48f, 0f),
                Vector3.one * 0.38f, whiteMaterial, false);
            CreatePart("Chef Puff R", PrimitiveType.Sphere, chef.transform, new Vector3(0.22f, 1.48f, 0f),
                Vector3.one * 0.38f, whiteMaterial, false);
            CreatePart("Chef Puff Top", PrimitiveType.Sphere, chef.transform, new Vector3(0f, 1.67f, 0f),
                Vector3.one * 0.40f, whiteMaterial, false);

            GameObject beerHelmet = new GameObject("Beer Helmet");
            beerHelmet.transform.SetParent(accessories.transform, false);
            CreatePart("Hat Color Beer", PrimitiveType.Sphere, beerHelmet.transform, new Vector3(0f, 1.10f, 0f),
                new Vector3(0.45f, 0.22f, 0.42f), playerMaterial, false);
            CreatePart("Beer Can L", PrimitiveType.Cylinder, beerHelmet.transform, new Vector3(-0.48f, 1.42f, 0f),
                new Vector3(0.14f, 0.30f, 0.14f), attractionMaterial, false);
            CreatePart("Beer Can R", PrimitiveType.Cylinder, beerHelmet.transform, new Vector3(0.48f, 1.42f, 0f),
                new Vector3(0.14f, 0.30f, 0.14f), yellowMaterial, false);
            CreatePart("Beer Tube", PrimitiveType.Cube, beerHelmet.transform, new Vector3(0f, 1.35f, 0.35f),
                new Vector3(0.82f, 0.04f, 0.04f), whiteMaterial, false);
        }

        private static void ApplyCharacterPalette(Renderer renderer, Color shirtColor)
        {
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor("_ShirtColor", shirtColor);
            properties.SetColor("_SkinColor", new Color(1f, 0.68f, 0.46f));
            properties.SetColor("_PantsColor", new Color(0.08f, 0.15f, 0.24f));
            properties.SetColor("_DarkColor", new Color(0.05f, 0.03f, 0.02f));
            renderer.SetPropertyBlock(properties);
        }

        private static ThirdPersonCamera BuildCamera(Transform target)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 6f, -8f);
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            cameraObject.AddComponent<AudioListener>();
            ThirdPersonCamera controller = cameraObject.AddComponent<ThirdPersonCamera>();
            controller.SetTarget(target);
            return controller;
        }

        private AttractionSource BuildInteractables(out DrinkPreparationStation drinkStation)
        {
            GameObject interactables = new GameObject("Party Interactables");

            drinkStation = BuildDrinkStation(interactables.transform);
            BuildDefensePickup(interactables.transform, DefenseItemType.Broom, new Vector3(-8f, 0.8f, 7f));
            BuildDefensePickup(interactables.transform, DefenseItemType.FryingPan, new Vector3(9f, 0.8f, -6f));

            GameObject balloons = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            balloons.name = "Balloon Bundle";
            balloons.transform.SetParent(interactables.transform);
            balloons.transform.position = new Vector3(7f, 1.25f, 4f);
            balloons.transform.localScale = Vector3.one * 0.8f;
            balloons.GetComponent<Renderer>().sharedMaterial = attractionMaterial;
            CreatePart("Yellow Balloon", PrimitiveType.Sphere, balloons.transform, new Vector3(-0.75f, 0.7f, 0.1f), Vector3.one * 0.9f, yellowMaterial, false);
            CreatePart("Blue Balloon", PrimitiveType.Sphere, balloons.transform, new Vector3(0.75f, 0.55f, 0f), Vector3.one * 0.85f, playerMaterial, false);
            CreatePart("Teal Balloon", PrimitiveType.Sphere, balloons.transform, new Vector3(0f, 1.1f, -0.2f), Vector3.one * 0.8f, truckMaterial, false);
            AttractionSource balloonSource = balloons.AddComponent<AttractionSource>();
            balloonSource.Configure("E - Soltar globos", 15, 10f, 8f, 6, 2, 12f);

            GameObject truck = CreateBlock("Ice Cream Truck", new Vector3(8f, 1.35f, 15f),
                new Vector3(5.8f, 2.7f, 2.6f), truckMaterial, interactables.transform);
            BuildTruckDetails(truck.transform);
            AttractionSource truckSource = truck.AddComponent<AttractionSource>();
            truckSource.Configure("E - Activar camion de helados", 100, 40f, 18f, 100, 0, 25f);
            return truckSource;
        }

        private DrinkPreparationStation BuildDrinkStation(Transform parent)
        {
            GameObject station = new GameObject("BOOZE LAB - Estacion de mezcla");
            station.transform.SetParent(parent);
            station.transform.position = new Vector3(-17f, 0f, -10.5f);
            CreatePart("Meson Booze", PrimitiveType.Cube, station.transform, new Vector3(0f, 0.72f, 0f),
                new Vector3(4.2f, 1.45f, 2.2f), woodMaterial, true);
            CreatePart("Cubierta metalica", PrimitiveType.Cube, station.transform, new Vector3(0f, 1.5f, 0f),
                new Vector3(4.5f, 0.12f, 2.4f), darkMaterial, false);
            CreatePart("Barril de booze", PrimitiveType.Cylinder, station.transform, new Vector3(-1.05f, 2.15f, 0f),
                new Vector3(0.62f, 0.72f, 0.62f), drinkMaterial, false);
            CreatePart("Aro barril superior", PrimitiveType.Cylinder, station.transform, new Vector3(-1.05f, 2.84f, 0f),
                new Vector3(0.68f, 0.07f, 0.68f), whiteMaterial, false);
            CreatePart("Mezclador", PrimitiveType.Cylinder, station.transform, new Vector3(0.15f, 1.98f, 0f),
                new Vector3(0.50f, 0.48f, 0.50f), glassMaterial, false);
            CreatePart("Grifo", PrimitiveType.Cube, station.transform, new Vector3(0.70f, 1.95f, -0.25f),
                new Vector3(0.48f, 0.10f, 0.12f), whiteMaterial, false);
            GameObject indicator = CreatePart("Luz de estado", PrimitiveType.Sphere, station.transform,
                new Vector3(0.15f, 2.58f, 0f), Vector3.one * 0.20f, truckMaterial, false);
            BuildBoozeBottles(station.transform);
            CreatePart("Poste cartel L", PrimitiveType.Cylinder, station.transform, new Vector3(-1.65f, 2.75f, 0.6f),
                new Vector3(0.07f, 1.2f, 0.07f), darkMaterial, false);
            CreatePart("Poste cartel R", PrimitiveType.Cylinder, station.transform, new Vector3(1.65f, 2.75f, 0.6f),
                new Vector3(0.07f, 1.2f, 0.07f), darkMaterial, false);
            CreatePart("Cartel Booze", PrimitiveType.Cube, station.transform, new Vector3(0f, 3.55f, 0.6f),
                new Vector3(3.8f, 0.85f, 0.12f), attractionMaterial, false);
            CreateWorldLabel("BOOZE LAB\nPREPARA AQUI", station.transform, new Vector3(0f, 3.55f, 0.48f), 0.045f);
            CreatePart("Booze Beacon", PrimitiveType.Cylinder, station.transform, new Vector3(0f, 4.55f, 0f),
                new Vector3(0.28f, 0.58f, 0.28f), drinkMaterial, false);
            CreateWorldLabel("BOOZE", station.transform, new Vector3(0f, 5.35f, 0f), 0.064f);
            DrinkPreparationStation component = station.AddComponent<DrinkPreparationStation>();
            component.Configure(9f, Mathf.Clamp(simulatedPlayerCount + 2, 4, 10));
            component.ConfigureVisual(indicator.GetComponent<Renderer>());
            return component;
        }

        private void BuildBoozeBottles(Transform parent)
        {
            for (int i = 0; i < 4; i++)
            {
                float x = 0.95f + i * 0.38f;
                CreatePart($"Booze Bottle {i + 1}", PrimitiveType.Cylinder, parent,
                    new Vector3(x, 1.85f, 0f), new Vector3(0.13f, 0.34f, 0.13f),
                    i % 2 == 0 ? drinkMaterial : truckMaterial, false);
                CreatePart($"Bottle Neck {i + 1}", PrimitiveType.Cylinder, parent,
                    new Vector3(x, 2.18f, 0f), new Vector3(0.07f, 0.12f, 0.07f),
                    i % 2 == 0 ? drinkMaterial : truckMaterial, false);
                CreatePart($"Bottle Cap {i + 1}", PrimitiveType.Cylinder, parent,
                    new Vector3(x, 2.31f, 0f), new Vector3(0.08f, 0.04f, 0.08f), whiteMaterial, false);
            }
        }

        private static void CreateWorldLabel(string text, Transform parent, Vector3 localPosition, float size)
        {
            GameObject label = new GameObject("World Label");
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = size;
            textMesh.color = Color.white;
            label.AddComponent<WorldBillboard>();
        }

        private void BuildDefensePickup(Transform parent, DefenseItemType itemType, Vector3 position)
        {
            GameObject pickup = new GameObject(itemType == DefenseItemType.Broom ? "Escoba defensiva" : "Sarten defensivo");
            pickup.transform.SetParent(parent);
            pickup.transform.position = position;
            BoxCollider pickupCollider = pickup.AddComponent<BoxCollider>();
            pickupCollider.size = new Vector3(1.5f, 1.6f, 1.5f);

            if (itemType == DefenseItemType.Broom)
            {
                CreatePart("Mango", PrimitiveType.Cylinder, pickup.transform, Vector3.zero,
                    new Vector3(0.10f, 0.85f, 0.10f), woodMaterial, false, new Vector3(0f, 0f, -24f));
                CreatePart("Cepillo", PrimitiveType.Cube, pickup.transform, new Vector3(0.68f, -0.68f, 0f),
                    new Vector3(0.75f, 0.18f, 0.35f), yellowMaterial, false, new Vector3(0f, 0f, -24f));
            }
            else
            {
                CreatePart("Sarten", PrimitiveType.Cylinder, pickup.transform, Vector3.zero,
                    new Vector3(0.75f, 0.16f, 0.75f), darkMaterial, false, new Vector3(90f, 0f, 0f));
                CreatePart("Mango", PrimitiveType.Cube, pickup.transform, new Vector3(0f, 0f, -0.85f),
                    new Vector3(0.22f, 0.18f, 1.15f), darkMaterial, false);
            }

            DefensePickup defensePickup = pickup.AddComponent<DefensePickup>();
            defensePickup.Configure(itemType, itemType == DefenseItemType.Broom ? 24 : 16, 18f);
        }

        private void BuildTruckDetails(Transform truck)
        {
            CreatePart("White Roof", PrimitiveType.Cube, truck, new Vector3(0f, 0.56f, 0f), new Vector3(1.05f, 0.18f, 1.08f), whiteMaterial, false);
            CreatePart("Serving Window", PrimitiveType.Cube, truck, new Vector3(0.05f, 0.12f, -0.53f), new Vector3(0.48f, 0.48f, 0.04f), darkMaterial, false);
            CreatePart("Cab Window", PrimitiveType.Cube, truck, new Vector3(-0.36f, 0.12f, -0.53f), new Vector3(0.22f, 0.40f, 0.04f), glassMaterial, false);
            CreatePart("Stripe", PrimitiveType.Cube, truck, new Vector3(0f, -0.22f, -0.525f), new Vector3(1.02f, 0.12f, 0.05f), attractionMaterial, false);
            CreatePart("Ice Cream Sign", PrimitiveType.Sphere, truck, new Vector3(0.15f, 0.88f, 0f), new Vector3(0.26f, 0.55f, 0.26f), whiteMaterial, false);
            Vector3 wheelScale = new Vector3(0.38f, 0.15f, 0.38f);
            CreatePart("Wheel FL", PrimitiveType.Cylinder, truck, new Vector3(-0.32f, -0.52f, -0.52f), wheelScale, darkMaterial, false, new Vector3(90f, 0f, 0f));
            CreatePart("Wheel FR", PrimitiveType.Cylinder, truck, new Vector3(0.32f, -0.52f, -0.52f), wheelScale, darkMaterial, false, new Vector3(90f, 0f, 0f));
            CreatePart("Wheel BL", PrimitiveType.Cylinder, truck, new Vector3(-0.32f, -0.52f, 0.52f), wheelScale, darkMaterial, false, new Vector3(90f, 0f, 0f));
            CreatePart("Wheel BR", PrimitiveType.Cylinder, truck, new Vector3(0.32f, -0.52f, 0.52f), wheelScale, darkMaterial, false, new Vector3(90f, 0f, 0f));
        }

        private static GameObject CreateBlock(string name, Vector3 position, Vector3 scale,
            Material material, Transform parent = null)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (name != "Grass")
            {
                block.GetComponent<MeshFilter>().sharedMesh = StylizedGeometry.ChamferedCube;
            }
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            block.layer = ArenaObstacleLayer;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static GameObject CreatePart(string name, PrimitiveType type, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider,
            Vector3? localEulerAngles = null)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            if (type == PrimitiveType.Cube && name != "Distant Lawn")
            {
                part.GetComponent<MeshFilter>().sharedMesh = StylizedGeometry.ChamferedCube;
            }
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEulerAngles ?? Vector3.zero);
            part.transform.localScale = localScale;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) collider.enabled = keepCollider;
            if (keepCollider) part.layer = ArenaObstacleLayer;
            return part;
        }
    }
}
