using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Neuralizer.UI {

    /// <summary>
    /// Gráfica de línea poligonal ligera para UI (ej. curvas de funciones de pertenencia).
    /// Genera su malla solo cuando los puntos cambian a través de <see cref="SetPoints"/>,
    /// no en cada frame, para no pesar en builds WebGL.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIPolyline : MaskableGraphic {

        [SerializeField] private float thickness = 3f;

        /// <summary>
        /// Grosor de la línea en píxeles de UI
        /// </summary>
        public float Thickness {
            get => thickness;
            set {
                thickness = value;
                SetVerticesDirty();
            }
        }

        private readonly List<Vector2> points = new();

        /// <summary>
        /// Reemplaza los puntos de la curva (en coordenadas locales del RectTransform, origen
        /// en la esquina inferior izquierda si el pivote está en (0,0)) y la redibuja.
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
                Vector2 dir = (b - a);
                if (dir.sqrMagnitude < 0.0001f) continue;
                dir.Normalize();
                Vector2 normal = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);

                int idx = vh.currentVertCount;
                vh.AddVert(a - normal, color, Vector2.zero);
                vh.AddVert(a + normal, color, Vector2.zero);
                vh.AddVert(b + normal, color, Vector2.zero);
                vh.AddVert(b - normal, color, Vector2.zero);

                vh.AddTriangle(idx, idx + 1, idx + 2);
                vh.AddTriangle(idx, idx + 2, idx + 3);
            }
        }
    }
}
