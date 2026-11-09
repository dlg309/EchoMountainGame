using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoMountain.Riding
{
    internal sealed class LabRideControls
    {
        private readonly InputActionMap actions = new("Lab riding");
        private readonly InputAction keyboardSteer;
        private readonly InputAction stick;
        private readonly InputAction brake;
        public InputAction Pop { get; }
        public InputAction Reset { get; }
        public InputAction Pause { get; }

        public LabRideControls()
        {
            keyboardSteer = actions.AddAction("Keyboard steer", InputActionType.Value);
            keyboardSteer.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/d")
                .With("Positive", "<Keyboard>/rightArrow");
            stick = actions.AddAction("Stick steer", InputActionType.Value, "<Gamepad>/leftStick");
            brake = actions.AddAction("Brake", InputActionType.Value, "<Gamepad>/leftTrigger");
            brake.AddBinding("<Keyboard>/s");
            brake.AddBinding("<Keyboard>/downArrow");
            Pop = AddButton("Pop", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Reset = AddButton("Reset", "<Keyboard>/r", "<Gamepad>/buttonEast");
            Pause = AddButton("Pause", "<Keyboard>/escape", "<Gamepad>/start");
        }

        public float Steer => Mathf.Clamp(keyboardSteer.ReadValue<float>() + stick.ReadValue<Vector2>().x, -1f, 1f);
        public float Brake => brake.ReadValue<float>();
        public void Enable() => actions.Enable();
        public void Disable() => actions.Disable();
        public void Dispose() => actions.Dispose();

        private InputAction AddButton(string name, string keyboard, string gamepad)
        {
            var action = actions.AddAction(name, InputActionType.Button, keyboard);
            action.AddBinding(gamepad);
            return action;
        }
    }
}
