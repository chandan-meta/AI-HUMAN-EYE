using UnityEngine;
namespace VisionLab {
    public class EyePart : MonoBehaviour {
        public string Id;
        public int VoiceId;
        public Renderer[] surfaces;
        public bool Visited;
        MaterialPropertyBlock block;
        public void Highlight(Color colour, float alpha = 1, float strength = .4f) {
            block ??= new MaterialPropertyBlock();
            foreach (var surface in surfaces) {
                foreach(var material in surface.sharedMaterials){material.SetFloat("_ZWrite",alpha>=.99f?1:0);material.renderQueue=alpha>=.99f?2000:3000;}
                block.SetColor("_Tint", colour); block.SetFloat("_Alpha", alpha);
                block.SetFloat("_Highlight", strength); surface.SetPropertyBlock(block);
            }
        }
    }
}
