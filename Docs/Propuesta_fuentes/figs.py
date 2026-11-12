# Genera las figuras del documento de revisión del Módulo 2 (Neuralizer).
# Reimplementa el motor difuso del juego (Mamdani, centroide con 40 muestras) para que
# cada gráfica muestre los valores reales que produce la app.
import os
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib import font_manager as fm
from matplotlib.patches import FancyBboxPatch, Circle, Rectangle
from matplotlib.colors import LinearSegmentedColormap

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "fig")
CAPS = r"A:\Proyectos\neuralizer\Docs\Capturas_Modulo2"
FONTS = r"A:\Proyectos\neuralizer\Assets\Fonts\Lexend"
os.makedirs(OUT, exist_ok=True)

for f in ("Lexend-Regular.ttf", "Lexend-SemiBold.ttf", "Lexend-Bold.ttf"):
    fm.fontManager.addfont(os.path.join(FONTS, f))
plt.rcParams.update({
    "font.family": ["Lexend", "DejaVu Sans"],
    "font.size": 11,
    "axes.edgecolor": "#C9CFDD",
    "axes.labelcolor": "#1C2440",
    "xtick.color": "#4A5578",
    "ytick.color": "#4A5578",
    "axes.titleweight": "semibold",
    "axes.titlesize": 13,
    "axes.titlecolor": "#1C2440",
    "axes.spines.top": False,
    "axes.spines.right": False,
    "savefig.dpi": 200,
    "savefig.bbox": "tight",
    "savefig.facecolor": "white",
})

# Paleta (versión para impresión de los tokens del juego)
INK = "#1C2440"
MUTED = "#6B7594"
CYAN = "#0E9BD6"
VIOLET = "#7B3FE4"
AMBER = "#E8A20C"
GREEN = "#1FAF6E"
RED = "#E5455F"
GRID = "#EEF1F7"
CASE_COLORS = [CYAN, VIOLET, GREEN, AMBER, RED]


# --------------------------------------------------------------------- motor
def trap(a, b, c, d):
    """Curva para DIBUJAR (hombros llenos hasta el borde del rango)."""
    def f(x):
        x = np.asarray(x, dtype=float)
        y = np.zeros_like(x)
        up = (x > a) & (x < b)
        y[up] = (x[up] - a) / (b - a)
        y[(x >= b) & (x <= c)] = 1
        dn = (x > c) & (x < d)
        y[dn] = (d - x[dn]) / (d - c)
        if a == b:
            y[(x >= a) & (x <= c)] = 1
        if c == d:
            y[(x >= b) & (x <= d)] = 1
        return y

    def exact(x):
        """Copia exacta de MembershipFunction.EvaluateTrapezoidal del juego (0 en x <= a o x >= d)."""
        if x <= a or x >= d:
            return 0.0
        if x < b:
            return (x - a) / (b - a) if b > a else 1.0
        if x <= c:
            return 1.0
        return (d - x) / (d - c) if d > c else 1.0

    f.exact = exact
    return f


def tri(a, p, d):
    return trap(a, p, p, d)


def sig(c, s):
    return lambda x: 1 / (1 + np.exp(-s * (np.asarray(x, float) - c)))


def sig_inv(c, s):
    return lambda x: 1 - 1 / (1 + np.exp(-s * (np.asarray(x, float) - c)))


def gauss(c, s):
    return lambda x: np.exp(-0.5 * ((np.asarray(x, float) - c) / s) ** 2)


def mu(f, x):
    if hasattr(f, "exact"):
        return float(f.exact(float(x)))
    return float(f(np.array([x]))[0])


