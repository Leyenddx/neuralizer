using UnityEngine;
using UnityEngine.Rendering;

public class LookAtCamera : MonoBehaviour {

        private void OnEnable() {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable() {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam) {
            transform.LookAt(cam.transform.position);
        }
}
