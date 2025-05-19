using UnityEngine;
using System.Reflection;

//[assembly: AssemblyTitle("My Mod")] // Optional: Set your mod's title for display in metadata
public class ModEntryPoint : MonoBehaviour // This class is the reserved entry point for the mod
{
    string _modName;
    string _dir;

    void Start()
    {
        // Get the mod name and directory from the assembly info
        var assembly = GetType().Assembly;
        _modName = assembly.GetName().Name;
        _dir = System.IO.Path.GetDirectoryName(assembly.Location);
        Debug.Log("Mod Init: " + _modName + " (" + _dir + ")");

        // Register callbacks for game events
        GlobalEvents.AddListener<GlobalEvents.GameStart>(GameLoaded);
        GlobalEvents.AddListener<GlobalEvents.LevelLoaded>(LevelLoaded);
        GlobalEvents.AddListener<GlobalEvents.LanguageChanged>(LanguageChanged);

        // Load asset bundles from mod folder
        LoadModBundle();

        // Load localization strings (if available)
        ApplyLocalization();
    }

    void LoadModBundle()
    {
#if UNITY_EDITOR
        // In Editor: skip loading bundles during Play In Editor mode
#else
        // Load resources and scenes from AssetBundles (must be built beforehand)
        ResourceManager.AddBundle(_modName + "_resources", AssetBundle.LoadFromFile(_dir + "/" + _modName + "_resources"));
        ResourceManager.AddSceneBundle(_modName + "_scenes", AssetBundle.LoadFromFile(_dir + "/" + _modName + "_scenes"));
#endif
    }

    void ApplyLocalization()
    {
        // Load English strings by default
        Localization.LoadStrings(_modName + "_strings_", "en");

        // Optional: load strings based on current language
        // Localization.LoadStrings(_modName + "_strings_", Localization.Language);
    }

    void LanguageChanged(GlobalEvents.LanguageChanged evnt)
    {
        // Reapply localization on language change
        ApplyLocalization();
    }

    void GameLoaded(GlobalEvents.GameStart evnt)
    {
        // Called when a new game starts or a save is loaded
    }

    void LevelLoaded(GlobalEvents.LevelLoaded evnt)
    {
        // Called when a new level is loaded
        // Debug.Log(evnt.levelName); // Uncomment for debug output
    }

    void Update()
    {
        // Called every frame — usually not needed for simple mods
    }
}