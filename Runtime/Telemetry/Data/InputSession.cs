using System;
using UnityEngine.InputSystem;

namespace Synapse.Runtime.Telemetry.Data
{
    [Serializable]
    public struct InputRecord
    {
        public uint Sequence;
        public Guid TrackedActionId;
        public InputActionPhase Phase;
        public double Time;
        public byte[] InputData;
    }

    [Serializable]
    public sealed class InputSession
    {
        public Guid SessionId = Guid.NewGuid();
        public string SessionName;
        public DateTime RecordedAtUtc = DateTime.UtcNow;
        public double Duration;
        public Guid[] TrackedActionIds = Array.Empty<Guid>();
        public InputRecord[] Records = Array.Empty<InputRecord>();
    }
}