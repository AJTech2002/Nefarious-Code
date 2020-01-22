using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class TerrainReference : MonoBehaviour
{
    
    public NoiseGenerator terrain
    {
        get
        {
            if (loaded)
            {

                return loadedTerrain;

                
            };


            return null;
        }
        set
        {
            terrain = value;
        }
    }

    public string terrainID = "01";

    private List<EdgeHelpers.Edge> tempEdges = new List<EdgeHelpers.Edge>();

    public List<EdgeHelpers.Edge> edges
    {

        get
        {
            if (tempEdges.Count == 0)
            {
                if (loaded && loadedTerrain != null)
                tempEdges = EdgeHelpers.GetEdges(loadedTerrain.highLOD.sharedMesh.triangles).FindBoundary();

            }

            return tempEdges;
        }

    }

    [Header("Set Parameters")]
    public int xTile;
    public int yTile;

    [Header("State")]
    public bool loaded;
    public NoiseGenerator worstCaseTemp;

    public NoiseGenerator loadedTerrain
    {
        get
        {
            if (worstCaseTemp != null) return worstCaseTemp;
            if (loaded)
            {
                if (Application.isPlaying && tempPlayingScene != null && tempPlayingScene.IsValid() && tempPlayingScene.isLoaded)
                {
                    worstCaseTemp = tempPlayingScene.GetRootGameObjects()[0].GetComponent<NoiseGenerator>();
                }
                else if (!Application.isPlaying && tempScene != null && tempScene != null && tempScene.IsValid() && tempScene.isLoaded)
                {
                    worstCaseTemp = tempScene.GetRootGameObjects()[0].GetComponent<NoiseGenerator>();
                }
                
                else
                {
                    Scene s = SceneManager.GetSceneByName(terrainID + "_Scene");
                    if (s!= null && s.IsValid() && s.isLoaded)
                    {
                        worstCaseTemp = s.GetRootGameObjects()[0].GetComponent<NoiseGenerator>();
                        tempPlayingScene = s;
                    }
                }
            }

            return worstCaseTemp;
        }
        set
        {
            worstCaseTemp = value;
        }
    }
    public int sceneIndex;
    public bool hasScene;

    private void Start()
    {
        
        if (loaded && loadedTerrain != null)
        {
            loadedTerrain.terrainID = terrainID;
            if (TerrainManager.instance.editingTerrain)
                loadedTerrain.SetupTerrain();
        }

        TerrainManager.LoadTerrain(this);
    }

    private Scene tempScene;
    private Scene tempPlayingScene;

    //This takes some time (do this stuff by default, this stuff should be remvoed later one bcause the prefabs will be setup naturally like this, run through a loop and have a settings that it iwll be applied to all)
    private void LoadTerrain(UnityEngine.AsyncOperation op)
    {
        Scene s = SceneManager.GetSceneByName(terrainID + "_Scene");
        
        loadedTerrain = s.GetRootGameObjects()[0].GetComponent<NoiseGenerator>();
        loadedTerrain.overrideEditing = TerrainManager.instance.editingTerrain;
        loadedTerrain.GetComponent<NoiseGenerator>().enabled = TerrainManager.instance.editingTerrain;
        Debug.LogWarning("LOD Group Hasn't been removed for all prefabs...");
        if (loadedTerrain.transform.GetChild(0).GetComponent<LODGroup>() != null)
            loadedTerrain.transform.GetChild(0).GetComponent<LODGroup>().enabled = false;
        loadedTerrain.transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
        loadedTerrain.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
        loadedTerrain.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);

        if (tempCallback != null)
            tempCallback.Invoke(s,tempArgs,"");

        if (loadedTerrain != null)
        {
            loadedTerrain.terrainID = terrainID;
            
            loaded = true;
            tempPlayingScene = s;
           
        }
        else if (loadedTerrain == null)
        {
            Debug.LogError("Scene was not loaded?");
            loaded = false;
        }

    }

    private System.Action<Scene, string, object> tempCallback;
    private string tempArgs;
    public void Load (System.Action<Scene, string, object> callback = null, string args = "")
    {
        hasScene = true;

        if (Application.isPlaying)
        {

            Scene s = SceneManager.GetSceneByName(terrainID + "_Scene");
           
            //Application.dataPath + "/TerrainChunks/" + terrainID + "_Scene" + ".unity"
            var scene = SceneManager.LoadSceneAsync(terrainID + "_Scene", LoadSceneMode.Additive);
            tempCallback = callback;
            tempArgs = args;
            scene.completed += new System.Action<AsyncOperation>(LoadTerrain);
            
        }

        else if (!Application.isPlaying)
        {
            Debug.Log("LOAD EDITMODE");
            
            var scene = EditorSceneManager.OpenScene(Application.dataPath + "/TerrainChunks/" + terrainID + "_Scene" + ".unity", OpenSceneMode.Additive);
            loadedTerrain = scene.GetRootGameObjects()[0].GetComponent<NoiseGenerator>();
            loadedTerrain.overrideEditing = true;
            loadedTerrain.terrainID = terrainID;
            loaded = true;
            tempScene = scene;
            loadedTerrain.editing = true;
            loadedTerrain.overrideEditing = true;
            loadedTerrain.resetOnStart = true;

            loadedTerrain.GetComponent<NoiseGenerator>().enabled = true;
            Debug.LogWarning("LOD Group Hasn't been removed for all prefabs...");
            if (loadedTerrain.transform.GetChild(0).GetComponent<LODGroup>() != null)
            loadedTerrain.transform.GetChild(0).GetComponent<LODGroup>().enabled = true;

            loadedTerrain.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
            loadedTerrain.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().enabled = true;
            loadedTerrain.transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
            loadedTerrain.transform.GetChild(0).GetChild(1).GetComponent<MeshRenderer>().enabled = true;
            loadedTerrain.transform.GetChild(0).GetChild(2).gameObject.SetActive(true);
            loadedTerrain.transform.GetChild(0).GetChild(2).GetComponent<MeshRenderer>().enabled = true;


            if (loadedTerrain == null)
            {
                Debug.LogError("Scene was not loaded?");
                loaded = false;
            }
        }
    }

    public void Unload() 
    {
        if (Application.isPlaying && tempPlayingScene.IsValid() && tempPlayingScene.isLoaded)
        {
            hasScene = true;
          
            var scene = SceneManager.UnloadSceneAsync(tempPlayingScene);

            loaded = false;
            loadedTerrain = null;


            if (loadedTerrain != null)
            {
                Debug.LogError("Scene was not unloaded?");
                loaded = false;
            }
        }
        else if (!Application.isPlaying)
        {
            Debug.Log("UNLOAD EDITMODE");
            hasScene = true;

            terrain.overrideEditing = false;
            terrain.editing = false;
            terrain.resetOnStart = false;

            var scene =  EditorSceneManager.CloseScene(tempScene, false);


            loaded = false;
            loadedTerrain = null;

            if (loadedTerrain != null)
            {
                Debug.LogError("Scene was not unloaded?");
                loaded = false;
            }
        }
    }

    public void EdgeCombine()
    {
        
        for (int x = -1 - 1; x < 1 + 2; x++)
        {
            for (int y = -1 - 1; y < 1 + 2; y++)
            {
                if (x == 0 && y == 0) continue;

                int neighX = xTile + x;
                int neighY = yTile + y;
                if (GameObject.FindObjectOfType<TerrainManager>().terrainGrid.ContainsKey(new Point(neighX, neighY)))
                {
                    TerrainReference neighboru = TerrainManager.instance.terrainGrid[new Point(neighX, neighY)];

                    bool gLX = (neighX > xTile) || (neighX < xTile);
                    bool gLY = (neighY > yTile) || (neighY < yTile);

                    if (gLX && gLY) continue;

                    if (!neighboru.loaded || neighboru.loadedTerrain == null)
                        continue;

                    if (neighX > xTile)
                    {
                        Stitch(edges, neighboru.GetSideEdges("right", neighboru.loadedTerrain.midLOD), loadedTerrain.highLOD, neighboru.loadedTerrain.midLOD);
                        Stitch(edges, neighboru.GetSideEdges("right", neighboru.loadedTerrain.highLOD), loadedTerrain.highLOD, neighboru.loadedTerrain.highLOD);
                        continue;
                    }

                    if (neighX < xTile)
                    {
                        Stitch(edges, neighboru.GetSideEdges("left", neighboru.loadedTerrain.midLOD), loadedTerrain.highLOD, neighboru.loadedTerrain.midLOD);
                        Stitch(edges, neighboru.GetSideEdges("left", neighboru.loadedTerrain.highLOD), loadedTerrain.highLOD, neighboru.loadedTerrain.highLOD);
                        continue;
                    }

                    if (neighY > yTile)
                    {
                        Stitch(edges, neighboru.GetSideEdges("up", neighboru.loadedTerrain.midLOD), loadedTerrain.highLOD, neighboru.loadedTerrain.midLOD);
                        Stitch(edges, neighboru.GetSideEdges("up", neighboru.loadedTerrain.highLOD), loadedTerrain.highLOD, neighboru.loadedTerrain.highLOD);
                        continue;
                    }

                    if (neighY < yTile)
                    {
                        Stitch(edges, neighboru.GetSideEdges("down", neighboru.loadedTerrain.midLOD), loadedTerrain.highLOD, neighboru.loadedTerrain.midLOD);
                        Stitch(edges, neighboru.GetSideEdges("down", neighboru.loadedTerrain.highLOD), loadedTerrain.highLOD, neighboru.loadedTerrain.highLOD);
                        continue;
                    }

                }
            }

        }
        /*
        */
    }

    public List<EdgeHelpers.Edge> GetSideEdges (string side, MeshFilter highLOD)
    {
        List<EdgeHelpers.Edge> boundary = EdgeHelpers.GetEdges(highLOD.sharedMesh.triangles).FindBoundary();
        List<EdgeHelpers.Edge> returnEdges = new List<EdgeHelpers.Edge>();

        for (int i = 0; i < boundary.Count; i++)
        {
            if (side == "right" && highLOD.sharedMesh.vertices[boundary[i].v1].x >= 0.95 && highLOD.sharedMesh.vertices[boundary[i].v2].x >= 0.95)
            {
                Debug.DrawLine(highLOD.transform.TransformPoint(highLOD.sharedMesh.vertices[boundary[i].v1]), highLOD.transform.TransformPoint(highLOD.sharedMesh.vertices[boundary[i].v2]), Color.red, 3);
                returnEdges.Add(boundary[i]);
                continue;
            }

            if (side == "left" && highLOD.sharedMesh.vertices[boundary[i].v1].x <= -0.95 && highLOD.sharedMesh.vertices[boundary[i].v2].x <= -0.95)
            {
                Debug.DrawLine(highLOD.transform.TransformPoint(highLOD.sharedMesh.vertices[boundary[i].v1]), highLOD.transform.TransformPoint(highLOD.sharedMesh.vertices[boundary[i].v2]), Color.blue, 3);
                returnEdges.Add(boundary[i]);
                continue;
            }

            if (side == "up" && highLOD.sharedMesh.vertices[boundary[i].v1].z >= 0.95 && highLOD.sharedMesh.vertices[boundary[i].v2].z >= 0.95)
            {
                Debug.DrawLine(highLOD.transform.TransformPoint(highLOD.sharedMesh.vertices[boundary[i].v1]), highLOD.transform.TransformPoint(highLOD.sharedMesh.vertices[boundary[i].v2]), Color.magenta, 3);
                returnEdges.Add(boundary[i]);
                continue;
            }
            if (side == "down" && highLOD.sharedMesh.vertices[boundary[i].v1].z <= -0.95 && highLOD.sharedMesh.vertices[boundary[i].v2].z <= -0.95)
            {
                Debug.DrawLine(highLOD.transform.TransformPoint(highLOD.sharedMesh.vertices[boundary[i].v1]), highLOD.transform.TransformPoint(highLOD.sharedMesh.vertices[boundary[i].v2]), Color.yellow, 3);
                returnEdges.Add(boundary[i]);
                continue;
            }
        }

        return returnEdges;
    }

    //This is where you use Job System
    public void Stitch (List<EdgeHelpers.Edge> A, List<EdgeHelpers.Edge> B, MeshFilter aMesh, MeshFilter bMesh)
    {
        Vector3[] aVerts = aMesh.mesh.vertices;
        Vector3[] bVerts = bMesh.mesh.vertices;

        for (int i = 0; i < A.Count; i++)
        {
            for (int c = 0; c < B.Count; c++)
            {
                Vector3 a1 = aMesh.transform.TransformPoint(aVerts[A[i].v1]);
                Vector3 a2 = aMesh.transform.TransformPoint(aVerts[A[i].v2]);

                Vector3 b1 = bMesh.transform.TransformPoint(bVerts[B[c].v1]);
                Vector3 b2 = bMesh.transform.TransformPoint(bVerts[B[c].v2]);

                if (Vector3.Distance(a1, b1) < TerrainManager.instance.terrainStitchDist)
                {
                    aVerts[A[i].v1] = aMesh.transform.InverseTransformPoint(b1);
  
                    continue;
                }

                if (Vector3.Distance(a1, b2) < TerrainManager.instance.terrainStitchDist)
                {
                    aVerts[A[i].v1] = aMesh.transform.InverseTransformPoint(b2);
                    
                    continue;
                }

                if (Vector3.Distance(a2, b1) < TerrainManager.instance.terrainStitchDist)
                {
                    aVerts[A[i].v2] = aMesh.transform.InverseTransformPoint(b1);
                   
                    continue;
                }

                if (Vector3.Distance(a2, b2) < TerrainManager.instance.terrainStitchDist)
                {
                    aVerts[A[i].v2] = aMesh.transform.InverseTransformPoint(b2);
                   
                    continue;
                }

            }
        }

        aMesh.mesh.vertices = aVerts;
        bMesh.mesh.vertices = bVerts;

        aMesh.mesh.RecalculateNormals();
        aMesh.mesh.RecalculateBounds();

        bMesh.mesh.RecalculateNormals();
        bMesh.mesh.RecalculateBounds();


        
    }
    //Combine edges -- 

}

