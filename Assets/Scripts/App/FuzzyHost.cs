using System;
using Neuralizer.Data;
using Neuralizer.Fuzzy;
using UnityEngine;
using UnityEngine.Events;

namespace Neuralizer {

    /// <summary>
    /// Anfitrión runtime de un nivel de lógica difusa: carga una <see cref="FuzzyScene"/>, evalúa
    /// el sistema difuso y notifica a la UI.
    /// </summary>
    /// <remarks>
    /// A diferencia de <see cref="NetworkHost"/>, la cantidad de variables, etiquetas y reglas es
    /// distinta en cada nivel — no hay un número fijo de "neuronas". Por eso esos datos NO se exponen
    /// como UnityEvents individuales cableados a mano en el Inspector (eso fue justo el problema que
    /// detectamos en el Módulo 1: un campo por cada elemento no escala). En vez de eso se exponen como
    /// eventos de C# normales (<see cref="OnVariablesLoaded"/>, <see cref="OnCasesLoaded"/>,
    /// <see cref="OnRankingChanged"/>) que un componente constructor de UI (ver <c>FuzzyVariableUI</c>)
    /// debe escuchar por código y usarlos para instanciar la cantidad correcta de elementos dinámicamente.
    /// Los eventos simples (texto, booleanos, sprites) sí se exponen como UnityEvent normales, igual que
    /// en <see cref="NetworkHost"/>, para poder cablearlos directo en el Inspector.
    /// </remarks>
    public class FuzzyHost : MonoBehaviour {

        public static FuzzyHost Instance { get; private set; }

        [SerializeField] private MedalsAsset medals;

        [Header("Eventos simples (cablear en el Inspector)")]
        [SerializeField] private UnityEvent<string> OnDisplayName;
        [SerializeField] private UnityEvent<string> OnNarrativeContext;
        [SerializeField] private UnityEvent OnSceneLoaded;
        [SerializeField] private UnityEvent OnSceneUnloaded;
        [SerializeField] private UnityEvent<bool> OnAllowScoring;
        [SerializeField] private UnityEvent OnInvalidSubmitted;
        [SerializeField] private UnityEvent OnValidSubmitted;
        [SerializeField] private UnityEvent<string> OnTimeString;
        [SerializeField] private UnityEvent<string> OnScoreString;
        [SerializeField] private UnityEvent<bool> OnShowMedal;
        [SerializeField] private UnityEvent<string> OnMedalName;
        [SerializeField] private UnityEvent<Sprite> OnMedalSprite;
        [SerializeField] private UnityEvent<string> OnMedalDescription;

        /// <summary>
        /// Se dispara con las variables de entrada del nivel cargado. Suscríbete por código
        /// (ej. desde un componente que construya la UI dinámicamente, uno por variable).
        /// </summary>
        public event Action<FuzzyVariable[]> OnVariablesLoaded;

        /// <summary>
        /// Se dispara con los casos de prueba (aliados/objetos) del nivel cargado.
        /// </summary>
        public event Action<FuzzyCase[]> OnCasesLoaded;

        /// <summary>
        /// Se dispara cada vez que se re-evalúa el sistema, con el orden resultante (índices dentro
        /// de <see cref="FuzzyScene.cases"/> del caso con mayor prioridad al de menor).
        /// </summary>
        public event Action<int[]> OnRankingChanged;

        /// <summary>
        /// Se dispara al llamar <see cref="SubmitResult"/>, con <b>true</b> si el ranking calculado
        /// coincidió con el orden esperado. Complementa a <see cref="OnValidSubmitted"/>/
        /// <see cref="OnInvalidSubmitted"/> (esos son UnityEvents para cablear en el Inspector; este
        /// es un evento de C# normal para consumidores por código, como UI construida dinámicamente).
        /// </summary>
        public event Action<bool> OnResultEvaluated;

