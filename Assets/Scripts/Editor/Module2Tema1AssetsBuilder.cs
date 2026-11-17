using Neuralizer.Data;
using Neuralizer.Fuzzy;
using Neuralizer.UI;
using UnityEditor;
using UnityEngine;

namespace Neuralizer.EditorTools {

    /// <summary>
    /// Crea (o actualiza en su lugar) el EJERCICIO del Tema 1 del Módulo 2 como asset
    /// <see cref="FuzzyScene"/>: "Distancia del enemigo" (ambientado en Fortnite). El jugador ajusta
    /// las funciones de pertenencia de la variable lingüística <b>Distancia</b> (Cerca, Medio, Lejos)
    /// hasta que cada enemigo quede clasificado en su conjunto. Se juega en <c>FuzzyLevel.unity</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Trae DOS juegos de curvas para el mismo problema — triangulares (principal) y trapezoidales
    /// (alternativo) — y el jugador elige con cuál resolverlo. Ambos arrancan mal calibrados ("todo le
    /// parece cerca") con 3 de 5 enemigos bien clasificados, y ambos tienen solución:
    /// </para>
    /// <list type="bullet">
    /// <item>Triangular — inicio: Cerca (0, 0, 160) · Medio (60, 180, 290) · Lejos (200, 300, 300).
    /// Referencia: Cerca (0, 0, 80) · Medio (40, 110, 200) · Lejos (150, 300, 300).</item>
    /// <item>Trapezoidal — inicio: Cerca (0, 0, 80, 130) · Medio (80, 130, 220, 260) · Lejos (220, 260, 300, 300).
    /// Referencia: Cerca (0, 0, 40, 70) · Medio (40, 70, 150, 190) · Lejos (150, 190, 300, 300).</item>
    /// </list>
    /// <para>
    /// El arma sugerida (escopeta / rifle / francotirador) es solo ambientación en este tema: las reglas
    /// SI–ENTONCES llegan en el tema siguiente.
    /// </para>
    /// </remarks>
    public static class Module2Tema1AssetsBuilder {

        public const string LevelPath = Module2LevelAssetsBuilder.Folder + "/Tema 1 Distancia.asset";

        private static readonly Color Near = EsportsTheme.CurveAlto;
        private static readonly Color Mid = EsportsTheme.NeonPurple;
        private static readonly Color Far = EsportsTheme.CurveBajo;

        [MenuItem("Tools/Neuralizer/Módulo 2/2. Tema 1 · Ejercicio (Distancia)", false, 21)]
        public static void Build() {
            if (!AssetDatabase.IsValidFolder(Module2LevelAssetsBuilder.Folder)) {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "FuzzyScenes");
            }

            FuzzyScene s = AssetDatabase.LoadAssetAtPath<FuzzyScene>(LevelPath);
            if (!s) {
                s = ScriptableObject.CreateInstance<FuzzyScene>();
                AssetDatabase.CreateAsset(s, LevelPath);
            }

            s.narrativeContext = "Tu escuadra decide el arma según la distancia del enemigo: cerca, escopeta; " +
                                 "medio, rifle de asalto; lejos, francotirador. Pero el visor está mal calibrado y " +
                                 "casi todo le parece cerca. Ajusta las curvas de Distancia.";
            s.mode = FuzzyExerciseMode.Classification;
            s.editableRuleWeights = false;
            s.editableMemberships = true;
            s.showControlSurface = false;

            s.system = new FuzzySystem {
                inputs = new[] {
                    new FuzzyVariable("Distancia", 0f, 300f,
                        new FuzzyLabel("Cerca", MembershipFunction.Triangular(0f, 0f, 160f), Near),
                        new FuzzyLabel("Medio", MembershipFunction.Triangular(60f, 180f, 290f), Mid),
                        new FuzzyLabel("Lejos", MembershipFunction.Triangular(200f, 300f, 300f), Far))
                },
                outputs = System.Array.Empty<FuzzyVariable>(),
                rules = System.Array.Empty<FuzzyRule>()
            };
            s.alternateInputs = new[] {
                new FuzzyVariable("Distancia", 0f, 300f,
                    new FuzzyLabel("Cerca", MembershipFunction.Trapezoidal(0f, 0f, 80f, 130f), Near),
                    new FuzzyLabel("Medio", MembershipFunction.Trapezoidal(80f, 130f, 220f, 260f), Mid),
                    new FuzzyLabel("Lejos", MembershipFunction.Trapezoidal(220f, 260f, 300f, 300f), Far))
            };
            s.primaryShapeName = "Triangular";
            s.alternateShapeName = "Trapezoidal";
            s.primaryIsTriangular = true;
            s.alternateIsTriangular = false;

            s.cases = new[] {
                Enemy("Rusher", 12f, "Cerca"),
                Enemy("Emboscada", 45f, "Cerca"),
                Enemy("Duelo", 90f, "Medio"),
                Enemy("Colina", 140f, "Medio"),
                Enemy("Sniper", 230f, "Lejos"),
            };
            EditorUtility.SetDirty(s);

            SerializedObject so = new(s);
            so.Update();
            so.FindProperty("displayName").stringValue = "Distancia del enemigo";
            so.FindProperty("storageName").stringValue = "m2_tema1_distancia";
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            Debug.Log("[Module2Tema1AssetsBuilder] Ejercicio del Tema 1 creado/actualizado en " + LevelPath);
        }

        private static FuzzyCase Enemy(string label, float distance, string expectedLabel) {
            return new FuzzyCase { label = label, inputValues = new[] { distance }, expectedLabel = expectedLabel };
        }
    }
}
