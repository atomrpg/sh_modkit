﻿
using UnityEngine;

using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using System.IO;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[DefaultExecutionOrder(-999)]
public class PlayInEditor : MonoBehaviour
{
#if UNITY_EDITOR
    public Light _sunTestLight = null;

    public string spawnScene = null;
    public bool useModEntryPoint = true;

    List<GameObject> tempSceneObj = new List<GameObject>();

    public class ResourcesBundle : ResourceManager.Bundle
    {
        public ResourcesBundle()
        {
        }

        override public UnityEngine.Object LoadAsset(string name, System.Type type)
        {
            return Resources.Load(System.IO.Path.ChangeExtension(name, null), type);
        }

        public override ResourceManager.IResourceManagerAsyncOperationHandle LoadAssetAsync(string name, System.Type type)
        {
            return new ResourceManager.ResourceManagerAsyncOperationHandle(Resources.LoadAsync(name, type));
        }

        public override void OnAssetModification()
        {
        }

        public override void UnloadUnusedAssets()
        {

        }

        public override void Unload(bool unloadAllLoadedObjects)
        {
            //skip
        }

        public override bool HasScene(string name)
        {
            return false;
        }

        public override ResourceManager.IResourceManagerAsyncOperationHandle LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, bool activateOnLoad)
        {
            return null;
        }

        public override ResourceManager.IResourceManagerAsyncOperationHandle UnloadSceneAsync(string sceneName)
        {
            return null;
        }

        override public bool Contains(string name)
        {
            return System.Array.IndexOf(GetAllAssetNames(), name) >= 0;
        }

        string[] allAssets = null;

