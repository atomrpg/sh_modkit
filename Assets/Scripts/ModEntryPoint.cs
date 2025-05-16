using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSon;
using System.Reflection;
using System.Runtime.CompilerServices;

//[assembly: AssemblyTitle("My Mod")] // ENTER MOD TITLE


public class ModEntryPoint : MonoBehaviour // ModEntryPoint - RESERVED LOOKUP NAME
{
    string modName;
    string dir;

    void Start()
    {
        var assembly = GetType().Assembly;
        modName = assembly.GetName().Name;
        dir = System.IO.Path.GetDirectoryName(assembly.Location);
        Debug.Log("Mod Init: " + modName + "(" + dir + ")");

        GlobalEvents.AddListener<GlobalEvents.GameStart>(GameLoaded);
        GlobalEvents.AddListener<GlobalEvents.LevelLoaded>(LevelLoaded);
		
		LoadModBundle();
    }

    void LoadModBundle()
    {
        ResourceManager.DEBUG_MODE = true;
#if UNITY_EDITOR
        // skip bundle loading in PIE mode
#else
        ResourceManager.AddBundle(modName + "_resources", AssetBundle.LoadFromFile(dir + "/" + modName + "_resources"));
        ResourceManager.AddSceneBundle(modName + "_scenes", AssetBundle.LoadFromFile(dir + "/" + modName + "_scenes"));
#endif
    }

    void GameLoaded(GlobalEvents.GameStart evnt)
    {
        Localization.LoadStrings("mymod_strings_", Localization.Language);
    }

    void LevelLoaded(GlobalEvents.LevelLoaded evnt)
    {
        Debug.Log(evnt.levelName);
    }

    void Update()
    {
        
    }
}