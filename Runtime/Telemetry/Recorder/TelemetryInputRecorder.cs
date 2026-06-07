using System.IO;
using System.Linq;
using Synapse.Runtime.Telemetry.Data;
using Synapse.Runtime.Telemetry.Recorder.LowLevel;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Synapse.Runtime.Telemetry.Recorder
{
    public class TelemetryInputRecorder : MonoBehaviour
    {
        [SerializeField] private InputActionAsset InputAsset;
        [SerializeField] private string OutputFileName = "telemetry.dat";
        [SerializeField] private string SessionName = "Telemetry Session";

        private InputDataSampler sampler;

        #region Unity Lifecycle
        private void Awake() {
            if (InputAsset == null) {
                Debug.LogError("[TelemetryRecorder] InputAsset missing.");
                return;
            }

            var actions = InputAsset.ToArray();
            InputSchema schema = new(actions);
            sampler = new InputDataSampler(schema);
        }

        private void OnDestroy() { sampler?.Dispose(); }
        #endregion

        #region Recorder Controls
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
        }
    }
}