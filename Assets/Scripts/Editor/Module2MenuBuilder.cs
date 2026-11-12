using System.Collections.Generic;
using Neuralizer.Data;
using Neuralizer.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Neuralizer.EditorTools {

    /// <summary>
    /// Herramienta de un solo uso (Editor > Neuralizer > Construir menú del Módulo 2) que agrega el
    /// selector de módulo ("1"/"2"), la lista de ejercicios del Módulo 2 y el "tema" visual del Módulo 2
    /// (fondo degradado + logo) al menú principal de <c>MainScene</c>, SIN tocar nada de lo que ya arma
    /// el Módulo 1 — solo agrega hermanos nuevos junto a los que ya existen, todos apagados por defecto
    /// salvo cuando el Módulo 2 está seleccionado. Pensada para ejecutarse una vez (o para reconstruir si
    /// se borra todo lo que agrega, ver <see cref="RemoveExisting"/>) en vez de armar esto a mano clic
    /// por clic.
    /// </summary>
    /// <remarks>
    /// Estructura de la que depende (ver MODULO2_GUIA.md e investigación de MainScene):
    /// UI/Main Menu Canvas/{BG, Safe Area/Wrapper/{App Bar, Logo Panel/Image, Content Panel,
    /// Version Label}, Start Button}. Esta herramienta agrega: "ModuleTabs" (primer hijo de Wrapper,
    /// oculto hasta presionar Start), "Module2ContentPanel" (hermano nuevo de "Content Panel", inicialmente
    /// inactivo), "Module2Background" (hermano nuevo de "BG" dentro de Main Menu Canvas, detrás de Safe
    /// Area) y "Module2Logo" (hermano nuevo de "Image" dentro de "Logo Panel") — y además reubica
    /// "App Bar" (Puntuaciones/Info) al final de Wrapper, sin tocar su contenido.
    /// </remarks>
    public static class Module2MenuBuilder {

        private const string WrapperPath = "UI/Main Menu Canvas/Safe Area/Wrapper";
        private const string Module1PanelName = "Content Panel";

        [MenuItem("Neuralizer/Construir menú del Módulo 2")]
        public static void Build() {
            GameObject wrapperGO = GameObject.Find(WrapperPath);
            if (!wrapperGO) {
                Debug.LogError($"Module2MenuBuilder: no se encontró '{WrapperPath}'. ¿Está abierta MainScene?");
                return;
            }
            Transform wrapper = wrapperGO.transform;
            // Wrapper -> Safe Area -> Main Menu Canvas: subimos dos niveles en vez de buscar por nombre
            // de nuevo, así seguimos ligados a la misma instancia de canvas que ya encontramos.
            Transform mainMenuCanvas = wrapper.parent.parent;

            RemoveExisting(wrapper, mainMenuCanvas);

            Transform module1Panel = wrapper.Find(Module1PanelName);
            if (!module1Panel) {
                Debug.LogError($"Module2MenuBuilder: no se encontró '{Module1PanelName}' dentro de Wrapper.");
                return;
            }

            Transform logoPanel = wrapper.Find("Logo Panel");
            Transform logoImage = logoPanel ? logoPanel.Find("Image") : null;

            GameObject module2Panel = BuildModule2Panel(wrapper);
            module2Panel.transform.SetSiblingIndex(module1Panel.GetSiblingIndex() + 1);
            module2Panel.SetActive(false);

            GameObject module2Logo = logoPanel ? BuildModule2Logo(logoPanel) : null;
            if (!logoPanel) {
                Debug.LogWarning("Module2MenuBuilder: no se encontró 'Logo Panel' — no se pudo agregar el logo del Módulo 2.");
            }

            Transform bgSibling = mainMenuCanvas.Find("BG");
            GameObject module2Background = bgSibling ? BuildModule2Background(mainMenuCanvas, bgSibling) : null;
            if (!bgSibling) {
                Debug.LogWarning("Module2MenuBuilder: no se encontró 'BG' en Main Menu Canvas — no se pudo agregar el fondo del Módulo 2.");
            }

            // Puntuaciones/Info: los ubicamos antes de construir ModuleTabs porque sus botones se
            // re-cablean ahí (para que adopten el tema neón al seleccionar el Módulo 2 — ver
            // CollectAppBarGraphics). Su posición se mueve al fondo de la pantalla, como en el boceto;
            // no se toca su contenido, y ese reposicionamiento es persistente (no ligado a si el
            // Módulo 2 sigue instalado: "Quitar menú del Módulo 2" no lo revierte).
            Transform appBar = wrapper.Find("App Bar");
            if (appBar) appBar.SetAsLastSibling();

            CollectAppBarGraphics(appBar, out Graphic[] appBarSurfaces, out Graphic[] appBarIcons);

            GameObject moduleTabs = BuildModuleTabs(wrapper, module1Panel.gameObject, module2Panel,
                module2Background, logoImage ? logoImage.gameObject : null, module2Logo,
                appBarSurfaces, appBarIcons);
            moduleTabs.transform.SetAsFirstSibling();
            // Oculto hasta presionar "Start": ver HookStartButton más abajo. Sin esto, el selector de
            // módulo aparece encima de la animación inicial "PRESS START TO LEARN", que es justo lo que
            // se marcó como incorrecto en la revisión de pantalla 1.
            moduleTabs.SetActive(false);
            HookStartButton(mainMenuCanvas, moduleTabs);

            // Deja que "Volver" desde un ejercicio del Módulo 2 caiga directo en el menú (con el tab
            // correcto seleccionado) en vez de mostrar de nuevo la animación de arranque — ver
            // MainMenuReturnState/MainMenuReturnHandler. Sin campos que cablear, así que no hace falta
            // reconstruirlo cada vez: si ya está, se deja como está.
            if (!mainMenuCanvas.GetComponent<MainMenuReturnHandler>()) {
                mainMenuCanvas.gameObject.AddComponent<MainMenuReturnHandler>();
            }

            // El Canvas del menú usa Reference Pixels Per Unit = 2 (estilo pixel art del Módulo 1), no
            // 100 como las escenas del Módulo 2: sin compensar, los bordes 9-slice (esquinas recortadas)
            // se dibujan 50 veces más chicos y las tarjetas se ven como rectángulos planos.
            CanvasScaler menuScaler = mainMenuCanvas.GetComponentInParent<CanvasScaler>();
            float slicedScale = menuScaler ? menuScaler.referencePixelsPerUnit / 100f : 1f;
            foreach (GameObject built in new[] { module2Panel, moduleTabs, module2Background, module2Logo }) {
                if (built) CompensateSlicedScale(built.transform, slicedScale);
            }

            EditorUtility.SetDirty(wrapperGO);
            EditorSceneManager.MarkSceneDirty(wrapperGO.scene);
            Debug.Log("Module2MenuBuilder: listo. Guarda la escena (Ctrl+S) para persistir los cambios.");
        }

        /// <summary>
        /// Versión ligera de <see cref="Build"/>: NO reconstruye el menú, solo busca la lista de
        /// ejercicios del Módulo 2 ya existente en MainScene, quita las entradas de niveles de datos
        /// de una corrida anterior (las que tienen <c>fuzzyLevel</c> asignado) y agrega una por cada
        /// <see cref="FuzzyScene"/> de la carpeta de niveles. Respeta cualquier retoque manual del resto del menú.
        /// </summary>
        [MenuItem("Neuralizer/Agregar niveles al menú del Módulo 2")]
        public static void AddLevelEntries() {
            // Con el rediseño, la lista del Módulo 2 (encabezado con progreso, secciones y tarjetas) se
            // arma completa en un solo paso para que el escalonado y el progreso queden coherentes.
            // Este comando se conserva como atajo: reconstruye el panel con los niveles actuales.
            Build();
        }

        [MenuItem("Neuralizer/Quitar menú del Módulo 2")]
        public static void Remove() {
            GameObject wrapperGO = GameObject.Find(WrapperPath);
            if (!wrapperGO) return;
            RemoveExisting(wrapperGO.transform, wrapperGO.transform.parent.parent);
            EditorSceneManager.MarkSceneDirty(wrapperGO.scene);
        }

        private static void RemoveExisting(Transform wrapper, Transform mainMenuCanvas) {
            Transform existingTabs = wrapper.Find("ModuleTabs");
            if (existingTabs) {
                // El listener que agregamos en el Start Button apunta a esta instancia exacta de
                // ModuleTabs; si no lo quitamos antes de destruirla, queda un listener "muerto" (target
                // null) acumulándose en el botón cada vez que se reconstruye este menú.
                UnhookStartButton(mainMenuCanvas, existingTabs.gameObject);
                Object.DestroyImmediate(existingTabs.gameObject);
            }
            Transform existingPanel = wrapper.Find("Module2ContentPanel");
            if (existingPanel) Object.DestroyImmediate(existingPanel.gameObject);

            // Resto de una versión anterior de esta herramienta (cuando sí existía un banner-placeholder
            // debajo del logo): ya no se vuelve a crear, pero si quedó guardado en la escena de una
            // ejecución vieja hay que destruirlo acá o se queda ahí para siempre — reconstruir ya no lo
            // toca porque Build() no lo referencia en absoluto.
            Transform existingBanner = wrapper.Find("Banner");
            if (existingBanner) Object.DestroyImmediate(existingBanner.gameObject);

            Transform existingBg = mainMenuCanvas ? mainMenuCanvas.Find("Module2Background") : null;
            if (existingBg) Object.DestroyImmediate(existingBg.gameObject);

            Transform logoPanel = wrapper.Find("Logo Panel");
            Transform existingLogo = logoPanel ? logoPanel.Find("Module2Logo") : null;
            if (existingLogo) Object.DestroyImmediate(existingLogo.gameObject);
        }

        // ---------------------------------------------------------------
        // Botón "Start": revelar ModuleTabs recién cuando se presiona
        // ---------------------------------------------------------------

        private static void HookStartButton(Transform mainMenuCanvas, GameObject moduleTabs) {
            Button startButton = FindStartButton(mainMenuCanvas);
            if (!startButton) {
                Debug.LogWarning("Module2MenuBuilder: no se encontró 'Start Button' — ModuleTabs no se revelará automáticamente al iniciar.");
                return;
            }
            // AddBoolPersistentListener (y no AddPersistentListener a secas) porque SetActive(bool)
            // necesita el argumento "true" grabado como parte de la llamada persistente — igual que ya
            // hacen, a mano, las otras llamadas a SetActive que trae este mismo botón (App Bar, Content
            // Panel, Version Label).
            UnityEventTools.AddBoolPersistentListener(startButton.onClick, moduleTabs.SetActive, true);
        }

        private static void UnhookStartButton(Transform mainMenuCanvas, GameObject moduleTabs) {
            Button startButton = FindStartButton(mainMenuCanvas);
            if (!startButton) return;
            for (int i = startButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--) {
                if (startButton.onClick.GetPersistentTarget(i) == moduleTabs) {
                    UnityEventTools.RemovePersistentListener(startButton.onClick, i);
                }
            }
        }

        private static Button FindStartButton(Transform mainMenuCanvas) {
            Transform t = mainMenuCanvas ? mainMenuCanvas.Find("Start Button") : null;
            return t ? t.GetComponent<Button>() : null;
        }

        // ---------------------------------------------------------------

        /// <summary>Escribe un array de referencias a Graphic en una SerializedProperty de tipo array — SerializedObject no acepta asignar un C# array entero de un golpe.</summary>
        private static void SetGraphicArray(SerializedProperty arrayProp, Graphic[] values) {
            arrayProp.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) {
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        /// <summary>
        /// Junta los Graphic (Image) de los fondos e iconos de "Scores Button"/"About Button" dentro de
        /// App Bar, para que <see cref="ModuleTabController"/> pueda re-tintarlos en vivo al cambiar de
        /// módulo. No se toca ningún color acá — ModuleTabController captura los originales del Módulo 1
        /// en runtime (Awake), así que esto solo necesita señalar QUÉ componentes re-tintar.
        /// </summary>
        private static void CollectAppBarGraphics(Transform appBar, out Graphic[] surfaces, out Graphic[] icons) {
            surfaces = new Graphic[0];
            icons = new Graphic[0];
            if (!appBar) return;

            Transform scoresButton = appBar.Find("Scores Button");
            Transform aboutButton = appBar.Find("About Button");
            if (!scoresButton || !aboutButton) {
                Debug.LogWarning("Module2MenuBuilder: no se encontraron 'Scores Button'/'About Button' en App Bar — la barra inferior no cambiará de tema en el Módulo 2.");
                return;
            }

            surfaces = new Graphic[] {
                scoresButton.GetComponent<Image>(),
                aboutButton.GetComponent<Image>(),
            };
            icons = new Graphic[] {
                scoresButton.Find("Icon") ? scoresButton.Find("Icon").GetComponent<Image>() : null,
                aboutButton.Find("Icon") ? aboutButton.Find("Icon").GetComponent<Image>() : null,
            };
        }

        private static GameObject BuildModuleTabs(Transform parent, GameObject module1Panel, GameObject module2Panel,
            GameObject module2Background, GameObject module1Logo, GameObject module2Logo,
            Graphic[] appBarSurfaces, Graphic[] appBarIcons) {
            RectTransform row = EsportsUIKit.CreateRect("ModuleTabs", parent);
            LayoutElement rowLE = row.gameObject.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 44f;
            rowLE.flexibleHeight = 0f;
            // Bug real de Unity (no un typo nuestro): un HorizontalLayoutGroup, en el MISMO
            // GameObject que un LayoutElement, también actúa como ILayoutElement de sí mismo frente al
            // VerticalLayoutGroup padre. Con la misma layoutPriority que nuestro LayoutElement, Unity
            // no deja ganar a uno — toma el valor MÁS GRANDE entre ambos por cada propiedad, y el
            // HorizontalLayoutGroup reporta flexibleHeight=1 (por cómo agrega el alto de sus hijos en
            // su eje cruzado), pisando nuestro flexibleHeight=0 explícito. Subir la prioridad del
            // LayoutElement por encima de la del LayoutGroup lo hace ganar sin ambigüedad.
            rowLE.layoutPriority = 2;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            // Igual que arriba: si esto queda en true (el default de Unity al agregar el componente
            // por script), los botones "1"/"2" se estiran a lo alto de lo que mida "row", que es
            // exactamente el bug visual que se vio en la primera prueba (pastillas gigantes).
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            rowLE.preferredHeight = 48f;

            TextMeshProUGUI caption = EsportsUIKit.CreateOverline(row, "Módulo", EsportsTheme.TextSecondary, MenuSize.Small);
            caption.gameObject.AddComponent<LayoutElement>().preferredWidth = 96f;

            // Pestañas sobre un indicador que se desliza (M2TabIndicator) en vez de cambiar de color de
            // golpe: se entiende de dónde viene y a dónde va la selección.
            RectTransform tabs = EsportsUIKit.CreateRect("Tabs", row);
            HorizontalLayoutGroup tabsLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.spacing = 6f;
            tabsLayout.childControlWidth = true;
            tabsLayout.childControlHeight = true;
            tabsLayout.childForceExpandWidth = false;
            tabsLayout.childForceExpandHeight = false;
            tabsLayout.childAlignment = TextAnchor.MiddleCenter;

            Image indicator = EsportsUIKit.CreateImage("Indicator", tabs, EsportsTheme.WithAlpha(EsportsTheme.NeonBlue, 0.32f));
            if (EsportsSkin.HasSprites) {
                indicator.sprite = EsportsSkin.ChamferFill;
                indicator.type = Image.Type.Sliced;
                indicator.pixelsPerUnitMultiplier = 1.6f;
                Image indicatorLine = EsportsUIKit.CreateSpriteImage("Line", indicator.rectTransform, EsportsSkin.ChamferLine, Image.Type.Sliced);
                indicatorLine.pixelsPerUnitMultiplier = 1.6f;
                indicatorLine.color = EsportsTheme.NeonBlue;
                indicatorLine.raycastTarget = false;
                EsportsUIKit.Stretch(indicatorLine.rectTransform);
            }
            indicator.raycastTarget = false;
            indicator.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            M2TabIndicator tabIndicator = tabs.gameObject.AddComponent<M2TabIndicator>();
            tabIndicator.SetIndicator(indicator.rectTransform);

            Button tab1 = EsportsUIKit.CreateGhostButton(tabs, "1", 56f, 44f);
            Button tab2 = EsportsUIKit.CreateGhostButton(tabs, "2", 56f, 44f);
            foreach (Button tab in new[] { tab1, tab2 }) {
                // En el Canvas del menú (540 px) 44 unidades ya son 44 px: el mínimo táctil.
                LayoutElement le = tab.GetComponent<LayoutElement>();
                le.minHeight = 44f;
                le.preferredHeight = 44f;
                TextMeshProUGUI label = tab.GetComponentInChildren<TextMeshProUGUI>();
                EsportsSkin.ApplyFont(label, TypeRole.Mono);
                label.fontSizeMax = MenuSize.Subtitle;
                label.fontStyle = FontStyles.Bold;
            }

            ModuleTabController controller = row.gameObject.AddComponent<ModuleTabController>();
            SerializedObject so = new(controller);
            so.FindProperty("module1Panel").objectReferenceValue = module1Panel;
            so.FindProperty("module2Panel").objectReferenceValue = module2Panel;
            so.FindProperty("module2Background").objectReferenceValue = module2Background;
            so.FindProperty("module1Logo").objectReferenceValue = module1Logo;
            so.FindProperty("module2Logo").objectReferenceValue = module2Logo;
            so.FindProperty("tab1Background").objectReferenceValue = tab1.GetComponent<Image>();
            so.FindProperty("tab2Background").objectReferenceValue = tab2.GetComponent<Image>();
            so.FindProperty("tabIndicator").objectReferenceValue = tabIndicator;
            SetGraphicArray(so.FindProperty("appBarSurfaces"), appBarSurfaces);
            SetGraphicArray(so.FindProperty("appBarIcons"), appBarIcons);
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(tab1.onClick, controller.ShowModule1);
            UnityEventTools.AddPersistentListener(tab2.onClick, controller.ShowModule2);

            return row.gameObject;
        }

        /// <summary>
        /// Fondo del Módulo 2: un panel a pantalla completa con el degradado neón azul→morado (el mismo
        /// que ya usan las tarjetas/botones del kit), como hermano de "BG" dentro de Main Menu Canvas —
        /// así queda detrás de todo el contenido (Safe Area, Start Button) sin reemplazar ni tocar el
        /// "BG" original del Módulo 1. Empieza apagado; <see cref="ModuleTabController"/> lo enciende
        /// solo mientras el Módulo 2 está seleccionado.
        /// </summary>
        private static GameObject BuildModule2Background(Transform mainMenuCanvas, Transform bgSibling) {
            // Fondo ambiental del sistema (azul noche, retícula, luces que derivan, viñeta) en vez del
            // degradado neón a pantalla completa, que competía con el texto de las tarjetas.
            RectTransform root = EsportsUIKit.CreateAmbientBackground(mainMenuCanvas, "Module2Background");
            root.SetSiblingIndex(bgSibling.GetSiblingIndex() + 1);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        /// <summary>
        /// Logotipo tipográfico del Módulo 2 dentro de "Logo Panel" (hermano del logo del Módulo 1):
        /// overline mono + titular con degradado cian→violeta + filete de acento. Si llega arte propio,
        /// basta con reemplazar el contenido del GameObject "Module2Logo". Empieza apagado;
        /// <see cref="ModuleTabController"/> lo enciende al seleccionar el Módulo 2.
        /// </summary>
        private static GameObject BuildModule2Logo(Transform logoPanel) {
            RectTransform root = EsportsUIKit.CreateRect("Module2Logo", logoPanel);
            EsportsUIKit.Stretch(root);

            RectTransform stack = EsportsUIKit.CreateRect("Stack", root);
            stack.anchorMin = new Vector2(0f, 0.5f);
            stack.anchorMax = new Vector2(1f, 0.5f);
            stack.pivot = new Vector2(0.5f, 0.5f);
            stack.sizeDelta = new Vector2(0f, 120f);
            VerticalLayoutGroup v = stack.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4f;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            TextMeshProUGUI over = EsportsUIKit.CreateOverline(stack, "Neuralizer  ·  Módulo 02", EsportsTheme.NeonBlue, MenuSize.Small);
            over.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI title = EsportsUIKit.CreateGradientText(stack, "Lógica Difusa", 44f, FontStyles.Normal,
                TextAlignmentOptions.Center, 0f, EsportsTheme.NeonBlue, EsportsTheme.NeonPurple);
            title.characterSpacing = 3f;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMax = 44f;
            title.fontSizeMin = 28f;

            RectTransform ruleHolder = EsportsUIKit.CreateRect("Rule", stack);
            ruleHolder.gameObject.AddComponent<LayoutElement>().preferredHeight = 3f;
            Image rule = EsportsUIKit.CreateImage("Line", ruleHolder, EsportsTheme.Accent);
            rule.raycastTarget = false;
            rule.rectTransform.anchorMin = rule.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rule.rectTransform.sizeDelta = new Vector2(64f, 3f);

            M2Motion.AddEntrance(stack.gameObject, 0f, 0.94f);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ---------------------------------------------------------------
        // Panel del Módulo 2 — sistema "Tactical Neon" (MODULO2_DISENO.md)
        // ---------------------------------------------------------------

        /// <summary>
        /// Tamaños del menú en px. El Canvas de MainScene es 540×960 con match de ancho, así que aquí
        /// 1 unidad = 1 px real (a diferencia de las escenas de ejercicios, que usan 720×1280).
        /// Misma escala 12/14/16/18/24 px que <see cref="EsportsTheme.TextSize"/>.
        /// </summary>
        private static class MenuSize {
            public const float Caption = 12f;
            public const float Small = 14f;
            public const float Body = 16f;
            public const float Subtitle = 18f;
            public const float Title = 26f;
            public const float Index = 24f;
        }

        private class EntrySpec {
            public string index, title, subtitle, chip, sceneName;
            public Color accent;
            public FuzzyScene level;
        }

        private static GameObject BuildModule2Panel(Transform parent) {
            RectTransform panel = EsportsUIKit.CreateRect("Module2ContentPanel", parent);
            // Mismo trato que "Content Panel": un LayoutElement flexible dentro del VerticalLayoutGroup
            // de Wrapper para que ocupe el espacio restante debajo de App Bar / Logo Panel.
            // "Logo Panel" pide flexibleHeight = 270 en el Wrapper: con un 1 acá, el logo se quedaba con
            // casi todo el espacio libre y la lista quedaba con altura ~0 (invisible). El panel del
            // Módulo 1 no sufre esto porque su propio layout le da altura preferida; este no tiene
            // layout interno, así que se le da un mínimo y un peso flexible mucho mayor.
            LayoutElement panelLE = panel.gameObject.AddComponent<LayoutElement>();
            panelLE.flexibleHeight = 3000f;
            panelLE.minHeight = 380f;

            // "panel" no tiene su propio LayoutGroup, así que su hijo (el área con scroll) debe
            // estirarse a mano para llenarlo — un LayoutElement no serviría aquí sin un LayoutGroup
            // en el padre que lo controle.
            RectTransform list = EsportsUIKit.CreateScrollArea(panel, 0f);
            RectTransform scrollRoot = (RectTransform)list.parent.parent;
            EsportsUIKit.Stretch(scrollRoot);
            VerticalLayoutGroup listLayout = list.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 10f;
            listLayout.padding = new RectOffset(4, 4, 6, 24);

            float delay = 0f;
            BuildHeader(list, delay, out TextMeshProUGUI progressLabel, out Image progressFill);

            List<EntrySpec> demos = new() {
                new EntrySpec { index = "01", title = "El Dilema del Healer", subtitle = "Ajusta el peso de una regla y decide a quién curar.", chip = "PRÁCTICA", sceneName = "FuzzyDemo", accent = EsportsTheme.NeonBlue },
                new EntrySpec { index = "02", title = "Lectura del Daño", subtitle = "Asocia color con urgencia, sin mirar números.", chip = "PRÁCTICA", sceneName = "FuzzyDamageDemo", accent = EsportsTheme.NeonBlue },
                new EntrySpec { index = "03", title = "Triage Rápido", subtitle = "Dos aliados en pantalla: ¿quién está peor?", chip = "PRÁCTICA", sceneName = "FuzzyComparisonDemo", accent = EsportsTheme.NeonBlue },
            };

            List<EntrySpec> levels = new();
            string[] guids = AssetDatabase.FindAssets("t:FuzzyScene", new[] { Module2LevelAssetsBuilder.Folder });
            string[] paths = System.Array.ConvertAll(guids, g => AssetDatabase.GUIDToAssetPath(g));
            System.Array.Sort(paths, System.StringComparer.Ordinal);
            if (paths.Length == 0) {
                Debug.LogWarning("Module2MenuBuilder: no hay niveles en " + Module2LevelAssetsBuilder.Folder +
                                 " — corre 'Tools/Neuralizer/Crear Niveles del Módulo 2 (FuzzyScene)' para agregarlos al menú.");
            }
            for (int i = 0; i < paths.Length; i++) {
                FuzzyScene level = AssetDatabase.LoadAssetAtPath<FuzzyScene>(paths[i]);
                levels.Add(new EntrySpec {
                    index = (i + 1).ToString("00"),
                    title = level.DisplayName,
                    subtitle = LevelSubtitle(level),
                    chip = level.mode == FuzzyExerciseMode.Classification ? "CLASIFICAR" : "ORDENAR",
                    sceneName = Module2LevelSceneBuilder.SceneName,
                    accent = EsportsTheme.NeonPurple,
                    level = level
                });
            }

            List<Module2ExerciseEntry> entries = new();
            List<GameObject> badges = new();

            delay += M2Ease.Stagger;
            M2Motion.AddEntrance(EsportsUIKit.CreateSectionHeader(list, "Entrenamiento", EsportsTheme.NeonBlue).gameObject, delay);
            foreach (EntrySpec spec in demos) {
                delay += M2Ease.Stagger;
                entries.Add(BuildExerciseEntry(list, spec, delay, out GameObject badge));
                badges.Add(badge);
            }

            if (levels.Count > 0) {
                delay += M2Ease.Stagger;
                M2Motion.AddEntrance(EsportsUIKit.CreateSectionHeader(list, "Niveles", EsportsTheme.NeonPurple).gameObject, delay);
                foreach (EntrySpec spec in levels) {
                    delay += M2Ease.Stagger;
                    entries.Add(BuildExerciseEntry(list, spec, delay, out GameObject badge));
                    badges.Add(badge);
                }
            }

            Module2MenuView view = panel.gameObject.AddComponent<Module2MenuView>();
            SerializedObject so = new(view);
            SerializedProperty entriesProp = so.FindProperty("entries");
            entriesProp.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++) entriesProp.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            SerializedProperty badgesProp = so.FindProperty("completedBadges");
            badgesProp.arraySize = badges.Count;
            for (int i = 0; i < badges.Count; i++) badgesProp.GetArrayElementAtIndex(i).objectReferenceValue = badges[i];
            so.FindProperty("progressLabel").objectReferenceValue = progressLabel;
            so.FindProperty("progressFill").objectReferenceValue = progressFill;
            so.ApplyModifiedPropertiesWithoutUndo();

            return panel.gameObject;
        }

        /// <summary>Ajusta <c>pixelsPerUnitMultiplier</c> de las imágenes Sliced/Tiled bajo <paramref name="root"/>.</summary>
        private static void CompensateSlicedScale(Transform root, float scale) {
            if (Mathf.Approximately(scale, 1f)) return;
            foreach (Image img in root.GetComponentsInChildren<Image>(true)) {
                if (img.type is Image.Type.Sliced or Image.Type.Tiled && img.sprite) {
                    img.pixelsPerUnitMultiplier *= scale;
                }
            }
        }

        private static string LevelSubtitle(FuzzyScene level) {
            if (level.mode == FuzzyExerciseMode.Classification) return "Ajusta las curvas hasta clasificar cada caso.";
            if (level.showControlSurface) return "Pesa las reglas y lee la superficie de control.";
            return level.editableMemberships ? "Corrige las curvas para ordenar bien los casos." : "Pesa las reglas para ordenar bien los casos.";
        }

        /// <summary>
        /// Encabezado del módulo: overline, titular, bajada y progreso de niveles. Corchetes de esquina
        /// como único adorno (el estilo HUD sale de ahí, sin ruido sobre el texto).
        /// </summary>
        private static void BuildHeader(Transform parent, float delay, out TextMeshProUGUI progressLabel, out Image progressFill) {
            RectTransform card = EsportsUIKit.CreateRect("Module2Header", parent);
            Image bg = card.gameObject.AddComponent<Image>();
            if (EsportsSkin.HasSprites) {
                bg.sprite = EsportsSkin.ChamferFill;
                bg.type = Image.Type.Sliced;
            }
            bg.color = EsportsTheme.WithAlpha(EsportsTheme.BgPanel, 0.82f);
            bg.raycastTarget = false;
            EsportsUIKit.AddGradient(bg, EsportsTheme.WithAlpha(EsportsTheme.BgPanelLight, 0.9f), EsportsTheme.WithAlpha(EsportsTheme.BgPanel, 0.75f));

            VerticalLayoutGroup v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(20, 20, 18, 18);
            v.spacing = 6f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            if (EsportsSkin.HasSprites) {
                Corner(card, new Vector2(0f, 1f), 0f);
                Corner(card, new Vector2(1f, 0f), 180f);
            }

            EsportsUIKit.CreateOverline(card, "Módulo 02  ·  Lógica difusa", EsportsTheme.NeonBlue, MenuSize.Caption);
            TextMeshProUGUI title = EsportsUIKit.CreateGradientText(card, "Decide como un pro", MenuSize.Title, FontStyles.Normal,
                TextAlignmentOptions.Left, 0f, EsportsTheme.TextPrimary, Color.Lerp(EsportsTheme.TextPrimary, EsportsTheme.NeonBlue, 0.45f));
            title.characterSpacing = 1f;
            EsportsUIKit.CreateText(card, "Razona con grados, no con sí o no: así piensa la IA de un buen jugador.",
                MenuSize.Body, EsportsTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Left, 0f);

            // Progreso de niveles: etiqueta + contador mono + barra con franjas que se llena al mostrarse.
            RectTransform progressRow = EsportsUIKit.CreateRect("ProgressRow", card);
            HorizontalLayoutGroup h = progressRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            progressRow.gameObject.AddComponent<LayoutElement>().minHeight = 30f;

            EsportsUIKit.CreateOverline(progressRow, "Niveles", EsportsTheme.TextSecondary, MenuSize.Caption);

            RectTransform barHolder = EsportsUIKit.CreateRect("Bar", progressRow);
            LayoutElement barLE = barHolder.gameObject.AddComponent<LayoutElement>();
            barLE.flexibleWidth = 1f;
            barLE.preferredHeight = 8f;
            barLE.minHeight = 8f;
            Image track = EsportsUIKit.CreateImage("Track", barHolder, EsportsTheme.WithAlpha(EsportsTheme.TextPrimary, 0.1f));
            EsportsUIKit.Stretch(track.rectTransform);
            track.raycastTarget = false;
            progressFill = EsportsUIKit.CreateImage("Fill", barHolder, EsportsTheme.Accent);
            EsportsUIKit.Stretch(progressFill.rectTransform);
            progressFill.raycastTarget = false;
            // Un Image "Filled" necesita sprite: sin él se dibuja completo aunque fillAmount sea 0.
            progressFill.sprite = EsportsSkin.Solid;
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillAmount = 0f;

            progressLabel = EsportsUIKit.CreateText(progressRow, "0/0", MenuSize.Small, EsportsTheme.TextPrimary,
                FontStyles.Normal, TextAlignmentOptions.Right, 0f);
            EsportsSkin.ApplyFont(progressLabel, TypeRole.Mono);
            progressLabel.fontStyle = FontStyles.Bold;
            progressLabel.textWrappingMode = TextWrappingModes.NoWrap;

            M2Motion.AddEntrance(card.gameObject, delay);
        }

        private static void Corner(RectTransform card, Vector2 anchor, float rotation) {
            Image bracket = EsportsUIKit.CreateSpriteImage("Bracket", card, EsportsSkin.Bracket);
            bracket.color = EsportsTheme.WithAlpha(EsportsTheme.NeonBlue, 0.8f);
            bracket.raycastTarget = false;
            bracket.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            RectTransform rt = bracket.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(22f, 22f);
            rt.localEulerAngles = new Vector3(0f, 0f, rotation);
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Tarjeta de ejercicio: índice mono grande ("01"), título, bajada, chip de tipo, indicador de
        /// completado y flecha de acción. Halo al pasar el puntero, escala al presionar y entrada
        /// escalonada (ver <see cref="M2Motion"/>). Devuelve la entrada y su indicador de completado.
        /// </summary>
        private static Module2ExerciseEntry BuildExerciseEntry(Transform parent, EntrySpec spec, float delay, out GameObject completedBadge) {
            RectTransform content = EsportsUIKit.CreateAccentCard(parent, "Entry_" + spec.index, spec.accent, out _, 1f);
            RectTransform root = (RectTransform)content.parent.parent;
            LayoutElement rootLE = root.gameObject.AddComponent<LayoutElement>();
            rootLE.minHeight = 92f;
            rootLE.preferredHeight = 96f;

            // Halo detrás de la tarjeta (primer hijo = se dibuja primero), apagado hasta el hover.
            Image glow = null;
            if (EsportsSkin.HasSprites) {
                glow = EsportsUIKit.CreateSpriteImage("Glow", root, EsportsSkin.ChamferGlow, Image.Type.Sliced);
                EsportsUIKit.Stretch(glow.rectTransform, -26f, -26f, -26f, -26f);
                glow.color = EsportsTheme.WithAlpha(spec.accent, 0f);
                glow.raycastTarget = false;
                glow.transform.SetAsFirstSibling();
            }

            // Barra de acento a la izquierda: identifica la sección por color de un vistazo.
            Image accentBar = EsportsUIKit.CreateImage("AccentBar", root.Find("Bg"), spec.accent);
            accentBar.raycastTarget = false;
            accentBar.rectTransform.anchorMin = new Vector2(0f, 0.22f);
            accentBar.rectTransform.anchorMax = new Vector2(0f, 0.78f);
            accentBar.rectTransform.pivot = new Vector2(0f, 0.5f);
            accentBar.rectTransform.sizeDelta = new Vector2(3f, 0f);
            accentBar.rectTransform.anchoredPosition = Vector2.zero;
            accentBar.transform.SetAsFirstSibling();

            HorizontalLayoutGroup row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 14f;
            row.padding = new RectOffset(6, 2, 0, 0);
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            TextMeshProUGUI index = EsportsUIKit.CreateText(content, spec.index, MenuSize.Index, spec.accent,
                FontStyles.Normal, TextAlignmentOptions.Center, 0f);
            EsportsSkin.ApplyFont(index, TypeRole.Mono);
            index.fontStyle = FontStyles.Bold;
            index.textWrappingMode = TextWrappingModes.NoWrap;
            index.gameObject.AddComponent<LayoutElement>().preferredWidth = 40f;

            RectTransform texts = EsportsUIKit.CreateRect("Texts", content);
            texts.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup v = texts.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2f;
            v.childAlignment = TextAnchor.MiddleLeft;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            TextMeshProUGUI title = EsportsUIKit.CreateText(texts, spec.title, MenuSize.Subtitle, EsportsTheme.TextPrimary,
                FontStyles.Bold, TextAlignmentOptions.Left, 0f);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            TextMeshProUGUI subtitle = EsportsUIKit.CreateText(texts, spec.subtitle, MenuSize.Small, EsportsTheme.TextSecondary,
                FontStyles.Normal, TextAlignmentOptions.Left, 0f);
            subtitle.lineSpacing = -8f;

            RectTransform chips = EsportsUIKit.CreateRect("Chips", texts);
            HorizontalLayoutGroup ch = chips.gameObject.AddComponent<HorizontalLayoutGroup>();
            ch.spacing = 6f;
            ch.childAlignment = TextAnchor.MiddleLeft;
            ch.childControlWidth = true;
            ch.childControlHeight = true;
            ch.childForceExpandWidth = false;
            ch.childForceExpandHeight = false;
            EsportsUIKit.CreateChip(chips, spec.chip, spec.accent, MenuSize.Caption - 1f);
            completedBadge = EsportsUIKit.CreateChip(chips, "Completado", EsportsTheme.NeonGreen, MenuSize.Caption - 1f).gameObject;
            completedBadge.SetActive(false);

            if (EsportsSkin.HasSprites) {
                EsportsUIKit.CreateIcon(content, EsportsSkin.Chevron, EsportsTheme.WithAlpha(EsportsTheme.TextPrimary, 0.7f), 22f);
            }

            Button button = root.gameObject.AddComponent<Button>();
            // El halo/borde tienen raycastTarget=false a propósito; "Bg" es el único hijo que recibe
            // el click y el que muestra el tinte de hover/press.
            Image bg = root.Find("Bg").GetComponent<Image>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.2f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.95f, 1f);
            colors.fadeDuration = M2Ease.Hover;
            button.colors = colors;

            M2Motion motion = EsportsUIKit.AddPressMotion(button);
            motion.SetEntrance(delay);
            if (glow) motion.SetHoverGlow(glow, 0f, 0.45f);

            Module2ExerciseEntry entry = root.gameObject.AddComponent<Module2ExerciseEntry>();
            SerializedObject so = new(entry);
            so.FindProperty("sceneName").stringValue = spec.sceneName;
            so.FindProperty("fuzzyLevel").objectReferenceValue = spec.level;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(button.onClick, entry.Load);
            return entry;
        }
    }
}
