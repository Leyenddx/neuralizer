using UnityEngine;

[ExecuteInEditMode]
public class AxisGizmo : MonoBehaviour {

    [SerializeField] private Camera source;
    [SerializeField] private Vector3 position;

    private void LateUpdate() {
        transform.position = source.ViewportToWorldPoint(position);
        transform.rotation = Quaternion.identity;
    }
}
