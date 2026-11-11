using UnityEngine;

namespace Neuralizer.UI {

    /// <summary>
    /// Paleta y tokens visuales compartidos para módulos con temática e-sports (Módulo 2 en
    /// adelante). El Módulo 1 (Minecraft) mantiene su propio estilo y no depende de esta clase.
    /// </summary>
    /// <remarks>
    /// Todo el color del "look gamer" vive aquí a propósito: para retocar la paleta más adelante
    /// (con arte propio, sprites de borde, etc.) basta con cambiar estos valores — no hay colores
    /// sueltos escondidos en cada pantalla. <see cref="EsportsUIKit"/> es la capa de construcción de
    /// UI que consume esta paleta; es la base pensada para que se pueda seguir retocando después.
    /// </remarks>
    public static class EsportsTheme {

        // Fondos oscuros
        public static readonly Color BgDeep = new(0.043f, 0.055f, 0.098f);       // fondo general, casi negro azulado
        public static readonly Color BgPanel = new(0.078f, 0.094f, 0.157f);      // relleno de tarjetas
        public static readonly Color BgPanelLight = new(0.114f, 0.137f, 0.212f); // superficies un poco más claras (sliders, etc.)

        // Acentos neón — el degradado azul→morado es la firma visual del módulo
        public static readonly Color NeonBlue = new(0.20f, 0.70f, 1f);
        public static readonly Color NeonPurple = new(0.70f, 0.35f, 1f);
        public static readonly Color NeonBlend = Color.Lerp(NeonBlue, NeonPurple, 0.5f);

        // Texto
        public static readonly Color TextPrimary = new(0.94f, 0.96f, 1f);
        public static readonly Color TextSecondary = new(0.62f, 0.68f, 0.82f);

        // Estados
        public static readonly Color NeonGreen = new(0.30f, 1f, 0.62f);
        public static readonly Color NeonRed = new(1f, 0.32f, 0.48f);

        // Curvas de la gráfica de pertenencia — azul/rojo (no azul/morado) para que la zona de cruce,
        // al mezclar ambos rellenos semitransparentes, dé un morado/índigo — igual que en el mockup
        // del Módulo 2 (muestreado directo del PNG: relleno derecho ≈ RGB(176,69,89), no violeta).
        public static readonly Color CurveBajo = NeonBlue;
        public static readonly Color CurveAlto = new(0.85f, 0.30f, 0.38f);

        // Color del punto de intersección entre curvas y de su etiqueta flotante
        public static readonly Color CurveIntersection = new(1f, 0.92f, 0.4f);
    }
}
