using BoozeBlocks.Distractions;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using UnityEngine;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class RunProgression : MonoBehaviour
    {
        private HordeDirector horde;
        private AttractionSource truck;
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
            truck.SetInteractionEnabled(false);
            ApplyCurrentWave();
        }

        private void Update()
        {
            if (horde != null && horde.CurrentWave != appliedWave) ApplyCurrentWave();
        }

        private void ApplyCurrentWave()
        {
            if (horde == null || truck == null) return;
            appliedWave = horde.CurrentWave;
            truck.SetInteractionEnabled(appliedWave >= 2);
        }
    }
}
