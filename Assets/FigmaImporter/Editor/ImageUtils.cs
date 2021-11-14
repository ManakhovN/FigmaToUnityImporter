using System;
using System.IO;
using System.Threading.Tasks;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
#if VECTOR_GRAHICS_IMPORTED
using Unity.VectorGraphics;
#endif

namespace FigmaImporter.Editor
{
    public class ImageUtils
    {
        public static void AddOverridenSprite(GameObject nodeGo, Sprite overridenSprite)
        {
            var image = nodeGo.AddComponent<Image>();
            image.sprite = overridenSprite;
        }
        
#if VECTOR_GRAHICS_IMPORTED
        public static void AddOverridenSvgSprite(GameObject nodeGo, Sprite overridenSprite)
        {
            var image = nodeGo.AddComponent<SVGImage>();
            image.sprite = overridenSprite;
        }

        public static async Task RenderSvgNodeAndApply(Node node, GameObject nodeGo, FigmaImporter importer)
        {
            FigmaNodesProgressInfo.CurrentInfo = "Loading image";
            FigmaNodesProgressInfo.ShowProgress(0f);
            var result = await importer.GetSvgImage(node.id);
            string spriteName = $"{node.name}_{node.id.Replace(':', '_')}.svg";
            var destinationPath = $"/{importer.GetRendersFolderPath()}/{spriteName}";
            SaveSvgTexture(result,destinationPath);
            using (var stream = new StreamReader(Application.dataPath + destinationPath))
                 SVGParser.ImportSVG(stream, ViewportOptions.DontPreserve, 0, 1, 100, 100);
            var t = nodeGo.transform as RectTransform;
            
            SVGImage image = null;
            Sprite sprite = null;
            FigmaNodesProgressInfo.CurrentInfo = "Saving rendered node";
            FigmaNodesProgressInfo.ShowProgress(0f);
            try
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets{destinationPath}");
                image = nodeGo.AddComponent<SVGImage>();
                image.sprite = sprite;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
        
        private static void SaveSvgTexture(byte[] bytes, string path)
        {
             var filePath = Application.dataPath + path;
             System.IO.File.WriteAllBytes(filePath, bytes);
             UnityEditor.AssetDatabase.Refresh();
            
        }

#endif
        
        public static Sprite ChangeTextureToSprite(string path)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void SaveTexture(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            if (bytes != null)
            {
                var filePath = Application.dataPath + path;
                System.IO.File.WriteAllBytes(filePath, bytes);
                UnityEditor.AssetDatabase.Refresh();
            }
        }

        public static void SetMask(Node node, GameObject nodeGo)
        {
            if (!node.clipsContent)
                return;
            if (node.fills.Length == 0)
                nodeGo.AddComponent<RectMask2D>();
            else
                nodeGo.AddComponent<Mask>();
        }
    }
}