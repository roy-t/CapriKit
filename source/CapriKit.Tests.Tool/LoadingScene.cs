using CapriKit.AssetPipeline;
using CapriKit.Tests.Tool.Tests.Framework;
using ImGuiNET;
using System.Numerics;

namespace CapriKit.Tests.Tool;

internal sealed class LoadingScene : IScene, IDisposable
{
    private readonly IEnumerable<ITestFactory> Factories;
    private readonly HashSet<AssetId> Seen;
    private string log;

    public LoadingScene(IEnumerable<ITestFactory> factories)
    {
        Factories = factories;
        Seen = [];
        log = string.Empty;
    }

    private void CheckProgress()
    {
        var total = 0;
        var loaded = 0;
        AssetId? lastCompletedItem = null;
        foreach (var factory in Factories)
        {
            total += factory.Total;
            loaded += factory.Loaded;
            var candidate = factory.LastCompletedItem;
            if (candidate != null && !Seen.Contains(candidate))
            {
                Seen.Add(candidate);
                log += $"{candidate}" + Environment.NewLine;
            }
        }

        Progress = loaded / (float)total;
        if (lastCompletedItem != null)
        {
            Status = $"Loading... {lastCompletedItem}";
        }
    }


    private const float BarWidth = 400.0f;
    private const float BarHeight = 24.0f;

    private const ImGuiWindowFlags Flags =
        ImGuiWindowFlags.NoDecoration |
        ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoSavedSettings |
        ImGuiWindowFlags.NoBringToFrontOnFocus |
        ImGuiWindowFlags.NoNavInputs;


    public float Progress { get; set; } = 0.0f;
    public string Status { get; set; } = "Loading...";

    public void Update(float elapsed)
    {
        CheckProgress();

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);

        // Zero padding so the window matches the viewport exactly, no rounding/border so it reads as a backdrop
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);

        if (ImGui.Begin("##loading", Flags))
        {
            var area = ImGui.GetContentRegionAvail();
            var bar = new Vector2(BarWidth, BarHeight);
            var label = ImGui.CalcTextSize(Status);
            var spacing = ImGui.GetStyle().ItemSpacing.Y;

            var blockHeight = bar.Y + spacing + label.Y;
            var top = (area.Y - blockHeight) * 0.5f;

            ImGui.SetCursorPos(new Vector2((area.X - bar.X) * 0.5f, top));
            ImGui.ProgressBar(Progress, bar, string.Empty); // empty overlay: suppresses the built-in "42%" text
            //ImGui.ProgressBar(-1.0f * (float)ImGui.GetTime(), bar, string.Empty);

            ImGui.SetCursorPosX((area.X - label.X) * 0.5f);
            ImGui.TextUnformatted(Status);
        }
        ImGui.End();

        ImGui.PopStyleVar(3);
    }

    public void Dispose()
    {

    }
}