def evaluate(inputs, outputs, rules, values, out_range=(0, 100)):
    """inputs: list[dict label->fn]; outputs: dict label->fn; rules: list of (conds, out_label, weight)."""
    xs = np.linspace(out_range[0], out_range[1], 40)
    agg = np.zeros_like(xs)
    for conds, out_label, w in rules:
        s = min(mu(inputs[i][lab], values[i]) for i, lab in conds) * w
        if s <= 0:
            continue
        out_f = outputs[out_label]
        curve = np.array([mu(out_f, x) for x in xs])
        agg = np.maximum(agg, np.minimum(curve, s))
    if agg.sum() <= 0:
        return (out_range[0] + out_range[1]) / 2
    return float((xs * agg).sum() / agg.sum())


# ------------------------------------------------------------------ helpers
def save(fig, name):
    path = os.path.join(OUT, name)
    fig.savefig(path)
    plt.close(fig)
    print("ok", name)


def style_axes(ax, xlabel=None, ylabel="Grado de pertenencia (μ)"):
    ax.set_facecolor("white")
    ax.grid(True, color=GRID, linewidth=1)
    ax.set_axisbelow(True)
    if xlabel:
        ax.set_xlabel(xlabel)
    if ylabel:
        ax.set_ylabel(ylabel)


def plot_variable(ax, labels, rng, title, cases=None, n=600, show_legend=True):
    xs = np.linspace(rng[0], rng[1], n)
    for (name, f, color) in labels:
        ys = f(xs)
        ax.fill_between(xs, ys, color=color, alpha=0.16, linewidth=0)
        ax.plot(xs, ys, color=color, linewidth=2.4, label=name)
    if cases:
        for i, (cname, x) in enumerate(cases):
            col = CASE_COLORS[i % len(CASE_COLORS)]
            ax.axvline(x, color=col, linestyle=(0, (4, 3)), linewidth=1.6)
            ax.text(x, 1.10 + 0.09 * (i % 2), cname, color=col, ha="center", va="bottom",
                    fontsize=9.5, fontweight="semibold")
    ax.set_xlim(rng)
    ax.set_ylim(-0.03, 1.32 if cases else 1.12)
    ax.set_yticks([0, 0.5, 1])
    ax.set_title(title, loc="left", pad=10)
    if show_legend:
        leg = ax.legend(loc="center right", frameon=True, fontsize=9.5, framealpha=0.95)
        leg.get_frame().set_edgecolor("#DDE2EC")


def bar_compare(ax, names, before, after, title, ylabel, before_label="Al inicio", after_label="Resuelto",
                ylim=(0, 100), fmt="{:.1f}"):
    x = np.arange(len(names))
    w = 0.36
    b1 = ax.bar(x - w / 2, before, w, color="#C9CFDD", label=before_label)
    b2 = ax.bar(x + w / 2, after, w, color=VIOLET, label=after_label)
    for bars in (b1, b2):
        for bar in bars:
            h = bar.get_height()
            ax.text(bar.get_x() + bar.get_width() / 2, h + 1.5, fmt.format(h), ha="center", va="bottom",
                    fontsize=9, color=INK)
    ax.set_xticks(x, names)
    ax.set_ylim(*ylim)
    ax.set_ylabel(ylabel)
    ax.set_title(title, loc="left", pad=10)
    ax.legend(frameon=False, fontsize=9, loc="upper right")
    ax.grid(True, axis="y", color=GRID)
    ax.set_axisbelow(True)


def box(ax, x, y, w, h, title, body, color, fs_title=12, fs_body=9.5):
    ax.add_patch(FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.02,rounding_size=0.06",
                                linewidth=2, edgecolor=color, facecolor="white"))
    ax.add_patch(Rectangle((x, y + h - 0.05), w, 0.05, color=color, linewidth=0))
    ax.text(x + w / 2, y + h - 0.2, title, ha="center", va="center", fontsize=fs_title,
            fontweight="semibold", color=INK)
    ax.text(x + w / 2, y + h / 2 - 0.12, body, ha="center", va="center", fontsize=fs_body,
            color=MUTED, linespacing=1.4)


