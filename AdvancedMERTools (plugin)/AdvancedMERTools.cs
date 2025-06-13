global using Log = AdvancedMERTools.AutoDebugLogger;

using CommandSystem;
using HarmonyLib;
using LabApi.Events.CustomHandlers;
using LabApi.Loader.Features.Plugins;
using ProjectMER.Features.Objects;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace AdvancedMERTools;

public class AdvancedMERTools : Plugin<Config>
{
    // LabApi Plugin overrides

    public override string Name => "AdvancedMERTools";

    public override string Description => "AdvancedMERTools";

    public override string Author => "MujisongPlay + DeadServer Team";

    public override Version Version => new Version(2025, 6, 7, 1);

    public override Version RequiredApiVersion => new Version(1, 0, 0, 0);

    // Plugin objects

    public static AdvancedMERTools Singleton { get; private set; }

    public static string AudioDir => Singleton.Config.AudioFolderPath;

    private AMERTEventHandlers AMERTEventsHandler { get; } = new();

    public SSKeybindSetting InteractbleObjectKeybindSetting { get; } = new SSKeybindSetting(69, $"AMERT - Interactable Object - {KeyCode.E}", KeyCode.E);

    // Tracked collections

    public List<HealthObject> HealthObjects { get; set; } = new();

    public List<InteractablePickup> InteractablePickups { get; set; } = new();

    public List<InteractableTeleporter> InteractableTeleporters { get; set; } = new();

    public List<CustomCollider> CustomColliders { get; set; } = new();

    public List<DummyDoor> DummyDoors { get; set; } = new();

    public List<DummyGate> DummyGates { get; set; } = new();

    public List<GroovyNoise> GroovyNoises { get; set; } = new();

    public List<CustomDoor> CustomDoors { get; set; } = new();

    public List<InteractableObject> InteractableObjects { get; set; } = new();

    public Dictionary<SchematicObject, Dictionary<string, List<AMERTInteractable>>> AMERTGroup { get; set; } = new();

    public Dictionary<SchematicObject, Dictionary<int, AMERTInteractable>> CodeClassPair { get; set; } = new();

    public Dictionary<Type, RandomExecutionModule> TypeSingletonPair { get; set; } = new();

    public Dictionary<int, List<InteractableObject>> IOkeys { get; set; } = new();

    public Dictionary<SchematicObject, Dictionary<string, FunctionExecutor>> FunctionExecutors { get; set; } = new();

    public Dictionary<SchematicObject, Dictionary<string, object>> SchematicVariables { get; set; } = new();

    public Dictionary<string, object> RoundVariable { get; set; } = new();

    public override void Enable()
    {
        Singleton = this;
        CustomHandlersManager.RegisterEventsHandler(AMERTEventsHandler);
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += AMERTEventsHandler.OnSSInput;
        ProjectMER.Events.Handlers.Schematic.SchematicSpawned += AMERTEventsHandler.OnSchematicSpawned;

        if (!Directory.Exists(AudioDir))
        {
            Log.Warn("AMERT Audio directory does not exist. Creating...");
            Directory.CreateDirectory(AudioDir);
        }

        Harmony harmony = new Harmony("AMERT");
        harmony.PatchAll();
    }

    public override void Disable()
    {
        CustomHandlersManager.UnregisterEventsHandler(AMERTEventsHandler);
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= AMERTEventsHandler.OnSSInput;
        ProjectMER.Events.Handlers.Schematic.SchematicSpawned -= AMERTEventsHandler.OnSchematicSpawned;
        Singleton = null;
    }

    public static void ExecuteCommand(string context)
    {
        string[] array = context.Trim().Split(new char[] { ' ' }, 512, StringSplitOptions.RemoveEmptyEntries);
        ICommand command1;
        if (CommandProcessor.RemoteAdminCommandHandler.TryGetCommand(array[0], out command1))
        {
            command1.Execute(array.Segment(1), ServerConsole.Scs, out _);
        }
    }
}
