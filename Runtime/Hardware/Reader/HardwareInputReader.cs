using System;
using System.Collections.Generic;
using Synapse.Runtime.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Synapse.Runtime.Hardware.Reader
{
    public class HardwareInputReader : MonoBehaviour, IInputReader
    {
        [Tooltip("The root Input Action Asset.")] [SerializeField]
        private InputActionAsset InputAsset;

        private readonly Dictionary<Guid, InputContext.ActionState> actionStates = new();
        private readonly List<InputAction> trackedStateActions = new();

        #region Unity Lifecycle
        private void Awake() { if(InputAsset != null) Initialize(InputAsset); }
        private void OnEnable() { if (InputAsset != null) InputAsset.Enable(); }
        private void OnDisable() { if (InputAsset != null) InputAsset.Disable(); }
        private void Update() { UpdateActionStates(); }
        #endregion

        #region Initialize InputReader
        /// <summary>
        /// Initializes the HardwareInputReader with a specified InputActionAsset.
        /// </summary>
        /// <param name="_inputAsset"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Initialize(InputActionAsset _inputAsset) {
            if (_inputAsset == null) { throw new ArgumentNullException(nameof(_inputAsset)); }
            if (InputAsset != null && InputAsset != _inputAsset && isActiveAndEnabled) { InputAsset.Disable(); }

            InputAsset = _inputAsset;
            BuildStateActionCache();
            if (isActiveAndEnabled) { InputAsset.Enable(); }
        }
        
        /// <summary>
        /// Builds a cache of frame-dependent actions for efficient state tracking.
        /// </summary>
        private void BuildStateActionCache() {
            actionStates.Clear();
            trackedStateActions.Clear();

            if (InputAsset == null) {
                Debug.LogError("[HardwareInputReader] InputActionAsset is not assigned.");
                return;
            }

            foreach (var action in InputAsset) {
                if (action.type == InputActionType.Button) {
                    actionStates[action.id] = new InputContext.ActionState();
                    trackedStateActions.Add(action);
                }
            }
        }
        #endregion

        #region Update Action States
        /// <summary>
        /// Update the states of cached frame-dependent actions.
        /// </summary>
        private void UpdateActionStates() {
            foreach (var action in trackedStateActions) {
                var state = actionStates[action.id];

                var isHeld = action.IsPressed();
                state.WasPressedThisFrame = !state.IsHeld && isHeld;
                state.WasReleasedThisFrame = state.IsHeld && !isHeld;
                state.IsHeld = isHeld;

                actionStates[action.id] = state;
            }
        }
        #endregion

        #region IInputReader Implementation
        public InputContext.ActionState ReadState(InputAction _action) =>
            _action != null && actionStates.TryGetValue(_action.id, out var state) ? state : default;
        
        public InputContext.ActionValue<T> ReadValue<T>(InputAction _action) where T : struct =>
            new(_action?.ReadValue<T>() ?? default);
        #endregion
    }
}