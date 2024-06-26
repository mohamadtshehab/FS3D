using System;
using Unity.VisualScripting;
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
    public ComputeShader BuoyancyShader;

    //It's like temp.. but it is not.
    public ComputeBuffer AdvectionStorage;
    public ComputeBuffer SolutionStorage;
    public ComputeBuffer DivergenceStorage;
    public ComputeBuffer GradientStorage;
    public ComputeBuffer ResultingDensity;
    public ComputeBuffer ResultingVelocity;
    public ComputeBuffer BuoyancyStorage;
    //The quantities that the compute shader will read from, and also, finally, store in.
    public ComputeBuffer P;
    public ComputeBuffer PreviousVelocity;
    public ComputeBuffer Velocity;
    public ComputeBuffer Density;
    public ComputeBuffer PreviousDensity;
    public ComputeBuffer Temperature;

    private ComputeBuffer Solid;

    public RenderTexture DensityTexture;
    public Material VolumeMaterial;
    public GameObject Cube;
    //public RenderTexture RenderDensity;

    //Constants.
    public int N;
    public float TimeStep;
    public float Diffusion;
    public float Viscosity;
    private int SliceCount;

    public float RoomTemperature;
    public float GasMolarMass;
    public float G;
    public float Pressure;
    public float R;

    void Start()
    {
        InitializeQuantities();
        BindMaterial();

    }

    void Update()
    {
        
        Pipeline();
    }

    //public float Normalize(float value, float minValue, float maxValue)
    //{
    //    return (value - minValue) / (maxValue - minValue);
    //}
    //public void NormalizeValues()
    //{
    //    float minTemperature = -100;
    //    float maxTemperature = 100;
    //    RoomTemperature = Normalize(RoomTemperature, minTemperature, maxTemperature);

    //    float minMolarMass = 10;
    //    float maxMolarMass = 0;
    //    GasMolarMass = Normalize(GasMolarMass, minMolarMass, maxMolarMass);

    //    float minPressure = 0;
    //    float maxPressure = 50;
    //    Pressure = Normalize(GasMolarMass, minPressure, maxPressure);

    //    float minG = 0;
    //    float maxG = 20;
    //    G = Normalize(GasMolarMass, minG, maxG);

    //    float minR = 0;
    //    float maxR = 50;
    //    R = Normalize(GasMolarMass, minR, maxR);

    //    float minDiffusion = 0;
    //    float maxDiffusion = 50;
    //    R = Normalize(GasMolarMass, minR, maxR);
    //    float minR = 0;
    //    float maxR = 50;
    //    R = Normalize(GasMolarMass, minR, maxR);



    //    // Repeat for other variables...
    //}
    void BindMaterial()
    {
        SliceCount = N - 1;
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
        P = CreateComputeBuffer();
        Density = CreateComputeBuffer();
        PreviousDensity = CreateComputeBuffer();
        Temperature = CreateComputeBuffer("float");

        //RenderDensity = CreateRenderTexture(N);
        //InitializeRenderTexture(RenderDensity, UnityEngine.Color.black);
        // Initialize Velocity
        SetRandomComputeBufferData(Velocity, 1);
        SetRandomComputeBufferData(PreviousVelocity, 1);
        SetRandomComputeBufferData(Density, 0.9f);
        SetRandomComputeBufferData(PreviousDensity, 1);
        SetRandomComputeBufferData(P, 1);
        SetRandomComputeBufferData(Temperature, 23,"float");

        AdvectionStorage = CreateComputeBuffer();
        SolutionStorage = CreateComputeBuffer();
        DivergenceStorage = CreateComputeBuffer();
        GradientStorage = CreateComputeBuffer();
        ResultingDensity = CreateComputeBuffer();
        ResultingVelocity = CreateComputeBuffer();
        BuoyancyStorage = CreateComputeBuffer();

        Solid = CreateComputeBuffer("int");
        SetRandomComputeBufferData(Solid, 1, "int");

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
        else if (dataType == "int")
        {
            cb = new ComputeBuffer(N * N * N, sizeof(int));
        }
        else
        {
            cb = new ComputeBuffer(N * N * N, sizeof(float));
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

    void SetRandomComputeBufferData(ComputeBuffer cb, float factor, string dataType = "vector")
    {
        int size = N * N * N;
        int xStart = (int) (N - 20);
        int xEnd = xStart + 19 ;
        int yStart = xStart;
        int yEnd = yStart + 19;
        int zStart = xStart;
        int zEnd = zStart + 19;


        if (dataType == "vector")
        {
            System.Random rand = new System.Random();
            Vector3[] data = new Vector3[size];
            for (int z = 0; z < N; ++z)
            {
                for (int y = 0; y < N; ++y)
                {
                    for (int x = 0; x < N; ++x)
                    {
                        bool condition = x >= xStart && x <= xEnd && y >= yStart && y <= yEnd && z >= zStart && z <= zEnd;
                        if (condition == true)
                        {
                            data[Index(x, y, z)] = new Vector3(0, 0, 0);
                        }
                        else
                        {
                            float r = (float)rand.NextDouble() * factor;
                            float g = (float)rand.NextDouble() * factor;
                            float b = (float)rand.NextDouble() * factor;
                            data[Index(x, y, z)] = new Vector3(r, g, b);
                        }
                    }
                }
            }
            cb.SetData(data);
        }
        else if (dataType == "int")
        {
            int[] data = new int[N * N * N];


            for (int z = 0; z < N; ++z)
            {
                for (int y = 0; y < N; ++y)
                {
                    for (int x = 0; x < N; ++x)
                    {
                        bool condition = x >= xStart && x <= xEnd && y >= yStart && y <= yEnd && z >= zStart && z <= zEnd;
                        if (condition == true)
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

        else
        {
            System.Random rand = new System.Random();
            float[] data = new float[N * N * N];

            for (int z = 0; z < N; ++z)
            {
                for (int y = 0; y < N; ++y)
                {
                    for (int x = 0; x < N; ++x)
                    {
                        bool condition = x >= xStart && x <= xEnd && y >= yStart && y <= yEnd && z >= zStart && z <= zEnd;
                        if (condition == true)
                        {
                            data[Index(x, y, z)] = 0;
                        }
                        else
                        {
                            data[Index(x, y, z)] = (float) rand.NextDouble() * factor;
                        }
                    }
                }
            }
            cb.SetData(data);
        }
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
        DispatchShader(SolveShader, kernel);
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
        float a = TimeStep * diffusion;
        return Solve(x, x0, a, 1 + 6 * a);
    }

    ComputeBuffer Project(ComputeBuffer velocity)
    {
        ComputeBuffer divergence = Diverge(velocity);

        ComputeBuffer solvedPressure = Solve(P, divergence, 1, 6);

        return Gradient(velocity, solvedPressure);
    }

    ComputeBuffer AddBuoyancy()
    {
        int kernel = BuoyancyShader.FindKernel("Buoyancy");
        BuoyancyShader.SetBuffer(kernel, "ResultingVelocity", BuoyancyStorage);
        BuoyancyShader.SetBuffer(kernel, "Velocity", Velocity);
        BuoyancyShader.SetBuffer(kernel, "Temperature", Temperature);
        BuoyancyShader.SetInt("N", N);
        BuoyancyShader.SetFloat("R", R);
        BuoyancyShader.SetFloat("G", G);
        BuoyancyShader.SetFloat("RoomTemperature", RoomTemperature);
        BuoyancyShader.SetFloat("Pressure", Pressure);
        BuoyancyShader.SetFloat("GasMolarMass", GasMolarMass);
        DispatchShader(BuoyancyShader, kernel);

        return BuoyancyStorage;
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
        ComputeBuffer buoyancyVelocity = AddBuoyancy();
        //Project current advected velocity (Result is stored in current velocity)
        ComputeBuffer correctedCurrentVelocity = Project(buoyancyVelocity);
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
        return x + (y * N) + (z * N * N);
    }

}
