using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Synapse.Input.Reader
{
    public class LiveInputReader : IInputReader
    {
        public InputActionAsset InputAsset { get; }
        
        /// <summary>
        /// Registry of Callbacks of all events of all InputActions in the InputActionAsset
        /// </summary>
        private readonly Dictionary<(InputAction action, InputActionPhase phase), InputActionCallback> actionCallbacks = new();

        public LiveInputReader(InputActionAsset _inputAsset) {
            // Supplied Parameter is null and invalid
            if (_inputAsset == null) {
                throw new ArgumentNullException(nameof(_inputAsset));
            }

            InputAsset = _inputAsset;
            RegisterEvents();
            InputAsset.Enable();
        }
        
        #region InputAction Events Routing
        /// <summary>
        /// Registers all events of all InputActions in the InputAsset to a single router method (DispatchEvent).
        /// </summary>
        private void RegisterEvents() {
            foreach (var action in InputAsset) {
                // Clear any existing hooks
                action.started -= DispatchEvent;
                action.performed -= DispatchEvent;
                action.canceled -= DispatchEvent;

                // Hook all events to the single router
                action.started += DispatchEvent;
                action.performed += DispatchEvent;
                action.canceled += DispatchEvent;
            }
        }

        /// <summary>
        /// Dispatches InputAction event to the appropriate callback based on the InputAction and its phase.
        /// </summary>
        /// <param name="ctx"></param>
        private void DispatchEvent(InputAction.CallbackContext ctx) {
            var key = (ctx.action, ctx.phase);
            if (actionCallbacks.TryGetValue(key, out var callback)) {
                callback?.Invoke(ctx);
            }
        }
        #endregion

        #region Input Reading & Subscription API
        public InputActionState ReadState(InputAction _action) => 
            _action != null ? new InputActionState {
                IsPressed = _action.IsPressed(),
                WasPressed = _action.WasPressedThisFrame(),
                WasReleased = _action.WasReleasedThisFrame(),
                
                IsInProgress = _action.IsInProgress(),
                WasPerformed = _action.WasPerformedThisFrame(),
                WasCompleted = _action.WasCompletedThisFrame()
            } : default;
        
        public InputActionValue<T> ReadValue<T>(InputAction _action) where T : struct =>
            new(_action?.ReadValue<T>() ?? default);
        
        public void Subscribe(InputAction action, InputActionPhase phase, InputActionCallback callback) {
            if (action == null || callback == null) return;
            
            var key = (action, phase);
            if (!actionCallbacks.TryAdd(key, callback)) {
                actionCallbacks[key] += callback;
            }
        }

        public void Unsubscribe(InputAction action, InputActionPhase phase, InputActionCallback callback) {
            if (action == null || callback == null) return;

            var key = (action, phase);
            if (actionCallbacks.ContainsKey(key)) {
                actionCallbacks[key] -= callback;
            }
        }
        #endregion
    }
}