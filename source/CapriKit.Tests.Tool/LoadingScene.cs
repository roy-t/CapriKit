using CapriKit.AssetPipeline;
using CapriKit.DirectX11.Contexts;
using CapriKit.Tests.Tool.Tests.Framework;
using ImGuiNET;
using Microsoft.Extensions.DependencyInjection;
using System.Numerics;

namespace CapriKit.Tests.Tool;

internal sealed class LoadingScene : IScene
{
    private const float BarWidth = 400.0f;
    private const float BarHeight = 24.0f;
    private const int VisibleAssets = 10;
    private const ImGuiWindowFlags Flags =
        ImGuiWindowFlags.NoDecoration |
        ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoSavedSettings |
        ImGuiWindowFlags.NoBringToFrontOnFocus |
        ImGuiWindowFlags.NoNavInputs;

    private const ImGuiWindowFlags ListFlags =
        ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse |
        ImGuiWindowFlags.NoNavInputs;
    private readonly IServiceProvider ServiceProvider;
    private readonly GameLoop GameLoop;
    private readonly IEnumerable<ITestFactory> Factories;
    private readonly HashSet<AssetId> Seen;
    private readonly List<string> Completed;
    private readonly ObjectFactory<MainScene> MainSceneFactory;

    public LoadingScene(IServiceProvider provider, GameLoop gameLoop, IEnumerable<ITestFactory> factories)
    {
        ServiceProvider = provider;
        GameLoop = gameLoop;
        Factories = factories;
        Seen = [];
        Completed = new List<string>(VisibleAssets + 1);
        MainSceneFactory = ActivatorUtilities.CreateFactory<MainScene>([typeof(List<ITestScreen>)]);
    }

    public float Progress { get; set; } = 0.0f;
    public string Status { get; set; } = "Loading...";

    private void CheckProgress()
    {
        var total = 0;
        var loaded = 0;
        foreach (var factory in Factories)
        {
            total += factory.Total;
            loaded += factory.Loaded;

            var candidate = factory.LastCompletedItem;
            if (candidate != null && Seen.Add(candidate))
            {
                Completed.Insert(0, candidate.ToString());
                if (Completed.Count > VisibleAssets)
                {
                    Completed.RemoveAt(Completed.Count - 1);
                }
            }
        }

        Progress = total > 0 ? loaded / (float)total : 1.0f;

        if (loaded == total)
        {
            List<ITestScreen> tests = [];
            foreach (var factory in Factories)
            {
                if (!factory.TryCreate(ServiceProvider, tests))
                {
                    throw new Exception("Factory completed but cannot construct");
                }
            }

            var main = MainSceneFactory(ServiceProvider, [tests]);
            GameLoop.ChangeScene(main);
        }
    }

    public void Update(DeviceContext _, float elapsed)
    {
        CheckProgress();

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);

        if (ImGui.Begin("##loading", Flags))
        {
            var area = ImGui.GetContentRegionAvail();
            var bar = new Vector2(BarWidth, BarHeight);
            var label = ImGui.CalcTextSize(Status);
            var spacing = ImGui.GetStyle().ItemSpacing.Y;

            var list = new Vector2(BarWidth, VisibleAssets * ImGui.GetTextLineHeightWithSpacing());

            var blockHeight = bar.Y + spacing + label.Y + spacing + list.Y;
            var top = (area.Y - blockHeight) * 0.5f;

            ImGui.SetCursorPos(new Vector2((area.X - bar.X) * 0.5f, top));
            ImGui.ProgressBar(Progress, bar, string.Empty); // empty overlay: suppresses the built-in "42%" text

            ImGui.SetCursorPosX((area.X - label.X) * 0.5f);
            ImGui.TextUnformatted(Status);

            ImGui.SetCursorPosX((area.X - list.X) * 0.5f);
            DrawCompleted(list);
        }
        ImGui.End();

        ImGui.PopStyleVar(3);
    }

    private void DrawCompleted(Vector2 size)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);

        if (ImGui.BeginChild("##completed", size, ImGuiChildFlags.None, ListFlags))
        {
            foreach (var asset in Completed)
            {
                ImGui.TextUnformatted(asset);
            }
        }
        ImGui.EndChild();

        ImGui.PopStyleColor();
    }

    public void Dispose() { }
}
