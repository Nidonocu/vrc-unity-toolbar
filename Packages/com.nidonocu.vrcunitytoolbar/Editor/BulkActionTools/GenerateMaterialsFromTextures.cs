#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityToolbarExtender.Nidonocu.BulkActionTools
{
    public class GenerateMaterialsFromTextures : EditorWindow
    {
        private Texture MaterialIcon;
        private Texture TextureIcon;
        private Texture FoldOutIcon;

        private string TextureFolderPath = string.Empty;

        private string MaterialFolderPath = string.Empty;

        private Material TemplateMaterial;

        private string[] MaterialTextureSlots = new string[0];

        private int SelectedTextureSlot = -1;

        private List<Material> Materials;

        private List<Texture2D> Textures;

        [MenuItem("Tools/Nidonocu/Generate Materials from Textures...", false, 300)]
        static void Init()
        {
            GenerateMaterialsFromTextures window = (GenerateMaterialsFromTextures)EditorWindow.GetWindow(
                typeof(GenerateMaterialsFromTextures), true, "Generate Materials from Textures", true);
            window.minSize = new Vector2(300, 600);
            window.maxSize = new Vector2(300, 600);
            window.Show();
        }

        void OnGUI()
        {
            if (MaterialIcon == null)
            {
                MaterialIcon = EditorGUIUtility.IconContent(@"d_Material Icon").image;
            }

            if (FoldOutIcon == null)
            {
                FoldOutIcon = EditorGUIUtility.IconContent(@"d_IN_foldout_act").image;
            }

            if (TextureIcon == null)
            {
                TextureIcon = EditorGUIUtility.IconContent(@"d_Texture Icon").image;
            }

            var WrappedLabelStyle = new GUIStyle(EditorStyles.wordWrappedLabel);

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.Label(new GUIContent("Use this tool to generate a set of materials based on a template " +
                "and a collection of textures."),
                WrappedLabelStyle);
            DrawHeaderGraphic();
            GUILayout.Space(20);

            GUILayout.Label(new GUIContent("Choose the material that will serve as a template for the new materials," +
                " and the texture slot property that will store the texture:"),
                WrappedLabelStyle);
            TemplateMaterial = (Material)EditorGUILayout.ObjectField("Template Material", TemplateMaterial, typeof(Material), false);

            EditorGUI.BeginDisabledGroup(TemplateMaterial == null);
            if (TemplateMaterial != null)
            {
                MaterialTextureSlots = TemplateMaterial.GetPropertyNames(MaterialPropertyType.Texture);
            }
            else
            {
                MaterialTextureSlots = new string[0];
                SelectedTextureSlot = -1;
            }
            SelectedTextureSlot = EditorGUILayout.Popup("Texture Slot", SelectedTextureSlot, MaterialTextureSlots);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(20);

            GUILayout.Label(new GUIContent("Chose the folder containing the textures that will be referenced. " +
                "Each texture will create one matching material:"),
                WrappedLabelStyle);
            GUILayout.Space(10);
            TextureFolderPath = (string)EditorGUILayout.TextField("Textures Folder", TextureFolderPath);

            if (GUILayout.Button("Select Textures Folder ..."))
            {
                DoFolderSelection(ref TextureFolderPath, "Textures");
            }

            GUILayout.Space(20);
            GUILayout.Label(new GUIContent("Chose the folder where the new materials will be created:"),
                WrappedLabelStyle);
            GUILayout.Space(10);

            MaterialFolderPath = (string)EditorGUILayout.TextField("Materials Folder", MaterialFolderPath);

            if (GUILayout.Button("Select New Materials Folder ..."))
            {
                DoFolderSelection(ref MaterialFolderPath, "New Materials");
            }

            GUILayout.Space(15);

            EditorGUI.BeginDisabledGroup(
                TemplateMaterial == null ||
                TextureFolderPath == string.Empty ||
                MaterialFolderPath == string.Empty ||
                SelectedTextureSlot == -1);
            if (GUILayout.Button("Create Materials"))
            {
                PerformMaterialCreation();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Texture-type suffix tags (such as '_A' for Albedo, '_N' for NormalMaps, etc.) will be omitted automatically " +
                "when generating the material names.",
                MessageType.Info);

            GUIHelperFunctions.DrawFooter();

            GUILayout.EndVertical();
        }

        private void PerformMaterialCreation()
        {
            if (!AssetDatabase.IsValidFolder(TextureFolderPath))
            {
                EditorUtility.DisplayDialog("Generate Materials from Textures", "Invalid texture folder selected, please check that the folder still exists.", "OK");
            }
            if (!AssetDatabase.IsValidFolder(MaterialFolderPath))
            {
                EditorUtility.DisplayDialog("Generate Materials from Textures", "Invalid material folder selected, please check that the folder still exists.", "OK");
            }
            string[] files = Directory.GetFiles(Application.dataPath + TextureFolderPath.Substring("Assets".Length));
            Textures = new List<Texture2D>();
            Materials = new List<Material>();

            for (int i = 0; i < files.Length; i++)
            {
                var textureProgress = (i / files.Length) / 2f;
                EditorUtility.DisplayProgressBar("Generate Materials from Textures", "Reading Textures", textureProgress);
                if (files[i].EndsWith(".meta"))
                {
                    continue;
                }
                var filePath = "Assets" + files[i].Substring(Application.dataPath.Length);
                if (AssetDatabase.GetMainAssetTypeAtPath(filePath) == typeof(Texture2D))
                {
                    Textures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(filePath));
                }
            }

            if (Textures.Count > 0)
            {
                var templateAsset = AssetDatabase.GetAssetPath(TemplateMaterial);

                for (int i = 0; i < Textures.Count; i++)
                {
                    var materialProgress = ((i / Textures.Count) / 2f) + 0.5f;
                    EditorUtility.DisplayProgressBar("Generate Materials from Textures", "Creating Materials", materialProgress);
                    var newMaterialPath = AssetDatabase.GenerateUniqueAssetPath(MaterialFolderPath + "/" + Textures[i].name + ".mat");
                    var copyResult = AssetDatabase.CopyAsset(templateAsset, newMaterialPath);
                    if (!copyResult)
                    {
                        EditorUtility.DisplayDialog("Generate Materials from Textures", "There was an error copying the template material. The operation will stop here.", "OK");
                        break;
                    }
                    var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(newMaterialPath);
                    newMaterial.SetTexture(MaterialTextureSlots[SelectedTextureSlot], Textures[i]);
                    Materials.Add(newMaterial);
                }
                EditorUtility.DisplayProgressBar("Generate Materials from Textures", "Finishing Up", 1f);
                AssetDatabase.SaveAssets();
                var message = Materials.Count.ToString() + ((Materials.Count == 1) ? " material was" : " materials were") + " created successfully!";
                var navigateTo = EditorUtility.DisplayDialog("Generate Materials from Textures", message, "Show Materials", "OK");
                EditorUtility.ClearProgressBar();
                if (navigateTo)
                {
                    EditorGUIUtility.PingObject(Materials[0]);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Generate Materials from Textures", "Sorry, no valid 2D Textures were found in the selected folder. No materials have been created.", "OK");
            }
        }

        private void DoFolderSelection(ref string FolderPath, string FolderName)
        {
            if (FolderPath == string.Empty)
            {
                FolderPath = EditorUtility.OpenFolderPanel($"Select {FolderName} Folder", "Assets", "");
            }
            else
            {
                FolderPath = EditorUtility.OpenFolderPanel($"Select {FolderName} Folder", FolderPath, "");
            }
            if (FolderPath.StartsWith(Application.dataPath))
            {
                FolderPath = "Assets" + FolderPath.Substring(Application.dataPath.Length);
            }
        }

        private void DrawHeaderGraphic()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(TextureIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.Label(new GUIContent(TextureIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.Label(new GUIContent(TextureIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(FoldOutIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.Label(new GUIContent(FoldOutIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(MaterialIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.Label(new GUIContent(MaterialIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.Label(new GUIContent(MaterialIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
        }
    }
}
#endif