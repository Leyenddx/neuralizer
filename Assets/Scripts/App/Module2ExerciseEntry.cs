using Neuralizer.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neuralizer {

    /// <summary>
    /// Entrada de la lista de ejercicios del Módulo 2 en el menú principal: al presionar, carga la
    /// escena del ejercicio. Hoy cada ejercicio del Módulo 2 vive en su propia escena autocontenida
    /// (igual que <c>FuzzyDemo.unity</c>) en vez de cargarse dentro de <c>MainScene</c> como hace el
    /// Módulo 1 — es la integración mínima que no obliga a rehacer <see cref="Fuzzy.FuzzyHealerDemo"/> como
    /// un panel embebido. Cableado desde el editor, ver <c>Editor/Module2MenuBuilder.cs</c>.
    /// </summary>
    /// <remarks>
    /// Si <see cref="fuzzyLevel"/> está asignado, la entrada es un nivel de datos (<see cref="FuzzyScene"/>)
    /// que se juega en la escena genérica <c>FuzzyLevel.unity</c>: antes de cargarla deja el nivel
    /// elegido en <see cref="FuzzyLevelSelection.Pending"/> para que <see cref="Fuzzy.FuzzyLevelController"/>
    /// lo recoja al arrancar.
    /// </remarks>
    public class Module2ExerciseEntry : MonoBehaviour {

        [SerializeField] private string sceneName = "FuzzyDemo";

        [Tooltip("Opcional: nivel de datos a jugar en la escena genérica de niveles (FuzzyLevel).")]
        [SerializeField] private FuzzyScene fuzzyLevel;

        /// <summary>Nivel de datos de esta entrada, o null si es un ejercicio de escena propia (demo).</summary>
        public FuzzyScene Level => fuzzyLevel;

        public void Load() {
            if (fuzzyLevel) FuzzyLevelSelection.Pending = fuzzyLevel;
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Puente estático entre el menú principal y la escena genérica de niveles difusos: el menú
    /// guarda aquí qué <see cref="FuzzyScene"/> abrir y <see cref="Fuzzy.FuzzyLevelController"/> lo consume
    /// al iniciar. Mismo patrón que <see cref="MainMenuReturnState"/>.
    /// </summary>
    public static class FuzzyLevelSelection {
        public static FuzzyScene Pending;
    }
}
