# Módulo 2 — Guía de uso: "El Dilema del Healer"

Esta guía explica, en términos simples, cómo funciona el Módulo 2 por dentro: qué se está
calculando, qué significa la gráfica, cómo se decide si el jugador "ganó" el ejercicio, y cómo
armar un ejercicio nuevo sin tocar el motor.

No hace falta saber programar para entender las primeras tres secciones. La última (cómo agregar
ejercicios) sí asume que alguien va a tocar el Editor de Unity.

---

## 1. ¿Qué enseña el módulo?

**Lógica difusa (fuzzy logic).** La idea central que el ejercicio busca transmitir es: en la vida
real (y en el diseño de IA para videojuegos) muy pocas cosas son estrictamente "sí" o "no". Un
personaje no tiene "poca vida" o "mucha vida" como dos casillas separadas — tiene *un poco* de
ambas al mismo tiempo, en distinta proporción, y esa proporción cambia el resultado.

El ejercicio ("El Dilema del Healer") pone al jugador en el lugar de una IA que decide a quién
curar primero entre dos personajes. La decisión "correcta" depende de combinar dos datos —cuánta
vida les queda y cuánto daño están recibiendo ahora mismo— de forma más parecida a como razonaría
una persona ("está bastante mal Y le están pegando duro, así que es urgente") que a un simple
`if (vida < 20)`.

---

## 2. Cómo funciona el sistema por dentro

El motor de lógica difusa vive en `Assets/Scripts/App/Fuzzy/` y sigue el proceso clásico de un
sistema Mamdani, en cuatro pasos:

### Paso 1 — Fuzzificación (`MembershipFunction.cs`, `FuzzyVariable.cs`)
Cada dato de entrada (por ejemplo "Daño Entrante", de 0 a 100) se traduce a "qué tan Bajo" o "qué
tan Alto" es, como un número entre 0 y 1 — no una categoría, un grado. Esto es literalmente lo que
dibujan las curvas de la gráfica (ver sección 3).

### Paso 2 — Reglas (`FuzzyRule.cs`)
Reglas del tipo "SI HP es Bajo Y DañoEntrante es Alto ENTONCES Prioridad es MuyAlta". Cada regla
tiene un **peso** (0 a 1) que indica qué tanto "cuenta" esa regla — el slider del ejercicio
justamente controla el peso de una regla específica. Con peso 0 la regla está apagada; con peso 1,
participa al máximo.

### Paso 3 — Agregación y Paso 4 — Defuzzificación (`FuzzySystem.cs`)
El sistema combina todas las reglas que aplican y calcula un único número final de salida (la
"Prioridad") usando el método del centroide — el promedio ponderado de la zona resultante. Ese
número final es el que se ve como "Prioridad: 75.0" en las tarjetas de cada héroe.

Todo este cálculo se repite cada vez que el jugador mueve el slider, a través de
`FuzzyHost.Evaluate()`.

---

## 3. Qué se ve en la gráfica

La gráfica grande muestra las **funciones de pertenencia** de la variable "DañoEntrante" (una de
las dos entradas del sistema, la otra es "HP" y no se grafica en este ejercicio por espacio):

- **Curva verde** = qué tan "Bajo" se considera un valor de daño en cada punto del eje horizontal.
- **Curva roja** = qué tan "Alto" se considera ese mismo valor.
- **Línea azul vertical** = dónde cae Reinhardt (su daño entrante actual: 20%).
- **Línea morada vertical** = dónde cae D.Va (su daño entrante actual: 85%).

Donde la línea azul cruza la curva verde en un punto alto, significa "Reinhardt está recibiendo
poco daño" (con alta certeza). Donde la línea morada cruza la curva roja en un punto alto, significa
"D.Va está recibiendo mucho daño". Esa lectura visual es exactamente el Paso 1 (fuzzificación) de
la sección anterior, hecho concreto.

Esta gráfica es **estática** (no depende del slider) — lo que sí cambia con el slider es el
resultado final de "Prioridad" de cada héroe, mostrado en las tarjetas de arriba.

---

## 4. Qué se está evaluando (cómo se sabe si el jugador "ganó")

1. El sistema calcula la Prioridad de cada caso (Reinhardt y D.Va) con los pesos actuales de las
   reglas.
2. Ordena los casos de mayor a menor prioridad — eso es el "Orden actual" que se ve en pantalla.
3. Al presionar **Evaluar**, `FuzzyHost.SubmitResult()` compara ese orden calculado contra el orden
   "correcto" que quedó definido al armar el ejercicio (`expectedRank` de cada `FuzzyCase`, ver
   sección 5). Si coincide exactamente, es correcto (panel verde); si no, incorrecto (panel rojo).

