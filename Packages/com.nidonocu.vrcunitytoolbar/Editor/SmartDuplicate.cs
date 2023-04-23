using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace UnityToolbarExtender.Nidonocu
{
    public class SmartDuplicate : ScriptableObject
    {
        public static void PerformDuplication()
        {
            if (Selection.activeObject != null)
            {
                foreach (var selectedItem in Selection.objects)
                {
                    Debug.Log(selectedItem.name);
                    if (selectedItem is GameObject gameObject)
                    {
                        if (PrefabUtility.IsAnyPrefabInstanceRoot(gameObject) && 
                            PrefabUtility.GetPrefabInstanceStatus(gameObject) == PrefabInstanceStatus.Connected)
                        {
                            var source = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                            var duplicatedPrefab = PrefabUtility.InstantiatePrefab(source);
                            // Set Parent
                            // Set Location within Hierarchy
                            // Set Name
                            // Rename existing?
                            
                        }
                        else
                        {
                            var duplicateObject = Instantiate(gameObject, gameObject.transform.position, gameObject.transform.rotation);
                            // Set Parent
                            // Set Location within Hierarchy
                            // Set Name
                            // Rename existing?
                        }
                        // Rename mode?
                    }
                    else
                    {
                        string path = AssetDatabase.GetAssetPath(selectedItem);
                        //AssetDatabase.CopyAsset(path, path.Insert(path.LastIndexOf('.'), " copy"));
                        // Work out name
                        // Copy file
                        // Rename mode?
                    }
                }
            }
        }
    }
}
#endif