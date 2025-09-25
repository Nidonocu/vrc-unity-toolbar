#if UNITY_EDITOR
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace UnityToolbarExtender.Nidonocu.BulkActionTools
{
    public class AnimationsInAnimatorsHelpWindow : EditorWindow
    {
        private Texture AnimatorIcon;
        private Texture FoldOutIcon;
        private Texture AnimationIcon;

        [MenuItem("Tools/Nidonocu/Store Animations in Animators...", false, 200)]
        static void Init()
        {

            AnimationsInAnimatorsHelpWindow window = (AnimationsInAnimatorsHelpWindow)EditorWindow.GetWindow(
                typeof(AnimationsInAnimatorsHelpWindow), 
                true,
                "Store Animations in Animators",
                true);
            window.minSize = new Vector2(400, 600);
            window.maxSize = new Vector2(400, 600);
            window.Show();
        }

        void OnGUI()
        {
            if (AnimatorIcon == null)
            {
                AnimatorIcon = EditorGUIUtility.IconContent(@"d_AnimatorController Icon").image;
            }

            if (FoldOutIcon == null)
            {
                FoldOutIcon = EditorGUIUtility.IconContent(@"d_IN_foldout_act").image;
            }

            if (AnimationIcon == null)
            {
                AnimationIcon = EditorGUIUtility.IconContent(@"d_AnimationClip Icon").image;
            }

            var WrappedLabelStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            var headerStyle = new GUIStyle(EditorStyles.largeLabel);
            var boldStyle = new GUIStyle(EditorStyles.boldLabel);

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(AnimatorIcon), GUILayout.MaxHeight(32f));
            GUILayout.Label("Menu options have been added to the Assets Menu to let you create Animation Clips inside an Animation Controller Asset.",
                WrappedLabelStyle, GUILayout.MaxHeight(32f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(FoldOutIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.Label(new GUIContent(AnimationIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.Label(new GUIContent(AnimationIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.Label(new GUIContent(AnimationIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.Label("Adding Child Animation Clips", headerStyle);
            GUILayout.Label(
                "To use these options, perform the following steps:" +
                "\n 1. Create an Animation Controller as normal." +
                "\n 2. Add all desired layers (if needed) and animation nodes to the controller." +
                "\n 3. Right-click the Animation Controller in the Project window and choose:",
                WrappedLabelStyle
                );
            GUILayout.Label("    Generate Child Animations", boldStyle);
            GUILayout.Space(5);
            GUILayout.Label(
                "Animation Clips will be created using Layer and Node names and placed as child assets " +
                "of the Animation Controller, keeping the two associated together.",
                WrappedLabelStyle
                );
            GUILayout.Space(5);
            GUILayout.Label(
                "If you add more animation nodes later, you can run the command again to add the additional animation clips " +
                "without changing existing ones.",
                WrappedLabelStyle
                );

            GUILayout.Space(10);

            GUILayout.Label("Removing Child Animation Clips", headerStyle);

            GUILayout.Label(
                "Animation Clips once added cannot be individually deleted, but all generated clips can be wiped at once." + 
                "\n To do this, right-click the animation controller and choose:",
                WrappedLabelStyle
                );
            GUILayout.Label("    Delete Child Animations", boldStyle);

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Once deleted, child animations cannot be restored via Undo!",
                MessageType.Warning);
            GUILayout.Space(20);

            if (GUILayout.Button("Close"))
            {
                Close();
            }

            GUIHelperFunctions.DrawFooter();

            GUILayout.EndVertical();
        }
    }

    public class AnimationsInAnimators : MonoBehaviour
    {
        [MenuItem("Assets/Generate Child Animations", false, 100)]
        public static void AddToAnimator()
        {
            var confirm = EditorUtility.DisplayDialog("Add Animation Clips", "Create new child animation clips for all empty layers and states?", "Yes", "No");
            if (!confirm)
            {
                return;
            }
            var selectedAnimators = Selection.GetFiltered<AnimatorController>(SelectionMode.Assets);
            var selectedAnimator = selectedAnimators[0];

            var animationClips = new List<AnimationClip>();

            foreach (var layer in selectedAnimator.layers)
            {
                var layerName = layer.name;
                if (layerName == "Base Layer")
                {
                    if (selectedAnimator.layers.Length == 1)
                    {
                        layerName = "";
                    }
                    else
                    {
                        layerName = "Base - ";
                    }
                }
                else
                {
                    layerName = layerName + " - ";
                }
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.motion == null)
                    {
                        var newAnimationClip = new AnimationClip();
                        newAnimationClip.name = layerName + state.state.name;
                        animationClips.Add(newAnimationClip);
                        state.state.motion = newAnimationClip;
                    }
                }
            }
            foreach (var animationClip in animationClips)
            {
                AssetDatabase.AddObjectToAsset(animationClip, selectedAnimator);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(animationClip));
            }
            AssetDatabase.SaveAssets();
            Debug.Log(animationClips.Count.ToString() + " clip(s) created and added to " + selectedAnimator.name);
            EditorGUIUtility.PingObject(selectedAnimator);
        }

        [MenuItem("Assets/Generate Child Animations", true, 100)]
        public static bool AddToAnimatorValidation()
        {
            var checkSelected = Selection.GetFiltered<AnimatorController>(SelectionMode.Assets);
            if (checkSelected.Length == 1)
            {
                return true;
            }
            return false;
        }

        [MenuItem("Assets/Delete Child Animations", false, 101)]
        public static void DeleteFromAnimator()
        {
            var selectedAnimators = Selection.GetFiltered<AnimatorController>(SelectionMode.Assets);
            var selectedAnimator = selectedAnimators[0];

            var confirm = EditorUtility.DisplayDialog("Delete Animation Clips", "Are you sure you want to Delete all child animation clips stored in this animator?", "Delete Clips", "Cancel");
            if (!confirm)
            {
                return;
            }
            Thread.Sleep(1000);
            var confirm2 = EditorUtility.DisplayDialog("Delete Animation Clips", "No wait, seriously. ARE YOU SURE? This can't be undone.", "DELETE CLIPS", "Cancel");
            if (!confirm2)
            {
                return;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(selectedAnimator));
            foreach (var asset in assets)
            {
                if (asset.GetType() == typeof(AnimationClip))
                {
                    FindAndRemoveClipFromStateMachine(selectedAnimator, (AnimationClip)asset);
                    AssetDatabase.RemoveObjectFromAsset(asset);
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static void FindAndRemoveClipFromStateMachine(AnimatorController controller, AnimationClip animationClip)
        {
            foreach (var layer in controller.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.motion == animationClip)
                    {
                        state.state.motion = null;
                        return;
                    }
                }
            }
        }

        [MenuItem("Assets/Delete Child Animations", true, 101)]
        public static bool DeleteFromAnimatorValidation()
        {
            var checkSelected = Selection.GetFiltered<AnimatorController>(SelectionMode.Assets);
            if (checkSelected.Length == 1)
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(checkSelected[0]));
                var hasClips = false;
                foreach (var asset in assets)
                {
                    if (asset.GetType() == typeof(AnimationClip))
                    {
                        hasClips = true;
                        break;
                    }
                }
                if (hasClips)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
#endif