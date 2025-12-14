#if UNITY_EDITOR
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace UnityToolbarExtender.Nidonocu.QuickInstallers
{
    [InitializeOnLoad]
    public static class MochieDownload
    {
        const string ShaderName = "Mochie/Standard";
        const string MochieReleasesAtomUrl = "https://github.com/MochiesCode/Mochies-Unity-Shaders/releases.atom";
        const string MochieReleasesApi = "https://api.github.com/repos/MochiesCode/Mochies-Unity-Shaders/releases";
        const string MochieLatestUrl = "https://github.com/MochiesCode/Mochies-Unity-Shaders/releases/latest";

        public static VRExtensionButtonsSettings settings;

        public static SerializedObject settingsObject;

        private static bool isLoading = false;

        private static string nextUpdateValue = string.Empty;

        private static string nextVersionNumber = string.Empty;

        private static string packageDownloadURL = string.Empty;

        private static string tempFilePath = string.Empty;

        private static bool downloadComplete = false;

        private static int progressID = -1;

        private static WebClient client;

        static MochieDownload()
        {
            EditorApplication.update += WaitForEditorReady;
        }

        private static void WaitForEditorReady()
        {
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                EditorApplication.update -= WaitForEditorReady;
                Initialize();
            }
        }

        private static void Initialize()
        {
            settings = VRExtensionButtonsSettings.GetOrCreateSettings();
            settingsObject = new SerializedObject(settings);

            if (CheckInstalledPackage.IsShaderPresent(ShaderName))
            {
                var dontUpdateCheck = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.NoMochieAutoUpdate));
                if (dontUpdateCheck.boolValue)
                {
                    return;
                }

                var lastFeedTime = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.LastMochieFeedUpdate));
                // If the value is blank, we don't know the current Mochie version and request an update
                if (lastFeedTime.stringValue == string.Empty)
                {
                    // Request we do update with the user
                    var updateRequest = EditorUtility.DisplayDialogComplex(
                    "VRC Unity Toolbar",
                    "The new version of the toolbar you installed supports automatic updating of the Mochie shader.\n" +
                    "The Mochie Shader is installed in this project, but we don't know what version it is because you installed it before this verison of the toolbar!\n" +
                    "So we can track and compare your current version to the latest release, can fetch and try to update the shader now?\n" +
                    "We can ask you again next time if it's not convenient right now, or never check for updates again in this project.",
                    "Update Mochie Shader",
                    "Ask me next time",
                    "Don't check for updates again");

                    if (updateRequest == 0)
                    {
                        _ = CheckForUpdate(MochieReleasesAtomUrl, MochieReleasesApi);
                        return;
                    }
                    else if (updateRequest == 2)
                    {
                        dontUpdateCheck.boolValue = true;
                        settingsObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(settings);
                        return;
                    }
                }
                // We have a valid previous value, do a silent update check in the background
                _ = CheckForUpdate(MochieReleasesAtomUrl, MochieReleasesApi, true);
            }
        }

        private static async Task CheckForUpdate(string feedUrl, string apiUrl, bool isInBackground = false)
        {
            isLoading = true;

            try
            {
                client = new WebClient();
                client.Headers.Add("User-Agent", "VRCUnityToolbar");

                var feedContent = await client.DownloadStringTaskAsync(feedUrl);
                nextUpdateValue = ParseAtomAndGetLastUpdateDate(feedContent);
                if (nextUpdateValue == string.Empty)
                {
                    throw new Exception("Error while parsing feed");
                }
                var currentValue = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.LastMochieFeedUpdate));
                if (nextUpdateValue != currentValue.stringValue)
                {
                    // Get new version value and target download URL
                    Debug.Log("Test");
                    HttpClient hClient = new HttpClient();
                    hClient.DefaultRequestHeaders.Add("User-Agent", "UnityEditorScript");
                    var test = await hClient.GetAsync(apiUrl);
                    var testString = test.Content.ReadAsStringAsync();
                    Debug.Log(testString.Result);
                    return;
                    var apiContent = await client.DownloadStringTaskAsync("https://api.github.com/zen");// apiUrl);
                    Debug.Log("Test2");
                    ParseApiAndLoadValues(apiContent);
                    if (nextVersionNumber == string.Empty || packageDownloadURL == string.Empty)
                    {
                        throw new Exception("Unable to locate new package version number or download path");
                    }
                    // Check values
                    var currentVersion = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.InstalledMochieVersion));
                    if (currentVersion.stringValue == string.Empty || IsSecondVersionNewer(currentVersion.stringValue, nextVersionNumber))
                    {
                        var newVersionCheck = false;

                        if (CheckInstalledPackage.IsShaderPresent(ShaderName))
                        {
                            var currentVersionString = (currentVersion.stringValue == string.Empty) ? "Unknown Version" : currentVersion.stringValue;

                            newVersionCheck = EditorUtility.DisplayDialog(
                                "VRC Unity Toolbar - Mochie Update",
                                "A new version of the Mochie shader is available!\n" +
                                $"Current Version: {currentVersionString}\n" +
                                $"New Version: {nextVersionNumber}\n" +
                                "You can update it right now and get a reminder next time you open your project.\n" +
                                "Would you like to download it now? You can keep using Unity while it does.",
                                "Start Download",
                                "Not now");
                        }
                        else
                        {
                            newVersionCheck = EditorUtility.DisplayDialog(
                                "VRC Unity Toolbar - Mochie Update",
                                $"The current version of the Mochie Shader is: {nextVersionNumber}\n" +
                                "Would you like to download it now? You can keep using Unity while it does.",
                                "Start Download",
                                "Not now");
                        }

                        if (newVersionCheck)
                        {
                            // Start Download
                            DoDownloadAndInstall();
                        }
                    }
                    else
                    {
                        Debug.Log($"[VRC Unity Toolbar] - [Mochie Updater] - Mochie Shader is up to date - Current version: {nextVersionNumber}");
                        if (!isInBackground)
                        {
                            EditorUtility.DisplayDialog(
                                "VRC Unity Toolbar - Mochie Update",
                                "You have the most recent version of the Mochie Shader already!\n" +
                                $"The current version of the Mochie Shader is: {nextVersionNumber}",
                                "OK");
                        }
                    }
                }

            }
            catch (Exception fault)
            {
                Debug.LogError("[VRC Unity Toolbar] - [Mochie Updater] - Error while checking for Mochie Update: " + fault.Message);
                if (!isInBackground)
                {
                    var updateError = EditorUtility.DisplayDialog(
                    "VRC Unity Toolbar - Mochie Update",
                    "Sorry, there was a problem while trying to check for updates. Details of what went wrong have been written in the Console.\n" +
                    "Would you like to try again?",
                    "Try Again",
                    "Cancel");
                    if (updateError)
                    {
                        // Retry
                        _ = CheckForUpdate(feedUrl, apiUrl);
                    }
                }
            }
            finally
            {
                isLoading = false;
            }
        }

        private static void DoDownloadAndInstall()
        {
            isLoading = true;
            var tempFolder = FileUtil.GetUniqueTempPathInProject();
            Directory.CreateDirectory(tempFolder);
            var uri = new Uri(packageDownloadURL);
            string fileName = Path.GetFileName(uri.AbsolutePath);
            tempFilePath = Path.Combine(tempFolder, fileName);
            progressID = Progress.Start("Downloading Mochie Package");
            Progress.RegisterCancelCallback(progressID, CancelDownload);

            client = new WebClient();
            client.DownloadProgressChanged += DownloadProgressChanged;
            client.DownloadFileCompleted += DownloadFileCompleted;
            client.DownloadFileAsync(uri, tempFilePath);
        }

        private static void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Progress.UnregisterCancelCallback(progressID);

            if (e.Cancelled)
            {
                Progress.Finish(progressID, Progress.Status.Canceled);

                return;
            }
            if (e.Error != null)
            {
                Progress.Finish(progressID, Progress.Status.Failed);
            }
            Progress.Report(progressID, 1f);
            Progress.Finish(progressID, Progress.Status.Succeeded);
            downloadComplete = true;
            isLoading = false;
            var readyToInstall = EditorUtility.DisplayDialog(
                    "VRC Unity Toolbar - Mochie Update",
                    "Download of the Mochie package has been completed.\n" +
                    $"Are you ready to import it now?",
                    "Import Now",
                    "Later");
            if (readyToInstall)
            {
                DoImportPackage();
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "VRC Unity Toolbar - Mochie Update",
                    "Okay, the download file will remain availible until you exit Unity.\n" +
                    "When you're ready, finish the install by going to:\n" +
                    "Tools > Nidonocu > Quick Installers > Mochie Shader",
                    "OK");
            }
        }

        private static void DoImportPackage()
        {
            if (!File.Exists(tempFilePath))
            {
                Debug.LogError($"[VRC Unity Toolbar] - [Mochie Updater] - Package could not be found at: {tempFilePath}");
                downloadComplete = false;
                EditorUtility.DisplayDialog(
                    "VRC Unity Toolbar - Mochie Update",
                    "There was a problem accessing the downloaded package file, it might have been deleted.\n" +
                    "Try again by accessing:\n" +
                    "Tools > Nidonocu > Quick Installers > Mochie Shader",
                    "OK");
                return;
            }
            try
            {
                AssetDatabase.ImportPackage(tempFilePath, true);
                // Save Last Checks
                var currentValue = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.LastMochieFeedUpdate));
                var currentVersion = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.InstalledMochieVersion));
                currentValue.stringValue = nextUpdateValue;
                currentVersion.stringValue = nextVersionNumber;
                settingsObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                Debug.Log($"[VRC Unity Toolbar] - [Mochie Updater] - Mochie updated to: {nextVersionNumber}");
            }
            catch (Exception fault)
            {
                Debug.LogError($"[VRC Unity Toolbar] - [Mochie Updater] - There was a problem importing the package: {fault.Message}");
                EditorUtility.DisplayDialog(
                    "VRC Unity Toolbar - Mochie Update",
                    "There was a problem importing the package file, details have been written in the Console.\n" +
                    "Try again by accessing:\n" +
                    "Tools > Nidonocu > Quick Installers > Mochie Shader",
                    "OK");
                return;
            }
            finally
            {
                var folderPath = Path.GetDirectoryName(tempFilePath);
                FileUtil.DeleteFileOrDirectory(folderPath);
                downloadComplete = false;
            }
        }

        private static void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Progress.Report(progressID, (float)e.ProgressPercentage / 5);
        }

        public static bool CancelDownload()
        {
            client.CancelAsync();
            var folderPath = Path.GetDirectoryName(tempFilePath);
            FileUtil.DeleteFileOrDirectory(folderPath);
            return true;
        }

        private static string ParseAtomAndGetLastUpdateDate(string xmlContent)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("atom", "http://www.w3.org/2005/Atom");

                XmlNode updatedNode = xmlDoc.SelectSingleNode("//atom:updated", nsmgr);

                if (updatedNode == null)
                {
                    throw new Exception("Unable to find 'Updated' tag in feed");
                }
                if (string.IsNullOrWhiteSpace(updatedNode.InnerText))
                {
                    throw new Exception("Updated tag was empty");
                }
                return updatedNode.InnerText;
            }
            catch (Exception fault)
            {
                Debug.LogError("[VRC Unity Toolbar] - [Mochie Updater] - Error while reading ATOM feed: " + fault.Message);
                return string.Empty;
            }
        }

        private static void ParseApiAndLoadValues(string jsonContent)
        {
            nextVersionNumber = string.Empty;
            packageDownloadURL = string.Empty;
            try
            {
                var releases = JsonConvert.DeserializeObject<List<GithubRelease>>(jsonContent);

                if (releases.Count == 0)
                {
                    throw new Exception("No releases returned by API");
                }

                nextVersionNumber = releases[0].tag_name;

                foreach (var asset in releases[0].assets)
                {
                    if (asset.name.EndsWith(".unitypackage"))
                    {
                        packageDownloadURL = asset.browser_download_url;
                        break;
                    }
                }
            }
            catch (Exception fault)
            {
                Debug.LogError("[VRC Unity Toolbar] - [Mochie Updater] - Error while reading API: " + fault.Message);
            }
        }

        static bool IsSecondVersionNewer(string v1, string v2)
        {
            // Remove leading 'v' if present
            v1 = v1.TrimStart('v', 'V');
            v2 = v2.TrimStart('v', 'V');

            var ver1 = new Version(v1);
            var ver2 = new Version(v2);

            return (ver1.CompareTo(ver2) < 0);
        }

        [MenuItem("Tools/Nidonocu/Quick Installers/Mochie Shader", false, 1001)]
        public static void RequestInstall()
        {
            if (isLoading)
                return;
            if (CheckInstalledPackage.IsShaderPresent(ShaderName))
            {
                if (downloadComplete)
                {
                    var readyToInstall = EditorUtility.DisplayDialog(
                    "VRC Unity Toolbar - Mochie Update",
                    "Download of the Mochie package has been completed.\n" +
                    $"Are you ready to import it now?",
                    "Import Now",
                    "Later");
                    if (readyToInstall)
                    {
                        DoImportPackage();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            "VRC Unity Toolbar - Mochie Update",
                            "Okay, the download file will remain availible until you exit Unity.\n" +
                            "When you're ready, finish the install by going to:\n" +
                            "Tools > Nidonocu > Quick Installers > Mochie Shader",
                            "OK");
                    }
                }
                else
                {
                    _ = CheckForUpdate(MochieReleasesAtomUrl, MochieReleasesApi);
                }
            }
            else
            {
                var result = EditorUtility.DisplayDialogComplex(
                "Quick Install - Mochie Shader",
                "Would you like to install the Mochie Shader Unity package?\n" +
                "The VRC Unity Toolbar will automatically monitor for new releases and help you upgrade automatically when a new version is released.\n" +
                "You can at least use this button to check their latest release page on GitHub.",
                "Download Package",
                "Cancel",
                "Visit Package GitHub Page");
                if (result == 0)
                {
                    _ = CheckForUpdate(MochieReleasesAtomUrl, MochieReleasesApi);
                }
                if (result == 2)
                {
                    Application.OpenURL(MochieLatestUrl);
                    return;
                }
            }
        }
    }

    public class GithubRelease
    {
        public string tag_name;

        public List<ReleaseAsset> assets;
    }

    public class ReleaseAsset
    {
        public string name;

        public string browser_download_url;
    }
}
#endif