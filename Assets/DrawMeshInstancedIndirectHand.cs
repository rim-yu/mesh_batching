using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawMeshInstancedIndirectHand : MonoBehaviour {
    public int numberOfHandAvatar;
    public float range;
    Vector3 boundsSize;

    public Material material;
    public ComputeShader computeShader;

    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;

    public Mesh mesh;
    private Bounds bounds;

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
        args[1] = (uint)numberOfHandAvatar;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments); 
        argsBuffer.SetData(args);

        // Initialize buffer with the given population.
        properties = new MeshProperty[numberOfHandAvatar];

        // BigAgent 
        for (int i = 0; i < numberOfHandAvatar; i++)
        {
            MeshProperty prop = new MeshProperty();
            Vector3 position = new Vector3(Random.Range(-range / 2.0f, range / 2.0f), Random.Range(-range / 2.0f, range / 2.0f), Random.Range(-range / 2.0f, range / 2.0f));
            Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
            Vector3 scale = new Vector3(5.0f, 5.0f, 5.0f);

            prop.mat = Matrix4x4.TRS(position, rotation, scale);
            prop.color = new Color(0.0f, 0.0f, 0.0f); 
            prop.velocity = new Vector3(0.0f, 0.0f, 0.0f);

            properties[i] = prop;
        }

        meshPropertiesBuffer = new ComputeBuffer(numberOfHandAvatar, MeshProperty.Size());
        meshPropertiesBuffer.SetData(properties);
        computeShader.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);
        material.SetBuffer("_Properties", meshPropertiesBuffer);
    }

    private void Start() {
        Setup();
    }

    private void Update() {
        computeShader.SetInt("_numberOfHandAvatar", numberOfHandAvatar);
        computeShader.SetVector("_boundSize", boundsSize);

        computeShader.Dispatch(kernel, Mathf.CeilToInt(numberOfHandAvatar / 64f), 1, 1);

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
    }
}