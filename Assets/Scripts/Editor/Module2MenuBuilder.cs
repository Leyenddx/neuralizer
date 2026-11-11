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

            EditorUtility.SetDirty(wrapperGO);
            EditorSceneManager.MarkSceneDirty(wrapperGO.scene);
            Debug.Log("Module2MenuBuilder: listo. Guarda la escena (Ctrl+S) para persistir los cambios.");
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

            RectTransform captionRT = EsportsUIKit.CreateRect("Caption", row);
            captionRT.gameObject.AddComponent<LayoutElement>().preferredWidth = 96f;
            EsportsUIKit.CreateTextFill(captionRT, "MÓDULO", 14f, EsportsTheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Left);

            Button tab1 = EsportsUIKit.CreateGhostButton(row, "1", 56f, 40f);
            Button tab2 = EsportsUIKit.CreateGhostButton(row, "2", 56f, 40f);

            ModuleTabController controller = row.gameObject.AddComponent<ModuleTabController>();
            SerializedObject so = new(controller);
            so.FindProperty("module1Panel").objectReferenceValue = module1Panel;
            so.FindProperty("module2Panel").objectReferenceValue = module2Panel;
            so.FindProperty("module2Background").objectReferenceValue = module2Background;
            so.FindProperty("module1Logo").objectReferenceValue = module1Logo;
            so.FindProperty("module2Logo").objectReferenceValue = module2Logo;
            so.FindProperty("tab1Background").objectReferenceValue = tab1.GetComponent<Image>();
            so.FindProperty("tab2Background").objectReferenceValue = tab2.GetComponent<Image>();
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
            RectTransform root = EsportsUIKit.CreateRect("Module2Background", mainMenuCanvas);
            EsportsUIKit.Stretch(root);
            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = Color.white;
            bg.raycastTarget = false;
            EsportsUIKit.AddGradient(bg, EsportsTheme.NeonBlue, EsportsTheme.NeonPurple, UIGradient.Direction.Diagonal);
            root.SetSiblingIndex(bgSibling.GetSiblingIndex() + 1);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        /// <summary>
        /// PLACEHOLDER: logo del Módulo 2 como hermano del logo actual dentro de "Logo Panel". El
        /// usuario va a reemplazar esto por su propio arte — este método (o el GameObject "Module2Logo"
        /// directo en la escena) es lo único que hay que tocar cuando llegue ese arte final. Empieza
        /// apagado; se enciende junto con <see cref="BuildModule2Background"/> al seleccionar el Módulo 2.
        /// </summary>
        private static GameObject BuildModule2Logo(Transform logoPanel) {
            RectTransform root = EsportsUIKit.CreateRect("Module2Logo", logoPanel);
            EsportsUIKit.Stretch(root);
            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = Color.white;
            EsportsUIKit.AddGradient(bg, EsportsTheme.NeonBlue, EsportsTheme.NeonPurple, UIGradient.Direction.Horizontal);
            EsportsUIKit.CreateTextFill(root, "MÓDULO 2\n(placeholder — reemplazar por el logo del módulo)",
                20f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static GameObject BuildModule2Panel(Transform parent) {
            RectTransform panel = EsportsUIKit.CreateRect("Module2ContentPanel", parent);
            // Mismo trato que "Content Panel": un LayoutElement flexible dentro del VerticalLayoutGroup
            // de Wrapper para que ocupe el espacio restante debajo de App Bar / Logo Panel.
            panel.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            // "panel" no tiene su propio LayoutGroup, así que su hijo (el área con scroll) debe
            // estirarse a mano para llenarlo — un LayoutElement no serviría aquí sin un LayoutGroup
            // en el padre que lo controle.
            RectTransform scrollContent = EsportsUIKit.CreateScrollArea(panel, 0f);
            RectTransform scrollRoot = (RectTransform)scrollContent.parent.parent;
            EsportsUIKit.Stretch(scrollRoot);

            BuildExerciseEntry(scrollContent, "Ejercicio 1", "El Dilema del Healer · Fuzzy logic", "FuzzyDemo");
            BuildExerciseEntry(scrollContent, "Ejercicio 2", "Lectura visual del daño · Fuzzy logic", "FuzzyDamageDemo");
            BuildExerciseEntry(scrollContent, "Ejercicio 3", "¿A quién curás primero? · Fuzzy logic", "FuzzyComparisonDemo");

            return panel.gameObject;
        }

        private static void BuildExerciseEntry(Transform parent, string title, string subtitle, string sceneName) {
            RectTransform content = EsportsUIKit.CreateAccentCard(parent, "ExerciseEntry", EsportsTheme.NeonBlue, out _, 2f, 6f);
            RectTransform root = (RectTransform)content.parent.parent;
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 84f;

            VerticalLayoutGroup textLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayout.childControlHeight = true;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childAlignment = TextAnchor.MiddleLeft;

            EsportsUIKit.CreateText(content, title, 18f, EsportsTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Left, 26f);
            EsportsUIKit.CreateText(content, subtitle, 14f, EsportsTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Left, 20f);

            Button button = root.gameObject.AddComponent<Button>();
            // El halo/borde tienen raycastTarget=false a propósito (ver EsportsUIKit); "Bg" es el
            // único hijo que sí recibe el click y el que debe mostrar el tinte de hover/press.
            button.targetGraphic = root.Find("Bg").GetComponent<Image>();

            Module2ExerciseEntry entry = root.gameObject.AddComponent<Module2ExerciseEntry>();
            SerializedObject so = new(entry);
            so.FindProperty("sceneName").stringValue = sceneName;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(button.onClick, entry.Load);
        }
    }
}
