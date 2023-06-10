using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FigmaImporter.Editor.EditorTree.TreeData;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FigmaImporter.Editor
{
    public class FigmaNodeGenerator
    {
        Vector2 offset = Vector2.zero;
        private RectTransform root = null;
        private readonly FigmaImporter _importer;

        public FigmaNodeGenerator(FigmaImporter importer)
        {
            _importer = importer;
        }

        public async Task GenerateNode(Node node, GameObject parent, IList<NodeTreeElement> nodeTreeElements)
        {
            FigmaNodesProgressInfo.CurrentNode ++;
            FigmaNodesProgressInfo.CurrentInfo = "Node generation in progress";
            FigmaNodesProgressInfo.ShowProgress(0f);
            
            //RendersFolderの有無の確認
            GenerateRenderSaveFolder(_importer.GetRendersFolderPath());
            
            var boundingBox = node.absoluteBoundingBox;
            if (parent == null)
            {
                throw new Exception("[FigmaImporter] Parent is null. Set the canvas reference.");
            }

            var isParentCanvas = parent.GetComponent<Canvas>();
            
            if (isParentCanvas)    
                offset = boundingBox.GetPosition();
            
            GameObject nodeGo = null;
            var treeElement = nodeTreeElements.First(x => x.figmaId == node.id);

            if (treeElement.actionType != ActionType.None)
            {
                nodeGo = isParentCanvas? null: TransformUtils.TryToFindPreviouslyCreatedObject(parent, node.id);
                RectTransform parentT = null;
                RectTransform rectTransform = null;
                if (nodeGo == null)
                {
                    nodeGo = new GameObject();
                    parentT = parent.GetComponent<RectTransform>();
                    if (isParentCanvas)
                        root = parentT;
                    nodeGo.name = $"{node.name} [{node.id}]";
                    rectTransform = nodeGo.AddComponent<RectTransform>();
                    TransformUtils.SetParent(parentT, rectTransform);
                }
                else
                {
                    rectTransform = (RectTransform) nodeGo.transform;
                    parent = rectTransform.parent.gameObject;
                    isParentCanvas = parent.GetComponent<Canvas>();
                    if (isParentCanvas)    
                        offset = boundingBox.GetPosition();
                    parentT = (RectTransform)nodeGo.transform.parent;
                }

                TransformUtils.SetPosition(parentT, rectTransform, boundingBox, _importer, offset);
                if (!isParentCanvas)
                    TransformUtils.SetConstraints(parentT, rectTransform, node.constraints);
                ImageUtils.SetMask(node, nodeGo);
            }
            
            switch (treeElement.actionType)
            {
                case ActionType.None:
                    break;
                case ActionType.Render:
                    if (treeElement.sprite != null)
                    {
                        ImageUtils.AddOverridenSprite(nodeGo, treeElement.sprite);
                        break;
                    }
                    await ImageUtils.RenderNodeAndApply(node, nodeGo, _importer);
                    break;
#if VECTOR_GRAHICS_IMPORTED
                case ActionType.SvgRender:
                    if (treeElement.sprite != null)
                    {
                        ImageUtils.AddOverridenSvgSprite(nodeGo, treeElement.sprite);
                        break;
                    }
                    await ImageUtils.RenderSvgNodeAndApply(node, nodeGo, _importer);
                    break;
#endif
                case ActionType.Generate:
                    if (treeElement.sprite != null)
                    {
                        ImageUtils.AddOverridenSprite(nodeGo, treeElement.sprite);
                    }
                    else
                    {
                        AddText(node, nodeGo);
                        AddFills(node, nodeGo);
                        if (node.children == null) return;
                    }
                    if (node.children == null) return;
                    await Task.WhenAll(node.children.Select(x => GenerateNode(x, nodeGo, nodeTreeElements))); //todo: Need to fix the progress bar because of simultaneous nodes generation.
                    break;
                case ActionType.Transform:
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        

        private void AddText(Node node, GameObject nodeGo)
        {
            if (node.type == "TEXT")
            {
                var tmp = TMPUtils.GetOrAddTMPComponentToObject(nodeGo);
                tmp.text = node.characters;
                var style = node.style;
                TMPUtils.ApplyFigmaStyleToTMP(tmp, style, _importer.Scale);                
                tmp.alignment = TMPUtils.FigmaAlignmentToTMP(style.textAlignHorizontal, style.textAlignVertical);
                tmp.fontStyle = TMPUtils.FigmaFontStyleToTMP(style.textDecoration, style.textCase);
                //tmp.characterSpacing = style.letterSpacing; //It doesn't work like that, need to make some calculations.
            }
        }

        
        


        private void AddFills(Node node, GameObject nodeGo)
        {
            var gg = GetGradientsGenerator();
            Image image = nodeGo.GetComponent<Image>();
            if (node.fills.Length > 0f && image == null && nodeGo.GetComponent<Graphic>()==null)
                image = nodeGo.AddComponent<Image>();
            
            var tmp = nodeGo.GetComponent<TextMeshProUGUI>();
            
            for (var index = 0; index < node.fills.Length; index++)
            {
                var fill = node.fills[index];
                if (index != 0)
                {
                    var go = TransformUtils.InstantiateChild(nodeGo, fill.type);
                    image = go.AddComponent<Image>();
                }

                switch (fill.type)
                {
                    case "SOLID":
                        if (tmp != null)
                            tmp.color = fill.color.ToColor();
                        else
                            image.color = fill.color.ToColor();
                        break;
                    case "GRADIENT_LINEAR" when tmp != null:
                        var gradient = fill.gradientStops;
                        tmp.enableVertexGradient = true;
                        var firstColor = gradient.Length <= 0 ? UnityEngine.Color.white : ColorUtils.ConvertToUnityColor(gradient[0].color);
                        var secondColor = gradient.Length <= 1 ? firstColor : ColorUtils.ConvertToUnityColor(gradient[1].color);
                        var thirdColor = gradient.Length <= 2 ? UnityEngine.Color.white : ColorUtils.ConvertToUnityColor(gradient[2].color);
                        var fourthColor = gradient.Length <= 3 ? thirdColor : ColorUtils.ConvertToUnityColor(gradient[3].color); 
                        tmp.colorGradient = new VertexGradient(firstColor, secondColor, thirdColor, fourthColor);
                        break;
                    default:
                        var tex = gg.GetTexture(fill, node.absoluteBoundingBox.GetSize(), 256);
                        string fileName = $"{node.name}_{index.ToString()}.png";
                        ImageUtils.SaveTexture(tex, $"/{_importer.GetRendersFolderPath()}/{fileName}");
                        var sprite = ImageUtils.ChangeTextureToSprite($"Assets/{_importer.GetRendersFolderPath()}/{fileName}");
                        image.sprite = sprite;
                        break;
                }

                if (image != null) 
                    image.enabled = fill.visible != "false";
            }
        }

        private static GradientsGenerator GetGradientsGenerator()
        {
            var foundAssets = AssetDatabase.FindAssets("t:GradientsGenerator");
            GradientsGenerator gg = null;
            if (foundAssets.Length == 0)
            {
                gg = ScriptableObject.CreateInstance<GradientsGenerator>();
                if (AssetDatabase.IsValidFolder("Assets/FigmaImporter/") == false)
                {
                    AssetDatabase.CreateFolder("Assets", "FigmaImporter");
                    if (AssetDatabase.IsValidFolder("Assets/FigmaImporter/Editor") == false)
                    {
                        AssetDatabase.CreateFolder("Assets/FigmaImporter", "Editor");
                    }
                }
                AssetDatabase.CreateAsset(gg, "Assets/FigmaImporter/Editor/GradientsGenerator.asset");
                AssetDatabase.SaveAssets();
            }
            else
            {
                var gradientGeneratorId = foundAssets[0];
                gg = AssetDatabase.LoadAssetAtPath<GradientsGenerator>(
                        AssetDatabase.GUIDToAssetPath(gradientGeneratorId));
            }
            return gg;
        }

        private static void GenerateRenderSaveFolder(string path)
        {
            var fullPath = Path.Combine(Application.dataPath, path);
            if (Directory.Exists(fullPath))
            {
                return;
            }
            Directory.CreateDirectory(fullPath);
        }
    }
}