def arrow(ax, x1, y1, x2, y2, color=MUTED):
    ax.annotate("", xy=(x2, y2), xytext=(x1, y1),
                arrowprops=dict(arrowstyle="-|>", color=color, lw=2, mutation_scale=16))


# ------------------------------------------------------------ figuras base
def fig_progression():
    fig, ax = plt.subplots(figsize=(10, 2.9))
    ax.set_xlim(0, 10)
    ax.set_ylim(0, 2.6)
    ax.axis("off")
    steps = [
        ("1 · Percibir", "Entrenamiento 02\nLectura del Daño", CYAN),
        ("2 · Decidir", "Entrenamiento 03\nTriage Rápido", CYAN),
        ("3 · Ajustar una regla", "Entrenamiento 01\nEl Dilema del Healer", CYAN),
        ("4 · Diseñar el sistema", "Niveles 01 – 04\ncurvas, reglas, superficie", VIOLET),
    ]
    w, gap = 2.1, 0.43
    for i, (t, b, c) in enumerate(steps):
        x = 0.1 + i * (w + gap)
        box(ax, x, 0.35, w, 1.85, t, b, c)
        if i < len(steps) - 1:
            arrow(ax, x + w + 0.05, 1.27, x + w + gap - 0.05, 1.27)
    ax.text(5, 0.08, "De sentir el grado de pertenencia  →  a construir un sistema difuso completo",
            ha="center", fontsize=10, color=MUTED, style="italic")
    save(fig, "progresion.png")


def fig_pipeline():
    fig, ax = plt.subplots(figsize=(10, 3.2))
    ax.set_xlim(0, 10)
    ax.set_ylim(0, 3)
    ax.axis("off")
    steps = [
        ("Fuzzificar", "Cada dato se convierte\nen grados μ (0–1)\nHP 40 → Bajo 0.6", CYAN),
        ("Reglas", "SI … Y … ENTONCES …\nY = mínimo · O = máximo\n× peso de la regla", VIOLET),
        ("Agregar", "Cada regla recorta su\ncurva de salida; se\nsuperponen (máximo)", VIOLET),
        ("Defuzzificar", "Centroide de la figura\n= un solo número\n(p. ej. Prioridad 87.9)", AMBER),
    ]
    w, gap = 2.1, 0.43
    for i, (t, b, c) in enumerate(steps):
        x = 0.1 + i * (w + gap)
        box(ax, x, 0.35, w, 2.3, t, b, c)
        if i < len(steps) - 1:
            arrow(ax, x + w + 0.05, 1.5, x + w + gap - 0.05, 1.5)
    ax.text(0.1, 0.05, "Datos del caso", fontsize=9.5, color=MUTED)
    ax.text(9.9, 0.05, "Decisión", fontsize=9.5, color=MUTED, ha="right")
    save(fig, "pipeline.png")


def fig_anatomy():
    fig, ax = plt.subplots(figsize=(8.5, 3.6))
    f = trap(20, 40, 60, 85)
    xs = np.linspace(0, 100, 600)
    ax.fill_between(xs, f(xs), color=CYAN, alpha=0.15)
    ax.plot(xs, f(xs), color=CYAN, linewidth=2.8)
    for name, x, y in (("a", 20, 0), ("b", 40, 1), ("c", 60, 1), ("d", 85, 0)):
        ax.plot([x], [y], "o", color=INK, markersize=7, zorder=5)
        ax.text(x, y + (0.08 if y else -0.16), name, ha="center", fontsize=13, fontweight="semibold", color=INK)
    ax.annotate("empieza a subir", xy=(20, 0), xytext=(4, 0.32), fontsize=9.5, color=MUTED,
                arrowprops=dict(arrowstyle="-", color=MUTED))
    ax.annotate("meseta: pertenencia total", xy=(50, 1), xytext=(50, 1.22), ha="center", fontsize=9.5, color=MUTED)
    ax.annotate("vuelve a 0", xy=(85, 0), xytext=(90, 0.32), fontsize=9.5, color=MUTED,
                arrowprops=dict(arrowstyle="-", color=MUTED))
    x0 = 30
    y0 = mu(f, x0)
    ax.plot([x0, x0], [0, y0], color=AMBER, linewidth=2, linestyle=(0, (3, 2)))
    ax.plot([0, x0], [y0, y0], color=AMBER, linewidth=2, linestyle=(0, (3, 2)))
    ax.plot([x0], [y0], "o", color=AMBER, markersize=9, zorder=6)
    ax.text(x0 + 2, y0 - 0.12, f"valor = {x0}  →  μ = {y0:.2f}", color=AMBER, fontsize=10.5, fontweight="semibold")
    style_axes(ax, "Valor de la variable")
    ax.set_xlim(0, 100)
    ax.set_ylim(-0.25, 1.38)
    ax.set_yticks([0, 0.5, 1])
    ax.set_title("Anatomía de una función de pertenencia trapezoidal", loc="left", pad=10)
    save(fig, "anatomia.png")


