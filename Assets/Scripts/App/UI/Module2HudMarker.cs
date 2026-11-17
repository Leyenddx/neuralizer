using UnityEngine;

namespace Neuralizer.UI {

    /// <summary>
    /// Componente marcador, sin lógica propia: identifica la raíz del HUD del ejercicio del Módulo 2
    /// generada por la herramienta de editor <c>Tools → Neuralizer → Módulo 2 →
    /// 3. Escena Ejercicio 1 (Healer)</c> (ver <c>Assets/Scripts/Editor/Module2HudBuilder.cs</c>), para que esa misma
    /// herramienta pueda encontrar y borrar una corrida anterior antes de reconstruir — sin este
    /// marcador no habría forma segura de distinguir "el Canvas que generó el builder" de cualquier
    /// otro Canvas que alguien haya agregado a mano a la escena.
    /// </summary>
    public class Module2HudMarker : MonoBehaviour { }
}
