using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace AutoMinion.Windows;

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


    string filterString = "";

    public override void Draw()
    {
        string playerName = Svc.ClientState?.LocalPlayer.Name.ToString();
        string playerWorld = Svc.ClientState?.LocalPlayer.HomeWorld.Value.Name.ToString();
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


        this.Configuration.StaticMode.TryGetValue(playerNameWorld, out bool enableStaticMinion);

        if (ImGui.Checkbox("Enable Static Mode (Will disable dynamic mode)", ref enableStaticMinion))
        {
            this.Configuration.StaticMode[currentCharacterKey] = enableStaticMinion;
            this.Configuration.StaticMinions[currentCharacterKey] = companionIdentifiers[selectedCompanionIndex];
            this.Configuration.Save();
        }



        if (!enableStaticMinion)
        {
            ImGui.BeginDisabled();
        }


        /*if (ImGui.Combo("", ref selectedCompanionIndex, companionIdentifiers, companionIdentifiers.Length))
        {
            // Update the dictionary with the selected minion for the current character@world
            this.Configuration.StaticMinions[currentCharacterKey] = companionIdentifiers[selectedCompanionIndex];
            this.Configuration.Save(); // Save the configuration
        }*/

        if (ImGui.BeginCombo($"##selectCompanion", this.Configuration.StaticMinions.ContainsKey(currentCharacterKey) ? this.Configuration.StaticMinions[currentCharacterKey] : ""))
        {

            //ImGui.SetNextItemWidth(150);
            ImGui.InputTextWithHint("##Filter", "Search...", ref filterString, 255);


            var filteredCompanions = companionIdentifiers.Where(c => c.Contains(filterString, StringComparison.OrdinalIgnoreCase)).ToArray();

            foreach (var c in filteredCompanions)
            {
                var selected = this.Configuration.StaticMinions[currentCharacterKey] == c;
                //if (ImGui.IsWindowAppearing() && selected) ImGui.SetScrollHereY();
                if (ImGui.Selectable($"{c}", selected))
                {
                    this.Configuration.StaticMinions[currentCharacterKey] = c;
                }
            }
            ImGui.EndCombo();
        }

        if (!enableStaticMinion)
        {
            ImGui.EndDisabled();
        }
    }

}
