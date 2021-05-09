using UnityEditor;
using UnityEngine;

namespace FigmaImporter.Editor
{
    public class FigmaImporterSettings : ScriptableObject
    {
        [SerializeField] private string clientCode = null;
        [SerializeField] private string state = null;
        [SerializeField] private string token = null;
        [SerializeField] private string url = null;
        [SerializeField] private string rendersPath = "FigmaImporter/Renders";
        
        public string ClientCode
        {
            get => clientCode;
            set => clientCode = value;
        }

        public string State
        {
            get => state;
            set => state = value;
        }

        public string Token
        {
            get => token;
            set => token = value;
        }

        public string Url
        {
            get => url;
            set => url = value;
        }

        public string RendersPath
        {
            get => rendersPath;
            set => rendersPath = value;
        }

        public static FigmaImporterSettings GetInstance()
        {
            FigmaImporterSettings result = null;
            var assets = AssetDatabase.FindAssets("t:FigmaImporterSettings");
            if (assets == null || assets.Length == 0)
            {
                result = CreateInstance<FigmaImporterSettings>();
                AssetDatabase.CreateAsset(result, "Assets/FigmaImporter/Editor/FigmaImporterSettings.asset");
                AssetDatabase.Refresh();
            }
            else
            {
                
                var assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
                result = AssetDatabase.LoadAssetAtPath<FigmaImporterSettings>(assetPath);
            }

            return result;
        }
    }
}
