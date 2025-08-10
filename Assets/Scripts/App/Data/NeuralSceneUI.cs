using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer.Data {
    public class NeuralSceneUI : MonoBehaviour {
        [SerializeField] private NeuralScene scene;

        [SerializeField] private MedalsAsset medals;
        [SerializeField] private UnityEvent<string> OnSceneName;
        [SerializeField] private UnityEvent<bool> OnAllowScoring;
        [SerializeField] private UnityEvent<bool> OnCompleted;
        [SerializeField] private UnityEvent<string> OnTimeString;
        [SerializeField] private UnityEvent<string> OnScoreString;
        [SerializeField] private UnityEvent<string> OnRatioString;
        [SerializeField] private UnityEvent<bool> OnShowMedal;
        [SerializeField] private UnityEvent<string> OnMedalName;
        [SerializeField] private UnityEvent<Sprite> OnMedalSprite;
        [SerializeField] private UnityEvent<string> OnMedalDescription;

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
            if(completed && scene.AllowScoring)
            {
                OnTimeString?.Invoke(Scene.FormattedTime);
                OnScoreString?.Invoke(Scene.Score.ToString());
                OnRatioString?.Invoke(Scene.FormattedRatio);
                OnShowMedal?.Invoke(true);
                Debug.Log(scene.DisplayName + ": " + scene.Ratio);
            }
            else {
                OnTimeString?.Invoke("---");
                OnScoreString?.Invoke("---");
                OnRatioString?.Invoke("---");
                OnShowMedal?.Invoke(false);
            }
            OnCompleted?.Invoke(completed);
            Medal medal = medals.GetFromRatio(Scene.NormalizedScore);
            if (medal != null) {
                OnMedalName?.Invoke(medal.name);
                OnMedalSprite?.Invoke(medal.sprite);
                OnMedalDescription?.Invoke(medal.description);
            } else {
                OnMedalName?.Invoke("");
                OnMedalSprite?.Invoke(null);
                OnMedalDescription?.Invoke("");
            }
        }

        public void LoadScene() {
            if (!Scene) return;
            NetworkHost.Instance.Scene = Scene;
        }
    }
}