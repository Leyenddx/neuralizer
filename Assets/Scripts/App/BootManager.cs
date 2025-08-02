using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer {
    public class BootManager : MonoBehaviour {

        [SerializeField] private UnityEvent<string> OnVersionText;

        private void Start() {
            OnVersionText?.Invoke("v" + Application.version);
        }
    }
}