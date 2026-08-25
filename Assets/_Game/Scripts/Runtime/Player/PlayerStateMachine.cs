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
        [SerializeField, Min(0f)] private float cooperativeKnockedDownDuration = 5f;
        [SerializeField, Min(0f)] private float recoveryDuration = 0.45f;

        private PlayerVitals vitals;
        private PlayerMotor motor;
        private float stateTimer;
        private bool hasSimulationAuthority = true;

        public PlayerState State { get; private set; } = PlayerState.Normal;
        public bool CanBeRevived => State == PlayerState.KnockedDown;

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
            if (!hasSimulationAuthority) return;
            if (State != PlayerState.KnockedDown && State != PlayerState.Recovering) return;

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f) return;

            if (State == PlayerState.KnockedDown)
            {
                State = PlayerState.Recovering;
                motor.SetKnockdownPhysics(false);
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
            stateTimer = PlayerRegistry.ActiveCount > 1 ? cooperativeKnockedDownDuration : knockedDownDuration;
            motor.SetKnockdownPhysics(true);
            motor.ApplyKnockdownImpulse();
            motor.SetControlEnabled(false);
        }

        public bool TryRevive()
        {
            if (!CanBeRevived) return false;
            EnsureDependencies();
            vitals.RecoverBalance();
            motor.SetKnockdownPhysics(false);
            State = PlayerState.Normal;
            stateTimer = 0f;
            motor.SetControlEnabled(true);
            return true;
        }

        public void ApplyRemoteState(PlayerState state)
        {
            if (State == state) return;
            State = state;
            stateTimer = 0f;
            if (motor != null) motor.SetControlEnabled(state == PlayerState.Normal);
        }

        public void SetSimulationAuthority(bool isAuthoritative)
        {
            hasSimulationAuthority = isAuthoritative;
        }

        private void HandleEliminated()
        {
            State = PlayerState.Eliminated;
            motor.SetKnockdownPhysics(true);
            motor.ApplyKnockdownImpulse();
            motor.SetControlEnabled(false);
        }

        private void EnsureDependencies()
        {
            if (vitals == null) vitals = GetComponent<PlayerVitals>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
        }
    }
}
