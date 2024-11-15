using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using AutoMinion.Windows;
using ECommons;
using ECommons.DalamudServices;
using ECommons.EzEventManager;
using ECommons.Logging;
using System;
using ECommons.GameFunctions;
using ECommons.Automation;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.Enums;
using System.Threading;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AutoMinion
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "AutoMinion";
        private const string CommandName = "/autominion";

        private IDalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("AutoMinion");

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            ECommonsMain.Init(pluginInterface, this);
            
            

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);


            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this);
            
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open AutoMinion Configuration Window"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;


            // On Plugin Load, record current location ID
            this.Configuration.CurrentTerritoryID = 0;

            // Subscribe to territory changes
            Svc.ClientState.TerritoryChanged += OnTerritoryChanged;
            
        }

        private bool CheckScreenFading()
        {
            unsafe
            {
                if ((GenericHelpers.TryGetAddonByName<AtkUnitBase>("FadeMiddle", out var a) && a->IsVisible) ||
                    (GenericHelpers.TryGetAddonByName<AtkUnitBase>("FadeBack", out var ab) && ab->IsVisible))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CanSummonMinion()
        {
            unsafe
            {
                var result = ActionManager.Instance()->GetActionStatus(ActionType.GeneralAction, 10);
                var canSummonMinion = result == 0;
                return canSummonMinion;
            }
        }

        private async void OnTerritoryChanged(ushort territoryTypeId)
        {
            this.Configuration.PreviousTerritoryID = this.Configuration.CurrentTerritoryID;
            this.Configuration.CurrentTerritoryID = territoryTypeId;
            this.Configuration.Save();

            var targetableStartTime = DateTime.Now;


            // Loop until the player is targetable or until canceled, with async waiting
            while ((DateTime.Now - targetableStartTime).TotalSeconds <= 60) // Inner loop timeout (e.g., 60 seconds)
            {
                if (Svc.ClientState.LocalPlayer?.IsTargetable == true)
                {
                    if (!CheckScreenFading())
                    {

                        

                        string playerName = Svc.ClientState?.LocalPlayer.Name.ToString();
                        string playerWorld = Svc.ClientState?.LocalPlayer.HomeWorld.Value.Name.ToString();
                        string playerNameWorld = $"{playerName}@{playerWorld}";

                        this.Configuration.StaticMode.TryGetValue(playerNameWorld, out bool IsStaticMode);


                        PluginLog.Information($"Player Name: {playerNameWorld}");
                        PluginLog.Information($"TerritoryTypeId: {territoryTypeId}");

                        if (IsInWorkshopChambers(territoryTypeId) || (IsInWorkshopChambers(this.Configuration.PreviousTerritoryID) && IsInHouse(territoryTypeId)))
                        {
                            // Going into private chambers or workshop
                            return;
                        }

                        if(!CanSummonMinion())
                        {
                            // In an area that minions can't be summoned
                            return;
                        }


                        if (IsInHouse(territoryTypeId))
                        {
                            // Logic for when the player is in a housing area
                            PluginLog.Information("Player is in a housing area.");

                            if (!IsStaticMode)
                            {
                                // Save Minion if not in static mode
                                SaveMinion();
                            }

                            var currentMinion = GetCompanion();
                            if (currentMinion != null)
                            {
                                PluginLog.Verbose("Minion exists, saving and dismissing");

                                // Dismiss Minion (example, adjust as needed)
                                Chat.Instance.SendMessage("/minion");
                            }
                        }
                        else
                        {
                            // Logic for when the player is outside housing areas
                            PluginLog.Information("Player is outside housing areas.");


                            var currentMinion = GetCompanion();
                            if (currentMinion != null)
                            {
                                PluginLog.Verbose("Player is now targetable, summoning minion");
                                // Attempt to retrieve and summon the saved minion asynchronously
                                await AttemptSummonMinionAsync(playerNameWorld);
                            }
                        }
                        return;
                    }
                }

                // Await asynchronously for a short period before checking again
                await Task.Delay(1000); // 1000 milliseconds delay
            }
        }

        private async Task AttemptSummonMinionAsync(string playerNameWorld)
        {
            // Your logic to check for the current minion and summon if needed
            // Ensure any network or I/O operations here are also asynchronous if possible

            // Example: Retrieving and summoning a saved minion
            string minionName = RetrieveMinion(playerNameWorld); // Ensure this method is quick and non-blocking
            if (!string.IsNullOrEmpty(minionName))
            {
                // Summon minion
                PluginLog.Verbose($"Trying to summon: {minionName}");
                Chat.Instance.SendMessage($"""/minion "{minionName}" """);
            }

            // If you have operations that need to await, make sure they are awaited here
            // Example: await SomeAsyncOperation();
        }

        private bool IsInWorkshopChambers(ushort territoryTypeId)
        {
            // List of known territory type IDs for housing areas, this list might not be complete or accurate
            // and should be updated based on the game's current data
            var housingTerritoryIds = new ushort[] { // Example IDs, these need to be updated
            
            384, // Private Chambers - Mist
            385, // Private Chambers - LB
            386, // Private Chambers - Goblet
            652, // Private Chambers - Shiro
            983, // Private Chambers - Emp

            423, // Company Workshop - Mist
            424, // Company Workshop - Goblet
            425, // Company Workshop - LB
            653, // Company Workshop - Shiro
            984, // Company Workshop - Emp
        };

            return Array.IndexOf(housingTerritoryIds, territoryTypeId) > -1;
        }


        private bool IsInHouse(ushort territoryTypeId)
        {
            // List of known territory type IDs for housing areas, this list might not be complete or accurate
            // and should be updated based on the game's current data
            var housingTerritoryIds = new ushort[] { // Example IDs, these need to be updated

            282, // Private Cottage - Mist
            283, // Private House - Mist
            284, // Private Mansion - Mist

            342, // Private Cottage - LB
            343, // Private House - LB
            344, // Private Mansion - LB
            
            345, // Private Cottage - Goblet
            346, // Private House - Goblet
            347, // Private Mansion - Goblet

            649, // Private Cottage - Shiro
            650, // Private House - Shiro
            651, // Private Mansion - Shiro

            980, // Private Cottage - Emp
            981, // Private House - Emp
            982, // Private Mansion - Emp

            608, // Topmast Apartment
            609, // Lily Hills Apartment
            610, // Sultana's Breath Apartment
            655, // Kobai Goten Apartment
            999, // Ingleside Apartment
            
            // Add other housing district IDs here
        };

            return Array.IndexOf(housingTerritoryIds, territoryTypeId) > -1;
        }

        public void SaveMinion()
        {
            // This will add a new key-value pair if the key does not exist,
            // or update the value if the key already exists.
            //string currentMinionName = "Morpho";
            PluginLog.Verbose("Attempting to save minion info");

            var currentMinion = GetCompanion();
            string currentMinionName = currentMinion?.Name.ToString();
            PluginLog.Verbose($"Current Minion Name: {currentMinionName}");

            string playerName = Svc.ClientState?.LocalPlayer.Name.ToString();
            string playerWorld = Svc.ClientState?.LocalPlayer.HomeWorld.Value.Name.ToString();
            string playerNameWorld = $"{playerName}@{playerWorld}";


            this.Configuration.SavedMinions[playerNameWorld] = currentMinionName;
            this.Configuration.Save();
        }

        public string RetrieveMinion(string charNameWorld)
        {
            string playerName = Svc.ClientState?.LocalPlayer.Name.ToString();
            string playerWorld = Svc.ClientState?.LocalPlayer.HomeWorld.Value.Name.ToString();
            string playerNameWorld = $"{playerName}@{playerWorld}";
            this.Configuration.StaticMode.TryGetValue(playerNameWorld, out bool IsStaticEnabled);


            if (!IsStaticEnabled)
            {
                if (this.Configuration.SavedMinions.TryGetValue(charNameWorld, out string minionName))
                {
                    return minionName; // Minion name found and returned
                }
                else
                {
                    return null; // No minion name found for the given charNameWorld
                }
            }
            else
            {
                if (this.Configuration.StaticMinions.TryGetValue(charNameWorld, out string minionName))
                {
                    return minionName; // Minion name found and returned
                }
                else
                {
                    return null; // No minion name found for the given charNameWorld
                }
            }
        }


        public static IGameObject? GetCompanion()
        {
            var companion = Svc.Objects[1];
            if (companion != null && companion.ObjectKind == ObjectKind.Companion) return companion;
            return null;
        }

        public void Dispose()
        {

            SaveMinion();
            this.WindowSystem.RemoveAllWindows();
            
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            
            this.CommandManager.RemoveHandler(CommandName);
            Svc.ClientState.TerritoryChanged -= OnTerritoryChanged;



        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            ConfigWindow.IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }
}
