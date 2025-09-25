#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace UnityToolbarExtender.Nidonocu.BulkActionTools
{
    public class UpdateFonts : EditorWindow
    {
        private Texture TextIcon;

        private Font selectedFont;
        private TMP_FontAsset selectedTMPFont;

        private GameObject rootObject;

        private List<Component> FoundComponents = new List<Component>();

        private bool showComponentsList = false;

        Vector2 componentsScrollPositon = Vector2.zero;

        [MenuItem("Tools/Nidonocu/Change UI Component Fonts...", false, 100)]
        static void Init()
        {
            UpdateFonts window = (UpdateFonts)EditorWindow.GetWindow(typeof(UpdateFonts), true, "Change UI Component Fonts", true);
            window.minSize = new Vector2(400, 500);
            window.maxSize = new Vector2(400, 500);
            window.Show();
        }

        void OnGUI()
        {
            if (TextIcon == null)
            {
                TextIcon = EditorGUIUtility.IconContent(@"d_Text Icon").image;
            }

            var WrappedLabelStyle = new GUIStyle(EditorStyles.wordWrappedLabel);

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.Label(new GUIContent("Use this tool to quickly change the font in all TextMeshPro and/or " +
                "legacy Text components in an object and all its children.", TextIcon),
                WrappedLabelStyle);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will not adjust font size or check font is a good one to use for a user interface!" +
                "\nMake sure the font you choose is easy to read!",
                MessageType.Warning);
            GUILayout.Space(20);

            // TARGET
            GUILayout.Label(
                "Select the object that contains the controls you want to change, such as a UI Canvas:",
                WrappedLabelStyle
                );
            rootObject = (GameObject)EditorGUILayout.ObjectField("Root Object", rootObject, typeof(GameObject), true);

            if (rootObject != null)
            {
                FoundComponents.Clear();
                SearchForComponents(typeof(Text));
                SearchForComponents(typeof(TextMeshPro));
                SearchForComponents(typeof(TextMeshProUGUI));
            }
            else
            {
                FoundComponents.Clear();
            }

            if (FoundComponents.Count > 0)
            {
                GUILayout.Space(10);
                showComponentsList = GUIHelperFunctions.RenderResultsList(
                    showComponentsList,
                    "Found Components",
                    ref componentsScrollPositon,
                    FoundComponents
                    );
            }
            else if (rootObject != null)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox(
                "The selected object contains no legacy text or TextMeshPro text components. There is nothing to update!",
            MessageType.Error);
            }
            GUILayout.Space(20);

            // FONTS
            GUILayout.Label(
                "Select the font asset you want to choose, choose either a font asset or TextMeshPro font asset:",
                WrappedLabelStyle
                );

            if (selectedFont != null && selectedTMPFont == null)
            {
                FindTMPFont();
            }
            else if (selectedFont == null && selectedTMPFont != null)
            {
                if (selectedTMPFont.sourceFontFile != null)
                {
                    selectedFont = selectedTMPFont.sourceFontFile;
                }
            }

            selectedFont = (Font)EditorGUILayout.ObjectField("Select Font Asset", selectedFont, typeof(Font), false);
            selectedTMPFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Select TMP Font Asset", selectedTMPFont, typeof(TMP_FontAsset), false);

            CheckFontMatch();

            GUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(
                (selectedFont == null && selectedTMPFont == null) ||
                rootObject == null ||
                FoundComponents.Count == 0
                );

            if (GUILayout.Button("Replace Font"))
            {
                var confirm = true;
                if (CheckIfHasComponentOfType(typeof(Text)) && selectedFont == null)
                {
                    confirm = EditorUtility.DisplayDialog(
                        "Change UI Component Fonts",
                        "You did not select a standard Font asset but some legacy text components exist within your selected object or its children." +
                        "\n If you continue, these objects will NOT have their font updated.",
                        "Continue Anyway",
                        "Cancel"
                        );
                }
                else if ((CheckIfHasComponentOfType(typeof(TextMeshPro)) ||
                    CheckIfHasComponentOfType(typeof(TextMeshProUGUI))) &&
                    selectedTMPFont == null)
                {
                    confirm = EditorUtility.DisplayDialog(
                        "Change UI Component Fonts",
                        "You did not select a TextMeshPro Font asset but some TextMeshPro text components exist within your selected object or its children." +
                        "\n If you continue, these objects will NOT have their font updated.",
                        "Continue Anyway",
                        "Cancel"
                        );
                }

                if (confirm)
                {
                    var legacyCount = PerformTextReplacement();
                    var tmpCount = PerformTextMeshProReplacement();

                    EditorUtility.DisplayDialog(
                        "Font Update Complete",
                        $"{legacyCount} Legacy Text Component{(legacyCount == 1 ? "" : "s")} were updated" +
                        $"\n{tmpCount} TextMeshPro Component{(tmpCount == 1 ? "" : "s")} were updated",
                        "OK"
                        );
                }
            }
            EditorGUI.EndDisabledGroup();

            GUIHelperFunctions.DrawFooter();

            GUILayout.EndVertical();
        }

        void FindTMPFont()
        {
            string[] foundFontAssets = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (var foundAsset in foundFontAssets)
            {
                var tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(foundAsset));
                if (tmpFont != null && tmpFont.sourceFontFile == selectedFont)
                {
                    selectedTMPFont = tmpFont;
                    break;
                }
            }
        }

        void CheckFontMatch()
        {
            if (selectedFont != null && selectedTMPFont != null)
            {
                if (selectedFont != selectedTMPFont.sourceFontFile)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.HelpBox(
                    "The selected TextMeshPro font doesn't appear to be based on the same font as the selected legacy font asset." +
                    "\nYou might have selected the wrong fonts. Check the two selected assets are correct.",
                MessageType.Warning);
                }
            }
        }

        void SearchForComponents(Type objectType)
        {
            foreach (var foundComponent in rootObject.GetComponentsInChildren(objectType, true))
            {
                FoundComponents.Add(foundComponent);
            }
        }

        bool CheckIfHasComponentOfType(Type objectType)
        {
            var result = rootObject.GetComponentInChildren(objectType, true);
            return (result != null);
        }

        int PerformTextReplacement()
        {
            var count = 0;

            if (selectedFont)
            {
                foreach (Text text in rootObject.GetComponentsInChildren<Text>(true))
                {
                    text.font = selectedFont;
                    count++;
                }
            }

            return count;
        }

        int PerformTextMeshProReplacement()
        {
            var count = 0;
            if (selectedTMPFont)
            {
                foreach (TextMeshPro item in rootObject.GetComponentsInChildren<TextMeshPro>(true))
                {
                    item.font = selectedTMPFont;
                    count++;
                }
                foreach (TextMeshProUGUI item in rootObject.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    item.font = selectedTMPFont;
                    count++;
                }
            }

            return count;
        }
    }
}
#endif
