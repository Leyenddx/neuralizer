using Neuralizer.Data;
using Neuralizer.Fuzzy;
using Neuralizer.UI;
using UnityEditor;
using UnityEngine;

namespace Neuralizer.EditorTools {

    /// <summary>
    /// Crea (o actualiza en su lugar, conservando el GUID) los 4 niveles de datos del Módulo 2 como
    /// assets <see cref="FuzzyScene"/> en <c>Assets/ScriptableObjects/FuzzyScenes/</c>, con los datos
    /// de <c>FUZZY_MODULE.md</c> §3 más el estado inicial "roto" que el jugador debe corregir.
    /// </summary>
    /// <remarks>
    /// Los estados iniciales se validaron simulando el motor (40 muestras, centroide): cada nivel
    /// arranca mal calificado y tiene solución con los controles que deja editables.
    /// <list type="bullet">
    /// <item>Nivel 1 (clasificación, curvas): las curvas arrancan corridas a la derecha; "Bonus" y
    /// "Ronda ganada" caen en la etiqueta equivocada. Con las curvas del documento, 5/5.</item>
    /// <item>Nivel 2 (ranking, curvas): "Parejo" arranca tan ancho que los tres carriles empatan en 50.
    /// Con las curvas del documento: Top 79.9 &gt; Mid 50 &gt; Bot 28.</item>
    /// <item>Nivel 3 (ranking, pesos): la regla 1 arranca con peso 0 (Reinhardt 75 &gt; D.Va 50). Con
    /// cualquier peso &gt; 0, D.Va pasa a ~88-90.</item>
    /// <item>Nivel 4 (ranking, pesos + superficie): la regla 1 arranca con peso 0 (Tanque 75 &gt;
    /// Tirador 50 &gt; Support 45.7). Al subirla: Tirador ~88-90 &gt; Tanque 75 &gt; Support 45.7.</item>
    /// </list>
    /// Correr esta herramienta otra vez SOBRESCRIBE los datos de los 4 assets con estos valores.
    /// </remarks>
    public static class Module2LevelAssetsBuilder {

        public const string Folder = "Assets/ScriptableObjects/FuzzyScenes";

        private static readonly Color Low = EsportsTheme.CurveBajo;
        private static readonly Color Mid = EsportsTheme.NeonPurple;
        private static readonly Color High = EsportsTheme.CurveAlto;
        private static readonly Color VeryHigh = EsportsTheme.NeonRed;

        [MenuItem("Tools/Neuralizer/Crear Niveles del Módulo 2 (FuzzyScene)")]
        public static void Build() {
            if (!AssetDatabase.IsValidFolder(Folder)) {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "FuzzyScenes");
            }

            BuildLevel1();
            BuildLevel2();
            BuildLevel3();
            BuildLevel4();

            AssetDatabase.SaveAssets();
            Debug.Log("[Module2LevelAssetsBuilder] 4 niveles creados/actualizados en " + Folder);
        }

        // -----------------------------------------------------------------------------------------

        private static void BuildLevel1() {
            FuzzyScene s = LoadOrCreate("Nivel 1 Economia");
            s.narrativeContext = "¿Qué tan preparado está tu equipo para esta ronda? El coach clasifica el dinero " +
                                 "del equipo en Eco, SemiBuy o FullBuy, pero sus curvas están mal calibradas. " +
                                 "Ajústalas para que cada ronda quede en la categoría correcta.";
            s.mode = FuzzyExerciseMode.Classification;
            s.editableRuleWeights = false;
            s.editableMemberships = true;
            s.showControlSurface = false;
            s.system = new FuzzySystem {
                inputs = new[] {
                    new FuzzyVariable("Dinero", 0f, 16000f,
                        new FuzzyLabel("Eco", MembershipFunction.Trapezoidal(0f, 0f, 4000f, 7000f), Low),
                        new FuzzyLabel("SemiBuy", MembershipFunction.Trapezoidal(4000f, 7000f, 9000f, 12000f), Mid),
                        new FuzzyLabel("FullBuy", MembershipFunction.Trapezoidal(9000f, 12000f, 16000f, 16000f), High))
                },
                outputs = System.Array.Empty<FuzzyVariable>(),
                rules = System.Array.Empty<FuzzyRule>()
            };
            s.cases = new[] {
                Case("Pistola", "Eco", 1500f),
                Case("Bonus", "SemiBuy", 4200f),
                Case("Force", "SemiBuy", 6000f),
                Case("Ganada", "FullBuy", 9500f),
                Case("Banco", "FullBuy", 12000f)
            };
            Finish(s, "Economía de Equipo", "m2_nivel1_economia");
        }

