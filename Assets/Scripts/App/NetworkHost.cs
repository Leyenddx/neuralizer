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

        // private static readonly ShaderKeyword architecturePerceptron = new("_ARCHITECTURE_PERCEPTRON");
        private static readonly ShaderKeyword planarGraph = new("_2D_GRAPH");
        public static NetworkHost Instance { get; private set; }

        [SerializeField] private SamplePoint targetPointPrefab;
        [SerializeField] private SamplePoint unwantedPointPrefab;
        [SerializeField] private Material neuronMaterial;
        [SerializeField] private Material paintMaterial;

        [SerializeField] private UnityEvent<string> OnDisplayName;
        [SerializeField] private UnityEvent<bool> OnOrbitalCamera;
        [SerializeField] private UnityEvent<bool> OnPlanarCamera;
        [SerializeField] private UnityEvent OnSceneLoaded;

        private NeuralScene currentScene;
        private UnityEngine.Pool.ObjectPool<SamplePoint> targetPool;
        private UnityEngine.Pool.ObjectPool<SamplePoint> unwantedPool;
        private NeuralRenderPass networkPass;

        public NeuralScene Scene {
            get => currentScene;
            set {
                currentScene = value;
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
                SamplePoint.ClearPoints();
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
                OnSceneLoaded?.Invoke();
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
            networkPass.Dispose();
            networkPass = null;
        }

        private Vector4 networkRenderingInfo = Vector4.zero;
        private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam) {
            networkRenderingInfo.x = 1f - Mathf.Exp(-((float)Screen.currentResolution.refreshRateRatio.value / Mathf.PI) * Time.unscaledDeltaTime);
            networkRenderingInfo.y = (networkRenderingInfo.y + 1) % 1024;
            networkRenderingInfo.z = cam.focusDistance;
            Shader.SetGlobalVector(NetworkRenderingInfo_Id, networkRenderingInfo);
            if (!Scene) {
                return;
            }
            cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(networkPass);
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
    }
}