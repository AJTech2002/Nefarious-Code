using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;
using UnityEditor;
public class QuadMesh : MonoBehaviour
{
    public string ID;
    public static QuadMesh instance;
    public List<Vector3> verts  = new List<Vector3>();
    public List<int> tris = new List<int>();
    public List<Vector2> UVs = new List<Vector2>();
    public Dictionary<Vector3, VertexReference> vertIndexDictionary = new Dictionary<Vector3, VertexReference>();

    [Header("Settings")]
    public bool drawGizmos = true;
    public bool drawSquares = true;
    public bool drawVerts = false;
    public bool zOrientation = false;

    public float maxHeight = 0.4f;
    public float threshold = 0.95f;
    public float minimumThreshold = 0.005f;
    public int maxDimensions = 20;

    public int minResolution = 3;

    public Quad parentQuad;

    [Header("Custom")]
    public Texture2D testTexture;
    public Plane testPlane;

    [Header("meta")]
    public Vector3 min;
    public Vector3 max;

    public Vector3 minBounds;
    public Vector3 maxBounds;

    //References
    private static Dictionary<Vector3, int> indexFromVector = new Dictionary<Vector3, int>();

    private Mesh emptyMesh;

    private void Awake()
    {
        instance = this;
        emptyMesh = new Mesh();
        parentQuad = new Quad(0, 0, 100, 100);

        AutoSubdivide(parentQuad);

//        NeighbourSubdivide(parentQuad);
        parentQuad.EnsureMaxDepth();

        parentQuad.FinaliseGeometry(min, max, transform.position.y);
       
        parentQuad.DeleteUnrequired(min, max, transform.position.y);
        tris.Clear();
        parentQuad.RedrawGeometry(min, max, transform.position.y);

        
        Vector3 minT = transform.InverseTransformPoint(min);
        Vector3 maxT = transform.InverseTransformPoint(max);
        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 localPos = verts[i];
            //float realXPos = trans.TransformPoint(localPos).x;
            //float realZPos = trans.TransformPoint(localPos).z;

            float realXPos = localPos.x;
            float realZPos = localPos.y;

            float percX = ((realXPos - minT.x) * 100) / (maxT.x - minT.x);
            float percY = ((realZPos - minT.y) * 100) / (maxT.y - minT.y);



            int pixelX = Mathf.Clamp(Mathf.RoundToInt(percX / 100 * testTexture.width), 0, testTexture.width - 1);
            int pixelY = Mathf.Clamp(Mathf.RoundToInt(percY / 100 * testTexture.height), 0, testTexture.height - 1);

            float r = testTexture.GetPixel(pixelX, pixelY).r;

            float val = r;
            float Fy = (val * maxHeight);

            localPos = new Vector3(localPos.x, localPos.y, Fy);

            verts[i] = localPos;
        }

        emptyMesh.vertices = verts.ToArray();
        emptyMesh.triangles = tris.ToArray();
        emptyMesh.uv = UVs.ToArray();

        emptyMesh.RecalculateBounds();
        emptyMesh.RecalculateNormals();

        emptyMesh.RecalculateTangents();




        GetComponent<MeshFilter>().mesh = emptyMesh;
        GetComponent<MeshCollider>().sharedMesh = emptyMesh;
        string path = "Assets/TerrainChunks/MeshData/";


        AssetDatabase.CreateAsset(emptyMesh, path + ID + "_MESH.mesh");
        AssetDatabase.SaveAssets();

