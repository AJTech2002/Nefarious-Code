using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Threading.Tasks;

public class NoiseGenerator : MonoBehaviour
{
    #region Variables
    [Header("Editor:")]
    public bool overrideEditing = false;
    public bool editing = false;
    public bool resetOnStart = false;

    
    
    public int xTile, yTile;


    //https://www.youtube.com/watch?v=TZFv493D7jo
    public Transform highTerrainTransform;
    

    //LODS
    public MeshFilter highLOD;
    [Header("Lower LODS")]
    public MeshFilter midLOD;
    public MeshFilter lowLOD;

    private TerrainManager inManager;
    private TerrainManager foundTerrainManager
    {
        get
        {
            if (inManager != null) return inManager;

            if (TerrainManager.instance != null)
            {
                inManager = TerrainManager.instance;
                return inManager;
            }

            if (GameObject.FindObjectOfType<TerrainManager>() != null)
            {
                inManager = GameObject.FindObjectOfType<TerrainManager>();
                return inManager;
            }

            return inManager;
        }

        set
        {
            inManager = value;
        }
    }

    public string terrainID;

    public int size
    {
        get
        {
            if (foundTerrainManager == null)
            { foundTerrainManager = TerrainManager.instance; }


            return foundTerrainManager.size;
        }
    }

    public float scale
    {
        get
        {
            if (foundTerrainManager == null)
            { foundTerrainManager = TerrainManager.instance; }


            return foundTerrainManager.scale;
        }
    }

    public float maxHeight
    {
        get
        {
            if (foundTerrainManager == null)
            { foundTerrainManager = TerrainManager.instance; }
            return foundTerrainManager.maxHeight;
        }
    }

    public float intensity
    {
        get
        {
            if (foundTerrainManager == null)
            { foundTerrainManager = TerrainManager.instance; }
            return foundTerrainManager.intensity;
        }
    }

    public int brushScale
    {
        get
        {
            if (foundTerrainManager == null)
            { foundTerrainManager = TerrainManager.instance; }
            return foundTerrainManager.brushScale;
        } set { foundTerrainManager.brushScale = value; }
    }

    public Texture2D brush
    {
        get { return foundTerrainManager.brush; }
    }

    public Vector2 initialResolution
    {
        get { return foundTerrainManager.initialResolution; }
    }


    [Header("Noise Texture Settings")]
    public Vector3 max;
    public Vector3 min;

    private Vector3 bounds;
    private Vector2 offset;
    private int blendWidth;
    public MeshRenderer highRenderer;
    private MeshRenderer midRenderer;
    private MeshRenderer lowRenderer;
    private bool up, down, left, right;
    private int edgeFadeRadius;
    private int neighbourBlend = 3;


    [HideInInspector]
    public float[] vals;

    [HideInInspector]
    public List<Point> points;

    public Texture2D uvTex;
    public Texture2D matTex;
    #endregion

    #region Initial Setups
    public void FixUpVals()
    {

        for (int x = 0; x < size; x++)
        {

            for (int y = 0; y < size; y++)
            {
                float c = vals[x * size + y];
                matTex.SetPixel(x, y, new Color(c,c,c));

            }

        }

        matTex.Apply();

    }

    public void SetupVals()
    {

        for (int x = 0; x < size; x++)
        {

            for (int y = 0; y < size; y++)
            {
                float c = matTex.GetPixel(x,y).r;
                vals[x * size + y] = c;
            }

        }
    }

    public float[] SetupValsT()
    {
        float[] vals = new float[size * size];
        for (int x = 0; x < size; x++)
        {

            for (int y = 0; y < size; y++)
            {
                float c = matTex.GetPixel(x, y).r;
                vals[x * size + y] = c;
            }

        }

        return vals;
    }

