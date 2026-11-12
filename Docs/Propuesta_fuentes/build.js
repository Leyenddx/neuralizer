// Construye "Neuralizer_Modulo2_Propuesta.docx" — documento visual para revisión del Módulo 2.
const fs = require("fs");
const path = require("path");
const {
  Document, Packer, Paragraph, TextRun, ImageRun, Table, TableRow, TableCell, AlignmentType,
  HeadingLevel, WidthType, ShadingType, BorderStyle, LevelFormat, PageBreak, Footer, Header,
  PageNumber, VerticalAlign, TableLayoutType,
} = require("docx");

const FIG = path.join(__dirname, "fig");
const OUT = process.argv[2] || path.join(__dirname, "Neuralizer_Modulo2_Propuesta.docx");

// ---------------------------------------------------------------- tokens
const C = {
  ink: "1C2440", muted: "5B6584", violet: "7B3FE4", cyan: "0E9BD6", amber: "C98A00",
  green: "1A9A61", red: "D63B55", line: "DDE2EC", soft: "F4F6FB", violetSoft: "F1EBFD",
  cyanSoft: "E7F5FC", amberSoft: "FFF6E0", greenSoft: "E6F6EF",
};
const FONT = "Calibri";
const PAGE_W = 12240, MARGIN = 1080, CONTENT = PAGE_W - 2 * MARGIN; // 10080 DXA = 7"
const PX_PER_DXA = 96 / 1440;

// --------------------------------------------------------------- helpers
function pngSize(file) {
  const b = fs.readFileSync(file);
  return { w: b.readUInt32BE(16), h: b.readUInt32BE(20) };
}

function img(name, widthIn, opts = {}) {
  const file = path.join(FIG, name);
  const { w, h } = pngSize(file);
  const wpx = Math.round(widthIn * 96);
  const hpx = Math.round(wpx * h / w);
  return new Paragraph({
    alignment: opts.align || AlignmentType.CENTER,
    spacing: { before: opts.before ?? 120, after: opts.after ?? 60 },
    keepNext: opts.keepNext ?? false,
    children: [new ImageRun({ type: "png", data: fs.readFileSync(file), transformation: { width: wpx, height: hpx },
      altText: { title: opts.alt || name, description: opts.alt || name, name } })],
  });
}

function run(text, o = {}) {
  return new TextRun({ text, font: o.font || FONT, size: o.size || 21, bold: o.bold, italics: o.italics,
    color: o.color || C.ink });
}

// p("texto **negrita** más texto") — negritas con **…**
function p(text, o = {}) {
  const parts = String(text).split(/(\*\*[^*]+\*\*)/g).filter(Boolean);
  return new Paragraph({
    alignment: o.align,
    spacing: { before: o.before ?? 60, after: o.after ?? 100, line: o.line ?? 300 },
    keepNext: o.keepNext,
    indent: o.indent,
    children: parts.map(t => t.startsWith("**")
      ? run(t.slice(2, -2), { ...o, bold: true })
      : run(t, o)),
  });
}

function caption(text) {
  return p(text, { size: 18, color: C.muted, italics: true, align: AlignmentType.CENTER, before: 0, after: 200 });
}

function h1(text) { return new Paragraph({ heading: HeadingLevel.HEADING_1, children: [new TextRun(text)] }); }
function h2(text) { return new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun(text)] }); }
function h3(text) { return new Paragraph({ heading: HeadingLevel.HEADING_3, children: [new TextRun(text)] }); }
function pageBreak() { return new Paragraph({ children: [new PageBreak()] }); }

let listInstance = 0;
function steps(items) {
  const inst = ++listInstance;
  return items.map(t => {
    const parts = String(t).split(/(\*\*[^*]+\*\*)/g).filter(Boolean);
    return new Paragraph({
      numbering: { reference: "steps", level: 0, instance: inst },
      spacing: { before: 40, after: 60, line: 290 },
      children: parts.map(x => x.startsWith("**") ? run(x.slice(2, -2), { bold: true }) : run(x)),
    });
  });
}
function bullets(items) {
  return items.map(t => {
    const parts = String(t).split(/(\*\*[^*]+\*\*)/g).filter(Boolean);
    return new Paragraph({
      numbering: { reference: "bullets", level: 0 },
      spacing: { before: 30, after: 50, line: 290 },
      children: parts.map(x => x.startsWith("**") ? run(x.slice(2, -2), { bold: true }) : run(x)),
    });
  });
}

const border = (color = C.line, size = 6) => ({ style: BorderStyle.SINGLE, size, color });
const borders = (color = C.line) => ({ top: border(color), bottom: border(color), left: border(color), right: border(color) });
const noBorders = () => ({ top: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" }, bottom: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
  left: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" }, right: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" } });

function cell(children, width, o = {}) {
  return new TableCell({
    width: { size: width, type: WidthType.DXA },
    borders: o.borders || borders(o.borderColor),
    shading: o.fill ? { fill: o.fill, type: ShadingType.CLEAR, color: "auto" } : undefined,
    margins: { top: o.pad ?? 90, bottom: o.pad ?? 90, left: 140, right: 140 },
    verticalAlign: o.vAlign || VerticalAlign.TOP,
    columnSpan: o.span,
    children: Array.isArray(children) ? children : [children],
  });
}

