using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neuralizer.UI {

    /// <summary>
    /// Kit de construcción de UI reutilizable para pantallas con la estética e-sports (ver
    /// <see cref="EsportsTheme"/>): tarjetas con borde neón y resplandor, botones y sliders con
    /// degradado, texto con degradado. Nació con el Módulo 2 pero no depende de nada específico de
    /// lógica difusa — cualquier módulo futuro con esta temática puede llamarlo directo.
    /// </summary>
    /// <remarks>
    /// Todo el "resplandor" (glow) se simula con 2-3 imágenes extra de baja opacidad detrás del
    /// borde, sin shaders ni materiales propios — barato para WebGL. Es una base deliberadamente
    /// simple: el día que haya arte propio (sprites de borde, texturas de glow reales), estos
    /// métodos son el único lugar que hay que tocar para que todas las pantallas se actualicen a la vez.
    /// </remarks>
    public static class EsportsUIKit {

        // ---------------------------------------------------------------
        // Primitivas genéricas
        // ---------------------------------------------------------------

        public static RectTransform CreateRect(string name, Transform parent) {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static void Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f) {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static Image CreateImage(string name, Transform parent, Color color) {
            RectTransform rt = CreateRect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        /// <summary>Imagen a partir de un <see cref="Sprite"/> con arte propio (en vez de color sólido) — para piezas como las del mockup del Módulo 2. No estira al padre por sí sola: configurar el RectTransform devuelto después de llamar.</summary>
        public static Image CreateSpriteImage(string name, Transform parent, Sprite sprite, Image.Type type = Image.Type.Simple) {
            RectTransform rt = CreateRect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = type;
            img.preserveAspect = false;
            return img;
        }

        /// <summary>
        /// Texto genérico pensado para vivir directo dentro de un VerticalLayoutGroup.
        /// <paramref name="size"/> debería ser un token de <see cref="EsportsTheme.TextSize"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="preferredHeight"/> se aplica como ALTURA MÍNIMA, no fija: si el texto
        /// necesita más líneas, el layout le da el alto que pide (preferred height de TMP) en vez de
        /// dejarlo desbordarse encima del elemento de abajo — esa era la causa de los textos encimados.
        /// </remarks>
        public static TextMeshProUGUI CreateText(Transform parent, string text, float size, Color color,
            FontStyles style, TextAlignmentOptions alignment, float preferredHeight) {
            RectTransform rt = CreateRect("Text", parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            EsportsSkin.ApplyFont(tmp, EsportsSkin.InferRole(size, style));
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            if (preferredHeight > 0f) {
                rt.gameObject.AddComponent<LayoutElement>().minHeight = preferredHeight;
            }
            return tmp;
        }

        /// <summary>
        /// Texto que se estira para llenar por completo su padre (p. ej. el Content de una tarjeta).
        /// Como el contenedor tiene tamaño fijo, el texto se autoajusta para no desbordarse: arranca en
        /// <paramref name="size"/> y solo se achica hasta <see cref="EsportsTheme.TextSize.Caption"/>
        /// (12 px reales) — nunca a un tamaño ilegible.
        /// </summary>
        public static TextMeshProUGUI CreateTextFill(Transform parent, string text, float size, Color color,
            FontStyles style, TextAlignmentOptions alignment) {
            RectTransform rt = CreateRect("Text", parent);
            Stretch(rt);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            EsportsSkin.ApplyFont(tmp, EsportsSkin.InferRole(size, style));
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = size;
            tmp.fontSizeMin = Mathf.Min(size, EsportsTheme.TextSize.Caption);
            return tmp;
        }

        /// <summary>Texto con degradado nativo de TMP (izquierda→derecha) — pensado para títulos.</summary>
        public static TextMeshProUGUI CreateGradientText(Transform parent, string text, float size,
            FontStyles style, TextAlignmentOptions alignment, float preferredHeight, Color colorLeft, Color colorRight) {
            RectTransform rt = CreateRect("GradientText", parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.color = Color.white;
            tmp.enableVertexGradient = true;
            tmp.colorGradient = new VertexGradient(colorLeft, colorRight, colorLeft, colorRight);
            EsportsSkin.ApplyFont(tmp, TypeRole.Display);
            tmp.raycastTarget = false;
            if (preferredHeight > 0f) {
                rt.gameObject.AddComponent<LayoutElement>().minHeight = preferredHeight;
            }
            return tmp;
        }

        public static UIPolyline CreatePolyline(Transform parent, Color color, float thickness) {
            RectTransform rt = CreateRect("Line", parent);
            Stretch(rt);
            rt.pivot = Vector2.zero;
            rt.gameObject.AddComponent<CanvasRenderer>();
            UIPolyline line = rt.gameObject.AddComponent<UIPolyline>();
            line.color = color;
            line.Thickness = thickness;
            return line;
        }

        /// <summary>Relleno de área bajo una curva (ver <see cref="UIFilledCurve"/>) — se usa detrás de un <see cref="UIPolyline"/> del mismo color para dar más claridad visual a una función de pertenencia.</summary>
        public static UIFilledCurve CreateFilledCurve(Transform parent, Color color) {
            RectTransform rt = CreateRect("Fill", parent);
            Stretch(rt);
            rt.pivot = Vector2.zero;
            rt.gameObject.AddComponent<CanvasRenderer>();
            UIFilledCurve fill = rt.gameObject.AddComponent<UIFilledCurve>();
            fill.color = color;
            return fill;
        }

        /// <summary>
        /// Punto luminoso (usado para marcar, ej., el cruce entre dos curvas): un centro sólido con
        /// 3 halos concéntricos de baja opacidad simulando resplandor, sin shaders.
        /// </summary>
        public static RectTransform CreateGlowDot(Transform parent, Color color, float size = 14f, float glowSpread = 16f) {
            RectTransform root = CreateRect("GlowDot", parent);
            // Explícito a propósito: un RectTransform recién creado por script ancla al CENTRO del
            // padre por default en este proyecto, no a la esquina inferior izquierda. Quien llama a
            // CreateGlowDot siempre después hace `dot.anchoredPosition = punto` esperando que ese punto
            // esté en el mismo espacio 0..width/0..height que usa el resto de la gráfica (curvas, grilla,
            // etc.) — sin fijar el ancla acá, el punto terminaba desplazado medio ancho/alto de más
            // (el bug real detrás de "el punto amarillo aparece flotando en un lugar que no es").
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.zero;
            root.sizeDelta = new Vector2(size, size);

            // Con el sprite radial del generador los halos son luz suave y redonda; sin él se cae a los
            // cuadrados semitransparentes de antes. La estructura (3 halos + núcleo) se mantiene porque
            // los demos tiñen cada capa conservando su alpha relativo.
            Sprite radial = EsportsSkin.GlowDot;
            for (int i = 3; i >= 1; i--) {
                float spread = glowSpread * i / 3f;
                float alpha = 0.16f * i;
                Image halo = CreateImage($"Glow{i}", root, new Color(color.r, color.g, color.b, alpha));
                halo.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                halo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                // El sprite radial se desvanece hacia el borde: se agranda para que el halo llegue igual de lejos.
                float haloSize = radial ? size + spread * 2.4f : size + spread;
                halo.rectTransform.sizeDelta = new Vector2(haloSize, haloSize);
                halo.rectTransform.anchoredPosition = Vector2.zero;
                halo.raycastTarget = false;
                if (radial) halo.sprite = radial;
            }

            Image core = CreateImage("Core", root, color);
            Stretch(core.rectTransform);
            core.raycastTarget = false;
            if (radial) {
                core.sprite = radial;
                // El sprite radial es suave: el núcleo un poco más grande para que el centro se lea sólido.
                EsportsUIKit.Stretch(core.rectTransform, -size * 0.35f, -size * 0.35f, -size * 0.35f, -size * 0.35f);
            }
            return root;
        }

        /// <summary>
        /// Marco rectangular SIN relleno (4 barras delgadas independientes, una por lado) directo
        /// sobre <paramref name="root"/> — a diferencia de <see cref="CreateNeonCard"/> (que sí
        /// necesita un relleno opaco encima del borde para que el hairline se note), esto deja el
        /// centro totalmente vacío para que se vea lo que sea que haya detrás (ej. el panel
        /// semitransparente de <see cref="CreateFramedCard"/>).
        /// </summary>
        public static void CreateOutline(RectTransform root, Color color, float thickness) {
            CreateOutline(root, color, thickness, out _);
        }

        /// <summary>Misma idea que <see cref="CreateOutline(RectTransform,Color,float)"/> pero devuelve las 4 barras — para cuando hace falta cambiarles el color después (ej. un aro que pulsa).</summary>
        public static void CreateOutline(RectTransform root, Color color, float thickness, out Image[] bars) {
            // CS1628: un parámetro "out" no se puede usar/capturar dentro de una local function — por
            // eso se arma en una lista local normal y se asigna a "bars" recién al final.
            List<Image> list = new(4);

            Image Bar(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta) {
                RectTransform rt = CreateRect(name, root);
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = pivot;
                rt.sizeDelta = sizeDelta;
                rt.anchoredPosition = Vector2.zero;
                Image img = rt.gameObject.AddComponent<Image>();
                img.color = color;
                img.raycastTarget = false;
                return img;
            }

            list.Add(Bar("OutlineTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, thickness)));
            list.Add(Bar("OutlineBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness)));
            list.Add(Bar("OutlineLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(thickness, 0f)));
            list.Add(Bar("OutlineRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(thickness, 0f)));

            bars = list.ToArray();
        }

        public static UIGradient AddGradient(Graphic graphic, Color colorA, Color colorB,
            UIGradient.Direction direction = UIGradient.Direction.Diagonal) {
            UIGradient grad = graphic.gameObject.AddComponent<UIGradient>();
            grad.ColorA = colorA;
            grad.ColorB = colorB;
            grad.GradientDirection = direction;
            return grad;
        }

        // ---------------------------------------------------------------
        // Piezas de estilo e-sports
        // ---------------------------------------------------------------

        /// <summary>
        /// Tarjeta oscura con un borde muy sutil (apenas un hairline con tinte azul→morado) — separa el
        /// panel del fondo por contraste, no por un marco brillante. Devuelve el RectTransform de
        /// contenido, ya con padding interno.
        /// </summary>
        /// <remarks>
        /// Antes tenía además un halo de resplandor (3 capas) y un borde en degradado a opacidad total —
        /// eso es justo lo que se marcó como "se ve anticuado" en la revisión visual, así que se quitó.
        /// <paramref name="glowSpread"/> queda en la firma sin usarse para no tener que tocar cada call
        /// site que ya lo pasaba.
        /// </remarks>
        public static RectTransform CreateNeonCard(Transform parent, string name, float borderThickness = 1f, float glowSpread = 0f) {
            RectTransform content = CreatePanel(parent, name, EsportsTheme.NeonBlend, 0.35f, borderThickness, out _);
            Stretch(content, 16f, 16f, 14f, 14f);
            return content;
        }

        /// <summary>Como <see cref="CreateNeonCard"/> pero con un color de acento en vez del degradado (p. ej. para diferenciar dos equipos/opciones) — mismo hairline sutil, sin halo.</summary>
        public static RectTransform CreateAccentCard(Transform parent, string name, Color accent, out Image border,
            float borderThickness = 1f, float glowSpread = 0f) {
            RectTransform content = CreatePanel(parent, name, accent, 0.45f, borderThickness, out border);
            Stretch(content, 14f, 14f, 12f, 12f);
            return content;
        }

        /// <summary>
        /// Panel base del sistema "Tactical Neon": relleno oscuro con esquinas recortadas y contorno
        /// fino del color de acento ENCIMA del relleno (con los sprites del generador). Sin los sprites
        /// cae al rectángulo + hairline anterior. Mantiene la jerarquía Border/Bg/Content de siempre
        /// porque otras herramientas buscan esos nombres. Devuelve "Content".
        /// </summary>
        private static RectTransform CreatePanel(Transform parent, string name, Color accent, float borderAlpha,
            float borderThickness, out Image border) {
            RectTransform root = CreateRect(name, parent);
            bool skinned = EsportsSkin.HasSprites;

            border = skinned
                ? CreateSpriteImage("Border", root, EsportsSkin.ChamferLine, Image.Type.Sliced)
                : CreateImage("Border", root, Color.white);
            border.color = EsportsTheme.WithAlpha(accent, borderAlpha);
            Stretch(border.rectTransform);
            border.raycastTarget = false;

            Image bg = skinned
                ? CreateSpriteImage("Bg", root, EsportsSkin.ChamferFill, Image.Type.Sliced)
                : CreateImage("Bg", root, Color.white);
            bg.color = EsportsTheme.BgPanel;
            if (skinned) {
                Stretch(bg.rectTransform);
                // El contorno va encima del relleno (mismo contorno exterior: debajo quedaría tapado).
                border.transform.SetAsLastSibling();
            }
            else {
                Stretch(bg.rectTransform, borderThickness, borderThickness, borderThickness, borderThickness);
            }

            return CreateRect("Content", bg.rectTransform);
        }

        public static RectTransform CreateAccentCard(Transform parent, string name, Color accent,
            float borderThickness = 2f, float glowSpread = 8f) {
            return CreateAccentCard(parent, name, accent, out _, borderThickness, glowSpread);
        }

        /// <summary>
        /// Tarjeta con arte propio (Sliced 9-slice) en vez del panel sólido+hairline de
        /// <see cref="CreateNeonCard"/> — usada por las pantallas que ya tienen asset del mockup
        /// (ver Module2SpriteImporter para los bordes de 9-slice configurados por archivo). Devuelve
        /// el RectTransform de contenido, ya con el padding pedido.
        /// </summary>
        public static RectTransform CreateSpriteCard(Transform parent, string name, Sprite sprite,
            float paddingLeft, float paddingRight, float paddingTop, float paddingBottom) {
            RectTransform root = CreateRect(name, parent);
            Image bg = CreateSpriteImage("Bg", root, sprite, Image.Type.Sliced);
            Stretch(bg.rectTransform);

            RectTransform content = CreateRect("Content", root);
            Stretch(content, paddingLeft, paddingRight, paddingTop, paddingBottom);
            return content;
        }

        /// <summary>
        /// Como <see cref="CreateNeonCard"/> (panel de color sólido/semitransparente + contenido) pero
        /// con un marco decorativo de arte propio ENCIMA (borde redondeado, corchetes de esquina,
        /// muesca superior, franjas — ver <c>Module2_Card.png</c>) en vez del hairline genérico.
        /// </summary>
        /// <remarks>
        /// A propósito NO usa el sprite como único fondo (eso fue el primer intento y salió mal: el
        /// relleno pálido que trae el PNG original no es el color que se ve en el mockup — ahí el
        /// interior de la tarjeta deja ver el degradado del fondo de pantalla por detrás). El PNG de
        /// <paramref name="frameSprite"/> debe tener el interior recortado a transparente (solo las
        /// líneas/decoraciones opacas) para que esto funcione — ver Module2SpriteImporter/el pipeline
        /// de recorte en la conversación con el usuario. <paramref name="panelColor"/> normalmente
        /// lleva alpha &lt; 1 para que el fondo de pantalla se note debajo, como en el mockup.
        /// </remarks>
        public static RectTransform CreateFramedCard(Transform parent, string name, Color panelColor, Sprite frameSprite,
            float paddingLeft, float paddingRight, float paddingTop, float paddingBottom) {
            RectTransform root = CreateRect(name, parent);

            Image bg = CreateImage("Bg", root, panelColor);
            Stretch(bg.rectTransform);

            if (frameSprite) {
                Image frame = CreateSpriteImage("Frame", root, frameSprite, Image.Type.Sliced);
                Stretch(frame.rectTransform);
                frame.raycastTarget = false;
            }

            RectTransform content = CreateRect("Content", root);
            Stretch(content, paddingLeft, paddingRight, paddingTop, paddingBottom);
            return content;
        }

        /// <summary>Como <see cref="CreateGradientButton"/> pero con arte propio (Sliced) de fondo en vez del degradado genérico — p. ej. el botón "Evaluar" del mockup del Módulo 2.</summary>
        public static Button CreateSpriteButton(Transform parent, Sprite sprite, string label,
            float preferredHeight, Color textColor, float textSize = 24f) {
            RectTransform root = CreateRect("Button", parent);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;

            Image bg = CreateSpriteImage("Bg", root, sprite, Image.Type.Sliced);
            Stretch(bg.rectTransform);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;

            if (!string.IsNullOrEmpty(label)) {
                CreateTextFill(root, label, textSize, textColor, FontStyles.Bold, TextAlignmentOptions.Center);
            }
            AddPressMotion(button);
            return button;
        }

        /// <summary>Botón-ícono sin texto a partir de un Sprite — p. ej. "Volver"/"Pantalla completa" del mockup del Módulo 2, donde el arte ya comunica la acción. Devuelve también la Image para poder teñirla (ej. estado activo).</summary>
        public static Button CreateSpriteIconButton(Transform parent, Sprite sprite, float preferredWidth, float preferredHeight, out Image icon) {
            RectTransform root = CreateRect("IconButton", parent);
            LayoutElement le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = preferredWidth;
            le.preferredHeight = preferredHeight;

            icon = CreateSpriteImage("Icon", root, sprite, Image.Type.Simple);
            icon.preserveAspect = true;
            Stretch(icon.rectTransform);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            AddPressMotion(button, 0.92f);
            return button;
        }

        public static Button CreateGradientButton(Transform parent, string label, float preferredHeight = 64f) {
            RectTransform root = CreateRect("Button", parent);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = Mathf.Max(preferredHeight, EsportsTheme.Metrics.TouchTarget);

            Image bg = root.gameObject.AddComponent<Image>();
            if (EsportsSkin.HasSprites) {
                bg.sprite = EsportsSkin.ChamferFill;
                bg.type = Image.Type.Sliced;
            }
            bg.color = Color.white;
            AddGradient(bg, EsportsTheme.NeonBlue, EsportsTheme.NeonPurple, UIGradient.Direction.Horizontal);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;

            // Texto oscuro sobre el degradado brillante: blanco sobre cian no llega a 3:1 de contraste.
            TextMeshProUGUI text = CreateTextFill(root, label, EsportsTheme.TextSize.Subtitle, EsportsTheme.BgDeep, FontStyles.Bold, TextAlignmentOptions.Center);
            text.rectTransform.offsetMin = new Vector2(12f, 0f);
            text.rectTransform.offsetMax = new Vector2(-12f, 0f);
            text.characterSpacing = 4f;
            text.fontStyle |= FontStyles.UpperCase;
            AddPressMotion(button);
            return button;
        }

        /// <summary>
        /// Selector desplegable con el mismo estilo oscuro/neón que el resto del kit — usado, por
        /// ejemplo, para elegir el tipo de curva (función de pertenencia) de un ejercicio en vivo.
        /// </summary>
        public static TMP_Dropdown CreateDropdown(Transform parent, string[] options, int startIndex, float preferredHeight = 40f) {
            preferredHeight = Mathf.Max(preferredHeight, EsportsTheme.Metrics.TouchTarget);
            RectTransform root = CreateRect("Dropdown", parent);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;

            // Caja con solo contorno (sin relleno sólido) — pedido explícito para que combine con las
            // tarjetas del mockup del Módulo 2, donde este tipo de caja deja ver el panel semitransparente
            // de atrás en vez de taparlo con un color plano. "bg" en sí queda transparente, solo sirve
            // de target de clicks (alpha 0 igual recibe raycasts, Unity no exige alpha > 0 para eso).
            //
            // OJO: un primer intento puso un "Outline" opaco de tamaño completo con un "OutlineInner"
            // transparente ENCIMA esperando que tapara el centro — error de concepto: en uGUI un hijo
            // transparente no "recorta" ni hace agujero en el Image del padre, cada Image se dibuja
            // como su propio quad sólido; el padre opaco se seguía viendo completo por detrás, dando
            // una barra sólida en vez de un contorno (justo lo que se vio en la captura). La forma
            // correcta de dibujar un marco sin relleno en uGUI es con 4 barras delgadas independientes.
            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);

            CreateOutline(root, EsportsTheme.TextPrimary, 1.5f);

            TMP_Dropdown dropdown = root.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = bg;

            TextMeshProUGUI label = CreateTextFill(root, options.Length > 0 ? options[startIndex] : "", EsportsTheme.TextSize.Body, EsportsTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
            label.rectTransform.offsetMin = new Vector2(14f, 0f);
            label.rectTransform.offsetMax = new Vector2(-14f, 0f);
            dropdown.captionText = label;

            // Template mínimo requerido por TMP_Dropdown para poder abrir la lista de opciones.
            RectTransform template = CreateRect("Template", root);
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, 2f);
            template.sizeDelta = new Vector2(0f, preferredHeight * options.Length);
            Image templateBg = template.gameObject.AddComponent<Image>();
            templateBg.color = EsportsTheme.BgPanel;
            template.gameObject.AddComponent<ScrollRect>();
            template.gameObject.SetActive(false);

            RectTransform viewport = CreateRect("Viewport", template);
            Stretch(viewport);
            viewport.gameObject.AddComponent<Image>().color = Color.white;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, preferredHeight);

            RectTransform item = CreateRect("Item", content);
            item.anchorMin = new Vector2(0f, 1f);
            item.anchorMax = new Vector2(1f, 1f);
            item.pivot = new Vector2(0.5f, 1f);
            item.sizeDelta = new Vector2(0f, preferredHeight);
            Toggle itemToggle = item.gameObject.AddComponent<Toggle>();
            Image itemBg = CreateImage("ItemBg", item, EsportsTheme.BgPanelLight);
            Stretch(itemBg.rectTransform);
            itemToggle.targetGraphic = itemBg;
            TextMeshProUGUI itemLabel = CreateTextFill(item, "Option", EsportsTheme.TextSize.Body, EsportsTheme.TextPrimary, FontStyles.Normal, TextAlignmentOptions.Left);
            itemLabel.rectTransform.offsetMin = new Vector2(14f, 0f);

            dropdown.template = template;
            dropdown.itemText = itemLabel;

            dropdown.options.Clear();
            foreach (string opt in options) dropdown.options.Add(new TMP_Dropdown.OptionData(opt));
            dropdown.value = startIndex;
            dropdown.RefreshShownValue();

            return dropdown;
        }

        /// <summary>Botón "fantasma": solo borde/relleno tenue, sin degradado — para acciones secundarias (volver, pantalla completa, cerrar) que no deben competir visualmente con el CTA principal.</summary>
        public static Button CreateGhostButton(Transform parent, string label, float preferredWidth = 110f, float preferredHeight = 44f) {
            RectTransform root = CreateRect("GhostButton", parent);
            LayoutElement le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = preferredWidth;
            le.preferredHeight = Mathf.Max(preferredHeight, EsportsTheme.Metrics.TouchTarget);
            le.minHeight = EsportsTheme.Metrics.TouchTarget;

            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = EsportsTheme.BgPanelLight;
            if (EsportsSkin.HasSprites) {
                bg.sprite = EsportsSkin.ChamferFill;
                bg.type = Image.Type.Sliced;
                // Esquinas más chicas que en los paneles: en un botón de 60 de alto el recorte grande
                // se comería la mitad de la altura.
                bg.pixelsPerUnitMultiplier = 1.6f;
                Image line = CreateSpriteImage("Line", root, EsportsSkin.ChamferLine, Image.Type.Sliced);
                line.pixelsPerUnitMultiplier = 1.6f;
                line.color = EsportsTheme.Stroke;
                line.raycastTarget = false;
                Stretch(line.rectTransform);
            }

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;

            TextMeshProUGUI text = CreateTextFill(root, label, EsportsTheme.TextSize.Body, EsportsTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
            text.rectTransform.offsetMin = new Vector2(10f, 0f);
            text.rectTransform.offsetMax = new Vector2(-10f, 0f);
            AddPressMotion(button);
            return button;
        }

        /// <summary>
        /// Área con scroll vertical (Viewport + Content con VerticalLayoutGroup) para cuando el
        /// contenido de una tarjeta puede crecer más de lo que entra a simple vista — ej. una lista
        /// de sliders de un ejercicio. Devuelve el RectTransform "Content" donde agregar hijos.
        /// </summary>
        public static RectTransform CreateScrollArea(Transform parent, float visibleHeight) {
            RectTransform root = CreateRect("ScrollArea", parent);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = visibleHeight;

            ScrollRect scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            RectTransform viewport = CreateRect("Viewport", root);
            Stretch(viewport);
            Image viewportImg = viewport.gameObject.AddComponent<Image>();
            viewportImg.color = Color.white;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            return content;
        }

        /// <summary>
        /// Línea delgada + handle tipo píldora oscura, sin relleno de color marcando el progreso —
        /// así se ve el slider en el mockup del Módulo 2 (antes tenía una barra con degradado
        /// azul→morado tipo "barra de carga", que no está en la referencia).
        /// </summary>
        public static Slider CreateGradientSlider(Transform parent, float min, float max, float value) {
            RectTransform root = CreateRect("Slider", parent);
            // Sin estirar explícitamente el ancho colapsa a 0 y solo se ve el handle.
            root.anchorMin = new Vector2(0f, 0.5f);
            root.anchorMax = new Vector2(1f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(0f, EsportsTheme.Metrics.SliderHeight);
            Slider slider = root.gameObject.AddComponent<Slider>();

            // Fondo transparente en toda el área: hace que tocar cualquier punto de la franja (no solo
            // el handle) mueva el slider — área táctil de 44 px reales aunque la línea sea delgada.
            Image hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0f);

            RectTransform trackRT = CreateRect("Track", root);
            trackRT.anchorMin = new Vector2(0f, 0.5f);
            trackRT.anchorMax = new Vector2(1f, 0.5f);
            trackRT.pivot = new Vector2(0.5f, 0.5f);
            trackRT.sizeDelta = new Vector2(-24f, 4f);
            trackRT.anchoredPosition = Vector2.zero;
            Image track = trackRT.gameObject.AddComponent<Image>();
            track.color = EsportsTheme.WithAlpha(EsportsTheme.TextPrimary, 0.22f);
            track.raycastTarget = false;

            // Tramo recorrido en cian sobre el riel tenue: el valor se lee de un vistazo sin mirar el número.
            RectTransform fillArea = CreateRect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.pivot = new Vector2(0.5f, 0.5f);
            fillArea.sizeDelta = new Vector2(-24f, 4f);
            fillArea.anchoredPosition = Vector2.zero;
            RectTransform fillRT = CreateRect("Fill", fillArea);
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.sizeDelta = Vector2.zero;
            Image fill = fillRT.gameObject.AddComponent<Image>();
            fill.color = EsportsTheme.NeonBlue;
            fill.raycastTarget = false;

            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea, 12f, 12f, 0f, 0f);
            // Handle más grande y con borde claro: antes era 10×26 unidades (≈7×20 px reales), difícil
            // de ver y de agarrar sobre el fondo oscuro.
            Image handle = CreateImage("Handle", handleArea, EsportsTheme.TextPrimary);
            // El Slider estira el handle a toda la altura del área (anclas Y 0→1), así que sizeDelta.y
            // SE SUMA a esa altura: -16 deja el handle en 44 (60 − 16) en vez de 104.
            handle.rectTransform.sizeDelta = new Vector2(24f, -16f);
            Image handleCore = CreateImage("Core", handle.rectTransform, EsportsTheme.NeonBlend);
            Stretch(handleCore.rectTransform, 3f, 3f, 3f, 3f);
            handleCore.raycastTarget = false;

            slider.targetGraphic = handle;
            slider.fillRect = fillRT;
            slider.handleRect = handle.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            return slider;
        }

        // ---------------------------------------------------------------
        // Sistema "Tactical Neon" (ver MODULO2_DISENO.md)
        // ---------------------------------------------------------------

        /// <summary>Feedback de presión (escala 0.97, 120 ms ease-out) para cualquier botón.</summary>
        public static M2Motion AddPressMotion(Button button, float scale = 0.97f) {
            M2Motion motion = button.GetComponent<M2Motion>();
            if (!motion) motion = button.gameObject.AddComponent<M2Motion>();
            motion.SetPressFeedback(true, scale);
            return motion;
        }

        /// <summary>
        /// Overline: etiqueta corta en mayúsculas, mono y con tracking amplio, encima de un título
        /// ("MÓDULO 2", "NIVEL 03"). Da el tono de HUD sin restar legibilidad al título.
        /// </summary>
        public static TextMeshProUGUI CreateOverline(Transform parent, string text, Color color, float size = EsportsTheme.TextSize.Caption) {
            TextMeshProUGUI tmp = CreateText(parent, text, size, color, FontStyles.Normal, TextAlignmentOptions.Left, 0f);
            EsportsSkin.ApplyFont(tmp, TypeRole.Mono);
            tmp.fontStyle = FontStyles.UpperCase;
            tmp.characterSpacing = 8f;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        /// <summary>Chip de estado ("DEMO", "COMPLETADO"): texto mono pequeño sobre una píldora tenue del color dado.</summary>
        public static RectTransform CreateChip(Transform parent, string text, Color color, float size = EsportsTheme.TextSize.Caption) {
            RectTransform root = CreateRect("Chip", parent);
            Image bg = root.gameObject.AddComponent<Image>();
            if (EsportsSkin.HasSprites) {
                bg.sprite = EsportsSkin.ChamferFill;
                bg.type = Image.Type.Sliced;
                bg.pixelsPerUnitMultiplier = 3f;
            }
            bg.color = EsportsTheme.WithAlpha(color, 0.16f);
            bg.raycastTarget = false;

            HorizontalLayoutGroup h = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(10, 10, 3, 3);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            ContentSizeFitter fit = root.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI label = CreateText(root, text, size, color, FontStyles.Normal, TextAlignmentOptions.Center, 0f);
            EsportsSkin.ApplyFont(label, TypeRole.Mono);
            label.fontStyle = FontStyles.UpperCase | FontStyles.Bold;
            label.characterSpacing = 4f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return root;
        }

        /// <summary>Ícono generado (chevron, check, play…) teñido, de tamaño fijo dentro de un layout.</summary>
        public static Image CreateIcon(Transform parent, Sprite sprite, Color color, float size) {
            Image icon = CreateSpriteImage("Icon", parent, sprite);
            icon.color = color;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            LayoutElement le = icon.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;
            le.minWidth = size;
            icon.rectTransform.sizeDelta = new Vector2(size, size);
            return icon;
        }

        /// <summary>
        /// Encabezado de sección: overline en cian + línea fina que llena el resto del ancho
        /// ("NIVELES ───────"). Separa grupos sin agregar cajas extra.
        /// </summary>
        public static RectTransform CreateSectionHeader(Transform parent, string text, Color color) {
            RectTransform row = CreateRect("Section_" + text, parent);
            HorizontalLayoutGroup h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = 28f;

            CreateOverline(row, text, color, EsportsTheme.TextSize.Small);
            Image line = CreateImage("Rule", row, EsportsTheme.WithAlpha(color, 0.3f));
            line.raycastTarget = false;
            LayoutElement le = line.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = 1f;
            le.minHeight = 1f;
            return row;
        }

        /// <summary>
        /// Fondo ambiental del Módulo 2, a pantalla completa dentro de <paramref name="parent"/>:
        /// base azul noche con degradado hacia violeta, retícula sutil, dos luces (cian y violeta) que
        /// derivan muy lento y viñeta en los bordes para centrar la mirada. Todo sin raycast.
        /// </summary>
        public static RectTransform CreateAmbientBackground(Transform parent, string name = "Background") {
            RectTransform root = CreateRect(name, parent);
            Stretch(root);

            Image baseImg = CreateImage("Base", root, Color.white);
            Stretch(baseImg.rectTransform);
            baseImg.raycastTarget = false;
            AddGradient(baseImg, EsportsTheme.BgDeep, Color.Lerp(EsportsTheme.BgDeep, EsportsTheme.NeonPurple, 0.16f),
                UIGradient.Direction.Vertical);

            if (EsportsSkin.HasSprites) {
                Light(root, "LightCyan", EsportsTheme.NeonBlue, new Vector2(0.15f, 0.82f), 760f, 0f);
                Light(root, "LightViolet", EsportsTheme.NeonPurple, new Vector2(0.9f, 0.25f), 900f, 0.45f);

                Image grid = CreateSpriteImage("Grid", root, EsportsSkin.Grid, Image.Type.Tiled);
                Stretch(grid.rectTransform);
                grid.color = EsportsTheme.WithAlpha(EsportsTheme.TextPrimary, 0.045f);
                grid.raycastTarget = false;

                Image vignette = CreateSpriteImage("Vignette", root, EsportsSkin.Vignette);
                Stretch(vignette.rectTransform);
                vignette.color = new Color(0f, 0f, 0f, 0.75f);
                vignette.raycastTarget = false;
            }
            return root;
        }

        private static void Light(RectTransform parent, string name, Color color, Vector2 anchor, float size, float phase) {
            Image light = CreateSpriteImage(name, parent, EsportsSkin.GlowDot);
            light.rectTransform.anchorMin = light.rectTransform.anchorMax = anchor;
            light.rectTransform.sizeDelta = new Vector2(size, size);
            light.color = EsportsTheme.WithAlpha(color, 0.25f);
            light.raycastTarget = false;
            light.gameObject.AddComponent<M2Ambient>().Configure(new Vector2(60f, 45f), 18f, phase, 0.16f, 0.3f);
        }
    }
}
