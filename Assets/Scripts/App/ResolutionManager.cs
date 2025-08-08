using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Neuralizer {
    public class ResolutionManager : MonoBehaviour {

        [SerializeField] private UniversalRenderPipelineAsset urp;

        private void LateUpdate() {
            float scale = Mathf.Clamp(720f / (float)Mathf.Min(Screen.width, Screen.height), 0.1f, 1f);
            urp.renderScale = scale;
            float rcp = 1f / scale;
            urp.msaaSampleCount = Mathf.RoundToInt(rcp * rcp);
        }
    }
}