using UnityEditor;

namespace FigmaImporter.Editor
{
    public class FigmaNodesProgressInfo
    {
        public static int NodesCount;
        public static int CurrentNode;
        public static string CurrentTitle;
        public static string CurrentInfo;

        public static void ShowProgress(float progress)
        {
            if (NodesCount!=0)
                CurrentTitle = $"Generating node {CurrentNode.ToString()}/{NodesCount.ToString()}";
            EditorUtility.DisplayProgressBar(CurrentTitle, CurrentInfo, progress);
        }

        public static void HideProgress()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}