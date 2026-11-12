# Módulo 2 — Guía de defensa: ejercicios y niveles

Guía para explicar y defender el Módulo 2 (lógica difusa). Para cada ejercicio y nivel explica qué
enseña, qué ve y qué controla el jugador, cómo se resuelve paso a paso y qué significa cada parámetro.
Los números de esta guía no son aproximaciones: salen de simular el mismo motor del juego.

**Estructura del módulo**

| En el menú | Qué es | Se califica |
|---|---|---|
| Entrenamiento 01 — El Dilema del Healer | Práctica guiada con interfaz propia | Sí, con porcentaje de calidad |
| Entrenamiento 02 — Lectura del Daño | Práctica perceptiva (sin respuesta correcta) | No |
| Entrenamiento 03 — Triage Rápido | Práctica de decisión rápida por rondas | Aciertos/rondas |
| Niveles 01–04 | Ejercicios de datos (`FuzzyScene`) que se juegan en una escena común | Sí: correcto/incorrecto + calidad |

La progresión va de **percibir** el grado de pertenencia (Ej. 2 y 3), a **ajustar una regla** (Ej. 1),
a **diseñar curvas y reglas** completas (Niveles 1–4).

---

## 1. Conceptos y parámetros (glosario)

Todo el módulo corre sobre un motor **Mamdani** propio (`Assets/Scripts/App/Fuzzy/`), con cuatro pasos:
**fuzzificar → evaluar reglas → agregar → defuzzificar**.

### Variable difusa
Un dato del problema ("HP", "Dinero", "Amenaza"…). Tiene:
- **Rango**: los valores crudos que puede tomar (ej. HP de 0 a 100; Dinero de 0 a 16 000).
- **Etiquetas lingüísticas**: palabras con las que razonamos ("Bajo", "Alto", "Eco"…). Cada una tiene
  su función de pertenencia.

Las variables de **entrada** describen la situación; la de **salida** es la decisión ("Prioridad").

### Grado de pertenencia (μ)
Número entre **0 y 1** que dice *qué tanto* un valor pertenece a una etiqueta. Es la idea central del
módulo: HP = 40 no es "bajo" o "alto", es **Bajo en 0.6 y Alto en 0.14 a la vez**.

### Función de pertenencia (la curva)
Define μ para cada valor del rango.

| Forma | Parámetros | Significado |
|---|---|---|
| **Trapezoidal** | a, b, c, d | `a`: empieza a subir desde 0 · `b`: llega a 1 · `c`: empieza a bajar · `d`: vuelve a 0. Entre b y c la pertenencia es total (meseta). |
| **Triangular** | a, pico, d | Trapezoide con b = c: un solo punto de pertenencia total. |
| **Sigmoide** | centro, pendiente | Curva en "S". El centro es donde μ = 0.5; la pendiente, qué tan brusco es el cambio. La inversa es la "S" que baja. |
| **Gaussiana** | centro, ancho (σ) | Campana: máxima en el centro y cae suave a ambos lados. |

Cuando a = b (o c = d) el borde es vertical: así se hacen las etiquetas de los extremos ("hombros").
En el juego se mantiene siempre **a ≤ b ≤ c ≤ d** (un punto no puede pasar a su vecino).

### Regla
`SI <condiciones> ENTONCES <salida> ES <etiqueta>`, por ejemplo
*SI HP es Bajo **Y** DañoEntrante es Alto ENTONCES Prioridad es MuyAlta*.
- **Y** toma el **mínimo** de los grados (todas las condiciones deben cumplirse).
- **O** toma el **máximo** (basta con una).
- **Peso (0 a 1)**: multiplica la fuerza de la regla. Con 0 la regla está apagada; con 1 cuenta completa.
- **Fuerza de activación** = (grado combinado de las condiciones) × peso.

### Agregación y defuzzificación (cómo sale un solo número)
1. Cada regla activa "recorta" la curva de su etiqueta de salida a la altura de su fuerza.
2. Se superponen todos los recortes tomando el **máximo** punto a punto.
3. El resultado final es el **centroide** (el "centro de masa") de esa figura, calculado con 40 muestras
   del rango de salida.

Si **ninguna regla se activa**, la salida es el **punto medio del rango** (50 en 0–100). Ese detalle
explica varios estados iniciales "rotos" de los niveles.

