using UnityEngine;
using UnityEngine.UI;

namespace Neuralizer {
    public class NeuronToggleButton : MonoBehaviour {
        [SerializeField] private Toggle toggle;
        [SerializeField] private NetworkController controller;
        [SerializeField] private TargetNeuron target;
        [SerializeField] private LayoutElement layout;

        private const float speed = 24f;

        private float targetWidth = 1f;

        public Toggle Toggle => toggle;
        
        public TargetNeuron Target => target;

        public bool IsActive { get; private set; }

        public void OnToggleChanged(bool value) {
            IsActive = value;
            targetWidth = value ? 2f : 1f;
            controller.OnTargetNeuronChanged(this, value);
        }

        private void LateUpdate() {
            layout.flexibleHeight =
                Mathf.Lerp(targetWidth, layout.flexibleHeight, Mathf.Exp(-speed * Time.unscaledDeltaTime));
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!layout) {
                layout = GetComponent<LayoutElement>();
            }
            if (!toggle) {
                toggle = GetComponent<Toggle>();
            }
        }
#endif
    }

    public enum TargetNeuron {
        None,
        AA,
        AB,
        AC,
        AD,
        BA,
        BB,
        BC,
        BD,
        Output
    }
}