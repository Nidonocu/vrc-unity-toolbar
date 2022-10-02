using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif
// TODO: CCK Compatibility doesn't work with components, must be imported as a classic asset to work
/*#if CVR_CCK_EXISTS
using ABI.CCK.Components;
#endif*/

#if UNITY_EDITOR
namespace UnityToolbarExtender.Nidonocu
{
    [InitializeOnLoad]
    public static class VRExtensionButtons
    {
        static bool m_switchToScene;

        static bool SwitchToScene
        {
            get { return m_switchToScene; }
            set
            {
                m_switchToScene = value;
                EditorPrefs.SetBool("SwitchToScene", value);
            }
        }

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3 || CVR_CCK_EXISTS
        static bool m_selectAvatar;

        static bool SelectAvatar
        {
            get { return m_selectAvatar; }
            set
            {
                m_selectAvatar = value;
                EditorPrefs.SetBool("SelectAvatar", value);
            }
        }
#endif

        static Object lastSelectedObject = null;

        static VRExtensionButtons()
        {
            m_switchToScene = EditorPrefs.GetBool("SwitchToScene", false);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            ToolbarExtender.RightToolbarGUI.Add(OnSceneToolbarGUI);

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3 || CVR_CCK_EXISTS
            m_selectAvatar = EditorPrefs.GetBool("SelectAvatar", false);
            EditorApplication.pauseStateChanged += OnPauseChanged;
            ToolbarExtender.RightToolbarGUI.Add(OnSelectAvatarToolbarGUI);
#endif
        }

        static void OnPauseChanged(PauseState obj)
        {
            if (SwitchToScene && obj == PauseState.Unpaused)
            {
                // Not sure why, but this must be delayed
                EditorApplication.delayCall += EditorWindow.FocusWindowIfItsOpen<SceneView>;
            }
        }

        static void OnPlayModeChanged(PlayModeStateChange obj)
        {
            // Check for Build Mode
            var buildObject = GameObject.Find("VRCSDK");
            if (buildObject != null && buildObject.GetComponent("RuntimeBlueprintCreation") != null)
            {
                Debug.Log("In Build Mode, skipping Toolbar button actions");
                return;
            }

            if (SwitchToScene && obj == PlayModeStateChange.EnteredPlayMode)
            {
                EditorWindow.FocusWindowIfItsOpen<SceneView>();
            }

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3 || CVR_CCK_EXISTS
            if (SelectAvatar && obj == PlayModeStateChange.EnteredPlayMode)
            {
                var selected = Selection.activeTransform;

                if (selected != null)
                {
                    Component childDesc = null;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
                    childDesc = selected.gameObject.GetComponentInChildren<VRCAvatarDescriptor>();
#endif
/*
#if CVR_CCK_EXISTS
					if (childDesc == null)
                        childDesc = selected.gameObject.GetComponentInChildren<CVRAvatar>();
#endif
*/
                    if (childDesc == null)
                    {
                        // If no AVD, check parent objects for one and select that
                        Component parentDesc = null;
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
                        parentDesc = selected.gameObject.GetComponentInParent<VRCAvatarDescriptor>();
#endif
/*
#if CVR_CCK_EXISTS
                        if (parentDesc == null)
                            parentDesc = selected.gameObject.GetComponentInParent<CVRAvatar>();
#endif
*/
                        if (parentDesc != null)
                        {
                            lastSelectedObject = Selection.activeObject;
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
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
                presentAVDs = Object.FindObjectsOfType<VRCAvatarDescriptor>();
#endif
/*
#if CVR_CCK_EXISTS
                if (presentAVDs.Length == 0)
				    presentAVDs = Object.FindObjectsOfType<CVRAvatar>();
#endif
*/
                foreach (Component presentAVD in presentAVDs)
                {
                    if (presentAVD.gameObject.activeInHierarchy)
                    {
                        lastSelectedObject = Selection.activeObject;
                        Selection.activeTransform = presentAVD.gameObject.transform;
                        break;
                    }
                }
            }
            if (SelectAvatar && obj == PlayModeStateChange.EnteredEditMode)
            {
                if (lastSelectedObject != null)
                {
                    Selection.activeObject = lastSelectedObject;
                    lastSelectedObject = null;
                }
            }
#endif
        }

        static void OnSceneToolbarGUI()
        {
            var tex = EditorGUIUtility.IconContent(@"UnityEditor.SceneView").image;

            GUI.changed = false;

            GUILayout.Toggle(m_switchToScene, new GUIContent(null, tex, "Focus SceneView when entering play mode"), "Command");
            if (GUI.changed)
            {
                SwitchToScene = !SwitchToScene;
            }
        }
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3 || CVR_CCK_EXISTS
        static void OnSelectAvatarToolbarGUI()
        {
            var tex = EditorGUIUtility.IconContent(@"d_Avatar Icon").image;

            GUI.changed = false;

            GUILayout.Toggle(m_selectAvatar, new GUIContent(null, tex, "Select the active avatar when entering play mode"), "Command");
            if (GUI.changed)
            {
                SelectAvatar = !SelectAvatar;
            }
        }
#endif
    }
}
#endif