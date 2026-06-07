namespace Synapse.Runtime.Core
{
    public static class InputContext
    {
        /// <summary>
        /// Represents the state of a frame-dependent InputAction.
        /// </summary>
        public struct ActionState
        {
            public bool IsHeld;
            public bool WasPressedThisFrame;
            public bool WasReleasedThisFrame;
        }

        /// <summary>
        /// Represents the value of a continuous InputAction.
        /// </summary>
        public struct ActionValue<T> where T : struct
        {
            public T Value;

            public ActionValue(T _value) {
                Value = _value;
            }
        }
    }
}