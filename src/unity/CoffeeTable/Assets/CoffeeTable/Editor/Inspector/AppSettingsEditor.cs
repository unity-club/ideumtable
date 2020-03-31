using CoffeeTable;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AppSettings))]
public class AppSettingsEditor : Editor
{
	private static class Styles
	{
		static Styles()
		{
			GUIStyle _boldFoldout = EditorStyles.foldout;
			_boldFoldout.fontStyle = FontStyle.Bold;
			boldFoldout = _boldFoldout;
		}

		public static readonly GUIContent titleText = new GUIContent("Coffee Table Settings");

		public static readonly GUIStyle boldFoldout;

		public static readonly GUIContent appSettingsHeader = new GUIContent("App Settings", "The application settings hold various pieces of metadata about your application, such as its name, author, and description needed to identify your app.");
		public static readonly GUIContent appSettingsName = new GUIContent("App Name", "The name of your application. No two application names can be the same, so be sure to choose something unique!");
		public static readonly GUIContent appSettingsAuthor = new GUIContent("Author(s)", "The people who made this application.");
		public static readonly GUIContent appSettingsDescription = new GUIContent("Description", "A short description about the app and and what it does.");
		public static readonly GUIContent appSettingsFullscreen = new GUIContent("Launch in Fullscreen", "If checked, your app will be launched in fullscreen by default, otherwise it will be launched as a half-screen app.");

		public static readonly GUIContent apiSettingsHeader = new GUIContent("API Settings");
		public static readonly GUIContent apiSettingsEnabled = new GUIContent("Enabled", "Uncheck to turn off the CoffeeTable API and all its functionality.");
		public static readonly GUIContent apiSettingsFallbackTcpPort = new GUIContent("Fallback TCP Port", "The TCP port that should be used to connect to the backend service's TCP server when no TCP port could be found otherwise.");
		public static readonly GUIContent apiSettingsFallbackHttpPort = new GUIContent("Fallback HTTP Port", "The HTTP port that should be used to connect to the backend service's HTTP server when no HTTP port could be found otherwise.");
	}

	private SerializedProperty mFallbackHttpPort;
	private SerializedProperty mFallbackTcpPort;
	private SerializedProperty mAppName;
	private SerializedProperty mAuthorName;
	private SerializedProperty mDescription;
	private SerializedProperty mLaunchInFullscreen;
	private SerializedProperty mIsApiEnabled;

	private bool mShowAppSettings = false;
	private bool mShowApiSettings = false;

	private void OnEnable()
	{
		if (target == null) return;
		mFallbackHttpPort = serializedObject.FindProperty(nameof(AppSettings.mFallbackHttpPort));
		mFallbackTcpPort = serializedObject.FindProperty(nameof(AppSettings.mFallbackTcpPort));
		mAppName = serializedObject.FindProperty(nameof(AppSettings.mAppName));
		mAuthorName = serializedObject.FindProperty(nameof(AppSettings.mAuthorName));
		mDescription = serializedObject.FindProperty(nameof(AppSettings.mDescription));
		mLaunchInFullscreen = serializedObject.FindProperty(nameof(AppSettings.mLaunchInFullscreen));
		mIsApiEnabled = serializedObject.FindProperty(nameof(AppSettings.mIsApiEnabled));
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.BeginVertical(EditorStyles.helpBox);
		GUILayout.Label(Styles.titleText, EditorStyles.boldLabel);
		EditorGUILayout.EndVertical();

		EditorGUI.indentLevel = 1;

		// App Settings Proper
		EditorGUILayout.BeginVertical(EditorStyles.helpBox);
		mShowAppSettings = EditorGUILayout.Foldout(mShowAppSettings, Styles.appSettingsHeader, Styles.boldFoldout);
		if (mShowAppSettings)
		{
			EditorGUILayout.PropertyField(mAppName, Styles.appSettingsName);
			EditorGUILayout.PropertyField(mAuthorName, Styles.appSettingsAuthor);
			EditorGUILayout.LabelField(Styles.appSettingsDescription);
			mDescription.stringValue = EditorGUILayout.TextArea(mDescription.stringValue, GUILayout.MaxHeight(75));
			EditorGUILayout.PropertyField(mLaunchInFullscreen, Styles.appSettingsFullscreen);
			EditorGUILayout.Space();
		}
		EditorGUILayout.EndVertical();

		// API Settings
		EditorGUILayout.BeginVertical(EditorStyles.helpBox);
		mShowApiSettings = EditorGUILayout.Foldout(mShowApiSettings, Styles.apiSettingsHeader, Styles.boldFoldout);
		if (mShowApiSettings)
		{
			EditorGUILayout.PropertyField(mIsApiEnabled, Styles.apiSettingsEnabled);
			EditorGUILayout.PropertyField(mFallbackTcpPort, Styles.apiSettingsFallbackTcpPort);
			EditorGUILayout.PropertyField(mFallbackHttpPort, Styles.apiSettingsFallbackHttpPort);
		}
		EditorGUILayout.EndVertical();

		if (serializedObject.ApplyModifiedProperties())
			EditorUtility.SetDirty(target);
	}
}