// Tabla con encabezado. rows: array de arrays de strings.
function table(headers, rows, widths, o = {}) {
  const total = widths.reduce((a, b) => a + b, 0);
  const head = new TableRow({
    tableHeader: true,
    children: headers.map((h, i) => cell(p(h, { bold: true, color: "FFFFFF", size: 19, before: 0, after: 0 }), widths[i],
      { fill: o.headFill || C.ink, borderColor: o.headFill || C.ink })),
  });
  const body = rows.map((r, ri) => new TableRow({
    cantSplit: true,
    children: r.map((t, i) => cell(p(t, { size: 19, before: 0, after: 0, line: 270, bold: o.boldFirst && i === 0 }), widths[i],
      { fill: ri % 2 ? C.soft : undefined })),
  }));
  return new Table({ width: { size: total, type: WidthType.DXA }, columnWidths: widths, rows: [head, ...body] });
}

// Recuadro de color con título + contenido (callout)
function callout(title, lines, color, fill) {
  const kids = [p(title, { bold: true, color, size: 21, before: 0, after: 60 })];
  for (const l of lines) kids.push(p(l, { size: 20, before: 0, after: 60, line: 280 }));
  return new Table({
    width: { size: CONTENT, type: WidthType.DXA }, columnWidths: [CONTENT],
    rows: [new TableRow({ children: [new TableCell({
      width: { size: CONTENT, type: WidthType.DXA },
      borders: { top: border(fill, 4), bottom: border(fill, 4), right: border(fill, 4), left: { style: BorderStyle.SINGLE, size: 36, color } },
      shading: { fill, type: ShadingType.CLEAR, color: "auto" },
      margins: { top: 140, bottom: 120, left: 220, right: 200 },
      children: kids,
    })] })],
  });
}

function spacer(after = 120) { return new Paragraph({ spacing: { before: 0, after }, children: [] }); }

// Ficha rápida: pares etiqueta / valor
function factSheet(pairs, accent) {
  const w1 = 2300, w2 = CONTENT - w1;
  return new Table({
    width: { size: CONTENT, type: WidthType.DXA }, columnWidths: [w1, w2],
    rows: pairs.map(([k, v]) => new TableRow({ cantSplit: true, children: [
      cell(p(k, { bold: true, size: 19, color: accent, before: 0, after: 0 }), w1, { fill: C.soft }),
      cell(p(v, { size: 20, before: 0, after: 0, line: 280 }), w2),
    ] })),
  });
}

// Cuadro de retroalimentación por ejercicio
function feedbackBox(name) {
  const box = "☐";
  const opt = (t, color) => new Paragraph({ spacing: { before: 0, after: 0 }, children: [
    new TextRun({ text: box + "  ", font: "Segoe UI Symbol", size: 24, color }),
    run(t, { bold: true, size: 20, color }),
  ] });
  const third = Math.floor(CONTENT / 3);
  const widths = [third, third, CONTENT - 2 * third];
  return new Table({
    width: { size: CONTENT, type: WidthType.DXA }, columnWidths: widths,
    rows: [
      new TableRow({ children: [cell(p(`Retroalimentación — ${name}`, { bold: true, color: "FFFFFF", size: 20, before: 0, after: 0 }),
        CONTENT, { span: 3, fill: C.violet, borderColor: C.violet })] }),
      new TableRow({ children: [
        cell(opt("Aprobar tal cual", C.green), widths[0], { borderColor: C.violet }),
        cell(opt("Aprobar con ajustes", C.amber), widths[1], { borderColor: C.violet }),
        cell(opt("Replantear / descartar", C.red), widths[2], { borderColor: C.violet }),
      ] }),
      new TableRow({ height: { value: 1500, rule: "atLeast" }, children: [
        cell([p("Comentarios / cambios sugeridos:", { size: 18, color: C.muted, italics: true, before: 0, after: 0 })],
          CONTENT, { span: 3, borderColor: C.violet }),
      ] }),
    ],
  });
}

// Captura de pantalla (izquierda) + leyenda numerada (derecha)
function captureWithLegend(imageName, imgWidthIn, title, items) {
  const left = Math.round(imgWidthIn * 1440) + 280;
  const right = CONTENT - left;
  const legend = [p(title, { bold: true, color: C.violet, size: 22, before: 0, after: 120 })];
  items.forEach(([t, d], i) => {
    legend.push(new Paragraph({ spacing: { before: 60, after: 20 }, keepNext: true, children: [
      new TextRun({ text: ` ${i + 1} `, font: FONT, size: 20, bold: true, color: C.ink, shading: { type: ShadingType.CLEAR, fill: "F5B82E", color: "auto" } }),
      run("  " + t, { bold: true, size: 20 }),
    ] }));
    legend.push(p(d, { size: 19, color: C.muted, before: 0, after: 80, line: 270, indent: { left: 360 } }));
  });
  return new Table({
    width: { size: CONTENT, type: WidthType.DXA }, columnWidths: [left, right],
    rows: [new TableRow({ cantSplit: true, children: [
      cell(img(imageName, imgWidthIn, { before: 0, after: 0 }), left, { borders: noBorders(), pad: 0 }),
      cell(legend, right, { borders: noBorders(), pad: 40 }),
    ] })],
  });
}

