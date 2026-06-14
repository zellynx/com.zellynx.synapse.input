using UnityEngine.InputSystem;

namespace Synapse.Input.Reader
{
    /// <summary>
    /// Represents the state of an InputAction.
    /// </summary>
    public struct InputActionState
    {
        public bool IsPressed;
        public bool WasPressed;
        public bool WasReleased;

        public bool IsInProgress;
        public bool WasPerformed;
        public bool WasCompleted;
    }

    /// <summary>
    /// Represents the value of an InputAction.
    /// </summary>
    public struct InputActionValue<T> where T : struct
    {
        public T Value;

        public InputActionValue(T _value) {
            Value = _value;
        }
    }
    
    /// <summary>
    /// Represents the callback for an InputAction event.
    /// </summary>
    public delegate void InputActionCallback(InputAction.CallbackContext context);

    public interface IInputReader
    {
        /// <summary>
        /// The InputActionAsset used by the InputReader.
        /// </summary>
        InputActionAsset InputAsset { get; }
        
        /// <summary>
        /// Returns the state of an InputAction.
        /// </summary>
        InputActionState ReadState(InputAction _action);

        /// <summary>
        /// Returns the value of an InputAction.
        /// </summary>
        InputActionValue<T> ReadValue<T>(InputAction _action) where T : struct;

        /// <summary>
        /// Subscribes to an event of specific phase of an InputAction
        /// </summary>
        void Subscribe(InputAction action, InputActionPhase phase, InputActionCallback callback);

        /// <summary>
        /// Unsubscribes from event of a specific phase of an InputAction.
        /// </summary>
        void Unsubscribe(InputAction action, InputActionPhase phase, InputActionCallback callback);
    }
}