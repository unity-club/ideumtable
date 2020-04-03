using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

//
// Code copied and modified from the Unity Cs Reference on Github at
// https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/BuildPlayerSceneTreeView.cs
// For use with the Coffee Table build system. The code linked above is the source code for
// the scene list in the default Unity build settings, which is repurposed here for a scene-select system
// in the coffee table build window.
//
namespace CoffeeTable.Editor.Inspector
{
	internal class SceneTreeViewItem : TreeViewItem
	{
		private const string kAssetsFolder = "Assets/";
		private const string kSceneExtension = ".unity";

		public static int kInvalidCounter = -1;

		public bool Active { get; set; }
		public int Counter { get; set; }
		public string FullName { get; set; }
		public GUID Guid { get; set; }

		public void UpdateName ()
		{
			var name = AssetDatabase.GUIDToAssetPath(Guid.ToString());
			if (name != FullName)
			{
				FullName = name;
				displayName = FullName;
				if (displayName.StartsWith(kAssetsFolder))
					displayName = displayName.Remove(0, kAssetsFolder.Length);
				var extension = displayName.LastIndexOf(kSceneExtension);
				if (extension > 0)
					displayName = displayName.Substring(0, extension);
			}
		}

		public SceneTreeViewItem(int id, int depth, GUID g, bool state) : base(id, depth)
		{
			Active = state;
			Counter = kInvalidCounter;
			Guid = g;
			FullName = string.Empty;
			UpdateName();
		}
	}

	internal class SceneTreeView : TreeView
	{
		public SceneTreeView(TreeViewState state) : base(state)
		{
			showBorder = true;
			EditorBuildSettings.sceneListChanged += HandleExternalSceneListChange;
		}

		internal void UnsubscribeListChange()
		{
			EditorBuildSettings.sceneListChanged -= HandleExternalSceneListChange;
		}

		private void HandleExternalSceneListChange()
		{
			Reload();
		}

		protected override TreeViewItem BuildRoot()
		{
			var root = new TreeViewItem(-1, -1);
			root.children = new List<TreeViewItem>();

			List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
			foreach (var sc in scenes)
			{
				var item = new SceneTreeViewItem(sc.guid.GetHashCode(), 0, sc.guid, sc.enabled);
				root.AddChild(item);
			}
			return root;
		}

		protected override bool CanBeParent(TreeViewItem item)
		{
			return false;
		}

		protected override void BeforeRowsGUI()
		{
			int counter = 0;
			foreach (var item in rootItem.children)
			{
				var treeViewItem = item as SceneTreeViewItem;
				if (treeViewItem == null)
					continue;

				treeViewItem.UpdateName();

				//Need to set counter here because RowGUI is only called on items that are visible.
				if (treeViewItem.Active)
				{
					treeViewItem.Counter = counter;
					counter++;
				}
				else
					treeViewItem.Counter = SceneTreeViewItem.kInvalidCounter;
			}

			base.BeforeRowsGUI();
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			var sceneItem = args.item as SceneTreeViewItem;
			if (sceneItem != null)
			{
				var sceneWasDeleted = sceneItem.Guid.Empty();
				var sceneExists = !sceneWasDeleted && File.Exists(sceneItem.FullName);

				using (new EditorGUI.DisabledScope(!sceneExists))
				{
					var newState = sceneItem.Active;
					if (!sceneExists)
						newState = false;
					newState = GUI.Toggle(new Rect(args.rowRect.x, args.rowRect.y, 16f, 16f), newState, "");
					if (newState != sceneItem.Active)
					{
						if (GetSelection().Contains(sceneItem.id))
						{
							var selection = GetSelection();
							foreach (var id in selection)
							{
								var item = FindItem(id, rootItem) as SceneTreeViewItem;
								item.Active = newState;
							}
						}
						else sceneItem.Active = newState;

						EditorBuildSettings.scenes = GetSceneList();
					}

					base.RowGUI(args);

					if (sceneItem.Counter != SceneTreeViewItem.kInvalidCounter)
					{
						DefaultGUI.LabelRightAligned(args.rowRect, "" + sceneItem.Counter, args.selected, args.focused);
					}
					else if (sceneItem.displayName == string.Empty || !sceneExists)
					{
						DefaultGUI.LabelRightAligned(args.rowRect, "Deleted", args.selected, args.focused);
					}
				}
			}
			else
				base.RowGUI(args);
		}

		protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
		{
			DragAndDropVisualMode visualMode = DragAndDropVisualMode.None;

			var draggedIDs = DragAndDrop.GetGenericData(nameof(SceneTreeViewItem)) as List<int>;
			if (draggedIDs != null && draggedIDs.Count > 0)
			{
				visualMode = DragAndDropVisualMode.Move;
				if (args.performDrop)
				{
					int newIndex = FindDropAtIndex(args);

					var result = new List<TreeViewItem>();
					int toInsert = 0;
					foreach (var item in rootItem.children)
					{
						if (toInsert == newIndex)
						{
							foreach (var id in draggedIDs)
							{
								result.Add(FindItem(id, rootItem));
							}
						}
						toInsert++;
						if (!draggedIDs.Contains(item.id))
						{
							result.Add(item);
						}
					}

					if (result.Count < rootItem.children.Count) //must be appending.
					{
						foreach (var id in draggedIDs)
						{
							result.Add(FindItem(id, rootItem));
						}
					}
					rootItem.children = result;
					EditorBuildSettings.scenes = GetSceneList();
					ReloadAndSelect(draggedIDs);
					Repaint();
				}
			}
			else if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
			{
				visualMode = DragAndDropVisualMode.Copy;
				if (args.performDrop)
				{
					var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
					var scenesToAdd = new List<EditorBuildSettingsScene>();
					var selection = new List<int>();

					foreach (var path in DragAndDrop.paths)
					{
						if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(SceneAsset))
						{
							var guid = new GUID(AssetDatabase.AssetPathToGUID(path));
							selection.Add(guid.GetHashCode());

							bool unique = true;
							foreach (var scene in scenes)
							{
								if (scene.path == path)
								{
									unique = false;
									break;
								}
							}
							if (unique)
								scenesToAdd.Add(new EditorBuildSettingsScene(path, true));
						}
					}


					int newIndex = FindDropAtIndex(args);
					scenes.InsertRange(newIndex, scenesToAdd);
					EditorBuildSettings.scenes = scenes.ToArray();
					ReloadAndSelect(selection);
					Repaint();
				}
			}
			return visualMode;
		}

		private void ReloadAndSelect(IList<int> hashCodes)
		{
			Reload();
			SetSelection(hashCodes, TreeViewSelectionOptions.RevealAndFrame);
			SelectionChanged(hashCodes);
		}

		protected override void DoubleClickedItem(int id)
		{
			SceneTreeViewItem item = FindItem(id, rootItem) as SceneTreeViewItem;
			if (item == null) return;
			int instanceID = AssetDatabase.LoadAssetAtPath<SceneAsset>(item.FullName).GetInstanceID();
			EditorGUIUtility.PingObject(instanceID);
		}

		protected int FindDropAtIndex(DragAndDropArgs args)
		{
			int indexToDrop = args.insertAtIndex;

			// covers if(args.dragAndDropPosition == DragAndDropPosition.OutsideItems) and a safety check.
			if (indexToDrop < 0 || indexToDrop > rootItem.children.Count)
				indexToDrop = rootItem.children.Count;

			return indexToDrop;
		}

		protected override bool CanStartDrag(CanStartDragArgs args)
		{
			return true;
		}

		protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
		{
			DragAndDrop.PrepareStartDrag();
			DragAndDrop.paths = null;
			DragAndDrop.objectReferences = new UnityEngine.Object[] { };
			DragAndDrop.SetGenericData(nameof(SceneTreeViewItem), new List<int>(args.draggedItemIDs));
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			DragAndDrop.StartDrag(nameof(SceneTreeView));
		}

		protected override void KeyEvent()
		{
			if ((Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace) &&
				(GetSelection().Count > 0))
			{
				RemoveSelection();
			}
		}

		protected override void ContextClicked()
		{
			if (GetSelection().Count > 0)
			{
				GenericMenu menu = new GenericMenu();
				menu.AddItem(EditorGUIUtility.TrTextContent("Remove Selection"), false, RemoveSelection);
				menu.ShowAsContext();
			}
		}

		protected void RemoveSelection()
		{
			foreach (var nodeID in GetSelection())
			{
				rootItem.children.Remove(FindItem(nodeID, rootItem));
			}
			EditorBuildSettings.scenes = GetSceneList();
			Reload();
			Repaint();
		}

		public EditorBuildSettingsScene[] GetSceneList()
		{
			var sceneList = new EditorBuildSettingsScene[rootItem.children.Count];
			for (int index = 0; index < rootItem.children.Count; index++)
			{
				var sceneItem = rootItem.children[index] as SceneTreeViewItem;
				sceneList[index] = new EditorBuildSettingsScene(sceneItem.FullName, sceneItem.Active);
			}
			return sceneList;
		}
	}
}
