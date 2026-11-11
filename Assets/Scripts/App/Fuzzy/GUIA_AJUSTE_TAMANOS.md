# Guía: ajustar el HUD del ejercicio (Módulo 2)

**Esta guía reemplaza a la versión anterior.** Hasta hace poco, toda la pantalla se armaba por código
en tiempo de ejecución y había que editar números en `FuzzyHealerDemo.cs` para ajustar tamaños. Eso ya
no es así: ahora la pantalla es una jerarquía de objetos normal de Unity, guardada en la escena, y se
edita **visualmente desde el Editor** — nada de tocar código para mover, agrandar o cambiar el color de
algo.

## Cómo ver y editar la pantalla

1. Abrí la escena `Assets/Scenes/FuzzyDemo.unity`.
2. En la ventana **Hierarchy** vas a ver un `Canvas` con toda la pantalla ya armada adentro —
   **se ve así en el Game view sin necesidad de darle Play.**
3. Hacé clic en cualquier objeto (`TopBar`, `BottomSheet`, `GraphArea`, un botón, un texto, etc.) y
   ajustá lo que haga falta en el **Inspector**: posición, tamaño (`Width`/`Height` del
   `RectTransform`), color de una `Image`, texto de un `TextMeshPro`, espaciado de un
   `Horizontal/Vertical Layout Group`, etc. — todo con los controles normales de Unity.
4. También podés arrastrar los "handles" azules directo en la **Scene view** para redimensionar o
   mover algo a ojo.
5. Los cambios se guardan con la escena (Ctrl+S / Cmd+S), como cualquier otro cambio de escena.

No hace falta darle Play para ver el resultado — el Game view ya muestra la pantalla tal cual está
guardada. Play solo hace falta para probar el *comportamiento* (slider, dropdown, botón Evaluar,
pantalla completa, etc.), no para ver el layout.

## Mapa rápido de la jerarquía

```
Canvas
├─ Background              → fondo de pantalla
├─ TopBar                  → botón Volver, banner "Ejercicio 1", botón pantalla completa
├─ GraphArea
│  └─ PlotArea              → ejes, marcas, curvas (Bajo/Alto)
├─ BottomSheet
│  └─ Content
│     ├─ Dropdown           → selector "Función"
│     ├─ FormulaBox         → caja oscura con la fórmula
│     ├─ WeightRow          → "0"/"1", slider, íconos R/D, valor
│     └─ Button              → "Evaluar"
└─ ResultBackdrop           → popup de resultado (oculto hasta presionar Evaluar)
```

Cada nombre de objeto en el Inspector corresponde 1 a 1 con esta lista — si buscás "el botón Evaluar",
es literalmente el objeto llamado `Button` dentro de `BottomSheet/Content`.

## Si algo se rompe o querés volver a la base del mockup

`Tools → Neuralizer → Reconstruir HUD del Ejercicio (Módulo 2)` borra el `Canvas` actual y lo vuelve a
armar desde cero con las medidas tomadas directo del mockup, y reconecta automáticamente las
referencias que usa `FuzzyHealerDemo.cs` (no hace falta arrastrar nada a mano después). **Ojo:** esto
descarta cualquier ajuste manual que hayas hecho en el Inspector sobre esos objetos — es un "reset",
no un merge.

Después de correrlo, revisá en el Inspector del objeto `HealerDemoController` (el que tiene el script
`FuzzyHealerDemo`) que todos los campos bajo cada sección (Barra superior, Gráfica, Tarjeta inferior,
Resultado) tengan algo asignado y no digan "None" — si falta alguno, la consola de Unity lo va a
marcar con un error claro apenas le des Play.

## Qué NO tocar sin avisar primero

- El componente `FuzzyHealerDemo` (en el objeto `HealerDemoController`) tiene una sección de
  referencias en el Inspector — son enlaces a los objetos de la jerarquía, no valores para "ajustar".
  Si arrastrás algo distinto ahí (o lo dejás vacío), el comportamiento en Play se rompe. Para layout,
  tocá los objetos de adentro del `Canvas`, no estos campos.
- `Assets/Scripts/App/UI/EsportsUIKit.cs` y `EsportsTheme.cs` son la base compartida que usa el
  builder al reconstruir — tocar esos archivos no afecta nada de lo que ya está en la escena, solo
  cómo se arma una pantalla **nueva** la próxima vez que se corra el builder.
- `Assets/Scripts/Editor/Module2HudBuilder.cs` es la herramienta que arma todo esto — tiene las
  medidas "de fábrica" tomadas del mockup. Solo hace falta tocarlo si querés cambiar esas medidas
  base (lo que se ve la PRIMERA vez que se construye o reconstruye el HUD), no para ajustes del día a
  día.
