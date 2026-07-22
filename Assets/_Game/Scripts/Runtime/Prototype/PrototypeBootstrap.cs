using System.Collections.Generic;
using BoozeBlocks.Camera;
using BoozeBlocks.Distractions;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
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

            CreateMaterials();
            BuildLighting();
            BuildArena();
            PlayerVitals player = null;
            for (int i = 0; i < simulatedPlayerCount; i++)
            {
                PlayerVitals createdPlayer = BuildPlayer(i, i == 0);
                if (i == 0) player = createdPlayer;
            }
            ThirdPersonCamera cameraController = BuildCamera(player.transform);
            AttractionSource truck = BuildInteractables(out DrinkPreparationStation drinkStation);

            GameObject systems = new GameObject("GameplaySystems");
            HordeDirector horde = systems.AddComponent<HordeDirector>();

            PrototypeRound round = systems.AddComponent<PrototypeRound>();
            round.Configure(150f);

            RunProgression progression = systems.AddComponent<RunProgression>();
            progression.Configure(horde, truck, drinkStation);

            PrototypeHud hud = systems.AddComponent<PrototypeHud>();
            hud.Configure(player, player.GetComponent<PlayerStateMachine>(),
                player.GetComponent<PlayerInteraction>(), round, horde,
                player.GetComponent<PlayerInventory>(), progression, drinkStation);

            PrototypeStartMenu startMenu = systems.AddComponent<PrototypeStartMenu>();
            startMenu.Configure(player, cameraController, horde, round, progression, hud);
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
            floorMaterial = CreateMaterial(new Color(0.16f, 0.42f, 0.25f));
            fenceMaterial = CreateMaterial(new Color(0.93f, 0.76f, 0.42f));
            woodMaterial = CreateMaterial(new Color(0.48f, 0.23f, 0.10f));
            darkMaterial = CreateMaterial(new Color(0.08f, 0.10f, 0.12f));
            playerMaterial = CreateMaterial(new Color(0.04f, 0.48f, 0.76f));
            skinMaterial = CreateMaterial(new Color(1f, 0.67f, 0.44f));
            pantsMaterial = CreateMaterial(new Color(0.10f, 0.19f, 0.30f));
            whiteMaterial = CreateMaterial(new Color(0.96f, 0.94f, 0.82f));
            drinkMaterial = CreateMaterial(new Color(1f, 0.55f, 0.06f));
            attractionMaterial = CreateMaterial(new Color(0.96f, 0.18f, 0.25f));
            yellowMaterial = CreateMaterial(new Color(1f, 0.78f, 0.10f));
            truckMaterial = CreateMaterial(new Color(0.05f, 0.72f, 0.76f));
            glassMaterial = CreateMaterial(new Color(0.16f, 0.31f, 0.40f));
            hedgeMaterial = CreateMaterial(new Color(0.08f, 0.32f, 0.14f));
            patioMaterial = CreateMaterial(new Color(0.70f, 0.63f, 0.50f));
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
            runtimeMaterials.Add(material);
            return material;
        }

        private static void BuildLighting()
        {
            GameObject lightObject = new GameObject("Warm Afternoon Sun");
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.88f, 0.69f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            RenderSettings.ambientLight = new Color(0.50f, 0.58f, 0.62f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.68f, 0.82f, 0.88f);
            RenderSettings.fogDensity = 0.006f;
        }

        private void BuildArena()
        {
            GameObject environment = new GameObject("Backyard Party");
            CreateBlock("Grass", new Vector3(0f, -0.5f, 0f), new Vector3(42f, 1f, 36f), floorMaterial, environment.transform);
            BuildFence(environment.transform);
            BuildHordeEntrances(environment.transform);
            BuildPatio(environment.transform);
            BuildPicnicTable(new Vector3(-6.5f, 0f, 5.2f), 12f, environment.transform);
            BuildPicnicTable(new Vector3(6.5f, 0f, -5.5f), -18f, environment.transform);
            BuildPicnicTable(new Vector3(13f, 0f, 8.5f), -32f, environment.transform);
            BuildGrill(new Vector3(-11.5f, 0f, -11.5f), environment.transform);
            BuildCooler(new Vector3(16f, 0f, -11.5f), environment.transform);
            BuildObstacleLayout(environment.transform);
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
                CreatePart($"Path Stone {i + 1}", PrimitiveType.Cube, environment.transform,
                    new Vector3(Mathf.Sin(i * 1.4f) * 0.45f, 0.04f, 2f + i * 1.35f),
                    new Vector3(1.4f, 0.08f, 0.85f), whiteMaterial, false,
                    new Vector3(0f, Mathf.Sin(i) * 9f, 0f));
            }
        }

        private void BuildFence(Transform parent)
        {
            CreateBlock("Fence North L", new Vector3(-11.5f, 0.8f, 18f), new Vector3(19f, 1.6f, 0.35f), fenceMaterial, parent);
            CreateBlock("Fence North R", new Vector3(11.5f, 0.8f, 18f), new Vector3(19f, 1.6f, 0.35f), fenceMaterial, parent);
            CreateBlock("Fence South L", new Vector3(-11.5f, 0.8f, -18f), new Vector3(19f, 1.6f, 0.35f), fenceMaterial, parent);
            CreateBlock("Fence South R", new Vector3(11.5f, 0.8f, -18f), new Vector3(19f, 1.6f, 0.35f), fenceMaterial, parent);
            CreateBlock("Fence East S", new Vector3(21f, 0.8f, -10f), new Vector3(0.35f, 1.6f, 16f), fenceMaterial, parent);
            CreateBlock("Fence East N", new Vector3(21f, 0.8f, 10f), new Vector3(0.35f, 1.6f, 16f), fenceMaterial, parent);
            CreateBlock("Fence West S", new Vector3(-21f, 0.8f, -10f), new Vector3(0.35f, 1.6f, 16f), fenceMaterial, parent);
            CreateBlock("Fence West N", new Vector3(-21f, 0.8f, 10f), new Vector3(0.35f, 1.6f, 16f), fenceMaterial, parent);
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
            CreatePart("Gate Post L", PrimitiveType.Cube, entrance.transform, new Vector3(-2f, 1.3f, 0f),
                new Vector3(0.35f, 2.6f, 0.35f), fenceMaterial, true);
            CreatePart("Gate Post R", PrimitiveType.Cube, entrance.transform, new Vector3(2f, 1.3f, 0f),
                new Vector3(0.35f, 2.6f, 0.35f), fenceMaterial, true);
            CreatePart("Gate Header", PrimitiveType.Cube, entrance.transform, new Vector3(0f, 2.75f, 0f),
                new Vector3(4.2f, 0.55f, 0.24f), darkMaterial, false);
            CreateWorldLabel(label, entrance.transform, new Vector3(0f, 2.78f, -0.15f), 0.052f);

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
            HordeEntrance component = entrance.AddComponent<HordeEntrance>();
            component.Configure(method, 14f, 9f, barricade);
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

        private void BuildHedge(string name, Vector3 position, Vector3 scale, Transform parent)
        {
            GameObject hedge = CreateBlock(name, position, scale, hedgeMaterial, parent);
            CreatePart("Leaf Top", PrimitiveType.Sphere, hedge.transform, new Vector3(0f, 0.52f, 0f),
                new Vector3(1.02f, 0.35f, 1.02f), hedgeMaterial, false);
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
            CreatePart("Shed Roof", PrimitiveType.Cube, shed.transform, new Vector3(0f, 0.58f, 0f),
                new Vector3(1.15f, 0.15f, 1.15f), attractionMaterial, false, new Vector3(0f, 0f, 7f));
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

        private void BuildGrill(Vector3 position, Transform parent)
        {
            GameObject grill = new GameObject("Barbecue Grill");
            grill.transform.SetParent(parent);
            grill.transform.position = position;
            CreatePart("Grill Body", PrimitiveType.Cube, grill.transform, new Vector3(0f, 1.1f, 0f), new Vector3(2.4f, 0.65f, 1.3f), darkMaterial, true);
            CreatePart("Hot Coals", PrimitiveType.Cube, grill.transform, new Vector3(0f, 1.48f, 0f), new Vector3(2.05f, 0.08f, 1.05f), attractionMaterial, false);
            for (int i = 0; i < 7; i++)
            {
                CreatePart($"Grill Bar {i + 1}", PrimitiveType.Cube, grill.transform,
                    new Vector3(-0.88f + i * 0.29f, 1.58f, 0f), new Vector3(0.035f, 0.035f, 1.12f),
                    darkMaterial, false);
            }
            CreatePart("Steak L", PrimitiveType.Cube, grill.transform, new Vector3(-0.5f, 1.57f, 0f), new Vector3(0.65f, 0.07f, 0.45f), woodMaterial, false, new Vector3(0f, 14f, 0f));
            CreatePart("Steak R", PrimitiveType.Cube, grill.transform, new Vector3(0.5f, 1.57f, 0f), new Vector3(0.65f, 0.07f, 0.45f), woodMaterial, false, new Vector3(0f, -12f, 0f));
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
            playerObject.AddComponent<PlayerDefenseController>();
            playerObject.AddComponent<PlayerStateMachine>();
            if (controllable) playerObject.AddComponent<PlayerInteraction>();
            else
            {
                input.enabled = false;
                motor.SetControlEnabled(false);
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
            camera.fieldOfView = 62f;
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
            CreateWorldLabel("BOOZE LAB\nPREPARA AQUI", station.transform, new Vector3(0f, 3.55f, 0.48f), 0.055f);
            CreatePart("Booze Beacon", PrimitiveType.Cylinder, station.transform, new Vector3(0f, 4.55f, 0f),
                new Vector3(0.28f, 0.58f, 0.28f), drinkMaterial, false);
            CreateWorldLabel("BOOZE", station.transform, new Vector3(0f, 5.35f, 0f), 0.082f);
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
            defensePickup.Configure(itemType, itemType == DefenseItemType.Broom ? 9 : 6, 18f);
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
