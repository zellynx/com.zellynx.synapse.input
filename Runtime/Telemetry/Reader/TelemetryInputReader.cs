using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Synapse.Runtime.Core;
using Synapse.Runtime.Telemetry.Data;
using Synapse.Runtime.Telemetry.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Synapse.Runtime.Telemetry.Reader
{
    public class TelemetryInputReader : MonoBehaviour, IInputReader
    {
        public enum State
        {
            Unloaded,
            LoadedAndReady,
            Reading,
            Paused,
            Completed
        }
        
        [SerializeField] private InputActionAsset InputAsset;
        [SerializeField] private string InputFileName = "telemetry.dat";
        [SerializeField] [Min(0f)] private float ReadSpeed = 1f;

        private InputSession inputSession;
        private readonly Dictionary<Guid, InputContext.ActionState> actionStates = new();
        private readonly Dictionary<Guid, byte[]> actionValues = new();
        private readonly List<Guid> trackedStateActionIds = new();
        private readonly List<Guid> trackedValueActionIds = new();
        private double readTime;
        private int recordIndex;
        
        public State CurrentState { get; private set; } = State.Unloaded;

        #region Unity Lifecycle
        private void Update() {
            if (CurrentState != State.Reading || inputSession == null) { return; }
            ProcessTelemetryInputSession(Time.deltaTime);
        }
        #endregion
        
        #region Load TelemetryInput Data
        public void LoadInputSession(InputSession _session, IEnumerable<InputAction> _definedActions) {
            inputSession = _session ?? throw new ArgumentNullException(nameof(_session));
            if (_definedActions == null) { throw new ArgumentNullException(nameof(_definedActions)); }

            actionStates.Clear();
            actionValues.Clear();
            trackedStateActionIds.Clear();
            trackedValueActionIds.Clear();

            foreach (var action in _definedActions) {
                if (action == null) { continue; }

                var actionId = action.id;
                if(action.type == InputActionType.Button) {
                    trackedStateActionIds.Add(actionId);
                    actionStates[action.id] = new InputContext.ActionState();
                }
                trackedValueActionIds.Add(actionId);
                actionValues[action.id] = Array.Empty<byte>();
            }

            readTime = 0;
            recordIndex = 0;
            CurrentState = State.LoadedAndReady;
        }
        
        private bool LoadFromFile(string _path, IEnumerable<InputAction> _definedActions) {
            if (string.IsNullOrWhiteSpace(_path)) {
                Debug.LogError("[TelemetryInputReader] " + $"File Path is null or empty:\n{_path}");
                return false;
            }

            if (!File.Exists(_path)) {
                Debug.LogError("[TelemetryInputReader] " + $"File not found:\n{_path}");
                return false;
            }

            using var stream = File.OpenRead(_path);
            
            var session = InputSessionDeserializer.Deserialize(stream);
            LoadInputSession(session, _definedActions);
            return true;
        }
        
        public void Load()
        {
            if (string.IsNullOrWhiteSpace(InputFileName)) {
                Debug.LogError("[TelemetryInputReader] " + "InputFileName is null or empty"); return;
            }
            var path = Path.Combine(Application.persistentDataPath, InputFileName);

            if (InputAsset == null) {
                Debug.LogError("[TelemetryInputReader] " + "InputAsset missing."); return;
            }
            var definedActions = InputAsset.ToArray();

            if (!LoadFromFile(path, definedActions)) { return; }
            Debug.Log("[TelemetryInputReader] " + $"Loaded session:\n{path}");
        }
        #endregion

        #region Reader Controls
        [ContextMenu("Begin")]
        public void Begin() {
            if (CurrentState == State.Unloaded) { Load(); }
            
            if (CurrentState != State.LoadedAndReady) {
                Debug.LogWarning("[TelemetryInputReader] " + "Reader must be in LoadedAndReady state to begin.");
                return;
            }

            CurrentState = State.Reading;
            Debug.Log("[TelemetryInputReader] Reading began.");
        }

        [ContextMenu("Pause")]
        public void Pause() {
            if (CurrentState != State.Reading) { return; }

            ResetStateFlags();
            CurrentState = State.Paused;
            Debug.Log("[TelemetryInputReader] Reading paused.");
        }
        
        [ContextMenu("Resume")]
        public void Resume() {
            if (CurrentState != State.Paused) {
                Debug.LogWarning("[TelemetryInputReader] " + "Reading is not paused.");
                return;
            }

            CurrentState = State.Reading;
            Debug.Log("[TelemetryInputReader] Reading resumed.");
        }

        [ContextMenu("End")]
        public void End() {
            if (CurrentState == State.Unloaded) { return; }

            readTime = 0;
            recordIndex = 0;
            ResetData();

            CurrentState = State.LoadedAndReady;
            Debug.Log("[TelemetryInputReader] Reading ended.");
        }

        [ContextMenu("Restart")]
        public void Restart() {
            End();
            Begin();
            Debug.Log("[TelemetryInputReader] Reading restarted.");
        }

        public void SetReadSpeed(float _speed) {
            ReadSpeed = Mathf.Max(0f, _speed);
            Debug.Log("[TelemetryInputReader] Reading speed set to " + $"{ReadSpeed:0.00}x");
        }

        public void IncreaseReadSpeed(float _amount = 0.25f) {
            ReadSpeed += _amount;
            ReadSpeed = Mathf.Max(0f, ReadSpeed);
            Debug.Log("[TelemetryInputReader] Reading speed increased to " + $"{ReadSpeed:0.00}x");
        }

        public void DecreaseReadSpeed(float _amount = 0.25f) {
            ReadSpeed -= _amount;
            ReadSpeed = Mathf.Max(0f, ReadSpeed);
            Debug.Log("[TelemetryInputReader] Reading speed decreased to " + $"{ReadSpeed:0.00}x");
        }
        #endregion

        #region TelemetryInput Processing
        private void ProcessTelemetryInputSession(float _deltaTime) {
            readTime += _deltaTime * ReadSpeed;

            ResetStateFlags();

            while (recordIndex < inputSession.Records.Length) {
                var inputRecord = inputSession.Records[recordIndex];
                
                if (inputRecord.Time > readTime) { break; }
                ProcessTelemetryInputRecord(inputRecord);
                recordIndex++;
            }

            // Reading session completed.
            if (recordIndex >= inputSession.Records.Length) {
                ResetData();
                CurrentState = State.Completed;
                Debug.Log("[TelemetryInputReader] Reading completed.");
            }
        }

        private void ProcessTelemetryInputRecord(InputRecord _record) {
            if (actionStates.TryGetValue(_record.TrackedActionId, out var state)) {
                switch (_record.Phase) {
                    case InputActionPhase.Started:
                    case InputActionPhase.Performed:
                        if (!state.IsHeld) { state.WasPressedThisFrame = true; }
                        state.IsHeld = true;
                        break;
                    case InputActionPhase.Canceled:
                        state.IsHeld = false;
                        state.WasReleasedThisFrame = true;
                        break;
                }
                actionStates[_record.TrackedActionId] = state;
            }
            if (actionValues.ContainsKey(_record.TrackedActionId)) {
                actionValues[_record.TrackedActionId] = _record.InputData ?? Array.Empty<byte>();
            }
        }

        private void ResetStateFlags() {
            foreach (var actionId in trackedStateActionIds) {
                var state = actionStates[actionId];

                state.WasPressedThisFrame = false;
                state.WasReleasedThisFrame = false;

                actionStates[actionId] = state;
            }
        }

        private void ResetData() {
            foreach (var actionId in trackedStateActionIds) {
                actionStates[actionId] = new InputContext.ActionState();
            }
            foreach (var actionId in trackedValueActionIds) {
                actionValues[actionId] = Array.Empty<byte>();
            }
        }
        #endregion

        #region IInputReader Implementation
        public InputContext.ActionState ReadState(InputAction _action) =>
            _action != null ? actionStates.GetValueOrDefault(_action.id) : default;

        public InputContext.ActionValue<T> ReadValue<T>(InputAction _action) where T : struct {
            if (_action == null) { return default; }
            if (!actionValues.TryGetValue(_action.id, out var valueData)) { return default; }
            if (valueData == null || valueData.Length == 0) { return default; }

            var value = ByteArrayToStructure<T>(valueData);
            return new InputContext.ActionValue<T>(value);
        }
        #endregion

        #region Data Conversion Utilities
        private static T ByteArrayToStructure<T>(byte[] _data) where T : struct {
            var expectedSize = System.Runtime.InteropServices.Marshal.SizeOf<T>();
            if (_data == null || _data.Length < expectedSize) { return default; }

            unsafe {
                fixed (byte* ptr = _data) {
                    return System.Runtime.InteropServices.Marshal.PtrToStructure<T>((IntPtr)ptr);
                }
            }
        }
        #endregion
    }
}