        verts.Clear();
        indexFromVector.Clear();
        tris.Clear();
        UVs.Clear();

    }

    private void OnDrawGizmos()
    {
        if (drawGizmos)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(min, 0.3f);
            Gizmos.DrawWireSphere(max, 0.3f);

            if (drawSquares)
                DrawQuad(parentQuad);

            if (drawVerts)
            for (int i = 0; i < verts.Count; i++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere((verts[i]), 0.1f);
            }
        }
    }

    private void DrawQuad(Quad q)
    {
        float xMinVal = min.x + (max.x - min.x) * q.minX / 100;
        float xMaxval = min.x + (max.x - min.x) * q.maxX / 100;
        float yMinVal = min.y + (max.y - min.y) * q.minY / 100;
        float yMaxVal = min.y + (max.y - min.y) * q.maxY / 100;

        if (zOrientation)
        {
            xMinVal = min.x + (max.x - min.x) * q.minX / 100;
            xMaxval = min.x + (max.x - min.x) * q.maxX / 100;
            yMinVal = min.z + (max.z - min.z) * q.minY / 100;
            yMaxVal = min.z + (max.z - min.z) * q.maxY / 100;
        }
        Gizmos.color = Color.green;

        (Vector2 minV, Vector2 maxV) = q.NormaliseValues(new Vector2(0, 0), new Vector2(testTexture.width, testTexture.height));

       

        if (!zOrientation)
        {
            Gizmos.DrawLine(new Vector3(xMinVal, yMinVal, transform.position.z), new Vector3(xMaxval, yMinVal, 0));
            Gizmos.DrawLine(new Vector3(xMinVal, yMinVal, transform.position.z), new Vector3(xMinVal, yMaxVal, 0));
            Gizmos.DrawLine(new Vector3(xMinVal, yMaxVal, transform.position.z), new Vector3(xMaxval, yMaxVal, 0));
            Gizmos.DrawLine(new Vector3(xMaxval, yMinVal, transform.position.z), new Vector3(xMaxval, yMaxVal, 0));
        }
        else
        {
            Gizmos.DrawLine(new Vector3(xMinVal, transform.position.y, yMinVal), new Vector3(xMaxval, transform.position.y, yMinVal));
            Gizmos.DrawLine(new Vector3(xMinVal, transform.position.y, yMinVal), new Vector3(xMinVal, transform.position.y, yMaxVal));
            Gizmos.DrawLine(new Vector3(xMinVal, transform.position.y, yMaxVal), new Vector3(xMaxval, transform.position.y, yMaxVal));
            Gizmos.DrawLine(new Vector3(xMaxval, transform.position.y, yMinVal), new Vector3(xMaxval, transform.position.y, yMaxVal));
        }

        if (q.subQuads.Count > 0)
        {
            for (int i = 0; i < q.subQuads.Count; i++)
            {
                DrawQuad(q.subQuads[i]);
            }
        }

    }

    private void DrawDebugQuad(Quad q, Color r)
    {
        float xMinVal = min.x + (max.x - min.x) * q.minX / 100;
        float xMaxval = min.x + (max.x - min.x) * q.maxX / 100;
        float yMinVal = min.y + (max.y - min.y) * q.minY / 100;
        float yMaxVal = min.y + (max.y - min.y) * q.maxY / 100;

        if (zOrientation)
        {
            xMinVal = min.x + (max.x - min.x) * q.minX / 100;
            xMaxval = min.x + (max.x - min.x) * q.maxX / 100;
            yMinVal = min.z + (max.z - min.z) * q.minY / 100;
            yMaxVal = min.z + (max.z - min.z) * q.maxY / 100;
        }
        

        (Vector2 minV, Vector2 maxV) = q.NormaliseValues(new Vector2(0, 0), new Vector2(testTexture.width, testTexture.height));



        if (!zOrientation)
        {
            Debug.DrawLine(new Vector3(xMinVal, yMinVal, transform.position.z), new Vector3(xMaxval, yMinVal, 0), r,20);
            Debug.DrawLine(new Vector3(xMinVal, yMinVal, transform.position.z), new Vector3(xMinVal, yMaxVal, 0), r,20);
            Debug.DrawLine(new Vector3(xMinVal, yMaxVal, transform.position.z), new Vector3(xMaxval, yMaxVal, 0), r,20);
            Debug.DrawLine(new Vector3(xMaxval, yMinVal, transform.position.z), new Vector3(xMaxval, yMaxVal, 0), r, 20);
        }
        else
        {
            Debug.DrawLine(new Vector3(xMinVal, transform.position.y, yMinVal), new Vector3(xMaxval, transform.position.y, yMinVal), r,20);
            Debug.DrawLine(new Vector3(xMinVal, transform.position.y, yMinVal), new Vector3(xMinVal, transform.position.y, yMaxVal), r,20);
            Debug.DrawLine(new Vector3(xMinVal, transform.position.y, yMaxVal), new Vector3(xMaxval, transform.position.y, yMaxVal), r,20);
            Debug.DrawLine(new Vector3(xMaxval, transform.position.y, yMinVal), new Vector3(xMaxval, transform.position.y, yMaxVal), r, 20);
        }

        if (q.subQuads.Count > 0)
        {
            for (int i = 0; i < q.subQuads.Count; i++)
            {
                DrawDebugQuad(q.subQuads[i],r);
            }
        }

    }

    public int maxSubdivisions = 0;
    private void AutoSubdivide(Quad q)
    {
        (Vector2 minV, Vector2 maxV) = q.NormaliseValues(new Vector2(0, 0), new Vector2(testTexture.width, testTexture.height));
        float f = averagePixels(minV, maxV);
       
        if (maxV.x - minV.x < maxDimensions || maxV.y - minV.y < maxDimensions)
            return;

        if ((f > minimumThreshold && f <= threshold) || q.level <= minResolution)
        {

            q.SubdivideQuad();

            AutoSubdivide(q.a);
            AutoSubdivide(q.b);
            AutoSubdivide(q.c);
            AutoSubdivide(q.d);

        }

    }

    private void NeighbourSubdivide  (Quad q)
    {
        if (q.subQuads.Count > 0)
        {
            NeighbourSubdivide(q.a);
            NeighbourSubdivide(q.b);
            NeighbourSubdivide(q.c);
            NeighbourSubdivide(q.d);
        }
        else
        {
            if (q.level < maxSubdivisions)
            {
                for (int i = 0; i < 2 + maxSubdivisions - q.level; i++)
                {
                    DrawDebugQuad(q, Color.green);
                    print("TRIED SUBDIVIDE : " + q.parent.level);
                    q.SubdivideQuad(false);
                    q.a.SubdivideQuad(false);
                    q.b.SubdivideQuad(false);
                    q.c.SubdivideQuad(false);
                    q.d.SubdivideQuad(false);
                    DrawDebugQuad(q, Color.red);
                }
            }
        }
    }

    private float averagePixels(Vector2 min, Vector2 max)
    {
        float avgPix = 0f;
        int am = 0;
        for (int x = Mathf.RoundToInt(min.x); x < Mathf.RoundToInt(max.x); x++)
        {
            for (int y = Mathf.RoundToInt(min.y); y < Mathf.RoundToInt(max.y); y++)
            {
                am++;
                avgPix += (testTexture.GetPixel(x, y).r);
            }
        }

        avgPix /= am;

        return avgPix;
    }

    [System.Serializable]
    public class Quad
    {
        public Quad parent;
        public int level = 0;
        public List<Edge> edges = new List<Edge>();
        public List<Triangle> triangles = new List<Triangle>();
        public List<int> localVerts = new List<int>();
        public List<Quad> subQuads
        {
            get
            {
                List<Quad> temp = new List<Quad>();
                if (a != null) temp.Add(a);
                if (b != null) temp.Add(b);
                if (c != null) temp.Add(c);
                if (d != null) temp.Add(d);

                return temp;
            }
        }

        public Quad(float minX, float minY, float maxX, float maxY)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
        }

        [Header("Children Quads")]
        //Upper left
        public Quad a = null;

        //Upper right
        public Quad b = null;

        //Bottom left
        public Quad c = null;

        //Bottom right
        public Quad d = null;

        [Header("Current Quad Properties")]
        //Percentages
        public float minX, maxX;
        public float minY, maxY;

        [Header("Values")]
        //Value to hold
        public float averageValue = 0f;

        //Creating the subquads
        public void SubdivideQuad(bool inc=true)
        {
            //Create the correct geometry
            Quad upperLeft = new Quad(minX, minY, minX + (maxX - minX) / 2, minY + (maxY - minY) / 2);
            
            Quad upperRight = new Quad(minX + (maxX - minX) / 2, minY, maxX, minY + (maxY - minY) / 2);
            
            Quad lowerLeft = new Quad(minX, minY + (maxY - minY) / 2, minX + (maxX - minX) / 2, maxY);
            
            Quad lowerRight = new Quad(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2, maxX, maxY);
            
            a = upperLeft;
            b = upperRight;
            c = lowerLeft;
            d = lowerRight;

            a.parent = this;
            b.parent = this;
            c.parent = this;
            d.parent = this;

            a.level = level + 1;
            b.level = level + 1;
            c.level = level + 1;
            d.level = level + 1;

            if (a.level > instance.maxSubdivisions && inc == true)
            {
                instance.maxSubdivisions = a.level;
            }
        }

        public void DeleteUnrequired(Vector3 inputMin, Vector3 inputMax, float y)
        {
            if (subQuads.Count > 0)
            {
                a.DeleteUnrequired(inputMin, inputMax, y);
                b.DeleteUnrequired(inputMin, inputMax, y);
                c.DeleteUnrequired(inputMin, inputMax, y);
                d.DeleteUnrequired(inputMin, inputMax, y);
            }
            else
            {

                for (int i = 0; i < localVerts.Count; i++)
                {
                    VertexReference reference = instance.vertIndexDictionary[instance.verts[localVerts[i]]];

                    if (reference.references == 1)
                    {
                        Vector3 v = instance.transform.TransformPoint(instance.verts[localVerts[i]]);
                        if (v.x > instance.minBounds.x && v.x < instance.maxBounds.x)
                        {
                            if (v.z > instance.maxBounds.z && v.z < instance.minBounds.z)
                            {
                               
                                localVerts.RemoveAt(i);

                            }
                        }
                    }
                }

            }

        }

        public void RedrawGeometry (Vector3 inputMin, Vector3 inputMax, float y)
        {
            if (subQuads.Count > 0)
            {
                a.RedrawGeometry(inputMin, inputMax, y);
                b.RedrawGeometry(inputMin, inputMax, y);
                c.RedrawGeometry(inputMin, inputMax, y);
                d.RedrawGeometry(inputMin, inputMax, y);
            }
            else
            {
                triangles.Clear();

                for (int i = 0; i < localVerts.Count; i++)
                {
                    if (i == localVerts.Count - 1 || i == 0) continue;

                    int aT = i - 1;
                    int bT = i;
                    int cT = localVerts.Count - 1;

                    Triangle t = new Triangle();
                    t.a = aT;
                    t.b = bT;
                    t.c = cT;

                    //Temporary Add
                    instance.tris.AddRange(new int[] { localVerts[aT], localVerts[bT], localVerts[cT] });

                    triangles.Add(t);
                }

                instance.tris.AddRange(new int[] { localVerts[localVerts.Count - 2], localVerts[0], localVerts[localVerts.Count - 1] });
                Triangle t2 = new Triangle();
                t2.a = 7;
                t2.b = 0;
                t2.c = 8;

                triangles.Add(t2);
            }
        }

        public void EnsureMaxDepth()
        {
            if (subQuads.Count > 0)
            {
                a.EnsureMaxDepth();
                b.EnsureMaxDepth();
                c.EnsureMaxDepth();
                d.EnsureMaxDepth();
            }
            else
            {
                //Work up from here
                if (parent != null)
                {
                    parent.Equalise();
                    
                }
                
            }
        }

        public void Equalise ()
        {
            int maxDepth = FindMaxDepth();

            int aDepth = a.FindMaxDepth();
            int bDepth = b.FindMaxDepth();
            int cDepth = c.FindMaxDepth();
            int dDepth = d.FindMaxDepth();

            if (aDepth < maxDepth-1)
            {
                for (int i = 0; i < maxDepth - aDepth ; i++)
                {
                    a.SubdivideQuad();
                    a.a.SubdivideQuad();
                    a.b.SubdivideQuad();
                    a.c.SubdivideQuad();
                    a.d.SubdivideQuad();
                }
            }

            if (bDepth < maxDepth - 1)
            {
                for (int i = 0; i <  maxDepth - bDepth ; i++)
                {
                    b.SubdivideQuad();
                    b.a.SubdivideQuad();
                    b.b.SubdivideQuad();
                    b.c.SubdivideQuad();
                    b.d.SubdivideQuad();
                }
            }

            if (cDepth < maxDepth - 1)
            {
                for (int i = 0; i <maxDepth - cDepth ; i++)
                {
                    c.SubdivideQuad();
                    c.a.SubdivideQuad();
                    c.b.SubdivideQuad();
                    c.c.SubdivideQuad();
                    c.d.SubdivideQuad();
                }
            }

            if (dDepth < maxDepth - 1)
            {
                for (int i = 0; i < maxDepth - dDepth ; i++)
                {
                    d.SubdivideQuad();
                    d.a.SubdivideQuad();
                    d.b.SubdivideQuad();
                    d.c.SubdivideQuad();
                    d.d.SubdivideQuad();
                }
            }

        }

        public int FindMaxDepth()
        {
            if (subQuads.Count > 0)
            {
                int aLevel = a.FindMaxDepth();
                int bLevel = b.FindMaxDepth();
                int cLevel = c.FindMaxDepth();
                int dLevel = d.FindMaxDepth();

                int[] levels = new int[] { aLevel, bLevel, cLevel, dLevel };

                int maxLevel = 0;
                for (int i = 0; i < levels.Length; i++)
                {
                    if (levels[i] > maxLevel)
                    {
                        maxLevel = levels[i];
                    }
                }

                return maxLevel;
            }
            else
            {
                return level;
            }
        }

        public void FinaliseGeometry(Vector3 inputMin, Vector3 inputMax, float y)
        {
            if (subQuads.Count > 0)
            {
                a.FinaliseGeometry(inputMin, inputMax, y);
                b.FinaliseGeometry(inputMin, inputMax, y);
                c.FinaliseGeometry(inputMin, inputMax, y);
                d.FinaliseGeometry(inputMin, inputMax, y);
            }
            else
            {

                (Vector2 minV2, Vector2 maxV2) = NormaliseValuesZ(inputMin, inputMax);
                
                Vector2 minA = new Vector2(minX / 100, minY / 100);
                Vector2 maxB = new Vector2(maxX / 100, minY / 100);
                Vector2 maxC = new Vector2(minX / 100, maxY / 100);
                Vector2 maxD = new Vector2(maxX / 100, maxY / 100);

                Vector2 UVAB = (minA + maxB) / 2;
                Vector2 UVAC = (minA + maxC) / 2;
                Vector2 UVCD = (maxC + maxD) / 2;
                Vector2 UVBD = (maxB + maxD) / 2;
                Vector2 UVCENTER = (minA + maxB + maxC + maxD) / 4;

                //0
                Vector3 a = new Vector3(minV2.x, y, minV2.y);
                
                //1
                Vector3 b = new Vector3(maxV2.x, y, minV2.y);

                //2
                Vector3 c = new Vector3(minV2.x, y, maxV2.y);
                
                //3
                Vector3 d = new Vector3(maxV2.x, y, maxV2.y);

                //Top (4)
                Vector3 aToB = (a + b) / 2;

                //Right (5)
                Vector3 bToD = (b + d) / 2;

                //Bottom (6)
                Vector3 cToD = (c + d) / 2;

                //Left (7)
                Vector3 aToC = (a + c) / 2;

                //Center (8)
                Vector3 center = (a + b + c + d) / 4;

                //A = 0, A->B = 1, B = 2, B->D = 3, D = 4, D->C = 5, C = 6, C->A = 7, CENTER = 8

                List<Vector3> rangeAdd = new List<Vector3>(new Vector3[] { a, aToB, b, bToD, d, cToD, c, aToC, center });
                List<Vector2> rangeAddUV = new List<Vector2>(new Vector2[] {minA, UVAB, maxB, UVBD, maxD, UVCD, maxC, UVAC,  UVCENTER});

                for (int i = 0; i < rangeAdd.Count; i++)
                {
                    if (!instance.verts.Contains(instance.transform.InverseTransformPoint(rangeAdd[i])))
                    {

                        instance.verts.Add(instance.transform.InverseTransformPoint(rangeAdd[i]));
                        instance.UVs.Add(rangeAddUV[i]);
                        VertexReference reference = new VertexReference();
                        reference.vertex = instance.verts.Count - 1;
                        if (i != 8)
                        reference.references = 1;
                        reference.quads = new List<Quad>();
                        reference.quads.Add(this);

                        instance.vertIndexDictionary.Add(instance.transform.InverseTransformPoint(rangeAdd[i]),reference);
                    }
                    else
                    {
                        VertexReference reference = instance.vertIndexDictionary[instance.transform.InverseTransformPoint(rangeAdd[i])];
                        reference.quads.Add(this);
                        reference.references += 1;
                    }
                    //Won't work because sometimes it's not called!!
                    localVerts.Add(instance.vertIndexDictionary[instance.transform.InverseTransformPoint(rangeAdd[i])].vertex);
                }
                
                //A -> B (Index of local vert corresponding to global vertex index)
                Edge e1 = new Edge(0,1,2);
                
                //B to D
                Edge e2 = new Edge(2,3,4);

                //C to D 
                Edge e3 = new Edge(4,5,6);

                //A -> C
                Edge e4 = new Edge(0, 7, 6);

                edges.AddRange(new Edge[] { e1, e2, e3, e4 });

                //If one of the verts get deleted a triangle get's made using the same ciclical pattern
                for (int i = 0; i < localVerts.Count; i++)
                {
                    if (i == localVerts.Count-1 || i == 0) continue;

                    int aT = i - 1;
                    int bT = i;
                    int cT = localVerts.Count-1;

                    Triangle t = new Triangle();
                    t.a = aT;
                    t.b = bT;
                    t.c = cT;

                    //Temporary Add
                    instance.tris.AddRange(new int[] { localVerts[aT], localVerts[bT], localVerts[cT] });

                    triangles.Add(t);
                }

                instance.tris.AddRange(new int[] { localVerts[localVerts.Count - 2], localVerts[0], localVerts[localVerts.Count-1] });
                Triangle t2 = new Triangle();
                t2.a = 7;
                t2.b = 0;
                t2.c = 8;

                triangles.Add(t2);

            }
        }

        public (Vector2, Vector2) NormaliseValues(Vector2 inputMin, Vector2 inputMax)
        {
            float xMinVal = inputMin.x + (inputMax.x - inputMin.x) * minX / 100;
            float xMaxval = inputMin.x + (inputMax.x - inputMin.x) * maxX / 100;
            float yMinVal = inputMin.y + (inputMax.y - inputMin.y) * minY / 100;
            float yMaxVal = inputMin.y + (inputMax.y - inputMin.y) * maxY / 100;

            return (new Vector2(xMinVal, yMinVal), new Vector2(xMaxval, yMaxVal));
        }

        public (Vector2, Vector2) NormaliseValuesZ(Vector3 inputMin, Vector3 inputMax)
        {
            float xMinVal = inputMin.x + (inputMax.x - inputMin.x) * minX / 100;
            float xMaxval = inputMin.x + (inputMax.x - inputMin.x) * maxX / 100;
            float yMinVal = inputMin.z + (inputMax.z - inputMin.z) * minY / 100;
            float yMaxVal = inputMin.z + (inputMax.z - inputMin.z) * maxY / 100;

            return (new Vector2(xMinVal, yMinVal), new Vector2(xMaxval, yMaxVal));
        }

    }
}

public struct Edge
{ public int a, b, c; public Edge(int _a, int _b, int _c) { a = _a; b = _b; c = _c; } }
public struct Triangle
{
    public int a, b, c;
}

public class VertexReference
{
    public List<QuadMesh.Quad> quads;
    public int vertex;
    public int references;
}