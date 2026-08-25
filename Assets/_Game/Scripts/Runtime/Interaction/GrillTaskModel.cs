using System;

namespace BoozeBlocks.Interaction
{
    public enum GrillTaskState
    {
        NeedsFuel,
        Heating,
        ReadyToCook,
        Cooking,
        ReadyToServe,
        Burned
    }

    public sealed class GrillTaskModel
    {
        private readonly float heatingDuration;
        private readonly float cookingDuration;
        private readonly float burnDuration;
        private float remaining;

        public GrillTaskModel(float heatingDuration, float cookingDuration, float burnDuration)
        {
            this.heatingDuration = Math.Max(0.1f, heatingDuration);
            this.cookingDuration = Math.Max(0.1f, cookingDuration);
            this.burnDuration = Math.Max(0.1f, burnDuration);
            State = GrillTaskState.NeedsFuel;
        }

        public GrillTaskState State { get; private set; }
        public float Remaining => Math.Max(0f, remaining);

        public bool TryInteract(float speedMultiplier = 1f)
        {
            float multiplier = Math.Clamp(speedMultiplier, 0.25f, 2f);
            switch (State)
            {
                case GrillTaskState.NeedsFuel:
                    State = GrillTaskState.Heating;
                    remaining = heatingDuration * multiplier;
                    return true;
                case GrillTaskState.ReadyToCook:
                    State = GrillTaskState.Cooking;
                    remaining = cookingDuration * multiplier;
                    return true;
                case GrillTaskState.ReadyToServe:
                    State = GrillTaskState.ReadyToCook;
                    remaining = 0f;
                    return true;
                case GrillTaskState.Burned:
                    State = GrillTaskState.ReadyToCook;
                    remaining = 0f;
                    return true;
                default:
                    return false;
            }
        }

        public bool Tick(float deltaTime)
        {
            if (deltaTime <= 0f || remaining <= 0f) return false;
            remaining -= deltaTime;
            if (remaining > 0f) return false;

            switch (State)
            {
                case GrillTaskState.Heating:
                    State = GrillTaskState.ReadyToCook;
                    break;
                case GrillTaskState.Cooking:
                    State = GrillTaskState.ReadyToServe;
                    remaining = burnDuration;
                    break;
                case GrillTaskState.ReadyToServe:
                    State = GrillTaskState.Burned;
                    break;
            }
            return true;
        }

        public void ApplySnapshot(GrillTaskState state, float remainingTime)
        {
            State = state;
            remaining = Math.Max(0f, remainingTime);
        }
    }
}
