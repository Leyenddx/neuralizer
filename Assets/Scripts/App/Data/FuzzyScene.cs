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

        /// <summary>
        /// Cómo se califica el nivel: ordenando los casos por la salida del sistema
        /// (<see cref="FuzzyExerciseMode.Ranking"/>) o clasificando cada caso en una etiqueta de la
        /// primera variable de entrada (<see cref="FuzzyExerciseMode.Classification"/>, para niveles
        /// introductorios sin reglas ni salida).
        /// </summary>
        [Header("Ejercicio")]
        public FuzzyExerciseMode mode = FuzzyExerciseMode.Ranking;

        /// <summary>El jugador puede mover el peso de cada regla.</summary>
        public bool editableRuleWeights = true;

        /// <summary>El jugador puede mover los puntos a/b/c/d de las etiquetas de las variables de entrada.</summary>
        public bool editableMemberships;

        /// <summary>Muestra la superficie de control (salida en función de las dos primeras entradas) como vista extra.</summary>
        public bool showControlSurface;

        /// <summary>
        /// Opcional: segundo juego de variables de entrada con OTRA forma de curva (p. ej. trapezoidal
        /// si <see cref="FuzzySystem.inputs"/> es triangular). Si no está vacío, el jugador puede alternar
        /// entre ambos para ver que el mismo problema se puede modelar de distintas maneras. Debe tener
        /// las mismas variables y etiquetas (mismos nombres y orden) que el juego principal.
        /// </summary>
        [Header("Formas alternativas (opcional)")]
        public FuzzyVariable[] alternateInputs = System.Array.Empty<FuzzyVariable>();

        /// <summary>Nombre que se muestra para el juego principal de curvas (<see cref="FuzzySystem.inputs"/>).</summary>
        public string primaryShapeName = "Triangular";

        /// <summary>Nombre que se muestra para <see cref="alternateInputs"/>.</summary>
        public string alternateShapeName = "Trapezoidal";

        /// <summary>El juego principal es triangular: se edita con 3 puntos (a, pico, d) en vez de 4.</summary>
        public bool primaryIsTriangular;

        /// <summary><see cref="alternateInputs"/> es triangular: se edita con 3 puntos (a, pico, d).</summary>
        public bool alternateIsTriangular;
    }

    /// <summary>
    /// Forma de calificar un <see cref="FuzzyScene"/>
    /// </summary>
    public enum FuzzyExerciseMode {
        /// <summary>Los casos se ordenan por la salida defuzzificada y se comparan contra <see cref="FuzzyCase.expectedRank"/></summary>
        Ranking = 0,
        /// <summary>Cada caso se clasifica en la etiqueta de mayor pertenencia de la primera entrada y se compara contra <see cref="FuzzyCase.expectedLabel"/></summary>
        Classification = 1
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

        /// <summary>
        /// Solo en modo <see cref="FuzzyExerciseMode.Classification"/>: etiqueta de la primera variable
        /// de entrada en la que este caso debería quedar con mayor pertenencia (ej. "Eco")
        /// </summary>
        public string expectedLabel;
    }
}