    //Something is wrong with this..
    public void SetupMinMax()
    {
        min = Vector3.zero;
        max = Vector3.zero;
        Vector3[] vs = lowLOD.sharedMesh.vertices;
        for (int i = 0; i < vs.Length; i++)
        {
            Vector3 v = (vs[i]);
            if (v.x < min.x) min.x = v.x;
            if (v.x > max.x) max.x = v.x;

            if (v.z < min.z) min.z = v.z;
            if (v.z > max.z) max.z = v.z;

        }
        min = lowLOD.transform.TransformPoint(min);
        max = lowLOD.transform.TransformPoint(max);
        Debug.DrawRay(min, Vector3.up * 20, Color.red, 2);
        Debug.DrawRay(max, Vector3.up * 20, Color.green, 2);
    }

    private void Start()
    {
        if (overrideEditing)
        {
            highLOD.mesh.MarkDynamic();
            midLOD.mesh.MarkDynamic();
            lowLOD.mesh.MarkDynamic();
        }
    }
    public void SetupTerrain()
    {
        if (overrideEditing)
        {
            //Should be done by the editor.
            foundTerrainManager = TerrainManager.instance;

            if (editing)
            {
                min = Vector3.zero;
                max = Vector3.zero;
                Vector3[] vs = lowLOD.mesh.vertices;
                for (int i = 0; i < vs.Length; i++)
                {
                    Vector3 v = (vs[i]);
                    if (v.x < min.x) min.x = v.x;
                    if (v.x > max.x) max.x = v.x;

                    if (v.z < min.z) min.z = v.z;
                    if (v.z > max.z) max.z = v.z;

                }
                min = lowLOD.transform.TransformPoint(min);
                max = lowLOD.transform.TransformPoint(max);
                Debug.DrawRay(min, Vector3.up * 20, Color.red, 100);
                Debug.DrawRay(max, Vector3.up * 20, Color.magenta, 100);

                highRenderer = highTerrainTransform.GetComponent<MeshRenderer>();
                midRenderer = midLOD.GetComponent<MeshRenderer>();
                lowRenderer = lowLOD.GetComponent<MeshRenderer>();
                highLOD = highTerrainTransform.GetComponent<MeshFilter>();

                

                if (resetOnStart)
                {
                    matTex = new Texture2D(size, size);


                    uvTex = new Texture2D(size, size);

                    //rendererA.material.mainTexture = matTex;
                    vals = new float[size * size];
                    FixUpVals();
                    //rendererA.material.mainTexture = matTex;
                    RegsiterChange();
                    RegisterTextureChange();
                }


            }
        }

    }
    #endregion

    #region Permanent and Temporary Change Registering 
    public void RegsiterChange()
    {
        
        //In case editor calls in Edit Mode
        if (highRenderer == null)
        {
            highRenderer = highTerrainTransform.GetComponent<MeshRenderer>();
            midRenderer = midLOD.GetComponent<MeshRenderer>();
            lowRenderer = lowLOD.GetComponent<MeshRenderer>();
            highLOD = highTerrainTransform.GetComponent<MeshFilter>();
        }

        //FixUpVals();
        MeshUpdates();
    }

    public void MeshUpdates()
    {
        UpdateMesh(highLOD);
        UpdateMesh(midLOD);
        UpdateMesh(lowLOD);
        highLOD.transform.GetComponent<MeshCollider>().sharedMesh = highLOD.sharedMesh;
    }
    public void RegsiterPermChange(float[] tVals)
    {

        UpdatePermMesh(highLOD, tVals);
        UpdatePermMesh(midLOD, tVals);
        UpdatePermMesh(lowLOD, tVals);
        highLOD.transform.GetComponent<MeshCollider>().sharedMesh = highLOD.sharedMesh;
    }
    public void RegisterTextureChange()
    {
        if (highRenderer == null)
            SetupTerrain();

        highRenderer.material.color = Color.white;
        highRenderer.material.mainTexture = uvTex;

        midRenderer.material.color = Color.white;
        midRenderer.material.mainTexture = uvTex;

        lowRenderer.material.color = Color.white;
        lowRenderer.material.mainTexture = uvTex;

    }
    public void RegisterEditorAccess()
    {
        highRenderer = highTerrainTransform.GetComponent<MeshRenderer>();
        midRenderer = midLOD.GetComponent<MeshRenderer>();
        lowRenderer = lowLOD.GetComponent<MeshRenderer>();
        highLOD = highTerrainTransform.GetComponent<MeshFilter>();
        matTex = new Texture2D(size, size);
        uvTex = new Texture2D(size, size);

        //rendererA.material.mainTexture = matTex;
        vals = new float[size * size];

    }
    public void RegisterPermTextureChange()
    {

        Debug.Log("Called");
        highRenderer.sharedMaterial.color = Color.white;
        highRenderer.sharedMaterial.mainTexture = uvTex;

        midRenderer.sharedMaterial.color = Color.white;
        midRenderer.sharedMaterial.mainTexture = uvTex;

        lowRenderer.sharedMaterial.color = Color.white;
        lowRenderer.sharedMaterial.mainTexture = uvTex;

    }
    Dictionary<string, NativeArray<Vector3>> tDict = new System.Collections.Generic.Dictionary<string, NativeArray<Vector3>>();
    NativeArray<Vector3> position;
    Queue<JobHandle> handles = new Queue<JobHandle>();

