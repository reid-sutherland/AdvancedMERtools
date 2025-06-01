using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Exiled.API.Features;
using Exiled.API.Enums;
using Exiled.Loader;
using UnityEngine;
using Exiled.API.Features.Pickups;
using CustomCulling;
using Interactables.Interobjects;
using Interactables;
using Interactables.Interobjects.DoorUtils;
using System.IO;
using Utf8Json;
using Exiled.Events.Features;
using Exiled.Events;
using PlayerRoles;
using Exiled.API.Features.Doors;
using HarmonyLib;
using Mirror;
using System.Reflection.Emit;
using PlayerRoles.FirstPersonControl;
using CommandSystem;
using CommandSystem.Commands;
using RemoteAdmin;
using UserSettings.ServerSpecific;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Searching;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;
using ProjectMER.Features;

/// 
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
/// 

namespace AdvancedMERTools
{
    public class AdvancedMERTools : Plugin<Config>
    {
        private EventManager manager;

        public static AdvancedMERTools Singleton;

        public List<HealthObject> healthObjects = new List<HealthObject> { };

        public List<InteractablePickup> InteractablePickups = new List<InteractablePickup> { };

        //public List<InteractableTeleporter> InteractableTPs = new List<InteractableTeleporter> { };

        public List<CustomCollider> CustomColliders = new List<CustomCollider> { };

        public List<DummyDoor> dummyDoors = new List<DummyDoor> { };

        public List<DummyGate> dummyGates = new List<DummyGate> { };

        public List<GroovyNoise> groovyNoises = new List<GroovyNoise> { };

        public List<CustomDoor> customDoors = new List<CustomDoor> { };

        public List<InteractableObject> interactableObjects = new List<InteractableObject> { };

        public Dictionary<SchematicObject, Dictionary<string, List<AMERTInteractable>>> AMERTGroup = new Dictionary<SchematicObject, Dictionary<string, List<AMERTInteractable>>> { };

        public Dictionary<SchematicObject, Dictionary<int, AMERTInteractable>> codeClassPair = new Dictionary<SchematicObject, Dictionary<int, AMERTInteractable>> { };

        public Dictionary<Type, RandomExecutionModule> TypeSingletonPair = new Dictionary<Type, RandomExecutionModule> { };

        public Dictionary<int, List<InteractableObject>> IOkeys = new Dictionary<int, List<InteractableObject>> { };

        public Dictionary<SchematicObject, Dictionary<string, FunctionExecutor>> FunctionExecutors = new Dictionary<SchematicObject, Dictionary<string, FunctionExecutor>> { };

        public Dictionary<SchematicObject, Dictionary<string, object>> SchematicVariables = new Dictionary<SchematicObject, Dictionary<string, object>> { };

        public Dictionary<string, object> RoundVariable = new Dictionary<string, object> { };

        public override void OnEnabled()
        {
            //ServerConsole.AddLog("\t\t!!!!");
            Singleton = this;
            manager = new EventManager();
            Harmony harmony = new Harmony("AMERT");
            harmony.PatchAll();
            Register();
        }

        public override void OnDisabled()
        {
            UnRegister();
            manager = null;
            Singleton = null;
        }

        void Register()
        {
            ProjectMER.Events.Handlers.Schematic.SchematicSpawned += manager.OnSchematicLoad;
            //ProjectMER.Events.Handlers.Internal. += manager.OnTeleport;

            ServerSpecificSettingsSync.ServerOnSettingValueReceived += manager.OnSSInput;
            Exiled.Events.Handlers.Map.Generated += manager.OnGen;
            //Exiled.Events.Handlers.Server.RoundStarted += manager.OnRound;
            //Exiled.Events.Handlers.Map.Decontaminating += manager.OnDecont;
            Exiled.Events.Handlers.Map.ExplodingGrenade += manager.OnGrenade;
            //Exiled.Events.Handlers.Warhead.Detonated += manager.OnAlpha;
            Exiled.Events.Handlers.Player.Spawned += manager.ApplyCustomSpawnPoint;
            Exiled.Events.Handlers.Player.SearchingPickup += manager.OnItemSearching;
            Exiled.Events.Handlers.Player.PickingUpItem += manager.OnItemPicked;
            //Exiled.Events.Handlers.Player.InteractingDoor += manager.OnInteracted;
        }

