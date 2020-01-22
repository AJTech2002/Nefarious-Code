using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Math = System.Math;
//For testing make it a monobehaviour
public class Quadtree : MonoBehaviour
{
    public bool drawGizmos = true;
    public bool zOrientation = false;

    public float maxHeight = 0.4f;
    public float threshold = 0.95f;
    public float minimumThreshold = 0.005f;
    public int maxDimensions = 20;

    public int minResolution = 3;

    private Quad parentQuad;
    [Header("Custom")]
    public Texture2D testTexture;
    public Plane testPlane;

    [Header("meta")]
    public Vector3 min;
    public Vector3 max;

    public Vector3 minBounds;
    public Vector3 maxBounds;

    private List<Vector3> vertexArray = new List<Vector3>();
    private Dictionary<Vector3, int> indexFromVector = new Dictionary<Vector3, int>();
    private Dictionary<int, float> heightWithIndex = new Dictionary<int, float>();

    public Dictionary<int, List<int>> trianglesInConnections = new Dictionary<int, List<int>>();

    private Dictionary<int, int> terrainVertexReference = new Dictionary<int, int>();
    private Dictionary<int, Quad> quadVertRelationship = new Dictionary<int, Quad>();

    public List<Vector2> UVs = new List<Vector2>();
    private List<int> triangles = new List<int>();

    private Mesh emptyMesh;
    private void Awake()
    {
        parentQuad = new Quad(0, 0, 100, 100);

        AutoSubdivide(parentQuad);

        

        emptyMesh = new Mesh();
        CreateMesh(parentQuad, ref emptyMesh);


        Vector3 minT = transform.InverseTransformPoint(min);
        Vector3 maxT = transform.InverseTransformPoint(max);
        for (int i = 0; i < vertexArray.Count; i++)
        {
            Vector3 localPos = vertexArray[i];
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

            vertexArray[i] = localPos;
        }
        SmoothBounds(parentQuad);

        emptyMesh.vertices = vertexArray.ToArray();
        emptyMesh.triangles = triangles.ToArray();
        emptyMesh.uv = UVs.ToArray();

        emptyMesh.RecalculateBounds();
        emptyMesh.RecalculateNormals();

        emptyMesh.RecalculateTangents();

       


        GetComponent<MeshFilter>().mesh = emptyMesh;
        vertexArray.Clear();
        indexFromVector.Clear();
        triangles.Clear();
        UVs.Clear();

        
    }

    private void SmoothBounds(Quad parentQuad)
    {
        List<int> ints = new List<int>();
        int[] t = ints.ToArray();

        foreach (KeyValuePair<int,int> keyValue in terrainVertexReference)
        {
            if (keyValue.Value < 4)
            {
                Vector3 v = transform.TransformPoint(vertexArray[keyValue.Key]);
                print(v + " Max " + maxBounds + " Min " + minBounds);
                if (v.x > minBounds.x && v.x < maxBounds.x )
                {
                    if (v.z > maxBounds.z && v.z < minBounds.z)
                    {


                        (float x, float y) = PercentageVector(v, max, min);

                        Quad foundQuad = quadVertRelationship[keyValue.Key];
                        (Vector3 aC, Vector3 bC, Vector3 cC, Vector3 dC) = foundQuad.NormalisedCorners(min, max, transform.position.y);

                        List<int> connectedVerts = trianglesInConnections[keyValue.Key];
                        float avgY = 0f;
                        int conAdd = 0;
                        for (int i = 0; i < connectedVerts.Count; i++)
                        {
                            if (vertexArray[connectedVerts[i]].z >= vertexArray[keyValue.Key].z)
                            {
                                conAdd++;
                                avgY += vertexArray[connectedVerts[i]].z;
                            }
                        }
                        avgY /= conAdd;

                        vertexArray[keyValue.Key] = new Vector3(vertexArray[keyValue.Key].x, vertexArray[keyValue.Key].y, avgY);

                        if (x > foundQuad.minX + (foundQuad.maxX - foundQuad.minX) / 2)
                        {
                            //Right
                            Debug.DrawLine(transform.TransformPoint(vertexArray[keyValue.Key]), transform.TransformPoint(vertexArray[keyValue.Key]) + Vector3.up, Color.blue, 100);
                           // vertexArray[keyValue.Key] = new Vector3(bC.x, transform.position.y, bC.z);

                        }
                        else if (x < foundQuad.minX + (foundQuad.maxX - foundQuad.minX) / 2)
                        {
                            //Left
                            Debug.DrawLine(transform.TransformPoint(vertexArray[keyValue.Key]), transform.TransformPoint(vertexArray[keyValue.Key]) + Vector3.up, Color.red, 100);
                            //vertexArray[keyValue.Key] = new Vector3(cC.x, transform.position.y, cC.z);
                        }
                    }
                    //Once quad is found
                }
            }
        }
    }

