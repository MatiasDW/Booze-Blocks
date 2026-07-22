using UnityEngine;

namespace BoozeBlocks.Player
{
    public enum PlayerState
    {
        Normal,
        KnockedDown,
        Recovering,
        Eliminated
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerVitals), typeof(PlayerMotor))]
    public sealed class PlayerStateMachine : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float knockedDownDuration = 1.25f;
        [SerializeField, Min(0f)] private float recoveryDuration = 0.45f;

        private PlayerVitals vitals;
        private PlayerMotor motor;
        private float stateTimer;

        public PlayerState State { get; private set; } = PlayerState.Normal;

        private void Awake()
        {
            EnsureDependencies();
        }

        private void OnEnable()
        {
            EnsureDependencies();
            if (vitals == null) return;
            vitals.KnockedDown += HandleKnockdown;
            vitals.Eliminated += HandleEliminated;
        }

        private void OnDisable()
        {
            if (vitals == null) return;
            vitals.KnockedDown -= HandleKnockdown;
            vitals.Eliminated -= HandleEliminated;
        }

        private void Update()
        {
            EnsureDependencies();
            if (State != PlayerState.KnockedDown && State != PlayerState.Recovering) return;

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f) return;

            if (State == PlayerState.KnockedDown)
            {
                State = PlayerState.Recovering;
                stateTimer = recoveryDuration;
                return;
            }

            vitals.RecoverBalance();
            State = PlayerState.Normal;
            motor.SetControlEnabled(true);
        }

        private void HandleKnockdown()
        {
            if (State == PlayerState.Eliminated || State == PlayerState.KnockedDown) return;
            State = PlayerState.KnockedDown;
            stateTimer = knockedDownDuration;
            motor.ApplyKnockdownImpulse();
            motor.SetControlEnabled(false);
        }

        private void HandleEliminated()
        {
            State = PlayerState.Eliminated;
            motor.SetControlEnabled(false);
        }

        private void EnsureDependencies()
        {
            if (vitals == null) vitals = GetComponent<PlayerVitals>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
        }
    }
}
