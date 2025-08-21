using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neuralizer {
    public class NetworkController : MonoBehaviour {
        [SerializeField] private NetworkHost network;
        [SerializeField] private LayoutElement aLayer;
        [SerializeField] private LayoutElement bLayer;
        [SerializeField] private LayoutElement outputLayer;
        [SerializeField] private NeuronToggleButton[] toggles;
        [SerializeField] private UnityEvent<float> OnWeightXLoaded;
        [SerializeField] private UnityEvent<float> OnWeightYLoaded;
        [SerializeField] private UnityEvent<float> OnWeightZLoaded;
        [SerializeField] private UnityEvent<float> OnWeightWLoaded;
        [SerializeField] private UnityEvent<float> OnBiasLoaded;
        [SerializeField] private UnityEvent<int> OnActivationFunctionLoaded;
        [SerializeField] private UnityEvent<bool> OnShowSettings;
        [SerializeField] private UnityEvent<bool> OnShowLayerA;
        [SerializeField] private UnityEvent<bool> OnShowLayerB;
        [SerializeField] private UnityEvent<bool> OnShowXWeight;
        [SerializeField] private UnityEvent<bool> OnShowYWeight;
        [SerializeField] private UnityEvent<bool> OnShowZWeight;
        [SerializeField] private UnityEvent<bool> OnShowWWeight;
        [SerializeField] private UnityEvent<bool> OnShowRandomizeNeuron;

        private const float speed = 24f;

        private bool isDirty = true;
        private Vector3 restFlexWidth = Vector3.one;
        private Vector3 targetFlexWidth = Vector3.one;
        private float flexT = 1f;

        private LayoutElement targetFlex;

        public Neuron CurrentNeuron { get; set; }
        public TargetNeuron CurrentTargetNeuron { get; set; }

        private void Start() {
            restFlexWidth.Set(aLayer.flexibleWidth, bLayer.flexibleWidth, outputLayer.flexibleWidth);
        }

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

        private void Update() {
            flexT = Mathf.Exp(-speed * Time.unscaledDeltaTime);
            aLayer.flexibleWidth = Mathf.Lerp(targetFlexWidth.x, aLayer.flexibleWidth, flexT);
            bLayer.flexibleWidth = Mathf.Lerp(targetFlexWidth.y, bLayer.flexibleWidth, flexT);
            outputLayer.flexibleWidth = Mathf.Lerp(targetFlexWidth.z, outputLayer.flexibleWidth, flexT);
        }

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

        public void OnWeightXChanged(float value) {
            if (CurrentNeuron == null) return;
            CurrentNeuron.weights.x = value / 4f;
            network.NotifyChange();
        }

        public void OnWeightYChanged(float value) {
            if (CurrentNeuron == null) return;
            CurrentNeuron.weights.y = value / 4f;
            network.NotifyChange();
        }

        public void OnWeightZChanged(float value) {
            if (CurrentNeuron == null) return;
            CurrentNeuron.weights.z = value / 4f;
            network.NotifyChange();
        }

        public void OnWeightWChanged(float value) {
            if (CurrentNeuron == null) return;
            CurrentNeuron.weights.w = value / 4f;
            network.NotifyChange();
        }

        public void OnBiasChanged(float value) {
            if (CurrentNeuron == null) return;
            CurrentNeuron.bias = value / 4f;
            network.NotifyChange();
        }

        public void OnActivationFunctionChanged(int i) {
            if (CurrentNeuron == null) return;
            CurrentNeuron.activationFunction = (ActivationFunction)i;
            network.NotifyChange();
        }

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