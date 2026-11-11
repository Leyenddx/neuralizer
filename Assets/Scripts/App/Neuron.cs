using Neuralizer.NetworkRendering;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Neuralizer {

    /// <summary>
    /// Definición de una neurona dentro de la red
    /// </summary>
    [System.Serializable]
    public class Neuron {

        /// <summary>
        /// Evalua la función sigmoide en <b>x</b>
        /// </summary>
        /// <param name="x">Valor a evaluar</param>
        /// <returns>Resultado de la evaluación de la función sigmoide</returns>
        /// <seealso href="https://es.wikipedia.org/wiki/Funci%C3%B3n_sigmoide"/>
        public static float Sigmoid(float x) {
            return 1f / (1f + Mathf.Exp(-x));
        }
        
        /// <summary>
        /// Evalua la función de tangente hiperbólica en <b>x</b>
        /// </summary>
        /// <param name="x">Valor a evaluar</param>
        /// <returns>Resultado de la evaluación de la función de tangente hiperbólica</returns>
        /// <seealso href="https://es.wikipedia.org/wiki/Tangente_hiperb%C3%B3lica"/>
        public static float Tanh(float x) {
            float e = Mathf.Exp(2f * x);
            return (e - 1f) / (e + 1f);
        }

        /// <summary>
        /// Evalua la función de pertenencia trapezoidal/triangular en <b>x</b> — misma fórmula que
        /// <see cref="Neuralizer.Fuzzy.MembershipFunction"/>, duplicada acá a propósito para que el
        /// lado CPU (<see cref="Sample"/>) y el lado GPU (shader) den exactamente el mismo resultado.
        /// </summary>
        /// <param name="x">Valor a evaluar</param>
        /// <param name="a">Punto donde empieza a subir desde 0</param>
        /// <param name="b">Punto donde llega a 1 subiendo</param>
        /// <param name="c">Punto donde empieza a bajar desde 1</param>
        /// <param name="d">Punto donde vuelve a 0</param>
        public static float Trapezoidal(float x, float a, float b, float c, float d) {
            if (x <= a || x >= d) return 0f;
            if (x < b) return b > a ? (x - a) / (b - a) : 1f;
            if (x <= c) return 1f;
            return d > c ? (d - x) / (d - c) : 1f;
        }

        /// <summary>Campana gaussiana centrada en <paramref name="center"/> con ancho <paramref name="spread"/> — misma fórmula que <see cref="Neuralizer.Fuzzy.MembershipFunction"/>.</summary>
        public static float Gaussiana(float x, float center, float spread) {
            float s = Mathf.Approximately(spread, 0f) ? 0.0001f : spread;
            float t = (x - center) / s;
            return Mathf.Exp(-0.5f * t * t);
        }

        public bool enabled = true;
        public Vector4 weights;
        public float bias;
        /// <summary>
        /// Parámetros extra para formas de activación que necesitan más que "pendiente + sesgo"
        /// (ver <see cref="ActivationFunction.Trapezoidal"/>/<see cref="ActivationFunction.Gaussiana"/>):
        /// (a, b, c, d) para trapezoidal, (centro, ancho, _, _) para gaussiana. Sin efecto en el resto
        /// de las funciones de activación.
        /// </summary>
        public Vector4 shape;
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
                ActivationFunction.Trapezoidal => Trapezoidal(value, shape.x, shape.y, shape.z, shape.w),
                ActivationFunction.Gaussiana => Gaussiana(value, shape.x, shape.y),
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