### Cómo se califica
- **Modo Ordenar (Ranking)**: se calcula la salida de cada caso y se ordenan de mayor a menor. Es correcto
  si el orden coincide con el esperado **y no hay empates** (diferencia menor a 0.01 = empate: el sistema
  "no sabe decidir").
- **Modo Clasificar**: cada caso cae en la etiqueta donde tiene mayor μ. Es correcto si todos caen en su
  etiqueta esperada (también sin empates).
- **Calidad del resultado (%)**: puntaje continuo, porque en lógica difusa casi nada es blanco o negro.
  - Ordenar: para cada par de casos consecutivos en el orden esperado,
    `0.5 + (diferencia de salida / rango de salida)`, limitado a [0, 1]; luego el promedio.
    50 % = empate; más de 50 % = el orden va en la dirección correcta.
  - Clasificar: por caso, `0.5 + 0.5 × (μ de la etiqueta esperada − mejor μ de las otras)`; promedio.

### Parámetros de un nivel (`FuzzyScene`)
| Campo | Significado |
|---|---|
| Display Name / Narrative Context | Título y situación que se muestran arriba |
| Mode | Ordenar o Clasificar |
| Editable Rule Weights | Si el jugador puede mover el peso de cada regla |
| Editable Memberships | Si el jugador puede mover a/b/c/d de las curvas de entrada |
| Show Control Surface | Muestra la vista "Superficie" (salida en función de 2 entradas) |
| Cases | Los casos a comparar: valores de entrada + posición o etiqueta esperada |

---

## 2. Entrenamiento 01 — El Dilema del Healer

**Qué enseña:** que una regla difusa combina dos datos con "Y" y que su **peso** decide cuánto influye.

**Situación:** Reinhardt (HP 18, DañoEntrante 20) y D.Va (HP 16, DañoEntrante 85). Ambos tienen poca
vida, pero D.Va recibe mucho más daño. Lo correcto es curar **primero a D.Va**.

**Variables**
| Variable | Etiquetas (trapezoidal a, b, c, d) |
|---|---|
| HP (0–100) | Bajo (0, 0, 30, 55) · Alto (35, 70, 100, 100) |
| DañoEntrante (0–100) | Bajo (0, 0, 30, 50) · Alto (30, 70, 100, 100) |
| Prioridad (salida, 0–100) | Baja (0, 0, 25, 50) · Alta triangular (50, 75, 100) · MuyAlta (75, 90, 100, 100) |

**Reglas**
1. SI HP es Bajo Y DañoEntrante es Alto → Prioridad MuyAlta — **peso inicial 0 (apagada)**
2. SI HP es Bajo Y DañoEntrante es Bajo → Prioridad Alta — peso 1
3. SI HP es Alto → Prioridad Baja — peso 1

**Qué ve el jugador**
- La gráfica de **DañoEntrante**: curva Bajo y curva Alto, con líneas verticales en 20 (Reinhardt) y 85
  (D.Va). Donde la línea corta una curva se lee el grado μ.
- El selector **Función** cambia la forma de las curvas de DañoEntrante (Trapezoidal, Triangular,
  Sigmoide, Gaussiana), y la caja de abajo muestra su fórmula.
- El **slider** controla el peso de la regla 1 (0.0000 a 1.0000).
- Los aros **R** y **D** cambian de verde a rojo según quién es más urgente *relativamente*: la diferencia
  de prioridad entre ambos, saturada a 30 puntos.

**Cómo se resuelve**
- Al inicio (peso 0): Reinhardt activa la regla 2 → **75**; a D.Va no le aplica ninguna regla activa → **50**
  (punto medio). Orden: Reinhardt > D.Va → **incorrecto**, calidad 25 %.
- Al subir el peso de la regla 1, D.Va (HP Bajo = 1, Daño Alto = 1) activa "MuyAlta" → **≈ 87–90**.
  Orden: D.Va > Reinhardt → **correcto**.

| Forma | Peso 0 (R / D) | Peso 0.3 (R / D) | Peso 1 (R / D) | Mensaje con peso 1 |
|---|---|---|---|---|
| Trapezoidal | 75 / 50 | 75 / 87.9 | 75 / 90 | ¡Excelente! (+15) |
| Triangular | 75 / 50 | 75 / 87.6 | 75 / 89.2 | ¡Excelente! (+14.2) |
| Sigmoide | 75 / 74.4 | 75 / 86.5 | 75 / 89.4 | ¡Excelente! (+14.4) |
| Gaussiana | 75 / 74.9 | 75 / 78.4 | 75.2 / 84.6 | Correcto por poco margen (+9.4) |

