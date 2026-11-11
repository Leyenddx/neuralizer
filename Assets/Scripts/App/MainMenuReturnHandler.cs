using UnityEngine;

namespace Neuralizer {

    /// <summary>
    /// Vive en "Main Menu Canvas". Si <see cref="MainMenuReturnState.SkipIntro"/> está activa (ver esa
    /// clase), salta la animación inicial y deja el menú listo tal como quedaría después de presionar
    /// "Start" — con el módulo correcto ya seleccionado — en vez de mostrar de nuevo la pantalla de
    /// arranque como si fuera la primera vez que se abre la app.
    /// </summary>
    /// <remarks>
    /// No toca en nada el arranque normal del Módulo 1: si la bandera no está activa (que es el caso de
    /// cualquier primer arranque real), este componente no hace absolutamente nada.
    /// </remarks>
    public class MainMenuReturnHandler : MonoBehaviour {

        private void Start() {
            if (!MainMenuReturnState.SkipIntro) return;
            MainMenuReturnState.SkipIntro = false;

            Transform canvas = transform;
            Transform startButton = canvas.Find("Start Button");
            Transform safeArea = canvas.Find("Safe Area");
            Transform wrapper = safeArea ? safeArea.Find("Wrapper") : null;
            if (!wrapper) return;

            // Mismos SetActive que ya hace el propio botón "Start" (ver su onClick en el editor de
            // MainScene) — solo que disparados automáticamente en vez de esperar el clic del jugador.
            if (startButton) startButton.gameObject.SetActive(false);
            SetActiveIfFound(wrapper, "App Bar", true);
            SetActiveIfFound(wrapper, "Content Panel", true);
            SetActiveIfFound(wrapper, "Version Label", true);
            Transform moduleTabs = wrapper.Find("ModuleTabs");
            if (moduleTabs) moduleTabs.gameObject.SetActive(true);

            // BootManager es un GameObject aparte a nivel raíz de la escena (no un hijo de este canvas),
            // así que se busca por tipo en vez de por Transform.Find.
            BootManager boot = FindAnyObjectByType<BootManager>();
            if (boot) boot.StartFadeIn();

            // Qué módulo queda seleccionado (ShowModule1/ShowModule2) lo decide ModuleTabController en
            // su propio Start() leyendo MainMenuReturnState.ReturnToModule directamente — NO se llama acá
            // a mano: como recién activamos "moduleTabs" arriba, Unity difiere su Start() a después de
            // que termine este método, así que una llamada aquí se vería pisada por ese Start().
        }

        private static void SetActiveIfFound(Transform parent, string childName, bool active) {
            Transform child = parent.Find(childName);
            if (child) child.gameObject.SetActive(active);
        }
    }
}
