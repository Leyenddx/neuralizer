using UnityEngine;
using UnityEngine.UI;

namespace Neuralizer.UI {

    /// <summary>
    /// Halo de un solo disparo (escala 1→1.35 mientras se desvanece, 500 ms ease-out). Reservado para
    /// momentos raros — resolver un nivel — que es donde las guías de movimiento permiten "delight".
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class M2Pulse : MonoBehaviour {

        private const float Duration = 0.5f;

        [SerializeField] private float startAlpha = 0.75f;
        [SerializeField] private float endScale = 1.35f;

        private Graphic graphic;
        private float start = -1f;

        private void Awake() {
            graphic = GetComponent<Graphic>();
            SetAlpha(0f);
        }

        public void Play(Color color) {
            if (!graphic) graphic = GetComponent<Graphic>();
            Color c = color;
            c.a = startAlpha;
            graphic.color = c;
            start = Time.unscaledTime;
            transform.localScale = Vector3.one;
        }

        private void Update() {
            if (start < 0f) return;
            float p = Mathf.Clamp01((Time.unscaledTime - start) / Duration);
            float e = M2Ease.Out(p);
            float s = M2Motion.ReducedMotion ? 1f : Mathf.Lerp(1f, endScale, e);
            transform.localScale = new Vector3(s, s, 1f);
            SetAlpha(Mathf.Lerp(startAlpha, 0f, e));
            if (p >= 1f) start = -1f;
        }

        private void SetAlpha(float a) {
            Color c = graphic.color;
            c.a = a;
            graphic.color = c;
        }
    }
}
