using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Neuralizer.UI {

    /// <summary>Rol tipográfico — decide la fuente, no el tamaño (el tamaño sale de <see cref="EsportsTheme.TextSize"/>).</summary>
    public enum TypeRole { Display, Heading, Body, Mono }

    /// <summary>
    /// "Piel" del Módulo 2: acceso con caché a los sprites generados (<c>Resources/Sprites/Module2/UI/</c>)
    /// y a las fuentes por rol. Todo es opcional — si un recurso no existe todavía, quien llama cae de
    /// vuelta a colores planos, así nada se rompe antes de correr el generador.
    /// </summary>
    public static class EsportsSkin {

        private const string SpriteFolder = "Sprites/Module2/UI/";
        private const string FontSetPath = "Module2/EsportsFonts";

        private static readonly Dictionary<string, Sprite> Sprites = new();
        private static EsportsFontSet fontSet;
        private static bool fontSetLoaded;

        public static Sprite ChamferFill => Get("M2_ChamferFill");
        public static Sprite ChamferLine => Get("M2_ChamferLine");
        public static Sprite ChamferGlow => Get("M2_ChamferGlow");
        public static Sprite Bracket => Get("M2_Bracket");
        public static Sprite Stripes => Get("M2_Stripes");
        public static Sprite Grid => Get("M2_Grid");
        public static Sprite GlowDot => Get("M2_GlowDot");
        public static Sprite Vignette => Get("M2_Vignette");
        public static Sprite Chevron => Get("M2_Chevron");
        public static Sprite Back => Get("M2_Back");
        public static Sprite Check => Get("M2_Check");
        public static Sprite Play => Get("M2_Play");
        /// <summary>Blanco sólido (para barras "Filled", que necesitan sprite).</summary>
        public static Sprite Solid => Get("M2_Solid");

        /// <summary><b>true</b> si los sprites del generador ya existen en el proyecto.</summary>
        public static bool HasSprites => ChamferFill;

        public static Sprite Get(string name) {
            if (Sprites.TryGetValue(name, out Sprite cached) && cached) return cached;
            Sprite sprite = Resources.Load<Sprite>(SpriteFolder + name);
            Sprites[name] = sprite;
            return sprite;
        }

        public static EsportsFontSet Fonts {
            get {
                if (!fontSetLoaded || !fontSet) {
                    fontSet = Resources.Load<EsportsFontSet>(FontSetPath);
                    fontSetLoaded = true;
                }
                return fontSet;
            }
        }

        public static TMP_FontAsset FontFor(TypeRole role) {
            EsportsFontSet set = Fonts;
            if (!set) return null;
            return role switch {
                TypeRole.Display => set.display,
                TypeRole.Heading => set.heading,
                TypeRole.Mono => set.mono,
                _ => set.body
            };
        }

        /// <summary>
        /// Aplica la fuente del rol. Con pesos reales (SemiBold/ExtraBold) se quita el flag Bold para
        /// no sumarle negrita falsa encima.
        /// </summary>
        public static void ApplyFont(TMP_Text text, TypeRole role) {
            TMP_FontAsset font = FontFor(role);
            if (!font) return;
            text.font = font;
            if (role is TypeRole.Display or TypeRole.Heading) {
                text.fontStyle &= ~FontStyles.Bold;
            }
            if (role == TypeRole.Display) {
                text.fontStyle |= FontStyles.UpperCase;
                text.characterSpacing = 2f;
            }
        }

        /// <summary>Rol por defecto según tamaño y estilo pedidos (lo usa el kit cuando quien llama no especifica).</summary>
        public static TypeRole InferRole(float size, FontStyles style) {
            if (size >= EsportsTheme.TextSize.Title && (style & FontStyles.Bold) != 0) return TypeRole.Display;
            return (style & FontStyles.Bold) != 0 ? TypeRole.Heading : TypeRole.Body;
        }
    }
}