// Dos imágenes lado a lado
function sideBySide(a, b, wIn) {
  const half = CONTENT / 2;
  return new Table({
    width: { size: CONTENT, type: WidthType.DXA }, columnWidths: [half, half],
    rows: [new TableRow({ cantSplit: true, children: [
      cell(img(a.name, wIn, { before: 0, after: 0 }), half, { borders: noBorders(), pad: 0 }),
      cell([img(b.name, wIn, { before: 0, after: 0 })], half, { borders: noBorders(), pad: 0 }),
    ] })],
  });
}

// Encabezado de ficha (banda de color con código + nombre)
function exerciseBanner(code, name, subtitle, color) {
  return new Table({
    width: { size: CONTENT, type: WidthType.DXA }, columnWidths: [1500, CONTENT - 1500],
    rows: [new TableRow({ cantSplit: true, children: [
      cell(p(code, { bold: true, size: 30, color: "FFFFFF", align: AlignmentType.CENTER, before: 0, after: 0 }), 1500,
        { fill: color, borderColor: color, vAlign: VerticalAlign.CENTER }),
      cell([p(name, { bold: true, size: 30, before: 0, after: 20 }), p(subtitle, { size: 19, color: C.muted, before: 0, after: 0 })],
        CONTENT - 1500, { fill: C.soft, borderColor: C.soft }),
    ] })],
  });
}

// ---------------------------------------------------------------- contenido
const children = [];
const add = (...xs) => xs.flat().forEach(x => children.push(x));

// ============ PORTADA
add(
  spacer(1400),
  p("NEURALIZER", { size: 22, bold: true, color: C.cyan, before: 0, after: 80 }),
  p("Módulo 2 — Lógica Difusa", { size: 56, bold: true, before: 0, after: 120 }),
  p("Propuesta de ejercicios y niveles para revisión", { size: 30, color: C.violet, before: 0, after: 400 }),
  new Paragraph({ border: { bottom: { style: BorderStyle.SINGLE, size: 12, color: C.amber, space: 1 } }, spacing: { after: 300 }, children: [] }),
  p("Este documento presenta, de forma visual, los 7 ejercicios propuestos para el Módulo 2: qué enseña cada uno, qué ve y controla el jugador, cómo se resuelve y qué significa cada parámetro. Todas las capturas y gráficas salen del prototipo jugable y de su motor real.", { size: 23, line: 340 }),
  spacer(200),
  factSheet([
    ["Estado", "Prototipo jugable en Unity (build WebGL, pantalla vertical 540×960)."],
    ["Contenido", "3 ejercicios de entrenamiento + 4 niveles."],
    ["Versión", "1.0 · noviembre 2026"],
    ["Preparado por", "__________________________"],
  ], C.violet),
  spacer(300),
  callout("Cómo revisar este documento", [
    "Cada ejercicio termina con un **cuadro de retroalimentación**: marca Aprobar, Aprobar con ajustes o Replantear, y escribe tus comentarios ahí mismo.",
    "Al final hay **decisiones abiertas** donde necesito tu criterio, y una hoja de evaluación general.",
  ], C.amber, C.amberSoft),
  pageBreak(),
);

// ============ 1. RESUMEN
add(
  h1("1. Resumen de la propuesta"),
  p("El Módulo 2 enseña **lógica difusa**: razonar con grados («bastante dañado», «algo peligroso») en lugar de umbrales de sí/no. Es la técnica con la que se programa la IA de muchos personajes de videojuegos. Para que sea cercano, todo está ambientado en situaciones de **esports** (economía de ronda, ganks, curar aliados, elegir objetivo)."),
  p("La progresión va de **percibir** el concepto a **diseñar** un sistema completo:", { keepNext: true }),
  img("progresion.png", 7.0, { alt: "Progresión del módulo" }),
  caption("Figura 1 · Progresión pedagógica del módulo."),
  table(["#", "Ejercicio", "Concepto que enseña", "El jugador…", "Se califica"], [
    ["E01", "El Dilema del Healer", "Reglas con «Y» y peso de una regla", "mueve el peso de una regla", "Sí (margen)"],
    ["E02", "Lectura del Daño", "Grado de pertenencia como sensación", "desliza el daño y observa el color", "No"],
    ["E03", "Triage Rápido", "Comparar grados bajo presión", "elige al aliado en peor estado", "Aciertos"],
    ["N01", "Economía de Equipo", "Calibrar funciones de pertenencia", "mueve los puntos de las curvas", "Sí"],
    ["N02", "Prioridad de Gank", "Etiquetas demasiado anchas no distinguen", "corrige el ancho de las curvas", "Sí"],
    ["N03", "El Dilema del Healer", "El mismo caso, como nivel de datos", "ajusta pesos de reglas", "Sí"],
    ["N04", "Prioridad de Objetivo", "Dos entradas, 4 reglas y superficie de control", "ajusta pesos y lee la superficie", "Sí"],
  ], [700, 2050, 2900, 2650, 1780], { boldFirst: true }),
  spacer(),
  pageBreak(),
);

