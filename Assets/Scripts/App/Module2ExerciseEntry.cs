using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neuralizer {

    /// <summary>
    /// Entrada de la lista de ejercicios del Módulo 2 en el menú principal: al presionar, carga la
    /// escena del ejercicio. Hoy cada ejercicio del Módulo 2 vive en su propia escena autocontenida
    /// (igual que <c>FuzzyDemo.unity</c>) en vez de cargarse dentro de <c>MainScene</c> como hace el
    /// Módulo 1 — es la integración mínima que no obliga a rehacer <see cref="FuzzyHealerDemo"/> como
    /// un panel embebido. Cableado desde el editor, ver <c>Editor/Module2MenuBuilder.cs</c>.
    /// </summary>
    public class Module2ExerciseEntry : MonoBehaviour {

        [SerializeField] private string sceneName = "FuzzyDemo";

        public void Load() {
            SceneManager.LoadScene(sceneName);
        }
    }
}
