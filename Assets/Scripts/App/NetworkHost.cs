using Neuralizer.Data;
using Neuralizer.NetworkRendering;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Neuralizer {
    public class NetworkHost : MonoBehaviour {

        private static readonly ShaderKeyword architecturePerceptron = new("_ARCHITECTURE_PERCEPTRON");
        private static readonly ShaderKeyword orbitalCamera = new("_2D_GRAPH");
        public static NetworkHost Instance { get; private set; }

        [SerializeField] private SamplePoint targetPointPrefab;
        [SerializeField] private SamplePoint unwantedPointPrefab;
        [SerializeField] private Material inputMaterial;
        [SerializeField] private Material layerMaterial;

        [SerializeField] private UnityEvent<string> OnDisplayName;
        [SerializeField] private UnityEvent<bool> OnOrbitalCamera;
        [SerializeField] private UnityEvent<bool> OnPlanarCamera;
        [SerializeField] private UnityEvent OnSceneLoaded;

        private NeuralScene currentScene;
        private UnityEngine.Pool.ObjectPool<SamplePoint> targetPool;
        private UnityEngine.Pool.ObjectPool<SamplePoint> unwantedPool;
        private NeuralRenderPass networkPass;
        private bool enqueuePass = false;

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
                architecturePerceptron.Enabled = Scene.architecture == Architecture.Perceptron;
                orbitalCamera.Enabled = Scene.useOrbitalCamera;
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
                inputMaterial = inputMaterial,
                layerMaterial = layerMaterial
            };
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable() {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            networkPass.Dispose();
            networkPass = null;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam) {
            if (!Scene) {
                return;
            }

            if (Scene.architecture == Architecture.Perceptron) {
                return;
            }

            // if (enqueuePass) {
            networkPass.AA = AA;
            networkPass.AB = AB;
            networkPass.AC = AC;
            networkPass.AD = AD;
            networkPass.BA = BA;
            networkPass.BB = BB;
            networkPass.BC = BD;
            networkPass.BD = BD;
            networkPass.Output = Output;
            cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(networkPass);
            //     enqueuePass = false;
            // }
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
            if (Scene.architecture == Architecture.Perceptron) {
                Output.SetShaderData();
            }

            SamplePoint.UpdatePoints();
            enqueuePass = true;
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