        void UnRegister()
        {
            ProjectMER.Events.Handlers.Schematic.SchematicSpawned -= manager.OnSchematicLoad;
            //MapEditorReborn.Events.Handlers.Teleport.Teleporting -= manager.OnTeleport;

            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= manager.OnSSInput;
            Exiled.Events.Handlers.Map.Generated -= manager.OnGen;
            //Exiled.Events.Handlers.Server.RoundStarted -= manager.OnRound;
            //Exiled.Events.Handlers.Map.Decontaminating -= manager.OnDecont;
            Exiled.Events.Handlers.Map.ExplodingGrenade -= manager.OnGrenade;
            //Exiled.Events.Handlers.Warhead.Detonated -= manager.OnAlpha;
            Exiled.Events.Handlers.Player.Spawned -= manager.ApplyCustomSpawnPoint;
            Exiled.Events.Handlers.Player.SearchingPickup -= manager.OnItemSearching;
            Exiled.Events.Handlers.Player.PickingUpItem -= manager.OnItemPicked;
            //Exiled.Events.Handlers.Player.InteractingDoor -= manager.OnInteracted;
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

    public static class EventHandler
    {
        public static Event<HealthObjectDeadEventArgs> HealthObjectDead { get; set; } = new Event<HealthObjectDeadEventArgs>();

        internal static void OnHealthObjectDead(HealthObjectDeadEventArgs ev)
        {
            EventHandler.HealthObjectDead.InvokeSafely(ev);
        }
    }

    public class EventManager
    {
        Config config = AdvancedMERTools.Singleton.Config;

        public void OnGrenade(Exiled.Events.EventArgs.Map.ExplodingGrenadeEventArgs ev)
        {
            foreach (HealthObject health in AdvancedMERTools.Singleton.healthObjects)
            {
                try
                {
                    health.OnGrenadeExplode(ev);
                }
                catch (NullReferenceException _)
                {
                    continue;
                }
            }
            //AdvancedMERTools.Singleton.healthObjects.ForEach(x => x.OnGrenadeExplode(ev));
        }

        public void OnItemSearching(Exiled.Events.EventArgs.Player.SearchingPickupEventArgs ev)
        {
            Log.Debug($"AMERT: OnItemSearching() has been called: player: {ev.Player.Nickname} - pickup type: {ev.Pickup.Type}");

            List<InteractablePickup> list = AdvancedMERTools.Singleton.InteractablePickups.FindAll(x => x.Pickup == ev.Pickup);
            List<Pickup> removeList = new List<Pickup> { };
            Log.Debug($"AMERT: OnItemSearching(): found InteractablePickups - total: {AdvancedMERTools.Singleton.InteractablePickups.Count} - count with matching pickup: {list.Count}");
            foreach (InteractablePickup interactable in list)
            {
                Log.Debug($"- interactable.name: {interactable.name} - interactable.Pickup.GameObject.name: {interactable.Pickup.GameObject.name}");
                if (interactable is FInteractablePickup)
                    continue;
                if (interactable.Base.InvokeType.HasFlag(InvokeType.Searching))
                {
                    Log.Debug($"-- calling RunProcess with ev.Pickup.GameObject.name: {ev.Pickup.GameObject.name} and pickup type: {ev.Pickup.Type}");
                    interactable.RunProcess(ev.Player, ev.Pickup, out bool Remove);
                    if (interactable.Base.CancelActionWhenActive)
                    {
                        ev.IsAllowed = false;
                    }
                    if (Remove && !removeList.Contains(interactable.Pickup))
                    {
                        removeList.Add(interactable.Pickup);
                    }
                }
            }
            foreach (FInteractablePickup interactable in list.Where(x => x is FInteractablePickup))
            {
                if (interactable.Base.InvokeType.HasFlag(InvokeType.Searching))
                {
                    interactable.RunProcess(ev.Player, ev.Pickup, out bool Remove);
                    if (interactable.Base.CancelActionWhenActive.GetValue(new FunctionArgument(interactable, ev.Player), true))
                    {
                        ev.IsAllowed = false;
                    }
                    if (Remove && !removeList.Contains(interactable.Pickup))
                    {
                        removeList.Add(interactable.Pickup);
                    }
                }
            }
            removeList.ForEach(x => x.Destroy());
            AdvancedMERTools.Singleton.dummyGates.ForEach(x => x.OnPickingUp(ev));
        }

        public void OnItemPicked(Exiled.Events.EventArgs.Player.PickingUpItemEventArgs ev)
        {
            Log.Debug($"AMERT: OnItemPicked() has been called: player: {ev.Player.Nickname} - pickup type: {ev.Pickup.Type}");

            List<InteractablePickup> list = AdvancedMERTools.Singleton.InteractablePickups.FindAll(x => x.Pickup == ev.Pickup);
            List<Pickup> removeList = new List<Pickup> { };
            Log.Debug($"AMERT: OnItemPicked(): found InteractablePickups - total: {AdvancedMERTools.Singleton.InteractablePickups.Count} - count with matching pickup: {list.Count}");
            foreach (InteractablePickup interactable in list)
            {
                if (interactable is FInteractablePickup)
                    continue;
                if (interactable.Base.InvokeType.HasFlag(InvokeType.Picked))
                {
                    interactable.RunProcess(ev.Player, ev.Pickup, out bool Remove);
                    if (Remove && !removeList.Contains(interactable.Pickup))
                    {
                        removeList.Add(interactable.Pickup);
                    }
                }
            }
            foreach (FInteractablePickup interactable in list.Where(x => x is FInteractablePickup))
            {
                if (interactable.Base.InvokeType.HasFlag(InvokeType.Picked))
                {
                    interactable.RunProcess(ev.Player, ev.Pickup, out bool Remove);
                    if (Remove && !removeList.Contains(interactable.Pickup))
                    {
                        removeList.Add(interactable.Pickup);
                    }
                }
            }
            removeList.ForEach(x => x.Destroy());
        }

        public void OnSSInput(ReferenceHub sender, ServerSpecificSettingBase setting)
        {
            //ServerConsole.AddLog("INPUTTED!!!");
            SSKeybindSetting sSKeybind = setting.OriginalDefinition as SSKeybindSetting;
            if (sSKeybind != null && (setting as SSKeybindSetting).SyncIsPressed)
            {
                KeyCode key = sSKeybind.SuggestedKey;
                //ServerConsole.AddLog(key.ToString());
                //ServerConsole.AddLog(((int)key).ToString());
                if (AdvancedMERTools.Singleton.IOkeys.ContainsKey((int)key) && Physics.Raycast(sender.PlayerCameraReference.position, sender.PlayerCameraReference.forward, out RaycastHit hit, 1000f, 1))
                {
                    //ServerConsole.AddLog(hit.collider.gameObject.name);
                    foreach (InteractableObject interactable in hit.collider.GetComponentsInParent<InteractableObject>())
                    {
                        Log.Debug($"-- found interactable with type: {interactable.GetType()} - name: {interactable.gameObject.name}");
                        if (!(interactable is FInteractableObject) && interactable.Base.InputKeyCode == (int)key && hit.distance <= interactable.Base.InteractionMaxRange)
                        {
                            Log.Debug($"--- running process....");
                            interactable.RunProcess(Player.Get(sender));
                        }
                    }
                    foreach (FInteractableObject interactable in hit.collider.GetComponentsInParent<FInteractableObject>())
                    {
                        if (interactable.Base.InputKeyCode == (int)key && hit.distance <= interactable.Base.InteractionMaxRange.GetValue(new FunctionArgument(interactable), 0f))
                        {
                            interactable.RunProcess(Player.Get(sender));
                        }
                    }
                }
            }
        }

        //public void OnTeleport(MapEditorReborn.Events.EventArgs.TeleportingEventArgs ev)
        //{
        //    //List<InteractableTeleporter> ITO = AdvancedMERTools.Singleton.InteractableTPs.FindAll(x => (x.TO == ev.EntranceTeleport && x.Base.InvokeType.HasFlag(TeleportInvokeType.Enter))
        //    //|| (x.TO == ev.ExitTeleport && x.Base.InvokeType.HasFlag(TeleportInvokeType.Exit)));
        //    //if (ITO.Count != 0 && ev.Player != null)
        //    //{
        //    //    ITO.ForEach(x => x.RunProcess(ev.Player));
        //    //}
        //}

        public void OnSchematicLoad(ProjectMER.Events.Arguments.SchematicSpawnedEventArgs ev)
        {
            AdvancedMERTools.Singleton.SchematicVariables.Add(ev.Schematic, new Dictionary<string, object> { });
            AdvancedMERTools.Singleton.AMERTGroup.Add(ev.Schematic, new Dictionary<string, List<AMERTInteractable>> { });
            AdvancedMERTools.Singleton.codeClassPair.Add(ev.Schematic, new Dictionary<int, AMERTInteractable> { });
            if (ev.Name.Equals("Gate", StringComparison.InvariantCultureIgnoreCase))
            {
                ev.Schematic.gameObject.AddComponent<DummyGate>();
            }
            DataLoad<GNDTO, GroovyNoise>("GroovyNoises", ev);
            DataLoad<HODTO, HealthObject>("HealthObjects", ev);
            DataLoad<IPDTO, InteractablePickup>("Pickups", ev);
            DataLoad<CCDTO, CustomCollider>("Colliders", ev);
            DataLoad<IODTO, InteractableObject>("Objects", ev);

            DataLoad<FGNDTO, FGroovyNoise>("FGroovyNoises", ev);
            DataLoad<FHODTO, FHealthObject>("FHealthObjects", ev);
            DataLoad<FIPDTO, FInteractablePickup>("FPickups", ev);
            DataLoad<FCCDTO, FCustomCollider>("FColliders", ev);
            DataLoad<FIODTO, FInteractableObject>("FObjects", ev);

            AdvancedMERTools.Singleton.FunctionExecutors.Add(ev.Schematic, new Dictionary<string, FunctionExecutor> { });
            string path = Path.Combine(ev.Schematic.DirectoryPath, ev.Schematic.Name + "-Functions.json");
            if (File.Exists(path))
            {
                List<FEDTO> ts = JsonConvert.DeserializeObject<List<FEDTO>>(File.ReadAllText(path), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = DSbinder });
                foreach (FEDTO dto in ts)
                {
                    FunctionExecutor tclass = ev.Schematic.gameObject.AddComponent<FunctionExecutor>();
                    tclass.OSchematic = ev.Schematic;
                    tclass.data = dto;
                    //AdvancedMERTools.Singleton.FunctionExecutors[ev.Schematic].Add(t, tclass);
                }
            }
        }

        public void DataLoad<Tdto, Tclass>(string name, ProjectMER.Events.Arguments.SchematicSpawnedEventArgs ev) where Tdto : AMERTDTO where Tclass : AMERTInteractable, new()
        {
            //ServerConsole.AddLog(ev.Schematic.DirectoryPath);
            //ServerConsole.AddLog(ev.Schematic.Base.SchematicName);
            //ServerConsole.AddLog(Assembly.GetAssembly(typeof(Real)).FullName);
            string path = Path.Combine(ev.Schematic.DirectoryPath, ev.Schematic.Name + $"-{name}.json");
            if (File.Exists(path))
            {
                List<Tdto> ts = JsonConvert.DeserializeObject<List<Tdto>>(File.ReadAllText(path), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = DSbinder });
                foreach (Tdto dto in ts)
                {
                    Transform target = FindObjectWithPath(ev.Schematic.transform, dto.ObjectId);
                    Tclass tclass = target.gameObject.AddComponent<Tclass>();
                    tclass.Base = dto;
                    tclass.Active = dto.Active;
                    tclass.OSchematic = ev.Schematic;
                    AdvancedMERTools.Singleton.codeClassPair[ev.Schematic].Add(dto.Code, tclass);
                    if (!AdvancedMERTools.Singleton.AMERTGroup[ev.Schematic].ContainsKey(dto.ScriptGroup))
                        AdvancedMERTools.Singleton.AMERTGroup[ev.Schematic].Add(dto.ScriptGroup, new List<AMERTInteractable> { });
                    if (dto.ScriptGroup != null && dto.ScriptGroup != "")
                        AdvancedMERTools.Singleton.AMERTGroup[ev.Schematic][dto.ScriptGroup].Add(tclass);
                }
            }
        }