    private void UpdateMesh (MeshFilter aF)
    {
        
        if (tDict.ContainsKey(aF.transform.name))
            position = tDict[aF.transform.name];
        else
        {
            position = new NativeArray<Vector3>(aF.mesh.vertices, Allocator.Persistent);
            tDict.Add(aF.transform.name, position);
        }

        NativeArray<float> colorVals = new NativeArray<float>(vals, Allocator.TempJob);
        //Vector3[] verts = aF.mesh.vertices;
        // position.CopyFrom(verts);



        //colorVals.CopyFrom(vals);
        //var byteArr = new NativeArray<Color32>(matTex.GetRawTextureData<Color32>().Length, Allocator.Persistent);
        //byteArr.CopyFrom(matTex.GetRawTextureData<Color32>());
        //SampleMesh();
        //SampleMesh(filterB);
        //SampleMesh(filterC);

        var job = new ApplyNoiseJob()
        {
            verts = position,
            colors = colorVals,
            min = highTerrainTransform.InverseTransformPoint(this.min),
            max = highTerrainTransform.InverseTransformPoint(this.max),
            maxHeight = this.maxHeight,
            size = this.size
        };

        JobHandle jobHandle = job.Schedule(aF.mesh.vertexCount, 10);
        jobHandle.Complete();

        //Vector3[] filteredVerts = new Vector3[aF.mesh.vertexCount];
        //filteredVerts = position.ToArray();
        
        //position.CopyTo(aF.mesh.vertices);
        aF.mesh.vertices = position.ToArray();
        //Don't need to be done every frame lol

        aF.mesh.RecalculateBounds();
        
        aF.mesh.RecalculateNormals();

       
        colorVals.Dispose();


    }

  
    private void OnDestroy()
    {
        foreach (NativeArray<Vector3> s in tDict.Values)
        {
            s.Dispose();
        }
    }

    private void UpdatePermMesh(MeshFilter aF, float[] vals)
    {
        Debug.Log(this.transform.name);
        var position = new NativeArray<Vector3>(aF.sharedMesh.vertexCount, Allocator.TempJob);

        Vector3[] verts = aF.sharedMesh.vertices;
        position.CopyFrom(verts);

        var colorVals = new NativeArray<float>(size * size, Allocator.TempJob);

        colorVals.CopyFrom(vals);
        //var byteArr = new NativeArray<Color32>(matTex.GetRawTextureData<Color32>().Length, Allocator.Persistent);
        //byteArr.CopyFrom(matTex.GetRawTextureData<Color32>());
        //SampleMesh();
        //SampleMesh(filterB);
        //SampleMesh(filterC);

        var job = new ApplyNoiseJob()
        {
            verts = position,
            colors = colorVals,
            min = highTerrainTransform.InverseTransformPoint(this.min),
            max = highTerrainTransform.InverseTransformPoint(this.max),
            maxHeight = this.maxHeight,
            size = this.size
        };

        JobHandle jobHandle = job.Schedule(aF.sharedMesh.vertexCount, 128);
        jobHandle.Complete();
        colorVals.Dispose();
        Vector3[] filteredVerts = new Vector3[aF.sharedMesh.vertexCount];
        filteredVerts = position.ToArray();
        for (int c = 0; c < filteredVerts.Length; c++)
        {
            filteredVerts[c] = (filteredVerts[c]);
        }

        aF.sharedMesh.vertices = filteredVerts;
        aF.sharedMesh.RecalculateBounds();
        aF.sharedMesh.RecalculateNormals();
       //


        position.Dispose();
        
    }
    #endregion

