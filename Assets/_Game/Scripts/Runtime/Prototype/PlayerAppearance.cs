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
            new Color(0.16f, 0.16f, 0.18f)
        };

        private static readonly Color[] SkinColors =
        {
            new Color(1f, 0.68f, 0.46f),
            new Color(0.76f, 0.43f, 0.25f),
            new Color(0.48f, 0.25f, 0.14f),
            new Color(1f, 0.80f, 0.63f)
        };

        private static readonly string[] ShirtNames = { "Azul", "Roja", "Mostaza", "Verde", "Ladrillo", "Negra" };
        private static readonly string[] SkinNames = { "Clara", "Canela", "Oscura", "Palida" };
        private static readonly string[] AccessoryNames = { "Sin gorro", "Fiesta", "Vaquero", "Jockey" };

        private Transform visualRoot;
        private Transform moustache;
        private Transform partyHat;
        private Transform cowboyHat;
        private Transform cap;
        private int shirtIndex;
        private int skinIndex;
        private int accessoryIndex;
        private bool hasMoustache;

        public string CurrentDescription { get; private set; } = "Clasico";
        public string ShirtName => ShirtNames[shirtIndex];
        public string SkinName => SkinNames[skinIndex];
        public string AccessoryName => AccessoryNames[accessoryIndex];
        public bool HasMoustache => hasMoustache;

        public void Initialize(Transform root, int playerIndex)
        {
            visualRoot = root;
            FindAccessories();
            shirtIndex = Mathf.Abs(playerIndex) % ShirtColors.Length;
            skinIndex = Mathf.Abs(playerIndex) % SkinColors.Length;
            ApplyAppearance();
        }

        public void CyclePreset()
        {
            CycleShirt(1);
            CycleSkin(1);
            accessoryIndex = (accessoryIndex + 1) % AccessoryNames.Length;
            hasMoustache = true;
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

        public void ToggleMoustache()
        {
            hasMoustache = !hasMoustache;
            ApplyAppearance();
        }

        private void ApplyAppearance()
        {
            if (visualRoot == null) return;
            FindAccessories();
            SetActive(moustache, hasMoustache);
            SetActive(partyHat, accessoryIndex == 1);
            SetActive(cowboyHat, accessoryIndex == 2);
            SetActive(cap, accessoryIndex == 3);

            Color shirt = ShirtColors[shirtIndex];
            Color skin = SkinColors[skinIndex];
            Color pants = shirtIndex % 2 == 0 ? new Color(0.08f, 0.15f, 0.24f) : new Color(0.24f, 0.10f, 0.08f);
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) ApplyColors(renderers[i], shirt, skin, pants);

            CurrentDescription = hasMoustache ? $"{AccessoryName} + bigote" : AccessoryName;
        }

        private static void ApplyColors(Renderer renderer, Color shirt, Color skin, Color pants)
        {
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader.name == "BoozeBlocks/StylizedCharacter")
            {
                properties.SetColor("_ShirtColor", shirt);
                properties.SetColor("_SkinColor", skin);
                properties.SetColor("_PantsColor", pants);
                properties.SetColor("_DarkColor", new Color(0.04f, 0.025f, 0.015f));
            }
            else
            {
                string part = renderer.gameObject.name;
                Color color = part.Contains("Sleeve") || part.Contains("Hat Color") ? shirt :
                    part.Contains("Hand") || part == "Leg" ? skin :
                    part.Contains("Shorts") ? pants : new Color(0.04f, 0.025f, 0.015f);
                properties.SetColor("_Color", color);
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
