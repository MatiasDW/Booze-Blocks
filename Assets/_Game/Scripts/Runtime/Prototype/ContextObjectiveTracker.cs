using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class ContextObjectiveTracker : MonoBehaviour
    {
        private PlayerVitals vitals;
        private PlayerInventory inventory;
        private DrinkPreparationStation drinkStation;
        private GrillTaskStation grill;
        private HordeDirector horde;
        private PlayerReviveTarget[] reviveTargets = System.Array.Empty<PlayerReviveTarget>();
        private HordeEntrance[] entrances = System.Array.Empty<HordeEntrance>();
        private float startedAt;
        private float refreshAt;

        public string CurrentObjective { get; private set; } = "Prepara la parrillada.";
        public string TutorialText { get; private set; } = string.Empty;

        public void Configure(PlayerVitals player, PlayerInventory playerInventory,
            DrinkPreparationStation station, GrillTaskStation grillStation, HordeDirector hordeDirector)
        {
            vitals = player;
            inventory = playerInventory;
            drinkStation = station;
            grill = grillStation;
            horde = hordeDirector;
            reviveTargets = FindObjectsByType<PlayerReviveTarget>();
            entrances = FindObjectsByType<HordeEntrance>();
            startedAt = Time.time;
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < refreshAt) return;
            refreshAt = Time.unscaledTime + 0.25f;
            Refresh();
        }

        private void Refresh()
        {
            if (vitals == null || inventory == null || drinkStation == null || grill == null || horde == null) return;

            float runTime = Time.time - startedAt;
            TutorialText = runTime switch
            {
                < 6f => "WASD para moverte | Mouse para mirar | Espacio para saltar",
                < 12f => "Acercate a los objetos y pulsa E para interactuar",
                < 18f => "F usa tu defensa | Q bebe una carga de booze",
                _ => string.Empty
            };

            bool teammateNeedsHelp = false;
            for (int i = 0; i < reviveTargets.Length; i++)
            {
                if (reviveTargets[i] != null && reviveTargets[i].CanInteract(vitals))
                {
                    teammateNeedsHelp = true;
                    break;
                }
            }

            HordeEntrance activeConstruction = null;
            for (int i = 0; i < entrances.Length; i++)
            {
                if (entrances[i] != null && entrances[i].IsBeingBuiltBy(vitals))
                {
                    activeConstruction = entrances[i];
                    break;
                }
            }

            if (teammateNeedsHelp)
                CurrentObjective = "URGENTE: acercate y levanta a tu companero con E.";
            else if (activeConstruction != null)
                CurrentObjective = $"MANTENTE CERCA: construyendo bloqueo {Mathf.RoundToInt(activeConstruction.BuildProgressRatio * 100f)}%.";
            else if (grill.State == GrillTaskState.ReadyToServe)
                CurrentObjective = "URGENTE: sirve el asado antes de que se queme.";
            else if (vitals.BuzzRatio < 0.35f && inventory.Model.DrinkServings == 0)
                CurrentObjective = drinkStation.State == DrinkStationState.Ready
                    ? "Rellena tu botella en el BOOZE LAB."
                    : "Prepara booze en el BOOZE LAB antes de quedarte seco.";
            else if (drinkStation.State == DrinkStationState.Ready &&
                     inventory.Model.DrinkServings < inventory.Model.DrinkCapacity)
                CurrentObjective = "La mezcla esta lista: rellena tu botella.";
            else if (inventory.Model.DefenseItem == DefenseItemType.None)
                CurrentObjective = "Busca una escoba o sarten para defender el patio.";
            else if (horde.IsPreparing)
                CurrentObjective = "PREPARACION: equipa una defensa, prepara booze y bloquea una entrada.";
            else if (!horde.IsWaveActive)
                CurrentObjective = "Descanso: bloquea entradas, prepara booze y atiende la parrilla.";
            else if (grill.State == GrillTaskState.NeedsFuel)
                CurrentObjective = "Enciende el carbon y comienza el asado.";
            else if (grill.State == GrillTaskState.ReadyToCook)
                CurrentObjective = "La parrilla esta caliente: pon la carne.";
            else
                CurrentObjective = "Resiste la oleada y reparte la horda entre las entradas.";
        }
    }
}
