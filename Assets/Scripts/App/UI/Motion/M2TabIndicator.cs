using UnityEngine;

namespace Neuralizer.UI {

    /// <summary>
    /// Indicador de pestaña activa que se desliza (220 ms ease-in-out) hasta la pestaña elegida, en
    /// vez de que el resaltado "salte" de golpe — así se entiende de dónde viene y a dónde va.
    /// </summary>
    /// <remarks>
    /// Va en el contenedor de pestañas. <see cref="indicator"/> debe ser hijo de ese contenedor con un
    /// <c>LayoutElement.ignoreLayout = true</c> (si no, el LayoutGroup lo trataría como otra pestaña) y
    /// estar primero en la jerarquía para dibujarse detrás de los textos.
    /// </remarks>
    public class M2TabIndicator : MonoBehaviour {

        [SerializeField] private RectTransform indicator;

        private RectTransform target;
        private Vector2 fromPos, fromSize;
        private float start = -1f;

        public void SetIndicator(RectTransform rect) => indicator = rect;

        /// <summary>Mueve el indicador a <paramref name="tab"/>. Sin animación la primera vez.</summary>
        public void MoveTo(RectTransform tab, bool instant = false) {
            if (!indicator) return;
            bool first = !target;
            target = tab;
            fromPos = indicator.anchoredPosition;
            fromSize = indicator.sizeDelta;
            start = instant || first || M2Motion.ReducedMotion ? -1f : Time.unscaledTime;
            if (start < 0f) Snap();
        }

        private void LateUpdate() {
            if (!indicator || !target) return;
            if (start < 0f) {
                // Sigue a la pestaña si el layout cambia de tamaño (rotación, resolución).
                Snap();
                return;
            }
            GetTargetRect(out Vector2 pos, out Vector2 size);
            float p = Mathf.Clamp01((Time.unscaledTime - start) / M2Ease.Move);
            float e = M2Ease.InOut(p);
            indicator.anchoredPosition = Vector2.LerpUnclamped(fromPos, pos, e);
            indicator.sizeDelta = Vector2.LerpUnclamped(fromSize, size, e);
            if (p >= 1f) start = -1f;
        }

        private void Snap() {
            GetTargetRect(out Vector2 pos, out Vector2 size);
            indicator.anchoredPosition = pos;
            indicator.sizeDelta = size;
        }

        /// <summary>Centro y tamaño de la pestaña en el espacio del contenedor (anclas del indicador al centro).</summary>
        private void GetTargetRect(out Vector2 pos, out Vector2 size) {
            RectTransform container = (RectTransform)indicator.parent;
            indicator.anchorMin = indicator.anchorMax = new Vector2(0.5f, 0.5f);
            indicator.pivot = new Vector2(0.5f, 0.5f);
            Vector3 world = target.TransformPoint(target.rect.center);
            Vector2 local = container.InverseTransformPoint(world);
            pos = local - container.rect.center;
            size = target.rect.size;
        }
    }
}
