//#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

using System.Reflection;

public class SceneList : EditorWindow
{
    [MenuItem("Game/Scene List %l")]
    static public void Init()
    {
        GetWindow<SceneList>().Show();
    }

    public void Awake()
    {
        Refresh();
    }



    void Refresh()
    {
        sceneList.Clear();

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            sceneList.Add(buildScene.path, false);
        }

        foreach (var bundle in ResourceManager.bundles)
        {
            foreach (var prefab in bundle.GetAllSceneNames())
            {
                sceneList.Add(prefab, false);
            }
        }
    }

    Dictionary<string, bool> sceneList = new Dictionary<string, bool>();

    Vector2 listScrollPos = Vector2.zero;

    string filter = "";

    private static bool IsSceneInBuildSettings(string scenePath)
    {
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path == scenePath && scene.enabled)
            {
                return true;
            }
        }
        return false;
    }

    public void OnGUI()
    {
        if (GUILayout.Button("[Refresh]"))
        {
            Refresh();
        }

        EditorGUILayout.BeginHorizontal();
        filter = EditorGUILayout.TextField(filter);
        if(GUILayout.Button("[X]"))
        {
            filter = "";
        }
        EditorGUILayout.EndHorizontal();
        //GUILayout.Space(20);

        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos);
   
        string filterLow = filter.ToLower();
        foreach (var scene in sceneList)
        {
            if (filter.Length == 0 || scene.Key.ToLower().Contains(filterLow))
            {
                if (GUILayout.Button(scene.Key))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        if (IsSceneInBuildSettings(scene.Key))
                        {
                            string logicPath = scene.Key.Replace(".unity", "_Logic.unity");
                            string editorPath = scene.Key.Replace(".unity", "_Editor.unity");

                            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(scene.Key)))
                            {
                                EditorSceneManager.OpenScene(scene.Key);
                                EditorSceneManager.OpenScene(logicPath, OpenSceneMode.Additive);
                            }
                            else
                            {
                                EditorSceneManager.OpenScene(logicPath);
                            }

                            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(editorPath)))
                            {
                                EditorSceneManager.OpenScene(editorPath, OpenSceneMode.Additive);
                            }
                        }
                        else // PIE mode
                        {
                            var playInEditor = GameObject.FindObjectOfType<PlayInEditor>();

                            if (playInEditor == null)
                            {
                                var goPIE = new GameObject("PlayInEditor");
                                playInEditor = goPIE.AddComponent<PlayInEditor>();
                                EditorUtility.SetDirty(goPIE);
                            }

                            playInEditor.spawnScene = scene.Key;
                            playInEditor.SpawnScene();

                            var goPlayer = GameObject.Find("Player");
                            if (goPlayer == null) // auto create player
                            {
                                goPlayer = new GameObject("Player");

                                var cc = goPlayer.AddComponent<CharacterComponent>();

                                {
                                    var c = new Character();
                                    c.CharProto = ResourceManager.Load<CharacterProto>("Entities/Character/Player", ResourceManager.EXT_ASSET);
                                    c.creatureProto = ResourceManager.Load<CreatureProto>("Entities/Creature/BaseMale", ResourceManager.EXT_ASSET);
                                    c.SetCapsValue(Character.CharacterCaps.Player);
                                    c.SetFraction("player");
                                    cc.SetEntity(c);
                                }

                                cc.InvalidateData();
                                var enterPoint = GameObject.Find("EnterPoint");
                                if (enterPoint != null) // auto place to EnterPoint
                                {
                                    cc.transform.position = enterPoint.transform.position;
                                }
                                EditorUtility.SetDirty(goPlayer);
                            }
                        }
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }
}
//#endif