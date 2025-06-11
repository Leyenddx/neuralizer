using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour {
    [SerializeField] private float cameraSpeed = 24f;
    [SerializeField] private CinemachineCamera[] cinemachineCameras;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform planarCameraTransform;
    [SerializeField] private CinemachinePositionComposer planarCameraComposer;
    [SerializeField] private UnityEvent<bool> OnShow2DField;
    [SerializeField] private UnityEvent<bool> OnShow2DFunction;
    [SerializeField] private UnityEvent<bool> OnShowDrawer;

    public bool AllowRotation { get; set; } = false;

    private Vector2 targetRotation = Vector2.zero;
    private Vector2 target2DRotation = Vector2.zero;
    private bool show2DFunction = false;
    private bool isFullscreen = false;
    private Vector2 targetComposerShift = Vector2.zero;

    private Vector2 targetLensShift = new(0f, -0.333333f);
    private Vector2 currentLensShift = Vector2.zero;

    private void Update() {
        float t = Mathf.Exp(-cameraSpeed * Time.deltaTime);
        cameraTransform.rotation = Quaternion.Slerp(Quaternion.Euler(targetRotation), cameraTransform.rotation, t);
        // planarCameraTransform.rotation =
        //     Quaternion.Slerp(Quaternion.Euler(target2DRotation), planarCameraTransform.rotation, t);
        // planarCameraComposer.Composition.ScreenPosition =
        //     Vector2.Lerp(targetComposerShift, planarCameraComposer.Composition.ScreenPosition, t);
        currentLensShift = Vector2.Lerp(targetLensShift, currentLensShift, t);
        foreach (var cinemachineCamera in cinemachineCameras) {
            cinemachineCamera.Lens.PhysicalProperties.LensShift = currentLensShift;
        }
    }

    public void OnPointerDelta(InputAction.CallbackContext ctx) {
        if (!AllowRotation) return;
        Vector2 delta = ctx.ReadValue<Vector2>();
        delta.x /= Screen.width;
        delta.y /= -Screen.height;
        targetComposerShift += delta;
        targetComposerShift.Set(Mathf.Clamp(targetComposerShift.x, -1f, 1f),
            Mathf.Clamp(targetComposerShift.y, -1f, 1f));
        delta *= 360f;
        targetRotation.x += delta.y;
        targetRotation.y += delta.x;
        targetRotation.x %= 360f;
        targetRotation.y %= 360f;
    }

    public void Toggle2DFunctionView() {
        show2DFunction = !show2DFunction;
        target2DRotation.Set(show2DFunction ? 90f : 0f, 0f);
        OnShow2DField?.Invoke(!show2DFunction);
        OnShow2DFunction?.Invoke(show2DFunction);
    }

    public void ToggleFullscreen() {
        isFullscreen = !isFullscreen;
        OnShowDrawer?.Invoke(!isFullscreen);
        targetLensShift.Set(0f, isFullscreen ? 0f : -0.333333f);
    }

    public void ResetCamera() {
        const float stepSize = 45f;
        targetRotation.x = Mathf.Round(targetRotation.x / stepSize) * stepSize;
        targetRotation.y = Mathf.Round(targetRotation.y / stepSize) * stepSize;
        targetComposerShift.Set(0f, 0f);
    }
}