        string[] FetchAssetNames()
        {
            string[] guids = AssetDatabase.FindAssets("", new string[] { "Assets/Resources" });

            allAssets = new string[guids.Length];

            for (int i = 0, end = guids.Length; i!=end; ++i)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]).ToLower().Replace(RefUtils.PATH_TO_RES, "");
                allAssets[i] = path;
            }

            return allAssets;
        }

        override public string[] GetAllAssetNames()
        {
            if(allAssets == null)
            {
                allAssets = FetchAssetNames();
            }

            return allAssets;
        }

        override public string[] GetAllSceneNames()
        {
            //TODO
            return new string[0];
        }
    }

    private static void LoadBundles(string path)
    {
        if (path.Length == 0 || !Directory.Exists(path))
        {
            return;
        }

        foreach (string f in Directory.GetFiles(path, "*.bundle", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(f) == ".bundle" /*&& !Path.GetFileName(f).StartsWith("scene_", System.StringComparison.OrdinalIgnoreCase)*/)
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(f);
                ResourceManager.AddBundle(bundle.name, bundle);
            }
        }
    }

    void CreateEntryPoint()
    {
        GameObject enterPoint = new GameObject("ModEntryPoint");
        enterPoint.AddComponent<ModEntryPoint>();
    }

    void Awake()
    {
        if (EditorApplication.isPlaying)
        {
            // disable main camera 
            var defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera)
            {
                defaultCamera.SetActive(false);
            }

            if (useModEntryPoint)
            {
                CreateEntryPoint();
            }

            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;


            AssetBundle.UnloadAllAssetBundles(true);

            ResourceManager.Reset();
            ResourceManager.SetAssetGetPathCallback(null);

            ResourceManager.AddBundle("resources", new ResourcesBundle());

            LoadBundles(Application.streamingAssetsPath);

            var gdir = PlayerPrefs.GetString("GAME_CONTENT_DIR", "");
            LoadBundles(gdir);

            GameObject game = ResourceManager.Load<GameObject>("Game", ResourceManager.EXT_PREFAB);

            if(game == null)
            {
                Debug.LogError("Game prefab not found from bundles");
            }

            Instantiate(game);

            if (spawnScene.Length > 0)
            {
                SpawnScene();
            }

            //SpawnObjects();

            if (_sunTestLight != null)
            {
                _sunTestLight.gameObject.SetActive(false);
            }

            DontDestroyOnLoad(gameObject);

            GlobalEvents.PerformEvent(new GlobalEvents.LanguageChanged() { langId = Localization.Language });
        }
        else
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
    }


    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (scene.name.ToLower().Contains("_editor"))
                {
                    SceneManager.SetActiveScene(scene);
                    break;
                }
            }
        }
    }

    void SpawnObjects()
    {
        bool isPlaying = Application.isPlaying;
        //fallback replace detect
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene activeScene = SceneManager.GetSceneAt(i);

            foreach (var obj in activeScene.GetRootGameObjects())
            {
                foreach (EntityComponent r in obj.GetComponentsInChildren<EntityComponent>())
                {
                    if (!IsValidEntityObject(r.gameObject))
                    {
                        if (r.Entity == null)
                        {
                            Debug.LogError("PIE Error [Entity is null]" + r.name);
                            continue;
                        }
                        else if (r.Entity.Prototype == null)
                        {
                            Debug.LogError("PIE Error [Prototype is null]" + r.name);
                            continue;
                        }
                        else if (r.Entity.Prototype.Prefab == null)
                        {
                            Debug.LogError("PIE Error [Prefab is null]" + r.name);
                            continue;
                        }

                        var prefab = r.Entity.Prototype.Prefab;

                        var copy = Instantiate(prefab, r.gameObject.transform.position, r.gameObject.transform.rotation);
                        copy.transform.localScale = r.transform.lossyScale;
                        copy.name = r.name;

                        copy.GetComponent<EntityComponent>().SetEntity(r.Entity);
                        r.gameObject.SetActive(false);
                        r.gameObject.name += "_temp";
                        if(isPlaying)
                        {
                            DestroyImmediate(r.gameObject);
                        }
                    }
                }
            }
        }
    }

    bool _requestShowScene = false;

    void Start()
    {
        if (EditorApplication.isPlaying)
        {
            SpawnObjects();
        }
        else
        {
            _requestShowScene = true;
        }
    }

    private void Update()
    {
        if (_requestShowScene && spawnScene != null && spawnScene.Length > 0 && ResourceManager.bundles.Count > 0)
        {
            _requestShowScene = false;

            SpawnScene();
        }
    }

    void CleanupTemp()
    {
        Debug.Log("CleanupTemp");

        if (tempSceneObj.Count > 0)
        {
            foreach (var goDel in tempSceneObj)
            {
                if (goDel != null)
                {
                    DestroyImmediate(goDel);
                }
            }

            tempSceneObj.Clear();
        }
    }

    public void SpawnScene()
    {
        CleanupTemp();

        //var resPath = RefUtils.ConvertAssetPathToResPath(spawnScene, "");

        if (Application.isPlaying)
        {
            ResourceManager.LoadSceneAsync(spawnScene, LoadSceneMode.Additive);
        }

        string sceneName = Path.GetFileNameWithoutExtension(spawnScene);
        Debug.Log("Spawn Scene: " + sceneName);

        Dictionary<GameObject, bool> loadMap = new();
        var asset = ResourceManager.Load<TextAsset>("Levels/" + sceneName + "_Logic", ResourceManager.EXT_SCRIPT);
        Game.LoadItemsFromSource(asset.text, loadMap);

        foreach (var goEnt in loadMap)
        {
            NotEditableTransform(goEnt.Key);
            goEnt.Key.gameObject.SetActive(goEnt.Value);
        }

        /*
        var prefab = ResourceManager.Load<GameObject>(resPath, ResourceManager.EXT_PREFAB); 

        var go = Instantiate(prefab);
        go.name = "Level";

        NotEditableTransform(go);

        var items = ResourceManager.Load<TextAsset>(RefUtils.ConvertAssetPathToResPath(spawnScene.Replace("level.prefab", "entities.json"), ""), ResourceManager.EXT_SCRIPT);
        */
        /*
         TODO: LoadItemsASync
        foreach(var goEnt in GammeL.LoadItems("", items.text))
        {
            NotEditableTransform(goEnt);
        }
        */

    }

    void NotEditableTransform(GameObject go)
    {
        for (int i = go.transform.childCount - 1; i >= 0; --i)
        {
            NotEditableTransform(go.transform.GetChild(i).gameObject);
        }

        tempSceneObj.Add(go);
        go.hideFlags |= HideFlags.NotEditable | HideFlags.DontSaveInEditor;
    }

    bool IsValidEntityObject(GameObject go)
    {
        return !PrefabUtility.IsPartOfAnyPrefab(go) && go.GetComponentsInChildren<Collider>(true).Length > 0;
    }
    /*
    [InitializeOnEnterPlayMode]
    static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
    {
        if (instance != null)
        {
            instance.CleanupTemp();
        }
    }
    */

    private void OnDestroy()
    {
        CleanupTemp();

        if (EditorApplication.isPlaying)
        {
            EntityComponent.DontInspectorGUI = true;
            ResourceManager.Reset();
            ResourceManager.SetAssetGetPathCallback(null);
        }
    }

    public static string GetScenePrefabPath(Transform obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent;
            path = "/" + obj.name + path;
        }
        return path.Remove(0, "/Level/".Length);
    }

    [MenuItem("Game/Create ScenePrefab (Selection)")]
    static void CreateScenePrefab()
    {
        var pie = GameObject.Find("PlayInEditor");
        string sceneName = pie.GetComponent<PlayInEditor>().spawnScene;

        HideFlags hideFlags = HideFlags.NotEditable | HideFlags.DontSaveInEditor;
        var selections = Selection.gameObjects;
        List<Object> newSelection = new List<Object>();
        foreach (var go in selections)
        {
            if ((go.hideFlags & hideFlags) == hideFlags)
            {
                var obj = new GameObject();
                var scenePrefab = obj.AddComponent<ScenePrefab>();
                scenePrefab.path = GetScenePrefabPath(go.transform);
                scenePrefab.sceneName = sceneName;
                scenePrefab.Spawn();

                obj.name = go.name;
                newSelection.Add(obj);

                obj.transform.position = go.transform.position;

                EditorUtility.SetDirty(obj);
            }
            else
            {
                Debug.Log("Isn't gameobject, skiped :" + go.name);
            }
        }

        Selection.objects = newSelection.ToArray();

        EditorSceneManager.MarkSceneDirty(pie.scene);
    }
#endif
}