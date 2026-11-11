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

        /// <summary>Texto genérico pensado para vivir directo dentro de un VerticalLayoutGroup.</summary>
        public static TextMeshProUGUI CreateText(Transform parent, string text, float size, Color color,
            FontStyles style, TextAlignmentOptions alignment, float preferredHeight) {
            RectTransform rt = CreateRect("Text", parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            if (preferredHeight > 0f) {
                rt.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            }
            return tmp;
        }

        /// <summary>Texto que se estira para llenar por completo su padre (p. ej. el Content de una tarjeta).</summary>
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
            tmp.textWrappingMode = TextWrappingModes.Normal;
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
            if (preferredHeight > 0f) {
                rt.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
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

            for (int i = 3; i >= 1; i--) {
                float spread = glowSpread * i / 3f;
                float alpha = 0.16f * i;
                Image halo = CreateImage($"Glow{i}", root, new Color(color.r, color.g, color.b, alpha));
                halo.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                halo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                halo.rectTransform.sizeDelta = new Vector2(size + spread, size + spread);
                halo.rectTransform.anchoredPosition = Vector2.zero;
                halo.raycastTarget = false;
            }

            Image core = CreateImage("Core", root, color);
            Stretch(core.rectTransform);
            core.raycastTarget = false;
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
            RectTransform root = CreateRect(name, parent);

            Image border = CreateImage("Border", root, new Color(EsportsTheme.NeonBlend.r, EsportsTheme.NeonBlend.g, EsportsTheme.NeonBlend.b, 0.3f));
            Stretch(border.rectTransform);
            border.raycastTarget = false;

            Image bg = CreateImage("Bg", root, EsportsTheme.BgPanel);
            Stretch(bg.rectTransform, borderThickness, borderThickness, borderThickness, borderThickness);

            RectTransform content = CreateRect("Content", bg.rectTransform);
            Stretch(content, 14f, 14f, 12f, 12f);
            return content;
        }

        /// <summary>Como <see cref="CreateNeonCard"/> pero con un color de acento en vez del degradado (p. ej. para diferenciar dos equipos/opciones) — mismo hairline sutil, sin halo.</summary>
        public static RectTransform CreateAccentCard(Transform parent, string name, Color accent, out Image border,
            float borderThickness = 1f, float glowSpread = 0f) {
            RectTransform root = CreateRect(name, parent);

            border = CreateImage("Border", root, new Color(accent.r, accent.g, accent.b, 0.4f));
            Stretch(border.rectTransform);
            border.raycastTarget = false;

            Image bg = CreateImage("Bg", root, EsportsTheme.BgPanel);
            Stretch(bg.rectTransform, borderThickness, borderThickness, borderThickness, borderThickness);

            RectTransform content = CreateRect("Content", bg.rectTransform);
            Stretch(content, 12f, 12f, 10f, 10f);
            return content;
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
            return button;
        }

        public static Button CreateGradientButton(Transform parent, string label, float preferredHeight = 64f) {
            RectTransform root = CreateRect("Button", parent);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;

            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = Color.white;
            AddGradient(bg, EsportsTheme.NeonBlue, EsportsTheme.NeonPurple, UIGradient.Direction.Horizontal);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;

            TextMeshProUGUI text = CreateTextFill(root, label, 24f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            return button;
        }

        /// <summary>
        /// Selector desplegable con el mismo estilo oscuro/neón que el resto del kit — usado, por
        /// ejemplo, para elegir el tipo de curva (función de pertenencia) de un ejercicio en vivo.
        /// </summary>
        public static TMP_Dropdown CreateDropdown(Transform parent, string[] options, int startIndex, float preferredHeight = 40f) {
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

            TextMeshProUGUI label = CreateTextFill(root, options.Length > 0 ? options[startIndex] : "", 18f, EsportsTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Left);
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
            TextMeshProUGUI itemLabel = CreateTextFill(item, "Option", 17f, EsportsTheme.TextPrimary, FontStyles.Normal, TextAlignmentOptions.Left);
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
            le.preferredHeight = preferredHeight;

            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = EsportsTheme.BgPanelLight;

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;

            CreateTextFill(root, label, 16f, EsportsTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
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
            root.sizeDelta = new Vector2(0f, 40f);
            Slider slider = root.gameObject.AddComponent<Slider>();

            RectTransform trackRT = CreateRect("Track", root);
            trackRT.anchorMin = new Vector2(0f, 0.5f);
            trackRT.anchorMax = new Vector2(1f, 0.5f);
            trackRT.pivot = new Vector2(0.5f, 0.5f);
            trackRT.sizeDelta = new Vector2(0f, 2f);
            trackRT.anchoredPosition = Vector2.zero;
            Image track = trackRT.gameObject.AddComponent<Image>();
            track.color = EsportsTheme.TextPrimary;
            track.raycastTarget = false;

            // El componente Slider exige un fillRect válido aunque acá no se pinte — ancla completa,
            // sin Image visible (alpha 0) encima del track.
            RectTransform fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea, 10f, 10f, 18f, 18f);
            RectTransform fillRT = CreateRect("Fill", fillArea);
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.sizeDelta = Vector2.zero;
            Image fill = fillRT.gameObject.AddComponent<Image>();
            fill.color = new Color(0f, 0f, 0f, 0f);
            fill.raycastTarget = false;

            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea, 10f, 10f, 0f, 0f);
            Image handle = CreateImage("Handle", handleArea, new Color(0.07f, 0.1f, 0.24f, 1f));
            handle.rectTransform.sizeDelta = new Vector2(10f, 26f);

            slider.targetGraphic = handle;
            slider.fillRect = fillRT;
            slider.handleRect = handle.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            return slider;
        }
    }
}