// ============ 2. CÓMO FUNCIONA
add(
  h1("2. Cómo funciona la lógica difusa en el juego"),
  p("Todos los ejercicios corren sobre el mismo motor (modelo **Mamdani**), que convierte los datos de una situación en una decisión en cuatro pasos:", { keepNext: true }),
  img("pipeline.png", 7.0, { alt: "Los cuatro pasos del motor difuso" }),
  caption("Figura 2 · De los datos a la decisión."),
  h3("La pieza clave: la función de pertenencia"),
  p("Cada etiqueta («Bajo», «Alto», «Eco»…) tiene una curva que dice **qué tanto** pertenece un valor a esa etiqueta, de 0 a 1. Los puntos a, b, c, d son justamente lo que el jugador mueve en los niveles de curvas:", { keepNext: true }),
  img("anatomia.png", 6.2, { alt: "Anatomía de una función trapezoidal" }),
  caption("Figura 3 · Una curva trapezoidal y cómo se lee un valor."),
  p("El motor admite cuatro formas de curva (el Ejercicio 01 permite cambiar entre ellas en vivo):", { keepNext: true }),
  img("formas.png", 7.0, { alt: "Formas de curva" }),
  caption("Figura 4 · Formas de función de pertenencia disponibles."),
  table(["Término", "Significado en el juego"], [
    ["Variable", "Un dato del problema (HP, Dinero, Amenaza). Tiene un rango de valores."],
    ["Etiqueta", "Una palabra con la que razonamos sobre la variable (Bajo, Alto, Eco…)."],
    ["Grado μ", "De 0 a 1: qué tanto pertenece un valor a una etiqueta. Un valor puede pertenecer a varias a la vez."],
    ["Regla", "SI <condiciones> ENTONCES <decisión>. «Y» toma el mínimo de los grados, «O» el máximo."],
    ["Peso", "Multiplica la fuerza de una regla (0 = apagada, 1 = completa)."],
    ["Centroide", "El «centro de masa» de todas las conclusiones: el número final de la decisión."],
    ["Calidad", "Puntaje continuo de qué tan holgado es el resultado (no solo correcto/incorrecto)."],
  ], [1900, CONTENT - 1900], { boldFirst: true }),
  pageBreak(),
);

// ============ 3. MENÚ Y PANTALLA DE NIVEL
add(
  h1("3. Recorrido visual de la interfaz"),
  captureWithLegend("cap_menu.png", 3.0, "Menú del Módulo 2", [
    ["Selector de módulo", "Cambia entre Módulo 1 y 2; el resaltado se desliza a la pestaña activa."],
    ["Identidad del módulo", "Logotipo tipográfico y fondo ambiental animado (estilo esports)."],
    ["Encabezado y progreso", "Explica la idea del módulo y muestra cuántos niveles se han completado."],
    ["Secciones", "«Entrenamiento» (prácticas guiadas) y «Niveles» (ejercicios de diseño)."],
    ["Tarjeta de ejercicio", "Número, nombre, descripción en una línea, tipo de ejercicio y estado «Completado»."],
  ]),
  caption("Figura 5 · Menú del Módulo 2 (captura del prototipo)."),
  pageBreak(),
  captureWithLegend("cap_nivel.png", 3.0, "Pantalla de un nivel", [
    ["Título del nivel", "Nombre del escenario."],
    ["Contexto narrativo", "La situación de juego y qué está mal con el sistema actual."],
    ["Casos", "Lo que hay que ordenar o clasificar, con sus valores y su resultado en vivo. El #1 se resalta en ámbar."],
    ["Pestañas", "Una por variable de entrada (y «Superficie» en el Nivel 04)."],
    ["Gráfica", "Curvas de la variable elegida, con una línea por cada caso."],
    ["Controles", "Pesos de reglas o puntos de curvas, según el nivel."],
    ["Estado en vivo", "El orden actual que produce el sistema, mientras se ajusta."],
    ["Evaluar", "Califica y muestra el resultado con su porcentaje de calidad."],
  ]),
  caption("Figura 6 · Anatomía de la pantalla de un nivel (Nivel 04)."),
  pageBreak(),
);

// ============ 4. ENTRENAMIENTO
add(h1("4. Ejercicios de entrenamiento"));

// ---- E01
add(
  exerciseBanner("E01", "El Dilema del Healer", "Entrenamiento · reglas con «Y» y peso de una regla", C.cyan),
  spacer(120),
  factSheet([
    ["Situación", "Dos aliados en crítico: **Reinhardt** (HP 18, daño entrante 20) y **D.Va** (HP 16, daño entrante 85). Solo puedes curar a uno primero."],
    ["Respuesta correcta", "Curar primero a **D.Va**: tiene casi la misma vida pero recibe mucho más daño."],
    ["El jugador controla", "El **peso** de la regla 1 (slider 0–1) y la **forma** de las curvas de daño (Trapezoidal, Triangular, Sigmoide, Gaussiana)."],
    ["Qué enseña", "Que una regla combina dos datos con «Y», y que encender o apagar una regla cambia la decisión."],
  ], C.cyan),
  spacer(120),
  img("ej1_curvas.png", 7.0, { alt: "Curvas de HP y daño entrante" }),
  caption("Figura 7 · Curvas de las dos entradas. Las líneas marcan dónde cae cada aliado."),
  table(["#", "Regla", "Peso inicial"], [
    ["1", "SI HP es Bajo Y DañoEntrante es Alto → Prioridad MuyAlta", "0 (apagada)"],
    ["2", "SI HP es Bajo Y DañoEntrante es Bajo → Prioridad Alta", "1"],
    ["3", "SI HP es Alto → Prioridad Baja", "1"],
  ], [600, 7480, 2000], { boldFirst: true }),
  spacer(),
  img("ej1_peso.png", 6.4, { alt: "Prioridad según el peso de la regla" }),
  caption("Figura 8 · Prioridad calculada de cada aliado según el peso de la regla 1."),
  h3("Cómo se resuelve"),
  steps([
    "Al inicio la regla 1 está apagada: Reinhardt obtiene **75** y D.Va **50** (no le aplica ninguna regla), así que el sistema cura primero a Reinhardt → **incorrecto**.",
    "El jugador sube el peso de la regla 1. D.Va (HP Bajo = 1, Daño Alto = 1) activa «MuyAlta» y salta a **≈ 88–90**.",
    "Al pulsar **Evaluar** se muestran ambas prioridades y el margen: con 10 puntos o más de ventaja, «¡Excelente!».",
    "Extra: con la curva **Gaussiana** el margen baja a +9.4 («correcto por poco margen»): la forma de la curva cambia qué tan decidido es el sistema.",
  ]),
  spacer(),
  feedbackBox("E01 El Dilema del Healer"),
  pageBreak(),
);