        /// <summary>
        /// Se dispara junto a <see cref="OnResultEvaluated"/> con un puntaje continuo [0, 1] de "qué
        /// tan bien" quedó el resultado, en vez de un simple sí/no. Con lógica difusa el resultado
        /// nunca es realmente binario — puede estar "casi bien" o "muy mal" — así que la UI puede usar
        /// este valor para mostrar una escala en vez de solo verde/rojo. Ver <see cref="EvaluateQuality"/>.
        /// </summary>
        public event Action<float> OnQualityEvaluated;

        /// <summary>
        /// Solo en niveles <see cref="FuzzyExerciseMode.Classification"/>: se dispara cada vez que se
        /// re-evalúa, con el índice de etiqueta (de la primera variable de entrada) que ganó para cada
        /// caso, en el mismo orden que <see cref="FuzzyScene.cases"/>. -1 si ninguna etiqueta tiene
        /// pertenencia o hay empate.
        /// </summary>
        public event Action<int[]> OnClassificationChanged;

        /// <summary>
        /// Diferencia mínima (en unidades de la salida) para que dos casos cuenten como "ordenados":
        /// un empate exacto significa que el sistema no sabe decidir, así que no se acepta como correcto.
        /// </summary>
        private const float TieEpsilon = 0.01f;

        private FuzzyScene currentScene;
        private float sceneStartTime;
        private float[] rankedScores = Array.Empty<float>();
        private int[] rankOrder = Array.Empty<int>();
        private int[] classIndex = Array.Empty<int>();

        /// <summary>Salida defuzzificada actual del caso <paramref name="caseIndex"/> (modo Ranking).</summary>
        public float ScoreOf(int caseIndex) => rankedScores[caseIndex];

        /// <summary>Etiqueta ganadora actual del caso <paramref name="caseIndex"/>, o -1 (modo Classification).</summary>
        public int ClassificationOf(int caseIndex) => classIndex[caseIndex];

        private bool IsClassification => currentScene && currentScene.mode == FuzzyExerciseMode.Classification;

        /// <summary>
        /// El nivel de lógica difusa actualmente cargado. Asignar dispara todos los eventos de carga.
        /// </summary>
        public FuzzyScene Scene {
            get => currentScene;
            set {
                currentScene = value;
                if (value) {
                    int caseCount = value.cases.Length;
                    rankedScores = new float[caseCount];
                    rankOrder = new int[caseCount];
                    classIndex = new int[caseCount];

                    OnDisplayName?.Invoke(value.DisplayName);
                    OnNarrativeContext?.Invoke(value.narrativeContext);
                    OnVariablesLoaded?.Invoke(value.system.inputs);
                    OnCasesLoaded?.Invoke(value.cases);
                    OnAllowScoring?.Invoke(value.AllowScoring);
                    OnSceneLoaded?.Invoke();
                    sceneStartTime = Time.time;
                    Evaluate();
                }
                else {
                    OnDisplayName?.Invoke("");
                    OnSceneUnloaded?.Invoke();
                }
            }
        }

        private void Awake() {
            Instance = this;
        }

        /// <summary>
        /// Descarga el nivel actual
        /// </summary>
        public void CloseScene() => Scene = null;

        /// <summary>
        /// Re-evalúa todos los casos de prueba contra el sistema difuso actual y calcula el ranking resultante.
        /// Llamar cada vez que el jugador cambia una variable (función de pertenencia, regla, etc).
        /// </summary>
        public void Evaluate() {
            if (!currentScene) return;
            FuzzySystem system = currentScene.system;
            FuzzyCase[] cases = currentScene.cases;

            if (IsClassification) {
                for (int i = 0; i < cases.Length; i++) {
                    classIndex[i] = Classify(cases[i], out _, out _);
                }
                OnClassificationChanged?.Invoke(classIndex);
                return;
            }

            for (int i = 0; i < cases.Length; i++) {
                rankedScores[i] = system.EvaluateSingle(cases[i].inputValues);
                rankOrder[i] = i;
            }

            // Ordena los índices de mayor a menor puntaje. Insertion sort: N siempre es pequeño
            // (pocos aliados/casos por nivel) y no reserva memoria, a diferencia de Array.Sort con comparador.
            for (int i = 1; i < rankOrder.Length; i++) {
                int key = rankOrder[i];
                float keyScore = rankedScores[key];
                int j = i - 1;
                while (j >= 0 && rankedScores[rankOrder[j]] < keyScore) {
                    rankOrder[j + 1] = rankOrder[j];
                    j--;
                }
                rankOrder[j + 1] = key;
            }

            OnRankingChanged?.Invoke(rankOrder);
        }