def fig_shapes():
    fig, axes = plt.subplots(1, 4, figsize=(11, 2.5), sharey=True)
    shapes = [
        ("Trapezoidal (a, b, c, d)", trap(15, 35, 65, 85)),
        ("Triangular (a, pico, d)", tri(20, 50, 80)),
        ("Sigmoide (centro, pendiente)", sig(50, 0.15)),
        ("Gaussiana (centro, ancho)", gauss(50, 15)),
    ]
    xs = np.linspace(0, 100, 400)
    for ax, (t, f) in zip(axes, shapes):
        ax.fill_between(xs, f(xs), color=VIOLET, alpha=0.14)
        ax.plot(xs, f(xs), color=VIOLET, linewidth=2.2)
        ax.set_title(t, fontsize=10, loc="left")
        ax.set_ylim(-0.03, 1.08)
        ax.set_yticks([0, 1])
        ax.set_xticks([0, 50, 100])
        ax.grid(True, color=GRID)
    save(fig, "formas.png")


# ---------------------------------------------------------- Entrenamiento 01
HP = {"Bajo": trap(0, 0, 30, 55), "Alto": trap(35, 70, 100, 100)}
DANO = {"Bajo": trap(0, 0, 30, 50), "Alto": trap(30, 70, 100, 100)}
PRIO3 = {"Baja": trap(0, 0, 25, 50), "Alta": tri(50, 75, 100), "MuyAlta": trap(75, 90, 100, 100)}
RULES_HEALER = lambda w: [([(0, "Bajo"), (1, "Alto")], "MuyAlta", w), ([(0, "Bajo"), (1, "Bajo")], "Alta", 1),
                          ([(0, "Alto")], "Baja", 1)]


