using UnityEngine;
using System.Reflection;

//[assembly: AssemblyTitle("My Mod")] // ENTER MOD TITLE
public class ModEntryPoint : MonoBehaviour // ModEntryPoint - RESERVED LOOKUP NAME
{
    string _modName;
    string _dir;

    void Start()
    {
        var assembly = GetType().Assembly;
        _modName = assembly.GetName().Name;
        _dir = System.IO.Path.GetDirectoryName(assembly.Location);
        Debug.Log("Mod Init: " + _modName + "(" + _dir + ")");

        GlobalEvents.AddListener<GlobalEvents.GameStart>(GameLoaded);
        GlobalEvents.AddListener<GlobalEvents.LevelLoaded>(LevelLoaded);
        GlobalEvents.AddListener<GlobalEvents.LanguageChanged>(LanguageChanged);

        LoadModBundle();

        ApplyLocalization();
    }

    void LoadModBundle()
    {
        //ResourceManager.DEBUG_MODE = true;
#if UNITY_EDITOR
        // skip bundle loading in PIE mode
#else
        ResourceManager.AddBundle(modName + "_resources", AssetBundle.LoadFromFile(dir + "/" + modName + "_resources"));
        ResourceManager.AddSceneBundle(modName + "_scenes", AssetBundle.LoadFromFile(dir + "/" + modName + "_scenes"));
#endif
    }

    void ApplyLocalization()
    {
        //One-lang supported
        Localization.LoadStrings(_modName + "_strings_", "en");
        //Multi-lang supported
        //Localization.LoadStrings(GetModName() + "_strings_", Localization.Language);
    }

    void LanguageChanged(GlobalEvents.LanguageChanged evnt)
    {
        ApplyLocalization();
    }

    void GameLoaded(GlobalEvents.GameStart evnt)
    {
    }

    void LevelLoaded(GlobalEvents.LevelLoaded evnt)
    {
        //Debug.Log(evnt.levelName);
    }

    void Update()
    {
        
    }
}