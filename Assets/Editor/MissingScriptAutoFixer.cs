using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class MissingScriptAutoFixer : EditorWindow
{
    private class MissingEntry
    {
        public string assetPath;
        public string guid;
        public MonoScript replacement;
    }

    private List<MissingEntry> missingList = new List<MissingEntry>();
    private Vector2 scroll;

    [MenuItem("Tools/Missing Script AutoFixer")]
    public static void Open()
    {
        GetWindow<MissingScriptAutoFixer>("Missing Script AutoFixer");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Scan Selection"))
        {
            ScanScene();
        }

        if (missingList.Count > 0)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var entry in missingList)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("guid", entry.guid.ToString());
                EditorGUILayout.LabelField("Asset Path", entry.assetPath);

                entry.replacement = (MonoScript)EditorGUILayout.ObjectField("New Script", entry.replacement, typeof(MonoScript), false);

                if (entry.replacement != null)
                {
                    string newGuid = "";
                    long localId = 0;
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(entry.replacement, out newGuid, out localId);

                    string newFileID = localId.ToString();
                    // string newGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(entry.replacement));
                    // string newFileID = "1423921309"; // Dll MonoBehaviour
                    string newLine = $"  m_Script: {{fileID: {newFileID}, guid: {newGuid}, type: 3}}";

                    string oldFileID = "11500000"; // Default MonoBehaviour
                    string oldLine = $"  m_Script: {{fileID: {oldFileID}, guid: {entry.guid}, type: 3}}";

                    EditorGUILayout.SelectableLabel($"Patch: {oldLine} → {newLine}");

                    if (GUILayout.Button("Apply Replacement"))
                    {
                        ReplaceInFile(entry.assetPath, oldLine, newLine);
                        Debug.Log($"Replaced in {entry.assetPath}");
                        EditorGUILayout.EndVertical();
                        ScanScene();
                        break;
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("No missing scripts found.");
        }
    }

    private void ScanScene()
    {
        missingList.Clear();

        var go = Selection.activeGameObject;
        string path = GetPrefabOrScenePath(go);
        string[] missingGuids = FindMissingScriptLine(path);

        foreach (string guid in missingGuids)
        {
            missingList.Add(new MissingEntry
            {
                assetPath = path,
                guid = guid,
            });
        }
    }

    public static string GetOutermostPrefabAssetPath(GameObject go)
    {
        return UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(go).assetPath;
    }

    private string GetPrefabOrScenePath(GameObject go)
    {
        return GetOutermostPrefabAssetPath(go);
            /*
        if (PrefabUtility.IsPartOfPrefabAsset(go))
        {
            return AssetDatabase.GetAssetPath(go);
        }
        else if (PrefabUtility.IsPartOfPrefabInstance(go))
        {
            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            return AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(root));
        }
        else
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        }*/
    }

    public static bool DoesScriptExist(string guid)
    {
        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (!string.IsNullOrEmpty(assetPath))
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            return script != null;
        }

        return false;
    }

    public static string ExtractGuidFromScriptLine(string line)
    {
        int guidIndex = line.IndexOf("guid:");
        if (guidIndex >= 0)
        {
            int start = guidIndex + "guid:".Length;
            int end = line.IndexOf(',', start);
            if (end == -1) end = line.IndexOf('}', start);
            if (end > start)
            {
                return line.Substring(start, end - start).Trim();
            }
        }
        return null;
    }

    private string[] FindMissingScriptLine(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);

        List<string> result = new List<string>();
        foreach (string line in lines)
        {
            if (line.TrimStart().StartsWith("m_Script:") && line.Contains("guid:") && line.Contains("fileID:"))
            {
                var guid = ExtractGuidFromScriptLine(line);
                if (!DoesScriptExist(guid))
                {
                    result.Add(guid);
                }
            }
        }

        return result.ToArray();
    }

    private void ReplaceInFile(string path, string oldLine, string newLine)
    {
        string text = File.ReadAllText(path);
        if (text.Contains(oldLine))
        {
            File.WriteAllText(path, text.Replace(oldLine, newLine));
            AssetDatabase.Refresh();
        }
    }
}
