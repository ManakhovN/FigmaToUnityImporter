using System;
using System.Collections.Generic;
using FigmaImporter.Editor.EditorTree.TreeData;
using UnityEditor.IMGUI.Controls;
using UnityEditor.TreeViewExamples;
using UnityEngine;

namespace FigmaImporter.Editor.EditorTree
{

	class MultiColumnLayout
	{
		[NonSerialized] bool m_Initialized;
		[SerializeField] TreeViewState m_TreeViewState; // Serialized in the window layout file so it survives assembly reloading
		[SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;
		SearchField m_SearchField;
		MultiColumnTreeView m_TreeView;

		public MultiColumnTreeView TreeView => m_TreeView;

		void InitIfNeeded(Rect rect, List<Node> nodes)
		{
			if (!m_Initialized)
			{
				// Check if it already exists (deserialized from window layout file or scriptable object)
				if (m_TreeViewState == null)
					m_TreeViewState = new TreeViewState();

				bool firstInit = m_MultiColumnHeaderState == null;
				var headerState = MultiColumnTreeView.CreateDefaultMultiColumnHeaderState(rect.width);
				if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
					MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
				m_MultiColumnHeaderState = headerState;
				
				var multiColumnHeader = new MyMultiColumnHeader(headerState);
				if (firstInit)
					multiColumnHeader.ResizeToFit ();
				int idCounter = 0;
				var treeModel = new TreeModel<NodeTreeElement>(GetData(nodes, ref idCounter));
				m_TreeView = new MultiColumnTreeView(m_TreeViewState, multiColumnHeader, treeModel);
				m_Initialized = true;
			}
		}
		
		IList<NodeTreeElement> GetData(IList<Node> nodes, ref int idCounter, int depth = -1)
		{
			List<NodeTreeElement> result = new List<NodeTreeElement>();
			int currentDepth = depth;
			if (currentDepth == -1)
			{
				result.Add(new NodeTreeElement("Root", "Root", ActionType.None, null, currentDepth, idCounter));
				idCounter++;
				currentDepth++;
			}

			foreach (var node in nodes)
			{
				result.Add(new NodeTreeElement(node.name,node.id, ActionType.None, null, currentDepth, idCounter));
				idCounter++;
				if (node.children != null)
					result.AddRange(GetData(node.children, ref idCounter, currentDepth + 1));				
			}

			// generate some test data
			return result; 
		}

		public void OnGUI(Rect rect, List<Node> nodes)
		{
			InitIfNeeded(rect, nodes);
			DoTreeView (rect);
		}

		void DoTreeView (Rect rect)
		{
			m_TreeView.OnGUI(rect);
		}
	}


	internal class MyMultiColumnHeader : MultiColumnHeader
	{
		public MyMultiColumnHeader(MultiColumnHeaderState state)
			: base(state)
		{
			canSort = false;
			height = DefaultGUI.defaultHeight;
		}
		
	}

}
