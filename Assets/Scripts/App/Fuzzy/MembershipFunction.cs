using UnityEngine;

namespace Neuralizer.Fuzzy {

    /// <summary>
    /// Forma de la función de pertenencia. Trapezoidal cubre también el caso triangular (b == c).
    /// </summary>
    public enum MembershipShape {
        Trapezoidal,
        Sigmoide,
        SigmoideInversa,
        Gaussiana
    }

    /// <summary>
    /// Función de pertenencia difusa. Soporta varias formas —trapezoidal/triangular, sigmoide,
    /// sigmoide inversa y gaussiana— seleccionables por <see cref="shape"/>. Pensado para que el
    /// tipo de curva sea un parámetro que se pueda cambiar en vivo (por ejemplo desde un dropdown
    /// en la UI), no algo fijo por código.
    /// </summary>
    /// <remarks>
    /// Trapezoidal (un triángulo es el caso particular donde <see cref="b"/> == <see cref="c"/>):
    /// <code>
    ///        b───c
    ///       ╱     ╲
    ///      ╱       ╲
    ///     a         d
    /// </code>
    /// Sigmoide / Sigmoide inversa / Gaussiana usan <see cref="center"/> y <see cref="spread"/> en
    /// vez de a/b/c/d — usar los métodos estáticos de fábrica para construir cada forma en vez de
    /// llenar los campos a mano.
    /// </remarks>
    [System.Serializable]
    public struct MembershipFunction {

        public MembershipShape shape;

        // --- Trapezoidal / Triangular ---

        /// <summary>Punto donde el grado de pertenencia empieza a subir desde 0 (trapezoidal)</summary>
        public float a;

        /// <summary>Punto donde el grado de pertenencia llega a 1 subiendo (trapezoidal)</summary>
        public float b;

        /// <summary>Punto donde el grado de pertenencia empieza a bajar desde 1 (trapezoidal)</summary>
        public float c;

        /// <summary>Punto donde el grado de pertenencia vuelve a 0 (trapezoidal)</summary>
        public float d;

        // --- Sigmoide / Sigmoide inversa / Gaussiana ---

        /// <summary>Punto de inflexión (sigmoide/sigmoide inversa) o media (gaussiana)</summary>
        public float center;

        /// <summary>
        /// Qué tan abrupta es la transición (sigmoide) o ancho de la campana (gaussiana). Nunca debe
        /// quedar en 0 — <see cref="Evaluate"/> se protege de eso, pero visualmente un spread muy
        /// chico se ve como un escalón vertical.
        /// </summary>
        public float spread;

        public MembershipFunction(float a, float b, float c, float d) {
            shape = MembershipShape.Trapezoidal;
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
            center = 0f;
            spread = 1f;
        }

        /// <summary>Crea una función de pertenencia triangular (b == c == pico)</summary>
        public static MembershipFunction Triangular(float a, float peak, float d) {
            return new MembershipFunction(a, peak, peak, d);
        }

        /// <summary>Crea una función de pertenencia trapezoidal explícita</summary>
        public static MembershipFunction Trapezoidal(float a, float b, float c, float d) {
            return new MembershipFunction(a, b, c, d);
        }

        /// <summary>
        /// Sigmoide (forma de S ascendente): sube de 0 a 1 alrededor de <paramref name="center"/>.
        /// <paramref name="spread"/> más grande = transición más abrupta.
        /// </summary>
        public static MembershipFunction Sigmoide(float center, float spread) {
            return new MembershipFunction {
                shape = MembershipShape.Sigmoide,
                center = center,
                spread = spread
            };
        }

        /// <summary>
        /// Sigmoide invertida (forma de S descendente, a veces llamada "Z"): baja de 1 a 0 alrededor
        /// de <paramref name="center"/>.
        /// </summary>
        public static MembershipFunction SigmoideInversa(float center, float spread) {
            return new MembershipFunction {
                shape = MembershipShape.SigmoideInversa,
                center = center,
                spread = spread
            };
        }

        /// <summary>
        /// Campana gaussiana centrada en <paramref name="center"/> con ancho <paramref name="spread"/>
        /// (equivalente a la desviación estándar — spread más grande = campana más ancha).
        /// </summary>
        public static MembershipFunction Gaussiana(float center, float spread) {
            return new MembershipFunction {
                shape = MembershipShape.Gaussiana,
                center = center,
                spread = spread
            };
        }

        /// <summary>
        /// Evalúa el grado de pertenencia [0, 1] de <paramref name="x"/> a esta función
        /// </summary>
        public readonly float Evaluate(float x) {
            return shape switch {
                MembershipShape.Sigmoide => EvaluateSigmoide(x),
                MembershipShape.SigmoideInversa => 1f - EvaluateSigmoide(x),
                MembershipShape.Gaussiana => EvaluateGaussiana(x),
                _ => EvaluateTrapezoidal(x)
            };
        }

        private readonly float EvaluateTrapezoidal(float x) {
            // Fuera del soporte vale 0. Los BORDES se resuelven abajo: con comparaciones estrictas, un
            // "hombro" (a == b o c == d) vale 1 justo en su borde (antes valía 0: salud 0 no era
            // "Critical"), y una rampa normal sigue valiendo 0 en a y en d.
            if (x < a || x > d) return 0f;
            if (x < b) return b > a ? (x - a) / (b - a) : 1f;
            if (x <= c) return 1f;
            return d > c ? (d - x) / (d - c) : 1f;
        }

        private readonly float EvaluateSigmoide(float x) {
            float s = Mathf.Approximately(spread, 0f) ? 0.0001f : spread;
            return 1f / (1f + Mathf.Exp(-s * (x - center)));
        }

        private readonly float EvaluateGaussiana(float x) {
            float s = Mathf.Approximately(spread, 0f) ? 0.0001f : spread;
            float t = (x - center) / s;
            return Mathf.Exp(-0.5f * t * t);
        }
    }
}
