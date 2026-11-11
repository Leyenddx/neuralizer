using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Neuralizer.UI {

    /// <summary>
    /// Relleno de área bajo una curva para UI (ej. el área de una función de pertenencia difusa,
    /// para que se lea de un vistazo "qué tan Bajo/Alto" es un valor, no solo la línea del borde).
    /// Dibuja un abanico de triángulos entre cada punto de la curva y una línea base horizontal
    /// (normalmente y = 0, la parte inferior del área del gráfico). Pensado para usarse junto a un
    /// <see cref="UIPolyline"/> que dibuje el borde nítido encima del relleno (el relleno solo, sin
    /// borde, se ve un poco "blando" en los bordes por el alpha bajo).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIFilledCurve : MaskableGraphic {

        [SerializeField] private float baseline = 0f;

        /// <summary>Altura (en coordenadas locales del RectTransform) de la línea base del relleno.</summary>
        public float Baseline {
            get => baseline;
            set {
                baseline = value;
                SetVerticesDirty();
            }
        }

        private readonly List<Vector2> points = new();

        /// <summary>
        /// Reemplaza los puntos de la curva superior (mismo formato que <see cref="UIPolyline.SetPoints"/>)
        /// y redibuja el relleno entre esos puntos y <see cref="Baseline"/>.
        /// </summary>
        public void SetPoints(IReadOnlyList<Vector2> newPoints) {
            points.Clear();
            points.AddRange(newPoints);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            if (points.Count < 2) return;

            for (int i = 0; i < points.Count - 1; i++) {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];

                int idx = vh.currentVertCount;
                vh.AddVert(new Vector3(a.x, baseline), color, Vector2.zero);
                vh.AddVert(new Vector3(a.x, a.y), color, Vector2.zero);
                vh.AddVert(new Vector3(b.x, b.y), color, Vector2.zero);
                vh.AddVert(new Vector3(b.x, baseline), color, Vector2.zero);

                vh.AddTriangle(idx, idx + 1, idx + 2);
                vh.AddTriangle(idx, idx + 2, idx + 3);
            }
        }
    }
}