def fig_healer():
    fig, axes = plt.subplots(1, 2, figsize=(11, 3.4))
    plot_variable(axes[0], [("Bajo", HP["Bajo"], CYAN), ("Alto", HP["Alto"], RED)], (0, 100),
                  "HP (vida restante)", cases=[("Reinhardt 18", 18), ("D.Va 16", 16)])
    style_axes(axes[0], "HP")
    plot_variable(axes[1], [("Bajo", DANO["Bajo"], CYAN), ("Alto", DANO["Alto"], RED)], (0, 100),
                  "DañoEntrante", cases=[("Reinhardt 20", 20), ("D.Va 85", 85)])
    style_axes(axes[1], "DañoEntrante", ylabel=None)
    save(fig, "ej1_curvas.png")

    weights = np.linspace(0, 1, 41)
    r = [evaluate([HP, DANO], PRIO3, RULES_HEALER(w), [18, 20]) for w in weights]
    d = [evaluate([HP, DANO], PRIO3, RULES_HEALER(w), [16, 85]) for w in weights]
    fig, ax = plt.subplots(figsize=(8.5, 3.4))
    ax.plot(weights, d, color=VIOLET, linewidth=2.8, label="D.Va")
    ax.plot(weights, r, color=CYAN, linewidth=2.8, label="Reinhardt")
    ax.axvspan(0, 0.012, color=RED, alpha=0.12)
    ax.annotate("Peso 0: la regla está apagada.\nD.Va no activa ninguna regla → 50 (punto medio)",
                xy=(0.0, 50), xytext=(0.12, 56), fontsize=9.5, color=RED,
                arrowprops=dict(arrowstyle="->", color=RED))
    ax.annotate("Con cualquier peso > 0 D.Va salta a ≈ 87–90\ny pasa a ser la prioridad (correcto)",
                xy=(0.3, 87.9), xytext=(0.42, 64), fontsize=9.5, color=VIOLET,
                arrowprops=dict(arrowstyle="->", color=VIOLET))
    ax.set_xlabel("Peso de la regla «HP Bajo Y Daño Alto → MuyAlta»")
    ax.set_ylabel("Prioridad calculada")
    ax.set_ylim(40, 100)
    ax.legend(frameon=False, loc="lower right")
    ax.grid(True, color=GRID)
    ax.set_title("Prioridad de cada aliado según el peso de la regla 1", loc="left", pad=10)
    save(fig, "ej1_peso.png")


# ---------------------------------------------------------- Entrenamiento 02/03
CRIT = trap(50, 70, 100, 100)
URGENCY = LinearSegmentedColormap.from_list("urg", [GREEN, "#F2C230", RED])


def fig_damage():
    fig, ax = plt.subplots(figsize=(9, 3.5))
    xs = np.linspace(0, 100, 600)
    ys = CRIT(xs)
    for i in range(len(xs) - 1):
        ax.fill_between(xs[i:i + 2], ys[i:i + 2], color=URGENCY(ys[i]), alpha=0.85, linewidth=0)
    ax.plot(xs, ys, color=INK, linewidth=2)
    for x in (55, 60, 65):
        y = mu(CRIT, x)
        ax.plot([x], [y], "o", color=INK, zorder=5)
        ax.text(x - 1.5, y + 0.06, f"{x} → {y:.2f}", fontsize=9.5, ha="right", color=INK)
    ax.text(25, 0.12, "0–50: nada crítico\ncontorno verde", ha="center", fontsize=10, color=GREEN, fontweight="semibold")
    ax.text(86, 0.55, "≥ 70: crítico total\ncontorno rojo", ha="center", fontsize=10, color="white", fontweight="semibold")
    style_axes(ax, "Daño recibido (% de vida perdida) — lo que controla el slider", "μ_Crítico")
    ax.set_xlim(0, 100)
    ax.set_ylim(0, 1.15)
    ax.set_title("μ_Crítico(x) = trapezoidal(50, 70, 100, 100) y el color que ve el jugador", loc="left", pad=10)
    save(fig, "ej2_curva.png")

    fig, ax = plt.subplots(figsize=(9, 3.2))
    ax.plot(xs, ys, color=INK, linewidth=2)
    ax.axvspan(0, 50, color=GREEN, alpha=0.10)
    ax.axvspan(70, 100, color=RED, alpha=0.10)
    ax.text(25, 0.62, "Zona μ = 0\n(todo se ve verde)", ha="center", fontsize=9.5, color=GREEN)
    ax.text(85, 0.62, "Zona μ = 1\n(todo se ve rojo)", ha="center", fontsize=9.5, color=RED)
    pairs = [(57, 68, CYAN, "Reinhardt 57 → 0.35"), (68, 68, VIOLET, "Reaper 68 → 0.90")]
    for x, _, col, txt in pairs:
        y = mu(CRIT, x)
        ax.plot([x], [y], "o", color=col, markersize=10, zorder=5)
        ax.text(x - 1.5, y + 0.07, txt, color=col, fontsize=9.5, ha="right", fontweight="semibold")
    style_axes(ax, "Daño sorteado para cada personaje", "μ_Crítico")
    ax.set_xlim(0, 100)
    ax.set_ylim(0, 1.15)
    ax.set_title("Triage: se sortean dos daños cuyos μ difieren ≥ 0.3 (siempre hay un «peor» visible)",
                 loc="left", pad=10, fontsize=11.5)
    save(fig, "ej3_curva.png")


