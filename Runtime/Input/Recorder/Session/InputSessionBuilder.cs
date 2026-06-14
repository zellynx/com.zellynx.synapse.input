using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.Utilities;

namespace Synapse.Input.Recorder
{
    public static class InputSessionBuilder
    {
        /// <summary>
        /// Creates InputRecorderSession from InputActionTrace
        /// </summary>
        public static InputSession CreateSession(InputActionTrace _trace, string _sessionName = null) {
            var records = new List<InputRecord>();

            uint sequence = 0;
            double startTime = -1;
            double endTime = 0;

            foreach (var eventPtr in _trace) {
                var record = CreateRecord(eventPtr, ref sequence, ref startTime, ref endTime);
                records.Add(record);
            }

            return new InputSession {
                Id = Guid.NewGuid(),
                Name = _sessionName,
                DateTime = DateTime.UtcNow,
                Duration = endTime,
                Records = records.ToArray()
            };
        }
        
        /// <summary>
        /// Created InputActionRecord from InputActionTrace.ActionEventPtr
        /// </summary>
        /// <param name="_eventPtr"></param>
        /// <param name="_sequence"></param>
        /// <param name="_startTime"></param>
        /// <param name="_endTime"></param>
        /// <returns></returns>
        private static InputRecord CreateRecord(InputActionTrace.ActionEventPtr _eventPtr, ref uint _sequence, ref double _startTime, ref double _endTime) {
            var action = _eventPtr.action;
            if (action == null) throw new InvalidOperationException("ActionEventPtr has null action reference."); 

            if (_startTime < 0)
                _startTime = _eventPtr.time;
            _endTime = _eventPtr.time - _startTime;

            return new InputRecord {
                Sequence = _sequence++,
                ActionId = action.id,
                Phase = _eventPtr.phase,
                Time = _eventPtr.time - _startTime,
                InputData = GetActionEventBytes(_eventPtr)
            };
        }

        /// <summary>
        /// Get ByteArray from InputActionTrace.ActionEventPtr valueData
        /// </summary>
        /// <param name="_eventPtr"></param>
        /// <returns></returns>
        private static unsafe byte[] GetActionEventBytes(InputActionTrace.ActionEventPtr _eventPtr) {
            var valueSize = _eventPtr.valueSizeInBytes;
            var valueData = new byte[valueSize];

            fixed (byte* valuePtr = valueData) {
                _eventPtr.ReadValue(valuePtr, valueSize);
            }

            return valueData;
        }
    }
}