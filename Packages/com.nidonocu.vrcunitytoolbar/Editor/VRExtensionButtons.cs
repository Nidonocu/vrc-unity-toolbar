using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3 && !UDON
using VRC.SDK3.Avatars.Components;
#endif

#if UNITY_EDITOR
namespace UnityToolbarExtender.Nidonocu
{
    public enum AutoSelectOnPlayMode
    {
        None = 0,
        Avatar = 1,
        GestureManager = 2
    }

    public enum SmartDuplicationNumberFormat
    {
        SingleDigit = 0,
        DoubleDigit = 1,
        TripleDigit = 2,
    }

    public enum SmartDuplicationBrackets
    {
        None = 0,
        Rounded = 1,
        Square = 2,
        Curly = 3,
        Angular = 4
    }

    public enum SmartDuplicationSeparator
    {
        None = 0,
        Space = 1,
        Pipe = 2,
        Dash = 3,
        Dot = 4,
        Underscore = 5,
        SpacedDash = 6,
        SpacedPipe = 7,
    }

    public enum SmartDuplicationPromptToRename
    {
        Never = 0,
        OnlyAssets = 1,
        OnlyGameObjects = 2,
        Everything = 3
    }

    [InitializeOnLoad]
    public static class VRExtensionButtons
    {
        static Object CurrentSelection = null;

        static Texture BackIcon;

        static Texture ForwardIcon;

        static Texture SmartDuplicateIcon;

        static Texture SceneIcon;

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3 && !UDON
        static Object lastSelectedObjectBeforePlay = null;

        static Texture AvatarIcon;
        static Texture VRCMenuIcon;

        static string GestureManagerAssemblyQualifiedTypeName = "BlackStartX.GestureManager.GestureManager, vrchat.blackstartx.gesture-manager, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        
        static bool? packageStatus = null;
#endif

        static bool NavigatingStack = false;

        public static VRExtensionButtonsSettings settings;

        public static SerializedObject settingsObject;

        static VRExtensionButtons()
        {
            // Remove old editor prefs
            EditorPrefs.DeleteKey("SwitchToScene");
            EditorPrefs.DeleteKey("SelectAvatar");

            settings = VRExtensionButtonsSettings.GetOrCreateSettings();
            settingsObject = new SerializedObject(settings);

            System.Action selectionAction = OnSelectionChanged;
            Selection.selectionChanged += selectionAction;
            ToolbarExtender.LeftToolbarGUI.Add(OnBackGUI);
            ToolbarExtender.LeftToolbarGUI.Add(OnForwardGUI);
            ToolbarExtender.LeftToolbarGUI.Add(OnDuplicateSelectionGUI);

            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            ToolbarExtender.RightToolbarGUI.Add(OnSceneToolbarGUI);

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3 && !UDON
            if (packageStatus == null)
            {
                packageStatus = CheckInstalledPackage.IsPackageInstalled(CheckInstalledPackage.GestureManagerPackageName);
            }
            EditorApplication.pauseStateChanged += OnPauseChanged;
            ToolbarExtender.RightToolbarGUI.Add(OnSelectAvatarToolbarGUI);
            if (packageStatus == true)
            {
                ToolbarExtender.RightToolbarGUI.Add(OnSelectGestureManagerToolbarGUI);
            }
#endif
        }

