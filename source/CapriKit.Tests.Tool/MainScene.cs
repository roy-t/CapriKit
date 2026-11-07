using CapriKit.DirectX11;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Debug;
using CapriKit.Tests.Tool.Tests.Framework;
using CapriKit.Win32.Input;
using ImGuiNET;

namespace CapriKit.Tests.Tool;

internal sealed class MainScene : IScene, IDisposable
{
    private readonly IReadOnlyList<ITestScreen> Tests;
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
                    if (ImGui.MenuItem($"{test.Title}", string.Empty, i == currentTestIndex))
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
                ImGui.Text($"CPU samples: {Performance.Cpu.SampleCount}/{Performance.Capacity}");
                UpdateTimingTable("CpuTimings", "CPU (ms)", Performance.Cpu);
                ImGui.Spacing();
                ImGui.SeparatorText("GPU Timings");
                ImGui.Text($"GPU samples: {Performance.Gpu.SampleCount}/{Performance.Capacity}");
                UpdateTimingTable("GpuTimings", "GPU (ms)", Performance.Gpu);
                ImGui.Spacing();
                ImGui.SeparatorText("GPU Statistics");
                UpdatePipelineTable();
            }
            ImGui.End();
        }
    }

    private static void UpdateTimingTable(string id, string header, TimingGroup group)
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
                ImGui.Text(group.Names[i]);
                ImGui.TableNextColumn();
                ImGui.Text($"{statistics[i].Median:F3}");
                ImGui.TableNextColumn();
                ImGui.Text($"{statistics[i].P99:F3}");
                ImGui.TableNextColumn();
                ImGui.Text($"{statistics[i].Noise:F3}");
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

    private static void PipelineRow(string label, ReadOnlySpan<PipelineStatistics> pipeline, Func<PipelineStatistics, ulong> selector)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(label);
        foreach (var statistics in pipeline)
        {
            ImGui.TableNextColumn();
            ImGui.Text($"{selector(statistics):N0}");
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