# ---------------------------------------------------------------- Niveles
def fig_level1():
    rng = (0, 16000)
    init = [("Eco", trap(0, 0, 4000, 7000), CYAN), ("SemiBuy", trap(4000, 7000, 9000, 12000), VIOLET),
            ("FullBuy", trap(9000, 12000, 16000, 16000), RED)]
    sol = [("Eco", trap(0, 0, 2000, 5000), CYAN), ("SemiBuy", trap(2000, 5000, 7000, 10000), VIOLET),
           ("FullBuy", trap(7000, 10000, 16000, 16000), RED)]
    cases = [("Pistola", 1500), ("Bonus", 4200), ("Force", 6000), ("Ganada", 9500), ("Banco", 12000)]
    fig, axes = plt.subplots(2, 1, figsize=(10, 6.4), sharex=True)
    plot_variable(axes[0], init, rng, "Al inicio — el coach está mal calibrado (3/5 bien)", cases)
    style_axes(axes[0])
    plot_variable(axes[1], sol, rng, "Solución de referencia — curvas corridas a la izquierda (5/5)", cases)
    style_axes(axes[1], "Dinero del equipo")
    note = dict(boxstyle="round,pad=0.35", fc="white", ec=RED, lw=1.2)
    axes[0].annotate("✗ Bonus cae en Eco (0.93)\n   debería ser SemiBuy", xy=(4200, 0.93), xytext=(1700, 0.38),
                     fontsize=9, color=RED, bbox=note, arrowprops=dict(arrowstyle="->", color=RED))
    axes[0].annotate("✗ Ganada cae en SemiBuy (0.83)\n   debería ser FullBuy", xy=(9500, 0.83), xytext=(6900, 0.38),
                     fontsize=9, color=RED, bbox=note, arrowprops=dict(arrowstyle="->", color=RED))
    fig.tight_layout()
    save(fig, "n1_curvas.png")


GANK_OUT = {"Baja": trap(0, 0, 25, 50), "Media": tri(25, 50, 75), "Alta": trap(50, 75, 100, 100)}
GANK_RULES = [([(0, "Perdiendo")], "Alta", 1), ([(0, "Parejo")], "Media", 1), ([(0, "Ganando")], "Baja", 1)]


def fig_level2():
    rng = (-3000, 3000)
    init = {"Perdiendo": trap(-3000, -3000, -2800, -2400), "Parejo": trap(-2800, -2000, 2000, 2800),
            "Ganando": trap(2400, 2800, 3000, 3000)}
    sol = {"Perdiendo": trap(-3000, -3000, -1500, -300), "Parejo": trap(-1500, -300, 300, 1500),
           "Ganando": trap(300, 1500, 3000, 3000)}
    cols = {"Perdiendo": RED, "Parejo": VIOLET, "Ganando": CYAN}
    cases = [("Top −1800", -1800), ("Mid 0", 0), ("Bot +1200", 1200)]
    fig, axes = plt.subplots(2, 1, figsize=(10, 6.2), sharex=True)
    plot_variable(axes[0], [(k, v, cols[k]) for k, v in init.items()], rng,
                  "Al inicio — «Parejo» es tan ancho que los tres carriles le parecen iguales", cases)
    style_axes(axes[0])
    plot_variable(axes[1], [(k, v, cols[k]) for k, v in sol.items()], rng,
                  "Solución de referencia — Parejo angosto, Perdiendo y Ganando más anchos", cases)
    style_axes(axes[1], "DesventajaDeOro (negativo = la línea va perdiendo)")
    fig.tight_layout()
    save(fig, "n2_curvas.png")

    names = ["Top", "Mid", "Bot"]
    vals = [-1800, 0, 1200]
    before = [evaluate([init], GANK_OUT, GANK_RULES, [v]) for v in vals]
    after = [evaluate([sol], GANK_OUT, GANK_RULES, [v]) for v in vals]
    fig, ax = plt.subplots(figsize=(7.5, 3.2))
    bar_compare(ax, names, before, after, "PrioridadGank de cada carril", "Prioridad (0–100)")
    ax.text(0.02, 0.92, "Inicio: triple empate en 50 → no se acepta", transform=ax.transAxes, fontsize=9, color=RED)
    save(fig, "n2_barras.png")