// ---- E02
add(
  exerciseBanner("E02", "Lectura del Daño", "Entrenamiento · el grado de pertenencia como sensación", C.cyan),
  spacer(120),
  factSheet([
    ["Situación", "Un personaje con un contorno de color, como el aviso de aliado dañado en un shooter."],
    ["El jugador controla", "Un slider de daño (0–100 %) **sin números visibles**."],
    ["Qué ve", "El contorno y el resplandor pasan de verde a amarillo y a rojo, y el pulso se acelera mientras más crítico."],
    ["Respuesta correcta", "No hay: es un ejercicio de percepción. Al final, «Revelar la curva real» muestra la función y el porcentaje exacto."],
    ["Qué enseña", "Que «se está poniendo rojo» es en realidad un grado entre 0 y 1 calculado por una curva."],
  ], C.cyan),
  spacer(120),
  img("ej2_curva.png", 6.6, { alt: "Curva mu crítico con colores" }),
  caption("Figura 9 · μ_Crítico(x) = trapezoidal(50, 70, 100, 100) y el color resultante."),
  table(["Daño (%)", "0 – 50", "55", "60", "65", "70 – 100"], [
    ["μ_Crítico", "0", "0.25", "0.50", "0.75", "1"],
    ["Contorno", "Verde", "Verde-amarillo", "Amarillo", "Naranja", "Rojo"],
  ], [1780, 1660, 1660, 1660, 1660, 1660], { boldFirst: true }),
  spacer(),
  feedbackBox("E02 Lectura del Daño"),
  pageBreak(),
);

// ---- E03
add(
  exerciseBanner("E03", "Triage Rápido", "Entrenamiento · comparar grados y decidir bajo presión", C.cyan),
  spacer(120),
);
// Captura a la izquierda + ficha rápida (con ancho propio) a la derecha.
{
  const left = 3000, right = CONTENT - left - 200;
  const fs2 = new Table({
    width: { size: right, type: WidthType.DXA }, columnWidths: [1500, right - 1500],
    rows: [
      ["Situación", "Reinhardt y Reaper con daños sorteados; cada contorno muestra su grado μ_Crítico."],
      ["El jugador", "Toca al que cree que está peor, **solo por el color**."],
      ["Resultado", "Se revela «PEOR», el porcentaje de cada uno y el contador Aciertos x/y."],
      ["Qué enseña", "Decidir comparando grados de pertenencia, como en una partida real."],
    ].map(([k, v]) => new TableRow({ cantSplit: true, children: [
      cell(p(k, { bold: true, size: 19, color: C.cyan, before: 0, after: 0 }), 1500, { fill: C.soft }),
      cell(p(v, { size: 20, before: 0, after: 0, line: 280 }), right - 1500),
    ] })),
  });
  add(new Table({
    width: { size: CONTENT, type: WidthType.DXA }, columnWidths: [left, CONTENT - left],
    rows: [new TableRow({ cantSplit: true, children: [
      cell(img("cap_ej3_triage.png", 1.9, { before: 0, after: 0 }), left, { borders: noBorders(), pad: 0 }),
      cell([fs2], CONTENT - left, { borders: noBorders(), pad: 0 }),
    ] })],
  }));
}
add(
  caption("Figura 10 · Pantalla de Triage Rápido (captura del prototipo)."),
  img("ej3_curva.png", 6.4, { alt: "Curva de triage" }),
  caption("Figura 11 · Cómo se sortea cada ronda: los dos grados deben diferir al menos 0.3."),
  h3("Cómo funciona cada ronda"),
  steps([
    "Se sortean dos daños entre 0 y 100 cuyos grados μ_Crítico difieran al menos **0.3** (para que la diferencia de color siempre se note).",
    "Cada contorno se pinta con su grado: más rojo = más crítico.",
    "El jugador toca a uno; está peor el de **mayor μ**.",
    "Se revela la respuesta y se suma al contador; «Siguiente ronda» repite con valores nuevos.",
  ]),
  spacer(),
  feedbackBox("E03 Triage Rápido"),
  pageBreak(),
);

