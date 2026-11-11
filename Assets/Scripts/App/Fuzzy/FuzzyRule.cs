using UnityEngine;

namespace Neuralizer.Fuzzy {

    /// <summary>
    /// Forma de combinar los antecedentes de una regla difusa
    /// </summary>
    public enum FuzzyOperator {
        /// <summary>
        /// Combina antecedentes con mínimo (todas las condiciones deben cumplirse)
        /// </summary>
        And = 0,
        /// <summary>
        /// Combina antecedentes con máximo (basta con que una condición se cumpla)
        /// </summary>
        Or = 1
    }

    /// <summary>
    /// Un antecedente de una regla: "la variable de entrada #<see cref="inputIndex"/> ES <see cref="label"/>"
    /// </summary>
    [System.Serializable]
    public struct FuzzyCondition {
        /// <summary>
        /// Índice de la variable de entrada dentro de <see cref="FuzzySystem.inputs"/>
        /// </summary>
        public int inputIndex;

        /// <summary>
        /// Nombre de la etiqueta lingüística de la variable (ej. "Bajo")
        /// </summary>
        public string label;
    }

    /// <summary>
    /// Regla difusa: SI (condiciones combinadas con Y/O) ENTONCES la variable de salida ES una etiqueta
    /// </summary>
    [System.Serializable]
    public class FuzzyRule {

        /// <summary>
        /// Descripción libre de la regla, solo para referencia en el editor (ej. "SI HP es Bajo Y Daño es Alto...")
        /// </summary>
        public string description;

        public FuzzyCondition[] conditions = System.Array.Empty<FuzzyCondition>();

        public FuzzyOperator combineWith = FuzzyOperator.And;

        /// <summary>
        /// Índice de la variable de salida dentro de <see cref="FuzzySystem.outputs"/>
        /// </summary>
        public int outputIndex;

        /// <summary>
        /// Nombre de la etiqueta de salida (ej. "Alta")
        /// </summary>
        public string outputLabel;

        /// <summary>
        /// Peso de la regla [0, 1]. Permite atenuar reglas secundarias sin borrarlas.
        /// </summary>
        [Range(0f, 1f)] public float weight = 1f;

        /// <summary>
        /// Evalúa el grado de activación (fuerza) de esta regla dado un arreglo de entradas crudas
        /// </summary>
        /// <param name="inputs">Variables de entrada del sistema, en el mismo orden que <see cref="FuzzySystem.inputs"/></param>
        /// <param name="rawInputValues">Valor crudo (no difuso) de cada variable de entrada</param>
        public float Strength(FuzzyVariable[] inputs, float[] rawInputValues) {
            if (conditions.Length == 0) return 0f;

            float result = combineWith == FuzzyOperator.And ? 1f : 0f;
            for (int i = 0; i < conditions.Length; i++) {
                FuzzyCondition c = conditions[i];
                float degree = inputs[c.inputIndex].DegreeOf(c.label, rawInputValues[c.inputIndex]);
                result = combineWith == FuzzyOperator.And ? Mathf.Min(result, degree) : Mathf.Max(result, degree);
            }
            return result * weight;
        }
    }
}
