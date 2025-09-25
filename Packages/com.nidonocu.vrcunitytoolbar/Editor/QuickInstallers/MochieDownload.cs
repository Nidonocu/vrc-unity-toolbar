#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnityToolbarExtender.Nidonocu.QuickInstallers
{
    public class MochieDownload : MonoBehaviour
    {
        const string ShaderName = "Mochie/Standard";
        const string MochieLatestURL = "https://github.com/MochiesCode/Mochies-Unity-Shaders/releases/latest";

        [MenuItem("Tools/Nidonocu/Quick Installers/Mochie Shader", false, 1001)]
        public static void RequestInstall()
        {
            if (CheckInstalledPackage.IsShaderPresent(ShaderName))
            {
                var installedResult = EditorUtility.DisplayDialog(
                    "Quick Install",
                    "The Mochie Shader is already installed in this project!\n" +
                    "You can visit GitHub anyway and check for an update yourself.",
                    "View GitHub",
                    "Close");
                if (installedResult)
                {
                    Application.OpenURL(MochieLatestURL);
                    return;
                }
            }
            else
            {
                var result = EditorUtility.DisplayDialog(
                "Quick Install",
                "Unforchnately, Mochie remains a completely un-automatable install.\n" +
                "This is due to the developer not supporting either the VCC or having a " +
                "Unity package manifest in their repo. This is rather annoying. :(\n" + 
                "You can at least use this button to check their latest release page on GitHub.",
                "Visit Latest Release Page",
                "Cancel");
                if (result)
                {
                    Application.OpenURL(MochieLatestURL);
                    return;
                }
            }
        }
    }
}
#endif