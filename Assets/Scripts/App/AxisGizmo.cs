using UnityEngine;

namespace Neuralizer {
    [ExecuteInEditMode]
    public class AxisGizmo : MonoBehaviour {
        private void LateUpdate() {
            transform.rotation = Quaternion.identity;
        }
    }
}