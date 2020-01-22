using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SubsceneTag 
{
    public string sceneTagName;
    public int sceneBuild;
    public bool loaded;
    public string parentID;

    public SceneType type;

    public SubsceneTag (string name, int build, SceneType sceneType, string parent)
    {
        sceneTagName = name;
        sceneBuild = build;
        loaded = false;
        type = sceneType;
        parentID = parent;
    }

}

//Superscript
[System.Serializable]
public class SceneMonoBehaviour : MonoBehaviour, ISceneLoadable
{ 

    public virtual void PreAwake()
    {

    }

    public virtual void OnUnload()
    {

    }

}

