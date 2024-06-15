using System;
using System.Diagnostics;
using System.Drawing;
using UnityEngine;

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

    public RenderTexture RenderDensity;

    //Constants.
    public int N;
    public float TimeStep;
    public int Iterations;
    public float Diffusion;
    public float Viscosity;

    public GameObject Quad;
    public Material material;

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
        material = Quad.GetComponent<Renderer>().material;
        material.mainTexture = RenderDensity;
    }

    void InitializeQuantities()
    {
        N = 1024;
        TimeStep = 1f;
        Iterations = 50;
        Diffusion = 0f;
        Viscosity = 0f;

        // Initialize RenderTexture
        Velocity = CreaeComputeBuffer(N);
        PreviousVelocity = CreaeComputeBuffer(N);
        Pressure = CreaeComputeBuffer(N);
        Density = CreaeComputeBuffer(N);
        PreviousDensity = CreaeComputeBuffer(N);

        RenderDensity = CreateRenderTexture(N);
        InitializeRenderTexture(RenderDensity, UnityEngine.Color.black);
        // Initialize Velocity
        SetRandomComputeBufferData(Velocity);
        SetRandomComputeBufferData(PreviousVelocity);
        SetRandomComputeBufferData(Density);
        SetRandomComputeBufferData(PreviousDensity);
        SetRandomComputeBufferData(Pressure);


        AdvectionStorage = CreaeComputeBuffer(N);
        SolutionStorage = CreaeComputeBuffer(N);
        DivergenceStorage = CreaeComputeBuffer(N);
        GradientStorage = CreaeComputeBuffer(N);
        ResultingDensity = CreaeComputeBuffer(N);
        ResultingVelocity = CreaeComputeBuffer(N);
    }

    ComputeBuffer CreaeComputeBuffer(int N)
    {
        ComputeBuffer cb = new ComputeBuffer(N * N, sizeof(float) * 3);
        return cb;
    }

    void SetComputeBufferData(ComputeBuffer cb)
    {
        int size = N * N;
        Vector3[] data = new Vector3[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = new Vector3(0, 0, 0);
        }
        cb.SetData(data);
    }

    void SetRandomComputeBufferData(ComputeBuffer cb)
    {
        int size = N * N;
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
        Texture2D t = CreateTexture2D(N);
            for (int y = 0; y < t.height; y++)
            {
                for (int x = 0; x < t.width; x++)
                {
                    t.SetPixel(x, y, color);
                }
            }

        t.Apply();
        Graphics.Blit(t, rt);
    }

    void InitializeRandomRenderTexture(RenderTexture rt)
    {
        Texture2D t = CreateTexture2D(N);
            for (int y = 0; y < t.height; y++)
            {
                for (int x = 0; x < t.width; x++)
                {
                    float randomValue = UnityEngine.Random.value;
                    t.SetPixel(x, y, new UnityEngine.Color(randomValue, randomValue, randomValue, 1.0f));
                }
            }
        t.Apply();
        //Copy3D(t, rt);
    }

    Texture2D CreateTexture2D(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBAFloat, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        return texture;
    }

    RenderTexture CreateRenderTexture(int size)
    {
        RenderTexture rt = new RenderTexture(N, N, 0)
        {
            enableRandomWrite = true
            //format = RenderTextureFormat.ARGBFloat
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
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
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

        RenderizeDensity(Density, RenderDensity);

    }

    void CopyRenderTextureToTexture2D(RenderTexture rt, Texture2D texture)
    {
        RenderTexture.active = rt;
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
    }

    RenderTexture CreateRenderTextureFromTexture2D(Texture2D texture)
    {
        var rt = new RenderTexture(texture.width, texture.height, 0)
        {
            enableRandomWrite = true,
        };
        rt.Create();
        Graphics.Blit(texture, rt);
        return rt;
    }

    //void AddDensity(int x, int y, float amount)
    //{
    //    Texture2D D = new Texture2D(N, N);
    //    CopyRenderTextureToTexture2D(Density, D);
    //    Color currentDensity = D.GetPixel(x, y);
    //    float newDensity = currentDensity.r + amount;
    //    D.SetPixel(x, y, new Color(newDensity, newDensity, newDensity, 1.0f));
    //    D.Apply();
    //    Graphics.Blit(D, Density);
    //    D = null;
    //}

    //void AddVelocity(int x, int y, float amountX, float amountY)
    //{
    //    Texture2D V = new Texture2D(N, N);
    //    CopyRenderTextureToTexture2D(Velocity, V);
    //    Color currentVelocity = V.GetPixel(x, y);
    //    float newVelocityX = currentVelocity.r + amountX;
    //    float newVelocityY = currentVelocity.g + amountY;
    //    V.SetPixel(x, y, new Color(newVelocityX, newVelocityY, 0.0f, 1.0f));
    //    V.Apply();
    //    Graphics.Blit(V, Velocity);
    //    V = null;
    //}

    //void AddDensity(int x, int y, float amount)
    //{
    //    int kernel = AddDensityShader.FindKernel("AddDensity");
    //    AddDensityShader.SetTexture(kernel, "ResultingDensity", ResultingDensity);
    //    AddDensityShader.SetTexture(kernel, "Density", Density);
    //    AddDensityShader.SetInt("X", x);
    //    AddDensityShader.SetInt("Y", y);
    //    AddDensityShader.SetFloat("Amount", amount);
    //    AddDensityShader.Dispatch(kernel, 1, 1, 1);
    //    Graphics.Blit(ResultingDensity, Density);
    //}

    //void AddVelocity(int x, int y, float amountX, float amountY)
    //{
    //    int kernel = AddVelocityShader.FindKernel("AddVelocity");
    //    AddVelocityShader.SetTexture(kernel, "ResultingVelocity", ResultingVelocity);
    //    AddVelocityShader.SetTexture(kernel, "Velocity", Velocity);
    //    AddVelocityShader.SetInt("X", x);
    //    AddVelocityShader.SetInt("Y", y);
    //    AddVelocityShader.SetFloat("AmountX", amountX);
    //    AddVelocityShader.SetFloat("AmountY", amountY);
    //    AddVelocityShader.Dispatch(kernel, 1, 1, 1);
    //    Graphics.Blit(ResultingVelocity, Velocity);
    //}
    //void HandleMouseInput()
    //{

    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        AddVelocity((int)(N / 2), (int)(N / 2), 10.0f, 10.0f);
    //        AddDensity((int)(N / 2), (int)(N / 2), 10000);
    //    }
            
    //}

}
