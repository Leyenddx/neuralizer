using Neuralizer.Fuzzy;
using UnityEngine;

namespace Neuralizer.Data {

    /// <summary>
    /// Contenedor de datos de un nivel del módulo de lógica difusa. Equivalente a
    /// <see cref="NeuralScene"/> pero para un <see cref="Neuralizer.Fuzzy.FuzzySystem"/> en vez
    /// de una red neuronal: variables, reglas y los casos de prueba que el jugador debe ordenar/clasificar.
    /// </summary>
    [CreateAssetMenu(fileName = "Fuzzy Scene", menuName = "Neuralizer/Fuzzy Scene")]
    public class FuzzyScene : ScorableExercise {

        /// <summary>
        /// Breve texto de contexto narrativo que se muestra al jugador (ej. "Dos aliados en crítico, ¿a quién curas?")
        /// </summary>
        [Header("Presentación")]
        [TextArea]
        public string narrativeContext;

        /// <summary>
        /// El sistema de inferencia difusa de este nivel (variables de entrada/salida y reglas)
        /// </summary>
        [Header("Sistema Difuso")]
        public FuzzySystem system = new();

        /// <summary>
        /// Los casos de prueba (aliados/objetos) que el jugador debe ordenar correctamente para resolver el nivel
        /// </summary>
        [Header("Casos de prueba")]
        public FuzzyCase[] cases = System.Array.Empty<FuzzyCase>();
    }

    /// <summary>
    /// Un caso de prueba: un aliado/objeto con valores concretos para cada variable de entrada,
    /// y la posición esperada en el ranking de resultado para considerar el nivel resuelto.
    /// </summary>
    [System.Serializable]
    public class FuzzyCase {

        /// <summary>
        /// Nombre para mostrar en la interfaz (ej. "Aliado A", "Mid Lane")
        /// </summary>
        public string label = "Caso A";

        /// <summary>
        /// Un valor por cada variable de entrada del sistema, en el mismo orden que <see cref="FuzzySystem.inputs"/>
        /// </summary>
        public float[] inputValues = System.Array.Empty<float>();

        /// <summary>
        /// Posición esperada en el ranking de salida (0 = debe resolverse/atenderse primero)
        /// </summary>
        public int expectedRank;
    }
}
