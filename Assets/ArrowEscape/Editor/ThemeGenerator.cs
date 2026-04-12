using UnityEngine;
using UnityEditor;
using Core;
using System.IO;
using System.Collections.Generic;

namespace EditorTools
{
    public class ThemeGenerator : EditorWindow
    {
        [MenuItem("Tools/Arrow Escape/Generate Example Themes")]
        public static void GenerateThemes()
        {
            string folderPath = "Assets/ArrowEscape/Themes";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Generate Soft Glow Texture
            Texture2D softGlowTexture = GenerateSoftGlowTexture();
            string texturePath = folderPath + "/SoftGlow.png";
            SaveTexture(softGlowTexture, texturePath);
            
            // Create Material for Soft Glow
            Material softGlowMaterial = new Material(Shader.Find("Sprites/Default"));
            softGlowMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            AssetDatabase.CreateAsset(softGlowMaterial, folderPath + "/SoftGlowMaterial.mat");

            // Load Resources
            Sprite arrowHead = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArrowEscape/Sprites_Ui/Arrow.png");
            Sprite circleBody = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArrowEscape/GameFx/Hyper Casual FX/Textures/Circle01.png");

            if (arrowHead == null) Debug.LogWarning("Could not find Arrow.png at expected path.");
            if (circleBody == null) Debug.LogWarning("Could not find Circle01.png at expected path.");

            // 1. Classic Line Theme
            ArrowTheme classic = ScriptableObject.CreateInstance<ArrowTheme>();
            classic.visualType = ArrowVisualType.LineRenderer;
            classic.headSprite = arrowHead;
            classic.headScale = 1.0f;
            classic.overrideColor = false;
            // Default layer with useArrowColor
            classic.lineLayers.Add(new LineLayer {
                layerName = "Core",
                width = 0.4f,
                endWidth = -1f,
                color = Color.white,
                useArrowColor = true, // Uses arrow's dynamic color
                textureMode = LineTextureMode.Stretch,
                sortingOrderOffset = 0
            });
            CreateAsset(classic, folderPath + "/Theme_ClassicLine.asset");

            // 2. Sprite Snake Theme
            ArrowTheme spriteSnake = ScriptableObject.CreateInstance<ArrowTheme>();
            spriteSnake.visualType = ArrowVisualType.SpriteBody;
            spriteSnake.headSprite = arrowHead;
            spriteSnake.headScale = 1.2f;
            spriteSnake.bodySprite = circleBody;
            spriteSnake.bodyScale = 0.8f;
            spriteSnake.rotateBodySprites = false;
            spriteSnake.overrideColor = false;
            CreateAsset(spriteSnake, folderPath + "/Theme_SpriteSnake.asset");

            // 3. Thick Line Theme (Configurable Line)
            ArrowTheme thickLine = ScriptableObject.CreateInstance<ArrowTheme>();
            thickLine.visualType = ArrowVisualType.LineRenderer;
            thickLine.headSprite = arrowHead;
            thickLine.headScale = 1.2f;
            thickLine.overrideColor = true;
            thickLine.themeColor = Color.yellow;
            thickLine.lineLayers.Add(new LineLayer {
                layerName = "Thick",
                width = 0.8f,
                endWidth = -1f,
                color = Color.yellow,
                useArrowColor = false,
                textureMode = LineTextureMode.Stretch,
                sortingOrderOffset = 0
            });
            CreateAsset(thickLine, folderPath + "/Theme_ThickLine.asset");

            // 4. Neon Glow Theme (Advanced) - WITH SOFT GLOW TEXTURE
            ArrowTheme neonGlow = ScriptableObject.CreateInstance<ArrowTheme>();
            neonGlow.visualType = ArrowVisualType.LineRenderer;
            neonGlow.headSprite = arrowHead;
            neonGlow.headScale = 1.0f;
            neonGlow.overrideColor = false; // Allow dynamic colors
            neonGlow.lineSmoothness = 10; // Smoother
            
            // Glow Layer (Behind) - Uses Soft Glow Texture
            neonGlow.lineLayers.Add(new LineLayer {
                layerName = "Glow",
                width = 1.5f,
                endWidth = 0.3f, // Tapered Glow
                color = new Color(1, 1, 1, 0.5f), // Semi-transparent white (will multiply with arrow color)
                useArrowColor = true, // Glow matches arrow color
                material = softGlowMaterial,
                textureMode = LineTextureMode.Stretch,
                sortingOrderOffset = -1
            });
            // Core Layer (Front)
            neonGlow.lineLayers.Add(new LineLayer {
                layerName = "Core",
                width = 0.3f,
                endWidth = 0f, // Taper to point
                color = Color.white,
                useArrowColor = true, // Core uses arrow color
                textureMode = LineTextureMode.Stretch,
                sortingOrderOffset = 0
            });
            CreateAsset(neonGlow, folderPath + "/Theme_NeonGlow.asset");

            // 5. Bordered Theme (Advanced) - Uses Arrow Color for Core
            ArrowTheme bordered = ScriptableObject.CreateInstance<ArrowTheme>();
            bordered.visualType = ArrowVisualType.LineRenderer;
            bordered.headSprite = arrowHead;
            bordered.headScale = 1.0f;
            bordered.overrideColor = false; // Allow dynamic colors
            bordered.showHeadOutline = true;
            bordered.headOutlineColor = Color.black;
            bordered.headOutlineScale = 1.3f;
            
            // Border Layer (Behind) - Fixed Black
            bordered.lineLayers.Add(new LineLayer {
                layerName = "Border",
                width = 0.6f,
                endWidth = 0.6f,
                color = Color.black,
                useArrowColor = false, // Fixed black border
                textureMode = LineTextureMode.Stretch,
                sortingOrderOffset = -1
            });
            // Core Layer (Front) - Uses Arrow Color
            bordered.lineLayers.Add(new LineLayer {
                layerName = "Core",
                width = 0.3f,
                endWidth = 0.3f,
                color = Color.white,
                useArrowColor = true, // Core uses arrow's dynamic color
                textureMode = LineTextureMode.Stretch,
                sortingOrderOffset = 0
            });
            CreateAsset(bordered, folderPath + "/Theme_Bordered.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = neonGlow;
            
            Debug.Log("Example Themes Generated in " + folderPath);
        }

        private static Texture2D GenerateSoftGlowTexture()
        {
            int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            
            Color[] colors = new Color[size * size];
            
            for (int y = 0; y < size; y++)
            {
                // Calculate distance from center line (y = 32)
                float distFromCenter = Mathf.Abs(y - 32f) / 32f; // 0 at center, 1 at edges
                
                // Soft falloff using smoothstep
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, distFromCenter);
                
                for (int x = 0; x < size; x++)
                {
                    colors[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return texture;
        }

        private static void SaveTexture(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);
            
            // Set texture import settings
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void CreateAsset(Object asset, string path)
        {
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
