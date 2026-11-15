using CapriKit.DirectX11;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Debug;
using CapriKit.Strings;
using CapriKit.Tests.Tool.Tests.Framework;
using CapriKit.Win32.Input;
using ImGuiNET;
using System.Numerics;
using System.Runtime.CompilerServices;
using static CapriKit.Strings.StringBuffer;

namespace CapriKit.Tests.Tool;

internal static class UI
{
    // ImGui copies text during each call and runs on a single thread, so one shared buffer is enough
    internal static readonly StringBuffer Buffer = new();

    public static void Text(StringBuffer buffer, [InterpolatedStringHandlerArgument("buffer")] ref StringBufferHandler handler)
    {
        ImGui.TextUnformatted(buffer.Write(ref handler));
    }
}


internal sealed class MainScene : IScene, IDisposable
{
    private readonly IReadOnlyList<ITestScreen> Tests;
    private readonly StringBuffer StringBuffer;
    private readonly Keyboard Keyboard;
    private readonly SwapChain SwapChain;
    private readonly PerformanceDataCollector Performance;
    private int currentTestIndex;

    private bool showPerformancePanel;

    public MainScene(Keyboard keyboard, SwapChain swapChain, PerformanceDataCollector performance, IEnumerable<ITestScreen> tests)
    {
        Tests = [.. tests.OrderBy(t => t.Title)];
        currentTestIndex = 0;
        Keyboard = keyboard;
        SwapChain = swapChain;
        Performance = performance;
        StringBuffer = new StringBuffer();
    }

    public void Update(DeviceContext context, float elapsed)
    {
        UpdateMenu();
        UpdatePanels();
        HandleInput();
        if (currentTestIndex >= 0 && currentTestIndex < Tests.Count)
        {
            Tests[currentTestIndex].Render(context, elapsed);
        }
    }
    private void HandleInput()
    {
        if (Keyboard.Pressed(VirtualKeyCode.VK_OEM_4)) // [
        {
            currentTestIndex = (currentTestIndex - 1 + Tests.Count) % Tests.Count;
        }
        if (Keyboard.Pressed(VirtualKeyCode.VK_OEM_6)) // ]
        {
            currentTestIndex = (currentTestIndex + 1) % Tests.Count;

        }
    }

