using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.UI;

namespace Neuralizer {
    public class BootManager : MonoBehaviour {

        [SerializeField] private LayoutElement logo;
        [SerializeField] private float fadeSpeed = Mathf.PI;
        [SerializeField] private float scaleSpeed = 1f;
        [SerializeField] private Vector2 scaleRange = new(0.95f, 1.05f);
        [SerializeField] private UnityEvent<string> OnVersionText;

        private bool animateScale = true;
        
        private void Start() {
            logo.flexibleHeight = 270f;
            OnVersionText?.Invoke("v" + Application.version);
            StartCoroutine(AnimateScale());
        }

        public void StartFadeIn() {
            animateScale = false;
            StartCoroutine(FadeIn());
        }

        IEnumerator AnimateScale() {
            while (animateScale) {
                logo.transform.localScale = Vector3.one * Mathf.LerpUnclamped(scaleRange.x, scaleRange.y, (Mathf.Sin(Time.unscaledTime * scaleSpeed) + 1f) / 2f);
                yield return null;
            }
            logo.transform.localScale = Vector3.one;
        }

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