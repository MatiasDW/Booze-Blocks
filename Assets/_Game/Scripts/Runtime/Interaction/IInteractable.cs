using BoozeBlocks.Player;

namespace BoozeBlocks.Interaction
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract(PlayerVitals player);
        void Interact(PlayerVitals player);
    }
}
