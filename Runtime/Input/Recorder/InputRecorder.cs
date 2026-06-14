using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Synapse.Input.Recorder
{
    public class InputRecorder
    {
        [Serializable]
        public sealed class Schema
        {
            public InputAction[] InputActions { get; set; }

            public bool RecordStates { get; set; }
            public bool RecordValues { get; set; }
            public bool RecordEvents { get; set; }

            public bool Contains(Guid _actionId) {
                return TryGetAction(_actionId, out _);
            }

            public bool TryGetAction(Guid _actionId, out InputAction _action) {
                foreach (var action in InputActions) {
                    if (action.id == _actionId) {
                        _action = action;
                        return true;
                    }
                }

                _action = null;
                return false;
            }

            public void Validate() {
                if (InputActions == null) {
                    throw new ArgumentNullException(nameof(InputActions));
                }

                HashSet<Guid> actionIds = new();
                foreach (var action in InputActions) {
                    if (action == null) {
                        throw new ArgumentException("InputSchema cannot contain null actions.", nameof(InputActions));
                    }

                    if (!actionIds.Add(action.id)) {
                        throw new InvalidOperationException($"Duplicate InputAction detected: {action.name}");
                    }
                }
            }
        }
        
        public string SessionName { get; set; }
        public string OutputFilePath { get; set; }
        public Schema RecorderSchema  { get; set; }
        
        private bool isRecording;
        private InputActionTrace trace;
        private InputSession session;

        public InputRecorder(string _sessionName, string _outputFilePath, Schema _recorderSchema) {
            // Supplied Parameters are null or invalid
            if(string.IsNullOrWhiteSpace(_sessionName)) throw new InvalidOperationException("SessionName has null or empty.");
            if(string.IsNullOrWhiteSpace(_outputFilePath)) throw new InvalidOperationException("OutputFilePath has null or empty.");
            if(_recorderSchema == null) throw new ArgumentNullException(nameof(_recorderSchema));
            
            SessionName = _sessionName;
            OutputFilePath = _outputFilePath;
            RecorderSchema = _recorderSchema;
            
            RecorderSchema.Validate();
        }
        
        #region InputRecorder Controls
        /// <summary>
        /// Start recording input based on provided InputRecorderSchema.
        /// </summary>
        /// <param name="_recorderSchema"></param>
        public void Start() {
            if (isRecording) {
                Debug.Log("InputRecorder is already recording.");
                return;
            }
            
            Reset();
            foreach (var action in RecorderSchema.InputActions) 
                trace.SubscribeTo(action);
            isRecording = true;
        }

        /// <summary>
        /// Clears all recorded data in InputActionTrace.
        /// </summary>
        public void Reset() {
            if (!isRecording) {
                trace = new InputActionTrace();
                return;
            }
            trace.Clear();
        }

        /// <summary>
        /// Stop recording input and create InputRecorderSession.
        /// </summary>
        /// <returns></returns>
        public void Stop() {
            if (!isRecording) {
                Debug.Log("InputRecorder is not recording.");
                return ;
            }
            
            trace.UnsubscribeFromAll();
            isRecording = false;
            session = InputSessionBuilder.CreateSession(trace, SessionName);
            trace.Dispose();
        }
        
        /// <summary>
        /// Save InputRecorderSession to file
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_session"></param>
        public void Save() {
            if (isRecording) {
                Debug.Log("Cannot save while InputRecorder is recording.");
                return;
            }
            
            using var stream = File.Create(OutputFilePath);
            InputSessionSerializer.Serialize(stream, session);
        }
        #endregion
    }
    /*#region Recorder Controls
        [ContextMenu("Start Recording")]
        public void StartRecording()
        {
            sampler.Clear();
            sampler.Start();
            Debug.Log("[TelemetryRecorder] Recording started.");
        }

        [ContextMenu("Stop Recording")]
        public void StopRecording()
        {
            sampler.Stop();
            Debug.Log("[TelemetryRecorder] Recording stopped.");
        }
        #endregion

        [ContextMenu("Save Recording")]
        public void SaveRecording()
        {
            var path = Path.Combine(Application.persistentDataPath, OutputFileName);
            sampler.SaveSessionToFile(path, SessionName);
            Debug.Log("[TelemetryRecorder] " + $"Saved recording:\n{path}");
        }*/
}