using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Unity.Collections;
public class VertexPainter : EditorWindow
{
    [MenuItem("Terrain Tools/Vertex Painting")]
    public static void ShowExample()
    {
        VertexPainter wnd = GetWindow<VertexPainter>();
        wnd.titleContent = new GUIContent("VertexPainter");
    }

    private SerializedObject presetManagerSerialized;
    private ObjectField presetObjectField;
    Texture2D temporaryImage;
    public void OnEnable()
    {
    }

    bool editingVerts = true;
    NoiseGenerator editorTemporaryNoiseGenerator;
    private Color paintColor;
    private Texture2D paintBrush;
    private bool editingSingleTerrain;

    NoiseGenerator currentTerrain;
    NoiseGenerator addingTerrain;

    private bool r, l, u, d;
    private int tempXA, tempYA;
    public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
    {
        byte[] _bytes = _texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
        Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
    }


    private void OnGUI()
    {
        editingSingleTerrain = GUILayout.Toggle(editingSingleTerrain, "Edit Selected Terrain?");

        if (editingSingleTerrain)
        {
            GUILayout.Label("Currently editing verts ? : " + editingVerts);
            if (GUILayout.Button("Edit Verts"))
            {
                editingVerts = true;
            }
            if (GUILayout.Button("Edit Textures"))
            {
                editingVerts = false;
            }

            paintColor = EditorGUILayout.ColorField(paintColor);

            GUILayout.Label("Painting Settings");
            if (GUILayout.Button("Clear Texture"))
            {
                //Clear
            }

            GUILayout.Label("Saving and Loading");
            //Incomplete
            if (GUILayout.Button("Save Terrain"))
            {

                if (Selection.gameObjects[0].GetComponent<NoiseGenerator>() != null)
                {
                    #region Texture Saving Method (Obsolete)
                    /*
                    NoiseGenerator g = Selection.activeGameObject.GetComponent<NoiseGenerator>();
                    string path = Application.dataPath;
                    string noisePath = path + "/Noise_" +g.terrainID + ".xpng";
                    string texturePath = path + "/Texture_" + g.terrainID + ".xpng";

                    g.FixUpVals();

                    SaveTextureAsPNG(g.matTex, path + "/Noise_" + g.terrainID + ".png");
                    

                    /*
                    byte[] matTexRaw = g.matTex.GetRawTextureData();
                    byte[] textureRaw = g.uvTex.GetRawTextureData();

                    FileStream s = new FileStream(noisePath, FileMode.Create);
                    BinaryWriter w = new BinaryWriter(s);
                    w.Write(matTexRaw);
                    w.Close();

                    FileStream s2 = new FileStream(texturePath, FileMode.Create);
                    BinaryWriter w2 = new BinaryWriter(s2);
                    w2.Write(textureRaw);
                    w2.Close();
                    Debug.Log("Saved!");*/
                    #endregion

                    for (int i = 0; i < Selection.gameObjects.Length; i++)
                    {
                        NoiseGenerator g = Selection.gameObjects[i].GetComponent<NoiseGenerator>();
                        MeshFilter low = g.lowLOD;
                        MeshFilter mid = g.midLOD;
                        MeshFilter high = g.highLOD;

                        string path = "Assets/TerrainChunks/MeshData/" + g.terrainID + "_MeshLODS_";

                        SaveTextureAsPNG(g.uvTex, path + "Texture_" + g.terrainID + ".png");
                        Mesh m = AssetDatabase.LoadAssetAtPath(path+"LOW.mesh", typeof(Mesh)) as Mesh;
                        if (m == null) {

                            AssetDatabase.CreateAsset(low.mesh, path + "LOW.mesh");
                            AssetDatabase.CreateAsset(mid.mesh, path + "MID.mesh");
                            AssetDatabase.CreateAsset(high.mesh, path + "HI.mesh");
                            AssetDatabase.CreateAsset(g.highRenderer.material, path + "MATERIAL.mat");


                            AssetDatabase.SaveAssets();

                        }
                        else { }


                       

                    }
                }

            }

            if (GUILayout.Button("Load Terrain"))
            {
                if (Selection.gameObjects[0].GetComponent<NoiseGenerator>() != null)
                {
                    for (int i = 0; i < Selection.gameObjects.Length; i++)
                    {
                        NoiseGenerator g = Selection.gameObjects[i].GetComponent<NoiseGenerator>();

                        /*Debug.Log("Selection : " + Selection.activeGameObject.name + " THEREFORE : " + "/Noise_" + g.terrainID + ".xpng");
                       
                        string noisePath = path + "/Noise_" + g.terrainID + ".png";
                        

                        byte[] matTexRaw;
                       

                        matTexRaw = File.ReadAllBytes(noisePath);

                        

                        Texture2D matTexG = new Texture2D(g.size, g.size);
                        matTexG.LoadImage(matTexRaw);

                        

                        g.RegisterEditorAccess();
                        g.matTex = matTexG;
                        g.uvTex = texG;
                        g.RegisterPermTextureChange();

                        float[] vals2 = g.SetupValsT();


                        g.RegsiterPermChange(vals2);

                        Debug.Log("Loaded!");*/
                        string path = "Assets/TerrainChunks/MeshData/" + g.terrainID + "_MeshLODS_";
                        
                        string texturePath = path + "Texture_" + g.terrainID + ".png";
                        byte[] textureRaw;
                        textureRaw = File.ReadAllBytes(texturePath);
                        Texture2D texG = new Texture2D(g.size, g.size);
                        texG.LoadImage(textureRaw);

                        g.uvTex = texG;
                        g.lowLOD.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path + "LOW.mesh");
                        g.midLOD.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path + "MID.mesh");
                        g.highLOD.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path + "HI.mesh");
                        Material m = AssetDatabase.LoadAssetAtPath<Material>(path + "MATERIAL.mat"); 
                        m.mainTexture = texG;
                        g.highRenderer.sharedMaterial = m;
                        g.highRenderer.sharedMaterial.mainTexture = texG;
                        g.midLOD.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = texG;
                        g.lowLOD.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = texG;
                        g.highLOD.GetComponent<MeshCollider>().sharedMesh = g.highLOD.sharedMesh;

                        g.editing = false;
                        g.resetOnStart = false;

                        EditorUtility.SetDirty(m);


                        AssetDatabase.SaveAssets();

                    }
                }
                //Set to all - once
                //set dirty?
            }

         

            GUILayout.BeginHorizontal();


            EditorGUILayout.IntField(tempXA);
            EditorGUILayout.IntField(tempYA);

            GUILayout.EndHorizontal();
        }
        else
        {
            
            //Neighbour adding & management

            if (currentTerrain == null || addingTerrain == null)
            { }
            else
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Add Up")) AddTerrain(0, 1);
                if (GUILayout.Button("Add Right")) AddTerrain(1, 0);
                if (GUILayout.Button("Add Left")) AddTerrain(-1, 0);
                if (GUILayout.Button("Add Down")) AddTerrain(0, -1);

                GUILayout.EndHorizontal();
            }

        }

        Repaint();
    }
    float scale = 1;
    //Vertex Painting Code?

    private void AddTerrain (int xAdd, int yAdd)
    {
        //Instance to duplicate

        //When creating new terrain make sure to replace min, max?
    }

    private Texture2D Conv(Texture2D tex, int newWidth, int newHeight)
    {
        Texture2D result = new Texture2D(tex.width, tex.height, tex.format, false);
        float incX = (1.0f / (float)newWidth);
        float incY = (1.0f / (float)newHeight);
        for (int i = 0; i < result.height; ++i)
        {
            for (int j = 0; j < result.width; ++j)
            {
                Color newColor = tex.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
                result.SetPixel(j, i, newColor);
            }
        }
        return result;
    }
    private void Update()
    {
        if (!Application.isPlaying || !editingSingleTerrain)
            return;

        Vector3 mP = Input.mousePosition;

        Ray ray = Camera.main.ScreenPointToRay(mP);
        RaycastHit hit;

        Vector3 centerPoint = Vector3.zero;

        //This code manages finding the points that you have hovered over when the game is playing
        if (Physics.Raycast(ray, out hit))
        {

            editorTemporaryNoiseGenerator = hit.transform.root.root.GetComponent<NoiseGenerator>();

            Vector3[] vertices = hit.transform.GetComponent<MeshFilter>().sharedMesh.vertices;
            int[] triangles = hit.transform.GetComponent<MeshFilter>().sharedMesh.triangles;

            Vector3 p0 = vertices[triangles[hit.triangleIndex * 3 + 0]];
            Vector3 p1 = vertices[triangles[hit.triangleIndex * 3 + 1]];
            Vector3 p2 = vertices[triangles[hit.triangleIndex * 3 + 2]];
            Transform hitTransform = hit.collider.transform;
            p0 = hitTransform.TransformPoint(p0);
            p1 = hitTransform.TransformPoint(p1);
            p2 = hitTransform.TransformPoint(p2);

            Debug.DrawLine(p0, p1, Color.red, 0.2f);
            Debug.DrawLine(p1, p2, Color.red, 0.2f);
            Debug.DrawLine(p2, p0, Color.red, 0.2f);

            //Find the average point on the mesh where your mouse is
            centerPoint = (p0 + p1 + p2) / 3;
            Debug.DrawLine(centerPoint, centerPoint + Vector3.up * 2, Color.green, 0.2f);

        }
        else
            editorTemporaryNoiseGenerator = null;

        if (editorTemporaryNoiseGenerator == null)
            return;
       
        Texture2D brush = new Texture2D(0, 0);

        editorTemporaryNoiseGenerator.brushScale = Mathf.Clamp(editorTemporaryNoiseGenerator.brushScale+Mathf.RoundToInt(Input.GetAxis("Mouse ScrollWheel")),1, 6);
       
        float intensity2 = 0f;
        if (editorTemporaryNoiseGenerator != null && editorTemporaryNoiseGenerator.brush != null)
        {

            //Intensity of the brsuh
            intensity2 = editorTemporaryNoiseGenerator.intensity;

            if (scale != editorTemporaryNoiseGenerator.brushScale)
            {
                //Supporting the scaling of the brush (settings can be changed in NoiseGenerator.cs)
                TextureScale.Bilinear(editorTemporaryNoiseGenerator.brush, Mathf.RoundToInt(editorTemporaryNoiseGenerator.initialResolution.x * editorTemporaryNoiseGenerator.brushScale), Mathf.RoundToInt(editorTemporaryNoiseGenerator.initialResolution.y * editorTemporaryNoiseGenerator.brushScale));
            }

            //Setting the current brush to the noise generator's brush (can change per terrain)
            brush = editorTemporaryNoiseGenerator.brush;
            scale = editorTemporaryNoiseGenerator.brushScale;
            
        }

        if (editorTemporaryNoiseGenerator != null)
         (tempXA, tempYA) = editorTemporaryNoiseGenerator.FindUVTextureIndex(centerPoint);
       
        if (Input.GetMouseButton(0))
        {
            if (editingVerts)
            {
                NoiseGenerator g = editorTemporaryNoiseGenerator;
                if (g == null)
                    return;

                (int a, int b, float[] vals) = g.FindTextureIndex(centerPoint);
                
                int size = g.size;

                int xtemp = 0;
                List<NoiseGenerator> tempTerrains = new List<NoiseGenerator>(4);
                for (int x = a - (brush.width) / 2; x < a + (brush.width) / 2; x++)
                {

                    int ytemp = 0;
                    float overallInt = 1f;
                    for (int y = b - (brush.height) / 2; y < b + (brush.height) / 2; y++)
                    {

                        float brushAlpha = brush.GetPixel(xtemp, ytemp).a;
                        u = false;

                        bool smoothing = false;
                        int mult = 1;
                        if (Input.GetKey(KeyCode.LeftShift))
                            mult = -1;

                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            smoothing = true;
                        }

                        float ar = 0;

                        if (x > g.size - 1 || y > g.size - 1 || x < 0 || y < 0)
                        {
                            
                            //x = y 
                            //- = +

                            if (x > g.size - 1)
                            {
                               
                                if (y > g.size - 1)
                                {
                                    NoiseGenerator g2 = null;
                                    TerrainReference rg2 = TerrainManager.GetNeighbour(g.xTile - 1, g.yTile - 1);
                                    if (rg2 != null)
                                        g2 = rg2.terrain;

                                    if (g2 != null)
                                    {
                                        int excessX = x - (g2.size);
                                        int excessY = y - (g2.size);
                                        //Here and then it's opposite for them?
                                        //Convert the intensity to the local coordinates of the texture (rgba).

                                        float vr = 0f;
                                        float intensity =  brushAlpha * intensity2 * mult;
                                        try
                                        {
                                                vr = Mathf.Clamp01(g2.vals[(excessX) * size + excessY] + (0.03f * intensity));
                                                g2.vals[(excessX) * size + excessY] = vr;
                                               // g2.matTex.SetPixel((excessX), excessY, new Color(ar, ar, ar));
                                                if (!tempTerrains.Contains(g2)) { tempTerrains.Add(g2); }
                                            
                                        }
                                        catch (System.Exception) { }
                                    }
                                }

                                else if (y < 0)
                                {

                                    NoiseGenerator g2 = null;
                                    TerrainReference rg2 = TerrainManager.GetNeighbour(g.xTile + 1, g.yTile - 1);
                                    if (rg2 != null)
                                        g2 = rg2.terrain;
                                    if (g2 != null)
                                    {
                                        
                                        int excessX = x - (g2.size);
                                        int excessY = Mathf.Abs(y);
                                        //Here and then it's opposite for them?
                                        //Convert the intensity to the local coordinates of the texture (rgba).

                                        float vr = 0f;
                                        float intensity = brushAlpha * intensity2 * mult;
                                        try
                                        {
                                                vr = Mathf.Clamp01(g2.vals[(excessX) * size + (g2.size - excessY)] + (0.03f * intensity));
                                                g2.vals[(excessX) * size + (g2.size - excessY)] = vr;
                                               // g2.matTex.SetPixel((excessX), (g2.size - excessY), new Color(ar, ar, ar));
                                                if (!tempTerrains.Contains(g2)) { tempTerrains.Add(g2); }
                                            
                                        }
                                        catch (System.Exception) { }
                                    }
                                }

                            }

                            if (x < 0)
                            {
                                if (y > g.size - 1)
                                {
                                    NoiseGenerator g2 = null;
                                    TerrainReference rg2 = TerrainManager.GetNeighbour(g.xTile - 1, g.yTile + 1);
                                    if (rg2 != null)
                                        g2 = rg2.terrain;
                                    if (g2 != null)
                                    {
                                        int excessX = Mathf.Abs(x);
                                        int excessY = y - (g2.size);
                                        //Here and then it's opposite for them?
                                        //Convert the intensity to the local coordinates of the texture (rgba).

                                        float vr = 0f;
                                        float intensity = brushAlpha * intensity2 * mult;
                                        try
                                        {
                                            vr = Mathf.Clamp01(g2.vals[(g.size - excessX) * size + excessY] + (0.03f * intensity));
                                            g2.vals[(g.size - excessX) * size + excessY] = vr;
                                           // g2.matTex.SetPixel((g.size - excessX), excessY, new Color(ar, ar, ar));
                                            if (!tempTerrains.Contains(g2)) { tempTerrains.Add(g2); }
                                            
                                        }
                                        catch (System.Exception) { }
                                    }
                                }

                                else if (y < 0)
                                {
                                    NoiseGenerator g2 = null;
                                    TerrainReference rg2 = TerrainManager.GetNeighbour(g.xTile + 1, g.yTile + 1);
                                    if (rg2 != null)
                                        g2 = rg2.terrain;
                                    if (g2 != null)
                                    {
                                        int excessX = Mathf.Abs(x);
                                        int excessY = Mathf.Abs(y);
                                        //Here and then it's opposite for them?
                                        //Convert the intensity to the local coordinates of the texture (rgba).

                                        float vr = 0f;
                                        float intensity = brushAlpha * intensity2 * mult;
                                        try
                                        {
                                            vr = Mathf.Clamp01(g2.vals[(g.size - excessX) * size + (g2.size - excessY)] + (0.03f * intensity));
                                            g2.vals[(g.size - excessX) * size + (g2.size - excessY)] = vr;
                                           // g2.matTex.SetPixel((g.size - excessX), (g2.size - excessY), new Color(ar, ar, ar));
                                            if (!tempTerrains.Contains(g2)) { tempTerrains.Add(g2); }
                                            
                                        }
                                        catch (System.Exception) { }
                                    }
                                }

                            }

                            if (x > g.size-1)
                            {
                                int place = g.yTile - 1;
                                NoiseGenerator g2 = null;
                                TerrainReference rg2 = TerrainManager.GetNeighbour(g.xTile, place);
                                if (rg2 != null)
                                    g2 = rg2.terrain;
                                if (g2 != null)
                                {
                                    int excessX = x - (g2.size);
                                    //Here and then it's opposite for them?
                                    //Convert the intensity to the local coordinates of the texture (rgba).

                                    float vr = 0f;
                                    float intensity = brushAlpha * intensity2 * mult;
                                    try
                                    {
                                        if (y >= 0 && y < size)
                                        {
                                            vr = Mathf.Clamp01(g2.vals[(excessX) * size + y] + (0.03f * intensity));
                                            g2.vals[(excessX) * size + y] = vr;
                                            //g2.matTex.SetPixel((excessX), y, new Color(ar, ar, ar));
                                            if (!tempTerrains.Contains(g2)) { tempTerrains.Add(g2); }
                                        }
                                    }
                                    catch (System.Exception) { }
                                }

                            }

                            //This wont work for all y vals
                            if (x < 0)
                            {
                                int place = g.yTile + 1;
                                NoiseGenerator g2 = null;
                                TerrainReference rg2 = TerrainManager.GetNeighbour(g.xTile,place);
                                if (rg2 != null)
                                    g2 = rg2.terrain;
                                if (g2 != null)
                                {
                                    int excessX = Mathf.Abs(x);
                                    //Here and then it's opposite for them?
                                    //Convert the intensity to the local coordinates of the texture (rgba).

                                    float vr = 0f;
                                    float intensity = brushAlpha * intensity2 * mult;
                                    try
                                    {
                                        if (y >= 0 && y < size)
                                        {
                                            vr = Mathf.Clamp01(g2.vals[(g.size - excessX) * size + y] + (0.03f * intensity));
                                            g2.vals[(g.size - excessX) * size + y] = vr;
                                            //g2.matTex.SetPixel((g.size - excessX), y, new Color(ar, ar, ar));
                                            if (!tempTerrains.Contains(g2)) { tempTerrains.Add(g2); }
                                        }
                                    }
                                    catch (System.Exception) { }
                                }
                            }

                            if (y > g.size - 1)
                            {
                                int place = g.xTile - 1;
                                NoiseGenerator g2 = null;
                                TerrainReference rg2 = TerrainManager.GetNeighbour(place, g.yTile);
                                if (rg2 != null)
                                    g2 = rg2.terrain;
                                if (g2 != null)
                                {
                                    
                                    int excessY = y - (g2.size);
                                    //Here and then it's opposite for them?
                                    //Convert the intensity to the local coordinates of the texture (rgba).

                                    float vr = 0f;
                                    float intensity = brushAlpha * intensity2 * mult;
                                    try
                                    {
                                       // if (y >= 0 && y < size)
                                        //{
                                        vr = Mathf.Clamp01(g2.vals[(x) * size + excessY] + (0.03f * intensity));
                                        g2.vals[(x) * size + excessY] = vr;
                                        //g2.matTex.SetPixel((x), excessY, new Color(ar, ar, ar));
                                        if (!tempTerrains.Contains(g2)) { tempTerrains.Add(g2); }
                                       // }
                                    }
                                    catch (System.Exception) { }
                                }

                            }

                            //This wont work for all y vals
                            if (y < 0)
                            {
                                int place = g.xTile + 1;
                                NoiseGenerator g2 = null;
                                TerrainReference rg2 = TerrainManager.GetNeighbour(place, g.yTile);
                                if (rg2 != null)
                                    g2 = rg2.terrain;
                                if (g2 != null)
                                {
                                    int excessY = Mathf.Abs(y);
                                    //Here and then it's opposite for them?
                                    //Convert the intensity to the local coordinates of the texture (rgba).

                                    float vr = 0f;
                                    float intensity = brushAlpha * intensity2 * mult;
                                    try
                                    {
                                        // if (y >= 0 && y < size)
                                        //{(g.size - excessX)
                                        vr = Mathf.Clamp01(g2.vals[(x) * size + (g2.size - excessY)] + (0.03f * intensity));
                                        g2.vals[(x) * size + (g2.size - excessY)] = vr;
                                        //g2.matTex.SetPixel((x), (g2.size - excessY), new Color(ar, ar, ar));
                                        if (!tempTerrains.Contains(g2)) { tempTerrains.Add(g2); }
                                        // }
                                    }
                                    catch (System.Exception) { }
                                }
                            }

                        }

                        if (!smoothing)
                        {
                            //Convert the intensity to the local coordinates of the texture (rgba).
                            float intensity = brushAlpha * intensity2 * mult;
                            try
                            {
                                if (y >= 0 && y < size && x >= 0 && x < size)
                                ar = Mathf.Clamp01(vals[Mathf.Clamp(x,0,size-1) * size + y] + (0.03f * intensity));
                            }
                            catch (System.Exception) { }
                        }
                        else
                        {
                           /* float intensity = brushAlpha * intensity2 * mult;
                            ar = Mathf.Clamp01(Mathf.Lerp(vals[Mathf.Clamp(x, 0, size - 1) * size + Mathf.Clamp(y, 0, size - 1)], 0, 0.2f * intensity)) * overallInt;*/
                        }

                        //g.matTex.SetPixel(Mathf.Clamp(x,0, size - 1), Mathf.Clamp(y, 0, size - 1), new Color(ar, ar, ar));
                        try
                        {
                            if (y >= 0 && y < size && x >= 0 && x < size)
                            {
                                vals[Mathf.Clamp(x, 0, size - 1) * size + y] = ar;
                                //g.matTex.SetPixel(Mathf.Clamp(x, 0, size - 1), y, new Color(ar, ar, ar));
                            }
                        }
                        catch (System.Exception e) { }
                        ytemp++;
                    }

                    xtemp++;
                }

                g.vals = vals;
               
                g.RegsiterChange();

                foreach (NoiseGenerator g2 in tempTerrains)
                {
                    g2.RegsiterChange();
                }

            }
            else
            {
                NoiseGenerator g = editorTemporaryNoiseGenerator;
                if (g == null)
                    return;
                (int a, int b) = g.FindUVTextureIndex(centerPoint);

                

                int size = g.size;

                int xtemp = 0;
                for (int x = a - (brush.width) / 2; x < a + (brush.width) / 2; x++)
                {

                    int ytemp = 0;
                    for (int y = b - (brush.height) / 2; y < b + (brush.height) / 2; y++)
                    {
                        float brushAlpha = brush.GetPixel(xtemp, ytemp).a;
                        bool smoothing = false;
                        int mult = 1;
                        if (Input.GetKey(KeyCode.LeftShift))
                            mult = -1;

                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            smoothing = true;
                        }

                        Color ar = new Color(1, 1, 1);


                        if (!smoothing)
                        {
                            Color originalColor = g.uvTex.GetPixel(g.size-1 - y,  x);

                            //Convert the intensity to the local coordinates of the texture (rgba).
                            float intensity = intensity2 * mult;

                            ar = Color.Lerp(originalColor,paintColor, brushAlpha* intensity2);
                            
                           
                        }
                        else
                        {
                            Color originalColor = g.uvTex.GetPixel(g.size - y, x);

                            //Convert the intensity to the local coordinates of the texture (rgba).
                            float intensity = intensity2 * mult;

                            ar = Color.Lerp(originalColor, paintColor, brushAlpha * intensity2 * paintColor.a);

                        }

                        

                         g.uvTex.SetPixel(Mathf.Clamp(g.size-1 - y,-1,g.size+1), Mathf.Clamp(x, -1, g.size+1), ar);
                        //vals[x * size + y] = ar;

                        ytemp++;
                    }

                    xtemp++;
                }

                g.uvTex.Apply();
                //g.vals = vals;
                //g.matTex.Apply();

                //Registering the change in the texture -> updates the mesh
                g.RegisterTextureChange();
            }
        }
    }
}