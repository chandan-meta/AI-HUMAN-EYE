using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace VisionLab {
    public class QualityTier : MonoBehaviour {
        public UniversalRenderPipelineAsset pc, tablet;
        void Awake(){QualitySettings.renderPipeline=Application.isMobilePlatform?tablet:pc;Application.targetFrameRate=Application.isMobilePlatform?30:60;}
    }
}
