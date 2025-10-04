using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.UI;

namespace Neuralizer {
    /// <summary>
    /// Componente que inicializa la interfaz y ajustes necesarios para iniciar correctamente la aplicación
    /// </summary>
    public class BootManager : MonoBehaviour {

        /// <summary>
        /// Componente a animar que contiene el logo
        /// </summary>
        [SerializeField] private LayoutElement logo;
        /// <summary>
        /// Velocidad de la animación de entrada
        /// </summary>
        [SerializeField] private float fadeSpeed = Mathf.PI;
        /// <summary>
        /// Velocidad de la animación de escalado
        /// </summary>
        [SerializeField] private float scaleSpeed = 1f;
        /// <summary>
        /// Rango de valores a animar en el escalado
        /// </summary>
        [SerializeField] private Vector2 scaleRange = new(0.95f, 1.05f);
        /// <summary>
        /// Evento que manda la versión de la aplicación a algún elemento de la interfaz
        /// </summary>
        [SerializeField] private UnityEvent<string> OnVersionText;

        private bool animateScale = true;
        
        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html"/>
        private void Start() {
            logo.flexibleHeight = 270f;
            OnVersionText?.Invoke("v" + Application.version);
            StartCoroutine(AnimateScale());
        }

        /// <summary>
        /// Inicializa la animación de entrada
        /// </summary>
        public void StartFadeIn() {
            animateScale = false;
            StartCoroutine(FadeIn());
        }

        /// <summary>
        /// Rutina que se encarga de la animación de escalado
        /// </summary>
        /// <returns>null por cada cuadro que ejecuta esta rutina</returns>
        IEnumerator AnimateScale() {
            while (animateScale) {
                logo.transform.localScale = Vector3.one * Mathf.LerpUnclamped(scaleRange.x, scaleRange.y, (Mathf.Sin(Time.unscaledTime * scaleSpeed) + 1f) / 2f);
                yield return null;
            }
            logo.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Rutina que se encarga de la animación de entrada
        /// </summary>
        /// <returns>null por cada cuadro que ejecuta esta rutina</returns>
        IEnumerator FadeIn() {
            do {
                logo.flexibleHeight =
                    Mathf.LerpUnclamped(0f, logo.flexibleHeight, Mathf.Exp(-fadeSpeed * Time.unscaledDeltaTime));
                yield return null;
            } while (logo.flexibleHeight > 0.025f);
            logo.flexibleHeight = 0f;
        }

    }
}