        static DataSerializationBinder DSbinder = new DataSerializationBinder();

        public class DataSerializationBinder : ISerializationBinder
        {
            Dictionary<string, Type> types;
            DefaultSerializationBinder db = new DefaultSerializationBinder();

            void ISerializationBinder.BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                db.BindToName(serializedType, out assemblyName, out typeName);
            }

            Type ISerializationBinder.BindToType(string assemblyName, string typeName)
            {
                if (types == null)
                {
                    types = Assembly.GetAssembly(typeof(AdvancedMERTools)).GetTypes().Where(t => t.IsClass && !t.IsAbstract && (typeof(Value).IsAssignableFrom(t) || typeof(Function).IsAssignableFrom(t))).ToDictionary(x => x.Name);
                    //ServerConsole.AddLog(string.Join("\n", types.Keys));
                }
                if (types.ContainsKey(typeName))
                {
                    return types[typeName];
                }
                else
                {
                    return db.BindToType(assemblyName, typeName);
                }
            }
        }

        public static Transform FindObjectWithPath(Transform target, string pathO)
        {
            pathO = pathO.Trim();
            if (pathO != "")
            {
                string[] path = pathO.Split(' ');
                for (int i = path.Length - 1; i > -1; i--)
                {
                    if (target.childCount == 0 || target.childCount <= int.Parse(path[i].ToString()))
                    {
                        ServerLogs.AddLog(ServerLogs.Modules.Administrative, "Advanced MER tools: Could not find appropriate child!", ServerLogs.ServerLogType.RemoteAdminActivity_Misc);
                        break;
                    }
                    target = target.GetChild(int.Parse(path[i]));
                }
            }
            return target;
        }

