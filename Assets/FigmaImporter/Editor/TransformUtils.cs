using UnityEngine;

namespace FigmaImporter.Editor
{
    public class TransformUtils
    {
        public static void SetConstraints(RectTransform parentTransform, RectTransform rectTransform,
            Constraints nodeConstraints)
        {
            Vector2 offsetMin = rectTransform.offsetMin;
            Vector2 offsetMax = rectTransform.offsetMax;
            var parentSize = parentTransform.rect.size;
            Vector2 positionMin = Vector2.Scale(rectTransform.anchorMin, parentSize) + offsetMin;
            Vector2 positionMax = Vector2.Scale(rectTransform.anchorMax, parentSize) + offsetMax;

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
        
        public static Vector3 ConvertVector(RectTransform parent, Vector3 anchoredPosition)
        {
            Vector3[] corners = new Vector3[4];
            parent.GetWorldCorners(corners);
            var deltaX = corners[3] - corners[0];
            var deltaY = corners[3] - corners[2];
            var posX = anchoredPosition.x * deltaX / parent.rect.width;
            var posY = anchoredPosition.y * deltaY / parent.rect.height;
            return posX + posY + corners[1];
        }
        
        public static void SetPosition(RectTransform parent, RectTransform rectTransform, AbsoluteBoundingBox boundingBox, FigmaImporter importer, Vector2 offset)
        {
            var canvas = parent.GetComponentInParent<Canvas>();
            rectTransform.pivot = Vector2.up;
            var newPosition = boundingBox.GetPosition() - offset;
            newPosition *= importer.Scale;
            var v = TransformUtils.ConvertVector((RectTransform)canvas.transform, newPosition);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, boundingBox.width * importer.Scale);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, boundingBox.height * importer.Scale);

            rectTransform.position = v;
        }
        
        public static void SetParent(RectTransform parentT, RectTransform rectTransform)
        {
            rectTransform.SetParent(parentT);
            rectTransform.localScale = Vector3.one;
        }
        
        public static GameObject TryToFindPreviouslyCreatedObject(GameObject parent, string nodeId)
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
        
        public static GameObject InstantiateChild(GameObject nodeGo, string name)
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
    }
}