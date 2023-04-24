using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace UnityToolbarExtender.Nidonocu
{
    public class SmartDuplicate : ScriptableObject
    {
        public static void PerformDuplication(VRExtensionButtonsSettings settings)
        {
            if (Selection.activeObject != null)
            {
                //GameObject newObject = null;
                foreach (var selectedItem in Selection.objects)
                {
                    if (selectedItem is GameObject gameObject && !PrefabUtility.IsPartOfPrefabAsset(selectedItem))
                    {
                        if (PrefabUtility.IsAnyPrefabInstanceRoot(gameObject) &&
                            PrefabUtility.GetPrefabInstanceStatus(gameObject) == PrefabInstanceStatus.Connected)
                        {
                            var source = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                            var duplicatedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(source);
                            // Set Parent
                            duplicatedPrefab.transform.SetParent(gameObject.transform.parent);
                            // Set Name
                            var newName = CreateDuplicateName(gameObject.name, settings);
                            GameObject targetSibling;
                            newName = CheckForNameClashes(newName, gameObject, settings, out targetSibling);
                            duplicatedPrefab.name = newName;
                            // Set Location within Hierarchy
                            duplicatedPrefab.transform.SetSiblingIndex(targetSibling.transform.GetSiblingIndex() + 1);
                            //newObject = duplicatedPrefab;
                            Undo.RegisterCreatedObjectUndo(duplicatedPrefab, "Duplicate Prefab");
                        }
                        else
                        {
                            var duplicateObject = Instantiate(gameObject, gameObject.transform.position, gameObject.transform.rotation);
                            // Set Parent
                            duplicateObject.transform.SetParent(gameObject.transform.parent);
                            // Set Name
                            var newName = CreateDuplicateName(gameObject.name, settings);
                            GameObject targetSibling;
                            newName = CheckForNameClashes(newName, gameObject, settings, out targetSibling);
                            duplicateObject.name = newName;
                            // Set Location within Hierarchy
                            duplicateObject.transform.SetSiblingIndex(targetSibling.transform.GetSiblingIndex() + 1);
                            //newObject = duplicateObject;
                            Undo.RegisterCreatedObjectUndo(duplicateObject, "Duplicate Object");
                        }
                    }
                    else
                    {
                        string assetPath = AssetDatabase.GetAssetPath(selectedItem);
                        string assetName = Path.GetFileNameWithoutExtension(assetPath);

                        var newName = CreateDuplicateName(assetName, settings, true);

                        string newPath = Path.Combine(Path.GetDirectoryName(assetPath), newName + Path.GetExtension(assetPath));

                        while (File.Exists(newPath))
                        {
                            newName = CreateDuplicateName(newName, settings, true);
                            newPath = Path.Combine(Path.GetDirectoryName(assetPath), newName + Path.GetExtension(assetPath));
                        }

                        AssetDatabase.CopyAsset(assetPath, newPath);
                    }
                }
                /*if (Selection.objects.Length == 1)
                {
                    if (Event.current.shift)
                    {
                        // Rename the Original
                    }
                    else
                    {
                        if (settings.smartDuplicationPromptToRename == SmartDuplicationPromptToRename.OnlyGameObjects ||
                            settings.smartDuplicationPromptToRename == SmartDuplicationPromptToRename.Everything)
                        {
                            Debug.Log("Ping");
                            Selection.activeObject = newObject;
                            EditorGUIUtility.PingObject(newObject);
                            EditorApplication.ExecuteMenuItem("Edit/Rename");
                        }
                    }
                }*/
            }
        }

        public static string CreateDuplicateName(string originalName, VRExtensionButtonsSettings settings, bool isFileName = false)
        {
            var result = Regex.Match(originalName, @"\d+");
            if (result.Success)
            {
                // There is a number in the existing string, we'll just replace it with a new number in our prefered numeric format
                var intValue = int.Parse(result.Value) + 1;

                switch (settings.smartDuplicationNumberFormat)
                {
                    case SmartDuplicationNumberFormat.DoubleDigit:
                        return Regex.Replace(originalName, @"\d+", intValue.ToString("D2"));
                    case SmartDuplicationNumberFormat.TripleDigit:
                        return Regex.Replace(originalName, @"\d+", intValue.ToString("D3"));
                    default:
                        return Regex.Replace(originalName, @"\d+", intValue.ToString());
                }
            }
            else
            {
                var newName = originalName;
                switch (settings.smartDuplicationSeparator)
                {
                    case SmartDuplicationSeparator.None:
                        break;
                    case SmartDuplicationSeparator.Space:
                        newName = newName + " ";
                        break;
                    case SmartDuplicationSeparator.Pipe:
                        if (isFileName)
                            newName = newName + "_";
                        else
                            newName = newName + "|";
                        break;
                    case SmartDuplicationSeparator.Dash:
                        newName = newName + "-";
                        break;
                    case SmartDuplicationSeparator.Underscore:
                        newName = newName + "_";
                        break;
                    case SmartDuplicationSeparator.SpacedDash:
                        newName = newName + " - ";
                        break;
                    case SmartDuplicationSeparator.SpacedPipe:
                        if (isFileName)
                            newName = newName + " - ";
                        else
                            newName = newName + " | ";
                        break;
                }

                switch (settings.smartDuplicationBrackets)
                {
                    case SmartDuplicationBrackets.None:
                        break;
                    case SmartDuplicationBrackets.Rounded:
                        newName = newName + "(";
                        break;
                    case SmartDuplicationBrackets.Square:
                        newName = newName + "[";
                        break;
                    case SmartDuplicationBrackets.Curly:
                        newName = newName + "{";
                        break;
                    case SmartDuplicationBrackets.Angular:
                        if (isFileName)
                            newName = newName + "[";
                        else
                            newName = newName + "<";
                        break;
                }

                var initialNumber = (settings.smartDuplicationCountsAtZero) ? 1 : 2;

                switch (settings.smartDuplicationNumberFormat)
                {
                    case SmartDuplicationNumberFormat.SingleDigit:
                        newName = newName + initialNumber.ToString();
                        break;
                    case SmartDuplicationNumberFormat.DoubleDigit:
                        newName = newName + initialNumber.ToString("D2");
                        break;
                    case SmartDuplicationNumberFormat.TripleDigit:
                        newName = newName + initialNumber.ToString("D3");
                        break;
                    default:
                        break;
                }

                switch (settings.smartDuplicationBrackets)
                {
                    case SmartDuplicationBrackets.None:
                        break;
                    case SmartDuplicationBrackets.Rounded:
                        newName = newName + ")";
                        break;
                    case SmartDuplicationBrackets.Square:
                        newName = newName + "]";
                        break;
                    case SmartDuplicationBrackets.Curly:
                        newName = newName + "}";
                        break;
                    case SmartDuplicationBrackets.Angular:
                        if (isFileName)
                            newName = newName + "]";
                        else
                            newName = newName + ">";
                        break;
                }

                return newName;
            }
        }

        private static string CheckForNameClashes(string newName, GameObject originalObject, VRExtensionButtonsSettings settings, out GameObject previousSibling)
        {
            if (originalObject.transform.parent == null)
            {
                previousSibling = originalObject;
                var checkClashObject = GameObject.Find(newName);
                if (checkClashObject != null)
                {
                    while (checkClashObject != null && checkClashObject.transform.parent == null)
                    {
                        previousSibling = checkClashObject;
                        newName = CreateDuplicateName(newName, settings);
                        checkClashObject = GameObject.Find(newName);
                    }
                }

                return newName;
            }
            else
            {
                previousSibling = originalObject;
                var checkClashObject = originalObject.transform.parent.Find(newName);
                if (checkClashObject != null)
                {
                    while (checkClashObject != null && checkClashObject.parent == originalObject.transform.parent)
                    {
                        previousSibling = checkClashObject.gameObject;
                        newName = CreateDuplicateName(newName, settings);
                        checkClashObject = originalObject.transform.parent.Find(newName);
                    }
                }

                return newName;
            }
        }
    }
}
#endif