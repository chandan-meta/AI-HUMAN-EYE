using UnityEngine;
namespace VisionLab {
    public class ScanSequenceController : MonoBehaviour {
        public S02_UIBinder view;
        public Transform plane;
        public float SweepY, LayerIndex;
        MaterialPropertyBlock block;
        public void Sample(float elapsed, bool reducedMotion) {
            int pass = Mathf.Clamp((int)(elapsed / 1.5f), 0, 3);
            SweepY = Mathf.Lerp(1.85f, -.05f, elapsed % 1.5f / 1.5f); LayerIndex = pass;
            if (plane) { plane.gameObject.SetActive(!reducedMotion && elapsed < 6); plane.localPosition = new Vector3(0, SweepY, 0); }
            block ??= new MaterialPropertyBlock();
            for (int i=0;i<view.layers.Length;i++) {
                view.layers[i].SetActive(i == pass || i >= 2 && pass >= 2);
                foreach (var renderer in view.layers[i].GetComponentsInChildren<Renderer>()) {
                    renderer.GetPropertyBlock(block); block.SetFloat("_SweepY", SweepY);
                    block.SetFloat("_LayerIndex", LayerIndex); block.SetFloat("_Alpha", i == pass ? 1 : .22f); renderer.SetPropertyBlock(block);
                }
            }
        }
        public void Finish() { if (plane) plane.gameObject.SetActive(false); for (int i=0;i<view.layers.Length;i++) view.layers[i].SetActive(i >= 2); }
    }
}