        static void OnPauseChanged(PauseState obj)
        {
            if (settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.switchToScene)).boolValue && obj == PauseState.Unpaused)
            {
                // Not sure why, but this must be delayed
                EditorApplication.delayCall += EditorWindow.FocusWindowIfItsOpen<SceneView>;
            }
        }

        static void OnPlayModeChanged(PlayModeStateChange obj)
        {
            if (settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.switchToScene)).boolValue && obj == PlayModeStateChange.EnteredPlayMode)
            {
                EditorWindow.FocusWindowIfItsOpen<SceneView>();
            }

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3 && !UDON
            var currentAutoSelectionMode = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.autoSelectOnPlay)).enumValueIndex;

            // Avatar selected and going to Play mode
            if (currentAutoSelectionMode == (int)AutoSelectOnPlayMode.Avatar && obj == PlayModeStateChange.EnteredPlayMode)
            {
                var selected = Selection.activeTransform;

                if (selected != null)
                {
                    Component childDesc = null;
                    childDesc = selected.gameObject.GetComponentInChildren<VRCAvatarDescriptor>();
                    if (childDesc == null)
                    {
                        // If no AVD, check parent objects for one and select that
                        Component parentDesc = null;
                        parentDesc = selected.gameObject.GetComponentInParent<VRCAvatarDescriptor>();
                        if (parentDesc != null)
                        {
                            lastSelectedObjectBeforePlay = Selection.activeObject;
                            Selection.activeTransform = parentDesc.gameObject.transform;
                            return;
                        }
                    }
                    else
                    {
                        // If not null, if it has an AV Descriptor, do nothing
                        return;
                    }
                }
                // If null, find the first active object with an AV Descriptor
                Object[] presentAVDs = new Object[0];
                presentAVDs = Object.FindObjectsOfType<VRCAvatarDescriptor>();
                foreach (Component presentAVD in presentAVDs)
                {
                    if (presentAVD.gameObject.activeInHierarchy)
                    {
                        lastSelectedObjectBeforePlay = Selection.activeObject;
                        Selection.activeTransform = presentAVD.gameObject.transform;
                        break;
                    }
                }
            }
            // GM selected and going to Play mode
            if (currentAutoSelectionMode == (int)AutoSelectOnPlayMode.GestureManager && obj == PlayModeStateChange.EnteredPlayMode)
            {
                if (packageStatus == null)
                {
                    packageStatus = CheckInstalledPackage.IsPackageInstalled(CheckInstalledPackage.GestureManagerPackageName);
                }
                if (packageStatus == true)
                {
                    var selected = Selection.activeTransform;
                    lastSelectedObjectBeforePlay = Selection.activeObject;

                    var GestureManagerObj = Object.FindObjectOfType(System.Type.GetType(GestureManagerAssemblyQualifiedTypeName));
                    if (GestureManagerObj == null)
                    {
                        EditorApplication.ExecuteMenuItem("Tools/Gesture Manager Emulator");
                        GestureManagerObj = Object.FindObjectOfType(System.Type.GetType(GestureManagerAssemblyQualifiedTypeName));
                        if (GestureManagerObj == null)
                        {
                            Debug.LogError("Unable to create Gesture Manager object!");
                        }
                    }
                    else
                    {
                        Selection.activeObject = GestureManagerObj;
                    }
                }
            }
            // Returning to Edit mode
            if (currentAutoSelectionMode != (int)AutoSelectOnPlayMode.None && obj == PlayModeStateChange.EnteredEditMode)
            {
                if (lastSelectedObjectBeforePlay != null)
                {
                    Selection.activeObject = lastSelectedObjectBeforePlay;
                    lastSelectedObjectBeforePlay = null;
                }
            }
