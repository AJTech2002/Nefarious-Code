using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.IO;
public class SceneHandler : MonoBehaviour
{
    [Header("Outer References")]
    public List<string> tagNames = new List<string>();
    public List<SubsceneTag> tagReference = new List<SubsceneTag>();
    
    public void AddNewTag (SubsceneTag tag)
    {
        tagNames.Add(tag.sceneTagName);
        tagReference.Add(tag);
    }

    
    public void LoadInObjects (List<GameObject> objects, string tag, bool editMode)
    {
        SubsceneTag referenceTag = FindTagFromName(tag);

        if (referenceTag.loaded)
        {
            Scene s = SceneManager.GetSceneByName(referenceTag.sceneTagName + "_SubScene");

            for (int i = 0; i < objects.Count; i++)
            {
                EditorSceneManager.MoveGameObjectToScene(objects[i], s);
            }

            EditorSceneManager.MarkAllScenesDirty();
        }
        else
        {
            LoadScene(referenceTag,editMode);
            Debug.LogError("Needed to load scene, retry loading.");
        }
    }

    private SubsceneTag FindTagFromName (string name)
    {
        for (int i = 0; i < tagNames.Count; i++)
        {
            if (tagNames[i]==name) { return tagReference[i]; }
        }

        Debug.LogError("Couldn't find it.");
        return null;
    }

    public void UnloadScene(SubsceneTag tag, bool editMode)
    {
       
        tag.loaded = false;
        //Call OnUnload();
        if (editMode)
        {
            var scene = EditorSceneManager.CloseScene(tempRef, false);

        }
        else
        {
            var scene = SceneManager.UnloadSceneAsync(tempRef);
        }

    }

    public void UnloadScene(string tag_name, bool editMode)
    {
        SubsceneTag tag = FindTagFromName(tag_name);
        tag.loaded = false;
        //Call OnUnload();
        if (editMode)
        {
            var scene = EditorSceneManager.CloseScene(tempRef, false);
            EditorSceneManager.MarkAllScenesDirty();
        }
        else
        {
            var scene = SceneManager.UnloadSceneAsync(tempRef);
        }

    }

    Scene tempRef;
    public void LoadScene(SubsceneTag tag, bool editMode)
    {

        tag.loaded = true;

        if (editMode)
        {
            var scene = EditorSceneManager.OpenScene(Application.dataPath + "/" + tag.parentID + "/SubScenes/" + tag.sceneTagName + "_SubScene" + ".unity", OpenSceneMode.Additive);
            tempRef = scene;
            EditorSceneManager.MarkAllScenesDirty();
        }
        else
        {
            Scene s = SceneManager.GetSceneByName(tag + "_SubScene");
            var scene = SceneManager.LoadSceneAsync(tag + "_SubScene", LoadSceneMode.Additive);
            
            tempRef = s;
        }

        //Call PreAWake(); on all components of type SceneMonoBehaviour
    }

    public void LoadScene(string tag_string, bool editMode)
    {
        SubsceneTag tag = FindTagFromName(tag_string);

        tag.loaded = true;

        if (editMode)
        {
            var scene = EditorSceneManager.OpenScene(Application.dataPath + "/" + tag.parentID + "/SubScenes/" + tag.sceneTagName + "_SubScene" + ".unity", OpenSceneMode.Additive);
            tempRef = scene;
            EditorSceneManager.MarkAllScenesDirty();
        }
        else
        {
            Scene s = SceneManager.GetSceneByName(tag.sceneTagName + "_SubScene");
            var scene = SceneManager.LoadSceneAsync(tag.sceneTagName + "_SubScene", LoadSceneMode.Additive);

            tempRef = s;
        }

        //Call PreAWake(); on all components of type SceneMonoBehaviour
    }
    //EditorSceneManager.MoveGameObjectToScene(selectedTerrain.terrain.gameObject, newScene);

    public void CreateScene (SubsceneTag tag, string ID)
    {
        if (tagNames.Contains(tag.sceneTagName))
            return;

        AddNewTag(tag);

        System.Collections.Generic.List<EditorBuildSettingsScene> scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        //Can just use copy from 
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            scenes.Add(s);
        }

        var originalScene = EditorSceneManager.GetActiveScene();
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        newScene.name = tag.sceneTagName + "_SubScene";
       

        string sT = tag.sceneTagName;

        var folder = Directory.CreateDirectory(Application.dataPath + "/" + ID + "/SubScenes");

        EditorSceneManager.SaveScene(newScene, Application.dataPath + "/" + ID + "/SubScenes/" + sT + "_SubScene" + ".unity");

       
        EditorSceneManager.SetActiveScene(originalScene);
        scenes.Add(new EditorBuildSettingsScene(Application.dataPath + "/" + ID + "/SubScenes/" + sT + "_SubScene" + ".unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorSceneManager.MarkAllScenesDirty();

        tag.loaded = true;

    }

    public void ToggleLoad(string tag, bool editMode)
    {
        SubsceneTag referenceTag = FindTagFromName(tag);
        if (referenceTag == null) return;

        if (referenceTag.loaded)
        {
            UnloadScene(referenceTag, editMode);
        }
        else
        {
            LoadScene(referenceTag, editMode);
        }

    }


    private Queue<SceneMonoBehaviour> awakes = new Queue<SceneMonoBehaviour>();
    public void QueueAwake (SceneMonoBehaviour script)
    {
        awakes.Enqueue(script);
    }

    private IEnumerator SetupAwakes()
    {
        while (awakes.Count > 0)
        {
            awakes.Dequeue().PreAwake();
            yield return new WaitForEndOfFrame();
        }
    }

}

public interface ISceneLoadable
{
    void PreAwake();
    void OnUnload();
}

public enum SceneType
{
    Renderer,
    Action,
    Mixed
}