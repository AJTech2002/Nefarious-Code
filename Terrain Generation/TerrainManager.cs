using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TerrainManager : MonoBehaviour
{
    public
        bool editingTerrain;
    public float terrainStitchDist = 0.1f;

    [Header("Brush Settings")]
    public Vector2 initialResolution;
    //Custom brushes are supported
    public Texture2D brush;
    public int brushScale = 2;
    public float intensity = 1f;

    [Header("NoiseProps")]
    public int size;
    public float scale;

    [Header("Height Params")]
    public float maxHeight;

    [Header("References")]
    public static TerrainManager instance;

    [Header("ID Increment")]
    public int id = 1;

    //Max dimensions
    public Dictionary<Point, TerrainReference> terrainGrid = new Dictionary<Point, TerrainReference>();
    [System.NonSerialized]
    public List<TerrainReference> allTerrains = new List<TerrainReference>();

   
    
    private void Awake()
    {
        instance = this;
        //Store reference
    }

    //This is used when the SceneManagement tool adds a new scene in - so it has to be registered.
    public static void LoadTerrain (TerrainReference terrain)
    {
       
        if (!instance.allTerrains.Contains(terrain))
        {
            if (instance.terrainGrid.ContainsKey(new Point(terrain.xTile, terrain.yTile)))
                instance.terrainGrid[new Point(terrain.xTile, terrain.yTile)] = terrain;
            else instance.terrainGrid.Add(new Point(terrain.xTile, terrain.yTile), terrain);
            instance.allTerrains.Add(terrain);
            return;
        }


    }

    private Queue<NoiseGenerator> noiseGenerators = new Queue<NoiseGenerator>();
    public void QueueChange (NoiseGenerator g)
    {
        if (!noiseGenerators.Contains(g))
             noiseGenerators.Enqueue(g);

        StartCoroutine("CheckQueue");

    }

    private IEnumerator CheckQueue ()
    {
        while (noiseGenerators.Count > 0)
        {
            noiseGenerators.Dequeue().MeshUpdates();
            yield return new WaitForEndOfFrame();
        }
    }

    public void RegularClearup()
    {

        for (int i = 0; i < allTerrains.Count; i++)
        {

            if (allTerrains[i].loaded)
                continue;

            if (!allTerrains[i].loaded)
            {
                Scene s = SceneManager.GetSceneByName(allTerrains[i].terrainID + "_Scene");

                if (s.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(s);
                }
            }
        }

    }

    public static void UnloadTerrain (TerrainReference terrain)
    {
        if (instance.allTerrains.Contains(terrain))
        {
            instance.allTerrains.Remove(terrain);
            instance.terrainGrid.Remove(new Point(terrain.xTile, terrain.yTile));
        }
    }
    public void NonStaticLoadTerrain(TerrainReference terrain)
    {
        if (!allTerrains.Contains(terrain))
        {
            if (terrainGrid.ContainsKey(new Point(terrain.xTile, terrain.yTile)))
                terrainGrid[new Point(terrain.xTile, terrain.yTile)] = terrain;
            else terrainGrid.Add(new Point(terrain.xTile, terrain.yTile), terrain);
            allTerrains.Add(terrain);
            return;
        }

        Debug.LogWarning("Trying to add a terrain that exists?");
    }
    public static TerrainReference GetNeighbour (int xAdd, int yAdd, TerrainReference cur)
    {
        if (instance.terrainGrid.ContainsKey(new Point(cur.xTile + xAdd, cur.yTile + yAdd)))
        {
            return instance.terrainGrid[new Point(cur.xTile + xAdd, cur.yTile + yAdd)];
        }
        return null;
    }

    public static TerrainReference GetNeighbour(int x, int y)
    {
        if (instance.terrainGrid.ContainsKey(new Point(x, y)))
        {
            return instance.terrainGrid[new Point(x,y)];
        }
        return null;
    }
    public TerrainReference GetNeighbourNonStatic(int x, int y)
    {
        if (terrainGrid.ContainsKey(new Point(x, y)))
        {
            return terrainGrid[new Point(x, y)];
        }
        return null;
    }
}

