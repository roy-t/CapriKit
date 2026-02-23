using ImGuiNET;
using System.Numerics;

namespace CapriKit.Tests.Tool.Tests.Framework;

internal static class TestUtilities
{
    public static ImmediateResult ShowModal(string message, bool open)
    {
        if (open)
        {
            ImGui.OpenPopup(nameof(ShowModal));
        }

        var result = ImmediateResult.Unknown;

        if (ImGui.BeginPopupModal(nameof(ShowModal), ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text(message);
            ImGui.Spacing();

            if (ImGui.Button("Yes", new Vector2(120, 0)))
            {
                result = ImmediateResult.Success;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SetItemDefaultFocus();

            ImGui.SameLine();
            if (ImGui.Button("No", new Vector2(120, 0)))
            {
                result = ImmediateResult.Failure;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        return result;
    }
}