En este ejercicio en particular, el orden correcto es D.Va primero — así que el jugador tiene que
subir el peso de la regla de daño lo suficiente para que el sistema "entienda" que un aliado con
poca vida y mucho daño entrante es más urgente que uno con poca vida pero daño bajo.

---

## 5. Cómo agregar un ejercicio nuevo

Hay dos caminos, según qué tan permanente quieras que sea el ejercicio.

### Camino A — Producción real (recomendado para el módulo final)

Este es el camino pensado para configurar ejercicios sin tocar código, usando el inspector
personalizado ya construido (`Assets/Scripts/Editor/FuzzySceneEditor.cs`):

1. En el Project window, clic derecho → `Create > Neuralizer > Fuzzy Scene`. Esto crea un asset
   `FuzzyScene` (mismo tipo de dato que usa `FuzzyHealerDemo.cs` internamente, pero como archivo
   reutilizable en vez de código).
2. En el Inspector de ese asset vas a ver:
   - **Narrative Context**: el texto de la situación (lo que se lee arriba del todo).
   - **System → Inputs**: las variables de entrada (arma cada una con su rango y sus etiquetas
     Bajo/Alto — o las que necesites — cada una con su función de pertenencia).
   - **System → Outputs**: la variable de salida (normalmente "Prioridad" o el nombre que le quieras
     dar).
   - **System → Rules**: las reglas SI-ENTONCES, cada una con su peso inicial.
   - **Cases**: los casos que se comparan (los "aliados" u "opciones" del ejercicio), cada uno con
     sus valores de entrada y su `expectedRank` (0 = debería quedar primero, 1 = segundo, etc).
3. El inspector trae una vista previa en vivo de las curvas y un panel de prueba con sliders para
   verificar el comportamiento del sistema antes de conectarlo a una pantalla — sin necesitar Play
   mode.
4. Falta construir la UI para ese ejercicio específico (ver siguiente punto) y conectar el asset a
   través de un `FuzzyHost` en la escena real del juego (`MainScene`). Ese cableado final —
   `FuzzyLevelUIBuilder` y el menú — es justo el trabajo pendiente documentado en
   `FUZZY_MODULE.md`, para cuando decidamos avanzar a integración real.

### Camino B — Demo aislada por código (el camino usado para "El Dilema del Healer")

Es el patrón de `FuzzyHealerDemo.cs`: útil para prototipar rápido un ejercicio completo con su
propia interfaz, sin depender de que la UI de producción ya esté lista. Para clonarlo:

1. Copia `FuzzyHealerDemo.cs` con otro nombre.
2. En `BuildScene()`, cambia las `FuzzyVariable`, las `FuzzyRule` y los `FuzzyCase` por los de tu
   nuevo ejercicio (mismo formato que en el Camino A, solo que escrito en código en vez de en el
   Inspector).
3. En `BuildUI()`, ajusta los textos (narrativa, nombres, etiquetas de la gráfica) — la construcción
   visual en sí (tarjetas neón, gráfica, slider, botón) ya está resuelta por
   `EsportsUIKit`/`EsportsTheme`, así que normalmente no hay que tocar esa parte.
4. Crea una escena nueva (`Assets/Scenes/`), agrega un GameObject vacío con tu script, dale Play.

### Sobre el estilo visual

Toda la paleta de colores vive en un solo lugar: `Assets/Scripts/App/UI/EsportsTheme.cs`. Cambiar
un color ahí (por ejemplo el azul o el morado neón) actualiza automáticamente todas las pantallas
que usan `EsportsUIKit`. Las piezas de UI reutilizables (tarjetas con borde neón, botones y sliders
con degradado, texto con degradado) están en `Assets/Scripts/App/UI/EsportsUIKit.cs` — es la base
para que, cuando haya arte propio, sea cuestión de reemplazar ahí adentro en vez de rehacer cada
pantalla.

---

## Resumen rápido

| Pregunta | Respuesta corta |
|---|---|
| ¿Qué calcula el sistema? | Un puntaje de "Prioridad" por cada caso, combinando entradas difusas con reglas con peso. |
| ¿Qué dibuja la gráfica? | Las funciones de pertenencia (Bajo/Alto) de una variable de entrada, con marcadores de dónde cae cada caso. |
| ¿Cómo se califica? | Comparando el orden calculado contra el orden esperado (`expectedRank`) al presionar Evaluar. |
| ¿Cómo agrego un ejercicio para producción? | `Create > Neuralizer > Fuzzy Scene`, llenar variables/reglas/casos en el Inspector. |
| ¿Cómo agrego un ejercicio de prueba rápida? | Copiar `FuzzyHealerDemo.cs` y cambiar los datos en `BuildScene()`. |
| ¿Dónde cambio los colores/estilo? | `EsportsTheme.cs` (colores) y `EsportsUIKit.cs` (piezas de UI). |
