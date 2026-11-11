# Módulo 2 — Lógica Difusa: guía de integración

Este documento resume lo que se agregó al proyecto para el nuevo módulo de lógica difusa, qué falta hacer en el Editor de Unity para dejarlo jugable, y los datos listos para capturar los 4 niveles acordados (temática esports).

Todo lo de aquí se escribió y se verificó compilando dentro del Editor (Unity 6000.6.0f1) el 18/09/2026. El Módulo 1 (redes neuronales) no se tocó.

## 1. Qué se agregó

```
Assets/Scripts/App/Fuzzy/
  MembershipFunction.cs   — función de pertenencia trapezoidal/triangular
  FuzzyVariable.cs        — variable difusa (HP, Daño, Prioridad...) + sus etiquetas
  FuzzyRule.cs             — regla SI-ENTONCES (Y/O, peso)
  FuzzySystem.cs           — motor Mamdani: fuzzificación → reglas → inferencia → centroide

Assets/Scripts/App/Data/
  ScorableExercise.cs      — base compartida de puntuación/medallas (extraída de NeuralScene)
  FuzzyScene.cs             — ScriptableObject de un nivel (equivalente a NeuralScene)

Assets/Scripts/App/
  FuzzyHost.cs              — MonoBehaviour runtime: carga un nivel, evalúa, califica

Assets/Scripts/Editor/
  FuzzySceneEditor.cs       — inspector personalizado: gráfica de curvas + prueba en vivo
```

Probado en el Editor: se creó un `FuzzyScene` de prueba (menú `Assets > Create > Neuralizer > Fuzzy Scene`), se le agregó una variable "HP" con una etiqueta "Bajo" (0, 0, 25, 50) y la curva se dibujó correctamente en el inspector. El asset de prueba se borró después de confirmar que funcionaba.

## 2. Cómo se relaciona con el Módulo 1

`NeuralScene` **no fue modificado** — se dejó intacto a propósito. `ScorableExercise` es una clase base nueva con la misma lógica de puntuación que ya tenía `NeuralScene`, y `FuzzyScene` hereda de ella. `NeuralScene` es candidato a migrar a heredar de `ScorableExercise` en el futuro (eliminaría la duplicación), pero eso implica tocar un archivo del Módulo 1 en producción — mejor hacerlo con Play Mode a la mano para probarlo, no a ciegas.

`FuzzyHost` es el equivalente de `NetworkHost`, pero **no copia** el patrón de "un UnityEvent por cada neurona" que tenía el motor de redes neuronales (esa fue una de las cosas que identificamos como deuda técnica). En vez de eso:

- Los datos que varían de tamaño por nivel (variables, casos, ranking) se exponen como eventos de C# normales (`OnVariablesLoaded`, `OnCasesLoaded`, `OnRankingChanged`) que un componente de UI debe escuchar **por código**, no cableando un slot fijo por Inspector.
- Los datos simples (nombre, booleanos, sprite de medalla) sí son `UnityEvent` cableables desde el Inspector, igual que en `NetworkHost`.

Esto significa que la UI del módulo difuso necesita un componente nuevo (algo como `FuzzyLevelUIBuilder`) que, al recibir `OnVariablesLoaded`, instancie un prefab por variable (con sus sliders de membresía) — no hay una escena fija con 8 slots como en Redes Neuronales. Ese componente de UI **no se escribió todavía** porque depende de diseñar el prefab visual en el Editor (ver sección 4).

## 3. Datos de los 4 niveles (listos para capturar)

Crear cada uno desde `Assets > Create > Neuralizer > Fuzzy Scene` dentro de `Assets/ScriptableObjects/FuzzyScenes/` (crear esa carpeta). El inspector personalizado ya permite llenar todo esto directo, con gráfica y prueba en vivo incluidas.

### Nivel 1 — Economía (CS2/Valorant)
- **Display Name:** Economía de Equipo
- **Narrative Context:** "¿Qué tan preparado está tu equipo para esta ronda?"
- **Variable de entrada:** `Dinero` (rango 0–16000)
  - Etiqueta `Eco`: (0, 0, 2000, 5000)
  - Etiqueta `SemiBuy`: (2000, 5000, 7000, 10000)
  - Etiqueta `FullBuy`: (7000, 10000, 16000, 16000)