    private Quad FindQuadFromVector (Vector3 v)
    {
        Quad tempQuad = parentQuad;

        (float x, float y) = PercentageVector(v, max, min);

        print("V " + v + " X : " + x + " Y : " + y);
        
        FindQuad(x, y, parentQuad, ref tempQuad);

        return tempQuad;
    }

    private void FindQuad (float percentageX, float percentageY, Quad starting, ref Quad outQuad)
    {
        if (starting.minX < percentageX && starting.maxX > percentageX)
        {
            if (starting.minY < percentageY && starting.maxY > percentageY)
            {

                   
                if (starting.subQuads.Count > 0)
                {
                    Quad t = null;
                    FindQuad(percentageX, percentageY, starting.a, ref t);

                    if (t != null) { outQuad = t; return; }

                    FindQuad(percentageX, percentageY, starting.b, ref t);

                    if (t != null) { outQuad = t; return; }


                    FindQuad(percentageX, percentageY, starting.c, ref t);

                    if (t != null) { outQuad = t; return; }
                    FindQuad(percentageX, percentageY, starting.d, ref t);

                    if (t != null) { outQuad = t; return; }
                }
                else
                {
                    outQuad = starting;
                }
            }
        }

    }

    private (float, float) PercentageVector (Vector3 p, Vector3 inputMax, Vector3 inputMin)
    {
        float xPerc = (p.x - inputMin.x) / (inputMax.x - inputMin.x) * 100;
        float yperc = (p.z - inputMin.z) / (inputMax.z - inputMin.z) * 100;

        return (xPerc, yperc);
    }

