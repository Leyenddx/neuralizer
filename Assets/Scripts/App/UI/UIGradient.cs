using UnityEngine;
using UnityEngine.UI;

namespace Neuralizer.UI {

    /// <summary>
    /// Tiñe los vértices del <see cref="Graphic"/> (normalmente un <see cref="Image"/>) al que está
    /// adjunto con un degradado entre dos colores, sin necesidad de un shader o material propio.
    /// Solo recalcula cuando el mesh se repuebla (cambio de tamaño, <c>SetVerticesDirty</c>), no en
    /// cada frame — barato para WebGL.
    /// </summary>
    /// <remarks>
    /// Pensado para <see cref="Image"/> y otros Graphics simples. Para texto TMP usar el degradado
    /// nativo de TextMeshPro (<c>enableVertexGradient</c> + <c>colorGradient</c>) en vez de este
    /// componente — ver <see cref="EsportsUIKit.CreateGradientText"/>.
    /// </remarks>
    [RequireComponent(typeof(Graphic))]
    public class UIGradient : BaseMeshEffect {

        public enum Direction { Vertical, Horizontal, Diagonal }

        [SerializeField] private Color colorA = Color.white;
        [SerializeField] private Color colorB = Color.black;
        [SerializeField] private Direction direction = Direction.Diagonal;

        public Color ColorA {
            get => colorA;
            set { colorA = value; graphic?.SetVerticesDirty(); }
        }

        public Color ColorB {
            get => colorB;
            set { colorB = value; graphic?.SetVerticesDirty(); }
        }

        public Direction GradientDirection {
            get => direction;
            set { direction = value; graphic?.SetVerticesDirty(); }
        }

        public override void ModifyMesh(VertexHelper vh) {
            if (!IsActive() || vh.currentVertCount == 0) return;

            UIVertex vertex = default;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < vh.currentVertCount; i++) {
                vh.PopulateUIVertex(ref vertex, i);
                minX = Mathf.Min(minX, vertex.position.x);
                maxX = Mathf.Max(maxX, vertex.position.x);
                minY = Mathf.Min(minY, vertex.position.y);
                maxY = Mathf.Max(maxY, vertex.position.y);
            }
            float width = Mathf.Max(maxX - minX, 0.0001f);
            float height = Mathf.Max(maxY - minY, 0.0001f);

            for (int i = 0; i < vh.currentVertCount; i++) {
                vh.PopulateUIVertex(ref vertex, i);
                float t = direction switch {
                    Direction.Horizontal => (vertex.position.x - minX) / width,
                    Direction.Vertical => 1f - (vertex.position.y - minY) / height,
                    _ => ((vertex.position.x - minX) / width + (1f - (vertex.position.y - minY) / height)) * 0.5f
                };
                Color32 blended = Color32.Lerp(colorA, colorB, Mathf.Clamp01(t));
                vertex.color = new Color32(
                    (byte)(blended.r * vertex.color.r / 255),
                    (byte)(blended.g * vertex.color.g / 255),
                    (byte)(blended.b * vertex.color.b / 255),
                    (byte)(blended.a * vertex.color.a / 255));
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
