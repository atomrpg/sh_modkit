﻿using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;

class StoreSteamService
{
    bool _init = false;
    public void Init()
    {
        if (!_init)
        {
            _init = SteamAPI.Init();
        }
    }

    public void Logout()
    {
        if (_init)
        {
            SteamAPI.ReleaseCurrentThreadMemory();
            SteamAPI.Shutdown();
            _init = false;
        }
    }


    public void Update()
    {
        if (_init)
        {
            SteamAPI.RunCallbacks();
        }
    }

    public uint GetAppId()
    {
        if (_init)
        {
            return SteamUtils.GetAppID().m_AppId;
        }
        else
        {
            return AppId_t.Invalid.m_AppId;
        }
    }

    public bool IsSign()
    {
        return _init && SteamAPI.IsSteamRunning();
    }
}

public class ModBuilder : EditorWindow
{
    private CallResult<SteamUGCQueryCompleted_t> OnSteamUGCQueryCompletedCallResult;
    private CallResult<CreateItemResult_t> OnCreateItemResultCallResult;
    private CallResult<SubmitItemUpdateResult_t> OnSubmitItemUpdateResultCallResult;

    private List<SteamUGCDetails_t> _modList = new List<SteamUGCDetails_t>();
    private int _modIndex = -1;

    StoreSteamService steam = new StoreSteamService();
    // string _modName = typeof(ModEntryPoint).Assembly.GetName().Name;
    [MenuItem("Game/Build Mod")]
    static public void BuildMod()
    {
        var window = EditorWindow.GetWindow<ModBuilder>();
        window.ShowWindow();
    }

    public void ShowWindow()
    {
        base.Show();
    }

