#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityToolbarExtender.Nidonocu.BulkActionTools
{
    public class UpdatePropertyInMaterials : EditorWindow
    {
        private Texture MaterialIcon;
        private Texture SettingsIcon;
        private Texture InfoIcon;

        private Shader Shader;

        private string[] ShaderAttributes = new string[0];

        bool useFilter = false;

        
        private int AttributeIndexToFilterBy = -1;

        private Color filterColor = Color.white;

        private Vector4 filterVector = Vector4.zero;

        private int filterIntValue = 0;

        private float filterFloatValue = 0f;

        private float filterRangeValue = 0f;

        private Texture filterTextureValue = null;



        private int AttributeIndexToChange = -1;

        private Color newColorValue = Color.white;

        private Vector4 newVectorValue = Vector4.zero;

        private int newIntValue = 0;

        private float newFloatValue = 0f;

        private float newRangeValue = 0f;

        private Texture newTextureValue = null;

        List<Material> FoundMaterials = new List<Material>();

        bool showMaterialsList = false;

        Vector2 materialScrollPositon = Vector2.zero;

        [MenuItem("Tools/Nidonocu/Bulk Update a Material Property...", false, 302)]
        static void Init()
        {
            UpdatePropertyInMaterials window = (UpdatePropertyInMaterials)EditorWindow.GetWindow(
                typeof(UpdatePropertyInMaterials), true, "Bulk Update a Material Property", true);
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

            if (SettingsIcon == null)
            {
                SettingsIcon = EditorGUIUtility.IconContent(@"d_ToolSettings").image;
            }

            if (InfoIcon == null)
            {
                InfoIcon = EditorGUIUtility.IconContent(@"console.infoicon.sml").image;
            }

            var WrappedLabelStyle = new GUIStyle(EditorStyles.wordWrappedLabel);

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.Label(new GUIContent("Use this tool to change a setting on every material " +
                "that uses the same shader. You can use the value of other properties on the shader to filter the material selection."),
                WrappedLabelStyle);
            DrawHeaderGraphic();
            GUILayout.Space(20);

            GUILayout.Label(new GUIContent("Start by choosing the shader the materials you want to find uses. " +
                "You can then filter the search by the value of any property in the material."),
                WrappedLabelStyle);

            Shader = (Shader)EditorGUILayout.ObjectField("Select Shader", Shader, typeof(Shader), false);

            GUILayout.Space(5);

            useFilter = EditorGUILayout.Toggle("Filter By Property Value", useFilter);

            if (useFilter)
            {
                RenderFilterUI(WrappedLabelStyle);
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Find All Materials"))
            {
                PerformMaterialSearch();
            }

            if (FoundMaterials.Count > 0)
            {
                showMaterialsList = GUIHelperFunctions.RenderResultsList(
                    showMaterialsList,
                    "Found Materials",
                    ref materialScrollPositon,
                    FoundMaterials
                    );
            }

            if (FoundMaterials.Count > 0)
            {
                RenderNewValueUI(WrappedLabelStyle);

                GUILayout.Space(10);

                EditorGUI.BeginDisabledGroup(Shader == null || AttributeIndexToChange == -1);
                if (GUILayout.Button("Update Property"))
                {
                    PerformPropertyUpdate();
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                GUILayout.Space(10);
                GUILayout.Label(new GUIContent("Find some Materials First!", InfoIcon), WrappedLabelStyle, GUILayout.MaxHeight(32f));
            }

            GUIHelperFunctions.DrawFooter();

            GUILayout.EndVertical();
        }

        private void PerformPropertyUpdate()
        {
            var confirm = EditorUtility.DisplayDialog("Bulk Update a Material Property", "Are you sure you wish to update all the found materials?", "Update Materials", "Cancel");
            if (confirm)
            {
                var updateProgress = 0f;
                var progressCount = 0;
                foreach (var material in FoundMaterials)
                {
                    updateProgress = (float)(progressCount / FoundMaterials.Count);
                    EditorUtility.DisplayProgressBar("Updating Materials", "Updating material property", updateProgress);
                    switch (Shader.GetPropertyType(AttributeIndexToChange))
                    {
                        case UnityEngine.Rendering.ShaderPropertyType.Color:
                            material.SetColor(Shader.GetPropertyName(AttributeIndexToChange), newColorValue);
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Vector:
                            material.SetVector(Shader.GetPropertyName(AttributeIndexToChange), newVectorValue);
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Float:
                            material.SetFloat(Shader.GetPropertyName(AttributeIndexToChange), newFloatValue);
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Range:
                            material.SetFloat(Shader.GetPropertyName(AttributeIndexToChange), newRangeValue);
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Texture:
                            material.SetTexture(Shader.GetPropertyName(AttributeIndexToChange), newTextureValue);
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Int:
                            material.SetInteger(Shader.GetPropertyName(AttributeIndexToChange), newIntValue);
                            break;
                        default:
                            Debug.LogWarning("Unsupported Type Update");
                            break;
                    }
                    progressCount++;
                }
                EditorUtility.DisplayProgressBar("Updating Materials", "Saving Changes", 1f);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Bulk Update a Material Property", "Materials updated successfully", "OK");
            }
        }

        private void PerformMaterialSearch()
        {
            FoundMaterials.Clear();
            string[] allMaterials = AssetDatabase.FindAssets("t:Material");
            var searchProgress = 0f;
            for (int i = 0; i < allMaterials.Length; i++)
            {
                searchProgress = (float)(i / allMaterials.Length);
                EditorUtility.DisplayProgressBar("Searching", "Searching Materials", searchProgress);
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(allMaterials[i]));
                if (material.shader == Shader)
                {
                    if (useFilter)
                    {
                        var passedFilter = false;
                        switch (Shader.GetPropertyType(AttributeIndexToFilterBy))
                        {
                            case UnityEngine.Rendering.ShaderPropertyType.Color:
                                var colorValue = material.GetColor(Shader.GetPropertyName(AttributeIndexToFilterBy));
                                if (colorValue == filterColor)
                                {
                                    passedFilter = true;
                                }
                                break;
                            case UnityEngine.Rendering.ShaderPropertyType.Vector:
                                var vectorValue = material.GetVector(Shader.GetPropertyName(AttributeIndexToFilterBy));
                                if (vectorValue == filterVector)
                                {
                                    passedFilter = true;
                                }
                                break;
                            case UnityEngine.Rendering.ShaderPropertyType.Float:
                                var floatValue = material.GetFloat(Shader.GetPropertyName(AttributeIndexToFilterBy));
                                if (floatValue == filterFloatValue)
                                {
                                    passedFilter = true;
                                }
                                break;
                            case UnityEngine.Rendering.ShaderPropertyType.Range:
                                var floatRangeValue = material.GetFloat(Shader.GetPropertyName(AttributeIndexToFilterBy));
                                if (floatRangeValue == filterRangeValue)
                                {
                                    passedFilter = true;
                                }
                                break;
                            case UnityEngine.Rendering.ShaderPropertyType.Texture:
                                var textureValue = material.GetTexture(Shader.GetPropertyName(AttributeIndexToFilterBy));
                                if (textureValue == filterTextureValue)
                                {
                                    passedFilter = true;
                                }
                                break;
                            case UnityEngine.Rendering.ShaderPropertyType.Int:
                                var intValue = material.GetInteger(Shader.GetPropertyName(AttributeIndexToFilterBy));
                                if (intValue == filterIntValue)
                                {
                                    passedFilter = true;
                                }
                                break;
                            default:
                                Debug.LogWarning("Unsupported Filter Type");
                                break;
                        }
                        if (!passedFilter)
                        {
                            continue;
                        }
                    }
                    FoundMaterials.Add(material);
                }
            }
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Bulk Update a Material Property", $"Found {FoundMaterials.Count} Material{(FoundMaterials.Count == 1 ? "" : "s")}", "OK");
        }

        private void RenderFilterUI(GUIStyle WrappedLabelStyle)
        {
            EditorGUI.BeginDisabledGroup(Shader == null);
            if (Shader != null)
            {
                ShaderAttributes = new string[Shader.GetPropertyCount()];
                for (int i = 0; i < Shader.GetPropertyCount(); ++i)
                {
                    ShaderAttributes[i] = Shader.GetPropertyName(i);
                }
            }
            else
            {
                ShaderAttributes = new string[0];
                AttributeIndexToFilterBy = -1;
            }
            AttributeIndexToFilterBy = EditorGUILayout.Popup("Filter By", AttributeIndexToFilterBy, ShaderAttributes);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(Shader == null || AttributeIndexToFilterBy == -1);
            if (AttributeIndexToFilterBy > -1 && AttributeIndexToChange < ShaderAttributes.Length)
            {
                switch (Shader.GetPropertyType(AttributeIndexToFilterBy))
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        filterColor = (Color)EditorGUILayout.ColorField("Filter Color Value", filterColor);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        filterVector = (Vector4)EditorGUILayout.Vector4Field("Filter Vector Value", filterVector);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                        filterFloatValue = (float)EditorGUILayout.FloatField("Filter Float Value", filterFloatValue);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                        var rangeLimits = Shader.GetPropertyRangeLimits(AttributeIndexToFilterBy);
                        filterRangeValue = (float)EditorGUILayout.Slider(
                            "Filter Range Value",
                            filterRangeValue,
                            rangeLimits.x,
                            rangeLimits.y);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        filterTextureValue = (Texture)EditorGUILayout.ObjectField("Filter Texture Value", filterTextureValue, typeof(Texture), false);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Int:
                        filterIntValue = (int)EditorGUILayout.IntField("Filter Int Value", filterIntValue);
                        break;
                    default:
                        GUILayout.Space(10);
                        GUILayout.Label(new GUIContent("Select a Property", InfoIcon), WrappedLabelStyle, GUILayout.MaxHeight(32f));
                        break;
                }
            }
            EditorGUI.EndDisabledGroup();
            if (Shader != null && AttributeIndexToFilterBy == -1)
            {
                GUILayout.Space(10);
                GUILayout.Label(new GUIContent("Select a Property", InfoIcon), WrappedLabelStyle, GUILayout.MaxHeight(32f));
            }
            if (Shader == null)
            {
                GUILayout.Space(10);
                GUILayout.Label(new GUIContent("Select a Shader first", InfoIcon), WrappedLabelStyle, GUILayout.MaxHeight(32f));
            }
        }

        private void RenderNewValueUI(GUIStyle WrappedLabelStyle)
        {
            GUILayout.Space(10);
            GUILayout.Label(new GUIContent("Now choose the property you wish to change on all the above materials " +
                            "and the value to set it to:"),
                            WrappedLabelStyle);

            EditorGUI.BeginDisabledGroup(Shader == null);

            if (Shader != null)
            {
                ShaderAttributes = new string[Shader.GetPropertyCount()];
                for (int i = 0; i < Shader.GetPropertyCount(); ++i)
                {
                    ShaderAttributes[i] = Shader.GetPropertyName(i);
                }
            }
            else
            {
                ShaderAttributes = new string[0];
                AttributeIndexToChange = -1;
            }
            AttributeIndexToChange = EditorGUILayout.Popup("Property To Change", AttributeIndexToChange, ShaderAttributes);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(Shader == null || AttributeIndexToChange == -1);
            if (AttributeIndexToChange > -1 && AttributeIndexToChange < ShaderAttributes.Length)
            {
                switch (Shader.GetPropertyType(AttributeIndexToChange))
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        newColorValue = (Color)EditorGUILayout.ColorField("New Color Value", newColorValue);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        newVectorValue = (Vector4)EditorGUILayout.Vector4Field("New Vector Value", newVectorValue);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                        newFloatValue = (float)EditorGUILayout.FloatField("New Float Value", newFloatValue);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                        var rangeLimits = Shader.GetPropertyRangeLimits(AttributeIndexToChange);
                        newRangeValue = (float)EditorGUILayout.Slider(
                            "New Range Value",
                            newRangeValue,
                            rangeLimits.x,
                            rangeLimits.y);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        newTextureValue = (Texture)EditorGUILayout.ObjectField("New Texture Value", newTextureValue, typeof(Texture), false);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Int:
                        newIntValue = (int)EditorGUILayout.IntField("New Int Value", newIntValue);
                        break;
                    default:
                        GUILayout.Space(10);
                        GUILayout.Label(new GUIContent("Select a Property", InfoIcon), WrappedLabelStyle, GUILayout.MaxHeight(32f));
                        break;
                }
            }
            EditorGUI.EndDisabledGroup();

            if (Shader != null && AttributeIndexToChange == -1)
            {
                GUILayout.Space(10);
                GUILayout.Label(new GUIContent("Select a Property", InfoIcon), WrappedLabelStyle, GUILayout.MaxHeight(32f));
            }
        }

        private void DrawHeaderGraphic()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(SettingsIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.Space(30);
            GUILayout.Label(new GUIContent(MaterialIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.Label(new GUIContent(MaterialIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.Label(new GUIContent(MaterialIcon), GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f));
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}
#endif