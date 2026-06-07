using UnityEngine.InputSystem;

namespace Synapse.Runtime.Core
{
    public interface IInputReader
    {
        /// <summary>
        /// Returns the state of a frame-dependent InputAction.
        /// </summary>
        InputContext.ActionState ReadState(InputAction _action);

        /// <summary>
        /// Returns the value of a continuous InputAction.
        /// </summary>
        InputContext.ActionValue<T> ReadValue<T>(InputAction _action) where T : struct;
    }
}