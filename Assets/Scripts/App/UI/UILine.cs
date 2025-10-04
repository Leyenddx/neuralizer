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
            rectTransform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 180f * Mathf.Atan(dif.y / dif.x) / Mathf.PI));
        }

#if UNITY_EDITOR
        private void OnValidate() {
            rectTransform ??= GetComponent<RectTransform>();
            canvas ??= rectTransform.GetComponentInParent<Canvas>();
        }
#endif
    }
}