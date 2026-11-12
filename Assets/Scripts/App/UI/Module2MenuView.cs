using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neuralizer.UI {

    /// <summary>
    /// Estado vivo del panel del Módulo 2 en el menú principal: marca como "COMPLETADO" los niveles
    /// ya resueltos y anima la barra de progreso del encabezado cada vez que el panel se muestra.
    /// Cableado por <c>Editor/Module2MenuBuilder.cs</c>.
    /// </summary>
    /// <remarks>
    /// La barra se llena con <c>fillAmount</c> (600 ms ease-out): es una indicación de estado que se ve
    /// de vez en cuando (al entrar al módulo), no en cada interacción, así que puede permitirse un poco
    /// más de duración que un control. La entrada escalonada de las tarjetas la hace cada
    /// <see cref="M2Motion"/> por su cuenta al activarse el panel.
    /// </remarks>
    public class Module2MenuView : MonoBehaviour {

        private const float FillDuration = 0.6f;

        [SerializeField] private Module2ExerciseEntry[] entries = System.Array.Empty<Module2ExerciseEntry>();
        [Tooltip("Un indicador por entrada (mismo orden); se activa si su nivel está completado. Puede ser null.")]
        [SerializeField] private GameObject[] completedBadges = System.Array.Empty<GameObject>();
        [SerializeField] private TextMeshProUGUI progressLabel;
        [SerializeField] private Image progressFill;

        private float fillTarget;
        private float fillStart = -1f;

        private void OnEnable() {
            int total = 0, done = 0;
            for (int i = 0; i < entries.Length; i++) {
                Data.FuzzyScene level = entries[i] ? entries[i].Level : null;
                bool completed = level && level.IsCompleted;
                if (level) {
                    total++;
                    if (completed) done++;
                }
                if (i < completedBadges.Length && completedBadges[i]) completedBadges[i].SetActive(completed);
            }

            if (progressLabel) progressLabel.text = $"{done}/{total}";
            fillTarget = total > 0 ? done / (float)total : 0f;
            if (progressFill) {
                progressFill.fillAmount = 0f;
                fillStart = Time.unscaledTime + 0.15f;
            }
        }

        private void Update() {
            if (fillStart < 0f || !progressFill) return;
            float p = Mathf.Clamp01((Time.unscaledTime - fillStart) / FillDuration);
            progressFill.fillAmount = fillTarget * M2Ease.Out(p);
            if (p >= 1f) fillStart = -1f;
        }
    }
}
