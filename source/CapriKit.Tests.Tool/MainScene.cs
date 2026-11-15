using CapriKit.DirectX11;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Debug;
using CapriKit.Strings;
using CapriKit.Tests.Tool.Tests.Framework;
using CapriKit.Win32.Input;
using ImGuiNET;
using System.Numerics;
using System.Runtime.CompilerServices;
using static CapriKit.Strings.FormatBuffer;

namespace CapriKit.Tests.Tool;

internal sealed class MainScene : IScene, IDisposable
{
    private readonly IReadOnlyList<ITestScreen> Tests;
    private readonly FormatBuffer FormatBuffer;
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
        FormatBuffer = new FormatBuffer();
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
                ImGui.TextUnformatted(FormatBuffer.Format($"CPU samples: {Performance.Cpu.SampleCount}/{Performance.Capacity}"));
                UpdateTimingTable("CpuTimings", "CPU (ms)", Performance.Cpu);
                ImGui.Spacing();
                ImGui.SeparatorText("CPU Memory");
                UpdateCpuMemoryTable(Performance);
                ImGui.Spacing();
                ImGui.SeparatorText("GPU Timings");
                ImGui.TextUnformatted(FormatBuffer.Format($"GPU samples: {Performance.Gpu.SampleCount}/{Performance.Capacity}"));
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

    private void UpdateGpuMemoryUsageTable(PerformanceDataCollector performance)
    {
        var fraction = performance.GpuMemory.MemoryBudget > 0
            ? performance.GpuMemory.MemoryUsage / (float)performance.GpuMemory.MemoryBudget
            : 0.0f;
        var used = performance.GpuMemory.MemoryUsage / (1024.0f * 1024.0f);
        var budget = performance.GpuMemory.MemoryBudget / (1024.0f * 1024.0f);
        ImGui.ProgressBar(fraction, Vector2.Zero, FormatBuffer.Format($"Used: {used:F2} MiB / Budget: {budget:F2} MiB"));
    }

    private void UpdateCpuMemoryTable(PerformanceDataCollector performance)
    {
        if (ImGui.BeginTable("CpuMemory", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Memory");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            var memory = performance.CpuMemory;
            MemoryRow("Private memory", FormatBuffer, $"{ToMiB(memory.PrivateMemorySize):F2} MiB");
            MemoryRow("Allocated total", FormatBuffer, $"{ToMiB(memory.AllocatedBytes):F2} MiB");
            MemoryRow("Allocated per frame", FormatBuffer, $"{memory.AllocatedBytesPerFrame} Bytes");
            MemoryRow("GC pause total", FormatBuffer, $"{memory.TotalPauseDuration:F3} ms");
            MemoryRow("GC pause last", FormatBuffer, $"{memory.LastPauseDuration:F3} ms");

            var generations = performance.GenerationSizes;
            MemoryRow("Gen0", FormatBuffer, $"{ToMiB(generations[0]):F2} MiB");
            MemoryRow("Gen1", FormatBuffer, $"{ToMiB(generations[1]):F2} MiB");
            MemoryRow("Gen2", FormatBuffer, $"{ToMiB(generations[2]):F2} MiB");
            MemoryRow("Large object heap", FormatBuffer, $"{ToMiB(generations[3]):F2} MiB");
            MemoryRow("Pinned object heap", FormatBuffer, $"{ToMiB(generations[4]):F2} MiB");

            ImGui.EndTable();
        }
    }

    private static void MemoryRow(string label, FormatBuffer buffer, [InterpolatedStringHandlerArgument("buffer")] ref StringBufferHandler value)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(label);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(buffer.Format(ref value));
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
                ImGui.TextUnformatted(FormatBuffer.Format($"{statistics[i].Median:F3}"));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(FormatBuffer.Format($"{statistics[i].P99:F3}"));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(FormatBuffer.Format($"{statistics[i].Noise:F3}"));
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
            ImGui.TextUnformatted(FormatBuffer.Format($"{selector(statistics):N0}"));
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
