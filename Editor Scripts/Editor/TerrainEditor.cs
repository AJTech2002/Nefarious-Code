using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class TerrainEditor : EditorWindow
{
    [MenuItem("Terrain Tools/Terrain Editor")]
    public static void ShowExample()
    {
        TerrainEditor wnd = GetWindow<TerrainEditor>();
        wnd.titleContent = new GUIContent("TerrainEditor");
    }

    public TerrainReference terrainPoint;
    
    //-----------

    //If exists
    public bool selectedExists;
    public (int, int) coords;
    public TerrainReference selectedTerrain;

    public TerrainManager manager;

    public int xNeighbours, yNeighbours;
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        terrainPoint = (TerrainReference)EditorGUILayout.ObjectField(terrainPoint, typeof(TerrainReference), true);
        manager = (TerrainManager)EditorGUILayout.ObjectField(manager, typeof(TerrainManager), true);
        
        if (manager != null && terrainPoint != null)
        PrintGrid();

        if (GUILayout.Button("Split Scene")) {

            if (selectedTerrain != null && selectedTerrain.terrain != null)
            {
                System.Collections.Generic.List<EditorBuildSettingsScene> scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
                //Can just use copy from 
                foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                {
                    scenes.Add(s);
                }

                var originalScene = EditorSceneManager.GetActiveScene();
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                newScene.name = selectedTerrain.terrainID + "_Scene";

                Debug.Log("Saved (" + selectedTerrain.xTile + "," + selectedTerrain.yTile + ")" + ": " + newScene.path);
                Debug.Log(selectedTerrain.terrain.gameObject.name);
                string sT = selectedTerrain.terrainID;

                selectedTerrain.terrain.editing = false;
                selectedTerrain.terrain.resetOnStart = false;
                
                EditorSceneManager.MoveGameObjectToScene(selectedTerrain.terrain.gameObject, newScene);
                selectedTerrain.loaded = false;
                selectedTerrain.hasScene = true;
                EditorSceneManager.SaveScene(newScene, Application.dataPath+"/TerrainChunks/"+sT+"_Scene"+".unity");
                
                selectedExists = false;
                coords = (0, 0);
                selectedTerrain = null;

                Debug.Log(newScene.path);
                Debug.Log(Application.dataPath);
                Debug.Log("What i I did! : " + "Assets/TerrainChunks/" + sT + "_Scene" + ".unity");
                EditorSceneManager.SetActiveScene(originalScene);
                scenes.Add(new EditorBuildSettingsScene("Assets/TerrainChunks/"+ sT + "_Scene" + ".unity", true));
                EditorBuildSettings.scenes = scenes.ToArray();
                EditorSceneManager.MarkAllScenesDirty();
            }

            
        }
        if (GUILayout.Button("Create")) { 
        
            //Ensure that the terrain doesn't exist
            if (selectedExists) { Debug.LogError("This terrain already exists...");}
            else
            {
                //Create a terrain holder in the position of existance

                //Create a terrain generator at the terrain holder position using prefab 

                
                if (terrainPoint.name != "TerrainHolder (0,0)") { Debug.LogError("You can only create terrains from the origin point..."); return; }
                Object g = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/TerrainHolder (0,0).prefab", typeof(GameObject));
                GameObject terrainHolder = (GameObject)Instantiate(g, null, true);
                terrainHolder.name = "TerrainHolder (" + coords.Item1 + "," + coords.Item2 + ")";


                Object t = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/TerrainGenerator.prefab",typeof(GameObject));
                GameObject terrainObject = (GameObject)Instantiate(t, null, true);
                terrainObject.name = "TerrainGenerator (" + coords.Item1 + "," + coords.Item2 + ")";

                TerrainReference r = terrainHolder.GetComponent<TerrainReference>();
                r.loadedTerrain = terrainObject.GetComponent<NoiseGenerator>();
                r.xTile = -coords.Item1;
                r.yTile = coords.Item2;

                manager.id++;
                r.terrainID = manager.id.ToString();
                r.sceneIndex = manager.id;
                terrainObject.GetComponent<NoiseGenerator>().xTile = -coords.Item1;
                terrainObject.GetComponent<NoiseGenerator>().yTile = coords.Item2;
                

                ReinitialiseMinMax(terrainObject.GetComponent<NoiseGenerator>());

                int multiplierX = -r.xTile;
                int multiplierY = terrainPoint.yTile - r.yTile;

                Vector3 p = new Vector3(terrainPoint.transform.position.x + terrainPoint.terrain.max.x * 2 * multiplierX, terrainPoint.transform.position.y, terrainPoint.transform.position.z + terrainPoint.terrain.max.z * 2 * multiplierY );
                
                terrainHolder.transform.position = p;
                terrainObject.transform.position = p;

                selectedTerrain = r;
                EditorSceneManager.MarkAllScenesDirty();
            }
        }
        if (GUILayout.Button("Delete")) { }
        if (GUILayout.Button("Load/Unload")) {

            if (selectedTerrain != null)
            {
                if (!selectedTerrain.loaded)
                    selectedTerrain.Load();
                else
                    selectedTerrain.Unload();
            }
            EditorSceneManager.MarkAllScenesDirty();
        }

        if (GUILayout.Button("Clear Manager"))
        {
            manager.allTerrains.Clear();
            manager.terrainGrid.Clear();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Move Here"))
        {
            if (selectedTerrain != null)
            {

                terrainPoint = selectedTerrain;

            }

        }
        xNeighbours = EditorGUILayout.IntSlider(xNeighbours, 1, 8);
        yNeighbours = EditorGUILayout.IntSlider(yNeighbours, 1, 8);
        if (selectedTerrain != null && selectedTerrain.gameObject != null)
            EditorGUILayout.LabelField("Selected Field : " + selectedTerrain.gameObject.name);
        else
            EditorGUILayout.LabelField("None");

        if (GUILayout.Button("Edge Stitch"))
        {
            if (selectedTerrain != null)
            {
                selectedTerrain.EdgeCombine();
            }
        }
      
        EditorGUILayout.EndHorizontal();

        Repaint();
    }

    private void ReinitialiseMinMax(NoiseGenerator g)
    {
        g.SetupMinMax();
    }

    private void PrintGrid()
    {
        if (terrainPoint != null)
        {
            GUI.color = Color.magenta;
            string ID = "M";
            if (terrainPoint.terrain != null) ID = terrainPoint.terrainID;
            if (GUI.Button(r(width / 2, height / 2, 30, 30),ID))
            {
                selectedTerrain = terrainPoint;
                Selection.activeGameObject = selectedTerrain.gameObject;
                coords = (terrainPoint.xTile, terrainPoint.yTile);
                selectedExists = true;
                if (selectedTerrain.loaded)
                {
                    ReinitialiseMinMax(selectedTerrain.loadedTerrain);

                    selectedTerrain.loadedTerrain.terrainID = selectedTerrain.terrainID;
                }
            }
            
            //Neighbour view
            for (int x = -xNeighbours-1; x < xNeighbours+2; x++)
            {
                for (int y = -yNeighbours - 1; y < yNeighbours + 2; y++)
                {
                    if (x == 0 && y == 0) continue;
                    float multiplierX = 40 * x;
                    float multiplierY = 40 * y;

                    GUI.color = Color.white;
                    int neighX = -terrainPoint.xTile + x;
                    int neighY = terrainPoint.yTile + y;

                    bool exists = true;

                    if (manager.GetNeighbourNonStatic(neighX, neighY) == null && GameObject.Find("TerrainHolder (" + neighX + "," + neighY + ")") == null)
                    {
                        exists = false;
                        GUI.color = Color.yellow;
                    }
                    else
                    {
                        GUI.color = Color.cyan;
                        if (GameObject.Find("TerrainHolder (" + neighX + "," + neighY + ")") != null)
                        if (GameObject.Find("TerrainHolder (" + neighX + "," + neighY + ")").GetComponent<TerrainReference>().loaded)
                        {
                            GUI.color = Color.green;
                            if (GameObject.Find("TerrainHolder (" + neighX + "," + neighY + ")").GetComponent<TerrainReference>().loadedTerrain == null)
                            {
                                GameObject.Find("TerrainHolder (" + neighX + "," + neighY + ")").GetComponent<TerrainReference>().loaded = false;
                            }
                            else
                            {
                                if (GameObject.Find("TerrainHolder (" + neighX + "," + neighY + ")").GetComponent<TerrainReference>().loadedTerrain.editing)
                                {
                                    GUI.color = Color.red;
                                }
                            }
                        }
                    }
                    if (neighX == coords.Item1 && neighY == coords.Item2) GUI.color = Color.blue;

                    if (GUI.Button(r(width / 2 + multiplierX, height / 2 + multiplierY, 30, 30)," "))
                    {
                        coords = (neighX, neighY);
                        selectedExists = exists;
                        
                        if (GameObject.Find("TerrainHolder (" + neighX + "," + neighY + ")") != null)
                        selectedTerrain = GameObject.Find("TerrainHolder (" + neighX + "," + neighY + ")").GetComponent<TerrainReference>();

                        Selection.activeGameObject = selectedTerrain.gameObject;

                        if (exists)
                        {
                            selectedTerrain = GameObject.Find("TerrainHolder (" + neighX + "," + neighY + ")").GetComponent<TerrainReference>();
                            if (selectedTerrain.loadedTerrain != null)
                            {
                                selectedTerrain.loadedTerrain.SetupMinMax();
                                selectedTerrain.loadedTerrain.terrainID = selectedTerrain.terrainID;
                            }
                            Debug.Log("Found matching terrain.");
                            
                        }
                    }

                }
            }

        }

        GUI.color = Color.white;
    }

    #region Helpers

    private bool dropped;
    // private Vector2 dropVect;

    Rect r(float x, float y, float xS, float yS)
    {
        return new Rect(new Vector2(x, y), new Vector2(xS, yS));
    }

    Rect r(float x, float y, float xS, float yS, out Rect rectOut)
    {
        rectOut = new Rect(new Vector2(x, y), new Vector2(xS, yS));
        return new Rect(new Vector2(x, y), new Vector2(xS, yS));
    }

    Rect o(Rect r, float xOff, float yOff, float xS, float yS)
    {
        return new Rect(new Vector2(r.x + xOff, r.y + yOff), new Vector2(xS, yS));
    }

    Rect xo(Rect r, float xOff, float yOff, float xS, float yS)
    {
        return new Rect(new Vector2(r.x + xOff + r.size.x, r.y + yOff), new Vector2(xS, yS));
    }

    Rect yo(Rect r, float xOff, float yOff, float xS, float yS)
    {
        return new Rect(new Vector2(r.x + xOff, r.y + yOff + r.size.y), new Vector2(xS, yS));
    }

    Rect o(Rect r, float xOff, float yOff, float xS, float yS, out Rect rectOut)
    {
        rectOut = new Rect(new Vector2(r.x + xOff, r.y + yOff), new Vector2(xS, yS));
        return new Rect(new Vector2(r.x + xOff, r.y + yOff), new Vector2(xS, yS));
    }

    private void DrawRectBox(Rect rect, string text, out Rect r)
    {
        GUI.Box(rect, text);
        r = rect;
    }

    private void DrawRectBox(Rect rect, string text)
    {
        GUI.Box(rect, text);
    }

    private float width
    {
        get
        {
            return Screen.width;
        }
    }

    private float height
    {
        get
        {
            return Screen.height;
        }
    }

   
    #endregion

}
