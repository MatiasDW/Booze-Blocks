using System.Collections.Generic;
using BoozeBlocks.Distractions;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class RunProgression : MonoBehaviour
    {
        private List<PlayerVitals> players = new List<PlayerVitals>(8);
        private HordeDirector horde;
        private AttractionSource truck;
        private DrinkPreparationStation drinkStation;
        private int appliedWave;

        public string StatusText
        {
            get
            {
                if (horde == null || truck == null) return string.Empty;
                if (horde.CurrentWave < 2) return "Proximo desbloqueo: camion en oleada 2";
                if (truck.IsActive) return "Camion activo: los ninos estan distraidos";
                if (truck.CooldownRemaining > 0f) return $"Camion recargando: {Mathf.CeilToInt(truck.CooldownRemaining)}s";
                return "Camion listo: activalo con E";
            }
        }

        public void Configure(HordeDirector director, AttractionSource iceCreamTruck,
            DrinkPreparationStation station)
        {
            horde = director;
            truck = iceCreamTruck;
            drinkStation = station;
            truck.SetInteractionEnabled(false);
            ApplyCurrentWave();
        }

        private void OnEnable()
        {
            PlayerRegistry.Changed += HandlePlayersChanged;
        }

        private void OnDisable()
        {
            PlayerRegistry.Changed -= HandlePlayersChanged;
        }

        private void Update()
        {
            if (horde != null && horde.CurrentWave != appliedWave) ApplyCurrentWave();
        }

        private void HandlePlayersChanged()
        {
            ApplyPlayerUpgrades(Mathf.Max(1, appliedWave));
        }

        private void ApplyCurrentWave()
        {
            if (horde == null || truck == null) return;
            appliedWave = horde.CurrentWave;
            truck.SetInteractionEnabled(appliedWave >= 2);
            if (drinkStation != null)
            {
                drinkStation.SetPreparationMultiplier(appliedWave >= 6 ? 0.65f : appliedWave >= 3 ? 0.8f : 1f);
            }
            ApplyPlayerUpgrades(appliedWave);
        }

        private void ApplyPlayerUpgrades(int wave)
        {
            players ??= new List<PlayerVitals>(8);
            PlayerRegistry.Fill(players, true);
            int drinkCapacity = 2 + (wave >= 3 ? 1 : 0) + (wave >= 5 ? 1 : 0);
            int defensePower = wave >= 4 ? 1 + (wave - 4) / 3 : 0;
            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].TryGetComponent(out PlayerInventory inventory)) continue;
                inventory.Model.SetDrinkCapacityAtLeast(drinkCapacity);
                inventory.Model.SetDefensePowerLevelAtLeast(defensePower);
            }
        }
    }
}
