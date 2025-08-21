using Neuralizer.Data;
using Neuralizer.NetworkRendering;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Neuralizer {
    public class NetworkHost : MonoBehaviour {
        private const string NetworkRenderingInfo_Name = "_NetworkRenderingInfo";
        private const string ZoomScale_Name = "_ZoomScale";
        private static readonly int NetworkRenderingInfo_Id = Shader.PropertyToID(NetworkRenderingInfo_Name);
        private static readonly int ZoomScale_ID = Shader.PropertyToID(ZoomScale_Name);

        private static readonly ShaderKeyword planarGraph = new("_2D_GRAPH");
        private static readonly ShaderKeyword fadeZ = new("_FADEZ");
        public static NetworkHost Instance { get; private set; }

        [SerializeField] private MedalsAsset medals;
        [SerializeField] private SamplePoint targetPointPrefab;
        [SerializeField] private SamplePoint unwantedPointPrefab;
        [SerializeField] private Material neuronMaterial;
        [SerializeField] private Material paintMaterial;
        [SerializeField] private Material blurMaterial;
        [SerializeField] private CinemachineRecomposer[] recomposers;

        [SerializeField] private UnityEvent<string> OnDisplayName;
        [SerializeField] private UnityEvent<bool> OnOrbitalCamera;
        [SerializeField] private UnityEvent<bool> OnPlanarCamera;
        [SerializeField] private UnityEvent<bool> OnZInputNeeded;
        [SerializeField] private UnityEvent OnSceneLoaded;
        [SerializeField] private UnityEvent OnSceneUnloaded;
        [SerializeField] private UnityEvent<bool> OnAllowScoring;
        [SerializeField] private UnityEvent<bool> OnAllowEditing;
        [SerializeField] private UnityEvent<bool> OnAllowRandomizeNeuron;
        [SerializeField] private UnityEvent<bool> OnAllowRandomizeNetwork;
        [SerializeField] private UnityEvent OnInvalidNetworkSubmitted;
        [SerializeField] private UnityEvent OnValidNetworkSubmitted;
        [SerializeField] private UnityEvent<float> OnWeightSliderMin;
        [SerializeField] private UnityEvent<float> OnWeightSliderMax;
        [SerializeField] private UnityEvent<float> OnBiasSliderMin;
        [SerializeField] private UnityEvent<float> OnBiasSliderMax;
        [SerializeField] private UnityEvent<string> OnTimeString;
        [SerializeField] private UnityEvent<string> OnScoreString;
        [SerializeField] private UnityEvent<string> OnRatioString;
        [SerializeField] private UnityEvent<bool> OnShowMedal;
        [SerializeField] private UnityEvent<string> OnMedalName;
        [SerializeField] private UnityEvent<Sprite> OnMedalSprite;
        [SerializeField] private UnityEvent<string> OnMedalDescription;

        [Header("Lines Weight Alpha Events")]
        [SerializeField] private UnityEvent<float> OnAA_X_Alpha;
        [SerializeField] private UnityEvent<float> OnAA_Y_Alpha;
        [SerializeField] private UnityEvent<float> OnAA_Z_Alpha;
        
        [SerializeField] private UnityEvent<float> OnAB_X_Alpha;
        [SerializeField] private UnityEvent<float> OnAB_Y_Alpha;
        [SerializeField] private UnityEvent<float> OnAB_Z_Alpha;
        
        [SerializeField] private UnityEvent<float> OnAC_X_Alpha;
        [SerializeField] private UnityEvent<float> OnAC_Y_Alpha;
        [SerializeField] private UnityEvent<float> OnAC_Z_Alpha;
        
        [SerializeField] private UnityEvent<float> OnAD_X_Alpha;
        [SerializeField] private UnityEvent<float> OnAD_Y_Alpha;
        [SerializeField] private UnityEvent<float> OnAD_Z_Alpha;
        
        [SerializeField] private UnityEvent<float> OnBA_X_Alpha;
        [SerializeField] private UnityEvent<float> OnBA_Y_Alpha;
        [SerializeField] private UnityEvent<float> OnBA_Z_Alpha;
        [SerializeField] private UnityEvent<float> OnBA_W_Alpha;
        
        [SerializeField] private UnityEvent<float> OnBB_X_Alpha;
        [SerializeField] private UnityEvent<float> OnBB_Y_Alpha;
        [SerializeField] private UnityEvent<float> OnBB_Z_Alpha;
        [SerializeField] private UnityEvent<float> OnBB_W_Alpha;
        
        [SerializeField] private UnityEvent<float> OnBC_X_Alpha;
        [SerializeField] private UnityEvent<float> OnBC_Y_Alpha;
        [SerializeField] private UnityEvent<float> OnBC_Z_Alpha;
        [SerializeField] private UnityEvent<float> OnBC_W_Alpha;
        
        [SerializeField] private UnityEvent<float> OnBD_X_Alpha;
        [SerializeField] private UnityEvent<float> OnBD_Y_Alpha;
        [SerializeField] private UnityEvent<float> OnBD_Z_Alpha;
        [SerializeField] private UnityEvent<float> OnBD_W_Alpha;
        
        [SerializeField] private UnityEvent<float> OnOutput_X_Alpha;
        [SerializeField] private UnityEvent<float> OnOutput_Y_Alpha;
        [SerializeField] private UnityEvent<float> OnOutput_Z_Alpha;
        [SerializeField] private UnityEvent<float> OnOutput_W_Alpha;

        private NeuralScene currentScene;
        private UnityEngine.Pool.ObjectPool<SamplePoint> targetPool;
        private UnityEngine.Pool.ObjectPool<SamplePoint> unwantedPool;
        private NeuralRenderPass networkPass;
        private float sceneStartTime = 0f;

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
                    var isPlanar = !value.useOrbitalCamera;
                    networkPass.isPlanar = isPlanar;
                    planarGraph.Enabled = networkPass.isPlanar;

					var fz = value.useOrbitalCamera && value.inputs == NeuralScene.WorldInputs.XY;
                    fadeZ.Enabled = fz;

                    ZoomScale = 1f / value.zoomFactor;
                    Shader.SetGlobalFloat(ZoomScale_ID, ZoomScale);
                    foreach (var recomposer in recomposers) {
                        recomposer.ZoomScale = ZoomScale;
                    }

                    Vector3 sum = Vector3.zero;
                    float weight = 0f;
                    foreach (Vector3 point in value.targetPoints) {
                        sum += point;
                        weight += 1f;
                    }

                    foreach (Vector3 point in value.unwantedPoints) {
                        sum += point;
                        weight += 1f;
                    }

                    if (!value.useOrbitalCamera || value.inputs == NeuralScene.WorldInputs.XY) {
                        sum.z = 0f;
                    }

                    transform.position = Vector3.Lerp(value.origin, sum / weight, value.manualAutoPositionRatio);

                    foreach (Vector3 point in value.targetPoints) {
                        var p = targetPool.Get();
                        p.transform.position = new Vector3(point.x, point.y,
                            isPlanar ? Mathf.PI * ZoomScale : fz ? 0f : point.z);
                        p.transform.localScale = Vector3.one * ZoomScale;
                    }

                    foreach (Vector3 point in value.unwantedPoints) {
                        var p = unwantedPool.Get();
                        p.transform.position =
                            new Vector3(point.x, point.y, isPlanar ? ZoomScale : fz ? 0f : point.z);
                        p.transform.localScale = Vector3.one * ZoomScale;
                    }

                    NotifyChange();
                    OnDisplayName?.Invoke(value.DisplayName);
                    OnPlanarCamera?.Invoke(!value.useOrbitalCamera);
                    OnOrbitalCamera?.Invoke(value.useOrbitalCamera);
                    OnZInputNeeded?.Invoke(value.ZInputNeeded);
                    OnAllowRandomizeNeuron?.Invoke(value.allowRandomizeNeuron);
                    OnAllowRandomizeNetwork?.Invoke(value.allowRandomizeNetwork);
                    OnAllowEditing?.Invoke(value.allowEditing);
                    OnAllowScoring?.Invoke(value.AllowScoring);
                    OnWeightSliderMin?.Invoke(Mathf.Floor(value.weightRange.x * 4f));
                    OnWeightSliderMax?.Invoke(Mathf.Ceil(value.weightRange.y * 4f));
                    OnBiasSliderMin?.Invoke(Mathf.Floor(value.biasRange.x * 4f));
                    OnBiasSliderMax?.Invoke(Mathf.Ceil(value.biasRange.y * 4f));
                    OnSceneLoaded?.Invoke();
                    sceneStartTime = Time.time;
                }
                else {
                    transform.position = Vector3.zero;
                    OnDisplayName?.Invoke("");
                    OnPlanarCamera?.Invoke(true);
                    OnOrbitalCamera?.Invoke(false);
                    OnZInputNeeded?.Invoke(false);
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

        public float ZoomScale { get; private set; } = 1f;

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
            if (cam.cameraType != CameraType.Game) return;

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
            SamplePoint created = Instantiate(prefab).GetComponent<SamplePoint>();
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

            OnAA_X_Alpha?.Invoke(AA.IntensityX);
            OnAA_Y_Alpha?.Invoke(AA.IntensityY);
            OnAA_Z_Alpha?.Invoke(AA.IntensityZ);

            OnAB_X_Alpha?.Invoke(AB.IntensityX);
            OnAB_Y_Alpha?.Invoke(AB.IntensityY);
            OnAB_Z_Alpha?.Invoke(AB.IntensityZ);

            OnAC_X_Alpha?.Invoke(AC.IntensityX);
            OnAC_Y_Alpha?.Invoke(AC.IntensityY);
            OnAC_Z_Alpha?.Invoke(AC.IntensityZ);

            OnAD_X_Alpha?.Invoke(AD.IntensityX);
            OnAD_Y_Alpha?.Invoke(AD.IntensityY);
            OnAD_Z_Alpha?.Invoke(AD.IntensityZ);

            OnBA_X_Alpha?.Invoke(BA.IntensityX);
            OnBA_Y_Alpha?.Invoke(BA.IntensityY);
            OnBA_Z_Alpha?.Invoke(BA.IntensityZ);
            OnBA_W_Alpha?.Invoke(BA.IntensityW);

            OnBB_X_Alpha?.Invoke(BB.IntensityX);
            OnBB_Y_Alpha?.Invoke(BB.IntensityY);
            OnBB_Z_Alpha?.Invoke(BB.IntensityZ);
            OnBB_W_Alpha?.Invoke(BB.IntensityW);

            OnBC_X_Alpha?.Invoke(BC.IntensityX);
            OnBC_Y_Alpha?.Invoke(BC.IntensityY);
            OnBC_Z_Alpha?.Invoke(BC.IntensityZ);
            OnBC_W_Alpha?.Invoke(BC.IntensityW);

            OnBD_X_Alpha?.Invoke(BD.IntensityX);
            OnBD_Y_Alpha?.Invoke(BD.IntensityY);
            OnBD_Z_Alpha?.Invoke(BD.IntensityZ);
            OnBD_W_Alpha?.Invoke(BD.IntensityW);

            OnOutput_X_Alpha?.Invoke(Output.IntensityX);
            OnOutput_Y_Alpha?.Invoke(Output.IntensityY);
            OnOutput_Z_Alpha?.Invoke(Output.IntensityZ);
            OnOutput_W_Alpha?.Invoke(Output.IntensityW);
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
            switch (Scene.inputs) {
                default:
                case NeuralScene.WorldInputs.XY:
                    Vector2 input = new(point.x, point.y);
                    switch (Scene.architecture) {
                        default:
                        case Architecture.Perceptron:
                            return Output.Sample(input);
                        case Architecture.OneLayer:
                            return Output.Sample(new Vector4(AA.Sample(input), AB.Sample(input), AC.Sample(input),
                                AD.Sample(input)));
                        case Architecture.TwoLayers:
                            Vector4 layer2_1 = new(AA.Sample(input), AB.Sample(input), AC.Sample(input),
                                AD.Sample(input));
                            return Output.Sample(new Vector4(BA.Sample(layer2_1), BB.Sample(layer2_1),
                                BC.Sample(layer2_1),
                                BD.Sample(layer2_1)));
                    }
                case NeuralScene.WorldInputs.XYZ:
                    switch (Scene.architecture) {
                        default:
                        case Architecture.Perceptron:
                            return Output.Sample(point);
                        case Architecture.OneLayer:
                            return Output.Sample(new Vector4(AA.Sample(point), AB.Sample(point), AC.Sample(point),
                                AD.Sample(point)));
                        case Architecture.TwoLayers:
                            Vector4 layer2_1 = new(AA.Sample(point), AB.Sample(point), AC.Sample(point),
                                AD.Sample(point));
                            return Output.Sample(new Vector4(BA.Sample(layer2_1), BB.Sample(layer2_1),
                                BC.Sample(layer2_1),
                                BD.Sample(layer2_1)));
                    }
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

        public void TrainEpoch() {
            //TODO train;
            NotifyChange();
        }

        public void SubmitResult() {
            float ratio = SamplePoint.CorrectnessRatio;
            if (ratio >= Scene.successThreshold) {
                OnValidNetworkSubmitted?.Invoke();
                if (Scene.AllowScoring) {
                    bool awardMedal = Scene.SubmitResult(Time.time - sceneStartTime, ratio);

                    OnShowMedal?.Invoke(awardMedal);
                    if (awardMedal) {
                        Medal medal;
                        medal = medals.GetFromRatio(Scene.NormalizedScore);
                        OnTimeString?.Invoke(Scene.FormattedTime);
                        OnRatioString?.Invoke(Scene.FormattedRatio);
                        OnScoreString?.Invoke(Scene.Score.ToString());
                        if (medal != null) {
                            OnMedalName?.Invoke(medal.name);
                            OnMedalSprite?.Invoke(medal.sprite);
                            OnMedalDescription?.Invoke(medal.description);
                        }
                        else {
                            OnMedalName?.Invoke("");
                            OnMedalSprite?.Invoke(null);
                            OnMedalDescription?.Invoke("");
                        }
                    } else {
                        OnTimeString?.Invoke(NeuralScene.FormatTime(Time.time - sceneStartTime));
                        OnRatioString?.Invoke(NeuralScene.FormatRatio(ratio));
                        OnMedalName?.Invoke("");
                        OnMedalSprite?.Invoke(null);
                        OnMedalDescription?.Invoke("");
                    }
                }
                else {
                    OnShowMedal?.Invoke(false);
                }

                Scene.IsCompleted = true;
            }
            else {
                OnInvalidNetworkSubmitted?.Invoke();
                if (!Scene.AllowScoring) { 
                    Scene.IsCompleted = true;
                }
            }
        }
    }
}