    private void UpdateMenu()
    {
        ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("@"))
            {
                ImGui.Checkbox("Show Performance Panel", ref showPerformancePanel);
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Tests"))
            {
                for (var i = 0; i < Tests.Count; i++)
                {
                    var test = Tests[i];
                    if (ImGui.MenuItem(test.Title, string.Empty, i == currentTestIndex))
                    {
                        currentTestIndex = i;
                    }
                }

                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }

    private void UpdatePanels()
    {
        if (showPerformancePanel)
        {
            // End must be called even if Begin returns false (window collapsed)
            if (ImGui.Begin("Performance"))
            {
                ImGui.SeparatorText("Settings");
                var vSync = SwapChain.VSync;
                if (ImGui.Checkbox("VSync", ref vSync))
                {
                    SwapChain.VSync = vSync;
                }

                ImGui.SameLine();
                if (ImGui.Button("Reset"))
                {
                    Performance.Reset();
                }

                ImGui.Spacing();
                ImGui.SeparatorText("CPU Timings");
                UI.Text(StringBuffer, $"CPU samples: {Performance.Cpu.SampleCount}/{Performance.Capacity}");
                UpdateTimingTable("CpuTimings", "CPU (ms)", Performance.Cpu);
                ImGui.Spacing();
                ImGui.SeparatorText("CPU Memory");
                UpdateCpuMemoryTable(Performance);
                ImGui.Spacing();
                ImGui.SeparatorText("GPU Timings");
                UI.Text(StringBuffer, $"GPU samples: {Performance.Gpu.SampleCount}/{Performance.Capacity}");
                UpdateTimingTable("GpuTimings", "GPU (ms)", Performance.Gpu);
                ImGui.Spacing();
                ImGui.SeparatorText("GPU Statistics");
                UpdatePipelineTable();
                ImGui.Spacing();
                ImGui.SeparatorText("GPU Memory");
                UpdateGpuMemoryUsageTable(Performance);

            }
            ImGui.End();
        }
    }

    private static void UpdateGpuMemoryUsageTable(PerformanceDataCollector performance)
    {
        var fraction = performance.GpuMemory.MemoryBudget > 0
            ? performance.GpuMemory.MemoryUsage / (float)performance.GpuMemory.MemoryBudget
            : 0.0f;
        var used = performance.GpuMemory.MemoryUsage / (1024.0f * 1024.0f);
        var budget = performance.GpuMemory.MemoryBudget / (1024.0f * 1024.0f);
        ImGui.ProgressBar(fraction, Vector2.Zero, $"Used: {used:F2} MiB / Budget: {budget:F2} MiB");
    }

    private static void UpdateCpuMemoryTable(PerformanceDataCollector performance)
    {
        if (ImGui.BeginTable("CpuMemory", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Memory");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            var memory = performance.CpuMemory;
            MemoryRow("Private memory", $"{ToMiB(memory.PrivateMemorySize):F2} MiB");
            MemoryRow("Allocated total", $"{ToMiB(memory.AllocatedBytes):F2} MiB");
            MemoryRow("Allocated per frame", $"{memory.AllocatedBytesPerFrame / 1024.0f:F2} KiB");
            MemoryRow("GC pause total", $"{memory.TotalPauseDuration:F3} ms");
            MemoryRow("GC pause last", $"{memory.LastPauseDuration:F3} ms");

            var generations = performance.GenerationSizes;
            MemoryRow("Gen0", $"{ToMiB(generations[0]):F2} MiB");
            MemoryRow("Gen1", $"{ToMiB(generations[1]):F2} MiB");
            MemoryRow("Gen2", $"{ToMiB(generations[2]):F2} MiB");
            MemoryRow("Large object heap", $"{ToMiB(generations[3]):F2} MiB");
            MemoryRow("Pinned object heap", $"{ToMiB(generations[4]):F2} MiB");

            ImGui.EndTable();
        }
    }

    private static void MemoryRow(string label, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(label);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(value);
    }

    private static float ToMiB(long bytes) => bytes / (1024.0f * 1024.0f);

    private void UpdateTimingTable(string id, string header, TimingGroup group)
    {
        if (ImGui.BeginTable(id, 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn(header);
            ImGui.TableSetupColumn("Median");
            ImGui.TableSetupColumn("p99");
            ImGui.TableSetupColumn("Noise");
            ImGui.TableHeadersRow();

            var statistics = group.Statistics;
            for (var i = 0; i < statistics.Length; i++)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(group.Names[i]);
                ImGui.TableNextColumn();
                UI.Text(StringBuffer, $"{statistics[i].Median:F3}");
                ImGui.TableNextColumn();
                UI.Text(StringBuffer, $"{statistics[i].P99:F3}");
                ImGui.TableNextColumn();
                UI.Text(StringBuffer, $"{statistics[i].Noise:F3}");
            }

            ImGui.EndTable();
        }
    }

    private void UpdatePipelineTable()
    {
        var pipeline = Performance.Pipeline;
        if (ImGui.BeginTable("PipelineStatistics", pipeline.Length + 1, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Pipeline");
            foreach (var name in Performance.Gpu.Names)
            {
                ImGui.TableSetupColumn(name);
            }
            ImGui.TableHeadersRow();

            PipelineRow("IA vertices", pipeline, static s => s.InputAssemblerVertices);
            PipelineRow("IA primitives", pipeline, static s => s.InputAssemblerPrimitives);
            PipelineRow("VS invocations", pipeline, static s => s.VertexShaderInvocations);
            PipelineRow("GS invocations", pipeline, static s => s.GeometryShaderInvocations);
            PipelineRow("GS primitives", pipeline, static s => s.GeometryShaderPrimitives);
            PipelineRow("HS invocations", pipeline, static s => s.HullShaderInvocations);
            PipelineRow("DS invocations", pipeline, static s => s.DomainShaderInvocations);
            PipelineRow("Rasterizer in", pipeline, static s => s.RasterizerInvocations);
            PipelineRow("Rasterized", pipeline, static s => s.RasterizerPrimitives);
            PipelineRow("PS invocations", pipeline, static s => s.PixelShaderInvocations);
            PipelineRow("CS invocations", pipeline, static s => s.ComputeShaderInvocations);

            ImGui.EndTable();
        }
    }

    private void PipelineRow(string label, ReadOnlySpan<PipelineStatistics> pipeline, Func<PipelineStatistics, ulong> selector)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(label);
        foreach (var statistics in pipeline)
        {
            ImGui.TableNextColumn();
            UI.Text(StringBuffer, $"{selector(statistics):N0}");
        }
    }

    public void Dispose()
    {
        foreach (var test in Tests)
        {
            test.Dispose();
        }
    }
}
