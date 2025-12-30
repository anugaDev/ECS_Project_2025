using Unity.Entities;

namespace PlayerInputs
{
    public partial class ShortcutInputSystem : SystemBase
    {
        private InputActions _inputActions;

        protected override void OnCreate()
        {
            _inputActions = new InputActions();
        }

        protected override void OnStartRunning()
        {
            _inputActions.Enable();
        }

        protected override void OnStopRunning()
        {
            _inputActions.Disable();
        }

        protected override void OnUpdate()
        {
            KeyShortcutInputComponent keyShortcutInputComponent = new KeyShortcutInputComponent();

            if (_inputActions.GameplayMap.SelectBaseShortcut.WasPressedThisFrame())
            {
                keyShortcutInputComponent.SelectBaseInput.Set();
            }
        }
    }
}