        public void OnGen()
        {
            AdvancedMERTools.Singleton.healthObjects.Clear();
            AdvancedMERTools.Singleton.InteractablePickups.Clear();
            AdvancedMERTools.Singleton.dummyDoors.Clear();
            AdvancedMERTools.Singleton.dummyGates.Clear();
            //AdvancedMERTools.Singleton.InteractableTPs.Clear();
            AdvancedMERTools.Singleton.CustomColliders.Clear();
            AdvancedMERTools.Singleton.groovyNoises.Clear();
            AdvancedMERTools.Singleton.codeClassPair.Clear();
            AdvancedMERTools.Singleton.interactableObjects.Clear();
            AdvancedMERTools.Singleton.FunctionExecutors.Clear();
            AdvancedMERTools.Singleton.SchematicVariables.Clear();
            AdvancedMERTools.Singleton.RoundVariable.Clear();
        }

        List<NetworkIdentity> identities = new List<NetworkIdentity> { };

        public void ApplyCustomSpawnPoint(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            if (MapUtils.LoadedMaps.IsEmpty())
            {
                return;
            }
            if (!config.CustomSpawnPointEnable)
            {
                return;
            }

            RoleTypeId playerRoleId = ev.Player.RoleManager.CurrentRole.RoleTypeId;
            List<SerializablePlayerSpawnpoint> list = new();
            foreach (var item in MapUtils.LoadedMaps)
            {
                MapSchematic loadedMapSchematic = item.Value;
                List<SerializablePlayerSpawnpoint> roleSpawns = loadedMapSchematic.PlayerSpawnpoints.Values.Where(spawnPoint => spawnPoint.Roles.Contains(playerRoleId)).ToList();
                list.AddRange(roleSpawns);
            }
            if (list.Count != 0)
            {
                SerializablePlayerSpawnpoint serializable = list.RandomItem();
                // TODO: I think serializable.Position is the position within the room. So need to find a new way to transform that position to the global position
                ev.Player.Teleport(serializable.Position);
            }
        }
    }
}
