using System;
using UnityEngine;
namespace VisionLab {
    [CreateAssetMenu(menuName = "Vision Lab/VO manifest")]
    public class VOManifest : ScriptableObject {
        [Serializable] public class Line { public int id; public string text; public AudioClip clip; }
        public Line[] lines;
        public Line Find(int id) => Array.Find(lines, line => line.id == id);
    }
}
