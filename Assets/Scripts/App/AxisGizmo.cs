using UnityEngine;

namespace Neuralizer {
    /// <summary>
    /// Componente auxiliar para el gizmo de rotación. Mantiene una rotación neutral sin importar la rotación de padre
    /// </summary>
    [ExecuteInEditMode]
    public class AxisGizmo : MonoBehaviour {
        private void LateUpdate() {
            transform.rotation = Quaternion.identity;
        }
    }
}