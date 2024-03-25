using System;
using System.Numerics;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using ECommons.Automation;
using ECommons.DalamudServices;
using ImGuiNET;

namespace AutoMinion.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;

    public MainWindow(Plugin plugin) : base(
        "AutoMinion", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.Plugin = plugin;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        //ImGui.Text($"The random config bool is {this.Plugin.Configuration.SomePropertyToBeSavedAndWithADefault}");

        if (ImGui.Button("Show Settings"))
        {
            this.Plugin.DrawConfigUI();
        }

        if (ImGui.Button("Summon Minion"))
        {


            // Check if minion is saved for player
            //string minionName = this.Plugin.RetrieveMinion(playerNameWorld);
            string minionName = "Morpho";

            // Summon minion
            if (minionName != null)
            {
                Chat.Instance.SendMessage($"/minion {minionName}");
            }
        }

    }
}
