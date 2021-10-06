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
                nodeGo = isParentCanvas? null: TryToFindPreviouslyCreatedObject(parent, node.id);
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
                    SetParent(parentT, rectTransform);
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

                SetPosition(parentT, rectTransform, boundingBox);
                if (!isParentCanvas)
                    SetConstraints(parentT, rectTransform, node.constraints);
                SetMask(node, nodeGo);
            }
            
            switch (treeElement.actionType)
            {
                case ActionType.None:
                    break;
                case ActionType.Render:
                    if (treeElement.sprite != null)
                    {
                        AddOverridenSprite(nodeGo, treeElement.sprite);
                        break;
                    }
                    await RenderNodeAndApply(node, nodeGo);
                    break;
                case ActionType.Generate:
                    if (treeElement.sprite != null)
                    {
                        AddOverridenSprite(nodeGo, treeElement.sprite);
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

        private GameObject TryToFindPreviouslyCreatedObject(GameObject parent, string nodeId)
        {
            string id = $"[{nodeId}]";
            if (parent.name.Contains(id))
                return parent;
            foreach (Transform child in parent.transform)
            {
                if (child.name.Contains(id))
                    return child.gameObject;
            }
            return null;
        }

        private void SetParent(RectTransform parentT, RectTransform rectTransform)
        {
            rectTransform.SetParent(parentT);
            rectTransform.localScale = Vector3.one;
        }

        private void AddOverridenSprite(GameObject nodeGo, Sprite overridenSprite)
        {
            var image = nodeGo.AddComponent<Image>();
            image.sprite = overridenSprite;
        }

        private void AddText(Node node, GameObject nodeGo)
        {
            if (node.type == "TEXT")
            {
                var t = nodeGo.transform as RectTransform;
                var offsetMin = t.offsetMin;
                var offsetMax = t.offsetMax;
                var tmp = nodeGo.GetComponent<TextMeshProUGUI>();
                if (tmp == null)
                    tmp = nodeGo.AddComponent<TextMeshProUGUI>(); // Somehow adding component changes size of the object???????
                t.offsetMin = offsetMin;
                t.offsetMax = offsetMax;
                var style = node.style;
                tmp.fontSize = style.fontSize * _importer.Scale;
                tmp.text = node.characters;
                var fontLinksId = AssetDatabase.FindAssets("t:FontLinks")[0];
                FontLinks fl = AssetDatabase.LoadAssetAtPath<FontLinks>(AssetDatabase.GUIDToAssetPath(fontLinksId));

                var fontName = string.IsNullOrEmpty(style.fontPostScriptName)
                    ? style.fontFamily
                    : style.fontPostScriptName;
                var font = fl.Get(fontName);
                if (font == null)
                {
                    Debug.LogError(
                        $"[FigmaImporter] Couldn't find font named {fontName}, please link it in FontLinks.asset");
                    fl.AddName(fontName);
                }
                else
                    tmp.font = font;

                var verticalAlignment = style.textAlignVertical;
                var horizontalAlignment = style.textAlignHorizontal;
                int alignment = 0;
                alignment += (verticalAlignment == "TOP" ? 1 : 0) << 8;
                alignment += (verticalAlignment == "CENTER" ? 1 : 0) << 9;
                alignment += (verticalAlignment == "BOTTOM" ? 1 : 0) << 10;
                alignment += (horizontalAlignment == "LEFT" ? 1 : 0) << 0;
                alignment += (horizontalAlignment == "CENTER" ? 1 : 0) << 1;
                alignment += (horizontalAlignment == "RIGHT" ? 1 : 0) << 2;
                alignment += (horizontalAlignment == "JUSTIFIED" ? 1 : 0) << 3;
                tmp.alignment = (TextAlignmentOptions) alignment;
                FontStyles fontStyle = 0;
                fontStyle |= (style.textDecoration == "UNDERLINE" ? FontStyles.Underline : 0);
                fontStyle |= (style.textDecoration == "STRIKETHROUGH" ? FontStyles.Strikethrough : 0);

                fontStyle |= (style.textCase == "UPPER" ? FontStyles.UpperCase : 0);
                fontStyle |= (style.textCase == "LOWER" ? FontStyles.LowerCase : 0);
                fontStyle |= (style.textCase == "SMALL_CAPS" ? FontStyles.SmallCaps : 0);
                tmp.fontStyle = fontStyle;

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
                SaveTexture(result, $"/{_importer.GetRendersFolderPath()}/{spriteName}");
                sprite = ChangeTextureToSprite($"Assets/{_importer.GetRendersFolderPath()}/{spriteName}");
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
                        SaveTexture(tex, $"/{_importer.GetRendersFolderPath()}/{fileName}");
                        var sprite = ChangeTextureToSprite($"Assets/{_importer.GetRendersFolderPath()}/{fileName}");
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

        private Sprite ChangeTextureToSprite(string path)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private void SaveTexture(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            if (bytes != null)
            {
                var filePath = Application.dataPath + path;
                System.IO.File.WriteAllBytes(filePath, bytes);
                UnityEditor.AssetDatabase.Refresh();
            }
        }

        private void SetMask(Node node, GameObject nodeGo)
        {
            if (!node.clipsContent)
                return;
            if (node.fills.Length == 0)
                nodeGo.AddComponent<RectMask2D>();
            else
                nodeGo.AddComponent<Mask>();
        }

        private void SetConstraints(RectTransform parentTransform, RectTransform rectTransform,
            Constraints nodeConstraints)
        {
            Vector2 offsetMin = rectTransform.offsetMin;
            Vector2 offsetMax = rectTransform.offsetMax;
            var parentSize = parentTransform.rect.size;
            Vector2 positionMin = Vector2.Scale(rectTransform.anchorMin, parentSize) + offsetMin;
            Vector2 positionMax = Vector2.Scale(rectTransform.anchorMax, parentSize) + offsetMax;

            var width = rectTransform.rect.width;
            var height = rectTransform.rect.height;
            Vector3 minAnchor = Vector2.one / 2f;
            Vector3 maxAnchor = Vector2.one / 2f;

            switch (nodeConstraints.horizontal)
            {
                case "LEFT_RIGHT":
                    minAnchor.x = 0f;
                    maxAnchor.x = 1f;
                    break;
                case "LEFT":
                    minAnchor.x = maxAnchor.x = 0f;
                    break;
                case "RIGHT":
                    minAnchor.x = maxAnchor.x = 1f;
                    break;
                case "CENTER":
                    minAnchor.x = maxAnchor.x = 0.5f;
                    break;
                case "SCALE":
                    minAnchor.x = rectTransform.anchorMin.x + rectTransform.offsetMin.x / parentTransform.rect.width;
                    maxAnchor.x = rectTransform.anchorMax.x + rectTransform.offsetMax.x / parentTransform.rect.width;
                    break;
                default:
                    Debug.LogError($"Unknown horizontal constraint {nodeConstraints.horizontal}");
                    break;
            }

            switch (nodeConstraints.vertical)
            {
                case "TOP_BOTTOM":
                    minAnchor.y = 0f;
                    maxAnchor.y = 1f;
                    break;
                case "BOTTOM":
                    minAnchor.y = maxAnchor.y = 0f;
                    break;
                case "TOP":
                    minAnchor.y = maxAnchor.y = 1f;
                    break;
                case "CENTER":
                    minAnchor.y = maxAnchor.y = 0.5f;
                    break;
                case "SCALE":
                    minAnchor.y = rectTransform.anchorMin.y + rectTransform.offsetMin.y / parentTransform.rect.height;
                    maxAnchor.y = rectTransform.anchorMax.y + rectTransform.offsetMax.y / parentTransform.rect.height;
                    break;
                default:
                    Debug.LogError($"Unknown horizontal constraint {nodeConstraints.horizontal}");
                    break;
            }

            rectTransform.anchorMin = minAnchor;
            rectTransform.anchorMax = maxAnchor;

            rectTransform.offsetMin = positionMin - Vector2.Scale(rectTransform.anchorMin, parentSize);
            rectTransform.offsetMax = positionMax - Vector2.Scale(rectTransform.anchorMax, parentSize);
        }

        public Vector3 InverseScale(Vector2 one, Vector2 two)
        {
            return new Vector2(one.x / two.x, one.y / two.y);
        }


        private void SetPosition(RectTransform parent, RectTransform rectTransform, AbsoluteBoundingBox boundingBox)
        {
            var canvas = parent.GetComponentInParent<Canvas>();
            rectTransform.pivot = Vector2.up;
            var newPosition = boundingBox.GetPosition() - offset;
            newPosition *= _importer.Scale;
            var v = ConvertVector((RectTransform)canvas.transform, newPosition);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, boundingBox.width * _importer.Scale);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, boundingBox.height * _importer.Scale);

            rectTransform.position = v;
        }

        private Vector3 ConvertVector(RectTransform parent, Vector3 anchoredPosition)
        {
            Vector3[] corners = new Vector3[4];
            parent.GetWorldCorners(corners);
            var deltaX = corners[3] - corners[0];
            var deltaY = corners[3] - corners[2];
            var posX = anchoredPosition.x * deltaX / parent.rect.width;
            var posY = anchoredPosition.y * deltaY / parent.rect.height;
            return posX + posY + corners[1];
        }

        private Vector2 SwitchToRectTransform(RectTransform from, RectTransform to)
        {
            Vector2 localPoint;
            Vector2 fromPivotDerivedOffset = new Vector2(from.rect.width * from.pivot.x + from.rect.xMin, from.rect.height * from.pivot.y + from.rect.yMin);
            Vector2 screenP = RectTransformUtility.WorldToScreenPoint(null, from.position);
            screenP += fromPivotDerivedOffset;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(to, screenP, null, out localPoint);
            Vector2 pivotDerivedOffset = new Vector2(to.rect.width * to.pivot.x + to.rect.xMin, to.rect.height * to.pivot.y + to.rect.yMin);
            return to.anchoredPosition + localPoint - pivotDerivedOffset;
        }
        
        private void Compare(Vector3 before, Vector3 after)
        {
            Debug.Log($"{before} :::: {after}");
        }

        public GameObject GenerateCanvas()
        {
            GameObject canvasGO = new GameObject("Canvas");
            var transform = canvasGO.AddComponent<RectTransform>();
            var canvas = canvasGO.AddComponent<Canvas>();
            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            var graphicsRaycaster = canvasGO.AddComponent<GraphicRaycaster>();
            return canvasGO;
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