    #region Job

    struct ApplyNoiseJob : IJobParallelFor {

        //Native array of Vector3 to store all the verts
        public NativeArray<Vector3> verts;
        
        public float rXPos, rYPos;

        [NativeDisableParallelForRestriction] //Stores the float[] of the heights that are created in the Texture2D (Texture2D's aren't supported in Job System)
        public NativeArray<float> colors;
        
        //Parameters
        public Vector3 min, max;
        public float maxHeight;
        public int size;

        //Execute Method
        public void Execute (int i)
        {

            Vector3 localPos = verts[i];
            //float realXPos = trans.TransformPoint(localPos).x;
            //float realZPos = trans.TransformPoint(localPos).z;

            float realXPos = localPos.x;
            float realZPos = localPos.z;

            float percX = ((realXPos - min.x)*100) / (max.x - min.x);
            float percY = ((realZPos - min.z)*100) / (max.z - min.z);

            int pixelX = Mathf.Clamp(Mathf.RoundToInt(percX/100 * size),0,size-1);
            int pixelY = Mathf.Clamp(Mathf.RoundToInt(percY/100 * size),0, size-1);

            float r = colors[pixelY * size + pixelX];
   
            float val = r;
            float Fy = (val * maxHeight);

            localPos = new Vector3(realXPos, Fy, realZPos);

            verts[i] = localPos;


        }

    }
    #endregion

    #region Mesh and Noise Generation
    private void SampleMesh()
    {
        Vector3[] verts = highLOD.mesh.vertices;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 localPos = highLOD.mesh.vertices[i];
            float realXPos = highTerrainTransform.TransformPoint(localPos).x;
            float realZPos = highTerrainTransform.TransformPoint(localPos).z;

            float percX = (realXPos - min.x) / (max.x - min.x);
            float percY = (realZPos - min.z) / (max.z - min.z);

            int pixelX = Mathf.RoundToInt(percX * size);
            int pixelY = Mathf.RoundToInt(percY * size);

            Color r = matTex.GetPixel(pixelX, pixelY);
            float val = r.r;

            verts[i].y = val * maxHeight;

        }

