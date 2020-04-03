using CoffeeTable.Editor.Builds;
using CoffeeTable.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoffeeTable.Editor.Inspector
{
	public class BuildWindow : EditorWindow
	{
		internal class Styles
		{
			public const int margin = 5;
			public static readonly GUIContent titleText = new GUIContent("Coffee Table Build");
			public static readonly GUIContent scenesInBuild = EditorGUIUtility.TrTextContent("Scenes In Build", "Which scenes to include in the build");
			public static readonly GUIContent addScenesTooltip = new GUIContent("Drag and drop scenes into the box below to select which scenes should appear in the built executable" +
				" when this application is built to the coffee table, or click Add Open Scenes to add the currently open scenes automatically. " +
				"Note that editting the list below will modify your included scenes when building for all platforms, not just for the coffee table.");
			public static readonly GUIContent addOpenSource = EditorGUIUtility.TrTextContent("Add Open Scenes");
			public static readonly GUIContent buildButton = EditorGUIUtility.TrTextContent("Build");
			public static readonly GUIContent beginBuildTitle = new GUIContent("Begin Build");
			public static readonly GUIContent buildSucceededNotification = new GUIContent("Build Succeeded");

			public static readonly GUIStyle tooltipWrapStyle;

			static Styles()
			{
				tooltipWrapStyle = new GUIStyle(EditorStyles.label);
				tooltipWrapStyle.wordWrap = true;
			}
		}

		private static readonly Regex applicationNamesRegex = new Regex(@"^[a-zA-Z0-9._\- ]*$");
		private static readonly string[] applicationNamesInvalidBoundCharacters = new [] { ".", "_", "-" };

		private const string ErrorHeader = "Please fix the following errors before proceeding:";
		private const string BuildErrorTitle = "Build Error";
		private const string BuildErrorMessage = "An error occured while building the application. Check the console output for more information.";
		private const string BuildErrorOk = "Ok";

		private AppSettings mSettings;
		private UnityEditor.Editor mSettingsEditor;

		private Vector2 mScrollPosition;

		[SerializeField]
		private TreeViewState mTreeViewState;
		private SceneTreeView mTreeView = null;

		private bool mBuilding = false;

		[MenuItem("Window/CoffeeTable/Build to Table")]
		public static void BuildToTable()
		{
			BuildWindow window = GetWindow<BuildWindow>("Build To Table");
		}

		private void ActiveScenesGUI()
		{
			if (mTreeView == null)
			{
				if (mTreeViewState == null)
					mTreeViewState = new TreeViewState();
				mTreeView = new SceneTreeView(mTreeViewState);
				mTreeView.Reload();
			}

			using (new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				GUILayout.Label(Styles.scenesInBuild, EditorStyles.boldLabel);

				using (new GUILayout.HorizontalScope())
				{
					GUILayout.Space(Styles.margin);
					using (new GUILayout.VerticalScope())
					{
						GUILayout.Label(Styles.addScenesTooltip, Styles.tooltipWrapStyle);
						GUILayout.Space(Styles.margin);
						Rect rect = GUILayoutUtility.GetRect(0, position.width, 100, position.height);
						mTreeView.OnGUI(rect);
						using (new GUILayout.HorizontalScope())
						{
							GUILayout.FlexibleSpace();
							if (GUILayout.Button(Styles.addOpenSource))
								AddOpenScenes();
						}
					}
					GUILayout.Space(Styles.margin);
				}
				GUILayout.Space(Styles.margin);
			}

		}

		private void BeginBuild()
		{
			if (mBuilding) return;
			if (mSettings == null) return;
			if (mTreeView == null) return;

			if (BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling)
			{
				Log.BuildLog.LogError("Cannot begin build while a build is in progress or while scripts are being compiled.");
				return;
			}

			var scenes = mTreeView.GetSceneList();
			if (scenes == null || scenes.Length == 0 || !scenes.Where(i => i.enabled).Any()) return;

			try
			{
				mBuilding = true;
				using (var builder = new TableBuilder())
				{
					builder.Settings = mSettings;
					builder.Scenes = scenes;
					builder.LoggerDelegate = (l, o) => Log.BuildLog.Write(l, o);
					builder.ProgressDelegate = (title, description, progress) => EditorUtility.DisplayProgressBar(title, description, progress);
					builder.OnDispose = () => EditorUtility.ClearProgressBar();

					var result = builder.Build();
					if (result == BuildResult.Succeeded)
						ShowNotification(Styles.buildSucceededNotification);
					else if (result == BuildResult.Failed) 
						EditorUtility.DisplayDialog(BuildErrorTitle, BuildErrorMessage, BuildErrorOk);
				}
			}
			finally
			{
				mBuilding = false;
			}
		}

		void AddOpenScenes()
		{
			List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

			bool isSceneAdded = false;
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (scene.path.Length == 0 && !EditorSceneManager.SaveScene(scene, "", false))
					continue;

				if (list.Any(s => s.path == scene.path))
					continue;

				GUID newGUID;
				GUID.TryParse(AssetDatabase.AssetPathToGUID(scene.path), out newGUID);
				var buildSettingsScene = (newGUID == default(GUID)) ?
					new EditorBuildSettingsScene(scene.path, true) :
					new EditorBuildSettingsScene(newGUID, true);
				list.Add(buildSettingsScene);
				isSceneAdded = true;
			}

			if (!isSceneAdded)
				return;

			EditorBuildSettings.scenes = list.ToArray();
			mTreeView.Reload();
			Repaint();
			GUIUtility.ExitGUI();
		}

		private void OnEnable()
		{
			mSettings = MenuItems.GetAppSettings(false);
			if (mSettings == null)
			{
				Close();
				return;
			}
			mSettingsEditor = UnityEditor.Editor.CreateEditor(mSettings);
			Selection.activeObject = null;
		}

		private void OnGUI()
		{
			using (var scrollScope = new GUILayout.ScrollViewScope(mScrollPosition, false, true, GUILayout.ExpandWidth(true)))
			{
				mScrollPosition = scrollScope.scrollPosition;

				if (mSettings == null) return;
				mSettingsEditor?.OnInspectorGUI();

				EditorGUILayout.Space();
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				GUILayout.Label(Styles.titleText, EditorStyles.boldLabel);
				EditorGUILayout.EndVertical();

				var errors = GetValidationErrors(mSettings);

				ActiveScenesGUI();

				if (errors.Any())
				{
					const string seperator = "    •  ";
					var errorString = ErrorHeader + "\n";
					errorString += seperator;
					errorString += string.Join($"\n{seperator}", errors);
					EditorGUILayout.HelpBox(errorString, MessageType.Error, true);
				}

				if (mSettings.mIsApiEnabled == false)
				{
					EditorGUILayout.HelpBox("You are building to the coffee table, but in your app settings, 'Is API Enabled' is unchecked. " +
						"This will prevent you from accessing the coffee table's functionality from within Unity in the built executable.", MessageType.Warning, true);
				}

				using (new EditorGUI.DisabledGroupScope(errors.Any()))
				{
					GUILayout.Space(Styles.margin);
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();

					bool buildButtonPressed = GUILayout.Button(Styles.buildButton);
					if (buildButtonPressed) BeginBuild();

					GUILayout.Space(Styles.margin);
					if (!buildButtonPressed) GUILayout.EndHorizontal();
					GUILayout.Space(Styles.margin);
				}
			}
		}

		private IEnumerable<string> GetValidationErrors(AppSettings settings)
		{
			if (string.IsNullOrWhiteSpace(settings.mAppName))
				yield return "Please give your app a name.";
			else if (!applicationNamesRegex.IsMatch(settings.mAppName) || settings.mAppName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
				yield return "Your app's name is not valid. Valid app names are composed of alphanumeric characters, underscores, hyphens and periods.";
			else if (settings.mAppName.BoundedByAny(applicationNamesInvalidBoundCharacters))
				yield return $"Your app's name cannot start or end with any of the following characters: {string.Join(string.Empty, applicationNamesInvalidBoundCharacters)}";
			if (string.IsNullOrWhiteSpace(settings.mAuthorName))
				yield return "Please give your app an author name.";
			if (string.IsNullOrWhiteSpace(settings.mDescription))
				yield return "Please give your app a description.";
			if (settings.mIcon == null)
				yield return "Please give your app an icon.";
			else if (!settings.mIcon.isReadable)
				yield return "Your app's icon is not readable. Please check the icon asset and ensure that Read/Write Enabled is checked in the asset's advanced settings.";
			if (mTreeView != null && !mTreeView.GetSceneList().Where(scene => scene.enabled).Any())
				yield return "Please add or enable at least one scene in the build.";
			if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
				yield return "Your installation of Unity currently cannot build to Windows-64 environments, which this API requires to build to the coffee table. Please add windows build support through the Unity Hub.";
		}

		private void OnDisable()
		{
			if (mSettingsEditor != null)
				DestroyImmediate(mSettingsEditor);
		}
	}
}
