#if UNITY_EDITOR
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor;

namespace UnityToolbarExtender.Nidonocu.QuickInstallers
{
    [InitializeOnLoad]
    public static class MochieDownload
    {
        public const string ShaderName = "Mochie/Standard";
        const string MochieReleasesAtomUrl = "https://github.com/MochiesCode/Mochies-Unity-Shaders/releases.atom";
        const string MochieReleasesApi = "https://api.github.com/repos/MochiesCode/Mochies-Unity-Shaders/releases";
        const string MochieLatestUrl = "https://github.com/MochiesCode/Mochies-Unity-Shaders/releases/latest";

        public static VRExtensionButtonsSettings settings;

        public static SerializedObject settingsObject;

        private static bool isLoading = false;

        public static bool GetIsLoading
        {
            get { return isLoading; }
        }

        private static bool isImportInProgress = false;

        public static bool GetIsImportInProgress
        {
            get { return isImportInProgress; }
        }

        private static string nextUpdateValue = string.Empty;

        private static string nextVersionNumber = string.Empty;

        private static string packageDownloadURL = string.Empty;

        private static string tempFilePath = string.Empty;

        private static bool downloadComplete = false;

        private static int progressID = -1;

        private static HttpClient client;

        private static CancellationTokenSource cancelTokenSource;

        static MochieDownload()
        {
            EditorApplication.update += WaitForEditorReady;
            EditorApplication.quitting += EditorApplication_quitting;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
        }

