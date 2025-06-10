using UnityEngine;

[ExecuteInEditMode]
public class AxisGizmo : MonoBehaviour {

    private void LateUpdate() {
        transform.rotation = Quaternion.identity;
    }
}
