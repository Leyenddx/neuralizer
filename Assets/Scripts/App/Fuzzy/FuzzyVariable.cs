using System;
using UnityEngine;

namespace Neuralizer.Fuzzy {

    /// <summary>
    /// Una etiqueta lingüística (ej. "Bajo", "Medio", "Alto") con su función de pertenencia asociada
    /// </summary>
    [System.Serializable]
    public class FuzzyLabel {

        [SerializeField] private string label = "Etiqueta";

        /// <summary>
        /// Función de pertenencia de esta etiqueta dentro del rango de su variable
        /// </summary>
        public MembershipFunction membership = new(0f, 0f, 25f, 50f);

        /// <summary>
        /// Color sugerido para dibujar esta etiqueta en la UI/editor (puramente visual)
        /// </summary>
        public Color displayColor = Color.white;

        public FuzzyLabel() { }

        /// <summary>
        /// Construye una etiqueta directamente desde código (ej. para demos o datos generados,
        /// sin pasar por el Inspector)
        /// </summary>
        public FuzzyLabel(string label, MembershipFunction membership, Color displayColor) {
            this.label = label;
            this.membership = membership;
            this.displayColor = displayColor;
        }

        public string Label => label;

        /// <summary>
        /// Grado de pertenencia [0, 1] de <paramref name="x"/> a esta etiqueta
        /// </summary>
        public float Evaluate(float x) => membership.Evaluate(x);
    }

    /// <summary>
    /// Variable difusa de entrada o salida (ej. "HP", "Daño Entrante", "Prioridad"),
    /// compuesta de varias etiquetas lingüísticas.
    /// </summary>
    [System.Serializable]
    public class FuzzyVariable {

        [SerializeField] private string variableName = "Variable";

        /// <summary>
        /// Rango de valores crudos (no difusos) que puede tomar esta variable, ej. (0, 100) para HP%
        /// </summary>
        [SerializeField] private Vector2 range = new(0f, 100f);

        [SerializeField] private FuzzyLabel[] labels = Array.Empty<FuzzyLabel>();

        public FuzzyVariable() { }

        /// <summary>
        /// Construye una variable directamente desde código (ej. para demos o datos generados,
        /// sin pasar por el Inspector)
        /// </summary>
        public FuzzyVariable(string name, float rangeMin, float rangeMax, params FuzzyLabel[] labels) {
            variableName = name;
            range = new Vector2(rangeMin, rangeMax);
            this.labels = labels;
        }

        public string Name => variableName;

        public Vector2 Range => range;

        public FuzzyLabel[] Labels => labels;

        /// <summary>
        /// Devuelve el grado de pertenencia de <paramref name="x"/> a la etiqueta con el nombre dado.
        /// Devuelve 0 si la etiqueta no existe.
        /// </summary>
        public float DegreeOf(string labelName, float x) {
            for (int i = 0; i < labels.Length; i++) {
                if (labels[i].Label == labelName) return labels[i].Evaluate(x);
            }
            return 0f;
        }

        /// <summary>
        /// Índice de una etiqueta por nombre, o -1 si no existe
        /// </summary>
        public int IndexOf(string labelName) {
            for (int i = 0; i < labels.Length; i++) {
                if (labels[i].Label == labelName) return i;
            }
            return -1;
        }
    }
}
