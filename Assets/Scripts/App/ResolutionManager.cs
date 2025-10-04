using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Neuralizer {
    
    /// <summary>
    /// Mantiene una resolución de salida a 720p como máximo para garantizar un mejor rendimiento en dispositivos moviles de gama baja
    /// </summary>
    public class ResolutionManager : MonoBehaviour {

        /// <summary>
        /// Referencia a los ajustes de renderizado
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.2/api/UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.html"/>
        [SerializeField] private UniversalRenderPipelineAsset urp;

        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDisable.html"/>
        private void OnDisable() {
            urp.renderScale = 1f;
            urp.msaaSampleCount = 1;
        }

        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.LateUpdate.html"/>
        private void LateUpdate() {
            float scale = Mathf.Clamp(720f / (float)Mathf.Min(Screen.width, Screen.height), 0.1f, 1f);
            urp.renderScale = scale;
            float rcp = 1f / scale;
            urp.msaaSampleCount = Mathf.RoundToInt(rcp * rcp);
        }
    }
}