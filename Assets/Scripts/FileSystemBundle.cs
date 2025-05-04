using UnityEngine;
using UnityEngine.SceneManagement;

public class FileSystemBundle : ResourceManager.Bundle
{
    readonly string dir;
    bool lockBase = false;

    public FileSystemBundle(string path)
    {
        dir = path;
    }

    override public UnityEngine.Object LoadAsset(string name, System.Type type)
    {
        UnityEngine.Object obj = null;
        string path = dir + "/" + name;

        if (System.IO.File.Exists(path))
        {
            string ext = System.IO.Path.GetExtension(name);
            switch (ext)
            {
                case ".png":
                    obj = LoadSprite(path);
                    break;
                case ".asset":
                    lockBase = true;
                    obj = ResourceManager.LoadByType(System.IO.Path.ChangeExtension(name, null), type, ext);
                    lockBase = false;
                    //Debug.Log(JsonUtility.ToJson(obj)); //dump proprties
                    JsonUtility.FromJsonOverwrite(LoadText(path), obj);
                    break;

            }
        }
        if (!obj)
        {
            return Resources.Load(System.IO.Path.ChangeExtension(name, null), type);
        }
        else
        {
            return obj;
        }
    }

    public override ResourceManager.IResourceManagerAsyncOperationHandle LoadAssetAsync(string name, System.Type type)
    {
        // @todo add support override
        return new ResourceManager.ResourceManagerAsyncOperationHandle(Resources.LoadAsync(System.IO.Path.ChangeExtension(name, null), type));
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

    public override ResourceManager.IResourceManagerAsyncOperationHandle LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, bool activateOnLoad)
    {
        return null;
    }

    public override bool HasScene(string name)
    {
        return false;
    }

    public override ResourceManager.IResourceManagerAsyncOperationHandle UnloadSceneAsync(string sceneName)
    {
        return null;
    }

    override public bool Contains(string name)
    {
        if (lockBase)
        {
            return false;
        }
        else
        {
            return System.IO.File.Exists(dir + "/" + name);
        }
    }

    override public string[] GetAllAssetNames()
    {
        return System.IO.Directory.GetFiles(dir, "*.*", System.IO.SearchOption.AllDirectories);
    }

    override public string[] GetAllSceneNames()
    {
        //TODO
        return new string[0];
    }

    public string LoadText(string path)
    {
        return System.IO.File.ReadAllText(path);
    }

    public Sprite LoadSprite(string path)
    {
        Texture2D tex2D = new Texture2D(2, 2);
        if (tex2D.LoadImage(System.IO.File.ReadAllBytes(path)))
        {
            return Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0, 0));
        }

        return null;
    }
}

