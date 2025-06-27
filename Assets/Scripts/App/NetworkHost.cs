using Neuralizer.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

namespace Neuralizer {
    public class NetworkHost : MonoBehaviour {
        private static readonly int WeightsA_ID = Shader.PropertyToID("_WeightsA");
        private static readonly int WeightsB_ID = Shader.PropertyToID("_WeightsB");
        private static readonly int WeightsC_ID = Shader.PropertyToID("_WeightsC");
        private static readonly int WeightsD_ID = Shader.PropertyToID("_WeightsD");
        private static readonly int WeightsE_ID = Shader.PropertyToID("_WeightsE");
        private static readonly int WeightsF_ID = Shader.PropertyToID("_WeightsF");

        private static readonly int WeightsOutput_ID = Shader.PropertyToID("_WeightsOutput");

        private static readonly int FunctionParamsID = Shader.PropertyToID("_FunctionParams");

        public static NetworkHost Instance { get; private set; }

        [SerializeField] private SamplePoint targetPointPrefab;
        [SerializeField] private SamplePoint unwantedPointPrefab;
        
        [SerializeField] private UnityEvent<string> OnDisplayName;
        [SerializeField] private UnityEvent<bool> OnOrbitalCamera;
        [SerializeField] private UnityEvent<bool> OnPlanarCamera;
        [SerializeField] private UnityEvent OnSceneLoaded;

        private NeuralScene currentScene;
        private ObjectPool<SamplePoint> targetPool;
        private ObjectPool<SamplePoint> unwantedPool;
        
        public ActivationFunction CurrentActivationFunction { get; private set; } = ActivationFunction.Stepped;
        public NetworkArchitecture Architecture { get; private set; } = NetworkArchitecture.Perceptron;

        public NeuralScene Scene {
            get => currentScene;
            set {
                currentScene = value;
                Architecture = value.networkArchitecture;

                switch (Architecture) {
                    default:
                    case NetworkArchitecture.Perceptron:
                        Shader.EnableKeyword("_ARCHITECTURE_I_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_2_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_3_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_2_2_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_3_3_1");
                    break;
                    case NetworkArchitecture.Two:
                        Shader.DisableKeyword("_ARCHITECTURE_I_1");
                        Shader.EnableKeyword("_ARCHITECTURE_I_2_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_3_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_2_2_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_3_3_1");
                    break;
                    case NetworkArchitecture.Three:
                        Shader.DisableKeyword("_ARCHITECTURE_I_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_2_1");
                        Shader.EnableKeyword("_ARCHITECTURE_I_3_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_2_2_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_3_3_1");
                    break;
                    case NetworkArchitecture.TwoTwo:
                        Shader.DisableKeyword("_ARCHITECTURE_I_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_2_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_3_1");
                        Shader.EnableKeyword("_ARCHITECTURE_I_2_2_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_3_3_1");
                    break;
                    case NetworkArchitecture.ThreeThree:
                        Shader.DisableKeyword("_ARCHITECTURE_I_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_2_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_3_1");
                        Shader.DisableKeyword("_ARCHITECTURE_I_2_2_1");
                        Shader.EnableKeyword("_ARCHITECTURE_I_3_3_1");
                    break;
                }
                value.A.ApplyTo(A);
                value.B.ApplyTo(B);
                value.C.ApplyTo(C);
                value.D.ApplyTo(D);
                value.E.ApplyTo(E);
                value.F.ApplyTo(F);
                value.Output.ApplyTo(Output);
                SamplePoint.ClearPoints();
                foreach (Vector3 point in value.targetPoints) {
                    targetPool.Get().transform.position = point;
                }
                foreach (Vector3 point in value.unwantedPoints) {
                    unwantedPool.Get().transform.position = point;
                }
                SamplePoint.UpdatePoints();
                OnDisplayName?.Invoke(value.DisplayName);
                OnPlanarCamera?.Invoke(!value.useOrbitalCamera);
                OnOrbitalCamera?.Invoke(value.useOrbitalCamera);
                OnSceneLoaded?.Invoke();
            }
        }

        public Neuron A { get; set; } = new("_FUNCTION_A");
        public Neuron B { get; set; } = new("_FUNCTION_B");
        public Neuron C { get; set; } = new("_FUNCTION_C");
        public Neuron D { get; set; } = new("_FUNCTION_D");
        public Neuron E { get; set; } = new("_FUNCTION_E");
        public Neuron F { get; set; } = new("_FUNCTION_F");
        public Neuron Output { get; set; } = new("_FUNCTION_OUTPUT");

        private void Awake() {
            Instance = this;
            targetPool = new ObjectPool<SamplePoint>(OnCreateTargetPoint, OnGetPoint, OnReleasePoint, OnDestroyPoint);
            unwantedPool = new ObjectPool<SamplePoint>(OnCreateUnwantedPoint, OnGetPoint, OnReleasePoint, OnDestroyPoint);
        }

        private SamplePoint CreatePoint(SamplePoint prefab, ObjectPool<SamplePoint> pool) {
            var created = (Instantiate(prefab, transform)).GetComponent<SamplePoint>();
            created.gameObject.SetActive(false);
            created.ParentPool = pool;
            return created;
        }

        private SamplePoint OnCreateTargetPoint() {
            return CreatePoint(targetPointPrefab, targetPool);
        }
        
        private SamplePoint OnCreateUnwantedPoint() {
            return CreatePoint(unwantedPointPrefab, unwantedPool);
        }

        private void OnGetPoint(SamplePoint obj) {
            obj.gameObject.SetActive(true);
        }

        private void OnReleasePoint(SamplePoint obj) {
            obj.gameObject.SetActive(false);
        }

        private void OnDestroyPoint(SamplePoint obj) {
            Destroy(obj.gameObject);
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
            NotifyChange();
        }

        public void NotifyChange() {
            Shader.SetGlobalVector(WeightsA_ID, A?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsB_ID, B?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsC_ID, C?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsD_ID, D?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsE_ID, E?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsF_ID, F?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(WeightsOutput_ID, Output?.Info ?? Vector4.zero);
            Shader.SetGlobalVector(FunctionParamsID, Output?.FunctionParams ?? Vector3.up);
            SamplePoint.UpdatePoints();
        }

        public float EvaluateAt(Vector3 point) {
            float value = 0f;
            Vector3 layer1;
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
            }
            return value;
        }
    }
}