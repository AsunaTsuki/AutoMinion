using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets2;

namespace SamplePlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base(
        "AutoMinion Configuration",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(400, 100);
        this.SizeCondition = ImGuiCond.Always;

        this.Configuration = plugin.Configuration;
        
    }

    public void Dispose() { }


    public unsafe System.Collections.Generic.IEnumerable<Companion> GetUnlockedMinions()
    {
        var unlockedCompanions = Svc.Data.GetExcelSheet<Companion>().Where(x => !x.Singular.ToString().IsNullOrEmpty() && UIState.Instance()->IsCompanionUnlocked(x.RowId));
        return unlockedCompanions;
    }

    public override void Draw()
    {
        bool enableStaticMinion = this.Configuration.EnableStaticMinion;
        if (ImGui.Checkbox("Enable Static Mode (Will disable dynamic mode)", ref enableStaticMinion))
        {
            this.Configuration.EnableStaticMinion = enableStaticMinion;
            this.Configuration.Save();
        }



        if (!enableStaticMinion)
        {
            ImGui.BeginDisabled();
        }

        string playerName = Svc.ClientState?.LocalPlayer.Name.ToString();
        string playerWorld = Svc.ClientState?.LocalPlayer.HomeWorld.GameData.Name.ToString();
        string playerNameWorld = $"{playerName}@{playerWorld}";

        var currentCharacterKey = playerNameWorld;

        // Your existing code to fetch and sort the companions
        var unlockedCompanions = GetUnlockedMinions().OrderBy(x => x.Singular.ToString()).ToArray();

        // Apply ToTitleCase to each identifier
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        var companionIdentifiers = unlockedCompanions
            .Select(x => textInfo.ToTitleCase(x.Singular.ToString().ToLower())) // Convert to lower case first to ensure consistent capitalization
            .ToArray();


        // Retrieve the current selection for this character@world, or default to the first companion
        string currentSelection = this.Configuration.StaticMinions.TryGetValue(currentCharacterKey, out var selectedMinion)
                                  ? selectedMinion
                                  : companionIdentifiers.FirstOrDefault();

        int currentIndex = Array.IndexOf(companionIdentifiers, currentSelection);
        if (currentIndex == -1) currentIndex = 0; // Default to the first item if not found

        int selectedCompanionIndex = currentIndex;
        if (ImGui.Combo("", ref selectedCompanionIndex, companionIdentifiers, companionIdentifiers.Length))
        {
            // Update the dictionary with the selected minion for the current character@world
            this.Configuration.StaticMinions[currentCharacterKey] = companionIdentifiers[selectedCompanionIndex];
            this.Configuration.Save(); // Save the configuration
        }

        if (!enableStaticMinion)
        {
            ImGui.EndDisabled();
        }
    }

}
