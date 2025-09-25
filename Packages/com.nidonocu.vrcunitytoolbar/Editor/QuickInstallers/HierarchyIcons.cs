#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityToolbarExtender.Nidonocu.QuickInstallers
{
    [ExecuteInEditMode]
    public class HierarchyIcons : ScriptableSingleton<HierarchyIcons>
    {
        const string HierarchyIconsPackageName = "com.opentoolkit.hierarchyicons";
        const string PackageGitHubURL = "https://github.com/OpenUnityToolkit/HierarchyIcons";
        const string PackageGitRepoURL = "https://github.com/OpenUnityToolkit/HierarchyIcons.git";

        private AddRequest AddRequest;

        [MenuItem("Tools/Nidonocu/Quick Installers/Hierarchy Icons", false, 1000)]
        public static void RequestInstall()
        {
            if (CheckInstalledPackage.IsPackageInstalled(HierarchyIconsPackageName))
            {
                var installedResult = EditorUtility.DisplayDialog(
                    "Quick Install", 
                    "The Hierarchy Icons package is already installed in this project!\n" + 
                    "You can manage or remove this package using the Package Manager.",
                    "View Package Manager",
                    "OK");
                if (installedResult)
                {
                    UnityEditor.PackageManager.UI.Window.Open(HierarchyIconsPackageName);
                }
                return;
            }
            var result = EditorUtility.DisplayDialogComplex(
                "Quick Install", 
                "Would you like to install the Hierarchy Icons Unity package?", 
                "Install Package", 
                "Cancel", 
                "Visit Package GitHub Page");

            if (result == 1)
            {
                return;
            }
            if (result == 2)
            {
                Application.OpenURL(PackageGitHubURL);
                return;
            }

            EditorUtility.DisplayProgressBar("Installing Package", "Adding Hierarchy Icons to the Project", 0.5f);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.BeginInstall();
        }

        public void BeginInstall()
        {
            EditorApplication.update += Update;
            AddRequest = Client.Add(PackageGitRepoURL);
        }

        private void Update()
        {
            if (AddRequest != null && AddRequest.IsCompleted)
            {
                if (AddRequest.Status == StatusCode.Success)
                {
                    var package = AddRequest.Result;
                    EditorUtility.DisplayProgressBar("Installing Package", "Install Complete!", 1f);
                    Debug.Log($"VRC Unity Toolbar Quick Installed the package: {package.name}" +
                        $"\nVersion: {package.version}" +
                        $"\nDisplay name: {package.displayName}");
                    EditorUtility.DisplayDialog(
                    "Quick Install",
                    "The Hierarchy Icons package was installed successfully!\n" +
                    $"The version installed was: {package.version}" ,
                    "OK");
                    EditorUtility.ClearProgressBar();
                }
                else
                {
                    EditorUtility.ClearProgressBar();
                    Debug.LogError($"VRC Unity Toolbar failed to install '{HierarchyIconsPackageName}'\n" +
                        $"Error Code: {AddRequest.Error.errorCode}\n" +
                        $"Error Message: {AddRequest.Error.message}");
                    var errorResult = EditorUtility.DisplayDialog(
                    "Quick Install",
                    "Sorry, there was a problem installing the package.\n" +
                    $"The following error message was reported: {AddRequest.Error.message}\n" +
                    "Please check you can access GitHub and the repository is accessible.",
                    "View Github",
                    "Close");
                    if (errorResult)
                    {
                        Application.OpenURL(PackageGitHubURL);
                    }
                }
                EditorApplication.update -= Update;
                DestroyImmediate(this);
            }
        }
    }
}
#endif