// ============ 5. NIVELES
add(
  h1("5. Niveles"),
  p("Los cuatro niveles comparten una misma pantalla (ver Figura 6) y se definen como **datos** (variables, reglas y casos), así que se pueden crear o modificar niveles sin programar. Cada nivel **arranca roto** a propósito y el jugador debe corregir el sistema."),
  p("Al pulsar **Evaluar** el juego califica (correcto si el orden o la clasificación coinciden y no hay empates) y muestra un porcentaje de calidad:", { keepNext: true }),
  new Table({
    width: { size: CONTENT, type: WidthType.DXA }, columnWidths: [3000, CONTENT - 3000],
    rows: [new TableRow({ cantSplit: true, children: [
      cell(img("cap_nivel_resultado.png", 1.9, { before: 0, after: 0 }), 3000, { borders: noBorders(), pad: 0 }),
      cell([
        callout("Reglas de calificación", [
          "**Ordenar:** el orden calculado debe coincidir con el esperado y sin empates (un empate significa que el sistema no sabe decidir).",
          "**Clasificar:** cada caso debe caer en su etiqueta esperada (la de mayor grado μ).",
          "**Calidad (%):** mide el margen. 50 % es empate; más de 50 % indica que el orden va en la dirección correcta.",
          "Un nivel solo queda «Completado» en el menú si se resuelve.",
        ], C.violet, C.violetSoft),
      ], CONTENT - 3000, { borders: noBorders(), pad: 0 }),
    ] })],
  }),
  caption("Figura 12 · Popup de resultado de un nivel."),
  pageBreak(),
);

// ---- N01
add(
  exerciseBanner("N01", "Economía de Equipo", "Nivel · Clasificar · el jugador mueve las curvas", C.violet),
  spacer(120),
  factSheet([
    ["Situación", "El coach clasifica el dinero del equipo en Eco, SemiBuy o FullBuy, pero sus curvas están mal calibradas."],
    ["Variable", "Dinero (0 – 16 000) con tres etiquetas: Eco, SemiBuy, FullBuy."],
    ["Casos", "Pistola 1500 → Eco · Bonus 4200 → SemiBuy · Force 6000 → SemiBuy · Ganada 9500 → FullBuy · Banco 12 000 → FullBuy."],
    ["Qué enseña", "El grado de pertenencia y cómo calibrar las curvas cambia la clasificación. No hay reglas: solo una variable."],
  ], C.violet),
  spacer(120),
  img("n1_curvas.png", 7.0, { alt: "Curvas del nivel 1 antes y después" }),
  caption("Figura 13 · Curvas al inicio (arriba) y una solución de referencia (abajo)."),
  table(["Etiqueta", "Al inicio (a, b, c, d)", "Solución de referencia"], [
    ["Eco", "0, 0, 4000, 7000", "0, 0, 2000, 5000"],
    ["SemiBuy", "4000, 7000, 9000, 12 000", "2000, 5000, 7000, 10 000"],
    ["FullBuy", "9000, 12 000, 16 000, 16 000", "7000, 10 000, 16 000, 16 000"],
  ], [2200, 3940, 3940], { boldFirst: true }),
  h3("Cómo se resuelve"),
  steps([
    "Al inicio hay **3 de 5** bien (calidad 58 %): Bonus cae en Eco y Ganada en SemiBuy.",
    "El jugador corre las tres curvas hacia la izquierda. Para **bajar** una curva se mueven los puntos de izquierda a derecha (a → b → c → d).",
    "Con la solución de referencia: Bonus queda SemiBuy 0.73 vs Eco 0.27; Ganada queda FullBuy 0.83 vs SemiBuy 0.17 → **5/5**, calidad 91 %.",
    "Cualquier calibración que deje a cada caso con su etiqueta como la de mayor grado también es válida.",
  ]),
  spacer(),
  feedbackBox("N01 Economía de Equipo"),
  pageBreak(),
);

// ---- N02
add(
  exerciseBanner("N02", "Prioridad de Gank", "Nivel · Ordenar · el jugador mueve las curvas", C.violet),
  spacer(120),
  factSheet([
    ["Situación", "El jungla debería priorizar Top > Mid > Bot, pero la etiqueta «Parejo» es tan ancha que todo le parece igual."],
    ["Entrada", "DesventajaDeOro (−3000 a 3000): Perdiendo, Parejo, Ganando."],
    ["Salida y reglas", "PrioridadGank (0–100). Perdiendo → Alta · Parejo → Media · Ganando → Baja (no editables)."],
    ["Casos", "Top −1800 (1º) · Mid 0 (2º) · Bot +1200 (3º)."],
    ["Qué enseña", "Una etiqueta demasiado ancha hace que el sistema no distinga entre casos."],
  ], C.violet),
  spacer(120),
  img("n2_curvas.png", 7.0, { alt: "Curvas del nivel 2 antes y después" }),
  caption("Figura 14 · «Parejo» cubre los tres casos al inicio (arriba); al angostarlo cada carril activa una regla distinta (abajo)."),
  img("n2_barras.png", 5.6, { alt: "Prioridad de cada carril" }),
  caption("Figura 15 · Prioridad calculada de cada carril antes y después."),
  h3("Cómo se resuelve"),
  steps([
    "Al inicio los tres carriles están en la meseta de «Parejo»: solo se activa la regla Media y los tres valen **50** → triple empate, no se acepta.",
    "El jugador angosta «Parejo» y ensancha «Perdiendo» y «Ganando».",
    "Resultado: Top **79.9** > Mid **50** > Bot **28** → correcto, calidad 76 %.",
  ]),
  spacer(),
  feedbackBox("N02 Prioridad de Gank"),
  pageBreak(),
);

