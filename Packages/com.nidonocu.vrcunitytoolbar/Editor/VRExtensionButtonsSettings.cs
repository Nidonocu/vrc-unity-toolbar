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

        public bool enableSmartDuplicationOverride = true;

        public bool smartDuplicationRunOnce = false;

        public bool smartDuplicationCountsAtZero = false;

        public SmartDuplicationNumberFormat smartDuplicationNumberFormat = SmartDuplicationNumberFormat.SingleDigit;

        public SmartDuplicationBrackets smartDuplicationBrackets = SmartDuplicationBrackets.None;

        public SmartDuplicationSeparator smartDuplicationSeparator = SmartDuplicationSeparator.Space;

        public SmartDuplicationPromptToRename smartDuplicationPromptToRename = SmartDuplicationPromptToRename.Everything;

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
        private static readonly GUIContent switchToSceneLabel = new GUIContent(
            "Switch to Scene View when Playing",
            "Will automatically set the active Viewport to the Scene view after pressing Play in the Unity Editor, " + 
            "allowing access to tools and gizmos and the editor flying camera.");
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3 && !UDON
        private static readonly GUIContent autoSelectOnPlayLabel = new GUIContent(
            "When Playing, automatically select:",
            "When configured, you will be able to automatically switch selection on playback to the selected object in, " +
            "the scene. Selection will revert to what was selected prior to play mode when returning to edit mode.");

        private static readonly string autoSelectNoneDetails = 
            "No automatic selection will take place, the currently selected object when you entered play mode will remain selected.";

        private static readonly string autoSelectAvatarDetails =
            @"The root avatar object will attempt to be selected when entering play mode. 
If there a multiple active avatars in the scene, then the parent avatar of the currently selected object will be selected. 
If no parent can be found, then the first avatar in the scene will be selected.";

        private static readonly string autoSelectGestureManDetails =
            @"The Gesture Manager control object will attempt to be selected when entering play mode. 
If the Gesture Manager control object can't be found, one will be automatically added to the scene.
This option of course, requires the Gesture Manager package to be installed!";

        static bool? packageStatus = null;

#else
        private static readonly string notAvatarProject =
            @"This is not an avatar project so this option is not available.";
#endif
        private static readonly string smartDuplicationExplain = @"Smart Duplication provides smarter behaviour when duplicating objects and assets in Unity.

Use the shortcut key, menu option or toolbar button to perform smart duplication.";

        //Hold Shift to also rename the current item to be the first in the duplicated list.";

        /*private static readonly GUIContent enableSmartDuplicationOverrideLabel = new GUIContent(
           "Perform Smart Duplication on Ctrl + D",
           "When configured, pressing Ctrl + D will use Smart Duplication instead of normal Unity Duplication. Otherwise, use the shortcut Alt + D.");*/

        private static readonly GUIContent smartDuplicationCountsAtZeroLabel = new GUIContent(
           "Start counting Smart Duplicates from Zero",
           "When enabled, the first smart duplicate (when no existing number is present) will use the number 1, otherwise it will use the number 2.");

        private static readonly GUIContent smartDuplicationNumberFormatLabel = new GUIContent(
           "Numeric Format for automatic numbering",
           "This number format will be used when auto-numbering a duplicate.");

        private static readonly GUIContent smartDuplicationSeparatorLabel = new GUIContent(
           "Separator for automatic numbering",
           "This separator will be used between the existing object name and the appended number when auto-numbering a duplicate.");

        private static readonly GUIContent smartDuplicationBracketsLabel = new GUIContent(
           "Brackets for automatic numbering",
           "These brackets will be placed around the number when auto-numbering a duplicate.");

        /*private static readonly GUIContent smartDuplicationPromptToRenameLabel = new GUIContent(
           "Prompty to Rename after Duplicating",
           "If configured, the editor will automatically enter renaming mode when duplicating to let you change the suggested name.");*/

        private static readonly string exampleHelp = @"Here are some examples of how automatic duplication will name your duplicates based on these settings:
