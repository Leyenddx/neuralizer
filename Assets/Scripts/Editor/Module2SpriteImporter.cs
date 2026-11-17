using UnityEditor;
using UnityEngine;

namespace Neuralizer.EditorTools {

    /// <summary>
    /// Configura los PNG/JPG del mockup del Módulo 2 (<c>Assets/Resources/Sprites/Module2/</c>) como
    /// Sprites usables en UI — por default Unity los importa como "Default" (textura 3D), no como
    /// "Sprite (2D and UI)", así que sin este paso <c>Image.sprite</c> no los acepta. Idempotente:
    /// correrlo varias veces no hace nada raro, solo deja los import settings en el estado esperado.
    /// </summary>
    /// <remarks>
    /// Viven bajo Resources/ (no en Assets/Sprites/, donde está el resto del arte del proyecto, que
    /// se empaqueta en Sprite Atlases) porque la pantalla del Módulo 2 construye toda su UI por código
    /// en runtime (ver FuzzyHealerDemo.cs) sin prefabs ni referencias [SerializeField] — Resources.Load
    /// es la única forma de que ese código cargue un Sprite por nombre y siga funcionando en el build
    /// de WebGL, no solo en el editor.
    /// </remarks>
    public static class Module2SpriteImporter {

        private const string FolderPath = "Assets/Resources/Sprites/Module2";

        // Bordes de 9-slice (left, bottom, right, top en píxeles de la textura original) para las
        // piezas que se van a estirar en vez de mostrarse a tamaño fijo — lo suficientemente grandes
        // para dejar fuera del área que se estira las esquinas/decoraciones (brackets, muescas,
        // franjas diagonales) de cada arte y que no se deformen.
        private static readonly (string file, Vector4 border)[] SlicedSprites = {
            // Marco del mockup con el interior relleno de azul marino translúcido
            // en vez de transparente — ver BuildBottomSheet: reemplaza el panel rectangular plano que
            // se usaba detrás de la tarjeta (sus esquinas cuadradas se salían del contorno redondeado
            // del marco, mostrando un bloque oscuro que no está en el mockup). Mismo border de 9-slice
            // porque viene del mismo arte fuente, solo cambia el color/alpha del relleno.
            ("Module2_CardFill.png", new Vector4(60, 40, 60, 90)),
            ("Module2_Banner.png", new Vector4(60, 10, 60, 10)),
        };

        private static readonly string[] SimpleSprites = {
            "Module2_ButtonBack.png",
            "Module2_ButtonFullscreen.png",
        };

        // Retratos de personajes (Ejercicio 2 — Lectura visual del daño): arte con licencia provisto
        // por el usuario, ya sin fondo (alpha real, no un recorte cuadrado), así que llevan el mismo
        // trato que SimpleSprites (sin 9-slice) pero desde la subcarpeta Heroes/ y con preserveAspect
        // en true al usarlos (eso se configura en el Image que los consume, no acá).
        private const string HeroFolderPath = "Assets/Resources/Sprites/Module2/Heroes";
        private static readonly string[] HeroSprites = {
            "Hero_Reaper.png",
            "Hero_Reinhardt.png",
            "Hero_Soldier76.png",
        };

        [MenuItem("Tools/Neuralizer/Módulo 2/Configurar sprites del mockup", false, 41)]
        public static void ConfigureSprites() {
            int configured = 0;

            foreach ((string file, Vector4 border) in SlicedSprites) {
                if (ConfigureOne(FolderPath, file, border)) configured++;
            }
            foreach (string file in SimpleSprites) {
                if (ConfigureOne(FolderPath, file, Vector4.zero)) configured++;
            }
            foreach (string file in HeroSprites) {
                if (ConfigureOne(HeroFolderPath, file, Vector4.zero)) configured++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Module2SpriteImporter] Configurados {configured} archivo(s) como Sprite (2D and UI) (incluye {FolderPath} y {HeroFolderPath}).");
        }

        private static bool ConfigureOne(string folder, string fileName, Vector4 border) {
            string path = $"{folder}/{fileName}";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) {
                Debug.LogWarning($"[Module2SpriteImporter] No se encontró (o no es textura): {path}");
                return false;
            }

            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite) {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single) {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (importer.mipmapEnabled) {
                importer.mipmapEnabled = false; // es UI en pantalla, no hace falta
                changed = true;
            }
            if (importer.alphaIsTransparency == false && fileName.EndsWith(".png")) {
                importer.alphaIsTransparency = true;
                changed = true;
            }
            if (importer.spriteBorder != border) {
                importer.spriteBorder = border;
                changed = true;
            }
            if (importer.filterMode != FilterMode.Bilinear) {
                importer.filterMode = FilterMode.Bilinear;
                changed = true;
            }
            // Estas piezas son líneas finas (bordes de 1-3px) sobre transparencia — la compresión por
            // default (DXT/Crunch) difumina justo ese tipo de borde de alto contraste. Sin compresión
            // se ven nítidas a cualquier escala; son pocos archivos chicos, el costo en build es mínimo.
            // Los retratos de personaje quedan fuera (son imágenes grandes tipo foto: ahí sí vale la
            // pena comprimir en vez de dejarlas sin comprimir).
            bool isHeroPortrait = System.Array.Exists(HeroSprites, f => f == fileName);
            if (!isHeroPortrait && importer.textureCompression != TextureImporterCompression.Uncompressed) {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            else if (isHeroPortrait && importer.textureCompression != TextureImporterCompression.Compressed) {
                importer.textureCompression = TextureImporterCompression.Compressed;
                changed = true;
            }

            if (changed) {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
            return true;
        }
    }
}
