using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace SamplePlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public Dictionary<string, string> SavedMinions = new Dictionary<string, string>();
        public Dictionary<string, string> StaticMinions { get; set; } = new Dictionary<string, string>();
        public ushort PreviousTerritoryID { get; set; } = 0;
        public ushort CurrentTerritoryID { get; set; } = 0;
        public bool EnableStaticMinion { get; set; } = false;


        //public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? PluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
