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
                    await RenderNodeAndApply(node, nodeGo);
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

        private async Task RenderNodeAndApply(Node node, GameObject nodeGo)
        {
            FigmaNodesProgressInfo.CurrentInfo = "Loading image";
            FigmaNodesProgressInfo.ShowProgress(0f);
            var result = await _importer.GetImage(node.id);
            var t = nodeGo.transform as RectTransform;
            string spriteName = $"{node.name}_{node.id.Replace(':', '_')}.png";
            
            Image image = null;
            Sprite sprite = null;
            FigmaNodesProgressInfo.CurrentInfo = "Saving rendered node";
            FigmaNodesProgressInfo.ShowProgress(0f);
            try
            {
                ImageUtils.SaveTexture(result, $"/{_importer.GetRendersFolderPath()}/{spriteName}");
                sprite = ImageUtils.ChangeTextureToSprite($"Assets/{_importer.GetRendersFolderPath()}/{spriteName}");
                if (Math.Abs(t.rect.width - sprite.texture.width) < 1f &&
                    Math.Abs(t.rect.height - sprite.texture.height) < 1f)
                {
                    image = nodeGo.AddComponent<Image>();
                    image.sprite = sprite;
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            var child = InstantiateChild(nodeGo, "Render");
            if (sprite != null)
            {
                image = child.AddComponent<Image>();
                image.sprite = sprite;
                t = child.transform as RectTransform;
                t.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sprite.texture.width);
                t.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sprite.texture.height);
            }
        }
        


        private void AddFills(Node node, GameObject nodeGo)
        {
            var gradientGeneratorId = AssetDatabase.FindAssets("t:GradientsGenerator")[0];
            GradientsGenerator gg =
                AssetDatabase.LoadAssetAtPath<GradientsGenerator>(AssetDatabase.GUIDToAssetPath(gradientGeneratorId));
            Image image = nodeGo.GetComponent<Image>();
            if (node.fills.Length > 0f && image == null && nodeGo.GetComponent<Graphic>()==null)
                image = nodeGo.AddComponent<Image>();
            for (var index = 0; index < node.fills.Length; index++)
            {
                var fill = node.fills[index];
                if (index != 0)
                {
                    var go = InstantiateChild(nodeGo, fill.type);
                    image = go.AddComponent<Image>();
                }

                switch (fill.type)
                {
                    case "SOLID":
                        var tmp = nodeGo.GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                            tmp.color = fill.color.ToColor();
                        else
                            image.color = fill.color.ToColor();
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

        private static GameObject InstantiateChild(GameObject nodeGo, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.parent = nodeGo.transform;
            go.transform.localScale = Vector3.one;
            var rTransform = go.AddComponent<RectTransform>();
            rTransform.position = Vector3.zero;
            rTransform.anchorMin = Vector2.zero;
            rTransform.anchorMax = Vector2.one;
            rTransform.offsetMin = rTransform.offsetMax = Vector2.zero;
            return go;
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