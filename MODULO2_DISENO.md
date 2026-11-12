# Módulo 2 — Sistema visual "Tactical Neon"

Documento de diseño del rediseño del Módulo 2 (menú + ejercicios). Resume la investigación, las
decisiones y dónde vive cada pieza en el código, para que cualquier pantalla nueva se vea igual.

## 1. Investigación y dirección

**Referencias de género (UI de shooters/esports competitivos):**

| Referencia | Qué tomamos | Qué evitamos |
|---|---|---|
| HUDs tácticos (Valorant, Apex) | Esquinas recortadas (chamfer), líneas finas, mayúsculas con tracking, jerarquía muy clara | Ruido decorativo que compite con la información |
| Overlays de transmisión esports | Etiquetas mono para cifras, "chips" de estado, acentos de color por equipo/caso | Animaciones largas en elementos que se usan seguido |
| Menús de selección de partida | Tarjetas grandes con índice (01, 02…), estado de progreso, flecha de acción | Texto pequeño sobre fondos con mucho detalle |

**Restricciones del producto:** app educativa (la lectura manda sobre la decoración), build WebGL a
540×960 en vertical, uGUI sin librerías de animación, usuarios que repiten ejercicios.

**Dirección elegida — "Tactical Neon":** fondos azul noche profundo, paneles con esquinas recortadas,
un solo acento frío (cian) para interacción, violeta para identidad del módulo y ámbar solo para lo
"más importante ahora" (rango #1, botón principal). Nada de degradados en texto de lectura.

## 2. Tokens

Todos en `Assets/Scripts/App/UI/EsportsTheme.cs`. **No usar colores ni tamaños sueltos.**

| Token | Uso |
|---|---|
| `BgDeep` / `BgPanel` / `BgPanelLight` | Fondo de pantalla / tarjetas / controles |
| `NeonBlue` (cian) | Interacción: foco, pestaña activa, sliders |
| `NeonPurple` | Identidad del módulo (fondo, bordes de tarjeta) |
| `Accent` (ámbar) | Lo principal: CTA, rango #1, nivel recomendado |
| `NeonGreen` / `NeonRed` | Correcto / incorrecto — siempre acompañados de texto, nunca solo color |
| `TextPrimary` / `TextSecondary` | Contraste ≥ 7:1 y ≥ 4.5:1 sobre `BgPanel` |

**Tipografía** (`EsportsTheme.TextSize`, unidades de Canvas 720×1280; el build se ve a 540×960):

| Token | px reales | Fuente | Uso |
|---|---|---|---|
| `Display` | 32 | Lexend ExtraBold, mayúsculas | Titular de resultado |
| `Title` | 24 | Lexend ExtraBold, mayúsculas | Título de pantalla |
| `Subtitle` | 18 | Lexend SemiBold | Títulos de tarjeta, botones |
| `Body` | 16 | Lexend Regular | Lectura e instrucciones (mínimo) |
| `Small` | 14 | Lexend / Noto Sans Mono | Etiquetas, valores |
| `Caption` | 12 | Noto Sans Mono, tracking | Overlines, metadatos (mínimo absoluto) |

Lexend se eligió porque está diseñada para reducir el esfuerzo de lectura (ideal en una app
educativa) y en sus pesos altos y en mayúsculas da el tono "gamer" sin sacrificar legibilidad. Las
cifras usan Noto Sans Mono para que no "bailen" al cambiar.

El menú vive en `MainScene`, cuyo Canvas es 540×960 con match de ancho: ahí 1 unidad = 1 px.

## 3. Recursos generados

`Tools → Neuralizer → Generar Recursos Visuales (Módulo 2)` (`Editor/Module2AssetGenerator.cs`)
crea por código, en `Assets/Resources/Sprites/Module2/UI/`:

| Sprite | Qué es |
|---|---|
| `M2_ChamferFill` / `M2_ChamferLine` | Panel con esquinas recortadas (relleno / contorno), 9-slice |
| `M2_ChamferGlow` | Halo suave con la misma forma, para estados activos |
| `M2_Bracket` | Corchete de esquina decorativo |
| `M2_Stripes` | Franjas diagonales (tile) para barras de progreso |
| `M2_Grid` | Retícula sutil (tile) del fondo |
| `M2_GlowDot` / `M2_Vignette` | Luz radial y viñeta del fondo |
| `M2_Chevron`, `M2_Back`, `M2_Check`, `M2_Play` | Iconos vectoriales simples (sin emoji) |

Además crea `Assets/Resources/Module2/EsportsFonts.asset`, que le dice al kit qué fuente usar en
cada rol. Todo se genera en blanco y se tiñe con `Image.color`, así un cambio de paleta no exige
regenerar nada.

## 4. Movimiento

Basado en los principios de Emil Kowalski (`emil-design-eng` / `animate`). Código en
`Assets/Scripts/App/UI/Motion/`.

| Qué | Frecuencia → decisión | Receta |
|---|---|---|
| Presionar botón/tarjeta | Muy frecuente → mínimo | escala 0.97, 120 ms ease-out; suelta en 160 ms |
| Entrada de la lista del menú | Ocasional | opacidad + escala 0.96→1, 260 ms ease-out, escalonado 45 ms |
| Popup de resultado | Ocasional | fondo + tarjeta escala 0.96→1, 240 ms entrada / 150 ms salida |
| Indicador de pestaña | Frecuente | desliza 220 ms ease-in-out |
| Acierto en un nivel | Raro → se permite "delight" | destello del título + halo que se expande 500 ms |
| Fondo ambiental | Decorativo constante | luces que derivan muy lento (sin afectar lectura) |

Curvas: `ease-out = cubic-bezier(0.23, 1, 0.32, 1)`, `ease-in-out = cubic-bezier(0.77, 0, 0.175, 1)`.
Nunca `ease-in`, nunca desde escala 0, nada que bloquee la interacción mientras se anima.
`M2Motion.ReducedMotion = true` quita escalas y desplazamientos y deja solo fundidos.

## 5. Cómo regenerar todo

**Un solo paso:** `Tools → Neuralizer → Regenerar Módulo 2 completo + capturas`
(`Editor/Module2BatchPipeline.cs`). Genera recursos, niveles, las 4 escenas y el menú de
`MainScene`, y deja capturas PNG a 540×960 en `Temp/Module2Captures/`. También corre sin abrir el
editor (con Unity cerrado):

```
Unity.exe -batchmode -quit -projectPath <ruta> -executeMethod Neuralizer.EditorTools.Module2BatchPipeline.Run
```

Ojo: reconstruye las escenas de ejercicios y el menú del Módulo 2 desde cero, así que se pierden los
retoques manuales hechos en esos objetos.

**A mano, paso por paso:**
1. `Tools → Neuralizer → Generar Recursos Visuales (Módulo 2)`
2. Abrir `MainScene` → `Neuralizer → Construir menú del Módulo 2` → Ctrl+S
3. `Tools → Neuralizer →` cada "Construir/Reconstruir …" de los ejercicios y `Construir Escena de Niveles (Módulo 2)`

## 6. Trampas conocidas de uGUI en este proyecto

- **Reference Pixels Per Unit:** el Canvas de `MainScene` usa 2 (estilo pixel art del Módulo 1); las
  escenas del Módulo 2 usan 100. Los sprites 9-slice escalan su borde con ese valor, así que el
  builder del menú compensa `pixelsPerUnitMultiplier` (si no, las esquinas recortadas desaparecen).
- **HorizontalLayoutGroup + LayoutElement en el mismo objeto:** el grupo reporta `flexibleHeight = 1`
  y, a igual prioridad, le gana al `LayoutElement`. Usar `flexibleHeight = 0` + `layoutPriority = 2`.
- **Handle de Slider:** el Slider estira el handle a toda la altura; `sizeDelta.y` se SUMA a esa altura.
- **Image "Filled" sin sprite** se dibuja completa aunque `fillAmount` sea 0 (usar `EsportsSkin.Solid`).
- **Listeners creados desde el editor:** `onClick.AddListener(lambda)` no se guarda en la escena; usar
  `UnityEventTools.Add…PersistentListener` o conectarlos en runtime.
