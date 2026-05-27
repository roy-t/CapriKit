using ImGuiNET;
using System.Numerics;

namespace CapriKit.Tests.Tool.Tests.Framework;

public enum ModalResult
{
    Unknown,
    Yes,
    No
}

internal static class TestUtilities
{
    public static ModalResult ShowModal(string message, bool open)
    {
        if (open)
        {
            ImGui.OpenPopup(nameof(ShowModal));
        }

        var result = ModalResult.Unknown;

        if (ImGui.BeginPopupModal(nameof(ShowModal), ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text(message);
            ImGui.Spacing();

            if (ImGui.Button("Yes", new Vector2(120, 0)))
            {
                result = ModalResult.Yes;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SetItemDefaultFocus();

            ImGui.SameLine();
            if (ImGui.Button("No", new Vector2(120, 0)))
            {
                result = ModalResult.No;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        return result;
    }
}
