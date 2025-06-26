using Neuralizer.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer {
    public class NetworkHost : MonoBehaviour {
        private static readonly int WeightsA_ID = Shader.PropertyToID("_WeightsA");
        private static readonly int WeightsB_ID = Shader.PropertyToID("_WeightsB");
        private static readonly int WeightsC_ID = Shader.PropertyToID("_WeightsC");

        private static readonly int WeightsD_ID = Shader.PropertyToID("_WeightsD");
        private static readonly int WeightsE_ID = Shader.PropertyToID("_WeightsE");
        private static readonly int WeightsF_ID = Shader.PropertyToID("_WeightsF");

        private static readonly int WeightsG_ID = Shader.PropertyToID("_WeightsG");
        private static readonly int WeightsH_ID = Shader.PropertyToID("_WeightsH");
        private static readonly int WeightsI_ID = Shader.PropertyToID("_WeightsI");

        private static readonly int WeightsOutput_ID = Shader.PropertyToID("_WeightsOutput");

        private static readonly int FunctionParamsID = Shader.PropertyToID("_FunctionParams");

        public static NetworkHost Instance { get; private set; }

        [SerializeField] private UnityEvent<bool> OnStepped;
        [SerializeField] private UnityEvent<bool> OnReLu;
        [SerializeField] private UnityEvent<bool> OnSigmoid;
        [SerializeField] private UnityEvent<bool> OnTanH;

        public float StepMin { get; set; } = 0f;
        public float StepMax { get; set; } = 1f;
        public float StepThreshold { get; set; } = 0f;
        public float ReLuOffset { get; set; } = 0f;
        public Vector4 FunctionParams => new(StepMin, StepMax, StepThreshold, ReLuOffset);
        public ActivationFunction CurrentActivationFunction { get; private set; }
        public NetworkArchitecture Architecture { get; private set; } = NetworkArchitecture.ThreeThreeThree;

        private NeuralScene currentScene;

        public NeuralScene Scene {
            get => currentScene;
            set {
                currentScene = value;
                Architecture = value.networkArchitecture;
                //TODO config
            }
        }

        public Neuron A { get; set; } = new();
        public Neuron B { get; set; } = new();
        public Neuron C { get; set; } = new();
        public Neuron D { get; set; } = new();
        public Neuron E { get; set; } = new();
        public Neuron F { get; set; } = new();
        public Neuron G { get; set; } = new();
        public Neuron H { get; set; } = new();
        public Neuron I { get; set; } = new();
        public Neuron Output { get; set; } = new();

        private void Awake() {
            Instance = this;
        }

        private void OnEnable() {
            this.SetSigmoid(true);
            A = Neuron.CreateRandom();
            B = Neuron.CreateRandom();
            C = Neuron.CreateRandom();
            
            D = Neuron.CreateRandom();
            E = Neuron.CreateRandom();
            F = Neuron.CreateRandom();
            
            G = Neuron.CreateRandom();
            H = Neuron.CreateRandom();
            I = Neuron.CreateRandom();
            
            Output = Neuron.CreateRandom();
            
            Architecture = NetworkArchitecture.ThreeThree;
            
            NotifyChange();
        }

        public void SetStepped(bool value) {
            if (!value) return;
            SetFunctionIndex((int)ActivationFunction.Stepped);
        }

        public void SetReLu(bool value) {
            if (!value) return;
            SetFunctionIndex((int)ActivationFunction.ReLu);
        }

        public void SetSigmoid(bool value) {
            if (!value) return;
            SetFunctionIndex((int)ActivationFunction.Sigmoid);
        }

        public void SetTanH(bool value) {
            if (!value) return;
            SetFunctionIndex((int)ActivationFunction.TanH);
        }

        public void SetFunctionIndex(int index) {
            CurrentActivationFunction = (ActivationFunction)index;
            OnStepped?.Invoke(CurrentActivationFunction == ActivationFunction.Stepped);
            OnReLu?.Invoke(CurrentActivationFunction == ActivationFunction.ReLu);
            OnSigmoid?.Invoke(CurrentActivationFunction == ActivationFunction.Sigmoid);
            OnTanH?.Invoke(CurrentActivationFunction == ActivationFunction.TanH);
            NotifyChange();
        }

        public void NotifyChange() {
            Shader.SetGlobalVector(WeightsA_ID, A?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsB_ID, B?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsC_ID, C?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsD_ID, D?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsE_ID, E?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsF_ID, F?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsG_ID, G?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsH_ID, H?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsI_ID, I?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsOutput_ID, Output?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(FunctionParamsID, FunctionParams);

            switch (CurrentActivationFunction) {
                case ActivationFunction.Sigmoid:
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_TANH");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_RELU");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_LINEAL");
                    Shader.EnableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                break;
                case ActivationFunction.TanH:
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_RELU");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_LINEAL");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                    Shader.EnableKeyword("_ACTIVATIONFUNCTION_TANH");
                break;
                case ActivationFunction.ReLu:
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_LINEAL");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_TANH");
                    Shader.EnableKeyword("_ACTIVATIONFUNCTION_RELU");
                break;
                case ActivationFunction.Stepped:
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_LINEAL");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_SIGMOID");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_TANH");
                    Shader.DisableKeyword("_ACTIVATIONFUNCTION_RELU");
                    Shader.EnableKeyword("_ACTIVATIONFUNCTION_STEPPED");
                break;
            }

            switch (Architecture) {
                default:
                case NetworkArchitecture.Perceptron:
                    Shader.EnableKeyword("_ARCHITECTURE_I_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_3_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_2_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_3_3_1");
                break;
                case NetworkArchitecture.Two:
                    Shader.DisableKeyword("_ARCHITECTURE_I_1");
                    Shader.EnableKeyword("_ARCHITECTURE_I_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_3_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_2_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_3_3_1");
                break;
                case NetworkArchitecture.Three:
                    Shader.DisableKeyword("_ARCHITECTURE_I_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_1");
                    Shader.EnableKeyword("_ARCHITECTURE_I_3_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_3_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_2_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_3_3_1");
                break;
                case NetworkArchitecture.TwoTwo:
                    Shader.DisableKeyword("_ARCHITECTURE_I_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_1");
                    Shader.EnableKeyword("_ARCHITECTURE_I_2_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_3_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_2_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_3_3_1");
                break;
                case NetworkArchitecture.ThreeThree:
                    Shader.DisableKeyword("_ARCHITECTURE_I_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_2_1");
                    Shader.EnableKeyword("_ARCHITECTURE_I_3_3_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_2_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_3_3_1");
                break;
                case NetworkArchitecture.TwoTwoTwo:
                    Shader.DisableKeyword("_ARCHITECTURE_I_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_3_1");
                    Shader.EnableKeyword("_ARCHITECTURE_I_2_2_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_3_3_1");
                break;
                case NetworkArchitecture.ThreeThreeThree:
                    Shader.DisableKeyword("_ARCHITECTURE_I_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_2_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_3_3_1");
                    Shader.DisableKeyword("_ARCHITECTURE_I_2_2_2_1");
                    Shader.EnableKeyword("_ARCHITECTURE_I_3_3_3_1");
                break;
            }

            SamplePoint.UpdatePoints();
        }


        public float EvaluateAt(Vector3 point) {
            float value = 0f;
            Vector3 layer1;
            Vector3 layer2;
            switch (Architecture) {
                default:
                case NetworkArchitecture.Perceptron:
                    value = Output.Sample(point);
                break;
                case NetworkArchitecture.Two:
                    value = Output.Sample(new Vector3(A.Sample(point), B.Sample(point), 0f));
                break;
                case NetworkArchitecture.Three:
                    value = Output.Sample(new Vector3(A.Sample(point), B.Sample(point), C.Sample(point)));
                break;
                case NetworkArchitecture.TwoTwo:
                    layer1 = new Vector3(A.Sample(point), B.Sample(point), 0f);
                    value = Output.Sample(new Vector3(D.Sample(layer1), E.Sample(layer1), 0f));
                break;
                case NetworkArchitecture.ThreeThree:
                    layer1 = new Vector3(A.Sample(point), B.Sample(point), C.Sample(point));
                    value = Output.Sample(new Vector3(D.Sample(layer1), E.Sample(layer1), F.Sample(point)));
                break;
                case NetworkArchitecture.TwoTwoTwo:
                    layer1 = new Vector3(A.Sample(point), B.Sample(point), 0f);
                    layer2 = new Vector3(D.Sample(layer1), E.Sample(layer1), 0f);
                    value = Output.Sample(new Vector3(G.Sample(layer2), H.Sample(layer2), 0f));
                break;
                case NetworkArchitecture.ThreeThreeThree:
                    layer1 = new Vector3(A.Sample(point), B.Sample(point), C.Sample(point));
                    layer2 = new Vector3(D.Sample(layer1), E.Sample(layer1), F.Sample(layer1));
                    value = Output.Sample(new Vector3(G.Sample(layer2), H.Sample(layer2), I.Sample(layer2)));
                break;
            }

            return value;
            //
            // return CurrentActivationFunction switch {
            //     ActivationFunction.Sigmoid => 1f / (1f + Mathf.Exp(-value)),
            //     ActivationFunction.TanH => (float)System.Math.Tanh(value),
            //     ActivationFunction.ReLu => Mathf.Max(0f, value + ReLuOffset),
            //     ActivationFunction.Stepped => value >= StepThreshold ? StepMax : StepMin,
            //     _ => value
            // };
        }
    }
}