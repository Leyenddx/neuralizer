using UnityEngine;

namespace Neuralizer.Fuzzy {

    /// <summary>
    /// Motor de inferencia difusa (modelo Mamdani): fuzzificación, evaluación de reglas,
    /// agregación y defuzzificación por centroide.
    /// </summary>
    /// <remarks>
    /// Pensado para builds WebGL: las evaluaciones reutilizan buffers internos en vez de
    /// reservar memoria nueva en cada llamada (evita presión sobre el recolector de basura).
    /// </remarks>
    [System.Serializable]
    public class FuzzySystem {

        private const int SamplesPerOutput = 40;

        public FuzzyVariable[] inputs = System.Array.Empty<FuzzyVariable>();
        public FuzzyVariable[] outputs = System.Array.Empty<FuzzyVariable>();
        public FuzzyRule[] rules = System.Array.Empty<FuzzyRule>();

        // Buffers reutilizables (no se recrean en cada evaluación)
        private float[] ruleStrengths;
        private float[] aggregatedSamples;

        private void EnsureBuffers() {
            if (ruleStrengths == null || ruleStrengths.Length != rules.Length) {
                ruleStrengths = new float[rules.Length];
            }
            aggregatedSamples ??= new float[SamplesPerOutput];
        }

        /// <summary>
        /// Evalúa el sistema completo para un conjunto de entradas y escribe el valor
        /// defuzzificado de cada salida en <paramref name="resultBuffer"/> (sin reservar memoria).
        /// </summary>
        /// <param name="rawInputValues">Valor crudo (no difuso) de cada entrada, en el mismo orden que <see cref="inputs"/></param>
        /// <param name="resultBuffer">Arreglo del mismo tamaño que <see cref="outputs"/> donde se escribe el resultado</param>
        public void Evaluate(float[] rawInputValues, float[] resultBuffer) {
            EnsureBuffers();

            for (int r = 0; r < rules.Length; r++) {
                ruleStrengths[r] = rules[r].Strength(inputs, rawInputValues);
            }

            for (int o = 0; o < outputs.Length; o++) {
                resultBuffer[o] = Defuzzify(o);
            }
        }

        /// <summary>
        /// Variante de <see cref="Evaluate(float[], float[])"/> que reserva y devuelve un arreglo nuevo.
        /// Prefiere la versión con buffer si vas a llamarla seguido (ej. cada frame).
        /// </summary>
        public float[] Evaluate(float[] rawInputValues) {
            float[] result = new float[outputs.Length];
            Evaluate(rawInputValues, result);
            return result;
        }

        /// <summary>
        /// Atajo para sistemas de una sola salida (el caso común de los niveles 1 a 3): evalúa
        /// y defuzzifica únicamente <see cref="outputs"/>[0] sin reservar memoria.
        /// </summary>
        public float EvaluateSingle(float[] rawInputValues) {
            EnsureBuffers();
            for (int r = 0; r < rules.Length; r++) {
                ruleStrengths[r] = rules[r].Strength(inputs, rawInputValues);
            }
            return outputs.Length > 0 ? Defuzzify(0) : 0f;
        }

        /// <summary>
        /// Defuzzifica una variable de salida específica usando el método del centroide,
        /// a partir de las fuerzas de regla ya calculadas en <see cref="ruleStrengths"/>.
        /// </summary>
        private float Defuzzify(int outputIndex) {
            FuzzyVariable output = outputs[outputIndex];
            Vector2 range = output.Range;

            for (int s = 0; s < SamplesPerOutput; s++) {
                aggregatedSamples[s] = 0f;
            }

            for (int r = 0; r < rules.Length; r++) {
                float strength = ruleStrengths[r];
                if (strength <= 0f) continue;
                FuzzyRule rule = rules[r];
                if (rule.outputIndex != outputIndex) continue;

                for (int s = 0; s < SamplesPerOutput; s++) {
                    float x = Mathf.Lerp(range.x, range.y, s / (float)(SamplesPerOutput - 1));
                    float membership = output.DegreeOf(rule.outputLabel, x);
                    float clipped = Mathf.Min(membership, strength);
                    if (clipped > aggregatedSamples[s]) aggregatedSamples[s] = clipped;
                }
            }

            float numerator = 0f;
            float denominator = 0f;
            for (int s = 0; s < SamplesPerOutput; s++) {
                float x = Mathf.Lerp(range.x, range.y, s / (float)(SamplesPerOutput - 1));
                numerator += x * aggregatedSamples[s];
                denominator += aggregatedSamples[s];
            }

            return denominator > 0f ? numerator / denominator : (range.x + range.y) * 0.5f;
        }

        /// <summary>
        /// Utilidad para generar la superficie de control del Nivel 4: evalúa la salida variando
        /// dos entradas sobre una grilla. No pensada para llamarse cada frame, solo cuando cambian
        /// los datos del nivel (ej. al entrar a la escena o al editar una regla).
        /// </summary>
        /// <param name="inputAIndex">Índice de la primer variable de entrada a variar (eje X)</param>
        /// <param name="inputBIndex">Índice de la segunda variable de entrada a variar (eje Y)</param>
        /// <param name="outputIndex">Índice de la variable de salida a muestrear</param>
        /// <param name="resolution">Resolución de la grilla por eje</param>
        /// <param name="fixedInputs">Valores base para el resto de las entradas que no varían</param>
        public float[,] EvaluateSurface(int inputAIndex, int inputBIndex, int outputIndex, int resolution, float[] fixedInputs) {
            float[,] surface = new float[resolution, resolution];
            float[] sample = (float[])fixedInputs.Clone();
            Vector2 rangeA = inputs[inputAIndex].Range;
            Vector2 rangeB = inputs[inputBIndex].Range;
            float[] resultBuffer = new float[outputs.Length];

            for (int i = 0; i < resolution; i++) {
                sample[inputAIndex] = Mathf.Lerp(rangeA.x, rangeA.y, i / (float)(resolution - 1));
                for (int j = 0; j < resolution; j++) {
                    sample[inputBIndex] = Mathf.Lerp(rangeB.x, rangeB.y, j / (float)(resolution - 1));
                    Evaluate(sample, resultBuffer);
                    surface[i, j] = resultBuffer[outputIndex];
                }
            }
            return surface;
        }
    }
}