- Este nivel no necesita reglas/salida todavía — es el nivel introductorio de "grado de pertenencia" (Nivel 1 del documento de diseño). Alcanza con la variable de entrada; el ejercicio es que el jugador ajuste los puntos hasta que un set de valores de prueba (aportarlos como `FuzzyCase`, sin `expectedRank`, solo para mostrar el grado) se clasifique como se espera.

### Nivel 2 — Prioridad de gank (League of Legends)
- **Display Name:** Prioridad de Gank
- **Narrative Context:** "SI una línea está perdiendo, ENTONCES gankea ahí."
- **Variable de entrada:** `DesventajaDeOro` (rango -3000–3000)
  - Etiqueta `Perdiendo`: (-3000, -3000, -1500, -300)
  - Etiqueta `Parejo`: (-1500, -300, 300, 1500)
  - Etiqueta `Ganando`: (300, 1500, 3000, 3000)
- **Variable de salida:** `PrioridadGank` (rango 0–100)
  - Etiqueta `Baja`: (0, 0, 25, 50)
  - Etiqueta `Media`: (25, 50, 75)
  - Etiqueta `Alta`: (50, 75, 100, 100)
- **Reglas:**
  1. SI `DesventajaDeOro` es `Perdiendo` ENTONCES `PrioridadGank` es `Alta`
  2. SI `DesventajaDeOro` es `Parejo` ENTONCES `PrioridadGank` es `Media`
  3. SI `DesventajaDeOro` es `Ganando` ENTONCES `PrioridadGank` es `Baja`
- **Casos de prueba:** `Top` (-1800), `Mid` (0), `Bot` (1200) — el jugador ajusta hasta que la prioridad calculada ordene correctamente Top > Mid > Bot.

### Nivel 3 — El Dilema del Healer (Overwatch / Marvel Rivals)
- **Display Name:** El Dilema del Healer
- **Narrative Context:** "Dos aliados en crítico. ¿A quién curas primero?"
- **Variables de entrada:**
  - `HP` (rango 0–100): `Bajo` (0, 0, 30, 55), `Alto` (35, 70, 100, 100)
  - `DañoEntrante` (rango 0–100): `Bajo` (0, 0, 30, 50), `Alto` (30, 70, 100, 100)
- **Variable de salida:** `Prioridad` (rango 0–100)
  - `Baja`: (0, 0, 25, 50) · `Media`: (25, 50, 75) · `Alta`: (50, 75, 100) · `MuyAlta`: (75, 90, 100, 100)
- **Reglas:**
  1. SI `HP` es `Bajo` **Y** `DañoEntrante` es `Alto` ENTONCES `Prioridad` es `MuyAlta`
  2. SI `HP` es `Bajo` **Y** `DañoEntrante` es `Bajo` ENTONCES `Prioridad` es `Alta`
  3. SI `HP` es `Alto` ENTONCES `Prioridad` es `Baja`
- **Casos de prueba:**
  - `Aliado A`: HP = 18, DañoEntrante = 20 → `expectedRank = 1` (resiste más, se cura segundo)
  - `Aliado B`: HP = 16, DañoEntrante = 85 → `expectedRank = 0` (riesgo letal, se cura primero)

### Nivel 4 — Prioridad de Objetivo (Teamfight LoL/Valorant/CS)
- **Display Name:** Prioridad de Objetivo
- **Narrative Context:** "Focus fire: ¿a quién ataca primero el equipo?"
- **Variables de entrada:**
  - `VidaRestante` (0–100): `Baja` (0, 0, 30, 50), `Alta` (30, 70, 100, 100)
  - `Amenaza` (0–100, qué tanto daño hace ese objetivo): `Baja` (0, 0, 30, 50), `Alta` (30, 70, 100, 100)
