using System;
using System.Collections.Generic;
using System.IO;
using Synapse.Input.Telemetry.Telemetry.Data;
using Synapse.Input.Telemetry.Telemetry.IO;
using UnityEngine.InputSystem.Utilities;

namespace Synapse.Input.Telemetry.Telemetry.Recorder.LowLevel
{
    public sealed class InputDataSampler : IDisposable
    {
        public bool IsSampling { get; private set; }

        private readonly InputSchema schema;
        private readonly InputActionTrace trace = new();

        #region Sampler Constructor
        public InputDataSampler(InputSchema _schema) {
            schema = _schema ?? throw new ArgumentNullException(nameof(_schema));
        }
        #endregion

        #region Sampler Controls
        public void Start() {
            if (IsSampling) { return; }

            foreach (var action in schema.InputActions) {
                if (action == null) { continue; }
                trace.SubscribeTo(action);
            }

            IsSampling = true;
        }

        public void Stop() {
            if (!IsSampling) { return; }

            foreach (var action in schema.InputActions) {
                if (action == null) { continue; }
                trace.UnsubscribeFrom(action);
            }

            IsSampling = false;
        }

        /// <summary>
        /// Clears all currently recorded input data from the sampler.
        ///
        /// If sampling is currently active, sampling continues
        /// after the recorded data is cleared.
        /// This method does NOT stop sampling.
        /// </summary>
        public void Clear() {
            trace.Clear();
        }
        
        public void Dispose() {
            if(IsSampling) Stop();
            trace.Dispose();
        }
        #endregion

        #region InputSession Creation
        /// <summary>
        /// Creates an InputSession from the currently
        /// recorded input data.
        ///
        /// If sampling is active, sampling is stopped
        /// before the session is created.
        /// </summary>
        public InputSession CreateSession(string _sessionName = null) {
            if (IsSampling) { Stop(); }
            
            List<InputRecord> records = new();

            uint sequence = 0;
            double startTime = -1;
            double endTime = 0;

            foreach (var eventPtr in trace) {
                var action = eventPtr.action;
                if (action == null) { continue; }
                
                if (startTime < 0) { startTime = eventPtr.time; }
                endTime = eventPtr.time - startTime;

                records.Add(new InputRecord
                {
                    Sequence = sequence++,
                    TrackedActionId = action.id,
                    Phase = eventPtr.phase,
                    Time = eventPtr.time - startTime,
                    InputData = ReadInputBytes(eventPtr)
                });
            }
            
            List<Guid> trackedActionIds = new();
            foreach (var action in schema.InputActions) {
                if (action == null) { continue; }
                trackedActionIds.Add(action.id);
            }

            return new InputSession
            {
                SessionName = _sessionName,
                TrackedActionIds = trackedActionIds.ToArray(),
                RecordedAtUtc = DateTime.UtcNow,
                Duration = endTime,
                Records = records.ToArray()
            };
        }
        #endregion
        
        public void SaveSessionToFile(string _path, string _sessionName = null) {
            if (string.IsNullOrWhiteSpace(_path)) {
                throw new ArgumentException("Invalid file path.", nameof(_path));
            }
            
            using var stream = File.Create(_path);
            WriteSession(stream, _sessionName);
        }
        
        public void WriteSession(Stream _stream, string _sessionName = null) {
            if (_stream == null) {
                throw new ArgumentNullException(nameof(_stream));
            }

            var session = CreateSession(_sessionName);
            InputSessionSerializer.Serialize(_stream, session);
        }

        #region Sampler Utilities
        private static unsafe byte[] ReadInputBytes(InputActionTrace.ActionEventPtr _eventPtr)
        {
            var valueSize = _eventPtr.valueSizeInBytes;
            switch (valueSize) {
                case <= 0:
                    return Array.Empty<byte>();
                case > TelemetryInputProtocol.MaxPayloadSize:
                    throw new InvalidOperationException("Input payload exceeded maximum " + $"allowed size: {valueSize}");
            }
            var valueData = new byte[valueSize];
            fixed (byte* valuePtr = valueData) {
                _eventPtr.ReadValue(valuePtr, valueSize);
            }

            return valueData;
        }
        #endregion
    }
}