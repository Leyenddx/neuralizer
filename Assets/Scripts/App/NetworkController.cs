using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neuralizer {
    /// <summary>
    /// Componente que permite la manipulación de la red neuronal activa al usuario
    /// </summary>
    public class NetworkController : MonoBehaviour {
        /// <summary>
        /// Referencia al anfitrión de red que contiene la red que queremos manipular
        /// </summary>
        [SerializeField] private NetworkHost network;

        /// <summary>
        /// Propiedades de interfaz para la primera capa de la red
        /// </summary>
        [SerializeField] private LayoutElement aLayer;

        /// <summary>
        /// Propiedades de interfaz para la segunda capa de la red
        /// </summary>
        [SerializeField] private LayoutElement bLayer;

        /// <summary>
        /// Propiedades de interfaz para la neurona de salida
        /// </summary>
        [SerializeField] private LayoutElement outputLayer;

        /// <summary>
        /// Lista de botones conmutadores
        /// </summary>
        [SerializeField] private NeuronToggleButton[] toggles;

        /// <summary>
        /// Evento que transmite el primer peso de la neurona objetivo
        /// </summary>
        [SerializeField] private UnityEvent<float> OnWeightXLoaded;

        /// <summary>
        /// Evento que transmite el segundo peso de la neurona objetivo
        /// </summary>
        [SerializeField] private UnityEvent<float> OnWeightYLoaded;

        /// <summary>
        /// Evento que transmite el tercer peso de la neurona objetivo
        /// </summary>
        [SerializeField] private UnityEvent<float> OnWeightZLoaded;

        /// <summary>
        /// Evento que transmite el cuarto peso de la neurona objetivo
        /// </summary>
        [SerializeField] private UnityEvent<float> OnWeightWLoaded;

        /// <summary>
        /// Evento que transmite el sesgo de la neurona objetivo
        /// </summary>
        [SerializeField] private UnityEvent<float> OnBiasLoaded;

        /// <summary>
        /// Evento que transmite el índice de la función de activación de la neurona objetivo
        /// </summary>
        [SerializeField] private UnityEvent<int> OnActivationFunctionLoaded;

        /// <summary>
        /// Evento que se llama cuando es apropiado mostrar los ajustes de la neurona
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnShowSettings;

        /// <summary>
        /// Evento que índica si se debe mostrar la primera capa de neuronas
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnShowLayerA;

        /// <summary>
        /// Evento que índica si se debe mostrar la segunda capa de neuronas
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnShowLayerB;

        /// <summary>
        /// EVento que índica si se debe mostrar el primer peso de la neurona objetivo
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnShowXWeight;

        /// <summary>
        /// EVento que índica si se debe mostrar el segundo peso de la neurona objetivo
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnShowYWeight;

        /// <summary>
        /// EVento que índica si se debe mostrar el tercer peso de la neurona objetivo
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnShowZWeight;

        /// <summary>
        /// EVento que índica si se debe mostrar el cuarto peso de la neurona objetivo
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnShowWWeight;

        /// <summary>
        /// EVento que índica si se permite aleatorizar la neurona objetivo
        /// </summary>
        [SerializeField] private UnityEvent<bool> OnShowRandomizeNeuron;

        private const float speed = 24f;

        private bool isDirty = true;
        private Vector3 restFlexWidth = Vector3.one;
        private Vector3 targetFlexWidth = Vector3.one;
        private float flexT = 1f;

        private LayoutElement targetFlex;

        /// <summary>
        /// La neurona objetivo que se va a manipular
        /// </summary>
        public Neuron CurrentNeuron { get; set; }

        /// <summary>
        /// Localizador de la neurona objetivo que queremos manipular
        /// </summary>
        public TargetNeuron CurrentTargetNeuron { get; set; }

        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html"/>
        private void Start() {
            restFlexWidth.Set(aLayer.flexibleWidth, bLayer.flexibleWidth, outputLayer.flexibleWidth);
        }

        /// <summary>
        /// Evento llamado cuando una nueva escena ha sido cargada. Este debe ser explicitamente ligado a <see cref="Neuralizer.NetworkHost"/> desde el editor de Unity
        /// </summary>
        /// <seealso cref="Neuralizer.Data.NeuralScene"/>
        public void SceneLoaded() {
            switch (network.Scene.architecture) {
                case Architecture.Perceptron:
                    OnShowLayerA?.Invoke(false);
                    OnShowLayerB?.Invoke(false);
                break;
                case Architecture.OneLayer:
                    OnShowLayerA?.Invoke(true);
                    OnShowLayerB?.Invoke(false);
                break;
                case Architecture.TwoLayers:
                    OnShowLayerA?.Invoke(true);
                    OnShowLayerB?.Invoke(true);
                break;
            }

            foreach (NeuronToggleButton button in toggles) {
                button.Toggle.SetIsOnWithoutNotify(false);
                var prefab = network.Scene.GetTargetNeuron(button.Target);
                button.gameObject.SetActive(prefab?.displayNeuron ?? false);
                OnTargetNeuronChanged(button, false);
            }

            CurrentNeuron = null;
            isDirty = true;
        }

        /// <summary>
        /// Localiza y prepara la interfaz cuando hay un cambio de neurona objetivo. Esta función debe ser llamada por cada <see cref="Neuralizer.NeuronToggleButton"/> cuando su valor cambia
        /// </summary>
        /// <param name="button">El botón que llamó esta función</param>
        /// <param name="value">El valor del botón</param>
        public void OnTargetNeuronChanged(NeuronToggleButton button, bool value) {
            Neuron targetNeuron = network.GetTargetNeuron(button.Target);
            if (value) {
                CurrentNeuron = targetNeuron;
                CurrentTargetNeuron = button.Target;
                OnWeightXLoaded?.Invoke(CurrentNeuron.weights.x * 4f);
                OnWeightYLoaded?.Invoke(CurrentNeuron.weights.y * 4f);
                OnWeightZLoaded?.Invoke(CurrentNeuron.weights.z * 4f);
                OnWeightWLoaded?.Invoke(CurrentNeuron.weights.w * 4f);
                OnBiasLoaded?.Invoke(CurrentNeuron.bias * 4f);
                OnActivationFunctionLoaded?.Invoke((int)CurrentNeuron.activationFunction);
                switch (button.Target) {
                    case TargetNeuron.AA:
                    case TargetNeuron.AB:
                    case TargetNeuron.AC:
                    case TargetNeuron.AD:
                        OnShowXWeight?.Invoke(true);
                        OnShowYWeight?.Invoke(true);
                        OnShowZWeight?.Invoke(network.Scene.ZInputNeeded);
                        OnShowWWeight?.Invoke(false);
                        targetFlexWidth.Set(restFlexWidth.x * 2f, restFlexWidth.y, restFlexWidth.z);
                    break;
                    case TargetNeuron.BA:
                    case TargetNeuron.BB:
                    case TargetNeuron.BC:
                    case TargetNeuron.BD:
                        OnShowXWeight?.Invoke(network.Scene.AA.displayNeuron);
                        OnShowYWeight?.Invoke(network.Scene.AB.displayNeuron);
                        OnShowZWeight?.Invoke(network.Scene.AC.displayNeuron);
                        OnShowWWeight?.Invoke(network.Scene.AD.displayNeuron);
                        targetFlexWidth.Set(restFlexWidth.x, restFlexWidth.y * 2f, restFlexWidth.z);
                    break;
                    case TargetNeuron.Output:
                        targetFlexWidth.Set(restFlexWidth.x, restFlexWidth.y, restFlexWidth.z * 2f);
                        switch (network.Scene.architecture) {
                            case Architecture.Perceptron:
                                OnShowXWeight?.Invoke(true);
                                OnShowYWeight?.Invoke(true);
                                OnShowZWeight?.Invoke(network.Scene.ZInputNeeded);
                                OnShowWWeight?.Invoke(false);
                            break;
                            case Architecture.OneLayer:
                                OnShowXWeight?.Invoke(network.Scene.AA.displayNeuron);
                                OnShowYWeight?.Invoke(network.Scene.AB.displayNeuron);
                                OnShowZWeight?.Invoke(network.Scene.AC.displayNeuron);
                                OnShowWWeight?.Invoke(network.Scene.AD.displayNeuron);
                            break;
                            case Architecture.TwoLayers:
                                OnShowXWeight?.Invoke(network.Scene.BA.displayNeuron);
                                OnShowYWeight?.Invoke(network.Scene.BB.displayNeuron);
                                OnShowZWeight?.Invoke(network.Scene.BC.displayNeuron);
                                OnShowWWeight?.Invoke(network.Scene.BD.displayNeuron);
                            break;
                        }

                    break;
                }
            }
            else if (targetNeuron == CurrentNeuron) {
                CurrentNeuron = null;
            }

            isDirty = true;
        }

        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html"/>
        private void Update() {
            flexT = Mathf.Exp(-speed * Time.unscaledDeltaTime);
            aLayer.flexibleWidth = Mathf.Lerp(targetFlexWidth.x, aLayer.flexibleWidth, flexT);
            bLayer.flexibleWidth = Mathf.Lerp(targetFlexWidth.y, bLayer.flexibleWidth, flexT);
            outputLayer.flexibleWidth = Mathf.Lerp(targetFlexWidth.z, outputLayer.flexibleWidth, flexT);
        }

        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.LateUpdate.html"/>
        private void LateUpdate() {
            if (isDirty) {
                isDirty = false;
                if (CurrentNeuron == null) {
                    targetFlexWidth.Set(restFlexWidth.x, restFlexWidth.y, restFlexWidth.z);
                    OnShowSettings?.Invoke(false);
                    OnShowRandomizeNeuron?.Invoke(false);
                }
                else {
                    OnShowSettings?.Invoke(true);
                    OnShowRandomizeNeuron?.Invoke(network.Scene.allowRandomizeNeuron);
                }
            }
        }

        /// <summary>
        /// Función para cambiar el primer peso de la neurona objetivo desde la interfaz
        /// </summary>
        /// <param name="value">El nuevo peso</param>
        public void OnWeightXChanged(float value) {
            if (CurrentNeuron == null) return;
            CurrentNeuron.weights.x = value / 4f;
            network.NotifyChange();
        }

        /// <summary>
        /// Función para cambiar el segundo peso de la neurona objetivo desde la interfaz
        /// </summary>
        /// <param name="value">El nuevo peso</param>
        public void OnWeightYChanged(float value) {
            if (CurrentNeuron == null) return;
            CurrentNeuron.weights.y = value / 4f;
            network.NotifyChange();
        }

        /// <summary>
        /// Función para cambiar el tercer peso de la neurona objetivo desde la interfaz
        /// </summary>
        /// <param name="value">El nuevo peso</param>
        public void OnWeightZChanged(float value) {
            if (CurrentNeuron == null) return;
            CurrentNeuron.weights.z = value / 4f;
            network.NotifyChange();
        }

        /// <summary>
        /// Función para cambiar el cuarto peso de la neurona objetivo desde la interfaz
        /// </summary>
        /// <param name="value">El nuevo peso</param>
        public void OnWeightWChanged(float value) {
            if (CurrentNeuron == null) return;
            CurrentNeuron.weights.w = value / 4f;
            network.NotifyChange();
        }
        
        /// <summary>
        /// Función para cambiar el sesgo primer peso de la neurona objetivo desde la interfaz
        /// </summary>
        /// <param name="value">El nuevo sesgo</param>
        public void OnBiasChanged(float value) {
            if (CurrentNeuron == null) return;
            CurrentNeuron.bias = value / 4f;
            network.NotifyChange();
        }
        
        /// <summary>
        /// Función para cambiar la función de activación de la neurona objetivo desde la interfaz
        /// </summary>
        /// <param name="value">El índice de la función de activación</param>
        public void OnActivationFunctionChanged(int i) {
            if (CurrentNeuron == null) return;
            CurrentNeuron.activationFunction = (ActivationFunction)i;
            network.NotifyChange();
        }

        /// <summary>
        /// Aleatoriza la neurona objetivo
        /// </summary>
        public void RandomizeCurrentNeuron() {
            if (CurrentNeuron == null) return;
            network.RandomizeNeuron(CurrentTargetNeuron);
            OnWeightXLoaded?.Invoke(CurrentNeuron.weights.x);
            OnWeightYLoaded?.Invoke(CurrentNeuron.weights.y);
            OnWeightZLoaded?.Invoke(CurrentNeuron.weights.z);
            OnWeightWLoaded?.Invoke(CurrentNeuron.weights.w);
            OnBiasLoaded?.Invoke(CurrentNeuron.bias);
            OnActivationFunctionLoaded?.Invoke((int)CurrentNeuron.activationFunction);
        }
    }
}