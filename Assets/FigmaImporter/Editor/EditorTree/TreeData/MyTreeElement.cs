using System;
using UnityEditor.TreeViewExamples;
using UnityEngine;
using Random = UnityEngine.Random;


namespace FigmaImporter.Editor.EditorTree.TreeData
{

	[Serializable]
	internal class MyTreeElement : TreeElement
	{
		public string figmaId;
		public ActionType actionType;
		public Sprite sprite;

		public MyTreeElement (string name, string figmaId, ActionType actionType, Sprite sprite, int depth, int id) : base (name, depth, id)
		{
			this.actionType = actionType;
			this.sprite = sprite;
			this.figmaId = figmaId;
		}
	}

	internal enum ActionType
	{
		None, Render, Generate, Transform
	}
}