* {0} will be duplicated to {1}
* {2} will be duplicated to {3}
* {4}, in the Asset Database, will be duplicated to {5}
Note: Some symbol choices can not be used for Asset duplication and alternatives will be used.";

        private static readonly string tooltipHelp =
            @"Hover over any option's label for more information!";
        /// <summary>
        /// Developer Logo
        /// </summary>
        private static readonly Texture Nido_Logo = Resources.Load<Texture>("nido_logo");

        /// <summary>
        /// URL for this package's home
        /// </summary>
        const string PackageURL = "https://nidonocu.github.io/vrc-unity-toolbar/";

        /// <summary>
        /// URL for developer's profile
        /// </summary>
        const string ProfileURL = "https://nidonocu.github.io/Virtual-Gryphon-Packages/";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/VRC Unity Toolbar", SettingsScope.Project)
            {
                label = "VRC Unity Toolbar",
                keywords = new HashSet<string>(new string[] { "vrc", "unity", "toolbar", "avatar", "scene", "play", "gesture", "manager", "duplicate", "copy", "numbering" }),
                guiHandler = (searchContext) =>
                {
                    var settings = VRExtensionButtons.settings;
                    var settingsObject = VRExtensionButtons.settingsObject;

                    EditorGUILayout.HelpBox(tooltipHelp, MessageType.Info);
                    EditorGUILayout.Space();

                    EditorGUIUtility.labelWidth = 300f;

                    EditorGUI.BeginChangeCheck();
                    EditorGUI.indentLevel++;

                    EditorGUILayout.LabelField("Scene View Override", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.switchToScene)), switchToSceneLabel);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();

                    DrawUILine(Color.gray);

                    EditorGUILayout.LabelField("Change Selection on Play", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3 && !UDON
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.autoSelectOnPlay)), autoSelectOnPlayLabel);

                    var helpString = "";
                    switch (settings.autoSelectOnPlay)
                    {
                        case AutoSelectOnPlayMode.None:
                            helpString = autoSelectNoneDetails;
                            break;
                        case AutoSelectOnPlayMode.Avatar:
                            helpString = autoSelectAvatarDetails;
                            break;
                        case AutoSelectOnPlayMode.GestureManager:
                            helpString = autoSelectGestureManDetails;
                            break;
                    }
                    EditorGUILayout.HelpBox(helpString, MessageType.Info);
                    if (settings.autoSelectOnPlay == AutoSelectOnPlayMode.GestureManager)
                    {
                        if (packageStatus == null)
                        {
                            packageStatus = CheckInstalledPackage.IsPackageInstalled(CheckInstalledPackage.GestureManagerPackageName);
                        }
                        if (packageStatus == false)
                        {
                            EditorGUILayout.HelpBox("Gesture Manager is not installed! Install it first using the Creator Companion!", MessageType.Warning);
                        }
                    }
#else
                    EditorGUILayout.HelpBox(notAvatarProject, MessageType.Info);
#endif
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();

                    DrawUILine(Color.gray);

                    EditorGUILayout.LabelField("Smart Duplication", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(smartDuplicationExplain, EditorStyles.wordWrappedLabel);
                    EditorGUI.indentLevel++;
                    //EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.enableSmartDuplicationOverride)), enableSmartDuplicationOverrideLabel);
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.smartDuplicationCountsAtZero)), smartDuplicationCountsAtZeroLabel);
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.smartDuplicationNumberFormat)), smartDuplicationNumberFormatLabel);
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.smartDuplicationSeparator)), smartDuplicationSeparatorLabel);
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.smartDuplicationBrackets)), smartDuplicationBracketsLabel);
                    //EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.smartDuplicationPromptToRename)), smartDuplicationPromptToRenameLabel);
                    var example1 = SmartDuplicate.CreateDuplicateName("ExampleObject", settings);
                    var example2 = SmartDuplicate.CreateDuplicateName("Object7Example", settings);
                    var example3 = SmartDuplicate.CreateDuplicateName("MaterialAsset", settings, true);
                    EditorGUILayout.HelpBox(string.Format(exampleHelp, "ExampleObject", example1, "Object7Example", example2, "MaterialAsset", example3), MessageType.Info);
                    EditorGUI.indentLevel--;

                    DrawUILine(Color.gray);

                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button(new GUIContent("Package Homepage", null, PackageURL), GUILayout.Height(48)))
                    {
                        Application.OpenURL(PackageURL);
                    }
                    if (GUILayout.Button(new GUIContent("      Nidonocu", Nido_Logo, ProfileURL), GUILayout.Height(48)))
                    {
                        Application.OpenURL(ProfileURL);
                    }

                    EditorGUILayout.EndHorizontal();


                    EditorGUILayout.Space(20f);

                    EditorGUILayout.LabelField("VRC Unity Toolbar - Version 2.0.0 - Created by Nidonocu © 2023", EditorStyles.boldLabel);

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

#region UI Support Functions
        /// <summary>
        /// Draw a horizontal line across the UI
        /// </summary>
        /// <param name="color">The colour of the line</param>
        /// <param name="thickness">The thickness in pixels</param>
        /// <param name="padding">The top and bottom padding around the line in pixels</param>
        public static void DrawUILine(Color color, int thickness = 1, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.width -= padding;
            r.height = thickness;
            r.x += padding / 2;
            r.y += padding / 2;
            EditorGUI.DrawRect(r, color);
        }
#endregion

    }
}
#endif