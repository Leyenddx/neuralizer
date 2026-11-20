using UnityEditor;
using UnityEngine;

namespace Neuralizer.EditorTools {

 
    public static class Module2SpriteImporter {

        private const string FolderPath = "Assets/Resources/Sprites/Module2";

        private static readonly (string file, Vector4 border)[] SlicedSprites = {
  
            ("Module2_CardFill.png", new Vector4(60, 40, 60, 90)),
            ("Module2_Banner.png", new Vector4(60, 10, 60, 10)),
        };

        private static readonly string[] SimpleSprites = {
            "Module2_ButtonBack.png",
            "Module2_ButtonFullscreen.png",
        };

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
                importer.mipmapEnabled = false; 
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