        highLOD.mesh.vertices = verts;
        highLOD.mesh.RecalculateBounds();
        highLOD.mesh.RecalculateNormals();
      //  highTerrainTransform.GetComponent<MeshCollider>().sharedMesh = highLOD.mesh;
       
    }
    //Sampling any mesh from the current noise texture
    private void SampleMesh(MeshFilter filterA)
    {
        Vector3[] verts = filterA.mesh.vertices;

        for (int i = 0; i < verts.Length; i++)
        {
            //Finding what vertext maps to what coordinates in the texture 

            Vector3 localPos = filterA.mesh.vertices[i];
            float realXPos = highTerrainTransform.TransformPoint(localPos).x;
            float realZPos = highTerrainTransform.TransformPoint(localPos).z;

            float percX = (realXPos - min.x) / (max.x - min.x);
            float percY = (realZPos - min.z) / (max.z - min.z);

            int pixelX = Mathf.RoundToInt(percX * size);
            int pixelY = Mathf.RoundToInt(percY * size);

            Color r = matTex.GetPixel(pixelX, pixelY);
            float val = r.r;

            verts[i].y = val * maxHeight;

        }

        filterA.mesh.vertices = verts;
        filterA.mesh.RecalculateBounds();
        filterA.mesh.RecalculateNormals();
        filterA.transform.GetComponent<MeshCollider>().sharedMesh = filterA.mesh;

    }
    //Redundant
    public Texture2D GenerateNoiseTexture()
    {
        Texture2D noise = new Texture2D(size, size);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float xCoord = (float)x / size * scale + offset.x;
                float yCoord = (float)y / size * scale + offset.y;


                float noiseValue = Noise(xCoord, yCoord);
                float fade = 1f;

                Color newColor = new Color(noiseValue * fade, noiseValue * fade, noiseValue * fade);

                noise.SetPixel(x, y, newColor);

            }
        }

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float finalFade = 1f;
                int inc = 0;
                float getAverageNeighbours = 1f;

                if (x > size - edgeFadeRadius || x < edgeFadeRadius || y > size - edgeFadeRadius || y < edgeFadeRadius)
                {

                    float fadeUp = 1f;
                    float fadeRight = 1f;

                    if (right && x > size - edgeFadeRadius)
                    {
                        fadeRight = Mathf.Lerp(0, 1, ((float)size - x) / (edgeFadeRadius));
                    }

                    else if (left && x < edgeFadeRadius)
                    {
                        fadeRight = Mathf.Lerp(0, 1, ((float)x) / (edgeFadeRadius));
                    }

                    if (up && y > size - edgeFadeRadius)
                    {
                        fadeUp = Mathf.Lerp(0, 1, ((float)size - y) / (edgeFadeRadius));
                    }

                    else if (down && y < edgeFadeRadius)
                    {
                        fadeUp = Mathf.Lerp(0, 1, ((float)y) / (edgeFadeRadius));
                    }

                    finalFade = fadeRight * fadeUp;

                }

                /*
                 for (int cX = x-neighbourBlend; cX < x+1+neighbourBlend; cX++)
                 {
                     for (int cY = y - neighbourBlend; cY < y + neighbourBlend + 1; cY++)
                     {
                         if (cX == x || cY == y)
                             continue;

                         if (cX > size - 1 || cY > size - 1 || cX < 0 || cY < 0)
                             continue;

                         inc++;
                         getAverageNeighbours += noise.GetPixel(cX, cY).r;

                     }
                 getAverageNeighbours /= inc;
                 }*/


                float v = noise.GetPixel(x, y).r * finalFade * getAverageNeighbours;

                Color newColor = new Color(v, v, v);

                noise.SetPixel(x, y, newColor);

            }
        }


        noise.Apply();

        return noise;

    }

    //Later blend to match the neighbours, so I need a neighbour manager to know what the left, right, up etc. actually is
    //My having this sepration of the terrains but still being able to connec them I think it is powerful.
    private void Blend(Texture2D noise)
    {
        Parallel.For(0, size, x => { 
            for (int y = 0; y < size; y++)
            {
                float finalFade = 1f;
                int inc = 0;
                float getAverageNeighbours = 1f;

                if (x > size - edgeFadeRadius || x < edgeFadeRadius || y > size - edgeFadeRadius || y < edgeFadeRadius)
                {

                    float fadeUp = 1f;
                    float fadeRight = 1f;

                    if (right && x > size - edgeFadeRadius)
                    {
                        fadeRight = Mathf.Lerp(0, 1, ((float)size - x) / (edgeFadeRadius));
                    }

                    else if (left && x < edgeFadeRadius)
                    {
                        fadeRight = Mathf.Lerp(0, 1, ((float)x) / (edgeFadeRadius));
                    }

                    if (up && y > size - edgeFadeRadius)
                    {
                        fadeUp = Mathf.Lerp(0, 1, ((float)size - y) / (edgeFadeRadius));
                    }

                    else if (down && y < edgeFadeRadius)
                    {
                        fadeUp = Mathf.Lerp(0, 1, ((float)y) / (edgeFadeRadius));
                    }

                    finalFade = fadeRight * fadeUp;

                }

                /*
                 for (int cX = x-neighbourBlend; cX < x+1+neighbourBlend; cX++)
                 {
                     for (int cY = y - neighbourBlend; cY < y + neighbourBlend + 1; cY++)
                     {
                         if (cX == x || cY == y)
                             continue;

                         if (cX > size - 1 || cY > size - 1 || cX < 0 || cY < 0)
                             continue;

                         inc++;
                         getAverageNeighbours += noise.GetPixel(cX, cY).r;

                     }
                 getAverageNeighbours /= inc;
                 }*/


                float v = Mathf.SmoothStep(0, vals[x * size + y], finalFade);

                vals[x * size + y] = v;

            }
        });


        noise.Apply();
    }

    //Generating a noise texture in parallel (quicker)
    public Texture2D GenerateNoiseTexture(bool parallelT)
    {
        Texture2D noise = new Texture2D(size, size);

        //Keeping an empty array the size of the noise texture's dimensions
        float[] vals2 = new float[size*size];

        //Run a Parallel.For
        Parallel.For(0, size,
            x =>
            {
                //Double loop
                Parallel.For(0, size, y =>
                {
                    //Coordinate creation based on parameters
                    float xCoord = (float)x / size * scale + offset.x;
                    float yCoord = (float)y / size * scale + offset.y;

                    //Getting perlin noise
                    float noiseValue = Noise(xCoord, yCoord);
                    
                    //Fading not supported yet
                    float fade = 1f;

                    //Generating color from noiseValue
                    float newColor = (noiseValue * fade)*1f;

                    //Setting the value of the pixel in the float[] array as Texture2D's aren't supported in threads other than the main
                    vals2[x * size + y] = newColor;

                });

            });

        //Transporting values from float[] to Texture -- find a better way to bind these
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float a = vals2[x * size + y];
                noise.SetPixel(x, y, new Color(a,a,a));
            }
        }

        vals = vals2;

        //Applying texture
        noise.Apply();


        return noise;
    }
    #endregion

    #region Helper Functions
    //Create perlin noise using x & y input
    public float Noise (float x, float y)
    {

        return Mathf.PerlinNoise(x, y);
       
    }

    //Finding a point on the grid given X & Y
    public Point FindPoint (int x, int y)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].x == x && points[i].y == y)
            {
                return points[i];
            }
        }

        return new Point();
    }

    //Finding the texture coordinates + returning texture darkness float[] based on worldPos parameter
    public (int, int, float[] vals) FindTextureIndex ( Vector3 worldPos )
    {

        Vector3 localPos = worldPos;
        //float realXPos = trans.TransformPoint(localPos).x;
        //float realZPos = trans.TransformPoint(localPos).z;

        float realXPos = localPos.x;
        float realZPos = localPos.z;

        float percX = ((realXPos - min.x) * 100) / (max.x - min.x);
        float percY = ((realZPos - min.z) * 100) / (max.z - min.z);



        int pixelX = Mathf.Clamp(Mathf.RoundToInt((percX) / 100 * size), 0, size - 1);
        int pixelY = Mathf.Clamp(Mathf.RoundToInt((percY) / 100 * size), 0, size - 1);

        return (pixelY, pixelX, vals);
    }

    //Finding a non-biased UV texture
    public (int, int) FindUVTextureIndex(Vector3 worldPos)
    {

        Vector3 localPos = worldPos;
        //float realXPos = trans.TransformPoint(localPos).x;
        //float realZPos = trans.TransformPoint(localPos).z;

        float realXPos = localPos.x;
        float realZPos = localPos.z;

        float percX = ((realXPos - min.x) * 100) / (max.x - min.x);
        float percY = ((realZPos - min.z) * 100) / (max.z - min.z);



        int pixelX = Mathf.Clamp(Mathf.RoundToInt((percX) / 100 * size), 0, size - 1);
        int pixelY = Mathf.Clamp(Mathf.RoundToInt((percY) / 100 * size), 0, size - 1);

        return (pixelX, pixelY);
    }
    #endregion
}

[System.Serializable]
public struct Point
{
    public int x;
    public int y;

    public Point (int _x, int _y) { x = _x; y = _y; }

    public override bool Equals(object obj)
    {
        Point p = (Point)obj;

        if (p.x == x && p.y == y) return true;
        return false;
    }

}
 