- **Variable de salida:** `PrioridadObjetivo` (0–100), mismas 4 etiquetas que el Nivel 3
- **Reglas:**
  1. SI `VidaRestante` es `Baja` **Y** `Amenaza` es `Alta` ENTONCES `PrioridadObjetivo` es `MuyAlta`
  2. SI `VidaRestante` es `Baja` **Y** `Amenaza` es `Baja` ENTONCES `PrioridadObjetivo` es `Media`
  3. SI `VidaRestante` es `Alta` **Y** `Amenaza` es `Alta` ENTONCES `PrioridadObjetivo` es `Alta`
  4. SI `VidaRestante` es `Alta` **Y** `Amenaza` es `Baja` ENTONCES `PrioridadObjetivo` es `Baja`
- **Casos de prueba:** al menos 3 "enemigos" con combinaciones distintas de vida/amenaza, para que se note el ranking.
- **Vista 3D:** usar `FuzzySystem.EvaluateSurface(0, 1, 0, resolución, valoresFijos)` para generar la malla de la superficie de control (altura = prioridad, X = VidaRestante, Y = Amenaza). Es la misma idea que la vista 3D del Módulo 1, pero mostrando esta superficie en vez de una frontera de clasificación.

## 4. Lo que falta hacer en el Editor visual (no se puede hacer a ciegas)

Esto sí necesita el Editor de Unity abierto con manos en el teclado, porque implica decisiones visuales y arrastrar referencias:

1. **Diseñar el prefab de "variable"**: un panel UI con el nombre de la variable, un slider por etiqueta (para mover los puntos a/b/c/d) y un preview de la curva — pensar en reutilizar el estilo del panel de fórmula del Módulo 1 (`f(w1x1+w2x2+b)`) para mantener identidad visual.
2. **Escribir `FuzzyLevelUIBuilder`**: un `MonoBehaviour` que escuche `FuzzyHost.OnVariablesLoaded`/`OnCasesLoaded` y haga `Instantiate` del prefab de variable una vez por cada elemento del arreglo (esto es código, se puede escribir sin Editor — puedo hacerlo en la próxima sesión si quieres).
3. **Agregar un GameObject `FuzzyHost` a `MainScene`**, junto a `Network Host` en la jerarquía, y cablear sus `UnityEvent` simples (nombre, tiempo, score, medalla) a los mismos textos/UI que ya usa el Módulo 1 donde tenga sentido reutilizarlos.
4. **Agregar un botón/pestaña en el menú principal** para entrar al nuevo módulo (existe `Main Menu Canvas` con la lista de ejercicios — el mismo patrón que ya usan sirve, solo agregando las 4 entradas nuevas apuntando a `FuzzyScene` en vez de `NeuralScene`).
5. **Superficie 3D del Nivel 4**: generar una malla procedural a partir de `EvaluateSurface(...)`, con el mismo enfoque que ya usan para la vista 3D de clasificación (buscar `NeuralRenderPass` como referencia de cómo está resuelto el render custom actual).

## 5. Notas de optimización (WebGL)

- `FuzzySystem.Evaluate`/`EvaluateSingle` reutilizan buffers internos — no reservan memoria en cada llamada, así que se pueden llamar cada frame sin generar presión al recolector de basura (importante en WebGL, donde los GC spikes se sienten como freezes).
- `FuzzyHost.Evaluate()` usa insertion sort para el ranking (sin `Array.Sort` + comparador, que sí reserva memoria por el delegate).
- `EvaluateSurface` sí reserva memoria (una malla nueva) — está pensado para llamarse solo al cargar el nivel o al cambiar un dato, nunca dentro de `Update()`.
- La herramienta de autoría (`FuzzySceneEditor.cs`) vive en una carpeta `Editor/`, por lo que Unity la excluye automáticamente del build de WebGL — no agrega peso.
- Al armar los prefabs de UI (paso 1 de la sección 4), evitar `Instantiate`/`Destroy` por frame; igual que el Módulo 1 hace con `ObjectPool<SamplePoint>`, conviene reciclar los paneles de variable si el jugador cambia de nivel seguido.
