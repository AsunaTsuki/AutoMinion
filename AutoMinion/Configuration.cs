using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace AutoMinion
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public Dictionary<string, string> SavedMinions = new Dictionary<string, string>();
        public Dictionary<string, string> StaticMinions { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, bool> StaticMode { get; set; } = new Dictionary<string, bool>();
        public ushort PreviousTerritoryID { get; set; } = 0;
        public ushort CurrentTerritoryID { get; set; } = 0;


        //public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private IDalamudPluginInterface? PluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
