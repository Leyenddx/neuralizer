using UnityEngine;

namespace Neuralizer.UI {

    /// <summary>
    /// Componente marcador, sin lógica propia: identifica los objetos raíz de la escena genérica de
    /// niveles del Módulo 2 (<c>FuzzyLevel.unity</c>) generados por <c>Tools → Neuralizer → Construir
    /// Escena de Niveles (Módulo 2)</c> (ver <c>Assets/Scripts/Editor/Module2LevelSceneBuilder.cs</c>),
    /// para que esa herramienta pueda borrar una corrida anterior antes de reconstruir. Mismo patrón
    /// que <see cref="Module2DamageDemoMarker"/>.
    /// </summary>
    public class Module2LevelMarker : MonoBehaviour { }
}