Al pulsar **Evaluar** se ven las dos prioridades y el veredicto según el margen: 10 puntos o más es
"¡Excelente!", menos es "correcto por poco margen", igual es empate, y negativo pide subir el peso.

**Para explicarlo:** con curvas suaves (sigmoide, gaussiana) los dos aliados quedan muy parejos al
inicio (75 vs 74.4), porque las curvas nunca llegan exactamente a 0: cada caso pertenece "un poco" a
todo. La forma de la curva cambia la decisión, no solo el dibujo.

---

## 3. Entrenamiento 02 — Lectura del Daño

**Qué enseña:** el **grado de pertenencia** como algo que se *siente*, sin números. Es exactamente
el contorno rojo de un aliado dañado en un shooter.

**Parámetros**
- **Slider (0–100)**: porcentaje de vida perdida del personaje. No muestra número a propósito.
- **μ_Crítico(x) = trapezoidal(50, 70, 100, 100)**: hasta 50 % de daño no es nada crítico; de 50 a 70
  sube linealmente; desde 70 es crítico al 100 %.

| Daño | 0–50 | 55 | 60 | 65 | 70–100 |
|---|---|---|---|---|---|
| μ_Crítico | 0 | 0.25 | 0.50 | 0.75 | 1 |
| Contorno | verde | verde-amarillo | **amarillo** | naranja | **rojo** |

El contorno y el resplandor interpolan verde → amarillo (μ = 0.5) → rojo, y el pulso se acelera
(de 0.6 a 6 ciclos) mientras más crítico.

**Cómo se usa:** no hay respuesta correcta. El jugador desliza, observa y después pulsa **Revelar la
curva real** para ver la gráfica, la fórmula y el porcentaje exacto. Ese es el "momento ajá": lo que
sentía como "se está poniendo rojo" era una función matemática.

---

## 4. Entrenamiento 03 — Triage Rápido

**Qué enseña:** a **decidir comparando grados** bajo presión, como en una partida real.

**Cómo funciona cada ronda**
1. Se sortean dos daños entre 0 y 100 (Reinhardt y Reaper) cuyos grados μ_Crítico difieran en al
   menos 0.3, para que la diferencia de color siempre se note.
2. Cada contorno se pinta con μ_Crítico, la misma curva (50, 70, 100, 100) del Entrenamiento 02.
3. El jugador toca al que cree que está peor, solo por el color.
4. Se revela "PEOR" sobre el correcto, el porcentaje de cada uno y el contador **Aciertos: x/y**.

**Parámetros:** la curva μ_Crítico; la separación mínima de **0.3 en μ** (no en daño crudo: dos daños
bajo 50 valen ambos μ = 0 y se ven idénticos aunque estén lejos); y la regla "está peor el de mayor μ".

---

## 5. Niveles (escena común)

**Pantalla de un nivel, de arriba a abajo:**
- Contexto narrativo.
- Tarjetas de casos, con sus valores, la meta (en Clasificar) y el resultado actual. En Ordenar, el
  **#1 en ámbar** es el que el sistema atendería primero.
- Gráfica de curvas con una línea vertical por caso, y pestañas por variable (más "Superficie" en el
  Nivel 4).
- Controles editables.
- Barra fija con el estado en vivo y **Evaluar**, que abre el resultado con su porcentaje de calidad.

**Consejo para mover curvas:** como siempre se cumple a ≤ b ≤ c ≤ d, para **bajar** una curva mueve los
puntos de **izquierda a derecha** (a → b → c → d); para **subirla**, de **derecha a izquierda**.

### Nivel 01 — Economía de Equipo (Clasificar · se editan curvas)
**Qué enseña:** el grado de pertenencia y cómo la *calibración* de las etiquetas cambia la clasificación.
No hay reglas ni salida: solo una variable.

- **Variable:** Dinero (0–16 000) con Eco, SemiBuy, FullBuy.
- **Casos:** Pistola 1500 → Eco · Bonus 4200 → SemiBuy · Force 6000 → SemiBuy · Ganada 9500 → FullBuy ·
  Banco 12000 → FullBuy.

