using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Neuralizer {
    public class PlayerController : MonoBehaviour {
        [SerializeField] private float cameraSpeed = 24f;
        [SerializeField] private CinemachineCamera[] cinemachineCameras;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform planarCameraTransform;
        [SerializeField] private CinemachinePositionComposer planarCameraComposer;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform visibilityReference;
        [SerializeField] private Vector2 lensShiftRanges;
        [SerializeField] private float sensorSize = 56.328f;
        [SerializeField] private UnityEvent<bool> OnShowDrawer;

        public bool AllowRotation { get; set; } = false;

        private Vector2 targetRotation = Vector2.zero;
        private Vector3 target2DRotation = Vector3.zero;
        private bool show2DFunction = false;
        private bool isFullscreen = false;
        private Vector2 targetComposerShift = Vector2.zero;

        private Vector2 targetLensShift = new(0f, -0.333333f);
        private Vector2 currentLensShift = Vector2.zero;

        private Vector2 currentSensorSize = Vector2.one;

        private void Update() {
            float t = Mathf.Exp(-cameraSpeed * Time.deltaTime);
            cameraTransform.rotation = Quaternion.Slerp(Quaternion.Euler(targetRotation), cameraTransform.rotation, t);
            planarCameraTransform.rotation =
                Quaternion.Slerp(Quaternion.Euler(target2DRotation), planarCameraTransform.rotation, t);
            planarCameraComposer.Composition.ScreenPosition =
                Vector2.Lerp(show2DFunction ? Vector2.zero : targetComposerShift,
                    planarCameraComposer.Composition.ScreenPosition, t);

            if (Screen.height > Screen.width) {
                currentSensorSize.Set(sensorSize, sensorSize * ((float)Screen.height / (float)Screen.width));
            }
            else {
                currentSensorSize.Set(sensorSize * ((float)Screen.width / (float)Screen.height), sensorSize);
            }

            targetLensShift.y = Mathf.LerpUnclamped(lensShiftRanges.x, lensShiftRanges.y,
                canvas.transform.InverseTransformPoint(visibilityReference.transform.position).y /
                canvas.pixelRect.height);

            currentLensShift = Vector2.Lerp(targetLensShift, currentLensShift, t);
            foreach (var cinemachineCamera in cinemachineCameras) {
                cinemachineCamera.Lens.PhysicalProperties.LensShift = currentLensShift;
                cinemachineCamera.Lens.PhysicalProperties.SensorSize = currentSensorSize;
            }
        }

        public void OnPointerDelta(InputAction.CallbackContext ctx) {
            if (!AllowRotation) return;
            Vector2 delta = ctx.ReadValue<Vector2>();
            delta.x /= Screen.width;
            delta.y /= -Screen.height;
            if (!show2DFunction) {
                targetComposerShift += delta * NetworkHost.Instance.ZoomScale;
                targetComposerShift.Set(Mathf.Clamp(targetComposerShift.x, -1f, 1f),
                    Mathf.Clamp(targetComposerShift.y, -1f, 1f));
            }

            delta *= 360f;
            targetRotation.x += delta.y;
            targetRotation.y += delta.x;
            targetRotation.x %= 360f;
            targetRotation.y %= 360f;
            if (show2DFunction) {
                target2DRotation.x -= delta.x;
                target2DRotation.x %= 360f;
            }
        }

        public void Toggle2DFunctionView() {
            Set2DFunctionView(!show2DFunction);
        }

        public void Set2DFunctionView(bool view) {
            show2DFunction = view;
            if (show2DFunction) {
                target2DRotation.Set(0f, 90f, 90f);
            }
            else {
                target2DRotation.Set(0f, 0f, 0f);
            }
        }

        public void ToggleFullscreen() {
            SetFullscreen(!isFullscreen);
        }

        public void SetFullscreen(bool fullscreen) {
            isFullscreen = fullscreen;
            OnShowDrawer?.Invoke(!isFullscreen);
        }
        
        public void ResetCamera() {
            const float stepSize = 45f;
            // targetRotation.x = Mathf.Round(targetRotation.x / stepSize) * stepSize;
            // targetRotation.y = Mathf.Round(targetRotation.y / stepSize) * stepSize;
            targetRotation.x = 0f;
            targetRotation.y = 0f;
            target2DRotation.x = Mathf.Round(target2DRotation.x / stepSize) * stepSize;
            targetComposerShift.Set(0f, 0f);
        }

        public void ClearCamera() {
            targetRotation.x = 0f;
            targetRotation.y = 0f;
            target2DRotation.x = 0f;
            targetComposerShift.Set(0f, 0f);
        }

    }
}