        private static void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredPlayMode)
            {
                if (isLoading)
                {
                    EditorUtility.DisplayDialog(
                                        "VRC Unity Toolbar - Mochie Update",
                                        "You have started testing (playing) your project, your download and install of the Mochie shader must be aborted.\n" +
                                        "Sorry about that!\n" +
                                        "You can resume after you finish building by going to:\n" +
                                        "Tools > Nidonocu > Quick Installers > Mochie Shader",
                                        "OK");
                    CancelDownload();
                }
                else
                {
                    CleanupTempFiles();
                }
            }
        }

        private static void EditorApplication_quitting()
        {
            CleanupTempFiles();
        }

        private static void WaitForEditorReady()
        {
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                EditorApplication.update -= WaitForEditorReady;
                VRCSdkControlPanel.OnSdkPanelEnable += VRCSdkControlPanel_OnSdkPanelEnable;
                Initialize();
            }
        }

        private static void VRCSdkControlPanel_OnSdkPanelEnable(object sender, EventArgs e)
        {
            if (VRCSdkControlPanel.TryGetBuilder<IVRCSdkBuilderApi>(out var builder))
            {
                builder.OnSdkBuildStart += Builder_OnSdkBuildStart;
            }
        }

        private static void Builder_OnSdkBuildStart(object sender, object e)
        {
            if (isLoading)
            {
                EditorUtility.DisplayDialog(
                                    "VRC Unity Toolbar - Mochie Update",
                                    "You have started building your project, your download and install of the Mochie shader must be aborted.\n" +
                                    "Sorry about that!\n" +
                                    "You can resume after you finish building by going to:\n" +
                                    "Tools > Nidonocu > Quick Installers > Mochie Shader",
                                    "OK");
                CancelDownload();
            } 
            else
            {
                CleanupTempFiles();
            }
        }

        private static void Initialize()
        {
            settings = VRExtensionButtons.settings;
            settingsObject = VRExtensionButtons.settingsObject;
            settingsObject.Update();

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
                    "The new version of the VRC Unity Toolbar that is installed in this project supports automatic updating of the Mochie shader.\n\n" +
                    "The Mochie Shader is installed in this project, but we don't know what version it is because you installed it manually rather than via the toolbar!\n\n" +
                    "So we can track and compare your current version to the latest release, can fetch and try to update the shader now?\n\n" +
                    "We can ask you again next time if it's not convenient right now, or never check for updates again in this project.",
                    "Update Mochie Shader",
                    "Ask me next time",
                    "Don't check for updates again");

                    if (updateRequest == 0)
                    {
                        _ = CheckForUpdate(MochieReleasesAtomUrl, MochieReleasesApi);
                        return;
                    }
                    else if (updateRequest == 1)
                    {
                        return;
                    }
                    else
                    {
                        settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.NoMochieAutoUpdate)).boolValue = true;
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
            // Don't run in play mode
            if (EditorApplication.isPlaying)
            {
                return;
            }

            var currentValue = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.LastMochieFeedUpdate));
            var currentVersion = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.InstalledMochieVersion));
            var lastCheckTime = settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.LastMochieUpdateCheckTime));
            
            // Skip background check if already checked today
            if (isInBackground && lastCheckTime.stringValue != string.Empty)
            {
                var lastCheckTimeValue = DateTime.Parse(lastCheckTime.stringValue);
                var today = DateTime.Today;

                if (lastCheckTimeValue.Date == today.Date)
                {
                    return;
                }
            }

            isLoading = true;

            try
            {
                client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "VRCUnityToolbar");

                var feedReponse = await client.GetAsync(feedUrl);
                var feedContent = await feedReponse.Content.ReadAsStringAsync();
                nextUpdateValue = ParseAtomAndGetLastUpdateDate(feedContent);
                if (nextUpdateValue == string.Empty)
                {
                    throw new Exception("Error while parsing feed");
                }
                if (nextUpdateValue != currentValue.stringValue)
                {
                    // Get new version value and target download URL
                    var apiResponse = await client.GetAsync(apiUrl);
                    var apiContent = await apiResponse.Content.ReadAsStringAsync();
                    ParseApiAndLoadValues(apiContent);
                    if (nextVersionNumber == string.Empty || packageDownloadURL == string.Empty)
                    {
                        throw new Exception("Unable to locate new package version number or download path");
                    }
                    // Check values
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
                            await DoDownloadAndInstall();
                        }
                    }
                    else
                    {
                        Debug.Log($"[VRC Unity Toolbar] - [Mochie Updater] - Mochie Shader is up to date - Current version: {currentVersion.stringValue}");
                        if (!isInBackground)
                        {
                            EditorUtility.DisplayDialog(
                                "VRC Unity Toolbar - Mochie Update",
                                "You have the most recent version of the Mochie Shader already!\n" +
                                $"The current version of the Mochie Shader is: {currentVersion.stringValue}",
                                "OK");
                        }
                    }
                } 
                else
                {

                    Debug.Log($"[VRC Unity Toolbar] - [Mochie Updater] - Mochie Shader is up to date - Current version: {currentVersion.stringValue}");
                    if (!isInBackground)
                    {
                        EditorUtility.DisplayDialog(
                            "VRC Unity Toolbar - Mochie Update",
                            "You have the most recent version of the Mochie Shader already!\n" +
                            $"The current version of the Mochie Shader is: {currentVersion.stringValue}",
                            "OK");
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
                settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.LastMochieUpdateCheckTime)).stringValue = DateTime.Now.ToString();
                settingsObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
        }

        private static async Task DoDownloadAndInstall()
        {
            isLoading = true;
            downloadComplete = false;
            var tempFolder = FileUtil.GetUniqueTempPathInProject();
            try
            {
                Directory.CreateDirectory(tempFolder);
                var uri = new Uri(packageDownloadURL);
                string fileName = Path.GetFileName(uri.AbsolutePath);
                tempFilePath = Path.Combine(tempFolder, fileName);
                if (Progress.Exists(progressID))
                {
                    Progress.Remove(progressID);
                }
                cancelTokenSource = new CancellationTokenSource();
                progressID = Progress.Start("Connecting", "Connecting to Github", Progress.Options.Indefinite);
                Progress.RegisterCancelCallback(progressID, CancelDownload);

                client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "VRCUnityToolbar");

                using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancelTokenSource.Token);

                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1;

                if (canReportProgress)
                {
                    Progress.UnregisterCancelCallback(progressID);
                    Progress.Remove(progressID);
                    progressID = Progress.Start("Downloading", "Downloading the Mochie Package from Github");
                    Progress.RegisterCancelCallback(progressID, CancelDownload);
                    Progress.SetTimeDisplayMode(progressID, Progress.TimeDisplayMode.ShowRemainingTime);
                }
                else
                {
                    Progress.SetTimeDisplayMode(progressID, Progress.TimeDisplayMode.ShowRunningTime);
                }

                var contentStream = await response.Content.ReadAsStreamAsync();
                using (contentStream)
                using (var fileStream = new FileStream(
                    tempFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 32768,
                    useAsync: true)
                    )
                {
                    var buffer = new byte[32768];
                    long totalRead = 0;
                    int bytesRead = 0;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancelTokenSource.Token)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancelTokenSource.Token);
                        totalRead += bytesRead;
                        if (canReportProgress)
                        {
                            ReportProgress(totalRead, totalBytes);
                        }
                    }
                }
                downloadComplete = true;
                Progress.Report(progressID, 1f);
                Progress.UnregisterCancelCallback(progressID);
                Progress.Finish(progressID, Progress.Status.Succeeded);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[VRC Unity Toolbar] - [Mochie Updater] - Download cancelled");
                Progress.Finish(progressID, Progress.Status.Canceled);
            }
            catch (Exception fault)
            {
                Debug.LogError("[VRC Unity Toolbar] - [Mochie Updater] - Error while downloading package: " + fault.Message);
                if (Progress.Exists(progressID))
                {
                    Progress.UnregisterCancelCallback(progressID);
                    Progress.Finish(progressID, Progress.Status.Failed);
                }
                EditorUtility.DisplayDialog(
                    "VRC Unity Toolbar - Mochie Update",
                    "There was a problem downloading package file, please check there wasn't an issue with your internet connection.\n" +
                    "Details of what went wrong have been written in the Console\n" +
                    "Try again by going to:\n" +
                    "Tools > Nidonocu > Quick Installers > Mochie Shader",
                    "OK");
            }
            finally
            {
                isLoading = false;
                Debug.Log("[VRC Unity Toolbar] - [Mochie Updater] - Download complete");
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
                    CleanupTempFiles();
                }
            }
        }

        private static void DoImportPackage()
        {
            if (Progress.Exists(progressID))
            {
                Progress.Remove(progressID);
            }
            if (!File.Exists(tempFilePath))
            {
                Debug.LogError($"[VRC Unity Toolbar] - [Mochie Updater] - Package could not be found at: {tempFilePath}");
                downloadComplete = false;
                EditorUtility.DisplayDialog(
                    "VRC Unity Toolbar - Mochie Update",
                    "There was a problem accessing the downloaded package file, it might have been deleted.\n" +
                    "Try again by going to:\n" +
                    "Tools > Nidonocu > Quick Installers > Mochie Shader",
                    "OK");
                return;
            }
            try
            {
                // Save Last Checks
                AssetDatabase.importPackageCancelled += AssetDatabase_importPackageCancelled;
                AssetDatabase.importPackageFailed += AssetDatabase_importPackageFailed;
                AssetDatabase.importPackageCompleted += AssetDatabase_importPackageCompleted;
                EditorApplication.update += CheckForImportWindow;
                isImportInProgress = true;
                EditorApplication.LockReloadAssemblies();
                AssetDatabase.ImportPackage(tempFilePath, true);
            }
            catch (Exception fault)
            {
                Debug.LogError($"[VRC Unity Toolbar] - [Mochie Updater] - There was a problem importing the package: {fault.Message}");
                EditorUtility.DisplayDialog(
                    "VRC Unity Toolbar - Mochie Update",
                    "There was a problem importing the package file, details have been written in the Console.\n" +
                    "Try again by going to:\n" +
                    "Tools > Nidonocu > Quick Installers > Mochie Shader",
                    "OK");
                return;
            }
        }

        // Handles a 'no-op' import where there were no changes to save, we need to still
        // wrap up and save out the new state
        private static bool importWindowOpened = false;

        private static void CheckForImportWindow()
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var foundWindow = false;
            foreach (var window in windows)
            {
                if (window.GetType().ToString() == "UnityEditor.PackageImport")
                {
                    foundWindow = true;
                    break;
                }
            }
            if (foundWindow)
            {
                importWindowOpened = true;
            }
            else if (!foundWindow && importWindowOpened)
            {
                if (isImportInProgress)
                {
                    CompleteImporting();
                }
            }
        }

        private static void AssetDatabase_importPackageCompleted(string packageName)
        {
            Debug.Log($"[VRC Unity Toolbar] - [Mochie Updater] - New Mochie files were installed!");
            CompleteImporting();            
        }

        public static void CompleteImporting()
        {
            isImportInProgress = false;
            CleanupEventHooks();
            settingsObject.Update();
            settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.LastMochieFeedUpdate)).stringValue = nextUpdateValue;
            settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.InstalledMochieVersion)).stringValue = nextVersionNumber;
            settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.LastMochieUpdateCheckTime)).stringValue = DateTime.Now.ToString();
            settingsObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            Debug.Log($"[VRC Unity Toolbar] - [Mochie Updater] - Mochie is recorded as on version: {nextVersionNumber}");
            CleanupTempFiles();
            EditorApplication.UnlockReloadAssemblies();
        }

        private static void AssetDatabase_importPackageFailed(string packageName, string errorMessage)
        {
            isImportInProgress = false;
            CleanupEventHooks();
            Debug.LogError($"[VRC Unity Toolbar] - [Mochie Updater] - Error during import of package: " + errorMessage);
            CleanupTempFiles();
            EditorApplication.UnlockReloadAssemblies();
        }

        private static void AssetDatabase_importPackageCancelled(string packageName)
        {
            isImportInProgress = false;
            CleanupEventHooks();
            Debug.LogWarning($"[VRC Unity Toolbar] - [Mochie Updater] - User cancelled import");
            //CleanupTempFiles();
            EditorApplication.UnlockReloadAssemblies();
        }

        private static void CleanupEventHooks()
        {
            AssetDatabase.importPackageCancelled -= AssetDatabase_importPackageCancelled;
            AssetDatabase.importPackageFailed -= AssetDatabase_importPackageFailed;
            AssetDatabase.importPackageCompleted -= AssetDatabase_importPackageCompleted;
            EditorApplication.update -= CheckForImportWindow;
        }

        public static void CleanupTempFiles()
        {
            downloadComplete = false;
            if (tempFilePath != string.Empty)
            {
                var folderPath = Path.GetDirectoryName(tempFilePath);
                if (Directory.Exists(folderPath))
                {
                    try
                    {
                        FileUtil.DeleteFileOrDirectory(folderPath);
                        tempFilePath = string.Empty;
                        Debug.Log("[VRC Unity Toolbar] - [Mochie Updater] - Cleaned up Temporary Files");
                    }
                    catch (Exception cleanUpFault)
                    {
                        Debug.LogError("[VRC Unity Toolbar] - [Mochie Updater] - Error cleaning up temporary download files: " + cleanUpFault.Message);
                    }
                }
            }
        }

        private static void ReportProgress(long totalRead, long totalBytes)
        {
            if (!Progress.Exists(progressID))
                return;
            float percent = totalRead / (float)totalBytes;
            Progress.Report(progressID, percent);
        }

        public static bool CancelDownload()
        {
            cancelTokenSource.Cancel();
            if (Progress.Exists(progressID))
            {
                Progress.UnregisterCancelCallback(progressID);
            }
            var folderPath = Path.GetDirectoryName(tempFilePath);
            FileUtil.DeleteFileOrDirectory(folderPath);
            return true;
        }

        private static string ParseAtomAndGetLastUpdateDate(string xmlContent)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);

                var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
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
                // Reset to blank if no shader found
                settingsObject.Update();
                settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.LastMochieFeedUpdate)).stringValue = "";
                settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.InstalledMochieVersion)).stringValue = "";
                settingsObject.FindProperty(nameof(VRExtensionButtonsSettings.LastMochieUpdateCheckTime)).stringValue = "";
                settingsObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);

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