PRIO4 = {"Baja": trap(0, 0, 25, 50), "Media": tri(25, 50, 75), "Alta": tri(50, 75, 100),
         "MuyAlta": trap(75, 90, 100, 100)}


def fig_level3():
    before = [evaluate([HP, DANO], PRIO4, RULES_HEALER(0), v) for v in ([18, 20], [16, 85])]
    after = [evaluate([HP, DANO], PRIO4, RULES_HEALER(1), v) for v in ([18, 20], [16, 85])]
    fig, ax = plt.subplots(figsize=(7.5, 3.2))
    bar_compare(ax, ["Reinhardt (debe ir 2º)", "D.Va (debe ir 1º)"], before, after,
                "Prioridad: regla 1 apagada vs. encendida", "Prioridad (0–100)",
                before_label="Peso regla 1 = 0", after_label="Peso regla 1 = 1")
    save(fig, "n3_barras.png")


VIDA = {"Baja": trap(0, 0, 30, 50), "Alta": trap(30, 70, 100, 100)}
AMEN = {"Baja": trap(0, 0, 30, 50), "Alta": trap(30, 70, 100, 100)}
RULES_L4 = lambda w1: [([(0, "Baja"), (1, "Alta")], "MuyAlta", w1), ([(0, "Baja"), (1, "Baja")], "Media", 1),
                       ([(0, "Alta"), (1, "Alta")], "Alta", 1), ([(0, "Alta"), (1, "Baja")], "Baja", 1)]
L4_CASES = [("Tanque", 80, 70), ("Support", 35, 20), ("Tirador", 25, 85)]


