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

   
    public static class Module2MenuBuilder {

        private const string WrapperPath = "UI/Main Menu Canvas/Safe Area/Wrapper";
        private const string Module1PanelName = "Content Panel";

        [MenuItem("Tools/Neuralizer/Módulo 2/5. Menú en MainScene (construir)", false, 24)]
        public static void Build() {
            GameObject wrapperGO = GameObject.Find(WrapperPath);
            if (!wrapperGO) {
                Debug.LogError($"Module2MenuBuilder: no se encontró '{WrapperPath}'. ¿Está abierta MainScene?");
                return;
            }
            Transform wrapper = wrapperGO.transform;
        
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


            Transform appBar = wrapper.Find("App Bar");
            if (appBar) appBar.SetAsLastSibling();

            CollectAppBarGraphics(appBar, out Graphic[] appBarSurfaces, out Graphic[] appBarIcons);

            GameObject moduleTabs = BuildModuleTabs(wrapper, module1Panel.gameObject, module2Panel,
                module2Background, logoImage ? logoImage.gameObject : null, module2Logo,
                appBarSurfaces, appBarIcons);
            moduleTabs.transform.SetAsFirstSibling();
     
            moduleTabs.SetActive(false);
            HookStartButton(mainMenuCanvas, moduleTabs);

    
            if (!mainMenuCanvas.GetComponent<MainMenuReturnHandler>()) {
                mainMenuCanvas.gameObject.AddComponent<MainMenuReturnHandler>();
            }


            CanvasScaler menuScaler = mainMenuCanvas.GetComponentInParent<CanvasScaler>();
            float slicedScale = menuScaler ? menuScaler.referencePixelsPerUnit / 100f : 1f;
            foreach (GameObject built in new[] { module2Panel, moduleTabs, module2Background, module2Logo }) {
                if (built) CompensateSlicedScale(built.transform, slicedScale);
            }

            EditorUtility.SetDirty(wrapperGO);
            EditorSceneManager.MarkSceneDirty(wrapperGO.scene);
            Debug.Log("Module2MenuBuilder: listo. Guarda la escena (Ctrl+S) para persistir los cambios.");
        }

        [MenuItem("Tools/Neuralizer/Módulo 2/Quitar menú de MainScene", false, 40)]
        public static void Remove() {
            GameObject wrapperGO = GameObject.Find(WrapperPath);
            if (!wrapperGO) return;
            RemoveExisting(wrapperGO.transform, wrapperGO.transform.parent.parent);
            EditorSceneManager.MarkSceneDirty(wrapperGO.scene);
        }

        private static void RemoveExisting(Transform wrapper, Transform mainMenuCanvas) {
            Transform existingTabs = wrapper.Find("ModuleTabs");
            if (existingTabs) {
         
                UnhookStartButton(mainMenuCanvas, existingTabs.gameObject);
                Object.DestroyImmediate(existingTabs.gameObject);
            }
            Transform existingPanel = wrapper.Find("Module2ContentPanel");
            if (existingPanel) Object.DestroyImmediate(existingPanel.gameObject);

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


        private static void SetGraphicArray(SerializedProperty arrayProp, Graphic[] values) {
            arrayProp.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) {
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }


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
   
            rowLE.layoutPriority = 2;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            rowLE.preferredHeight = 48f;

            TextMeshProUGUI caption = EsportsUIKit.CreateOverline(row, "Módulo", EsportsTheme.TextSecondary, MenuSize.Small);
            caption.gameObject.AddComponent<LayoutElement>().preferredWidth = 96f;

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


        private static GameObject BuildModule2Background(Transform mainMenuCanvas, Transform bgSibling) {

            RectTransform root = EsportsUIKit.CreateAmbientBackground(mainMenuCanvas, "Module2Background");
            root.SetSiblingIndex(bgSibling.GetSiblingIndex() + 1);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

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
    
            LayoutElement panelLE = panel.gameObject.AddComponent<LayoutElement>();
            panelLE.flexibleHeight = 3000f;
            panelLE.minHeight = 380f;

 
            RectTransform list = EsportsUIKit.CreateScrollArea(panel, 0f);
            RectTransform scrollRoot = (RectTransform)list.parent.parent;
            EsportsUIKit.Stretch(scrollRoot);
            VerticalLayoutGroup listLayout = list.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 10f;
            listLayout.padding = new RectOffset(4, 4, 6, 24);

            float delay = 0f;
            BuildHeader(list, delay, out TextMeshProUGUI progressLabel, out Image progressFill);


            FuzzyScene tema1Exercise = AssetDatabase.LoadAssetAtPath<FuzzyScene>(Module2Tema1AssetsBuilder.LevelPath);
            if (!tema1Exercise) {
                Debug.LogWarning("Module2MenuBuilder: falta " + Module2Tema1AssetsBuilder.LevelPath +
                                 " — corre 'Tools → Neuralizer → Módulo 2 → 2. Tema 1 · Ejercicio (Distancia)'.");
            }
            List<EntrySpec> tema1 = new() {
                new EntrySpec { index = "1.1", title = "Estado de salud", subtitle = "Overwatch · ¿Fine, Caution o Critical?",
                    chip = "EJEMPLO", sceneName = Module2Tema1ExampleBuilder.SceneName, accent = EsportsTheme.NeonBlue },
                new EntrySpec { index = "1.2", title = "Distancia del enemigo", subtitle = "Fortnite · ¿Cerca, medio o lejos?",
                    chip = "EJERCICIO", sceneName = Module2LevelSceneBuilder.SceneName, accent = EsportsTheme.NeonPurple, level = tema1Exercise },
            };

            List<Module2ExerciseEntry> entries = new();
            List<GameObject> badges = new();

            delay += M2Ease.Stagger;
            M2Motion.AddEntrance(EsportsUIKit.CreateSectionHeader(list, "Tema 1 · No todo es verdad o mentira", EsportsTheme.NeonBlue).gameObject, delay);
            foreach (EntrySpec spec in tema1) {
                delay += M2Ease.Stagger;
                entries.Add(BuildExerciseEntry(list, spec, delay, out GameObject badge));
                badges.Add(badge);
            }

            delay += M2Ease.Stagger;
            TextMeshProUGUI soon = EsportsUIKit.CreateText(list, "Próximos temas en construcción.", MenuSize.Small,
                EsportsTheme.TextSecondary, FontStyles.Italic, TextAlignmentOptions.Center, 40f);
            M2Motion.AddEntrance(soon.gameObject, delay);

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

    
            RectTransform progressRow = EsportsUIKit.CreateRect("ProgressRow", card);
            HorizontalLayoutGroup h = progressRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            progressRow.gameObject.AddComponent<LayoutElement>().minHeight = 30f;

            EsportsUIKit.CreateOverline(progressRow, "Ejercicios", EsportsTheme.TextSecondary, MenuSize.Caption);

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


        private static Module2ExerciseEntry BuildExerciseEntry(Transform parent, EntrySpec spec, float delay, out GameObject completedBadge) {
            RectTransform content = EsportsUIKit.CreateAccentCard(parent, "Entry_" + spec.index, spec.accent, out _, 1f);
            RectTransform root = (RectTransform)content.parent.parent;
            LayoutElement rootLE = root.gameObject.AddComponent<LayoutElement>();
            rootLE.minHeight = 92f;
            rootLE.preferredHeight = 96f;


            Image glow = null;
            if (EsportsSkin.HasSprites) {
                glow = EsportsUIKit.CreateSpriteImage("Glow", root, EsportsSkin.ChamferGlow, Image.Type.Sliced);
                EsportsUIKit.Stretch(glow.rectTransform, -26f, -26f, -26f, -26f);
                glow.color = EsportsTheme.WithAlpha(spec.accent, 0f);
                glow.raycastTarget = false;
                glow.transform.SetAsFirstSibling();
            }

       
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
            index.gameObject.AddComponent<LayoutElement>().preferredWidth = 52f;

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
