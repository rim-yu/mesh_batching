using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawMeshInstancedIndirectDemo : MonoBehaviour {
    public int population;
    public int numberOfBigAgent = 27;
    public float range;
    public float deltaTime;
    public float neighborRadius;

    [Header("Cohesion")]
    public float cohWeight;

    [Header("Alignment")]
    public float alignWeight;

    [Header("Avoidance")]
    public float avoidWeight;
    public float avoidanceRadius;
    public float minDistance;

    Vector3 boundsSize;

    [Header(" ")]
    public Material material;
    public ComputeShader computeShader;

    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;

    public Mesh mesh;
    private Bounds bounds;

    Vector3[] currentVelocityArray;
    Vector3[] cohVelocityArray;
    Vector3[] alignVelocityArray;
    Vector3[] avoidVelocityArray;

    private ComputeBuffer currentVelocityBuffer;
    private ComputeBuffer cohVelocityBuffer;
    private ComputeBuffer alignVelocityBuffer;
    private ComputeBuffer avoidVelocityBuffer;

    int kernel;
    MeshProperty[] properties;

    private struct MeshProperty
    { 
        public Matrix4x4 mat;
        public Vector4 color;
        public Vector3 velocity;

        public static int Size() {
            return
                sizeof(float) * 4 * 4 + // mat
                sizeof(float) * 4 +     // color
                sizeof(float) * 3;      // velocity 
        }
    }
    private void Setup() {
        bounds = new Bounds(this.gameObject.transform.position, Vector3.one * (range + 1));
        boundsSize = Vector3.one * (range + 1);
        InitializeBuffers(); 
    }

    private void InitializeBuffers() { 
        kernel = computeShader.FindKernel("CSMain"); 

        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)population;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments); 
        argsBuffer.SetData(args);

        // Initialize buffer with the given population.
        properties = new MeshProperty[population];

        // BigAgent 
        for (int i = 0; i < numberOfBigAgent; i++)
        {
            MeshProperty prop = new MeshProperty();
            Vector3 position = new Vector3(Random.Range(-range / 2.0f, range / 2.0f), Random.Range(-range / 2.0f, range / 2.0f), Random.Range(-range / 2.0f, range / 2.0f));
            Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
            Vector3 scale = new Vector3(0.2f, 0.2f, 0.2f);

            prop.mat = Matrix4x4.TRS(position, rotation, scale);
            prop.color = Color.HSVToRGB(i * (1.0f / numberOfBigAgent), 1.0f, 1.0f); 
            prop.velocity = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f));

            properties[i] = prop;
        }

        // Agent
        for (int i = numberOfBigAgent; i < population - numberOfBigAgent; i++)
        {
            MeshProperty prop = new MeshProperty();
            Vector3 position = new Vector3(Random.Range(-range / 2.0f, range / 2.0f), Random.Range(-range / 2.0f, range / 2.0f), Random.Range(-range / 2.0f, range / 2.0f));
            Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
            Vector3 scale = new Vector3(0.1f, 0.1f, 0.1f);

            prop.mat = Matrix4x4.TRS(position, rotation, scale);
            prop.color = Color.HSVToRGB(Random.Range(0.65f, 0.8f), Random.Range(0.4f, 0.6f), 1);
            prop.velocity = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f));

            properties[i] = prop;
        }

        meshPropertiesBuffer = new ComputeBuffer(population, MeshProperty.Size());
        meshPropertiesBuffer.SetData(properties);
        computeShader.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);
        material.SetBuffer("_Properties", meshPropertiesBuffer);

        // For Debug.
        //currentVelocityArray = new Vector3[population];
        //currentVelocityBuffer = new ComputeBuffer(population, 12);
        //currentVelocityBuffer.SetData(currentVelocityArray);
        //computeShader.SetBuffer(kernel, "_currentVelocityBuffer", currentVelocityBuffer);

        //cohVelocityArray = new Vector3[population];
        //cohVelocityBuffer = new ComputeBuffer(population, 12);
        //cohVelocityBuffer.SetData(cohVelocityArray);
        //computeShader.SetBuffer(kernel, "_cohVelocityBuffer", cohVelocityBuffer);

        //alignVelocityArray = new Vector3[population];
        //alignVelocityBuffer = new ComputeBuffer(population, 12);
        //alignVelocityBuffer.SetData(alignVelocityArray);
        //computeShader.SetBuffer(kernel, "_alignVelocityBuffer", alignVelocityBuffer);

        //avoidVelocityArray = new Vector3[population];
        //avoidVelocityBuffer = new ComputeBuffer(population, 12);
        //avoidVelocityBuffer.SetData(avoidVelocityArray);
        //computeShader.SetBuffer(kernel, "_avoidVelocityBuffer", avoidVelocityBuffer);
    }

    private void Start() {
        Setup();
    }

    private void Update() { 
        computeShader.SetFloat("_neighborRadius", neighborRadius);
        computeShader.SetFloat("_deltaTime", deltaTime);
        computeShader.SetInt("_population", population); 
        computeShader.SetFloat("_avoidanceRadius", avoidanceRadius);
        computeShader.SetFloat("_cohWeight", cohWeight);
        computeShader.SetFloat("_alignWeight", alignWeight);
        computeShader.SetFloat("_avoidWeight", avoidWeight);
        computeShader.SetFloat("_minDistance", minDistance);
        computeShader.SetVector("_boundSize", boundsSize);
        computeShader.SetInt("_numberOfBigAgent", numberOfBigAgent);

        computeShader.Dispatch(kernel, Mathf.CeilToInt(population / 64f), 1, 1);
       
        // For Debug.
        //currentVelocityBuffer.GetData(currentVelocityArray); 
        //cohVelocityBuffer.GetData(cohVelocityArray); // cohesion velocity buffer
        //alignVelocityBuffer.GetData(alignVelocityArray); // alignment velocity buffer
        //avoidVelocityBuffer.GetData(avoidVelocityArray); // avoidance velocity buffer

        //for (int i = 0; i < population; i++)
        //{
            //Debug.Log($"currentVelocity[{i}] = {currentVelocityArray[i]}");
            //Debug.Log($"cohVelocity[{i}] = {cohVelocityArray[i]}");
            //Debug.Log($"alignVelocity[{i}] = {alignVelocityArray[i]}");
            //Debug.Log($"avoidVelocity[{i}] = {avoidVelocityArray[i]}");
        //}
///////////
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private void OnDisable()
    {
        if (meshPropertiesBuffer != null)
        {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
        argsBuffer = null;
    }

    private void FixedUpdate()
    {
        // Debug.Log(Time.deltaTime);
    }
}