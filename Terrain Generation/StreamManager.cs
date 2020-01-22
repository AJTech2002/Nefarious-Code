using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public class StreamManager : MonoBehaviour
{
    //s
    [Header("Options")]
    public bool streaming;
    public Transform player;
    public int radiusX, radiusY;
    public float chunkWaitTime;
    public float tRandomCheck = 3;

    [Header("Temp")]
    public TerrainReference terrainPoint;
    public List<TerrainReference> visibleNeighbours;
    public bool inRange;

    [Header("Distance Rules")]
    public List<DistanceRule> distanceRules = new List<DistanceRule>();

    private void Awake()
    {
        
    }
    private void Start()
    {
        if (streaming)
        {
            manager = TerrainManager.instance;

            Invoke("LoadVisibleNeighbours", 1);

            EvaluateDistanceRules();
        }
    }

    //This is where you do chunk tests
    public void EvaluateDistanceRules ()
    {

        for (int i = 0; i < distanceRules.Count; i++)
        {
            int distX = Mathf.Abs(distanceRules[i].reference.xTile - terrainPoint.xTile);
            int distY = Mathf.Abs(distanceRules[i].reference.yTile - terrainPoint.yTile);

            if (!distanceRules[i].lowerThan)
            {
                if (distX > radiusX || distY > radiusY)
                {
                    if (!distanceRules[i].loaded)
                    {
                        LoadScene(distanceRules[i].reference.terrainID, distanceRules[i].subsceneTag, false, distanceRules[i].checkForAwakes);
                        distanceRules[i].loaded = true;
                    }

                    //distanceRules[i].reference.Load(new System.Action<Scene, string, object>(CallSubScene), distanceRules[i].subsceneTag);
                }
                else if (distX <= radiusX && distY <= radiusY)
                {
                    if (distanceRules[i].loaded)
                    {
                        UnloadScene(distanceRules[i].subsceneTag, false, distanceRules[i].checkForAwakes);
                        distanceRules[i].loaded = false;
                    }
                }
            }
            if (distanceRules[i].lowerThan)
            {
                if (distX < radiusX && distY < radiusY)
                {
                    if (!distanceRules[i].loaded)
                    {
                        LoadScene(distanceRules[i].reference.terrainID, distanceRules[i].subsceneTag, false, distanceRules[i].checkForAwakes);
                        distanceRules[i].loaded = true;
                    }
                }
                else if (distX >= radiusX || distY >= radiusY)
                {
                    if (distanceRules[i].loaded)
                    {
                        UnloadScene(distanceRules[i].subsceneTag, false, distanceRules[i].checkForAwakes);
                        distanceRules[i].loaded = false;
                    }
                }
            }
        }
    }

    private void CallSubScene (Scene s, string arg1, object arg2)
    {
        if (s.GetRootGameObjects()[0].GetComponent<SceneHandler>() != null)
        {
            s.GetRootGameObjects()[0].GetComponent<SceneHandler>().LoadScene(arg1, false);
        }        
    }

    private TerrainManager manager;
    private Queue<TerrainReference> loadQueue = new Queue<TerrainReference>();
    bool ld = false;
    public void LoadVisibleNeighbours()
    {
    
        if (terrainPoint.loaded == false)
        {
            loadQueue.Enqueue(terrainPoint);
        }
        for (int x = -radiusX; x < radiusX+1; x++)
        {
            for (int y = -radiusY; y < radiusY+1; y++)
            {
               
                int neighX = -terrainPoint.xTile + x;
                int neighY = terrainPoint.yTile + y;

                if (x == 0 && y == 0)
                    continue;


                //Double inv.
                if (manager.terrainGrid.ContainsKey(new Point(-neighX,neighY)))
                {
                    TerrainReference refT = manager.terrainGrid[new Point(-neighX, neighY)];

                    if (refT.loaded)
                    {
                        if (!visibleNeighbours.Contains(refT)) visibleNeighbours.Add(refT);
                    }
                    else
                    {
                        //Load into scene
                        loadQueue.Enqueue(refT);
                        if (!visibleNeighbours.Contains(refT))
                        {
                            visibleNeighbours.Add(refT);
                        }
                    }
                }

            }
        }

        if (loadQueue.Count > 0)
        {
            StartCoroutine("LoadChunks");
        }

        
    }

    private IEnumerator LoadChunks()
    {
        while (loadQueue.Count > 0)
        {
            loadQueue.Dequeue().Load();
            yield return new WaitForSeconds(chunkWaitTime);
        }
        ld = true;
    }

    private Queue<TerrainReference> unloadQueue = new Queue<TerrainReference>();
    private IEnumerator UnloadChunks()
    {
        while (unloadQueue.Count > 0)
        {
            unloadQueue.Dequeue().Unload();
            yield return new WaitForSeconds(chunkWaitTime);
        }
    }

    public void CheckVisibleNeighbourOcclusion ()
    {

    }

    public void LoadUnloadNeighbours()
    {

    }

    //Update not allowed, make sure to change this to a call based
    
    float t;
    private void Update()
    {
        if (streaming && ld)
        {
            if (terrainPoint.loadedTerrain.highLOD.gameObject.activeInHierarchy == false)
            {
                terrainPoint.loadedTerrain.highLOD.gameObject.SetActive(true);
                terrainPoint.loadedTerrain.highRenderer.enabled = true;
                terrainPoint.loadedTerrain.midLOD.gameObject.SetActive(false);
            }
            if (!PlayerIsInBounds(terrainPoint))
            {
                //New terrain must be checked. Has to be in a neighbour, don't use raycasts too uncertain
                for (int i = 0; i < visibleNeighbours.Count; i++)
                {
                    if (PlayerIsInBounds(visibleNeighbours[i]) && terrainPoint != visibleNeighbours[i])
                    {
                        terrainPoint = visibleNeighbours[i];
                        terrainPoint.loadedTerrain.highLOD.gameObject.SetActive(true);
                        terrainPoint.loadedTerrain.highRenderer.enabled = true;
                        terrainPoint.loadedTerrain.midLOD.gameObject.SetActive(false);

                        visibleNeighbours[i].loadedTerrain.lowLOD.gameObject.SetActive(false);
                        break;

                    }
                }

                if (!visibleNeighbours.Contains(terrainPoint))
                {
                    visibleNeighbours.Add(terrainPoint);
                }

                UnloadAndLoadVisible();
                LoadVisibleNeighbours();
                EvaluateDistanceRules();

            }
            else
            {
                t -= Time.deltaTime;
                if (t <= 1)
                {
                    manager.RegularClearup();
                }
                if (t <= 0)
                {
                    UnloadAndLoadVisible();
                    t = tRandomCheck;
                }
            }
        }
    }

    private void UnloadAndLoadVisible()
    {
        for (int i = 0; i < visibleNeighbours.Count; i++)
        {

            int distX = Mathf.Abs(visibleNeighbours[i].xTile - terrainPoint.xTile);
            int distY = Mathf.Abs(visibleNeighbours[i].yTile - terrainPoint.yTile);

            if (distX > radiusX || distY > radiusY)
            {
                
                //Unload
                unloadQueue.Enqueue(visibleNeighbours[i]);
                visibleNeighbours.RemoveAt(i);
            }
            else
            {
                if (visibleNeighbours[i] == terrainPoint)
                    continue;

                if (visibleNeighbours[i].loaded)
                {
                    visibleNeighbours[i].loadedTerrain.midLOD.gameObject.SetActive(true);
                    visibleNeighbours[i].loadedTerrain.lowLOD.gameObject.SetActive(false);
                    visibleNeighbours[i].loadedTerrain.highLOD.gameObject.SetActive(false);
                }
                continue;
            }

        }

        if (unloadQueue.Count > 0)
        {
            StartCoroutine("UnloadChunks");
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < visibleNeighbours.Count; i++)
        {
            
            Gizmos.DrawSphere(visibleNeighbours[i].transform.position, 5);
        }
    }

    public bool PlayerIsInBounds (TerrainReference currentTerrain)
    {
        if (currentTerrain.loaded == false || currentTerrain.loadedTerrain == null)
            return false;

        if (player.position.x > currentTerrain.loadedTerrain.max.x && player.position.x < currentTerrain.loadedTerrain.min.x && player.position.z > currentTerrain.loadedTerrain.max.z && player.position.z < currentTerrain.loadedTerrain.min.z)
            return true;

        return false;
    }

    #region SubScene Streaming



    public void UnloadScene(string tag_name, bool editMode, bool doQueue)
    {
        if (!tempDictionary.ContainsKey(tag_name))
            return;


        Scene tempRef = tempDictionary[tag_name];

        if (!tempRef.IsValid() || !tempRef.isLoaded)
            return;
        
            //Call OnUnload();
        if (editMode)
        {
            var scene = EditorSceneManager.CloseScene(tempRef, false);
            EditorSceneManager.MarkAllScenesDirty();
        }
        else
        {
            if (doQueue)
            {
                GameObject[] gos = tempRef.GetRootGameObjects();
                List<SceneMonoBehaviour> mbs = new List<SceneMonoBehaviour>();
                for (int i = 0; i < gos.Length; i++)
                {

                    if (gos[i].GetComponent<SceneMonoBehaviour>() != null)
                        mbs.Add(gos[i].GetComponent<SceneMonoBehaviour>());

                    mbs.AddRange(gos[i].GetComponentsInChildren<SceneMonoBehaviour>());
                }

                for (int i = 0; i < mbs.Count; i++)
                {
                    mbs[i].OnUnload();
                }
            }

            var scene = SceneManager.UnloadSceneAsync(tempRef);
        }

        tempDictionary.Remove(tag_name);
    }

    Dictionary<string, Scene> tempDictionary = new Dictionary<string, Scene>();

    public void LoadScene(string ID, string sceneTagName, bool editMode, bool doQueue)
    {

        if (editMode)
        {
            if (!tempDictionary.ContainsKey(sceneTagName))
            {
                var scene = EditorSceneManager.OpenScene(Application.dataPath + "/" + ID + "/SubScenes/" + sceneTagName + "_SubScene" + ".unity", OpenSceneMode.Additive);
                if (!tempDictionary.ContainsKey(sceneTagName))
                    tempDictionary.Add(sceneTagName, scene);
                EditorSceneManager.MarkAllScenesDirty();
            }
        }
        else
        {
            if (!tempDictionary.ContainsKey(sceneTagName))
            {
                Scene s = SceneManager.GetSceneByName(sceneTagName + "_SubScene");
                var scene = SceneManager.LoadSceneAsync(sceneTagName + "_SubScene", LoadSceneMode.Additive);

                scene.completed += LoadedScene;

                temps.Add((sceneTagName, doQueue));
               
            }
        }

        //Call PreAWake(); on all components of type SceneMonoBehaviour
    }

    List<(string,bool)> temps = new List<(string, bool)>();

    private void LoadedScene (UnityEngine.AsyncOperation op)
    {
        (string sceneTagName, bool doQueue) = temps[temps.Count - 1];
        tempDictionary.Add(sceneTagName, SceneManager.GetSceneByName(sceneTagName + "_SubScene"));
        if (doQueue)
        {
            QueueAll(SceneManager.GetSceneByName(sceneTagName + "_SubScene"));
        }
        temps.RemoveAt(temps.Count - 1);
    }

    private void QueueAll (Scene s)
    {
        GameObject[] gos = s.GetRootGameObjects();

        List<SceneMonoBehaviour> mbs = new List<SceneMonoBehaviour>();
        for (int i = 0; i < gos.Length; i++)
        {

            if (gos[i].GetComponent<SceneMonoBehaviour>() != null)
                mbs.Add(gos[i].GetComponent<SceneMonoBehaviour>());

            mbs.AddRange(gos[i].GetComponentsInChildren<SceneMonoBehaviour>());
        }


        for (int i = 0; i < mbs.Count; i++)
        {
            mbs[i].PreAwake();
        }

    }

    private void DestroyAll (Scene s)
    {

    }

    private Queue<SceneMonoBehaviour> awakes = new Queue<SceneMonoBehaviour>();
    public void QueueAwake(SceneMonoBehaviour script)
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


    //EditorSceneManager.MoveGameObjectToScene(selectedTerrain.terrain.gameObject, newScene);

    #endregion

}

[System.Serializable]
public class Landmark
{
    int radiusRemove;
    string sceneID;
    string sceneSubTag;
}

[System.Serializable]
public class SceneRule
{
    public bool loaded = false;
    public bool checkForAwakes;
    public TerrainReference reference;
    public string ID
    {
        get
        {
            return reference.terrainID;
        }
    }
    public string subsceneTag;
}


[System.Serializable]
public class DistanceRule : SceneRule
{

    public int xDistance, yDistance;
    public bool lowerThan;

}
