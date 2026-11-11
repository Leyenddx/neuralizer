namespace Neuralizer {

    /// <summary>
    /// Bandera estática para saltar la animación de "PRESS START TO LEARN" y entrar directo al menú de
    /// ejercicios (con el módulo correcto ya seleccionado) cuando se vuelve desde un ejercicio del
    /// Módulo 2 — en vez de recargar MainScene desde cero como si fuera el primer arranque de la app.
    /// </summary>
    /// <remarks>
    /// Sobrevive a <c>SceneManager.LoadScene</c> (los campos estáticos no se reinician al cargar una
    /// escena nueva, solo con un domain reload / reinicio del proceso), que es justo lo que se necesita
    /// acá: <see cref="Fuzzy.FuzzyHealerDemo"/> la marca antes de volver a MainScene, y
    /// <see cref="MainMenuReturnHandler"/> la lee (y apaga) al llegar.
    /// </remarks>
    public static class MainMenuReturnState {
        public static bool SkipIntro;
        public static int ReturnToModule = 1;
    }
}
