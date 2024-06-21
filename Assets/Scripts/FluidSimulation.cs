using System;
using UnityEngine;
using UnityEngine.Rendering;

public class FluidSimulation : MonoBehaviour
{
    public ComputeShader AdvectShader;
    public ComputeShader SolveShader;
    public ComputeShader DivergeShader;
    public ComputeShader GradientShader;
    public ComputeShader AddDensityShader;
    public ComputeShader AddVelocityShader;
    public ComputeShader CopyShader;
    public ComputeShader RenderizeDensityShader;

    //It's like temp.. but it is not.
    public ComputeBuffer AdvectionStorage;
    public ComputeBuffer SolutionStorage;
    public ComputeBuffer DivergenceStorage;
    public ComputeBuffer GradientStorage;
    public ComputeBuffer ResultingDensity;
    public ComputeBuffer ResultingVelocity;

    //The quantities that the compute shader will read from, and also, finally, store in.
    public ComputeBuffer Pressure;
    public ComputeBuffer PreviousVelocity;
    public ComputeBuffer Velocity;
    public ComputeBuffer Density;
    public ComputeBuffer PreviousDensity;

    //public RenderTexture RenderDensity;

    //Constants.
    public int N;
    public float TimeStep;
    public int Iterations;
    public float Diffusion;
    public float Viscosity;

    public GameObject[,,] Cubes;
    public Renderer[,,] CubeRenderers;

    void Start()
    {
        InitializeQuantities();
        BindMaterial();


    }

    void Update()
    {
        Pipeline();
    }

    void BindMaterial()
    {
        //material = Cube.GetComponent<Renderer>().material;
        //material.SetTexture("_VolumeTex", RenderDensity);
    }