#endif
        }

        const int MaximumStackSize = 100;

        static void OnSelectionChanged()
        {
            if (!NavigatingStack)
            {
                var backIDArray = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.BackIDsStack));
                backIDArray.arraySize++;
                int newIndex = backIDArray.arraySize - 1;
                var newIDItem = backIDArray.GetArrayElementAtIndex(newIndex);
                if (CurrentSelection != null)
                {
                    newIDItem.intValue = CurrentSelection.GetInstanceID();
                }

                if (backIDArray.arraySize > MaximumStackSize)
                {
                    backIDArray.DeleteArrayElementAtIndex(0);
                }

                var forwardIDArray = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.ForwardIDsStack));
                forwardIDArray.ClearArray();

                CurrentSelection = Selection.activeObject;

                settingsObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
            else { NavigatingStack = false; }
        }

        static void NavigateBack()
        {
            NavigatingStack = true;
            var backIDArray = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.BackIDsStack));
            int index = backIDArray.arraySize - 1;
            var iDItem = backIDArray.GetArrayElementAtIndex(index).intValue;
            Selection.activeObject = EditorUtility.InstanceIDToObject(iDItem);

            var forwardIDArray = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.ForwardIDsStack));
            forwardIDArray.arraySize++;
            int newIndex = forwardIDArray.arraySize - 1;
            var newIDItem = forwardIDArray.GetArrayElementAtIndex(newIndex);
            if (CurrentSelection != null)
            {
                newIDItem.intValue = CurrentSelection.GetInstanceID();
            }

            backIDArray.DeleteArrayElementAtIndex(backIDArray.arraySize - 1);

            CurrentSelection = Selection.activeObject;

            settingsObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
        }

        static void NavigateForward()
        {
            NavigatingStack = true;
            var forwardIDArray = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.ForwardIDsStack));
            int index = forwardIDArray.arraySize - 1;
            var iDItem = forwardIDArray.GetArrayElementAtIndex(index).intValue;
            Selection.activeObject = EditorUtility.InstanceIDToObject(iDItem);

            var backIDArray = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.BackIDsStack));
            backIDArray.arraySize++;
            int newIndex = backIDArray.arraySize - 1;
            var newIDItem = backIDArray.GetArrayElementAtIndex(newIndex);
            if (CurrentSelection != null)
            {
                newIDItem.intValue = CurrentSelection.GetInstanceID();
            }

            forwardIDArray.DeleteArrayElementAtIndex(forwardIDArray.arraySize - 1);

            CurrentSelection = Selection.activeObject;

            settingsObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
        }

        static void OnBackGUI()
        {
            if (BackIcon == null)
            {
                BackIcon = EditorGUIUtility.IconContent(@"ArrowNavigationLeft").image;
            }
            var backIDArray = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.BackIDsStack));
            EditorGUI.BeginDisabledGroup(backIDArray.arraySize == 0);
            if (GUILayout.Button(new GUIContent(null, BackIcon, "Navigate to previous selection"), "Command"))
            {
                NavigateBack();
            }
            EditorGUI.EndDisabledGroup();
        }

        static void OnForwardGUI()
        {
            if (ForwardIcon == null)
            {
                ForwardIcon = EditorGUIUtility.IconContent(@"ArrowNavigationRight").image;
            }
            var forwardIDArray = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.ForwardIDsStack));
            EditorGUI.BeginDisabledGroup(forwardIDArray.arraySize == 0);
            if (GUILayout.Button(new GUIContent(null, ForwardIcon, "Navigate to next selection"), "Command"))
            {
                NavigateForward();
            }
            EditorGUI.EndDisabledGroup();
        }

        static void OnDuplicateSelectionGUI()
        {
            if (SmartDuplicateIcon == null)
            {
                SmartDuplicateIcon = Resources.Load<Texture>("SmartDuplicate");
            }
            GUILayout.Space(10f);
            EditorGUI.BeginDisabledGroup(Selection.activeObject == null);
            if (GUILayout.Button(new GUIContent(null, SmartDuplicateIcon, "Smart Duplicate"), "Command"))
            {
                ExecuteSmartDuplication();
            }
            EditorGUI.EndDisabledGroup();
        }

        [MenuItem("Edit/Smart Duplication &d")]
        public static void ExecuteSmartDuplication()
        {
            if (!settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.smartDuplicationRunOnce)).boolValue)
            {
                EditorUtility.DisplayDialog("Smart Duplication",
                    @"You've just used the Smart Duplication feature of the VRC Unity Toolbar for the first time in this project. 

Just so you know, you can change the options of how duplicates are automatically numbered under Edit > Project Settings > VRC Unity Toolbar.

This dialog won't appear again in this project, sorry for the interruption!", "OK");
                settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.smartDuplicationRunOnce)).boolValue = true;
                settingsObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
            SmartDuplicate.PerformDuplication(settings);
        }

        static void OnSceneToolbarGUI()
        {
            if (SceneIcon == null)
            {
                SceneIcon = EditorGUIUtility.IconContent(@"UnityEditor.SceneView").image;
            }

            GUI.changed = false;

            GUILayout.Toggle(settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.switchToScene)).boolValue, new GUIContent(null, SceneIcon, "Focus SceneView when entering play mode"), "Command");
            if (GUI.changed)
            {
                settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.switchToScene)).boolValue = !settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.switchToScene)).boolValue;
                settingsObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
        }
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3 && !UDON
        static void OnSelectAvatarToolbarGUI()
        {
            if (AvatarIcon == null)
            {
                AvatarIcon = EditorGUIUtility.IconContent(@"d_Avatar Icon").image;
            }

            GUI.changed = false;

            var currentAutoSelectionMode = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.autoSelectOnPlay)).enumValueIndex;

            GUILayout.Toggle(currentAutoSelectionMode == ((int)AutoSelectOnPlayMode.Avatar), new GUIContent(null, AvatarIcon, "Select the active avatar when entering play mode"), "Command");
            if (GUI.changed)
            {
                if (currentAutoSelectionMode == ((int)AutoSelectOnPlayMode.Avatar))
                {
                    settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.autoSelectOnPlay)).enumValueIndex = (int)AutoSelectOnPlayMode.None;
                }
                else
                {
                    settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.autoSelectOnPlay)).enumValueIndex = (int)AutoSelectOnPlayMode.Avatar;
                }
                settingsObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
        }

        static void OnSelectGestureManagerToolbarGUI()
        {
            if (VRCMenuIcon == null)
            {
                VRCMenuIcon = Resources.Load<Texture>("VRC_Menu_Icon");
            }

            GUI.changed = false;

            var currentAutoSelectionMode = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.autoSelectOnPlay)).enumValueIndex;

            GUILayout.Toggle(currentAutoSelectionMode == ((int)AutoSelectOnPlayMode.GestureManager), new GUIContent(null, VRCMenuIcon, "Select the gesture manager control object when entering play mode"), "Command");
            if (GUI.changed)
            {
                if (currentAutoSelectionMode == ((int)AutoSelectOnPlayMode.GestureManager))
                {
                    settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.autoSelectOnPlay)).enumValueIndex = (int)AutoSelectOnPlayMode.None;
                }
                else
                {
                    settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.autoSelectOnPlay)).enumValueIndex = (int)AutoSelectOnPlayMode.GestureManager;
                }
                settingsObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
        }
#endif
    }
}
#endif