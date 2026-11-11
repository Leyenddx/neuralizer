using UnityEngine;

namespace Neuralizer.UI {
    
    /// <summary>
    /// Componente que "dibuja" una linea entre dos elementos
    /// </summary>
    [ExecuteInEditMode]
    public class UILine : MonoBehaviour {
        
        /// <summary>
        /// El grosor de la linea
        /// </summary>
        [SerializeField] private float thickness = 5f;
        /// <summary>
        /// El punto de partida de la linea
        /// </summary>
        [SerializeField] private RectTransform from;
        /// <summary>
        /// El punto final de la linea
        /// </summary>
        [SerializeField] private RectTransform to;
        /// <summary>
        /// El contenedor de la linea
        /// </summary>
        [SerializeField] private RectTransform rectTransform;
        /// <summary>
        /// El Canvas que contiene todos los elementos que nos interesan
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/Canvas.html"/>
        [SerializeField] private Canvas canvas;

        /// <summary>
        /// Función de Unity llamada en cada cambio detectado en la interfaz
        /// </summary>
        private void OnGUI() {
            if (!from) return;
            if (!to) return;
            if (!rectTransform) return;

            if (from.position.x > to.position.x) {
                (from, to) = (to, from);
            }

            if (!from.gameObject.activeSelf || !to.gameObject.activeSelf) return;
            
            rectTransform.position = (from.position + to.position) / 2f;
            Vector3 dif = to.position - from.position;
            rectTransform.sizeDelta = new Vector3(dif.magnitude / canvas.scaleFactor, thickness);

            // Si ambos extremos coinciden (p. ej. antes de que el layout se calcule), el ángulo
            // no está definido: Atan(0/0) daba NaN y Quaternion.Euler(NaN) disparaba una aserción.
            if (dif.sqrMagnitude < 1e-6f) return;

            // Atan2 da el mismo ángulo que Atan(y/x) para x >= 0, pero sin dividir entre cero.
            rectTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dif.y, dif.x) * Mathf.Rad2Deg);
        }

#if UNITY_EDITOR
        private void OnValidate() {
            rectTransform ??= GetComponent<RectTransform>();
            canvas ??= rectTransform.GetComponentInParent<Canvas>();
        }
#endif
    }
}