    void InitializeQuantities()
    {
        N = 32;
        TimeStep = 1f;
        Iterations = 50;
        Diffusion = 0f;
        Viscosity = 0f;

        // Initialize RenderTexture
        Velocity = CreateComputeBuffer(N);
        PreviousVelocity = CreateComputeBuffer(N);
        Pressure = CreateComputeBuffer(N);
        Density = CreateComputeBuffer(N);
        PreviousDensity = CreateComputeBuffer(N);

        //RenderDensity = CreateRenderTexture(N);
        //InitializeRenderTexture(RenderDensity, UnityEngine.Color.black);
        // Initialize Velocity
        SetRandomComputeBufferData(Velocity);
        SetRandomComputeBufferData(PreviousVelocity);
        SetRandomComputeBufferData(Density);
        SetRandomComputeBufferData(PreviousDensity);
        SetRandomComputeBufferData(Pressure);


        AdvectionStorage = CreateComputeBuffer(N);
        SolutionStorage = CreateComputeBuffer(N);
        DivergenceStorage = CreateComputeBuffer(N);
        GradientStorage = CreateComputeBuffer(N);
        ResultingDensity = CreateComputeBuffer(N);
        ResultingVelocity = CreateComputeBuffer(N);

        Cubes = new GameObject[N, N, N];
        CubeRenderers = new Renderer[N, N, N];
        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < N; y++)
            {
                for (int z = 0; z < N; z++)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = new Vector3(x, y, z);
                    Cubes[x, y, z] = cube;

                    Renderer cubeRenderer = cube.GetComponent<Renderer>();
                    CubeRenderers[x, y, z] = cubeRenderer;
                }
            }
        }

    }

    ComputeBuffer CreateComputeBuffer(int N)
    {
        ComputeBuffer cb = new ComputeBuffer(N * N * N, sizeof(float) * 3);
        return cb;
    }

    void SetComputeBufferData(ComputeBuffer cb)
    {
        int size = N * N * N;
        Vector3[] data = new Vector3[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = new Vector3(0, 0, 0);
        }
        cb.SetData(data);
    }

    void SetRandomComputeBufferData(ComputeBuffer cb)
    {
        int size = N * N * N;
        System.Random rand = new System.Random();
        Vector3[] data = new Vector3[size];
        for (int i = 0; i < size; i++)
        {
            float x = (float)rand.NextDouble();
            float y = (float)rand.NextDouble();
            float z = (float)rand.NextDouble();
            data[i] = new Vector3(x, y, z);
        }
        cb.SetData(data);
    }

    void InitializeRenderTexture(RenderTexture rt, UnityEngine.Color color)
    {
        Texture3D t = CreateTexture3D(N);
        for (int z = 0; z < t.depth; z++)
        {
            for (int y = 0; y < t.height; y++)
            {
                for (int x = 0; x < t.width; x++)
                {
                    t.SetPixel(x, y, z, color);
                }
            }
        }
            
        t.Apply();
        Graphics.Blit(t, rt);
    }

    void InitializeRandomRenderTexture(RenderTexture rt)
    {
        Texture3D t = CreateTexture3D(N);
        for(int z = 0; z < t.depth; z++)
        {
            for (int y = 0; y < t.height; y++)
            {
                for (int x = 0; x < t.width; x++)
                {
                    float randomValue = UnityEngine.Random.value;
                    t.SetPixel(x, y, z, new UnityEngine.Color(randomValue, randomValue, randomValue, 1.0f));
                }
            }
        }

        t.Apply();
        //Copy3D(t, rt);
    }

    Texture3D CreateTexture3D(int size)
    {
        var texture = new Texture3D(size, size, size, TextureFormat.RGBAFloat, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        return texture;
    }

    RenderTexture CreateRenderTexture(int size)
    {
        RenderTexture rt = new RenderTexture(size, size, 0)
        {
            enableRandomWrite = true,
            // Optionally set the format:
            // format = RenderTextureFormat.ARGBFloat
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = size
        };
        rt.Create();
        return rt;
    }


    void InitializeRandomTexture2D(Texture2D texture)
    {
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, new UnityEngine.Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1.0f));
            }
        }
        texture.Apply();
    }

    void InitializeTexture2D(Texture2D texture, UnityEngine.Color color)
    {
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
    }



    void DispatchShader(ComputeShader shader, int kernel)
    {
        int threadGroupsX = Mathf.CeilToInt(N / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(N / 8.0f);
        int threadGroupsZ = Mathf.CeilToInt(N / 8.0f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ);
    }

    ComputeBuffer Solve(ComputeBuffer x, ComputeBuffer x0, float a, float c)
    {
        int kernel = SolveShader.FindKernel("Solve");
        SolveShader.SetBuffer(kernel, "Solution", SolutionStorage);
        SolveShader.SetBuffer(kernel, "X", x);
        SolveShader.SetBuffer(kernel, "X0", x0);
        SolveShader.SetFloat("A", a);
        SolveShader.SetFloat("C", c);
        SolveShader.SetInt("Iterations", Iterations);
        //for (int i = 0; i < Iterations; i++)
        //{
            DispatchShader(SolveShader, kernel);
            //Copy(SolutionStorage, x);
        //}
        return SolutionStorage;
    }

    ComputeBuffer Diverge(ComputeBuffer velocity)
    {
        int kernel = DivergeShader.FindKernel("Diverge");
        DivergeShader.SetBuffer(kernel, "Divergence", DivergenceStorage);
        DivergeShader.SetBuffer(kernel, "Velocity", velocity);
        DivergeShader.SetInt("N", N);
        DispatchShader(DivergeShader, kernel);
        return DivergenceStorage;
    }

    ComputeBuffer Gradient(ComputeBuffer velocity, ComputeBuffer pressure)
    {
        int kernel = GradientShader.FindKernel("Gradient");
        GradientShader.SetBuffer(kernel, "ResultingVelocity", GradientStorage);
        GradientShader.SetBuffer(kernel, "Pressure", pressure);
        GradientShader.SetBuffer(kernel, "Velocity", velocity);
        GradientShader.SetInt("N", N);
        DispatchShader(GradientShader, kernel);
        return GradientStorage;
    }

    ComputeBuffer Advect(ComputeBuffer quantity, ComputeBuffer toAdvectOverVelocity)
    {
        int kernel = AdvectShader.FindKernel("Advect");
        AdvectShader.SetBuffer(kernel, "Quantity", AdvectionStorage);
        AdvectShader.SetBuffer(kernel, "ToAdvectQuantity", quantity);
        AdvectShader.SetBuffer(kernel, "ToAdvectOverVelocity", toAdvectOverVelocity);
        AdvectShader.SetFloat("TimeStep", TimeStep);
        AdvectShader.SetInt("N", N);
        DispatchShader(AdvectShader, kernel);

        return AdvectionStorage;
    }

    ComputeBuffer Diffuse(ComputeBuffer x, ComputeBuffer x0, float diffusion)
    {
        float a = TimeStep * diffusion * (N - 2) * (N - 2);
        return Solve(x, x0, a, 1 + 6 * a);
    }

    ComputeBuffer Project(ComputeBuffer velocity)
    {
        ComputeBuffer divergence = Diverge(velocity);

        ComputeBuffer solvedPressure = Solve(Pressure, divergence, 1, 6);

        return Gradient(velocity, solvedPressure);
    }

    public void Copy(ComputeBuffer source, ComputeBuffer target)
    {
        int kernel = CopyShader.FindKernel("Copy");
        CopyShader.SetBuffer(kernel, "Target", target);
        CopyShader.SetBuffer(kernel, "Source", source);
        CopyShader.SetInt("N", N);
        DispatchShader(CopyShader, kernel);
    }

    public void RenderizeDensity(ComputeBuffer density, RenderTexture renderDensity)
    {
        int kernel = RenderizeDensityShader.FindKernel("RenderizeDensity");
        RenderizeDensityShader.SetTexture(kernel, "Target", renderDensity);
        RenderizeDensityShader.SetBuffer(kernel, "Source", density);
        RenderizeDensityShader.SetInt("N", N);
        DispatchShader(RenderizeDensityShader, kernel);
    }

    public void Pipeline()
    {
        //Diffuse Previous Velocity over The current Velocity (Result is stored in previous velocity)
        ComputeBuffer diffusedPreviousVelocity = Diffuse(PreviousVelocity, Velocity, Viscosity);
        //Project diffused Previous Velocity (Result is stored in previous velocity).
        ComputeBuffer correctedPreviousVelocity = Project(diffusedPreviousVelocity);
        Copy(correctedPreviousVelocity, PreviousVelocity);

        //Advect current velocity over previous velocity (result is stored in current velocity)
        ComputeBuffer advectedCurrentVelocity = Advect(Velocity, PreviousVelocity);
        //Project current advected velocity (Result is stored in current velocity)
        ComputeBuffer correctedCurrentVelocity = Project(advectedCurrentVelocity);
        Copy(correctedCurrentVelocity, Velocity);

        //Diffuse Previous Density over The current Density (Result is stored in previous Density)
        ComputeBuffer diffusedPreviousDensity = Diffuse(PreviousDensity, Density, Diffusion);
        Copy(diffusedPreviousDensity, PreviousDensity);

        //Advect current Density over current velocity (result is stored in current density)
        ComputeBuffer advectedCurrentDensity = Advect(Density, Velocity);
        Copy(advectedCurrentDensity, Density);
        //Vector3[] densityData = new Vector3[N * N * N];
        //Density.GetData(densityData);

        AsyncGPUReadback.Request(Density, OnCompleteReadBack);

        void OnCompleteReadBack(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                var data = request.GetData<Vector3>();
                for (int x = 0; x < N; x++)
                {
                    for (int y = 0; y < N; y++)
                    {
                        for (int z = 0; z < N; z++)
                        {
                            Vector3 currentColor = data[Index(x, y, z)];
                            CubeRenderers[x, y, z].material.color = new Color(currentColor.x, currentColor.y, currentColor.z, 1);
                        }
                    }
                }

            }
        }


    }

    int Index(int x, int y, int z)
    {
        return x + (y * N) + (y * N * N);
    }

}
