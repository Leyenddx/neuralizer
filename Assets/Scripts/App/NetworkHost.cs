using System;
using System.Collections.Generic;
using Neuralizer.Data;
using Neuralizer.NetworkRendering;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Neuralizer {
    public class NetworkHost : MonoBehaviour {
        private static readonly int WeightsA_ID = Shader.PropertyToID("_WeightsA");
        private static readonly int WeightsB_ID = Shader.PropertyToID("_WeightsB");
        private static readonly int WeightsC_ID = Shader.PropertyToID("_WeightsC");
        private static readonly int WeightsD_ID = Shader.PropertyToID("_WeightsD");

        private static readonly int WeightsOutput_ID = Shader.PropertyToID("_WeightsOutput");

        private static readonly int FunctionParamsID = Shader.PropertyToID("_FunctionParams");

        public static NetworkHost Instance { get; private set; }

        [SerializeField] private SamplePoint targetPointPrefab;
        [SerializeField] private SamplePoint unwantedPointPrefab;
        [SerializeField] private Material inputLayerMaterial;
        [SerializeField] private Material hiddenLayerMaterial;
        [SerializeField] private Material outputLayerMaterial;
        
        [SerializeField] private UnityEvent<string> OnDisplayName;
        [SerializeField] private UnityEvent<bool> OnOrbitalCamera;
        [SerializeField] private UnityEvent<bool> OnPlanarCamera;
        [SerializeField] private UnityEvent OnSceneLoaded;

        private NeuralScene currentScene;
        private UnityEngine.Pool.ObjectPool<SamplePoint> targetPool;
        private UnityEngine.Pool.ObjectPool<SamplePoint> unwantedPool;
        private NeuralRenderPass networkPass;
        private bool enqueuePass = false;
        
        public ActivationFunction CurrentActivationFunction { get; private set; } = ActivationFunction.Stepped;
        
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

        public Neuron AA { get; set; } = new();
        public Neuron AB { get; set; } = new();
        public Neuron AC { get; set; } = new();
        public Neuron AD { get; set; } = new();
        public Neuron BA { get; set; } = new();
        public Neuron BB { get; set; } = new();
        public Neuron BC { get; set; } = new();
        public Neuron BD { get; set; } = new();
        public Neuron Output { get; set; } = new();

        private void Awake() {
            Instance = this;
            targetPool = new(OnCreateTargetPoint, OnGetPoint, OnReleasePoint, OnDestroyPoint);
            unwantedPool = new(OnCreateUnwantedPoint, OnGetPoint, OnReleasePoint, OnDestroyPoint);
        }

        private void OnEnable() {
            networkPass = new() {
                inputLayerMaterial = inputLayerMaterial,
                hiddenLayerMaterial = hiddenLayerMaterial,
                outputLayerMaterial = outputLayerMaterial
            };
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable() {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            networkPass.Dispose();
            networkPass = null;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam) {
            if (enqueuePass) {
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
                enqueuePass = false;
            }
        }


        private SamplePoint CreatePoint(SamplePoint prefab, UnityEngine.Pool.ObjectPool<SamplePoint> pool) {
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
            SamplePoint.UpdatePoints();
        }

        public void Randomize() {
            Neuron.Randomize(AA);
            Neuron.Randomize(AB);
            Neuron.Randomize(AC);
            Neuron.Randomize(AD);
            Neuron.Randomize(BA);
            Neuron.Randomize(BB);
            Neuron.Randomize(BC);
            Neuron.Randomize(BD);
            Neuron.Randomize(Output);
            NotifyChange();
        }

        public float EvaluateAt(Vector3 point) {
            float value = 0f;
            return value;
        }
    }
}