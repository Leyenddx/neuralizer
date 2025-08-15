using Neuralizer.NetworkRendering;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Neuralizer {

    [System.Serializable]
    public class Neuron {

        public static float Sigmoid(float x) {
            return 1f / (1f + Mathf.Exp(-x));
        }
        
        public static float Tanh(float x) {
            float e = Mathf.Exp(2f * x);
            return (e - 1f) / (e + 1f);
        }

        public bool enabled = true;
        public Vector4 weights;
        public float bias;
        public ActivationFunction activationFunction;
        public Material material;

        public float GetIntensity(float value) {
            float abs = Mathf.Abs(value);
            return Mathf.LerpUnclamped(0.15f, 1f, abs / (abs + 1.618f));
        }
        
        public float IntensityX => GetIntensity(weights.x);
        
        public float IntensityY => GetIntensity(weights.y);
        
        public float IntensityZ => GetIntensity(weights.z);
        
        public float IntensityW => GetIntensity(weights.w);
        
        public float EvaluateActivationFunction(float value) {
            return activationFunction switch {
                ActivationFunction.Sigmoid => Sigmoid(value),
                ActivationFunction.TanH => Tanh(value),
                ActivationFunction.ReLu => Mathf.Max(0f, value),
                ActivationFunction.Stepped => value > 0 ? 1f : 0f,
                _ => value
            };
        }

        public float Sample(Vector3 value) {
            return enabled ? EvaluateActivationFunction(Vector3.Dot(value, weights) + bias) : 0f;
        }

        public float Sample(Vector4 value) {
            return enabled ? EvaluateActivationFunction(Vector4.Dot(value, weights) + bias) : 0f;
        }

        public float Sample(Vector2 value) {
            return enabled ? EvaluateActivationFunction(Vector2.Dot(value, weights) + bias) : 0f;
        }

        public void PrepareForRenderPass(Material baseMaterial) {
            material = Object.Instantiate(baseMaterial);
        }

        public void SetShaderData() {
            NeuralRenderPass.SetShaderData(this);
        }

        public TextureHandle SetupRenderTarget(string name, RenderGraph renderGraph, RenderTextureDescriptor descriptor) {
            return UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, name,
                false,
                FilterMode.Bilinear);
        }

        public static void Randomize(Neuron target) {
            target.weights.Set((Random.value - 0.5f) * 10f, (Random.value - 0.5f) * 10f, (Random.value - 0.5f) * 10f,
                (Random.value - 0.5f) * 10f);
            target.bias = Random.Range(-5f, 5f);
            target.activationFunction = (ActivationFunction)Random.Range(0, 4);
        }
    }
}