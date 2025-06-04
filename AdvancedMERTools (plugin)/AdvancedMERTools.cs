global using Log = LabApi.Features.Console.Logger;

using CommandSystem;
using HarmonyLib;
using LabApi.Events.CustomHandlers;
using LabApi.Loader.Features.Plugins;
using MEC;
using ProjectMER.Features.Objects;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.IO;
using UserSettings.ServerSpecific;

/// <NOTES>
/// CURRENT CHANGES FOR ProjectMER REFACTOR
/// - Generally I think all the doors are scuffed and probably unusable lol.
/// - Effects: How the hell do CustomEffects (or any effects for that matter) work in LabApi???
/// - DummyDoor: Idk what the hell it's for so I just completely removed it for now.
/// - InteractableTeleport: Don't need this so removing for now.
/// </NOTES>

namespace AdvancedMERTools;

public class AdvancedMERTools : Plugin<Config>
{
    public override string Name => "AdvancedMERTools";

    public override string Description => "AdvancedMERTools";

    public override string Author => "Michal78900 -> DeadServer Team";

    public override Version Version => new Version(2025, 6, 1, 1);

    public override Version RequiredApiVersion => new Version(1, 0, 0, 0);

    public static AdvancedMERTools Singleton { get; private set; }

    public static string MapsDir => ProjectMER.ProjectMER.MapsDir;

    public static string SchematicsDir => ProjectMER.ProjectMER.SchematicsDir;

    public static string AudioDir => Singleton.Config.AudioFolderPath;

    // TODO: Replace EventManager with Lab's CustomHandlersManager

    //public GenericEventsHandler GenericEventsHandler { get; } = new();

    private EventManager manager;



    public List<HealthObject> HealthObjects { get; set; } = new();

    public List<InteractablePickup> InteractablePickups { get; set; } = new();

    //public List<InteractableTeleporter> InteractableTPs { get; set; } = new();

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

        // Make sure this plugin is enabled later so that ProjectMER is set up first
        Timing.CallDelayed(3.0f, () =>
        {
            if (!Directory.Exists(MapsDir))
            {
                Log.Warn("ProjectMER Maps directory does not exist. Creating...");
                Directory.CreateDirectory(MapsDir);
            }
            if (!Directory.Exists(SchematicsDir))
            {
                Log.Warn("ProjectMER Schematics directory does not exist. Creating...");
                Directory.CreateDirectory(SchematicsDir);
            }
            if (!Directory.Exists(AudioDir))
            {
                Log.Warn("AMERT Audio directory does not exist. Creating...");
                Directory.CreateDirectory(AudioDir);
            }
            Log.Info($"AdvancedMERTools: ProjectMER is loading Schematics directory: {SchematicsDir}");

            Harmony harmony = new Harmony("AMERT");
            harmony.PatchAll();

            ServerSpecificSettingsSync.ServerOnSettingValueReceived += AMERTEventsHandler.OnSSInput;

            LabApi.Events.Handlers.ServerEvents.MapGenerated += AMERTEventsHandler.OnMapGenerated;
            //LabApi.Events.Handlers.PlayerEvents. += AMERTEventsHandler.OnGrenade;
            LabApi.Events.Handlers.PlayerEvents.Spawned += AMERTEventsHandler.ApplyCustomSpawnPoint;
            LabApi.Events.Handlers.PlayerEvents.SearchingPickup += AMERTEventsHandler.OnSearchingPickup;
            LabApi.Events.Handlers.PlayerEvents.PickingUpItem += AMERTEventsHandler.OnPickingUpItem;
            ProjectMER.Events.Handlers.Schematic.SchematicSpawned += AMERTEventsHandler.OnSchematicLoad;
        });
    }

    public override void Disable()
    {
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= AMERTEventsHandler.OnSSInput;
        LabApi.Events.Handlers.ServerEvents.MapGenerated -= AMERTEventsHandler.OnMapGenerated;
        //LabApi.Events.Handlers.PlayerEvents. -= AMERTEventsHandler.OnGrenade;
        LabApi.Events.Handlers.PlayerEvents.Spawned -= AMERTEventsHandler.ApplyCustomSpawnPoint;
        LabApi.Events.Handlers.PlayerEvents.SearchingPickup -= AMERTEventsHandler.OnSearchingPickup;
        LabApi.Events.Handlers.PlayerEvents.PickingUpItem -= AMERTEventsHandler.OnPickingUpItem;
        ProjectMER.Events.Handlers.Schematic.SchematicSpawned -= AMERTEventsHandler.OnSchematicLoad;

        CustomHandlersManager.UnregisterEventsHandler(AMERTEventsHandler);
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
