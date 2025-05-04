using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using System.Net;
using System.Collections.Specialized;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;

[InitializeOnLoad]
public class ModEditorToolbarExtender
{
    static ModEditorToolbarExtender()
    {
        ToolbarExtender.OnToolbarGUILeft += OnToolbarLeftGUI;
        ToolbarExtender.OnToolbarGUIRight += OnToolbarRightGUI;
    }

    static GUIContent sceneListContent = new GUIContent("Scene List", EditorGUIUtility.IconContent("SceneAsset Icon").image, "Game/Scene List %l");
    static GUIContent assetListContent = new GUIContent("Asset List", EditorGUIUtility.IconContent("ParticleSystemForceField Gizmo").image, "Game/Asset List %j");
    static GUIContent saveEntitesContent = new GUIContent("Save Logic", EditorGUIUtility.IconContent("Update-Available").image, "Game/Save Logic");

    static void OnToolbarLeftGUI()
    {
        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(sceneListContent, EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            SceneList.Init();
        }

        GUILayout.Space(10);

        if (GUILayout.Button(assetListContent, EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            AssetViewer.Init();
        }

        GUILayout.EndHorizontal();
    }

    static void OnToolbarRightGUI()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button(saveEntitesContent, EditorStyles.toolbarButton))
        {
            GameEditor.SaveLevel();
        }

        GUILayout.Space(20);

        GUILayout.EndHorizontal();
    }
}
