using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Synapse.Runtime.Telemetry.Data
{
    [Serializable]
    public sealed class InputSchema
    {
        public IReadOnlyList<InputAction> InputActions => inputActions;
        private readonly InputAction[] inputActions;
        
        public InputSchema() { inputActions = Array.Empty<InputAction>(); }
        public InputSchema(InputAction[] _inputActions) {
            inputActions = _inputActions != null ? (InputAction[])_inputActions.Clone() : Array.Empty<InputAction>();
            
            HashSet<Guid> uniqueIds = new();
            foreach (var action in InputActions) {
                if (action == null) { continue; }
                if (!uniqueIds.Add(action.id)) {
                    throw new InvalidOperationException("[InputSchema] Duplicate InputAction detected: " + $"{action.name}");
                }
            }
        }
    }
}