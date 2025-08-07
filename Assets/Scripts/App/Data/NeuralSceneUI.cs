using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer.Data {
    public class NeuralSceneUI : MonoBehaviour {
        [SerializeField] private NeuralScene scene;

        [SerializeField] private UnityEvent<string> OnSceneName;
        [SerializeField] private UnityEvent<bool> OnAllowScoring;
        [SerializeField] private UnityEvent<bool> OnCompleted;

        public NeuralScene Scene {
            get => scene;
            set {
                if (scene && enabled) {
                    scene.OnIsCompleted -= OnCompletedCallback;
                }
                scene = value;
                if (!value) return;
                if (enabled) {
                    scene.OnIsCompleted += OnCompletedCallback;
                }
                OnCompletedCallback(value.IsCompleted);
                OnSceneName?.Invoke(scene.DisplayName);
                OnAllowScoring?.Invoke(scene.AllowScoring);
            }
        }

        private void OnEnable() {
            Scene = Scene;
            if (Scene) {
                Scene.OnIsCompleted += OnCompletedCallback;
            }
        }

        private void OnDisable() {
            if (Scene) {
                Scene.OnIsCompleted -= OnCompletedCallback;
            }
        }

        private void OnCompletedCallback(bool value) {
            OnCompleted?.Invoke(value);
        }

        public void LoadScene() {
            if (!Scene) return;
            NetworkHost.Instance.Scene = Scene;
        }
    }
}