        /// <summary>
        /// Evalúa un único valor de salida para un conjunto de entradas arbitrario, sin afectar el
        /// ranking actual. Útil para previsualizar en vivo mientras se edita una función de pertenencia.
        /// </summary>
        public float EvaluateSingle(float[] rawInputValues) {
            return currentScene ? currentScene.system.EvaluateSingle(rawInputValues) : 0f;
        }

        /// <summary>
        /// Calcula qué tan bien el ranking actual coincide con el orden esperado, como un puntaje
        /// continuo entre 0 y 1 — no un simple sí/no. Para cada par de casos consecutivos en el orden
        /// esperado, mide el margen real (la diferencia de puntaje entre ambos, normalizada por el
        /// rango de la variable de salida) y lo convierte en [0, 1]: 0.5 es un empate exacto, 1 es el
        /// caso que debería ir primero ganando con el máximo margen posible, 0 es lo contrario (perdiendo
        /// con el máximo margen). El resultado final es el promedio de todos los pares. Así, subir el
        /// peso de una regla "a medias" ya se refleja como una mejora parcial, no como un fallo seco.
        /// </summary>
        public float EvaluateQuality() {
            if (!currentScene) return 1f;

            if (IsClassification) {
                // Por caso: 0.5 + la mitad del margen entre la pertenencia a la etiqueta esperada y la
                // mejor de las demás. 1 = totalmente dentro de la esperada, 0 = totalmente en otra.
                FuzzyCase[] cls = currentScene.cases;
                if (cls.Length == 0) return 1f;
                float total = 0f;
                for (int i = 0; i < cls.Length; i++) {
                    Classify(cls[i], out float expectedDegree, out float bestOtherDegree);
                    total += Mathf.Clamp01(0.5f + 0.5f * (expectedDegree - bestOtherDegree));
                }
                return total / cls.Length;
            }

            if (rankOrder.Length < 2 || currentScene.system.outputs.Length == 0) return 1f;

            FuzzyCase[] cases = currentScene.cases;
            int n = cases.Length;
            float[] scoreByExpectedRank = new float[n];
            for (int i = 0; i < n; i++) {
                scoreByExpectedRank[cases[i].expectedRank] = rankedScores[i];
            }

            Vector2 outRange = currentScene.system.outputs[0].Range;
            float span = Mathf.Max(0.0001f, outRange.y - outRange.x);

            float sum = 0f;
            int pairs = n - 1;
            for (int r = 0; r < pairs; r++) {
                float margin = scoreByExpectedRank[r] - scoreByExpectedRank[r + 1];
                sum += Mathf.Clamp01(0.5f + margin / span);
            }
            return sum / pairs;
        }

