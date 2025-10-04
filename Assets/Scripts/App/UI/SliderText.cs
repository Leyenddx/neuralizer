using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neuralizer.UI {
    /// <summary>
    /// Componente que sincroniza el valor de un Slider a un elemento de texto
    /// </summary>
    /// <seealso href="https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Slider.html"/>
    [ExecuteInEditMode]
    public class SliderText : MonoBehaviour {

        /// <summary>
        /// El Slider que nos interesa
        /// </summary>
        [SerializeField] private Slider slider;

        /// <summary>
        /// Evento con el valor del Slider representado en forma de string
        /// </summary>
        [SerializeField] private UnityEvent<string> OnTextUpdate;

        /// <summary>
        /// Función de Unity llamada en cada cambio detectado en la interfaz
        /// </summary>
        private void OnGUI() {
            if (slider) {

                OnTextUpdate?.Invoke((Mathf.Round((slider.value / 4f) * 100f) / 100f).ToString());

                //OnTextUpdate?.Invoke(slider.wholeNumbers
                //    ? Mathf.RoundToInt(slider.value).ToString()
                //    : (Mathf.Round(slider.value * 100f) / 100f).ToString());
            }
        }

#if UNITY_EDITOR
        private void OnValidate() {
            slider ??= GetComponent<Slider>();
        }
#endif

    }
}
