using System;
using UnityEngine.InputSystem;

namespace Synapse.Input.Recorder
{
    [Serializable]
    public struct InputRecord
    {
        public uint Sequence;
        public Guid ActionId;
        public InputActionPhase Phase;
        public double Time;
        public byte[] InputData;
    }
    
    [Serializable]
    public sealed class InputSession
    {
        public Guid Id;
        public string Name;
        public DateTime DateTime;
        public double Duration;
        public InputRecord[] Records = Array.Empty<InputRecord>();
    }
}