using System.Collections.Generic;
using Neuralizer.Data;
using Neuralizer.Fuzzy;
using UnityEditor;
using UnityEngine;

namespace Neuralizer.EditorTools {


    [CustomEditor(typeof(FuzzyScene))]
    public class FuzzySceneEditor : UnityEditor.Editor {

        private int previewInputIndex;
        private float[] testValues = System.Array.Empty<float>();

        public override void OnInspectorGUI() {
            serializedObject.Update();
            FuzzyScene scene = (FuzzyScene)target;

            EditorGUILayout.LabelField("Ejercicio", EditorStyles.boldLabel);
            DrawPropertyByName("displayName");
            DrawPropertyByName("storageName");
            DrawPropertyByName("scoreboardID");
            DrawPropertyByName("minTime");
            DrawPropertyByName("minMaxScores");
            DrawPropertyByName("successThreshold");
            DrawPropertyByName("narrativeContext");
            DrawPropertyByName("mode");
            DrawPropertyByName("editableRuleWeights");
            DrawPropertyByName("editableMemberships");
            DrawPropertyByName("showControlSurface");
            DrawPropertyByName("alternateInputs");
            DrawPropertyByName("primaryShapeName");
            DrawPropertyByName("alternateShapeName");
            DrawPropertyByName("primaryIsTriangular");
            DrawPropertyByName("alternateIsTriangular");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Sistema Difuso", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("system.inputs"), new GUIContent("Variables de Entrada"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("system.outputs"), new GUIContent("Variables de Salida"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("system.rules"), new GUIContent("Reglas"), true);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Casos de prueba", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cases"), new GUIContent("Casos"), true);

            serializedObject.ApplyModifiedProperties();

            DrawValidation(scene);
            DrawMembershipPreview(scene);
            DrawLiveTest(scene);
        }

        private void DrawPropertyByName(string propertyName) {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            if (prop != null) EditorGUILayout.PropertyField(prop);
        }

        /// <summary>
        /// Revisa referencias cruzadas (índices de variables/reglas) y avisa de datos inconsistentes
        /// antes de que fallen en silencio durante el juego.
        /// </summary>
        private void DrawValidation(FuzzyScene scene) {
            FuzzySystem system = scene.system;
            List<string> warnings = new();

            for (int r = 0; r < system.rules.Length; r++) {
                FuzzyRule rule = system.rules[r];
                if (rule.outputIndex < 0 || rule.outputIndex >= system.outputs.Length) {
                    warnings.Add($"Regla {r + 1}: el índice de salida ({rule.outputIndex}) no corresponde a ninguna variable de salida.");
                }
                foreach (FuzzyCondition c in rule.conditions) {
                    if (c.inputIndex < 0 || c.inputIndex >= system.inputs.Length) {
                        warnings.Add($"Regla {r + 1}: una condición apunta a una variable de entrada que no existe (índice {c.inputIndex}).");
                    }
                }
            }

            foreach (FuzzyCase fuzzyCase in scene.cases) {
                if (fuzzyCase.inputValues.Length != system.inputs.Length) {
                    warnings.Add($"Caso '{fuzzyCase.label}': tiene {fuzzyCase.inputValues.Length} valores de entrada, pero el sistema define {system.inputs.Length} variables.");
                }
            }

            if (warnings.Count == 0) return;

            EditorGUILayout.Space(10);
            foreach (string warning in warnings) {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }

        /// <summary>
        /// Dibuja las funciones de pertenencia de la variable de entrada seleccionada, para poder
        /// ver la forma de las etiquetas (Bajo/Medio/Alto, etc.) sin tener que hacer cuentas mentales.
        /// </summary>
        private void DrawMembershipPreview(FuzzyScene scene) {
            FuzzySystem system = scene.system;
            if (system.inputs.Length == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Vista previa de funciones de pertenencia", EditorStyles.boldLabel);

            string[] names = new string[system.inputs.Length];
            for (int i = 0; i < names.Length; i++) {
                names[i] = string.IsNullOrEmpty(system.inputs[i].Name) ? $"Variable {i}" : system.inputs[i].Name;
            }

            previewInputIndex = Mathf.Clamp(previewInputIndex, 0, names.Length - 1);
            previewInputIndex = EditorGUILayout.Popup("Variable", previewInputIndex, names);

            FuzzyVariable variable = system.inputs[previewInputIndex];
            Rect rect = GUILayoutUtility.GetRect(10, 140, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.13f));

            if (variable.Labels.Length == 0 || variable.Range.y <= variable.Range.x) {
                EditorGUI.LabelField(rect, "Sin etiquetas o rango inválido", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            const int samples = 64;
            foreach (FuzzyLabel label in variable.Labels) {
                Vector3[] points = new Vector3[samples];
                for (int s = 0; s < samples; s++) {
                    float t = s / (float)(samples - 1);
                    float x = Mathf.Lerp(variable.Range.x, variable.Range.y, t);
                    float y = label.Evaluate(x);
                    points[s] = new Vector3(rect.x + t * rect.width, rect.yMax - y * rect.height, 0f);
                }
                Handles.color = label.displayColor.a > 0.01f ? label.displayColor : Color.cyan;
                Handles.DrawAAPolyLine(3f, points);
            }
        }

        /// <summary>
        /// Panel para mover las entradas con sliders y ver el resultado defuzzificado en vivo,
        /// para probar el balance de un nivel sin entrar a Play Mode.
        /// </summary>
        private void DrawLiveTest(FuzzyScene scene) {
            FuzzySystem system = scene.system;
            if (system.inputs.Length == 0 || system.outputs.Length == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Probar en vivo", EditorStyles.boldLabel);

            if (testValues.Length != system.inputs.Length) {
                testValues = new float[system.inputs.Length];
            }

            for (int i = 0; i < system.inputs.Length; i++) {
                FuzzyVariable v = system.inputs[i];
                string label = string.IsNullOrEmpty(v.Name) ? $"Entrada {i}" : v.Name;
                testValues[i] = EditorGUILayout.Slider(label, testValues[i], v.Range.x, v.Range.y);
            }

            float result = system.EvaluateSingle(testValues);
            string outputName = string.IsNullOrEmpty(system.outputs[0].Name) ? "Salida" : system.outputs[0].Name;
            EditorGUILayout.LabelField($"Resultado ({outputName})", result.ToString("0.00"));
        }
    }
}