        private static void BuildLevel2() {
            FuzzyScene s = LoadOrCreate("Nivel 2 Gank");
            s.narrativeContext = "SI una línea está perdiendo, ENTONCES gankea ahí. El jungla debería priorizar " +
                                 "Top > Mid > Bot, pero la etiqueta \"Parejo\" es tan ancha que todo le parece igual. " +
                                 "Corrige las curvas de DesventajaDeOro.";
            s.mode = FuzzyExerciseMode.Ranking;
            s.editableRuleWeights = false;
            s.editableMemberships = true;
            s.showControlSurface = false;
            s.system = new FuzzySystem {
                inputs = new[] {
                    new FuzzyVariable("DesventajaDeOro", -3000f, 3000f,
                        new FuzzyLabel("Perdiendo", MembershipFunction.Trapezoidal(-3000f, -3000f, -2800f, -2400f), High),
                        new FuzzyLabel("Parejo", MembershipFunction.Trapezoidal(-2800f, -2000f, 2000f, 2800f), Mid),
                        new FuzzyLabel("Ganando", MembershipFunction.Trapezoidal(2400f, 2800f, 3000f, 3000f), Low))
                },
                outputs = new[] { PriorityOutput3("PrioridadGank") },
                rules = new[] {
                    Rule(FuzzyOperator.And, "Alta", 1f, (0, "Perdiendo")),
                    Rule(FuzzyOperator.And, "Media", 1f, (0, "Parejo")),
                    Rule(FuzzyOperator.And, "Baja", 1f, (0, "Ganando"))
                }
            };
            // Orden del arreglo = Bot, Mid, Top a propósito: si el jugador deja empates, el orden
            // estable mostraría Bot primero (incorrecto) en vez de "acertar" por casualidad.
            s.cases = new[] {
                Case("Bot", 2, 1200f),
                Case("Mid", 1, 0f),
                Case("Top", 0, -1800f)
            };
            Finish(s, "Prioridad de Gank", "m2_nivel2_gank");
        }

        private static void BuildLevel3() {
            FuzzyScene s = LoadOrCreate("Nivel 3 Healer");
            s.narrativeContext = "Dos aliados en crítico. ¿A quién curas primero? Tu IA de healer ignora una regla " +
                                 "clave. Ajusta el peso de las reglas para que cure primero a quien corre más riesgo.";
            s.mode = FuzzyExerciseMode.Ranking;
            s.editableRuleWeights = true;
            s.editableMemberships = false;
            s.showControlSurface = false;
            s.system = new FuzzySystem {
                inputs = new[] {
                    new FuzzyVariable("HP", 0f, 100f,
                        new FuzzyLabel("Bajo", MembershipFunction.Trapezoidal(0f, 0f, 30f, 55f), High),
                        new FuzzyLabel("Alto", MembershipFunction.Trapezoidal(35f, 70f, 100f, 100f), Low)),
                    new FuzzyVariable("DañoEntrante", 0f, 100f,
                        new FuzzyLabel("Bajo", MembershipFunction.Trapezoidal(0f, 0f, 30f, 50f), Low),
                        new FuzzyLabel("Alto", MembershipFunction.Trapezoidal(30f, 70f, 100f, 100f), High))
                },
                outputs = new[] { PriorityOutput4("Prioridad") },
                rules = new[] {
                    Rule(FuzzyOperator.And, "MuyAlta", 0f, (0, "Bajo"), (1, "Alto")),
                    Rule(FuzzyOperator.And, "Alta", 1f, (0, "Bajo"), (1, "Bajo")),
                    Rule(FuzzyOperator.And, "Baja", 1f, (0, "Alto"))
                }
            };
            s.cases = new[] {
                Case("Reinhardt", 1, 18f, 20f),
                Case("D.Va", 0, 16f, 85f)
            };
            Finish(s, "El Dilema del Healer", "m2_nivel3_healer");
        }

