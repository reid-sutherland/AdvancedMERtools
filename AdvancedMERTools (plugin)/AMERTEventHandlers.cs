using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PlayerRoles;
using ProjectMER.Events.Arguments;
using ProjectMER.Features;
using ProjectMER.Features.Serializable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace AdvancedMERTools;

public class AMERTEventHandlers : CustomEventsHandler
{
    public Config Config => AdvancedMERTools.Singleton.Config;

    public static List<NetworkIdentity> Identities { get; set; } = new();

    public static DataSerializationBinder DSbinder { get; set; } = new();

    public class DataSerializationBinder : ISerializationBinder
    {
        public Dictionary<string, Type> Types { get; set; }
        public DefaultSerializationBinder DefaultBinder { get; set; } = new();

        void ISerializationBinder.BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            DefaultBinder.BindToName(serializedType, out assemblyName, out typeName);
        }

        Type ISerializationBinder.BindToType(string assemblyName, string typeName)
        {
            if (Types == null)
            {
                Types = Assembly.GetAssembly(typeof(AdvancedMERTools)).GetTypes().Where(t => t.IsClass && !t.IsAbstract && (typeof(Value).IsAssignableFrom(t) || typeof(Function).IsAssignableFrom(t))).ToDictionary(x => x.Name);
            }
            if (Types.ContainsKey(typeName))
            {
                return Types[typeName];
            }
            else
            {
                return DefaultBinder.BindToType(assemblyName, typeName);
            }
        }
    }

    // These must be registered manually as they are not defined in LabApi

    public void OnSSInput(ReferenceHub sender, ServerSpecificSettingBase setting)
    {
        SSKeybindSetting sSKeybind = setting.OriginalDefinition as SSKeybindSetting;
        if (sSKeybind != null && (setting as SSKeybindSetting).SyncIsPressed)
        {
            Log.Debug($"ssKeypress: {sSKeybind.PlayerPrefsKey} - {sSKeybind.SuggestedKey}");
            KeyCode key = sSKeybind.SuggestedKey;
            if (AdvancedMERTools.Singleton.IOkeys.ContainsKey((int)key) && Physics.Raycast(sender.PlayerCameraReference.position, sender.PlayerCameraReference.forward, out RaycastHit hit, 1000f, 1))
            {
                foreach (InteractableObject interactable in hit.collider.GetComponentsInParent<InteractableObject>())
                {
                    Log.Debug($"-- found interactable with type: {interactable.GetType()} - name: {interactable.gameObject.name} - sender: {sender.nicknameSync.DisplayName}");
                    if (!(interactable is FInteractableObject) && interactable.Base.InputKeyCode == (int)key && hit.distance <= interactable.Base.InteractionMaxRange)
                    {
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

    public void OnSchematicSpawned(SchematicSpawnedEventArgs ev)
    {
        AdvancedMERTools.Singleton.SchematicVariables.Add(ev.Schematic, new Dictionary<string, object> { });
        AdvancedMERTools.Singleton.AMERTGroup.Add(ev.Schematic, new Dictionary<string, List<AMERTInteractable>> { });
        AdvancedMERTools.Singleton.CodeClassPair.Add(ev.Schematic, new Dictionary<int, AMERTInteractable> { });
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
            List<FEDTO> ts = JsonConvert.DeserializeObject<List<FEDTO>>(File.ReadAllText(path), new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = DSbinder,
            });
            foreach (FEDTO dto in ts)
            {
                FunctionExecutor tclass = ev.Schematic.gameObject.AddComponent<FunctionExecutor>();
                tclass.OSchematic = ev.Schematic;
                tclass.Data = dto;
                AdvancedMERTools.Singleton.FunctionExecutors[ev.Schematic].Add(dto.FunctionName, tclass);
            }
        }
    }

    // The rest can just be overrided from CustomEventsHandler

    public override void OnServerMapGenerated(MapGeneratedEventArgs ev)
    {
        AdvancedMERTools.Singleton.HealthObjects.Clear();
        AdvancedMERTools.Singleton.InteractablePickups.Clear();
        AdvancedMERTools.Singleton.DummyDoors.Clear();
        AdvancedMERTools.Singleton.DummyGates.Clear();
        AdvancedMERTools.Singleton.CustomColliders.Clear();
        AdvancedMERTools.Singleton.GroovyNoises.Clear();
        AdvancedMERTools.Singleton.CodeClassPair.Clear();
        AdvancedMERTools.Singleton.InteractableObjects.Clear();
        AdvancedMERTools.Singleton.FunctionExecutors.Clear();
        AdvancedMERTools.Singleton.SchematicVariables.Clear();
        AdvancedMERTools.Singleton.RoundVariable.Clear();
    }

    public override void OnServerProjectileExploded(ProjectileExplodedEventArgs ev)
    {
        foreach (HealthObject health in AdvancedMERTools.Singleton.HealthObjects)
        {
            try
            {
                health.OnProjectileExploded(ev);
            }
            catch (NullReferenceException ex)
            {
                Log.Error($"NullReferenceException when calling HealthObject.OnProjectileExploded");
                Log.Debug($"Exception: {ex.Message}");
                continue;
            }
        }
    }

    public override void OnPlayerSearchingPickup(PlayerSearchingPickupEventArgs ev)
    {
        List<InteractablePickup> list = AdvancedMERTools.Singleton.InteractablePickups.FindAll(x => x.Pickup == ev.Pickup);
        List<Pickup> removeList = new() { };
        Log.Debug($"OnPlayerSearchingPickup: found InteractablePickups - total: {AdvancedMERTools.Singleton.InteractablePickups.Count} - count with matching pickup: {list.Count}");
        foreach (InteractablePickup interactable in list)
        {
            Log.Debug($"- interactable.name: {interactable.name} - interactable.Pickup.GameObject.name: {interactable.Pickup.GameObject.name}");
            if (interactable is FInteractablePickup)
            {
                continue;
            }
            if (interactable.Base.InvokeType.HasFlag(InvokeType.Searching))
            {
                Log.Debug($"-- calling RunProcess with ev.Pickup.GameObject.name: {ev.Pickup.GameObject.name} and pickup type: {ev.Pickup.Type}");
                interactable.RunProcess(ev.Player, ev.Pickup, out bool remove);
                if (interactable.Base.CancelActionWhenActive)
                {
                    ev.IsAllowed = false;
                }
                if (remove && !removeList.Contains(interactable.Pickup))
                {
                    removeList.Add(interactable.Pickup);
                }
            }
        }
        foreach (FInteractablePickup interactable in list.Where(x => x is FInteractablePickup))
        {
            if (interactable.Base.InvokeType.HasFlag(InvokeType.Searching))
            {
                interactable.RunProcess(ev.Player, ev.Pickup, out bool remove);
                if (interactable.Base.CancelActionWhenActive.GetValue(new FunctionArgument(interactable, ev.Player), true))
                {
                    ev.IsAllowed = false;
                }
                if (remove && !removeList.Contains(interactable.Pickup))
                {
                    removeList.Add(interactable.Pickup);
                }
            }
        }
        removeList.ForEach(x => x.Destroy());
        AdvancedMERTools.Singleton.DummyGates.ForEach(x => x.OnSearchingPickup(ev));
    }

    public override void OnPlayerPickingUpItem(PlayerPickingUpItemEventArgs ev)
    {
        List<InteractablePickup> list = AdvancedMERTools.Singleton.InteractablePickups.FindAll(x => x.Pickup == ev.Pickup);
        List<Pickup> removeList = new() { };
        Log.Debug($"OnPlayerPickingUpItem: found InteractablePickups - total: {AdvancedMERTools.Singleton.InteractablePickups.Count} - count with matching pickup: {list.Count}");
        foreach (InteractablePickup interactable in list)
        {
            if (interactable is FInteractablePickup)
            {
                continue;
            }
            if (interactable.Base.InvokeType.HasFlag(InvokeType.Picked))
            {
                interactable.RunProcess(ev.Player, ev.Pickup, out bool remove);
                if (remove && !removeList.Contains(interactable.Pickup))
                {
                    removeList.Add(interactable.Pickup);
                }
            }
        }
        foreach (FInteractablePickup interactable in list.Where(x => x is FInteractablePickup))
        {
            if (interactable.Base.InvokeType.HasFlag(InvokeType.Picked))
            {
                interactable.RunProcess(ev.Player, ev.Pickup, out bool remove);
                if (remove && !removeList.Contains(interactable.Pickup))
                {
                    removeList.Add(interactable.Pickup);
                }
            }
        }
        removeList.ForEach(x => x.Destroy());
    }

    public override void OnPlayerSpawned(PlayerSpawnedEventArgs ev)
    {
        if (MapUtils.LoadedMaps.IsEmpty())
        {
            return;
        }
        if (!Config.CustomSpawnPointEnable)
        {
            return;
        }

        RoleTypeId playerRoleId = ev.Player.Role;
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
            ev.Player.Position = serializable.Position + Vector3.up;
        }
    }

    public void DataLoad<Tdto, Tclass>(string name, SchematicSpawnedEventArgs ev) where Tdto : AMERTDTO where Tclass : AMERTInteractable, new()
    {
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
                AdvancedMERTools.Singleton.CodeClassPair[ev.Schematic].Add(dto.Code, tclass);
                if (!AdvancedMERTools.Singleton.AMERTGroup[ev.Schematic].ContainsKey(dto.ScriptGroup))
                {
                    AdvancedMERTools.Singleton.AMERTGroup[ev.Schematic].Add(dto.ScriptGroup, new List<AMERTInteractable> { });
                }
                if (dto.ScriptGroup != null && dto.ScriptGroup != "")
                {
                    AdvancedMERTools.Singleton.AMERTGroup[ev.Schematic][dto.ScriptGroup].Add(tclass);
                }
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
}