// ---- N03
add(
  exerciseBanner("N03", "El Dilema del Healer", "Nivel · Ordenar · el jugador ajusta pesos de reglas", C.violet),
  spacer(120),
  factSheet([
    ["Situación", "La misma de E01, ahora como nivel de datos dentro de la pantalla común de niveles."],
    ["Diferencia con E01", "La salida Prioridad tiene una etiqueta extra («Media») y se editan los pesos de las tres reglas."],
    ["Qué enseña", "Repasar el concepto de E01 con la interfaz de diseño completa (antes de los niveles más abiertos)."],
  ], C.violet),
  spacer(120),
  new Table({
    width: { size: CONTENT, type: WidthType.DXA }, columnWidths: [3000, CONTENT - 3000],
    rows: [new TableRow({ cantSplit: true, children: [
      cell(img("cap_nivel3.png", 1.9, { before: 0, after: 0 }), 3000, { borders: noBorders(), pad: 0 }),
      cell([img("n3_barras.png", 4.6, { before: 300, after: 0 })], CONTENT - 3000, { borders: noBorders(), pad: 0 }),
    ] })],
  }),
  caption("Figura 16 · Nivel 03: captura y prioridad con la regla 1 apagada vs. encendida."),
  h3("Cómo se resuelve"),
  steps([
    "Al inicio: Reinhardt **75** > D.Va **50** → incorrecto (calidad 25 %).",
    "Subir el peso de la regla «HP Bajo Y Daño Alto → MuyAlta».",
    "D.Va pasa a **≈ 88–90** → orden correcto (calidad hasta 65 %).",
  ]),
  spacer(),
  feedbackBox("N03 El Dilema del Healer"),
  pageBreak(),
);

// ---- N04
add(
  exerciseBanner("N04", "Prioridad de Objetivo", "Nivel · Ordenar · pesos de reglas + superficie de control", C.violet),
  spacer(120),
  factSheet([
    ["Situación", "Focus fire: el equipo ignora a los enemigos frágiles pero peligrosos."],
    ["Entradas", "VidaRestante y Amenaza (0–100), ambas con Baja y Alta."],
    ["Salida", "PrioridadObjetivo con Baja, Media, Alta y MuyAlta."],
    ["Casos", "Tirador (vida 25, amenaza 85) → 1º · Tanque (80, 70) → 2º · Support (35, 20) → 3º."],
    ["Qué enseña", "Sistemas de dos entradas con reglas que cubren todas las combinaciones, y a leer la superficie de control."],
  ], C.violet),
  spacer(120),
  table(["#", "Regla", "Peso inicial"], [
    ["1", "Vida Baja Y Amenaza Alta → MuyAlta", "0 (apagada)"],
    ["2", "Vida Baja Y Amenaza Baja → Media", "1"],
    ["3", "Vida Alta Y Amenaza Alta → Alta", "1"],
    ["4", "Vida Alta Y Amenaza Baja → Baja", "1"],
  ], [600, 7480, 2000], { boldFirst: true }),
  spacer(),
  img("n4_curvas.png", 7.0, { alt: "Curvas del nivel 4" }),
  caption("Figura 17 · Curvas de las dos entradas con la posición de cada enemigo."),
  img("n4_barras.png", 5.8, { alt: "Prioridad de cada enemigo" }),
  caption("Figura 18 · Prioridad de cada enemigo con la regla 1 apagada vs. encendida."),
  img("n4_superficie.png", 6.8, { alt: "Superficie de control" }),
  caption("Figura 19 · Superficie de control: el color es la prioridad para cada combinación de vida y amenaza."),
  h3("Cómo se resuelve"),
  steps([
    "Al inicio: Tanque **75** > Tirador **50** > Support **45.7** → incorrecto (calidad 52 %). El Tirador no activa ninguna regla encendida.",
    "Subir el peso de la regla 1 (Vida Baja Y Amenaza Alta → MuyAlta).",
    "Tirador pasa a **≈ 88–90**: Tirador > Tanque > Support → correcto (calidad hasta 72 %).",
    "En la pestaña **Superficie** se ve cómo la esquina «poca vida + mucha amenaza» se enciende en rojo: el jugador ve qué región de decisiones cambió su regla.",
  ]),
  callout("Detalle a explicar", [
    "Support (vida 35, amenaza 20) activa dos reglas a la vez: la 2 con fuerza 0.75 y la 4 con 0.13. Su prioridad (45.7) es una mezcla de ambas: así razona un sistema difuso cuando un caso está «entre» categorías.",
  ], C.cyan, C.cyanSoft),
  spacer(),
  feedbackBox("N04 Prioridad de Objetivo"),
  pageBreak(),
);

