using UnityEngine;
using UnityEngine.UI;

namespace Neuralizer.UI {

    /// <summary>
    /// Deriva ambiental muy lenta para las luces del fondo (decorativo, constante). Movimiento
    /// sinusoidal suave + "respiración" de opacidad. Amplitud pequeña y período largo a propósito: el
    /// fondo debe sentirse vivo sin competir con lo que el usuario está leyendo.
    /// </summary>
    public class M2Ambient : MonoBehaviour {

        [SerializeField] private Vector2 amplitude = new(40f, 30f);
        [SerializeField] private float period = 14f;
        [SerializeField] private float phase;
        [SerializeField] private float alphaMin = 0.18f;
        [SerializeField] private float alphaMax = 0.32f;

        private RectTransform rect;
        private Graphic graphic;
        private Vector2 origin;

        public void Configure(Vector2 amp, float periodSeconds, float phaseOffset, float minAlpha, float maxAlpha) {
            amplitude = amp;
            period = periodSeconds;
            phase = phaseOffset;
            alphaMin = minAlpha;
            alphaMax = maxAlpha;
        }

        private void Awake() {
            rect = (RectTransform)transform;
            graphic = GetComponent<Graphic>();
            origin = rect.anchoredPosition;
        }

        private void Update() {
            float t = (Time.unscaledTime / Mathf.Max(1f, period) + phase) * Mathf.PI * 2f;
            if (!M2Motion.ReducedMotion) {
                rect.anchoredPosition = origin + new Vector2(Mathf.Sin(t) * amplitude.x, Mathf.Sin(t * 0.7f + 1.3f) * amplitude.y);
            }
            if (graphic) {
                Color c = graphic.color;
                c.a = Mathf.Lerp(alphaMin, alphaMax, 0.5f + 0.5f * Mathf.Sin(t * 0.5f));
                graphic.color = c;
            }
        }
    }
}
