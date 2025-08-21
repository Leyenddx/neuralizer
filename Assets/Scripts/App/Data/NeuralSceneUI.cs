using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer.Data {
    /// <summary>
    /// Componente de visualización de <see cref="NeuralScene"/>
    /// </summary>
    /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.html"/>
    public class NeuralSceneUI : MonoBehaviour {
        /// <summary>
        /// La escena que queremos visualizar
        /// </summary>
        [SerializeField] private NeuralScene scene;
        /// <summary>
        /// El contenedor de medallas que queremos utilizar
        /// </summary>
        [SerializeField] private MedalsAsset medals;
        /// <summary>
        /// Evento para mostrar <see cref="NeuralScene.DisplayName"/>
        /// </summary>
        [SerializeField] private UnityEvent<string> OnSceneName;
        /// <summary>
        /// Evento para mostrar <see cref="NeuralScene.AllowScoring"/>
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnAllowScoring;
        /// <summary>
        /// Evento para mostrar <see cref="NeuralScene.IsCompleted"/>
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnCompleted;
        /// <summary>
        /// Evento para mostrar <see cref="NeuralScene.FormattedTime"/>
        /// </summary>
        [SerializeField] private UnityEvent<string> OnTimeString;
        /// <summary>
        /// Evento para mostrar <see cref="NeuralScene.Score"/>
        /// </summary>
        [SerializeField] private UnityEvent<string> OnScoreString;
        /// <summary>
        /// Evento para mostrar <see cref="NeuralScene.Ratio"/>
        /// </summary>
        [SerializeField] private UnityEvent<string> OnRatioString;
        /// <summary>
        /// Evento para mostrar si hay una medalla obtenida
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnShowMedal;
        /// <summary>
        /// Evento para mostrar <see cref="Medal.name"/>
        /// </summary>
        [SerializeField] private UnityEvent<string> OnMedalName;
        /// <summary>
        /// Evento para mostrar <see cref="Medal.sprite"/>
        /// </summary>
        [SerializeField] private UnityEvent<Sprite> OnMedalSprite;
        /// <summary>
        /// Evento para mostrar <see cref="Medal.description"/>
        /// </summary>
        [SerializeField] private UnityEvent<string> OnMedalDescription;

        /// <summary>
        /// La escena que queremos visualizar. Cambiar este valor dispara los eventos correspondientes
        /// </summary>
        /// <seealso cref="scene"/>
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

        /// <summary>
        /// Esta función se llama cuando el objeto ha sido activado
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnEnable.html"/>
        private void OnEnable() {
            Scene = Scene;
            if (Scene) {
                Scene.OnUpdated += OnUpdated;
            }
        }


        /// <summary>
        /// Esta función se llama cuando el objeto ha sido desactivado
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDisable.html"/>
        private void OnDisable() {
            if (Scene) {
                Scene.OnUpdated -= OnUpdated;
            }
        }

        /// <summary>
        /// Función que es llamada cuando el evento <see cref="NeuralScene.OnUpdated"/> ha sido disparado
        /// </summary>
        private void OnUpdated() {
            bool completed = scene.IsCompleted;
            if(completed && scene.AllowScoring)
            {
                OnTimeString?.Invoke(Scene.FormattedTime);
                OnScoreString?.Invoke(Scene.Score.ToString());
                OnRatioString?.Invoke(Scene.FormattedRatio);
                OnShowMedal?.Invoke(true);
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

        /// <summary>
        /// Carga la escena actual (<see cref="Scene"/>) a la red
        /// </summary>
        /// <seealso cref="Neuralizer.NetworkHost"/>
        public void LoadScene() {
            if (!Scene) return;
            NetworkHost.Instance.Scene = Scene;
        }
    }
}