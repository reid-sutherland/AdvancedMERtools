global using Log = LabApi.Features.Console.Logger;

using CommandSystem;
using HarmonyLib;
using LabApi.Loader.Features.Plugins;
using ProjectMER.Features.Objects;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.IO;
using UserSettings.ServerSpecific;

/// <NOTES>
/// LabApi Update needed:
/// Generally speaking, ProjectMER is the updated version of MER that 
/// completely removes EXILED and ONLY uses LabApi.
/// So, I think if we're updating to match that, we probably need to 
/// go through this code and refactor all EXILED code to LabApi as well.
///
/// CURRENT CHANGES FOR ProjectMER REFACTOR
/// - Generally I think all the doors are scuffed and probably unusable lol.
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
        Log.Info($"AdvancedMERTools Enabled!");
        Singleton = this;

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

        manager = new EventManager();
        //CustomHandlersManager.RegisterEventsHandler(GenericEventsHandler);

        Harmony harmony = new Harmony("AMERT");
        harmony.PatchAll();

        ServerSpecificSettingsSync.ServerOnSettingValueReceived += manager.OnSSInput;
        ProjectMER.Events.Handlers.Schematic.SchematicSpawned += manager.OnSchematicLoad;
        LabApi.Events.Handlers.ServerEvents.MapGenerated += manager.OnMapGenerated;
        //LabApi.Events.Handlers.PlayerEvents. += manager.OnGrenade;
        LabApi.Events.Handlers.PlayerEvents.Spawned += manager.ApplyCustomSpawnPoint;
        LabApi.Events.Handlers.PlayerEvents.SearchingPickup += manager.OnSearchingPickup;
        LabApi.Events.Handlers.PlayerEvents.PickedUpItem += manager.OnPickedUpItem;
    }

    public override void Disable()
    {
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= manager.OnSSInput;
        ProjectMER.Events.Handlers.Schematic.SchematicSpawned -= manager.OnSchematicLoad;
        LabApi.Events.Handlers.ServerEvents.MapGenerated -= manager.OnMapGenerated;
        //LabApi.Events.Handlers.PlayerEvents. -= manager.OnGrenade;
        LabApi.Events.Handlers.PlayerEvents.Spawned -= manager.ApplyCustomSpawnPoint;
        LabApi.Events.Handlers.PlayerEvents.SearchingPickup -= manager.OnSearchingPickup;
        LabApi.Events.Handlers.PlayerEvents.PickedUpItem -= manager.OnPickedUpItem;

        manager = null;
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
