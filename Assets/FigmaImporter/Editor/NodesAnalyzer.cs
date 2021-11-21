using System.Collections.Generic;
using System.Linq;
using FigmaImporter.Editor.EditorTree.TreeData;
using UnityEditor.TreeViewExamples;

namespace FigmaImporter.Editor
{
    public class NodesAnalyzer
    {
        public static void AnalyzeRenderMode(IList<Node> nodes, IList<NodeTreeElement> nodesTreeElements)
        {
            foreach (var node in nodes)
            {
                AnalyzeSingleNode(node, nodesTreeElements.FirstOrDefault(x=>x.figmaId == node.id));
                if (node.children != null)
                {
                    AnalyzeRenderMode(node.children, nodesTreeElements);
                }
            }
        }

        public static void AnalyzeTransformMode(IList<Node> nodes, IList<NodeTreeElement> nodesTreeElements)
        {
            foreach (var node in nodes)
            {
                nodesTreeElements.FirstOrDefault(x => x.figmaId == node.id).actionType = ActionType.Transform;
                if (node.children != null)
                {
                    AnalyzeTransformMode(node.children, nodesTreeElements);
                }
            }
        }
        
        public static void AnalyzeSVGMode(IList<Node> nodes, IList<NodeTreeElement> nodesTreeElements)
        {
            foreach (var node in nodes)
            {
                AnalyzeSingleNodeSVG(node, nodesTreeElements.FirstOrDefault(x=>x.figmaId == node.id));
                if (node.children != null)
                {
                    AnalyzeSVGMode(node.children, nodesTreeElements);
                }
            }
        }

        private static void AnalyzeSingleNodeSVG(Node node, NodeTreeElement treeElement)
        {
            #if VECTOR_GRAHICS_IMPORTED
            if (node.type != "TEXT" && (node.children == null || node.children.Length == 0))
            {
                treeElement.actionType = ActionType.SvgRender;
            }
            else
            {
                treeElement.actionType = ActionType.Generate;
            }
            #endif
        }
        
        private static void AnalyzeSingleNode(Node node, NodeTreeElement treeElement)
        {
            if (node.type != "TEXT" && (node.children == null || node.children.Length == 0))
            {
                treeElement.actionType = ActionType.Render;
            }
            else
            {
                treeElement.actionType = ActionType.Generate;
            }
        }
        
        public static void CheckActions(IList<Node> nodes, IList<NodeTreeElement> nodesTreeElements)
        {
            if (nodes == null)
                return;
            
            foreach (var node in nodes)
            {
                var treeElement = nodesTreeElements.First(x=>x.figmaId == node.id);
                if (treeElement.actionType == ActionType.Render)
                {
                    SetChildrenActionRecursively(node.children, ActionType.None, nodesTreeElements);
                }
                else
                {
                    CheckActions(node.children, nodesTreeElements);
                }
            }
        }

        private static void SetChildrenActionRecursively(IList<Node> nodes, ActionType actionType,
            IList<NodeTreeElement> nodesTreeElements)
        {
            if (nodes == null)
                return;
            foreach (var node in nodes)
            {
                nodesTreeElements.First(x => x.figmaId == node.id).actionType = actionType;
                SetChildrenActionRecursively(node.children, actionType, nodesTreeElements);
            }
        }
    }
}