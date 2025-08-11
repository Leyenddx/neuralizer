using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer {
    public class EnabledListener : MonoBehaviour {

        [SerializeField] private UnityEvent<bool> OnEnabledChanged;
        [SerializeField] private UnityEvent<bool> OnDisabledChanged;
        [SerializeField] private UnityEvent OnEnabled;
        [SerializeField] private UnityEvent OnDisabled;

        private void OnEnable() {
            OnEnabledChanged?.Invoke(true);
            OnDisabledChanged?.Invoke(false);
            OnEnabled?.Invoke();
        }

        private void OnDisable() {
            OnEnabledChanged?.Invoke(false);
            OnDisabledChanged?.Invoke(true);
            OnDisabled?.Invoke();
        }
    }
}