        /// <summary>
        /// Envía el resultado actual a calificación: el nivel se considera resuelto si el ranking
        /// calculado coincide exactamente con el orden esperado de cada caso de prueba. El puntaje
        /// continuo (<see cref="EvaluateQuality"/>) se informa aparte vía <see cref="OnQualityEvaluated"/>
        /// para que la UI pueda mostrar matices además del resultado binario de calificación.
        /// </summary>
        public void SubmitResult() {
            if (!currentScene) return;
            bool correct = IsCurrentResultCorrect();

            OnQualityEvaluated?.Invoke(EvaluateQuality());
            OnResultEvaluated?.Invoke(correct);

            if (correct) {
                OnValidSubmitted?.Invoke();
                if (currentScene.AllowScoring) {
                    bool awardMedal = currentScene.SubmitResult(Time.time - sceneStartTime);
                    OnShowMedal?.Invoke(awardMedal);
                    if (awardMedal) {
                        Medal medal = medals ? medals.GetFromRatio(currentScene.NormalizedScore) : null;
                        OnTimeString?.Invoke(currentScene.FormattedTime);
                        OnScoreString?.Invoke(currentScene.Score.ToString());
                        OnMedalName?.Invoke(medal != null ? medal.name : "");
                        OnMedalSprite?.Invoke(medal != null ? medal.sprite : null);
                        OnMedalDescription?.Invoke(medal != null ? medal.description : "");
                    }
                }
                currentScene.IsCompleted = true;
            }
            else {
                // Un intento fallido nunca marca el nivel como completado. (La versión heredada de
                // NetworkHost lo hacía para ejercicios sin tabla de puntuación, y el menú del Módulo 2
                // mostraba "Completado" después de fallar.)
                OnInvalidSubmitted?.Invoke();
            }
        }

        /// <summary>
        /// <b>true</b> si el resultado actual resolvería el nivel. Ranking: el orden calculado coincide
        /// con <see cref="FuzzyCase.expectedRank"/> y no hay empates entre posiciones consecutivas.
        /// Classification: cada caso cae en su <see cref="FuzzyCase.expectedLabel"/> sin empate.
        /// </summary>
        public bool IsCurrentResultCorrect() {
            if (!currentScene) return false;
            FuzzyCase[] cases = currentScene.cases;
            if (cases.Length == 0) return false;

            if (IsClassification) {
                FuzzyLabel[] labels = currentScene.system.inputs.Length > 0
                    ? currentScene.system.inputs[0].Labels
                    : Array.Empty<FuzzyLabel>();
                for (int i = 0; i < cases.Length; i++) {
                    int got = classIndex[i];
                    if (got < 0 || labels[got].Label != cases[i].expectedLabel) return false;
                }
                return true;
            }

            for (int i = 0; i < rankOrder.Length; i++) {
                if (cases[rankOrder[i]].expectedRank != i) return false;
                if (i > 0 && rankedScores[rankOrder[i - 1]] - rankedScores[rankOrder[i]] < TieEpsilon) return false;
            }
            return true;
        }

        /// <summary>
        /// Etiqueta de la primera variable de entrada con mayor pertenencia para el caso dado (-1 si
        /// ninguna pertenece o hay empate en el máximo). También devuelve la pertenencia a la etiqueta
        /// esperada del caso y la mejor pertenencia entre las demás, para el puntaje continuo.
        /// </summary>
        private int Classify(FuzzyCase fuzzyCase, out float expectedDegree, out float bestOtherDegree) {
            expectedDegree = 0f;
            bestOtherDegree = 0f;
            FuzzyVariable[] inputs = currentScene.system.inputs;
            if (inputs.Length == 0 || fuzzyCase.inputValues.Length == 0) return -1;

            FuzzyLabel[] labels = inputs[0].Labels;
            float x = fuzzyCase.inputValues[0];
            int best = -1;
            float bestDegree = 0f;
            float secondDegree = 0f;
            for (int l = 0; l < labels.Length; l++) {
                float degree = labels[l].Evaluate(x);
                if (labels[l].Label == fuzzyCase.expectedLabel) expectedDegree = degree;
                else if (degree > bestOtherDegree) bestOtherDegree = degree;

                if (degree > bestDegree) {
                    secondDegree = bestDegree;
                    bestDegree = degree;
                    best = l;
                }
                else if (degree > secondDegree) {
                    secondDegree = degree;
                }
            }
            return bestDegree - secondDegree < TieEpsilon ? -1 : best;
        }
    }
}
