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

        // Fondos oscuros — "azul noche" (ver MODULO2_DISENO.md §2)
        public static readonly Color BgDeep = new(0.039f, 0.055f, 0.102f);       // #0A0E1A fondo general
        public static readonly Color BgPanel = new(0.071f, 0.094f, 0.161f);      // #121829 relleno de tarjetas
        public static readonly Color BgPanelLight = new(0.110f, 0.141f, 0.251f); // #1C2440 controles, pestañas inactivas

        // Acentos neón — cian = interacción, violeta = identidad del módulo
        public static readonly Color NeonBlue = new(0.18f, 0.77f, 1f);           // #2EC5FF
        public static readonly Color NeonPurple = new(0.61f, 0.36f, 1f);         // #9B5CFF
        public static readonly Color NeonBlend = Color.Lerp(NeonBlue, NeonPurple, 0.5f);

        /// <summary>Ámbar: SOLO para "lo más importante ahora" (CTA principal, rango #1). Usarlo poco.</summary>
        public static readonly Color Accent = new(1f, 0.77f, 0.24f);             // #FFC53D

        // Texto — contraste ≥ 7:1 (primario) y ≥ 4.5:1 (secundario) sobre BgPanel
        public static readonly Color TextPrimary = new(0.93f, 0.95f, 1f);        // #EEF2FF
        public static readonly Color TextSecondary = new(0.60f, 0.65f, 0.78f);   // #9AA6C8

        /// <summary>Línea fina neutra (separadores, contornos de controles inactivos).</summary>
        public static readonly Color Stroke = new(1f, 1f, 1f, 0.10f);

        // Estados — siempre acompañados de texto/ícono, nunca solo color
        public static readonly Color NeonGreen = new(0.24f, 0.96f, 0.63f);       // #3DF5A0
        public static readonly Color NeonRed = new(1f, 0.30f, 0.43f);            // #FF4D6D

        /// <summary>Devuelve <paramref name="c"/> con otro alpha (atajo para tintes de sprites blancos).</summary>
        public static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);

        // Curvas de la gráfica de pertenencia — azul/rojo (no azul/morado) para que la zona de cruce,
        // al mezclar ambos rellenos semitransparentes, dé un morado/índigo — igual que en el mockup
        // del Módulo 2 (muestreado directo del PNG: relleno derecho ≈ RGB(176,69,89), no violeta).
        public static readonly Color CurveBajo = NeonBlue;
        public static readonly Color CurveAlto = new(0.85f, 0.30f, 0.38f);

        // Color del punto de intersección entre curvas y de su etiqueta flotante
        public static readonly Color CurveIntersection = new(1f, 0.92f, 0.4f);

        /// <summary>
        /// Escala tipográfica del Módulo 2, en unidades del Canvas (referencia 720×1280).
        /// </summary>
        /// <remarks>
        /// El build WebGL se muestra a 540×960, así que 1 unidad de Canvas = 0.75 px reales. Cada token
        /// está calculado para caer en la escala 12/14/16/18/24/32 px de las guías de legibilidad: el
        /// texto de lectura (<see cref="TextSize.Body"/>) nunca baja de 16 px y nada baja de 12 px
        /// (<see cref="TextSize.Caption"/>). Usar SIEMPRE estos tokens en vez de números sueltos.
        /// </remarks>
        public static class TextSize {
            /// <summary>12 px — solo metadatos terciarios (rangos de eje, notas). Mínimo absoluto.</summary>
            public const float Caption = 16f;
            /// <summary>14 px — etiquetas secundarias, valores de sliders, leyendas.</summary>
            public const float Small = 19f;
            /// <summary>16 px — texto de lectura e instrucciones.</summary>
            public const float Body = 21f;
            /// <summary>18 px — títulos de tarjeta, etiquetas de botón, encabezados de sección.</summary>
            public const float Subtitle = 24f;
            /// <summary>24 px — título de pantalla, cifras destacadas.</summary>
            public const float Title = 32f;
            /// <summary>32 px — titulares de resultado.</summary>
            public const float Display = 42f;
        }

        /// <summary>Medidas de interacción y espaciado (unidades de Canvas, referencia 720×1280).</summary>
        public static class Metrics {
            /// <summary>Alto mínimo de cualquier control tocable: 44 px reales (guía de área táctil).</summary>
            public const float TouchTarget = 60f;
            /// <summary>Alto de un slider completo (incluye el área de agarre).</summary>
            public const float SliderHeight = 60f;
            /// <summary>Espaciado base (grilla de 8 px reales ≈ 11 unidades).</summary>
            public const float Gap = 12f;
            /// <summary>Espaciado amplio entre bloques.</summary>
            public const float GapLarge = 20f;
        }
    }
}
