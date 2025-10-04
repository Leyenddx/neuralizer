using UnityEngine;
using UnityEngine.Rendering;

namespace Neuralizer {
    /// <summary>
    /// Componente que obliga al objeto a mirar a la cámara activa
    /// </summary>
    public class LookAtCamera : MonoBehaviour {
        
        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnEnable.html"/>
        private void OnEnable() {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDisable.html"/>
        private void OnDisable() {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        /// <summary>
        /// Evento llamado cada vez que una cámara comenzara a renderizar
        /// </summary>
        /// <param name="ctx">El contexto actual del renderizador</param>
        /// <param name="cam">La cámara que está por renderizar</param>
        private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam) {
            transform.forward = (cam.transform.position - transform.position).normalized;
        }
    }
}