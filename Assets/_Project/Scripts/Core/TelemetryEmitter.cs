using System;
using System.IO;
using UnityEngine;
namespace VisionLab {
    public class TelemetryEmitter : MonoBehaviour {
        [Serializable] class Entry { public string timestamp, session, type, detail; public float elapsed; }
        public string SessionId { get; private set; } = Guid.NewGuid().ToString("N");
        StreamWriter writer;
        float start;
        void Awake() {
            start = Time.realtimeSinceStartup;
            try { writer = new StreamWriter(Path.Combine(Application.persistentDataPath, "S02_" + SessionId + ".jsonl"), true) { AutoFlush = true }; }
            catch (IOException e) { Debug.LogWarning("Telemetry unavailable: " + e.Message); }
            catch (UnauthorizedAccessException e) { Debug.LogWarning("Telemetry unavailable: " + e.Message); }
        }
        public void Emit(string type, string detail = "") {
            var line = JsonUtility.ToJson(new Entry { timestamp = DateTime.UtcNow.ToString("O"), session = SessionId, type = type, detail = detail, elapsed = Time.realtimeSinceStartup - start });
            try { writer?.WriteLine(line); } catch (IOException e) { Debug.LogWarning(e.Message); writer?.Dispose(); writer = null; }
        }
        void OnDestroy() { writer?.Dispose(); }
    }
}
