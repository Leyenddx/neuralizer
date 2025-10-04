using UnityEngine;

namespace Neuralizer.UI {
    /// <summary>
    /// Clase que detecta y ajusta un RectTransform para utilizar la zona segura de un dispositivo movil
    /// </summary>
    /// <seealso href="https://docs.unity3d.com/ScriptReference/Device.Screen-safeArea.html"/>
    /// <seealso href="https://docs.unity3d.com/ScriptReference/RectTransform.html"/>
    [ExecuteInEditMode]
    public class SafeArea : MonoBehaviour {
        /// <summary>
        /// El RectTransform a ajustar
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/RectTransform.html"/>
        [SerializeField] private RectTransform rectTransform;

        private Rect safeArea;
        private Vector2 minAnchor;
        private Vector2 maxAnchor;

        private bool ignoreCallback = false;

        /// <summary>
        /// Evento de Unity llamado cuando hay un cambio en las dimensiones de <see cref="rectTransform"/> para llamar automáticamente la función <see cref="ResizeToFit"/>
        /// </summary>
        private void OnRectTransformDimensionsChange() {
            if (ignoreCallback) {
                ignoreCallback = false;
                return;
            }

            ResizeToFit();
        }

        void Awake() {
            ResizeToFit();
        }

        /// <summary>
        /// Calcula las anclas de acuerdo a la zona segura del dispocitivo
        /// </summary>
        private void ResizeToFit() {
            safeArea = Screen.safeArea;
            minAnchor = safeArea.position;
            maxAnchor = minAnchor + safeArea.size;

            minAnchor.x /= Screen.width;
            minAnchor.y /= Screen.height;
            maxAnchor.x /= Screen.width;
            maxAnchor.y /= Screen.height;

#if UNITY_EDITOR
            if (float.IsNaN(minAnchor.x) ||
                float.IsNaN(minAnchor.y) ||
                float.IsNaN(maxAnchor.x) ||
                float.IsNaN(maxAnchor.y) ||
                float.IsInfinity(minAnchor.x) ||
                float.IsInfinity(minAnchor.y) ||
                float.IsInfinity(maxAnchor.x) ||
                float.IsInfinity(maxAnchor.y)) {
                ignoreCallback = true;
                rectTransform.anchorMin = Vector2.zero;
                ignoreCallback = true;
                rectTransform.anchorMax = Vector2.one;
                return;
            }
#endif
            ignoreCallback = true;
            rectTransform.anchorMin = minAnchor;
            ignoreCallback = true;
            rectTransform.anchorMax = maxAnchor;
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!rectTransform) {
                rectTransform = GetComponent<RectTransform>();
            }
        }
#endif
    }
}