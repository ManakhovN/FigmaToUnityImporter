using System;
using UnityEditor.TreeViewExamples;
using UnityEngine;
using Random = UnityEngine.Random;


namespace FigmaImporter.Editor.EditorTree.TreeData
{

	[Serializable]
	public class NodeTreeElement : TreeElement
	{
		public string figmaId;
		public ActionType actionType;
		public Sprite sprite;

		public NodeTreeElement (string name, string figmaId, ActionType actionType, Sprite sprite, int depth, int id) : base (name, depth, id)
		{
			this.actionType = actionType;
			this.sprite = sprite;
			this.figmaId = figmaId;
		}
	}

	public enum ActionType
	{
		None,
		Render,
		Generate,
		Transform,
#if VECTOR_GRAHICS_IMPORTED
		SvgRender
#endif
	}
}
