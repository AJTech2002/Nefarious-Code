using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using System.Threading.Tasks;
using Unity.Mathematics;

public class OptimisedJobTest : MonoBehaviour
{
    [Header("Wave Parameters")]
    public float waveScale;
    public float waveOffsetSpeed;
    public float waveHeight;

    [Header("References and Prefabs")]
    public MeshFilter waterMeshFilter;
    public Transform spawnPrefab;
    private Mesh waterMesh;
    public MeshCollider waterCollider;

    //Private Mesh Job Properties
    NativeArray<Vector3> waterVertices;
    NativeArray<Vector3> waterNormals;

    Vector3[] waterModifiedVertices;
    Vector3[] waterModifiedNormals;

    //Job Handles
    UpdateMeshJob meshModificationJob;
    JobHandle meshModificationJobHandle;

    private void Start()
    {
        InitialiseData();   
    }



    //This is where the appropriate mesh verticies are loaded in
    private void InitialiseData()
    {
        
        //This allows 
        waterMesh = waterMeshFilter.mesh;
        waterMesh.MarkDynamic();

        //The verticies will be reused throughout the life of the program so the Allocator has to be set to Persistent
        waterVertices = new NativeArray<Vector3>(waterMesh.vertices, Allocator.Persistent);
        waterNormals = new NativeArray<Vector3>(waterMesh.normals, Allocator.Persistent);

        waterModifiedNormals = new Vector3[waterNormals.Length];
        waterModifiedVertices = new Vector3[waterVertices.Length];

    }

    private void Update()
    {
        meshModificationJob = new UpdateMeshJob()
        {
            vertices = waterVertices,
            normals = waterNormals,
            offsetSpeed = waveOffsetSpeed,
            time = Time.time,
            scale = waveScale,
            height = waveHeight
        };

        meshModificationJobHandle = meshModificationJob.Schedule(waterVertices.Length, 64);
    }

    private void LateUpdate()
    {
        meshModificationJobHandle.Complete();

        // copy our results to managed arrays so we can assign them
        meshModificationJob.vertices.CopyTo(waterModifiedVertices);
        meshModificationJob.normals.CopyTo(waterModifiedNormals);

        waterMesh.vertices = waterModifiedVertices;
        
        //Most expensive
        waterMesh.RecalculateNormals();
        waterMesh.RecalculateBounds();

        waterCollider.sharedMesh = waterMesh;
        

    }

   

    private void OnDestroy()
    {
        // make sure to Dispose() any NativeArrays when we're done
        waterVertices.Dispose();
        waterNormals.Dispose();
    }

    [BurstCompile]
    private struct UpdateMeshJob : IJobParallelFor
    {
        public NativeArray<Vector3> vertices;
        public NativeArray<Vector3> normals;

        [ReadOnly]
        public float offsetSpeed;

        [ReadOnly]
        public float time;

        [ReadOnly]
        public float scale;

        [ReadOnly]
        public float height;

        public void Execute(int i)
        {
            //Vertex values are always between -1 and 1 (facing partially upwards)
            if (normals[i].z > 0f)
            {
                var vertex = vertices[i];

                float noiseValue = Noise(((vertex.x * scale + 1) / 2) + offsetSpeed * time, ((vertex.z * scale) + 1) / 2);

                vertices[i] = new Vector3(vertex.x, vertex.y, noiseValue * height + 0.3f);
            }

        }

        private float Noise(float x, float y)
        {
            float2 pos = math.float2(x, y);
            return noise.snoise(pos);
        }


    }

   
}
