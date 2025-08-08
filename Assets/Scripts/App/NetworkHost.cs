using Neuralizer.Data;
using Neuralizer.NetworkRendering;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Neuralizer {
    public class NetworkHost : MonoBehaviour {
        private const string NetworkRenderingInfo_Name = "_NetworkRenderingInfo";
        private static readonly int NetworkRenderingInfo_Id = Shader.PropertyToID(NetworkRenderingInfo_Name);

        private static readonly ShaderKeyword planarGraph = new("_2D_GRAPH");
        public static NetworkHost Instance { get; private set; }

        [SerializeField] private SamplePoint targetPointPrefab;
        [SerializeField] private SamplePoint unwantedPointPrefab;
        [SerializeField] private Material neuronMaterial;
        [SerializeField] private Material paintMaterial;
        [SerializeField] private Material blurMaterial;

        [SerializeField] private UnityEvent<string> OnDisplayName;
        [SerializeField] private UnityEvent<bool> OnOrbitalCamera;
        [SerializeField] private UnityEvent<bool> OnPlanarCamera;
        [SerializeField] private UnityEvent OnSceneLoaded;
        [SerializeField] private UnityEvent OnSceneUnloaded;
        [SerializeField] private UnityEvent<bool> OnAllowEditing;
        [SerializeField] private UnityEvent<bool> OnAllowTraining;
        [SerializeField] private UnityEvent<bool> OnAllowRandomizeNeuron;
        [SerializeField] private UnityEvent<bool> OnAllowRandomizeNetwork;
        [SerializeField] private UnityEvent OnInvalidNetworkSubmitted;
        [SerializeField] private UnityEvent OnValidNetworkSubmitted;
        [SerializeField] private UnityEvent<float> OnWeightSliderMin;
        [SerializeField] private UnityEvent<float> OnWeightSliderMax;
        [SerializeField] private UnityEvent<float> OnBiasSliderMin;
        [SerializeField] private UnityEvent<float> OnBiasSliderMax;
        private NeuralScene currentScene;
        private UnityEngine.Pool.ObjectPool<SamplePoint> targetPool;
        private UnityEngine.Pool.ObjectPool<SamplePoint> unwantedPool;
        private NeuralRenderPass networkPass;

        public NeuralScene Scene {
            get => currentScene;
            set {
                currentScene = value;
                SamplePoint.ClearPoints();
                if (value) {
                    value.AA.ApplyTo(AA);
                    value.AB.ApplyTo(AB);
                    value.AC.ApplyTo(AC);
                    value.AD.ApplyTo(AD);
                    value.BA.ApplyTo(BA);
                    value.BB.ApplyTo(BB);
                    value.BC.ApplyTo(BC);
                    value.BD.ApplyTo(BD);
                    value.Output.ApplyTo(Output);
                    networkPass.architecture = value.architecture;
                    networkPass.isPlanar = !Scene.useOrbitalCamera;
                    planarGraph.Enabled = networkPass.isPlanar;
                    transform.position = value.origin;
                    // Vector3 sum = Vector3.zero;
                    // float weight = 0f;
                    // foreach (Vector3 point in value.targetPoints) {
                    //     sum += point;
                    //     weight += 1f;
                    // }
                    // foreach (Vector3 point in value.unwantedPoints) {
                    //     sum += point;
                    //     weight += 1f;
                    // }
                    // if (networkPass.isPlanar) {
                    //     sum.z = 0f;
                    // }
                    // transform.position = sum / weight;
                    foreach (Vector3 point in value.targetPoints) {
                        targetPool.Get().transform.position = point;
                    }
                    foreach (Vector3 point in value.unwantedPoints) {
                        unwantedPool.Get().transform.position = point;
                    }

                    NotifyChange();
                    OnDisplayName?.Invoke(value.DisplayName);
                    OnPlanarCamera?.Invoke(!value.useOrbitalCamera);
                    OnOrbitalCamera?.Invoke(value.useOrbitalCamera);
                    OnAllowTraining?.Invoke(value.allowTraining);
                    OnAllowRandomizeNeuron?.Invoke(value.allowRandomizeNeuron);
                    OnAllowRandomizeNetwork?.Invoke(value.allowRandomizeNetwork);
                    OnAllowEditing?.Invoke(value.allowEditing);
                    OnWeightSliderMin?.Invoke(value.weightRange.x);
                    OnWeightSliderMax?.Invoke(value.weightRange.y);
                    OnBiasSliderMin?.Invoke(value.biasRange.x);
                    OnBiasSliderMax?.Invoke(value.biasRange.y);
                    OnSceneLoaded?.Invoke();
                }
                else {
                    transform.position = Vector3.zero;
                    OnDisplayName?.Invoke("");
                    OnPlanarCamera?.Invoke(true);
                    OnOrbitalCamera?.Invoke(false);
                    OnAllowTraining?.Invoke(false);
                    OnAllowRandomizeNeuron?.Invoke(false);
                    OnAllowRandomizeNetwork?.Invoke(false);
                    OnSceneUnloaded?.Invoke();
                }
            }
        }

        public Neuron AA { get; } = new();
        public Neuron AB { get; } = new();
        public Neuron AC { get; } = new();
        public Neuron AD { get; } = new();
        public Neuron BA { get; } = new();
        public Neuron BB { get; } = new();
        public Neuron BC { get; } = new();
        public Neuron BD { get; } = new();
        public Neuron Output { get; } = new();

        private void Awake() {
            Instance = this;
            targetPool = new(OnCreateTargetPoint, OnGetPoint, OnReleasePoint, OnDestroyPoint);
            unwantedPool = new(OnCreateUnwantedPoint, OnGetPoint, OnReleasePoint, OnDestroyPoint);
        }

        private void OnEnable() {
            networkPass = new() {
                neuronMaterial = neuronMaterial,
                paintMaterial = paintMaterial,
                blurMaterial = blurMaterial,
                AA = AA,
                AB = AB,
                AC = AC,
                AD = AD,
                BA = BA,
                BB = BB,
                BC = BC,
                BD = BD,
                Output = Output
            };
            networkPass.Initialize();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable() {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            networkPass = null;
        }

        private Vector4 networkRenderingInfo = Vector4.zero;

        private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam) {
            if (!currentScene) return;
            if (!Scene) return;

            const float shutterSpeed = 1f / 36f;
            networkRenderingInfo.x = 1f - (Time.unscaledDeltaTime / shutterSpeed);
            networkRenderingInfo.x = Mathf.Clamp01(networkRenderingInfo.x);
            networkRenderingInfo.y = (networkRenderingInfo.y + 1) % 1024;
            networkRenderingInfo.z = cam.focusDistance;
            Shader.SetGlobalVector(NetworkRenderingInfo_Id, networkRenderingInfo);

            cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(networkPass);
        }

        public void CloseScene() {
            Scene = null;
        }

        private SamplePoint CreatePoint(SamplePoint prefab, UnityEngine.Pool.ObjectPool<SamplePoint> pool) {
            SamplePoint created = (Instantiate(prefab, transform)).GetComponent<SamplePoint>();
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

        public void NotifyChange() {
            SamplePoint.UpdatePoints();
        }

        public void Randomize() {
            Neuron.Randomize(AA);
            Neuron.Randomize(AB);
            Neuron.Randomize(AC);
            Neuron.Randomize(AD);
            AA.weights.w = 0f;
            AB.weights.w = 0f;
            AC.weights.w = 0f;
            AD.weights.w = 0f;
            Neuron.Randomize(BA);
            Neuron.Randomize(BB);
            Neuron.Randomize(BC);
            Neuron.Randomize(BD);
            Neuron.Randomize(Output);
            if (Scene.architecture == Architecture.Perceptron) {
                Output.weights.w = 0f;
                if (!Scene.useOrbitalCamera) {
                    Output.weights.z = 0f;
                }
            }
            else if (!Scene.useOrbitalCamera) {
                AA.weights.z = 0f;
                AB.weights.z = 0f;
                AC.weights.z = 0f;
                AD.weights.z = 0f;
            }

            NotifyChange();
        }

        public void RandomizeNeuron(TargetNeuron target) {
            Neuron targetNeuron = GetTargetNeuron(target);
            Neuron.Randomize(targetNeuron);
            switch (target) {
                case TargetNeuron.AA:
                case TargetNeuron.AB:
                case TargetNeuron.AC:
                case TargetNeuron.AD:
                    targetNeuron.weights.w = 0f;
                break;
                case TargetNeuron.BA:
                case TargetNeuron.BB:
                case TargetNeuron.BC:
                case TargetNeuron.BD:
                    if (!Scene.useOrbitalCamera) {
                        AA.weights.z = 0f;
                        AB.weights.z = 0f;
                        AC.weights.z = 0f;
                        AD.weights.z = 0f;
                        AA.weights.w = 0f;
                        AB.weights.w = 0f;
                        AC.weights.w = 0f;
                        AD.weights.w = 0f;
                    }

                break;
                case TargetNeuron.Output:
                    if (Scene.architecture == Architecture.Perceptron) {
                        Output.weights.w = 0f;
                        if (!Scene.useOrbitalCamera) {
                            Output.weights.z = 0f;
                        }
                    }
                    else if (!Scene.useOrbitalCamera) {
                        Output.weights.z = 0f;
                        Output.weights.w = 0f;
                    }

                break;
            }

            NotifyChange();
        }

        public float EvaluateAt(Vector3 point) {
            switch (Scene.architecture) {
                default:
                case Architecture.Perceptron:
                    return Output.Sample(point);
                case Architecture.OneLayer:
                    return Output.Sample(new Vector4(AA.Sample(point), AB.Sample(point), AC.Sample(point),
                        AD.Sample(point)));
                case Architecture.TwoLayers:
                    Vector4 layer2_1 = new(AA.Sample(point), AB.Sample(point), AC.Sample(point), AD.Sample(point));
                    return Output.Sample(new Vector4(BA.Sample(layer2_1), BB.Sample(layer2_1), BC.Sample(layer2_1),
                        BD.Sample(layer2_1)));
            }
        }

        public Neuron GetTargetNeuron(TargetNeuron target) {
            switch (target) {
                default:
                case TargetNeuron.None:
                    return null;
                case TargetNeuron.AA:
                    return AA;
                case TargetNeuron.AB:
                    return AB;
                case TargetNeuron.AC:
                    return AC;
                case TargetNeuron.AD:
                    return AD;
                case TargetNeuron.BA:
                    return BA;
                case TargetNeuron.BB:
                    return BB;
                case TargetNeuron.BC:
                    return BC;
                case TargetNeuron.BD:
                    return BD;
                case TargetNeuron.Output:
                    return Output;
            }
        }

        public void SubmitResult() {
            if (SamplePoint.AreAllValid()) {
                OnValidNetworkSubmitted?.Invoke();
                Scene.IsCompleted = true;
            }
            else {
                OnInvalidNetworkSubmitted?.Invoke();
            }
        }
    }
}