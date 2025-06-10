using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour {

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform planarCameraTransform;

    public bool AllowRotation { get; set; } = false;

    private Vector2 rotation = Vector2.zero;
    
    private Vector2 planarRotation = Vector2.zero;

    public void OnPointerDelta(InputAction.CallbackContext ctx) {
        if (!AllowRotation) return;
        Vector2 delta = ctx.ReadValue<Vector2>();
        delta.x /= Screen.width;
        delta.y /= Screen.height;
        delta *= 360f;
        rotation.x += delta.y;
        rotation.y -= delta.x;
        rotation.x %= 360f;
        rotation.y %= 360f;
        planarRotation.x = Mathf.Clamp(planarRotation.x + delta.y, 0f, 90f);
        planarRotation.y = 0f;
        cameraTransform.rotation = Quaternion.Euler(rotation);
        planarCameraTransform.rotation = Quaternion.Euler(planarRotation);
    }
}