    void RequestInfo()
    {
        steam.Init();
        if (steam.IsSign())
        {
            OnSteamUGCQueryCompletedCallResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnSteamUGCQueryCompleted);
            SteamAPICall_t handle = SteamUGC.SendQueryUGCRequest(SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(), EUserUGCList.k_EUserUGCList_Published, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items, EUserUGCListSortOrder.k_EUserUGCListSortOrder_CreationOrderDesc, AppId_t.Invalid, SteamUtils.GetAppID(), 1));
            OnSteamUGCQueryCompletedCallResult.Set(handle);
            OnSubmitItemUpdateResultCallResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItemUpdateResult);
            OnCreateItemResultCallResult = CallResult<CreateItemResult_t>.Create(OnCreateItemResult);
        }
    }

    void OnSubmitItemUpdateResult(SubmitItemUpdateResult_t pCallback, bool bIOFailure)
    {
        EditorUtility.ClearProgressBar();
        string readableResult = pCallback.m_eResult.ToString();
        string legalAgreementNeeded = pCallback.m_bUserNeedsToAcceptWorkshopLegalAgreement ? "Yes" : "No";
        string fileId = pCallback.m_nPublishedFileId.ToString();

        string msg =
            "Workshop item update completed.\n\n" +
            $"• Result: {readableResult}\n" +
            $"• Requires Legal Agreement: {legalAgreementNeeded}\n" +
            $"• File ID: {fileId}";

        Debug.Log($"[SubmitItemUpdateResult] Result={readableResult}, LegalAgreement={legalAgreementNeeded}, FileID={fileId}");
        EditorUtility.DisplayDialog("Workshop Update", msg, "OK");
    }


    void OnCreateItemResult(CreateItemResult_t pCallback, bool bIOFailure)
    {
        Debug.Log("[" + CreateItemResult_t.k_iCallback + " - CreateItemResult] - " + pCallback.m_eResult + " -- " + pCallback.m_nPublishedFileId + " -- " + pCallback.m_bUserNeedsToAcceptWorkshopLegalAgreement);

        SteamUGCDetails_t details = new SteamUGCDetails_t
        {
            m_nPublishedFileId = pCallback.m_nPublishedFileId
        };
        _modList.Add(details);
        _modIndex = _modList.Count - 1;
    }

    void OnSteamUGCQueryCompleted(SteamUGCQueryCompleted_t pCallback, bool bIOFailure)
    {
        Debug.Log("[" + SteamUGCQueryCompleted_t.k_iCallback + " - SteamUGCQueryCompleted] - " + pCallback.m_handle + " -- " + pCallback.m_eResult + " -- " + pCallback.m_unNumResultsReturned + " -- " + pCallback.m_unTotalMatchingResults + " -- " + pCallback.m_bCachedData);

        for (uint i = 0; i < pCallback.m_unNumResultsReturned; i++)
        {
            bool ret = SteamUGC.GetQueryUGCResult(pCallback.m_handle, i, out SteamUGCDetails_t details);
            _modList.Add(details);
        }
    }

    private void Update()
    {
        steam.Update();
    }

    const string PATH_TO_ASSETS = "/assets";
    const string PATH_BUILD_BUNDLE = "Temp/ModBuild";
    const string PATH_BUILD_DLL = "Temp/ModBuild_dll";

    bool buildAssetBundle = true;
    //bool stripShaders = false;
    bool clearLogs = true;

    BuildTarget buildTarget = BuildTarget.StandaloneWindows64;

    public static void ClearLogConsole()
    {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        clearMethod.Invoke(null, null);
    }

    void CopyBundle(string dataAsset, string modResFolder, string bundle)
    {
        var resPath = dataAsset + "/Temp/ModBuild/" + bundle;
        if (File.Exists(resPath))
        {
            Copy(resPath, modResFolder + "/" + bundle);
            //Copy(resPath + ".manifest", modResFolder + "/" + bundle + ".manifest");
        }
    }

    string _rgchTitle = "";
    string _rgchDescription = "";

    private void OnGUI()
    {
        string modName = typeof(ModEntryPoint).Assembly.GetName().Name;
        GUILayout.Label("Build Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Mod Name", modName);
        if (GUILayout.Button("Change"))
        {
            var obj = AssetDatabase.LoadAssetAtPath<Object>("Assets/Scripts/MyMod.asmdef");
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
        EditorGUILayout.EndHorizontal();

        clearLogs = GUILayout.Toggle(clearLogs, "Clear Logs");
        buildAssetBundle = GUILayout.Toggle(buildAssetBundle, "Build Asset Bundle");
        //stripShaders = GUILayout.Toggle(stripShaders, "Strip Shaders");

        if (GUILayout.Button("BUILD"))
        {
            if (modName.Length > 0)
            {
                //ShaderBuildProcessor.SetEnabled(stripShaders);
                if (Directory.Exists(PATH_BUILD_BUNDLE))
                {
                    Directory.Delete(PATH_BUILD_BUNDLE, true);
                }

                if (Directory.Exists(PATH_BUILD_DLL))
                {
                    Directory.Delete(PATH_BUILD_DLL, true);
                }

                if (Directory.Exists(PATH_BUILD_BUNDLE))
                {
                    throw new System.Exception("Temp/ModBuild exist");
                }

                if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
                RenderSettings.fog = true; // force enable fog

                Directory.CreateDirectory(PATH_BUILD_BUNDLE);

                string[] levelBundleList = null;

                {
                    foreach (var assetBundleName in AssetDatabase.GetAllAssetBundleNames())
                    {
                        AssetDatabase.RemoveAssetBundleName(assetBundleName, true);
                    }
                    AssetDatabase.Refresh();
                }

                {
                    EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

                    foreach (var scene in scenes)
                    {
                        if (!scene.enabled)
                            continue;

                        string path = scene.path;

                        if (string.IsNullOrEmpty(path))
                            continue;

                        AssetImporter importer = AssetImporter.GetAtPath(path);
                        if (importer != null)
                        {
                            importer.SetAssetBundleNameAndVariant(modName + "_scenes", "");
                        }
                    }
                }

                //HACK for unique id
                AssetImporter.GetAtPath("Assets/Resources").SetAssetBundleNameAndVariant(modName + "_resources", "");

                AssetDatabase.Refresh();

                if (buildAssetBundle)
                {
                    AssetBundleBuild[] builds = UnityEditor.Build.Content.ContentBuildInterface.GenerateAssetBundleBuilds();

                    string stripPrefix = "Assets/Resources/";

                    for (int j = 0; j < builds.Length; j++)
                    {
                        builds[j].addressableNames = new string[builds[j].assetNames.Length];
                        for (int i = 0; i < builds[j].assetNames.Length; i++)
                        {
                            string originalPath = builds[j].assetNames[i];
                            if (originalPath.StartsWith(stripPrefix, System.StringComparison.OrdinalIgnoreCase))
                            {
                                builds[j].addressableNames[i] = originalPath.Substring(stripPrefix.Length).ToLower();
                            }
                            else
                            {
                                builds[j].addressableNames[i] = originalPath.ToLower(); // fallback
                            }
                            //Debug.Log(builds[j].addressableNames[i]);
                        }
                    }

                    BuildPipeline.BuildAssetBundles(PATH_BUILD_BUNDLE, builds, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);
                    //BuildPipeline.BuildAssetBundles(PATH_BUILD_BUNDLE, BuildAssetBundleOptions.ChunkBasedCompression/*BuildAssetBundleOptions.DisableWriteTypeTree*/, buildTarget);
                }

                if (clearLogs)
                {
                    ClearLogConsole();
                }

                //copy dll
                string modsFolder = Application.persistentDataPath + "/Mods";//; + "/../../AtomTeam/Swordhaven/Mods";

                if (!Directory.Exists(modsFolder))
                {
                    Directory.CreateDirectory(modsFolder);
                }

                var scs = new UnityEditor.Build.Player.ScriptCompilationSettings();
                scs.group = BuildTargetGroup.Standalone;
                scs.options = UnityEditor.Build.Player.ScriptCompilationOptions.None;
                scs.target = buildTarget;
                UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(scs, "Temp/ModBuild_dll");

                Copy("Temp/ModBuild_dll/" + modName + ".dll", modsFolder + "/" + modName + ".dll");
                Copy("Temp/ModBuild_dll/" + modName + ".pdb", modsFolder + "/" + modName + ".pdb");

                //copy res
                string modResFolder = modsFolder;

                string dataAsset = Application.dataPath;
                int index = dataAsset.ToLower().IndexOf(PATH_TO_ASSETS);
                dataAsset = dataAsset.Remove(index, PATH_TO_ASSETS.Length);

                CopyBundle(dataAsset, modResFolder, modName + "_resources");
                CopyBundle(dataAsset, modResFolder, modName + "_scenes");

                Copy("Temp/ModBuild_dll/" + modName + ".dll", "Temp/ModBuild/" + modName + ".dll");
                Copy("Temp/ModBuild_dll/" + modName + ".pdb", "Temp/ModBuild/" + modName + ".pdb");

                EditorUtility.RevealInFinder(modsFolder + "/" + modName + ".dll");

                AssetViewerDB.Load();
            }
        }

        GUILayout.Space(50);

        GUILayout.Label("Publish Settings", EditorStyles.boldLabel);

        if (steam.IsSign())
        {
            EditorGUILayout.LabelField("App Id", steam.GetAppId().ToString());

            if (_modIndex >= _modList.Count)
            {
                if (GUILayout.Button("Refresh"))
                {
                    RequestInfo();
                }
            }
            else if (_modIndex < 0)
            {
                for (int i = 0; i != _modList.Count; ++i)
                {
                    if (GUILayout.Button("Open Mod Item(" + _modList[i].m_rgchTitle + ")"))
                    {
                        _modIndex = i;
                        _rgchTitle = _modList[i].m_rgchTitle;
                        _rgchDescription = _modList[i].m_rgchDescription;
                    }
                }

                if (GUILayout.Button("Create New Mod Item"))
                {
                    _rgchTitle = "";
                    _rgchDescription = "";
                    SteamAPICall_t handle = SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
                    OnCreateItemResultCallResult.Set(handle);
                }
            }
            else
            {
                SteamUGCDetails_t details = _modList[_modIndex]; //copy temp
                EditorGUILayout.LabelField("Mod Id", details.m_nPublishedFileId.ToString());
                _rgchTitle = EditorGUILayout.TextField("Title", _rgchTitle);
                _rgchDescription = EditorGUILayout.TextField("Description", _rgchDescription);
                details.m_eVisibility = (ERemoteStoragePublishedFileVisibility)EditorGUILayout.EnumPopup(details.m_eVisibility);
                _modList[_modIndex] = details; //assign

                if (GUILayout.Button("Upload details"))
                {
                    EditorUtility.DisplayCancelableProgressBar("Uploading to Steam Workshop", "Please wait...", 0);

                    var handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), details.m_nPublishedFileId);
                    SteamUGC.SetItemTitle(handle, _rgchTitle);
                    SteamUGC.SetItemDescription(handle, _rgchDescription);
                    SteamUGC.SetItemVisibility(handle, details.m_eVisibility);
                    SteamAPICall_t callHandle = SteamUGC.SubmitItemUpdate(handle, "");
                    OnSubmitItemUpdateResultCallResult.Set(callHandle);
                }

                GUILayout.Space(20);

                EditorGUILayout.HelpBox("Select and upload preview image to Steam.\n\n" +
                    "Requirements:\n" +
                    "• Format: PNG\n" +
                    "• Size: 512x512 pixels\n" +
                    "• Max file size: 1 MB", MessageType.Info);

                if (GUILayout.Button("Upload preview image"))
                {
                    EditorUtility.DisplayCancelableProgressBar("Uploading to Steam Workshop", "Please wait...", 0);

                    var handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), details.m_nPublishedFileId);
                    SteamUGC.SetItemPreview(handle, EditorUtility.OpenFilePanel("Preview mod image", "", "png"));
                    SteamAPICall_t callHandle = SteamUGC.SubmitItemUpdate(handle, "");
                    OnSubmitItemUpdateResultCallResult.Set(callHandle);
                }

                GUILayout.Space(20);

                EditorGUILayout.HelpBox(
                    "Make sure to press the \"BUILD\" button before uploading.\nOnly pre-built mod content can be uploaded to Steam.", MessageType.Info);

                if (GUILayout.Button("Upload content"))
                {
                    EditorUtility.DisplayCancelableProgressBar("Uploading to Steam Workshop", "Please wait...", 0);

                    var handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), details.m_nPublishedFileId);

                    string dataAsset = Application.dataPath;
                    int index = dataAsset.ToLower().IndexOf(PATH_TO_ASSETS);
                    dataAsset = dataAsset.Remove(index, PATH_TO_ASSETS.Length);

                    string modsFolder = dataAsset + "/" + PATH_BUILD_BUNDLE;

                    SteamUGC.SetItemContent(handle, modsFolder);
                    SteamAPICall_t callHandle = SteamUGC.SubmitItemUpdate(handle, "");
                    OnSubmitItemUpdateResultCallResult.Set(callHandle);
                }

                if (GUILayout.Button("Edit on Steam"))
                {
                    string url = $"https://steamcommunity.com/workshop/filedetails/?id={details.m_nPublishedFileId}";
                    Application.OpenURL(url);
                }

                if (GUILayout.Button("Set Visibility to Public"))
                {
                    EditorUtility.DisplayCancelableProgressBar("Uploading to Steam Workshop", "Please wait...", 0);

                    var handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), details.m_nPublishedFileId);
                    SteamUGC.SetItemTitle(handle, _rgchTitle);
                    SteamUGC.SetItemDescription(handle, _rgchDescription);
                    SteamUGC.SetItemVisibility(handle, details.m_eVisibility);
                    SteamAPICall_t callHandle = SteamUGC.SubmitItemUpdate(handle, "");
                    OnSubmitItemUpdateResultCallResult.Set(callHandle);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Attach to Steam account", MessageType.Warning);

            if (GUILayout.Button("Attach"))
            {
                RequestInfo();
            }
        }
    }

    private static string AddCompilerDefines(string defines, params string[] toAdd)
    {
        List<string> splitDefines = new List<string>(defines.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));
        foreach (var add in toAdd)
            if (!splitDefines.Contains(add))
                splitDefines.Add(add);

        return string.Join(";", splitDefines.ToArray());
    }

    private static string RemoveCompilerDefines(string defines, params string[] toRemove)
    {
        List<string> splitDefines = new List<string>(defines.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));
        foreach (var remove in toRemove)
            splitDefines.Remove(remove);

        return string.Join(";", splitDefines.ToArray());
    }

    void Copy(string src, string dst)
    {
        Debug.Log("Copy " + src + " -> " + dst);
        File.Copy(src, dst, true);
    }

    void OnDestroy()
    {
        steam.Logout();
    }
}