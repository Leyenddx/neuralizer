using UnityEngine;

namespace Neuralizer.UI {

    /// <summary>
    /// Curvas y duraciones de movimiento del Módulo 2 (ver MODULO2_DISENO.md §4). Las curvas son las
    /// recomendadas por las guías de animación de Emil Kowalski — más "decididas" que las de fábrica.
    /// Nunca se usa ease-in en UI: arranca lento justo cuando el usuario está mirando.
    /// </summary>
    public static class M2Ease {

        /// <summary>Respuesta al toque (presionar).</summary>
        public const float Press = 0.12f;
        /// <summary>Soltar: un poco más lento que presionar para que se sienta elástico.</summary>
        public const float Release = 0.16f;
        /// <summary>Halo / color al pasar el puntero.</summary>
        public const float Hover = 0.15f;
        /// <summary>Entrada de un elemento de lista.</summary>
        public const float Enter = 0.26f;
        /// <summary>Separación entre elementos escalonados (30–80 ms recomendado).</summary>
        public const float Stagger = 0.045f;
        /// <summary>Apertura de modal.</summary>
        public const float ModalIn = 0.24f;
        /// <summary>Cierre de modal: la salida siempre más rápida que la entrada.</summary>
        public const float ModalOut = 0.15f;
        /// <summary>Indicador de pestaña desplazándose.</summary>
        public const float Move = 0.22f;

        /// <summary>ease-out fuerte: cubic-bezier(0.23, 1, 0.32, 1). Entradas, salidas, feedback.</summary>
        public static float Out(float t) => CubicBezier(0.23f, 1f, 0.32f, 1f, t);

        /// <summary>ease-in-out fuerte: cubic-bezier(0.77, 0, 0.175, 1). Movimiento sobre pantalla.</summary>
        public static float InOut(float t) => CubicBezier(0.77f, 0f, 0.175f, 1f, t);

        /// <summary>
        /// Evalúa una cubic-bezier CSS (P0 = 0,0 y P3 = 1,1) en x = <paramref name="x"/>: resuelve el
        /// parámetro t con Newton-Raphson (cae a bisección si la derivada es casi 0) y devuelve y(t).
        /// </summary>
        public static float CubicBezier(float x1, float y1, float x2, float y2, float x) {
            if (x <= 0f) return 0f;
            if (x >= 1f) return 1f;

            float t = x;
            for (int i = 0; i < 8; i++) {
                float err = Bezier(x1, x2, t) - x;
                if (Mathf.Abs(err) < 1e-5f) break;
                float d = BezierDerivative(x1, x2, t);
                if (Mathf.Abs(d) < 1e-6f) break;
                t -= err / d;
            }

            // Bisección de respaldo si Newton no convergió dentro de [0, 1].
            if (t < 0f || t > 1f || Mathf.Abs(Bezier(x1, x2, t) - x) > 1e-3f) {
                float lo = 0f, hi = 1f;
                t = x;
                for (int i = 0; i < 24; i++) {
                    float v = Bezier(x1, x2, t);
                    if (Mathf.Abs(v - x) < 1e-5f) break;
                    if (v < x) lo = t; else hi = t;
                    t = (lo + hi) * 0.5f;
                }
            }
            return Bezier(y1, y2, t);
        }

        private static float Bezier(float p1, float p2, float t) {
            float u = 1f - t;
            return 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t;
        }

        private static float BezierDerivative(float p1, float p2, float t) {
            float u = 1f - t;
            return 3f * u * u * p1 + 6f * u * t * (p2 - p1) + 3f * t * t * (1f - p2);
        }
    }
}
