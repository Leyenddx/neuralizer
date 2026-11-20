using System;
using System.Collections.Generic;
using System.IO;
using Neuralizer.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Neuralizer.EditorTools {

  
    public static class Module2AssetGenerator {

        private const string SpriteFolder = "Assets/Resources/Sprites/Module2/UI";
        private const string FontSetFolder = "Assets/Resources/Module2";
        private const string FontSetPath = FontSetFolder + "/EsportsFonts.asset";

        /// <summary>Tamaño del recorte diagonal de las esquinas (px del sprite).</summary>
        private const int Cut = 18;
        private const int PanelSize = 96;
        private const int GlowMargin = 28;

        [MenuItem("Tools/Neuralizer/Módulo 2/1. Recursos visuales (sprites y fuentes)", false, 20)]
        public static void Generate() {
            EnsureFolder(SpriteFolder);
            EnsureFolder(FontSetFolder);

            List<(string name, Texture2D tex, Vector4 border, bool tiled)> items = new() {
                ("M2_ChamferFill", ChamferPanel(PanelSize, 0, PanelMode.Fill), Border(Cut + 6), false),
                ("M2_ChamferLine", ChamferPanel(PanelSize, 0, PanelMode.Line), Border(Cut + 6), false),
                ("M2_ChamferGlow", ChamferPanel(PanelSize + GlowMargin * 2, GlowMargin, PanelMode.Glow), Border(GlowMargin + Cut + 6), false),
                ("M2_Bracket", Bracket(32), Vector4.zero, false),
                ("M2_Stripes", Stripes(32), Vector4.zero, true),
                ("M2_Grid", Grid(64), Vector4.zero, true),
                ("M2_GlowDot", GlowDot(256), Vector4.zero, false),
                ("M2_Vignette", Vignette(256), Vector4.zero, false),
                ("M2_Chevron", Icon(64, IconKind.Chevron), Vector4.zero, false),
                ("M2_Back", Icon(64, IconKind.Back), Vector4.zero, false),
                ("M2_Check", Icon(64, IconKind.Check), Vector4.zero, false),
                ("M2_Play", Icon(64, IconKind.Play), Vector4.zero, false),
                ("M2_Solid", Rasterize(8, 8, (_, _) => 1f), Vector4.zero, false),
            };

            foreach (var (name, tex, border, tiled) in items) {
                string path = $"{SpriteFolder}/{name}.png";
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                ConfigureImporter(path, border, tiled);
            }

            CreateFontSet();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Module2AssetGenerator] {items.Count} sprites generados en {SpriteFolder} y set de fuentes en {FontSetPath}.");
        }

        // -----------------------------------------------------------------------------------------
        // Importación
        // -----------------------------------------------------------------------------------------

        private static void ConfigureImporter(string path, Vector4 border, bool tiled) {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            // Tight genera una malla recortada que rompe Tiled/Sliced — siempre rectángulo completo.
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static Vector4 Border(int px) => new(px, px, px, px);

        private static void EnsureFolder(string path) {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // -----------------------------------------------------------------------------------------
        // Fuentes
        // -----------------------------------------------------------------------------------------

        private static void CreateFontSet() {
            EsportsFontSet set = AssetDatabase.LoadAssetAtPath<EsportsFontSet>(FontSetPath);
            if (!set) {
                set = ScriptableObject.CreateInstance<EsportsFontSet>();
                AssetDatabase.CreateAsset(set, FontSetPath);
            }

            TMP_FontAsset body = LoadFont("Assets/Fonts/Lexend.asset");
            set.body = body;
            set.heading = Or(LoadFont("Assets/Fonts/Lexend/Lexend-SemiBold SDF.asset"), body);
            set.display = Or(LoadFont("Assets/Fonts/Lexend/Lexend-ExtraBold SDF.asset"), set.heading);
            set.mono = Or(LoadFont("Assets/Fonts/Noto Sans Mono.asset"), body);

            // Los pesos de Lexend no traen respaldo: si falta un glifo (¿, μ, →) caerían a un cuadro.
            // Les damos Lexend Regular como respaldo, que a su vez ya respalda en LiberationSans.
            AddFallback(set.heading, body);
            AddFallback(set.display, body);
            AddFallback(set.mono, body);
            EditorUtility.SetDirty(set);
        }

        private static TMP_FontAsset Or(TMP_FontAsset preferred, TMP_FontAsset fallback) => preferred ? preferred : fallback;

        private static TMP_FontAsset LoadFont(string path) {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (!font) Debug.LogWarning("[Module2AssetGenerator] No se encontró la fuente " + path);
            return font;
        }

        private static void AddFallback(TMP_FontAsset font, TMP_FontAsset fallback) {
            if (!font || !fallback || font == fallback) return;
            font.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
            if (font.fallbackFontAssetTable.Contains(fallback)) return;
            font.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(font);
        }

        // -----------------------------------------------------------------------------------------
        // Rasterización (SDF)
        // -----------------------------------------------------------------------------------------

        private enum PanelMode { Fill, Line, Glow }

        /// <summary>
        /// Panel con las esquinas superior-izquierda e inferior-derecha recortadas en diagonal — la
        /// asimetría es lo que le da el aire "táctico" sin que parezca una caja genérica.
        /// </summary>
        private static Texture2D ChamferPanel(int size, int margin, PanelMode mode) {
            float lo = margin + 1f;
            float hi = size - margin - 1f;
            Vector2[] poly = {
                new(lo, lo),
                new(hi - Cut, lo),
                new(hi, lo + Cut),
                new(hi, hi),
                new(lo + Cut, hi),
                new(lo, hi - Cut),
            };

            return Rasterize(size, size, (x, y) => {
                float d = ConvexDistance(poly, new Vector2(x, y));
                return mode switch {
                    PanelMode.Fill => Coverage(d),
                    PanelMode.Line => Coverage(Mathf.Abs(d + 1f) - 1f),
                    _ => d <= 0f ? 1f : Mathf.Exp(-(d * d) / (2f * 9f * 9f)) * 0.9f
                };
            });
        }

        /// <summary>Corchete en "L" para la esquina superior izquierda (se rota/espeja desde la UI).</summary>
        private static Texture2D Bracket(int size) {
            const float t = 3f;
            float len = size * 0.55f;
            return Rasterize(size, size, (x, y) => {
                float top = size - 1f;
                float horizontal = (x <= len && y >= top - t) ? 1f : 0f;
                float vertical = (x <= t && y >= top - len) ? 1f : 0f;
                return Mathf.Max(horizontal, vertical);
            });
        }

        /// <summary>Franjas a 45° (tile sin costura: el período divide al tamaño).</summary>
        private static Texture2D Stripes(int size) {
            const float period = 16f;
            const float width = 6f;
            return Rasterize(size, size, (x, y) => {
                float u = Mathf.Repeat(x + y, period);
                float edge = Mathf.Min(u, period - u) - (period - width) * 0.5f;
                return Mathf.Clamp01(0.5f + edge);
            });
        }

        /// <summary>Retícula: línea de 1 px arriba y a la izquierda de cada celda + punto en el cruce.</summary>
        private static Texture2D Grid(int size) {
            return Rasterize(size, size, (x, y) => {
                bool line = x < 1f || y < 1f;
                bool dot = x < 3f && y < 3f;
                return dot ? 1f : line ? 0.55f : 0f;
            });
        }

        private static Texture2D GlowDot(int size) {
            float r = size * 0.5f;
            return Rasterize(size, size, (x, y) => {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r)) / r;
                float f = Mathf.Clamp01(1f - d);
                return f * f * (3f - 2f * f) * f;
            });
        }

        private static Texture2D Vignette(int size) {
            float r = size * 0.5f;
            float maxD = Mathf.Sqrt(2f) * r;
            return Rasterize(size, size, (x, y) => {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r)) / maxD;
                float t = Mathf.InverseLerp(0.45f, 1f, d);
                return t * t * (3f - 2f * t);
            });
        }

        private enum IconKind { Chevron, Back, Check, Play }

        private static Texture2D Icon(int size, IconKind kind) {
            const float stroke = 3.6f;
            return Rasterize(size, size, (x, y) => {
                Vector2 p = new(x, y);
                switch (kind) {
                    case IconKind.Chevron:
                        return Coverage(Polyline(p, new(24, 14), new(42, 32), new(24, 50)) - stroke);
                    case IconKind.Back:
                        return Coverage(Polyline(p, new(40, 14), new(22, 32), new(40, 50)) - stroke);
                    case IconKind.Check:
                        return Coverage(Polyline(p, new(14, 33), new(27, 20), new(50, 45)) - stroke);
                    default:
                        Vector2[] tri = { new(22, 14), new(52, 32), new(22, 50) };
                        return Coverage(ConvexDistance(tri, p) - 0.5f);
                }
            });
        }

        // -----------------------------------------------------------------------------------------
        // Utilidades geométricas
        // -----------------------------------------------------------------------------------------

        /// <summary>Rellena una textura blanca evaluando la cobertura en el centro de cada píxel.</summary>
        private static Texture2D Rasterize(int w, int h, Func<float, float, float> alphaAt) {
            Texture2D tex = new(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    float a = Mathf.Clamp01(alphaAt(x + 0.5f, y + 0.5f));
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);
            return tex;
        }

        /// <summary>Distancia con signo → cobertura de un píxel (antialias de 1 px).</summary>
        private static float Coverage(float signedDistance) => Mathf.Clamp01(0.5f - signedDistance);

        /// <summary>
        /// Distancia con signo (positiva afuera) a un polígono convexo: el máximo de las distancias a
        /// cada arista. Exacta adentro y muy cerca del borde, que es lo que importa para el antialias;
        /// lejos de los vértices subestima un poco (da halos de esquinas rectas, que encaja con el estilo).
        /// </summary>
        private static float ConvexDistance(Vector2[] poly, Vector2 p) {
            float area = 0f;
            for (int i = 0; i < poly.Length; i++) {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
                area += a.x * b.y - b.x * a.y;
            }
            float orientation = Mathf.Sign(area);

            float d = float.MinValue;
            for (int i = 0; i < poly.Length; i++) {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
                Vector2 edge = (b - a).normalized;
                Vector2 outward = new Vector2(edge.y, -edge.x) * orientation;
                d = Mathf.Max(d, Vector2.Dot(p - a, outward));
            }
            return d;
        }

        private static float Polyline(Vector2 p, params Vector2[] points) {
            float d = float.MaxValue;
            for (int i = 0; i < points.Length - 1; i++) {
                d = Mathf.Min(d, SegmentDistance(p, points[i], points[i + 1]));
            }
            return d;
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b) {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
