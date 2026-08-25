using UnityEngine;
using UnityEngine.InputSystem;

namespace BoozeBlocks.Player
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputAction moveAction;
        private InputAction interactAction;
        private InputAction useItemAction;
        private InputAction drinkAction;
        private InputAction jumpAction;
        private int interactPressedFrame = -1;
        private int useItemPressedFrame = -1;
        private int drinkPressedFrame = -1;
        private int jumpPressedFrame = -1;
        private bool useRemoteInput;
        private Vector2 remoteMove;
        private byte pendingRemoteActions;

        public Vector2 Move => useRemoteInput ? remoteMove : moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        public bool InteractPressed => useRemoteInput ? ConsumeRemoteAction(1) :
            interactPressedFrame == Time.frameCount || WasPressed(interactAction) ||
            (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
        public bool UseItemPressed => useRemoteInput ? ConsumeRemoteAction(2) :
            useItemPressedFrame == Time.frameCount || WasPressed(useItemAction) ||
            (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame);
        public bool DrinkPressed => useRemoteInput ? ConsumeRemoteAction(4) :
            drinkPressedFrame == Time.frameCount || WasPressed(drinkAction) ||
            (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame);
        public bool JumpPressed => useRemoteInput ? ConsumeRemoteAction(8) :
            jumpPressedFrame == Time.frameCount || WasPressed(jumpAction);

        private void Awake()
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");

            interactAction = new InputAction("Interact", InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonSouth");
            interactAction.performed += _ => interactPressedFrame = Time.frameCount;

            useItemAction = new InputAction("Use item", InputActionType.Button);
            useItemAction.AddBinding("<Keyboard>/f");
            useItemAction.AddBinding("<Mouse>/leftButton");
            useItemAction.AddBinding("<Gamepad>/rightShoulder");
            useItemAction.performed += _ => useItemPressedFrame = Time.frameCount;

            drinkAction = new InputAction("Drink", InputActionType.Button);
            drinkAction.AddBinding("<Keyboard>/q");
            drinkAction.AddBinding("<Gamepad>/leftShoulder");
            drinkAction.performed += _ => drinkPressedFrame = Time.frameCount;

            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonNorth");
            jumpAction.performed += _ => jumpPressedFrame = Time.frameCount;

        }

        private void Update()
        {
            if (useRemoteInput) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.eKey.wasPressedThisFrame) interactPressedFrame = Time.frameCount;
            if (keyboard.fKey.wasPressedThisFrame) useItemPressedFrame = Time.frameCount;
            if (keyboard.qKey.wasPressedThisFrame) drinkPressedFrame = Time.frameCount;
            if (keyboard.spaceKey.wasPressedThisFrame) jumpPressedFrame = Time.frameCount;
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            interactAction?.Enable();
            useItemAction?.Enable();
            drinkAction?.Enable();
            jumpAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            interactAction?.Disable();
            useItemAction?.Disable();
            drinkAction?.Disable();
            jumpAction?.Disable();
        }

        private void OnDestroy()
        {
            moveAction?.Dispose();
            interactAction?.Dispose();
            useItemAction?.Dispose();
            drinkAction?.Dispose();
            jumpAction?.Dispose();
        }

        private static bool WasPressed(InputAction action)
        {
            return action != null && action.enabled && action.WasPressedThisFrame();
        }

        public void SetRemoteWorldInput(Vector2 worldMove)
        {
            useRemoteInput = true;
            remoteMove = Vector2.ClampMagnitude(worldMove, 1f);
        }

        public bool TryGetRemoteWorldDirection(out Vector3 direction)
        {
            direction = new Vector3(remoteMove.x, 0f, remoteMove.y);
            return useRemoteInput;
        }

        public void PulseRemoteActions(byte actions)
        {
            useRemoteInput = true;
            pendingRemoteActions |= actions;
        }

        public void ClearRemoteInput()
        {
            remoteMove = Vector2.zero;
        }

        private bool ConsumeRemoteAction(byte action)
        {
            if ((pendingRemoteActions & action) == 0) return false;
            pendingRemoteActions = (byte)(pendingRemoteActions & ~action);
            return true;
        }
    }
}
