using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

public class SubsceneManager : EditorWindow
{
    [MenuItem("Terrain Tools/SubsceneManager")]
    public static void ShowExample()
    {
        SubsceneManager wnd = GetWindow<SubsceneManager>();
        wnd.titleContent = new GUIContent("SubsceneManager");
    }

    public void OnEnable()
    {
       
    }

    private TerrainReference selectedTerrain;
    private NoiseGenerator selectedNoise;
    private SceneHandler selectedHandler;
    private TerrainManager manager;


    private List<GameObject> selectedObjects = new List<GameObject>();
    private void OnGUI()
    {

        GUILayout.BeginHorizontal();
        if (GameObject.FindObjectOfType<TerrainManager>() != null)
            manager = GameObject.FindObjectOfType<TerrainManager>();
        manager = (TerrainManager) EditorGUILayout.ObjectField(manager, typeof(TerrainManager));
       
        if (Selection.activeTransform != null && Selection.activeTransform.GetComponent<SceneHandler>() != null)
        {
            
            NoiseGenerator g = Selection.activeGameObject.transform.GetComponent<NoiseGenerator>();
            selectedNoise = g;
            if (g == null)
            {
                Debug.LogError("You can't have a SceneHandler without a NoiseGenerator");
            }

            if (GameObject.Find("TerrainHolder (" + g.xTile + "," + g.yTile + ")") != null)
            selectedTerrain = GameObject.Find("TerrainHolder (" + g.xTile + "," + g.yTile + ")").GetComponent<TerrainReference>();
                selectedHandler = Selection.activeGameObject.transform.GetComponent<SceneHandler>();
            
            if (selectedTerrain != null)
                GUILayout.Label("Selected : " + selectedTerrain.terrainID + " at " + " X : " + selectedTerrain.xTile + " Y : " + selectedTerrain.yTile);
            else
                GUILayout.Label("Selected : " + g.terrainID + " at " + " X : " + g.xTile + " Y : " + g.yTile);

        }
        else if (Selection.activeTransform != null && Selection.activeGameObject.transform.GetComponent<TerrainReference>() != null)
        {
            selectedTerrain = Selection.activeGameObject.transform.GetComponent<TerrainReference>();
            if (selectedTerrain.loaded)
            {
                selectedHandler = selectedTerrain.loadedTerrain.GetComponent<SceneHandler>();

                GUILayout.Label("Selected : " + selectedTerrain.terrainID + " at " + " X : " + selectedTerrain.xTile + " Y : " + selectedTerrain.yTile);
            }
            else
            {
                GUILayout.Label("You haven't loaded in the scene yet, so you can't really use this yet.");
            }
        }
        /*else
        {
            selectedTerrain = (TerrainReference) EditorGUILayout.ObjectField(selectedTerrain, typeof(TerrainReference));
            if (selectedTerrain.loaded)
            {
                selectedHandler = selectedTerrain.loadedTerrain.GetComponent<SceneHandler>();

                GUILayout.Label("Selected : " + selectedTerrain.terrainID + " at " + " X : " + selectedTerrain.xTile + " Y : " + selectedTerrain.yTile);
            }
            else
            {
                GUILayout.Label("You haven't loaded in the scene yet, so you can't really use this yet.");
            }
        } */

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (selectedTerrain != null)
            GUILayout.Label("Selected : " + selectedTerrain.terrainID + " at " + " X : " + selectedTerrain.xTile + " Y : " + selectedTerrain.yTile);
        else
            GUILayout.Label("Selected : " + selectedNoise.terrainID + " at " + " X : " + selectedNoise.xTile + " Y : " + selectedNoise.yTile);

        if (GUILayout.Button("Load/Unload"))
        {
            selectedHandler.ToggleLoad(editingTag, true);
        }

        if (GUILayout.Button("Add Selected to Tag"))
        {
            selectedHandler.LoadInObjects(new List<GameObject>(Selection.gameObjects),editingTag,true);
        }

        GUILayout.EndHorizontal();
        //Selecting objects in scene

        //Applying tag (create if one doesn't exist) + add to those objects
        //Aplying tag-type
        //APplying priority
        //Add to sub-stream manager [two lists for serialization]
        GUILayout.BeginHorizontal();
        GUILayout.Label("Scene Tag : ");
        editingTag = GUILayout.TextField(editingTag);
        EditorGUILayout.Separator();
        GUILayout.EndHorizontal();
        //Save tag (Load the scene, add, save)
        

        EditorGUILayout.BeginVertical();
        GUILayout.Label("Create new Sub-Scene : ");
        EditorGUILayout.BeginHorizontal(); GUILayout.Label("Tag : "); _sceneTagName = GUILayout.TextField(_sceneTagName); EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal(); GUILayout.Label("Scene Type : ");  _type = (SceneType)EditorGUILayout.EnumPopup(_type); EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Create"))
        {
            if (selectedTerrain != null)
                selectedHandler.CreateScene(new SubsceneTag(_sceneTagName, sceneBuildID, _type, selectedTerrain.terrainID),selectedTerrain.terrainID);
            else
                selectedHandler.CreateScene(new SubsceneTag(_sceneTagName, sceneBuildID, _type, selectedNoise.terrainID), selectedNoise.terrainID);
        }
        EditorGUILayout.EndVertical();
        //----------------

        //Loop through tags allow to load / unload + change the settings (type etc.)
        Repaint();
    }

    private string editingTag;

    private string _sceneTagName;

    private string _parentID;
    private int sceneBuildID
    {
        get
        {
            
            return 0;
        }
    }

    private SceneType _type;
}