    public Vector3 NormalisePoint(Vector3 inputMin, Vector3 inputMax, Vector2 knownPerc, float knownY)
    {
        float xMinVal = inputMin.x + (inputMax.x - inputMin.x) * knownPerc.x / 100;
        float yMinVal = inputMin.z + (inputMax.z - inputMin.z) * knownPerc.y / 100;

        return (new Vector3(xMinVal, knownY, yMinVal));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            parentQuad = new Quad(0, 0, 100, 100);

            AutoSubdivide(parentQuad);

            emptyMesh = new Mesh();
            CreateMesh(parentQuad, ref emptyMesh);


            emptyMesh.vertices = vertexArray.ToArray();
            emptyMesh.triangles = triangles.ToArray();

            emptyMesh.RecalculateBounds();
            emptyMesh.RecalculateNormals();
            emptyMesh.RecalculateTangents();

            GetComponent<MeshFilter>().mesh = emptyMesh;
            vertexArray.Clear();
            indexFromVector.Clear();
            triangles.Clear();
        }
    }

    private void CreateMesh (Quad parent, ref Mesh mesh)
    {
        if (parent.subQuads.Count <= 0)
        {
           // (Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight) = parent.NormalisedCorners(new Vector2(min.x, min.z), new Vector2(max.x, max.z), transform.position.y);

            (Vector2 minV2, Vector2 maxV2) = parent.NormaliseValues(new Vector2(min.x, min.z), new Vector2(max.x, max.z));


            Vector3 topLeft = transform.InverseTransformPoint(new Vector3(minV2.x, transform.position.y, minV2.y));
            Vector3 topRight = transform.InverseTransformPoint(new Vector3(maxV2.x, transform.position.y, minV2.y));

            Vector3 bottomLeft = transform.InverseTransformPoint(new Vector3(minV2.x, transform.position.y, maxV2.y));
            Vector3 bottomRight = transform.InverseTransformPoint(new Vector3(maxV2.x, transform.position.y, maxV2.y));

            

            try
            {
                
                int topLeftIndex = indexFromVector[topLeft];

                terrainVertexReference[topLeftIndex] += 1;
                
                

                int topRightIndex = indexFromVector[topRight];

                terrainVertexReference[topRightIndex] += 1;


                int bottomLeftIndex = indexFromVector[bottomLeft];

                terrainVertexReference[bottomLeftIndex] += 1;

                int bottomRightIndex = indexFromVector[bottomRight];

                terrainVertexReference[bottomRightIndex] += 1;

                trianglesInConnections[topLeftIndex].AddRange(new int[] { topRightIndex, bottomLeftIndex, bottomRightIndex });
                trianglesInConnections[topRightIndex].AddRange(new int[] { topLeftIndex, bottomLeftIndex, bottomRightIndex });
                trianglesInConnections[bottomRightIndex].AddRange(new int[] { topLeftIndex, bottomLeftIndex,topRightIndex });
                trianglesInConnections[bottomLeftIndex].AddRange(new int[] { topLeftIndex, topRightIndex, bottomRightIndex });

                triangles.Add(topLeftIndex); triangles.Add(topRightIndex); triangles.Add(bottomRightIndex);
                triangles.Add(bottomRightIndex); triangles.Add(bottomLeftIndex); triangles.Add(topLeftIndex);
            }
            catch { }

            return;
        }
        else {

            CreateMesh(parent.a, ref mesh);
            CreateMesh(parent.b, ref mesh);
            CreateMesh(parent.c, ref mesh);
            CreateMesh(parent.d, ref mesh);

        };
    }

    private void SmoothSubdivide (Quad q)
    {
        int maxSubdivisions = 0;
        if (q.subQuads.Count >= 0)
        {
            if (q.a.level > maxSubdivisions) q.a.level = maxSubdivisions;
            if (q.b.level > maxSubdivisions) q.b.level = maxSubdivisions;
            if (q.c.level > maxSubdivisions) q.c.level = maxSubdivisions;
            if (q.d.level > maxSubdivisions) q.d.level = maxSubdivisions;


        }
        else
        {
            return;
        }
    }

    private void AutoSubdivide(Quad q)
    {
        (Vector2 minV, Vector2 maxV) = q.NormaliseValues(new Vector2(0, 0), new Vector2(testTexture.width, testTexture.height));
        float f = averagePixels(minV, maxV);
        (Vector2 minV2, Vector2 maxV2) = q.NormaliseValues(new Vector2(min.x, min.z), new Vector2(max.x, max.z));

       

        Vector3 a = transform.InverseTransformPoint(new Vector3(minV2.x, transform.position.y, minV2.y));
        Vector3 b = transform.InverseTransformPoint(new Vector3(maxV2.x, transform.position.y, minV2.y));

        Vector3 c = transform.InverseTransformPoint(new Vector3(minV2.x, transform.position.y, maxV2.y));
        Vector3 d = transform.InverseTransformPoint(new Vector3(maxV2.x, transform.position.y, maxV2.y));

        //Not actually accurate (need to do with a better method
        if (!vertexArray.Contains(a)) {  vertexArray.Add(a); trianglesInConnections.Add(vertexArray.Count - 1, new List<int>()); quadVertRelationship.Add(vertexArray.Count - 1, q);  indexFromVector.Add(a, vertexArray.Count - 1); UVs.Add(new Vector2(q.minX / 100, q.minY / 100)); heightWithIndex.Add(vertexArray.Count - 1, f); terrainVertexReference.Add(vertexArray.Count-1, 0); }
        if (!vertexArray.Contains(b)) { vertexArray.Add(b); trianglesInConnections.Add(vertexArray.Count - 1, new List<int>()); quadVertRelationship.Add(vertexArray.Count - 1, q); indexFromVector.Add(b, vertexArray.Count - 1); UVs.Add(new Vector2(q.maxX / 100, q.minY / 100)); heightWithIndex.Add(vertexArray.Count - 1, f); terrainVertexReference.Add(vertexArray.Count - 1, 0); }
        if (!vertexArray.Contains(c)) { vertexArray.Add(c); trianglesInConnections.Add(vertexArray.Count - 1, new List<int>()); quadVertRelationship.Add(vertexArray.Count - 1, q); indexFromVector.Add(c, vertexArray.Count - 1); UVs.Add(new Vector2(q.minX / 100, q.maxY / 100)); heightWithIndex.Add(vertexArray.Count - 1, f); terrainVertexReference.Add(vertexArray.Count - 1, 0); }
        if (!vertexArray.Contains(d)) { vertexArray.Add(d); trianglesInConnections.Add(vertexArray.Count - 1, new List<int>()); quadVertRelationship.Add(vertexArray.Count - 1, q); indexFromVector.Add(d, vertexArray.Count - 1); UVs.Add(new Vector2(q.maxX / 100, q.maxY / 100)); heightWithIndex.Add(vertexArray.Count - 1, f); terrainVertexReference.Add(vertexArray.Count - 1, 0); }

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

    private float averagePixels (Vector2 min, Vector2 max)
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

    private void OnDrawGizmos()
    {
        if (drawGizmos)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(min, 0.3f);
            Gizmos.DrawWireSphere(max, 0.3f);
            DrawQuad(parentQuad);

            for (int i = 0; i < vertexArray.Count; i++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.TransformPoint(vertexArray[i]), 0.05f);
            }
        }
    }

    private void DrawQuad(Quad q)
    {
        float xMinVal = min.x + (max.x - min.x) * q.minX / 100;
        float xMaxval = min.x + (max.x - min.x) * q.maxX / 100;
        float yMinVal = min.y + (max.y - min.y) * q.minY / 100;
        float yMaxVal = min.y + (max.y - min.y) * q.maxY / 100;

        if (zOrientation) { 
             xMinVal = min.x + (max.x - min.x) * q.minX/100;
             xMaxval = min.x + (max.x - min.x) * q.maxX/100;
             yMinVal = min.z + (max.z - min.z) * q.minY / 100;
             yMaxVal = min.z +  (max.z - min.z) * q.maxY / 100;
        }
        Gizmos.color = Color.green;

        (Vector2 minV, Vector2 maxV) = q.NormaliseValues(new Vector2(0, 0), new Vector2(testTexture.width, testTexture.height));

        if (maxV.x - minV.x < maxDimensions || maxV.y - minV.y < maxDimensions)
            return;

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

    [System.Serializable]
    class Quad
    {
        [HideInInspector]
        public Quad Parent;
        public string parentLetter;

        public int level = 0;

        //Based on percentages
        public List<Vector2> cracks;

        public List<Vector2> rightCracks;
        public List<Vector2> leftCracks;
        public List<Vector2> topCracks;
        public List<Vector2> bottomCracks;

        public List<Vector2> ReturnAllCracks()
        {
            List<Vector2> temp = cracks;

            for (int i = 0; i < subQuads.Count; i++)
            {
                temp.AddRange(subQuads[i].ReturnAllCracks());
            }

            return temp;
        }

        public List<Vector2> ReturnCrackDirection(string dir)
        {
            List<Vector2> tempCracks = new List<Vector2>();
            if (dir == "R")
            {
                tempCracks.AddRange(rightCracks);

                //Rights
                if (b != null)
                {
                    tempCracks.AddRange(b.ReturnCrackDirection(dir));
                    tempCracks.AddRange(this.d.ReturnCrackDirection(dir));
                }
                return tempCracks;
            }

            else if (dir == "L")
            {
                tempCracks.AddRange(leftCracks);

                if (b != null)
                {
                    tempCracks.AddRange(a.ReturnCrackDirection(dir));
                    tempCracks.AddRange(c.ReturnCrackDirection(dir));
                }
                return tempCracks;

            }

            else if (dir == "U")
            {
                tempCracks.AddRange(topCracks);

                if (b != null)
                {
                    tempCracks.AddRange(a.ReturnCrackDirection(dir));
                    tempCracks.AddRange(b.ReturnCrackDirection(dir));
                }
                return tempCracks;

            }

            else if (dir == "D")
            {
                tempCracks.AddRange(bottomCracks);

                if (b != null)
                {
                    tempCracks.AddRange(d.ReturnCrackDirection(dir));
                    tempCracks.AddRange(c.ReturnCrackDirection(dir));
                }
                return tempCracks;

            }

            return null;

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


        public (Vector2, Vector2) NormaliseValues(Vector2 inputMin, Vector2 inputMax)
        {
            float xMinVal = inputMin.x + (inputMax.x - inputMin.x) * minX / 100;
            float xMaxval = inputMin.x + (inputMax.x - inputMin.x) * maxX / 100;
            float yMinVal = inputMin.y + (inputMax.y - inputMin.y) * minY / 100;
            float yMaxVal = inputMin.y + (inputMax.y - inputMin.y) * maxY / 100;

            return (new Vector2(xMinVal, yMinVal), new Vector2(xMaxval, yMaxVal));
        }

        public (Vector2, Vector2, Vector2, Vector2) NormalisedCorners(Vector2 inputMin, Vector2 inputMax, float y)
        {
            (Vector2 minV2, Vector2 maxV2) = NormaliseValues(inputMin, inputMax);

            Vector3 a = new Vector3(minV2.x, y, minV2.y);
            Vector3 b = new Vector3(maxV2.x, y, minV2.y);

            Vector3 c = new Vector3(minV2.x, y, maxV2.y);
            Vector3 d = new Vector3(maxV2.x, y, maxV2.y);

            return (a, b, c, d);

        }

        public (Vector2, Vector2, Vector2, Vector2) NormalisedCracks(Vector2 inputMin, Vector2 inputMax, float y)
        {

            return (Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        }

        public Vector2 Normalisedcenter(Vector2 inputMin, Vector2 inputMax, float y)
        {

            return Vector2.zero;
        }
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

        //Return quads with correct bounds
        public List<Quad> SubdividedQuads()
        {
            cracks.Clear();

            Quad upperLeft = new Quad(minX, minY, minX + (maxX - minX) / 2, minY + (maxY - minY) / 2);
            upperLeft.Parent = this;
            upperLeft.parentLetter = "A";

            Quad upperRight = new Quad(minX + (maxX - minX) / 2, minY, maxX, minY + (maxY - minY) / 2);
            upperRight.Parent = this;
            upperRight.parentLetter = "B";

            Quad lowerLeft = new Quad(minX, minY + (maxY - minY) / 2, minX + (maxX - minX) / 2, maxY);
            lowerLeft.Parent = this;
            lowerLeft.parentLetter = "C";

            Quad lowerRight = new Quad(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2, maxX, maxY);
            lowerRight.Parent = this;
            lowerRight.parentLetter = "D";

            cracks.Add(new Vector2(upperLeft.minX, upperLeft.maxY));
            cracks.Add(new Vector2(upperLeft.maxX, upperLeft.minY));
            cracks.Add(new Vector2(upperRight.maxX, upperRight.maxY));
            cracks.Add(new Vector2(upperRight.minX, upperRight.maxY));

            leftCracks.Add(new Vector2(a.minX, a.maxY));
            rightCracks.Add(new Vector2(b.maxX, b.maxY));
            bottomCracks.Add(new Vector2(c.maxX, b.maxY));
            topCracks.Add(new Vector2(a.maxX, a.minY));

            return new List<Quad>(new Quad[] { upperLeft, upperRight, lowerLeft, lowerRight });
        }

        public void SubdivideQuad()
        {
            /* Quad upperLeft = new Quad(minX, minY, minX + (maxX - minX) / 2, minY + (maxY - minY) / 2);
             Quad upperRight = new Quad(minX + (maxX - minX) / 2, minY, maxX, minY + (maxY - minY) / 2);
             Quad lowerLeft = new Quad(minX, minY + (maxY - minY) / 2, minX + (maxX - minX) / 2, maxY);
             Quad lowerRight = new Quad(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2, maxX, maxY); */

            Quad upperLeft = new Quad(minX, minY, minX + (maxX - minX) / 2, minY + (maxY - minY) / 2);
            upperLeft.Parent = this;
            upperLeft.parentLetter = "A";

            Quad upperRight = new Quad(minX + (maxX - minX) / 2, minY, maxX, minY + (maxY - minY) / 2);
            upperRight.Parent = this;
            upperRight.parentLetter = "B";

            Quad lowerLeft = new Quad(minX, minY + (maxY - minY) / 2, minX + (maxX - minX) / 2, maxY);
            lowerLeft.Parent = this;
            lowerLeft.parentLetter = "C";

            Quad lowerRight = new Quad(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2, maxX, maxY);
            lowerRight.Parent = this;
            lowerRight.parentLetter = "D";



            a = upperLeft;
            b = upperRight;
            c = lowerLeft;
            d = lowerRight;
            a.level = level + 1;
            b.level = level + 1;
            c.level = level + 1;
            d.level = level + 1;

        }

        public List<Vector2> EdgePercentages(string direction)
        {
            if (direction == "A" && Parent != null)
            {
                List<Vector2> A = Parent.FindRecursiveLeft(null);
                List<Vector2> B = Parent.FindRecursiveTop(null);
                A.AddRange(B);
                return A;
            }

            if (direction == "B" && Parent != null)
            {
                List<Vector2> A = Parent.FindRecursiveRight(null);
                List<Vector2> B = Parent.FindRecursiveTop(null);
                A.AddRange(B);
                return A;
            }

            if (direction == "C" && Parent != null)
            {
                List<Vector2> A = Parent.FindRecursiveLeft(null);
                List<Vector2> B = Parent.FindRecursiveBottom(null);
                A.AddRange(B);
                return A;
            }

            if (direction == "D" && Parent != null)
            {
                List<Vector2> A = Parent.FindRecursiveRight(null);
                List<Vector2> B = Parent.FindRecursiveBottom(null);
                A.AddRange(B);
                return A;
            }

            Debug.LogError("There's an error with your input buddy.");
            return null;
        }

        public List<Vector2> FindRecursiveRight(Quad starting)
        {
            List<Vector2> temp = new List<Vector2>();

            if (b != null)
            {
                if (b.subQuads.Count <= 0)
                {
                    temp.Add(new Vector2(b.maxX, b.maxY));
                    return temp;
                }
                else { }
            }

            if (d != null)
            {
                if (d.subQuads.Count <= 0)
                {
                    temp.Add(new Vector2(d.minX, d.minY));
                    return temp;
                }
                else
                {

                }
            }

            return temp;
        }

        public List<Vector2> FindRecursiveLeft(Quad starting)
        {
            List<Vector2> temp = new List<Vector2>();

            if (a != null)
            {
                if (a.subQuads.Count <= 0)
                {
                    temp.Add(new Vector2(a.minX, a.maxY));
                    return temp;
                }
                else { }
            }

            if (c != null)
            {
                if (c.subQuads.Count <= 0)
                {
                    temp.Add(new Vector2(c.minX, c.minY));
                    return temp;
                }
                else
                {

                }
            }

            return temp;
        }

        public List<Vector2> FindRecursiveBottom(Quad starting)
        {
            List<Vector2> temp = new List<Vector2>();
            if (c != null)
            {
                if (c.subQuads.Count <= 0)
                {
                    temp.Add(new Vector2(c.maxX, a.maxY));
                    return temp;
                }
                else { }
            }

            if (d != null)
            {
                if (d.subQuads.Count <= 0)
                {
                    temp.Add(new Vector2(d.minX, d.maxY));
                    return temp;
                }
                else
                {

                }
            }

            return temp;
        }

        public List<Vector2> FindRecursiveTop(Quad starting)
        {
            List<Vector2> temp = new List<Vector2>();

            if (a != null)
            {
                if (a.subQuads.Count <= 0)
                {
                    temp.Add(new Vector2(a.maxX, a.minY));
                    return temp;
                }
                else { }
            }

            if (b != null)
            {
                if (b.subQuads.Count <= 0)
                {
                    temp.Add(new Vector2(b.minX, b.minY));
                    return temp;
                }
                else
                {

                }
            }

            return temp;
        }

    }

}

//Isn't optimised at the moment

