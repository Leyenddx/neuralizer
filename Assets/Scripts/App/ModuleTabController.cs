using UnityEngine;
using UnityEngine.UI;

namespace Neuralizer {

    /// <summary>
    /// Controla qué panel de contenido del menú principal se muestra: la lista de ejercicios del
    /// Módulo 1 (redes neuronales) o la del Módulo 2 (lógica difusa). También alterna el "tema" visual
    /// compartido del menú (fondo y logo) y marca cuál pestaña "1"/"2" está activa. Cableado desde el
    /// editor a los botones del selector de módulo, ver <c>Editor/Module2MenuBuilder.cs</c>.
    /// </summary>
    /// <remarks>
    /// El Módulo 1 nunca se retoca: mientras esté seleccionado, todo queda exactamente como ya estaba
    /// (<see cref="module2Background"/> apagado, <see cref="module1Logo"/> encendido). El look nuevo
    /// (degradado neón + logo del Módulo 2) solo aparece mientras el Módulo 2 está activo, y desaparece
    /// por completo al volver al Módulo 1 — es un overlay adicional, no un reemplazo de nada existente.
    /// </remarks>
    public class ModuleTabController : MonoBehaviour {

        [SerializeField] private GameObject module1Panel;
        [SerializeField] private GameObject module2Panel;

        [Header("Tema del Módulo 2 (el Módulo 1 nunca cambia)")]
        [SerializeField] private GameObject module2Background;
        [SerializeField] private GameObject module1Logo;
        [SerializeField] private GameObject module2Logo;

        [Header("Resaltado de la pestaña activa")]
        [SerializeField] private Image tab1Background;
        [SerializeField] private Image tab2Background;

        [Header("Barra inferior (Puntuaciones/Info) — tema del Módulo 2")]
        [Tooltip("Fondos (pill) de los botones de la barra inferior.")]
        [SerializeField] private Graphic[] appBarSurfaces;
        [Tooltip("Iconos dentro de esos botones.")]
        [SerializeField] private Graphic[] appBarIcons;

        // Colores originales del Módulo 1, capturados en runtime (no a mano en el editor) para que,
        // si algún día cambian en la escena, esto se siga adaptando solo sin tener que re-cablear nada.
        private Color[] _appBarSurfacesOriginal;
        private Color[] _appBarIconsOriginal;

        private void Awake() {
            _appBarSurfacesOriginal = CaptureColors(appBarSurfaces);
            _appBarIconsOriginal = CaptureColors(appBarIcons);
        }

        /// <summary>
        /// El GameObject de este componente empieza inactivo (oculto hasta presionar "Start", ver
        /// Module2MenuBuilder o MainMenuReturnHandler), así que Start() corre recién en ese primer
        /// frame activo — es el punto natural para dejar todo en el estado inicial correcto.
        /// </summary>
        /// <remarks>
        /// Si se volvió de un ejercicio del Módulo 2 (<see cref="MainMenuReturnState.ReturnToModule"/>
        /// == 2, puesta por <see cref="MainMenuReturnHandler"/> antes de activar este GameObject),
        /// arranca en Módulo 2 en vez del default. Ojo con el orden: MainMenuReturnHandler.Start()
        /// activa este GameObject y Unity difiere el Start() de acá al final del frame — llamar
        /// ShowModule2() "a mano" desde MainMenuReturnHandler no sirve porque este Start() correría
        /// después y lo pisaría con ShowModule1(); por eso la decisión vive acá, leyendo la bandera
        /// directamente, y no como una llamada externa.
        /// </remarks>
        private void Start() {
            if (MainMenuReturnState.ReturnToModule == 2) {
                MainMenuReturnState.ReturnToModule = 1; // de un solo uso: no debe heredarlo un reload posterior
                ShowModule2();
            }
            else {
                ShowModule1();
            }
        }

        public void ShowModule1() {
            module1Panel.SetActive(true);
            module2Panel.SetActive(false);
            if (module2Background) module2Background.SetActive(false);
            if (module1Logo) module1Logo.SetActive(true);
            if (module2Logo) module2Logo.SetActive(false);
            SetTabSelected(tab1Background, tab2Background);
            ApplyColors(appBarSurfaces, _appBarSurfacesOriginal);
            ApplyColors(appBarIcons, _appBarIconsOriginal);
        }

        public void ShowModule2() {
            module1Panel.SetActive(false);
            module2Panel.SetActive(true);
            if (module2Background) module2Background.SetActive(true);
            if (module1Logo) module1Logo.SetActive(false);
            if (module2Logo) module2Logo.SetActive(true);
            SetTabSelected(tab2Background, tab1Background);
            ApplyColor(appBarSurfaces, UI.EsportsTheme.BgPanelLight);
            ApplyColor(appBarIcons, UI.EsportsTheme.TextPrimary);
        }

        private static void SetTabSelected(Image selected, Image other) {
            if (selected) selected.color = UI.EsportsTheme.NeonBlue;
            if (other) other.color = UI.EsportsTheme.BgPanelLight;
        }

        private static Color[] CaptureColors(Graphic[] graphics) {
            if (graphics == null) return new Color[0];
            Color[] colors = new Color[graphics.Length];
            for (int i = 0; i < graphics.Length; i++) {
                if (graphics[i]) colors[i] = graphics[i].color;
            }
            return colors;
        }

        private static void ApplyColors(Graphic[] graphics, Color[] colors) {
            if (graphics == null || colors == null) return;
            for (int i = 0; i < graphics.Length && i < colors.Length; i++) {
                if (graphics[i]) graphics[i].color = colors[i];
            }
        }

        private static void ApplyColor(Graphic[] graphics, Color color) {
            if (graphics == null) return;
            foreach (Graphic g in graphics) {
                if (g) g.color = color;
            }
        }
    }
}