// ============ 6. DECISIONES ABIERTAS
add(
  h1("6. Decisiones abiertas para el revisor"),
  p("Puntos donde necesito tu criterio antes de cerrar el módulo:"),
);
const decisions = [
  ["Personajes y arte", "Los entrenamientos usan retratos de Overwatch (Reinhardt, D.Va, Reaper, Soldier 76). ¿Mantenemos esa referencia, usamos personajes propios o genéricos?", "Mantener · Personajes propios · Genéricos"],
  ["Dificultad de N03 y N04", "En ambos basta con encender la regla (cualquier peso mayor a 0 resuelve), porque cada caso decisivo activa una sola regla. ¿Agregamos casos donde varias reglas compitan, para que el valor exacto del peso importe?", "Así está bien · Hacerlos más exigentes"],
  ["Puntuación y medallas", "Los niveles hoy no otorgan puntaje ni medallas (solo correcto/incorrecto + calidad). ¿Activamos tiempo, puntaje y medallas como en el Módulo 1?", "Sí · No · Solo en niveles"],
  ["Cantidad de niveles", "Hoy son 4 niveles + 3 entrenamientos. ¿Agregamos un nivel final donde el jugador escriba sus propias reglas desde cero?", "Así está bien · Agregar nivel final"],
  ["Pistas en Clasificar", "En N01 se muestra la etiqueta esperada de cada caso («Meta: SemiBuy»). ¿Es la ayuda adecuada o preferimos que el jugador la deduzca?", "Mostrar meta · Ocultar meta"],
];
add(table(["Tema", "Pregunta", "Opciones (marca una)"],
  decisions.map(([a, b, c]) => [a, b, c.split(" · ").map(o => "☐ " + o).join("   ")]),
  [1900, 5280, 2900], { boldFirst: true }));
add(spacer(240));

// ============ 7. EVALUACIÓN GENERAL
add(
  h2("Evaluación general del módulo"),
  table(["Aspecto", "Calificación (1 – 5)", "Comentarios"], [
    ["Claridad de los conceptos", "1   2   3   4   5", ""],
    ["Progresión de dificultad", "1   2   3   4   5", ""],
    ["Temática esports / motivación", "1   2   3   4   5", ""],
    ["Diseño visual de la interfaz", "1   2   3   4   5", ""],
    ["Utilidad educativa general", "1   2   3   4   5", ""],
  ], [3000, 2200, 4880], { boldFirst: true }),
  spacer(200),
  new Table({
    width: { size: CONTENT, type: WidthType.DXA }, columnWidths: [CONTENT],
    rows: [
      new TableRow({ children: [cell(p("Comentarios generales y prioridades de cambio", { bold: true, color: "FFFFFF", before: 0, after: 0 }), CONTENT, { fill: C.ink, borderColor: C.ink })] }),
      new TableRow({ height: { value: 2600, rule: "atLeast" }, children: [cell(p(""), CONTENT)] }),
    ],
  }),
);

// ---------------------------------------------------------------- documento
const doc = new Document({
  creator: "Neuralizer",
  title: "Neuralizer · Módulo 2 — Propuesta para revisión",
  description: "Ejercicios y niveles del Módulo 2 (lógica difusa) para revisión",
  styles: {
    default: { document: { run: { font: FONT, size: 21, color: C.ink } } },
    paragraphStyles: [
      { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { font: FONT, size: 36, bold: true, color: C.ink },
        paragraph: { spacing: { before: 240, after: 160 }, keepNext: true, outlineLevel: 0,
          border: { bottom: { style: BorderStyle.SINGLE, size: 8, color: C.violet, space: 4 } } } },
      { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { font: FONT, size: 28, bold: true, color: C.violet },
        paragraph: { spacing: { before: 240, after: 120 }, keepNext: true, outlineLevel: 1 } },
      { id: "Heading3", name: "Heading 3", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { font: FONT, size: 24, bold: true, color: C.cyan },
        paragraph: { spacing: { before: 200, after: 80 }, keepNext: true, outlineLevel: 2 } },
    ],
  },
  numbering: {
    config: [
      { reference: "steps", levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT,
        style: { paragraph: { indent: { left: 420, hanging: 300 } }, run: { bold: true, color: C.violet } } }] },
      { reference: "bullets", levels: [{ level: 0, format: LevelFormat.BULLET, text: "•", alignment: AlignmentType.LEFT,
        style: { paragraph: { indent: { left: 420, hanging: 260 } } } }] },
    ],
  },
  sections: [{
    properties: { page: { size: { width: PAGE_W, height: 15840 }, margin: { top: 1000, bottom: 1000, left: MARGIN, right: MARGIN } } },
    headers: { default: new Header({ children: [new Paragraph({ alignment: AlignmentType.RIGHT, children: [
      run("NEURALIZER · MÓDULO 2 — PROPUESTA PARA REVISIÓN", { size: 15, color: C.muted, bold: true })] })] }) },
    footers: { default: new Footer({ children: [new Paragraph({ alignment: AlignmentType.CENTER, children: [
      new TextRun({ children: ["Página ", PageNumber.CURRENT, " de ", PageNumber.TOTAL_PAGES], font: FONT, size: 17, color: C.muted })] })] }) },
    children,
  }],
});

Packer.toBuffer(doc).then(buf => {
  fs.writeFileSync(OUT, buf);
  console.log("OK", OUT, buf.length);
});
