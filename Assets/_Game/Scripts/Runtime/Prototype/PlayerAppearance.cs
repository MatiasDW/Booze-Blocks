using UnityEngine;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PlayerAppearance : MonoBehaviour
    {
        private static readonly Color[] ShirtColors =
        {
            new Color(0.05f, 0.48f, 0.76f),
            new Color(0.93f, 0.20f, 0.26f),
            new Color(0.98f, 0.62f, 0.06f),
            new Color(0.08f, 0.66f, 0.40f),
            new Color(0.78f, 0.24f, 0.12f),
            new Color(0.16f, 0.16f, 0.18f),
            new Color(0.52f, 0.20f, 0.68f),
            new Color(0.95f, 0.42f, 0.65f),
            new Color(0.92f, 0.90f, 0.78f),
            new Color(0.10f, 0.68f, 0.72f)
        };

        private static readonly Color[] SkinColors =
        {
            new Color(1f, 0.68f, 0.46f),
            new Color(0.76f, 0.43f, 0.25f),
            new Color(0.48f, 0.25f, 0.14f),
            new Color(1f, 0.80f, 0.63f),
            new Color(0.63f, 0.34f, 0.20f),
            new Color(0.35f, 0.17f, 0.10f)
        };

        private static readonly Color[] PantsColors =
        {
            new Color(0.08f, 0.15f, 0.24f), new Color(0.24f, 0.10f, 0.08f),
            new Color(0.12f, 0.30f, 0.20f), new Color(0.30f, 0.26f, 0.12f),
            new Color(0.12f, 0.12f, 0.13f), new Color(0.45f, 0.18f, 0.30f)
        };

        private static readonly Color[] HairColors =
        {
            new Color(0.04f, 0.025f, 0.015f), new Color(0.22f, 0.09f, 0.03f),
            new Color(0.56f, 0.30f, 0.08f), new Color(0.82f, 0.68f, 0.30f),
            new Color(0.45f, 0.45f, 0.45f)
        };

        private static readonly string[] ShirtNames =
            { "Azul", "Roja", "Mostaza", "Verde", "Ladrillo", "Negra", "Morada", "Rosada", "Crema", "Turquesa" };
        private static readonly string[] SkinNames = { "Clara", "Canela", "Oscura", "Palida", "Bronce", "Profunda" };
        private static readonly string[] PantsNames = { "Azul noche", "Vino", "Verde", "Caqui", "Negro", "Ciruela" };
        private static readonly string[] HairNames = { "Negro", "Castano", "Cobrizo", "Rubio", "Canoso" };
        private static readonly string[] AccessoryNames =
            { "Sin gorro", "Fiesta", "Vaquero", "Jockey", "Chef", "Casco chelero" };
        private static readonly string[] FacialHairNames = { "Afeitado", "Bigote", "Barba", "Bigote y barba" };

        private Transform visualRoot;
        private Transform moustache;
        private Transform partyHat;
        private Transform cowboyHat;
        private Transform cap;
        private Transform chefHat;
        private Transform beerHelmet;
        private Transform beard;
        private Transform glasses;
        private int shirtIndex;
        private int skinIndex;
        private int pantsIndex;
        private int hairIndex;
        private int accessoryIndex;
        private int facialHairIndex;
        private bool hasGlasses;

        public string CurrentDescription { get; private set; } = "Clasico";
        public string ShirtName => ShirtNames[shirtIndex];
        public string SkinName => SkinNames[skinIndex];
        public string PantsName => PantsNames[pantsIndex];
        public string HairName => HairNames[hairIndex];
        public string AccessoryName => AccessoryNames[accessoryIndex];
        public string FacialHairName => FacialHairNames[facialHairIndex];
        public bool HasMoustache => facialHairIndex is 1 or 3;
        public bool HasGlasses => hasGlasses;
        public int ShirtIndex => shirtIndex;
        public int SkinIndex => skinIndex;
        public int PantsIndex => pantsIndex;
        public int HairIndex => hairIndex;
        public int AccessoryIndex => accessoryIndex;
        public int FacialHairIndex => facialHairIndex;

        public void Initialize(Transform root, int playerIndex)
        {
            visualRoot = root;
            FindAccessories();
            shirtIndex = Mathf.Abs(playerIndex) % ShirtColors.Length;
            skinIndex = Mathf.Abs(playerIndex) % SkinColors.Length;
            pantsIndex = Mathf.Abs(playerIndex) % PantsColors.Length;
            hairIndex = Mathf.Abs(playerIndex) % HairColors.Length;
            ApplyAppearance();
        }

        public void CyclePreset()
        {
            CycleShirt(1);
            CycleSkin(1);
            CyclePants(1);
            CycleHair(1);
            accessoryIndex = (accessoryIndex + 1) % AccessoryNames.Length;
            facialHairIndex = 1;
            ApplyAppearance();
        }

        public void CycleShirt(int direction)
        {
            shirtIndex = Wrap(shirtIndex + direction, ShirtColors.Length);
            ApplyAppearance();
        }

        public void CycleSkin(int direction)
        {
            skinIndex = Wrap(skinIndex + direction, SkinColors.Length);
            ApplyAppearance();
        }

        public void CycleAccessory(int direction)
        {
            accessoryIndex = Wrap(accessoryIndex + direction, AccessoryNames.Length);
            ApplyAppearance();
        }

        public void CyclePants(int direction)
        {
            pantsIndex = Wrap(pantsIndex + direction, PantsColors.Length);
            ApplyAppearance();
        }

        public void CycleHair(int direction)
        {
            hairIndex = Wrap(hairIndex + direction, HairColors.Length);
            ApplyAppearance();
        }

        public void CycleFacialHair(int direction)
        {
            facialHairIndex = Wrap(facialHairIndex + direction, FacialHairNames.Length);
            ApplyAppearance();
        }

        public void ToggleMoustache()
        {
            facialHairIndex = HasMoustache ? 0 : 1;
            ApplyAppearance();
        }

        public void ToggleGlasses()
        {
            hasGlasses = !hasGlasses;
            ApplyAppearance();
        }

        public void ApplySnapshot(int shirt, int skin, int pants, int hair, int accessory,
            int facialHair, bool glassesEnabled)
        {
            int nextShirt = Wrap(shirt, ShirtColors.Length);
            int nextSkin = Wrap(skin, SkinColors.Length);
            int nextPants = Wrap(pants, PantsColors.Length);
            int nextHair = Wrap(hair, HairColors.Length);
            int nextAccessory = Wrap(accessory, AccessoryNames.Length);
            int nextFacialHair = Wrap(facialHair, FacialHairNames.Length);
            if (shirtIndex == nextShirt && skinIndex == nextSkin && pantsIndex == nextPants &&
                hairIndex == nextHair && accessoryIndex == nextAccessory &&
                facialHairIndex == nextFacialHair && hasGlasses == glassesEnabled) return;

            shirtIndex = nextShirt;
            skinIndex = nextSkin;
            pantsIndex = nextPants;
            hairIndex = nextHair;
            accessoryIndex = nextAccessory;
            facialHairIndex = nextFacialHair;
            hasGlasses = glassesEnabled;
            ApplyAppearance();
        }

        private void ApplyAppearance()
        {
            if (visualRoot == null) return;
            FindAccessories();
            SetActive(moustache, HasMoustache);
            SetActive(beard, facialHairIndex is 2 or 3);
            SetActive(glasses, hasGlasses);
            SetActive(partyHat, accessoryIndex == 1);
            SetActive(cowboyHat, accessoryIndex == 2);
            SetActive(cap, accessoryIndex == 3);
            SetActive(chefHat, accessoryIndex == 4);
            SetActive(beerHelmet, accessoryIndex == 5);

            Color shirt = ShirtColors[shirtIndex];
            Color skin = SkinColors[skinIndex];
            Color pants = PantsColors[pantsIndex];
            Color hair = HairColors[hairIndex];
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) ApplyColors(renderers[i], shirt, skin, pants, hair);

            CurrentDescription = $"{AccessoryName}, {FacialHairName}{(hasGlasses ? ", lentes" : string.Empty)}";
        }

        private static void ApplyColors(Renderer renderer, Color shirt, Color skin, Color pants, Color hair)
        {
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader.name == "BoozeBlocks/StylizedCharacter")
            {
                properties.SetColor("_ShirtColor", shirt);
                properties.SetColor("_SkinColor", skin);
                properties.SetColor("_PantsColor", pants);
                properties.SetColor("_DarkColor", hair);
            }
            else
            {
                string part = renderer.gameObject.name;
                if (part.Contains("Sleeve") || part.Contains("Hat Color")) properties.SetColor("_Color", shirt);
                else if (part.Contains("Hand") || part == "Leg") properties.SetColor("_Color", skin);
                else if (part.Contains("Shorts")) properties.SetColor("_Color", pants);
                else if (part.Contains("Hair") || part.Contains("Moustache") || part.Contains("Beard"))
                    properties.SetColor("_Color", hair);
            }
            renderer.SetPropertyBlock(properties);
        }

        private void FindAccessories()
        {
            if (visualRoot == null) return;
            moustache = visualRoot.Find("Accessories/Moustache");
            partyHat = visualRoot.Find("Accessories/Party Hat");
            cowboyHat = visualRoot.Find("Accessories/Cowboy Hat");
            cap = visualRoot.Find("Accessories/Cap");
            chefHat = visualRoot.Find("Accessories/Chef Hat");
            beerHelmet = visualRoot.Find("Accessories/Beer Helmet");
            beard = visualRoot.Find("Accessories/Beard");
            glasses = visualRoot.Find("Accessories/Glasses");
        }

        private static void SetActive(Transform target, bool active)
        {
            if (target != null) target.gameObject.SetActive(active);
        }

        private static int Wrap(int value, int count)
        {
            return (value % count + count) % count;
        }
    }
}