| Etiqueta | Inicio (mal calibrada) | Solución de referencia |
|---|---|---|
| Eco | (0, 0, 4000, 7000) | (0, 0, 2000, 5000) |
| SemiBuy | (4000, 7000, 9000, 12000) | (2000, 5000, 7000, 10000) |
| FullBuy | (9000, 12000, 16000, 16000) | (7000, 10000, 16000, 16000) |

**Estado inicial:** 3 de 5 bien; calidad 58 %.
- **Bonus (4200)** cae en Eco (μ 0.93) en vez de SemiBuy (0.07).
- **Ganada (9500)** cae en SemiBuy (0.83) en vez de FullBuy (0.17).

El coach "es demasiado conservador": todo le parece más barato de lo que es.

**Solución:** correr las tres curvas hacia la izquierda.
- **Bonus** queda SemiBuy 0.73 vs Eco 0.27.
- **Ganada** queda FullBuy 0.83 vs SemiBuy 0.17.

Resultado: **5/5**, calidad 91 %. Cualquier calibración que deje a cada caso con su etiqueta como la de
mayor μ también es válida: no hay una sola respuesta.

### Nivel 02 — Prioridad de Gank (Ordenar · se editan curvas)
**Qué enseña:** que si una etiqueta es demasiado ancha, el sistema "no distingue" (todo le parece igual).

- **Entrada:** DesventajaDeOro (−3000 a 3000) con Perdiendo, Parejo, Ganando.
- **Salida:** PrioridadGank (0–100) con Baja (0, 0, 25, 50), Media triangular (25, 50, 75) y Alta (50, 75, 100, 100).
- **Reglas** (peso 1, no editables):
  - Perdiendo → Alta
  - Parejo → Media
  - Ganando → Baja
- **Casos:** Top −1800 (debe ir 1º) · Mid 0 (2º) · Bot +1200 (3º).

| Etiqueta | Inicio | Solución de referencia |
|---|---|---|
| Perdiendo | (−3000, −3000, −2800, −2400) | (−3000, −3000, −1500, −300) |
| Parejo | (−2800, −2000, 2000, 2800) | (−1500, −300, 300, 1500) |
| Ganando | (2400, 2800, 3000, 3000) | (300, 1500, 3000, 3000) |

**Estado inicial:** los tres casos están en la meseta de "Parejo" (μ = 1), así que solo se activa la
regla Media y los tres valen **50**. Es un **empate triple**, que no se acepta: calidad 50 %.

**Solución:** angostar "Parejo" y ensanchar "Perdiendo" y "Ganando".
- **Top:** Perdiendo = 1 → **79.9**.
- **Mid:** Parejo = 1 → **50**.
- **Bot:** Parejo 0.25 + Ganando 0.75 → **28**.

Orden Top > Mid > Bot, **correcto**, calidad 76 %.

### Nivel 03 — El Dilema del Healer (Ordenar · se editan pesos)
Es la versión "de datos" del Entrenamiento 01, con las mismas variables y casos. La única diferencia es
que la salida Prioridad suma la etiqueta **Media** (25, 50, 75) para tener cuatro etiquetas.
- **Inicio** (regla 1 en peso 0): Reinhardt **75** > D.Va **50** → incorrecto, calidad 25 %.
- **Solución:** subir el peso de la regla 1 ("HP Bajo Y Daño Alto → MuyAlta"). D.Va pasa a **≈ 88–90**,
  orden correcto y calidad hasta 65 %.

### Nivel 04 — Prioridad de Objetivo (Ordenar · se editan pesos · superficie)
**Qué enseña:** sistemas de **dos entradas con cuatro reglas** que cubren todas las combinaciones, y a
**leer la superficie de control**.

- **Entradas:** VidaRestante y Amenaza (0–100), ambas con Baja (0, 0, 30, 50) y Alta (30, 70, 100, 100).
- **Salida:** PrioridadObjetivo con Baja, Media, Alta y MuyAlta.

**Reglas**
1. Vida Baja Y Amenaza Alta → **MuyAlta** — **peso inicial 0**
2. Vida Baja Y Amenaza Baja → Media
3. Vida Alta Y Amenaza Alta → Alta
4. Vida Alta Y Amenaza Baja → Baja

**Casos y sus grados**

