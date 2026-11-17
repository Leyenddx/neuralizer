using TMPro;
using UnityEngine;

namespace Neuralizer.UI {

    /// <summary>
    /// Qué fuente usa cada rol tipográfico del Módulo 2 (ver <see cref="TypeRole"/>). El asset vive en
    /// <c>Assets/Resources/Module2/EsportsFonts.asset</c> y lo crea
    /// <c>Tools → Neuralizer → Módulo 2 → 1. Recursos visuales</c>; <see cref="EsportsSkin"/> lo carga.
    /// </summary>
    [CreateAssetMenu(fileName = "EsportsFonts", menuName = "Neuralizer/Esports Font Set")]
    public class EsportsFontSet : ScriptableObject {
        [Tooltip("Titulares y títulos de pantalla (Lexend ExtraBold, en mayúsculas)")]
        public TMP_FontAsset display;
        [Tooltip("Títulos de tarjeta, botones, etiquetas fuertes (Lexend SemiBold)")]
        public TMP_FontAsset heading;
        [Tooltip("Texto de lectura (Lexend Regular)")]
        public TMP_FontAsset body;
        [Tooltip("Cifras, overlines, metadatos (Noto Sans Mono)")]
        public TMP_FontAsset mono;
    }
}
