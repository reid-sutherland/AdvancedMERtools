using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace AdvancedMERTools;

public static class ServerSettings
{
    public static SSGroupHeader Header { get; set; } = new SSGroupHeader("AMERT Keybinds");

    public static SSKeybindSetting CreateIOSettingForKeycode(KeyCode keycode) => new(null, $"{IOLabelPreamble} - {keycode}", keycode, allowSpectatorTrigger: false);

    public static string IOLabelPreamble { get; set; } = "AMERT - Interactable Object";

    public static void RegisterSettings()
    {
        // I'm tired of dealing with this bs so just always make an AMERT header
        ServerSpecificSettingsSync.DefinedSettings ??= new ServerSpecificSettingBase[0];
        List<ServerSpecificSettingBase> original = ServerSpecificSettingsSync.DefinedSettings.ToList();
        if (original.FindIndex(x => x is SSGroupHeader && x.Label.Equals(Header.Label)) == -1)
        {
            Log.Debug($"AMERTKeybindHeader did not exist - adding");
            original.Add(Header);
        }

        ServerSpecificSettingsSync.DefinedSettings = original.ToArray();
        Log.Debug($"Sending SS defined settings: {ServerSpecificSettingsSync.DefinedSettings.Length}");
        ServerSpecificSettingsSync.SendToAll();
    }
}