| Caso | Vida | Amenaza | μ Vida (Baja / Alta) | μ Amenaza (Baja / Alta) | Debe ir |
|---|---|---|---|---|---|
| Tirador | 25 | 85 | 1 / 0 | 0 / 1 | 1º |
| Tanque | 80 | 70 | 0 / 1 | 0 / 1 | 2º |
| Support | 35 | 20 | 0.75 / 0.13 | 1 / 0 | 3º |

**Inicio:** Tanque **75** (regla 3) > Tirador **50** (su única regla, la 1, está apagada) > Support
**45.7**. Support mezcla las reglas 2 (0.75) y 4 (0.13). Incorrecto, calidad 52 %.

**Solución:** subir la regla 1. Tirador pasa a **≈ 88–90**: Tirador > Tanque > Support, **correcto**,
calidad hasta 72 %. El equipo aprende a hacer *focus* al enemigo frágil pero peligroso.

**Superficie (pestaña "Superficie"):** el eje X es VidaRestante, el eje Y es Amenaza, y el color es la
prioridad (azul = baja, violeta = media, rojo = alta). Los puntos son los casos. Al subir la regla 1 la
esquina **arriba-izquierda** (poca vida, mucha amenaza) se enciende en rojo: el jugador *ve* qué región
del espacio de decisiones cambió su regla.

---

## 6. Preguntas probables y cómo responderlas

**¿Por qué lógica difusa y no un `if (vida < 20)`?**
Porque un umbral duro trata igual a quien tiene 19 de vida que a quien tiene 1, y muy distinto a quien
tiene 20. Las reglas difusas combinan varios datos en grados y producen decisiones suaves, como razona
una persona. Es la técnica clásica para IA de NPCs y para control.

**¿Por qué en el Healer basta con subir el peso un poquito?**
Porque a D.Va solo le aplica una regla. En Mamdani, recortar una sola curva a cualquier altura deja el
centroide casi en el mismo lugar (87.2 con peso 0.01; 90 con peso 1). El peso importa de verdad cuando
**varias reglas compiten** sobre el mismo caso (como Support en el Nivel 4). Es un buen punto para
mostrar que el peso no es "volumen" lineal.

**¿Por qué no se acepta un empate?**
Un empate significa que el sistema no sabe a quién atender primero. Aceptarlo premiaría una
configuración que no decide nada.

**¿Por qué una calificación en porcentaje además de correcto/incorrecto?**
Porque en lógica difusa la respuesta casi nunca es blanco o negro: un orden correcto por 0.1 puntos es
mucho más frágil que uno correcto por 30. La calidad mide ese margen.

**¿Qué es el centroide?**
El "centro de masa" de la figura que forman las reglas activas recortadas. Es el valor que mejor
representa todas las conclusiones a la vez, ponderadas por cuánto se activó cada una.

**¿Se puede crear un nivel nuevo sin programar?**
Sí. Se usa `Assets → Create → Neuralizer → Fuzzy Scene`, se llenan variables, reglas y casos en el
Inspector (con vista previa de curvas y prueba en vivo), y se regenera el menú.

---

## 7. Correcciones hechas antes de presentar

1. **Triage Rápido ya no puede dar un empate falso.** Antes se exigían 18 puntos de diferencia en el
   daño crudo, pero dos daños bajo 50 (μ = 0) o sobre 70 (μ = 1) se veían iguales y el juego declaraba
   "peor" a uno por desempate arbitrario ("0 % vs 0 %"). Ahora la separación se exige en el **grado μ**
   (al menos 0.3), que es lo que se ve en el contorno: siempre hay una respuesta correcta visible.
2. **Un nivel solo queda "Completado" si se resuelve.** El motor heredado del Módulo 1 marcaba
   completado cualquier intento en ejercicios sin tabla de puntuación; ahora un fallo no cuenta.
3. **El resultado del Entrenamiento 01 muestra las prioridades reales y el margen.** Antes el veredicto
   salía de un porcentaje cuyo máximo posible era ≈ 65 %, así que "¡Excelente!" era inalcanzable. Ahora:
   - D.Va primero por **10 puntos o más** → "¡Excelente!"
   - D.Va primero por menos → "¡Correcto! … por poco margen"
   - Empate → aviso en ámbar
   - Reinhardt primero → "Sube el peso de la regla"

   Ejemplo: con curva Gaussiana y peso 1, D.Va 84.6 vs Reinhardt 75.2 (9.4 puntos) da "correcto por
   poco margen". Es un buen ejemplo de que la forma de la curva afecta qué tan decidido es el sistema.
