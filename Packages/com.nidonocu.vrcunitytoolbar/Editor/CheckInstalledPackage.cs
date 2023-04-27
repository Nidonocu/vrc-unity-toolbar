#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Linq;


namespace UnityToolbarExtender.Nidonocu
{
    public static class CheckInstalledPackage
    {
        public const string GestureManagerPackageName = "vrchat.blackstartx.gesture-manager";

        static ListRequest Request;
        public static bool IsPackageInstalled(string packageName)
        {
            Request = Client.List();
            while (!Request.IsCompleted) ;
            return Request.Result.Any(p => p.name == packageName);
        }
    }
}
#endif