        private static void BuildLevel4() {
            FuzzyScene s = LoadOrCreate("Nivel 4 Objetivo");
            s.narrativeContext = "Focus fire: ¿a quién ataca primero el equipo? Hoy el equipo ignora a los enemigos " +
                                 "frágiles pero peligrosos. Ajusta las reglas y mira cómo cambia la superficie de control.";
            s.mode = FuzzyExerciseMode.Ranking;
            s.editableRuleWeights = true;
            s.editableMemberships = false;
            s.showControlSurface = true;
            s.system = new FuzzySystem {
                inputs = new[] {
                    new FuzzyVariable("VidaRestante", 0f, 100f,
                        new FuzzyLabel("Baja", MembershipFunction.Trapezoidal(0f, 0f, 30f, 50f), High),
                        new FuzzyLabel("Alta", MembershipFunction.Trapezoidal(30f, 70f, 100f, 100f), Low)),
                    new FuzzyVariable("Amenaza", 0f, 100f,
                        new FuzzyLabel("Baja", MembershipFunction.Trapezoidal(0f, 0f, 30f, 50f), Low),
                        new FuzzyLabel("Alta", MembershipFunction.Trapezoidal(30f, 70f, 100f, 100f), High))
                },
                outputs = new[] { PriorityOutput4("PrioridadObjetivo") },
                rules = new[] {
                    Rule(FuzzyOperator.And, "MuyAlta", 0f, (0, "Baja"), (1, "Alta")),
                    Rule(FuzzyOperator.And, "Media", 1f, (0, "Baja"), (1, "Baja")),
                    Rule(FuzzyOperator.And, "Alta", 1f, (0, "Alta"), (1, "Alta")),
                    Rule(FuzzyOperator.And, "Baja", 1f, (0, "Alta"), (1, "Baja"))
                }
            };
            s.cases = new[] {
                Case("Tanque", 1, 80f, 70f),
                Case("Support", 2, 35f, 20f),
                Case("Tirador", 0, 25f, 85f)
            };
            Finish(s, "Prioridad de Objetivo", "m2_nivel4_objetivo");
        }

        // -----------------------------------------------------------------------------------------

        private static FuzzyVariable PriorityOutput3(string name) {
            return new FuzzyVariable(name, 0f, 100f,
                new FuzzyLabel("Baja", MembershipFunction.Trapezoidal(0f, 0f, 25f, 50f), Low),
                new FuzzyLabel("Media", MembershipFunction.Triangular(25f, 50f, 75f), Mid),
                new FuzzyLabel("Alta", MembershipFunction.Trapezoidal(50f, 75f, 100f, 100f), High));
        }

        private static FuzzyVariable PriorityOutput4(string name) {
            return new FuzzyVariable(name, 0f, 100f,
                new FuzzyLabel("Baja", MembershipFunction.Trapezoidal(0f, 0f, 25f, 50f), Low),
                new FuzzyLabel("Media", MembershipFunction.Triangular(25f, 50f, 75f), Mid),
                new FuzzyLabel("Alta", MembershipFunction.Triangular(50f, 75f, 100f), High),
                new FuzzyLabel("MuyAlta", MembershipFunction.Trapezoidal(75f, 90f, 100f, 100f), VeryHigh));
        }

        private static FuzzyRule Rule(FuzzyOperator op, string outputLabel, float weight,
            params (int input, string label)[] conditions) {
            FuzzyCondition[] conds = new FuzzyCondition[conditions.Length];
            for (int i = 0; i < conditions.Length; i++) {
                conds[i] = new FuzzyCondition { inputIndex = conditions[i].input, label = conditions[i].label };
            }
            return new FuzzyRule {
                conditions = conds,
                combineWith = op,
                outputIndex = 0,
                outputLabel = outputLabel,
                weight = weight
            };
        }

        private static FuzzyCase Case(string label, int expectedRank, params float[] inputs) {
            return new FuzzyCase { label = label, expectedRank = expectedRank, inputValues = inputs };
        }

        private static FuzzyCase Case(string label, string expectedLabel, params float[] inputs) {
            return new FuzzyCase { label = label, expectedLabel = expectedLabel, inputValues = inputs };
        }

        private static FuzzyScene LoadOrCreate(string fileName) {
            string path = $"{Folder}/{fileName}.asset";
            FuzzyScene scene = AssetDatabase.LoadAssetAtPath<FuzzyScene>(path);
            if (scene) return scene;
            scene = ScriptableObject.CreateInstance<FuzzyScene>();
            AssetDatabase.CreateAsset(scene, path);
            return scene;
        }

        /// <summary>Rellena las descripciones de reglas y los campos privados de <see cref="ScorableExercise"/>.</summary>
        private static void Finish(FuzzyScene scene, string displayName, string storageName) {
            FuzzySystem system = scene.system;
            foreach (FuzzyRule rule in system.rules) {
                rule.description = Describe(system, rule);
            }
            EditorUtility.SetDirty(scene);

            SerializedObject so = new(scene);
            so.Update();
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("storageName").stringValue = storageName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string Describe(FuzzySystem system, FuzzyRule rule) {
            System.Text.StringBuilder sb = new("SI ");
            for (int c = 0; c < rule.conditions.Length; c++) {
                if (c > 0) sb.Append(rule.combineWith == FuzzyOperator.And ? " Y " : " O ");
                sb.Append(system.inputs[rule.conditions[c].inputIndex].Name).Append(" es ").Append(rule.conditions[c].label);
            }
            sb.Append(" ENTONCES ").Append(system.outputs[rule.outputIndex].Name).Append(" es ").Append(rule.outputLabel);
            return sb.ToString();
        }
    }
}
