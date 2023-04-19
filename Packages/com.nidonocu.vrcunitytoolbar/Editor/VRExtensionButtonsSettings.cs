using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace UnityToolbarExtender.Nidonocu
{
    public class VRExtensionButtonsSettings : ScriptableObject
    {
        public bool switchToScene = false;

        public AutoSelectOnPlayMode autoSelectOnPlay = AutoSelectOnPlayMode.None;

        public List<int> BackIDsStack = new List<int>();

        public List<int> ForwardIDsStack = new List<int>();

        public static string SettingsPath = "Assets/Settings/VRCUnityToolbarSettings.asset";

        private static VRExtensionButtonsSettings _settings;

        public static VRExtensionButtonsSettings GetSettings()
        {
            if (_settings)
                return _settings;

             var settings = AssetDatabase.LoadAssetAtPath<VRExtensionButtonsSettings>(SettingsPath);

            if (settings == null)
                _settings = settings = CreateInstance<VRExtensionButtonsSettings>();

            return settings;
        }

        internal static VRExtensionButtonsSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<VRExtensionButtonsSettings>(SettingsPath);
            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(SettingsPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));

                _settings = settings = CreateInstance<VRExtensionButtonsSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }
    }
    internal static class VRExtensionButtonsSettingsProvider
    {
        private static readonly GUIContent switchToSceneLabel = new GUIContent("Switch to Scene View when Playing", "Will automatically set the active Viewport to the Scene view after pressing Play in the Unity Editor, allowing access to tools and gizmos and the editor flying camera.");

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/VRC Unity Toolbar", SettingsScope.Project)
            {
                label = "VRC Unity Toolbar",
                keywords = new HashSet<string>(new string[] { "vrc", "unity", "toolbar", "avatar", "scene", "play", "gesture", "manager", "duplicate", "copy", "numbering" }),
                guiHandler = (searchContext) =>
                {
                    var settings = VRExtensionButtonsSettings.GetOrCreateSettings();
                    var settingsObject = new SerializedObject(settings);

                    EditorGUIUtility.labelWidth = 300f;

                    EditorGUI.BeginChangeCheck();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.switchToScene)), switchToSceneLabel);

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("VRC Unity Toolbar created by Nidonocu © 2023", EditorStyles.boldLabel);

                    EditorGUIUtility.labelWidth = 0f;

                    EditorGUI.indentLevel--;
                    if (EditorGUI.EndChangeCheck())
                    {
                        settingsObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(settings);
                    }
                },
            };

            return provider;
        }

    }
}
#endif