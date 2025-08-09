using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer.Data {
    public class NeuralSceneUI : MonoBehaviour {
        [SerializeField] private NeuralScene scene;

        [SerializeField] private MedalsAsset medals;
        [SerializeField] private UnityEvent<string> OnSceneName;
        [SerializeField] private UnityEvent<bool> OnAllowScoring;
        [SerializeField] private UnityEvent<bool> OnCompleted;
        [SerializeField] private UnityEvent<string> OnScoreString;
        [SerializeField] private UnityEvent<bool> OnShowMedal;
        [SerializeField] private UnityEvent<Sprite> OnMedalSprite;

        public NeuralScene Scene {
            get => scene;
            set {
                if (scene && enabled) {
                    scene.OnUpdated -= OnUpdated;
                }
                scene = value;
                if (!value) return;
                if (enabled) {
                    scene.OnUpdated += OnUpdated;
                }

                OnUpdated();
                OnSceneName?.Invoke(scene.DisplayName);
                OnAllowScoring?.Invoke(scene.AllowScoring);
            }
        }

        private void OnEnable() {
            Scene = Scene;
            if (Scene) {
                Scene.OnUpdated += OnUpdated;
            }
        }

        private void OnDisable() {
            if (Scene) {
                Scene.OnUpdated -= OnUpdated;
            }
        }

        private void OnUpdated() {
            bool completed = scene.IsCompleted;
            OnCompleted?.Invoke(completed);
            OnScoreString?.Invoke(completed ? scene.Score.ToString() : "---");
            OnShowMedal?.Invoke(scene.AllowScoring && completed);
            OnMedalSprite?.Invoke(medals.GetFromRatio(scene.NormalizedScore));
        }

        public void LoadScene() {
            if (!Scene) return;
            NetworkHost.Instance.Scene = Scene;
        }
    }
}