using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace AdvancedMERTools;

public static class ServerSettings
{
    public static SSGroupHeader Header { get; set; } = new SSGroupHeader("AMERT Keybinds");

    public static SSKeybindSetting IODefaultKeySetting { get; set; } = new SSKeybindSetting(null, $"AMERT - Interactable Object - {KeyCode.E}", KeyCode.E, allowSpectatorTrigger: false);

    public static void RegisterSettings()
    {
        // I'm tired of dealing with this bs so just always make an AMERT header + default IO keybind (E)
        ServerSpecificSettingsSync.DefinedSettings ??= new ServerSpecificSettingBase[0];
        List<ServerSpecificSettingBase> original = ServerSpecificSettingsSync.DefinedSettings.ToList();
        if (original.FindIndex(x => x is SSGroupHeader && x.Label.Equals(Header.Label)) == -1)
        {
            Log.Debug($"AMERTKeybindHeader did not exist - adding");
            original.Add(Header);
        }
        if (original.FindIndex(x => x is SSKeybindSetting && x.Label.Equals(IODefaultKeySetting.Label)) == -1)
        {
            Log.Debug($"AMERTIOKeybind did not exist - adding");
            original.Add(IODefaultKeySetting);
        }

        ServerSpecificSettingsSync.DefinedSettings = original.ToArray();
        Log.Debug($"Sending SS defined settings: {ServerSpecificSettingsSync.DefinedSettings.Length}");
        ServerSpecificSettingsSync.SendToAll();
    }
}
