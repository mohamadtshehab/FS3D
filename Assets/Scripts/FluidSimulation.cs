using System;
using UnityEngine;
using UnityEngine.Rendering;

public class FluidSimulation : MonoBehaviour
{
    public ComputeShader AdvectShader;
    public ComputeShader SolveShader;
    public ComputeShader DivergeShader;
    public ComputeShader GradientShader;
    private ComputeShader AddDensityShader;
    private ComputeShader AddVelocityShader;
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

    public ComputeBuffer Solid;

    public RenderTexture DensityTexture;
    public Material VolumeMaterial;
    public GameObject Cube;
    //public RenderTexture RenderDensity;

    //Constants.
    public int N;
    public float TimeStep;
    public int Iterations;
    public float Diffusion;
    public float Viscosity;
    public int SliceCount;
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
        for (int i = 0; i < SliceCount; i++)
        {
            float sliceDepth = (float)i / (SliceCount - 1);

            GameObject slice = GameObject.CreatePrimitive(PrimitiveType.Quad);
            slice.transform.SetParent(transform, false);
            slice.transform.localPosition = new Vector3(0, 0, sliceDepth);
            slice.transform.localScale = new Vector3(1, 1, 1);

            Material sliceMaterial = new Material(Shader.Find("Custom/VolumeShader"));
            sliceMaterial.SetTexture("_VolumeTex", DensityTexture);
            sliceMaterial.SetFloat("_Slice", (float)i / (SliceCount - 1));

            Renderer sliceRenderer = slice.GetComponent<Renderer>();
            sliceRenderer.material = sliceMaterial;
        }
    }



    void InitializeQuantities()
    {

        // Initialize RenderTexture
        Velocity = CreateComputeBuffer();
        PreviousVelocity = CreateComputeBuffer();
        Pressure = CreateComputeBuffer();
        Density = CreateComputeBuffer();
        PreviousDensity = CreateComputeBuffer();

        //RenderDensity = CreateRenderTexture(N);
        //InitializeRenderTexture(RenderDensity, UnityEngine.Color.black);
        // Initialize Velocity
        SetRandomComputeBufferData(Velocity);
        SetRandomComputeBufferData(PreviousVelocity);
        SetRandomComputeBufferData(Density);
        SetRandomComputeBufferData(PreviousDensity);
        SetRandomComputeBufferData(Pressure);


        AdvectionStorage = CreateComputeBuffer();
        SolutionStorage = CreateComputeBuffer();
        DivergenceStorage = CreateComputeBuffer();
        GradientStorage = CreateComputeBuffer();
        ResultingDensity = CreateComputeBuffer();
        ResultingVelocity = CreateComputeBuffer();

        Solid = CreateComputeBuffer("int");
        SetSolidVoxels(Solid);

        DensityTexture = CreateRenderTexture(N);
        InitializeRandomRenderTexture(DensityTexture);

        
    }

    ComputeBuffer CreateComputeBuffer(string dataType = "vector")
    {
        ComputeBuffer cb;
        if (dataType == "vector")
        {
            cb = new ComputeBuffer(N * N * N, sizeof(float) * 3);
        }
        else
        {
            cb = new ComputeBuffer(N * N * N, sizeof(int));
        }
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

    void SetSolidVoxels(ComputeBuffer cb)
    {
        int[] data = new int[N * N * N];
        int start = (int) (N / 2);
        int end = (int) (N / 2 + 5);
        for (int z = 0; z < N; ++z)
        {
            for (int y= 0; y < N; ++y)
            {
                for (int x = 0; x < N; ++x)
                {
                    if ( x >= start && x <= end && y >= start && y <= end && z >= start && z <= end)
                    {
                        data[Index(x, y, z)] = 1;
                    }
                    else
                    {
                        data[Index(x, y, z)] = 0;
                    }
                }
            }
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
            dimension = TextureDimension.Tex3D,
            volumeDepth = size
        };
        rt.Create();
        return rt;
    }


    void InitializeRandomTexture3D(Texture3D texture)
    {
        for (int z = 0; z < N; z++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, z, new UnityEngine.Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1.0f));
                }
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
        DivergeShader.SetBuffer(kernel, "Solid", Solid);
        DivergeShader.SetBuffer(kernel, "Velocity", velocity);
        DivergeShader.SetInt("N", N);
        DispatchShader(DivergeShader, kernel);
        return DivergenceStorage;
    }

    ComputeBuffer Gradient(ComputeBuffer velocity, ComputeBuffer pressure)
    {
        int kernel = GradientShader.FindKernel("Gradient");
        GradientShader.SetBuffer(kernel, "ResultingVelocity", GradientStorage);
        GradientShader.SetBuffer(kernel, "Solid", Solid);
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

        RenderizeDensity(Density, DensityTexture);
    }

    int Index(int x, int y, int z)
    {
        return x + (y * N) + (y * N * N);
    }

}