public static class EdgeHelpers
{
    public struct Edge
    {
        public int v1;
        public int v2;
        public int triangleIndex;
        public Edge(int aV1, int aV2, int aIndex)
        {
            v1 = aV1;
            v2 = aV2;
            triangleIndex = aIndex;
        }
    }

    public static List<Edge> GetEdges(int[] aIndices)
    {
        List<Edge> result = new List<Edge>();
        for (int i = 0; i < aIndices.Length; i += 3)
        {
            int v1 = aIndices[i];
            int v2 = aIndices[i + 1];
            int v3 = aIndices[i + 2];
            result.Add(new Edge(v1, v2, i));
            result.Add(new Edge(v2, v3, i));
            result.Add(new Edge(v3, v1, i));
        }
        return result;
    }

    public static List<Edge> FindBoundary(this List<Edge> aEdges)
    {
        List<Edge> result = new List<Edge>(aEdges);
        for (int i = result.Count - 1; i > 0; i--)
        {
            for (int n = i - 1; n >= 0; n--)
            {
                if (result[i].v1 == result[n].v2 && result[i].v2 == result[n].v1)
                {
                    // shared edge so remove both
                    result.RemoveAt(i);
                    result.RemoveAt(n);
                    i--;
                    break;

                    
                }
            }
        }
        return result;
    }
    public static List<Edge> SortEdges(this List<Edge> aEdges)
    {
        List<Edge> result = new List<Edge>(aEdges);
        for (int i = 0; i < result.Count - 2; i++)
        {
            Edge E = result[i];
            for (int n = i + 1; n < result.Count; n++)
            {
                Edge a = result[n];
                if (E.v2 == a.v1)
                {
                    // in this case they are already in order so just continoue with the next one
                    if (n == i + 1)
                        break;
                    // if we found a match, swap them with the next one after "i"
                    result[n] = result[i + 1];
                    result[i + 1] = a;
                    break;
                }
            }
        }
        return result;
    }
}