def fig_level4():
    fig, axes = plt.subplots(1, 2, figsize=(11, 3.4))
    plot_variable(axes[0], [("Baja", VIDA["Baja"], RED), ("Alta", VIDA["Alta"], CYAN)], (0, 100),
                  "VidaRestante", cases=[(n, v) for n, v, _ in L4_CASES])
    style_axes(axes[0], "VidaRestante")
    plot_variable(axes[1], [("Baja", AMEN["Baja"], CYAN), ("Alta", AMEN["Alta"], RED)], (0, 100),
                  "Amenaza", cases=[(n, a) for n, _, a in L4_CASES])
    style_axes(axes[1], "Amenaza", ylabel=None)
    save(fig, "n4_curvas.png")

    before = [evaluate([VIDA, AMEN], PRIO4, RULES_L4(0), [v, a]) for _, v, a in L4_CASES]
    after = [evaluate([VIDA, AMEN], PRIO4, RULES_L4(1), [v, a]) for _, v, a in L4_CASES]
    fig, ax = plt.subplots(figsize=(7.5, 3.2))
    bar_compare(ax, [f"{n} (debe ir {o})" for (n, _, _), o in zip(L4_CASES, ["2º", "3º", "1º"])], before, after,
                "PrioridadObjetivo: regla 1 apagada vs. encendida", "Prioridad (0–100)",
                before_label="Peso regla 1 = 0", after_label="Peso regla 1 = 1")
    save(fig, "n4_barras.png")

    # Superficie de control antes / después
    res = 41
    grid = np.linspace(0, 100, res)
    cmap = LinearSegmentedColormap.from_list("surf", ["#2E86C9", VIOLET, RED])
    fig, axes = plt.subplots(1, 2, figsize=(10.5, 4.3))
    for ax, w, title in ((axes[0], 0, "Regla 1 apagada (inicio)"), (axes[1], 1, "Regla 1 encendida (solución)")):
        z = np.array([[evaluate([VIDA, AMEN], PRIO4, RULES_L4(w), [vx, ay]) for vx in grid] for ay in grid])
        im = ax.imshow(z, origin="lower", extent=(0, 100, 0, 100), cmap=cmap, vmin=0, vmax=100, aspect="auto")
        for i, (n, v, a) in enumerate(L4_CASES):
            ax.plot(v, a, "o", markersize=10, markerfacecolor="white", markeredgecolor=INK, markeredgewidth=2)
            ax.text(v + 3, a + 3, n, fontsize=9.5, color="white", fontweight="semibold")
        ax.set_title(title, loc="left", pad=8)
        ax.set_xlabel("VidaRestante")
        ax.spines[:].set_visible(False)
    axes[0].set_ylabel("Amenaza")
    axes[1].add_patch(Rectangle((0, 50), 45, 50, fill=False, edgecolor=AMBER, linewidth=2.5, linestyle=(0, (5, 3))))
    axes[1].text(2, 52, "poca vida + mucha amenaza\n→ ahora es prioridad máxima", fontsize=8.5, color="white")
    cbar = fig.colorbar(im, ax=axes, shrink=0.85, pad=0.02)
    cbar.set_label("Prioridad")
    save(fig, "n4_superficie.png")


# ------------------------------------------------------ capturas anotadas
def annotate_capture(src, dst, callouts, crop=None):
    img = plt.imread(os.path.join(CAPS, src))
    if crop:
        y0, y1 = crop
        img = img[y0:y1]
    h, w = img.shape[:2]
    fig = plt.figure(figsize=(w / 120, h / 120))
    ax = fig.add_axes([0, 0, 1, 1])
    ax.imshow(img)
    ax.axis("off")
    for n, (x, y) in enumerate(callouts, start=1):
        if crop:
            y = y - crop[0]
        ax.add_patch(Circle((x, y), 15, color=AMBER, zorder=5))
        ax.add_patch(Circle((x, y), 15, fill=False, edgecolor="white", linewidth=2, zorder=6))
        ax.text(x, y, str(n), ha="center", va="center", fontsize=11, fontweight="bold", color=INK, zorder=7)
    fig.savefig(os.path.join(OUT, dst), dpi=120, bbox_inches="tight", pad_inches=0)
    plt.close(fig)
    print("ok", dst)


def captures():
    annotate_capture("menu_modulo2.png", "cap_menu.png", [
        (238, 24),   # pestañas de módulo
        (40, 150),   # logotipo
        (500, 300),  # encabezado + progreso
        (40, 470),   # sección
        (505, 470),  # tarjeta de ejercicio
    ])
    annotate_capture("nivel4.png", "cap_nivel.png", [
        (470, 36),   # título
        (510, 100),  # narrativa
        (510, 180),  # tarjetas de casos
        (500, 345),  # pestañas de vista
        (510, 500),  # gráfica
        (500, 690),  # controles
        (40, 874),   # orden en vivo
        (500, 922),  # evaluar
    ])
    for name in ("nivel1.png", "nivel2.png", "nivel3.png", "nivel_resultado.png", "ej3_triage.png"):
        img = plt.imread(os.path.join(CAPS, name))
        plt.imsave(os.path.join(OUT, "cap_" + name), img)
        print("ok cap_" + name)


if __name__ == "__main__":
    fig_progression()
    fig_pipeline()
    fig_anatomy()
    fig_shapes()
    fig_healer()
    fig_damage()
    fig_level1()
    fig_level2()
    fig_level3()
    fig_level4()
    captures()
