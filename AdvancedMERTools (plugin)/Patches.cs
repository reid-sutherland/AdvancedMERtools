using LabApi.Features.Wrappers;
using HarmonyLib;
using Interactables.Interobjects.DoorUtils;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;
using System.Collections.Generic;
using System.IO;
using Utf8Json;
using System.Linq;

namespace AdvancedMERTools;

// TODO: DoorObject doesn't exist anymore.. no similar methods in SerializableDoor... idk what to patch
//[HarmonyPatch(typeof(DoorObject), nameof(DoorObject.Init))]
//public class DoorSpawnPatcher
//{
//    static void Prefix(DoorObject __instance)
//    {
//        if (AdvancedMERTools.Singleton.Config.AutoRun && __instance.gameObject.TryGetComponent(out Interactables.Interobjects.BasicDoor basicDoor) && basicDoor.Rooms.Length == 0)
//        {
//            string str = "DoorLCZ";
//            if (__instance.gameObject.name.Contains("HCZ")) str = "DoorHCZ";
//            if (__instance.gameObject.name.Contains("EZ")) str = "DoorEZ";
//            //SchematicObject @object = ObjectSpawner.SpawnSchematic(str, basicDoor.transform.position, basicDoor.transform.rotation, isStatic: false);
//            SchematicObject @object = ObjectSpawner.SpawnSchematic(str, basicDoor.transform.position, basicDoor.transform.rotation, basicDoor.transform.localScale, null);
//            DummyDoor dummy = @object.gameObject.AddComponent<DummyDoor>();
//            AdvancedMERTools.Singleton.dummyDoors.Add(dummy);
//            dummy.RealDoor = Door.Get(basicDoor);
//        }
//    }
//}

//[HarmonyPatch(nameof(DoorVariant), nameof(DoorVariant.NetworkTargetState), MethodType.Setter)]
//internal static class DoorVariantPatcher
//{
//    public static void Prefix(DoorVariant instance, bool value)
//    {
//        Log.Debug($"Patching DoorVariant.NetworkTargetState: {instance} - value: {value}");
//        if (instance != null)
//        {
//            DummyDoor d = AdvancedMERTools.Singleton.DummyDoors.Find(x => x.RealDoor == Door.Get(instance));
//            if (d != null)
//            {
//                d.OnInteractDoor(value);
//            }
//        }
//    }
//}

//[HarmonyPatch(nameof(DoorVariant), nameof(DoorVariant.NetworkActiveLocks), MethodType.Setter)]
//public class DoorVariantLockPatcher
//{
//    public static void Prefix(DoorVariant instance, ushort value)
//    {
//        CustomDoor d = AdvancedMERTools.Singleton.CustomDoors.Find(x => x.Door == Door.Get(instance));
//        if (d != null)
//        {
//            d.OnLockChange(value);
//        }
//    }
//}

[HarmonyPatch(typeof(MapUtils), nameof(MapUtils.LoadMap), typeof(string))]
public class MapLoadingPatcher
{
    public static void Postfix(string mapName)
    {
        if (string.IsNullOrEmpty(mapName))
        {
            return;
        }
        if (!MapUtils.LoadedMaps.ContainsKey(mapName))
        {
            Log.Error($"MapLoadingPatcher.Postfix: Map '{mapName}' does not exist in MapUtils.LoadedMaps");
            return;
        }

        string path = Path.Combine(ProjectMER.ProjectMER.MapsDir, mapName + "-ITeleporters.json");
        if (File.Exists(path))
        {
            List<ITDTO> iTDTOs = JsonSerializer.Deserialize<List<ITDTO>>(File.ReadAllText(path));
            TeleportObject[] teleports = MapUtils.LoadedMaps[mapName].SpawnedObjects.Where(x => x.Base is SerializableTeleport).Cast<TeleportObject>().ToArray();
            foreach (ITDTO to in iTDTOs)
            {
                int n = int.Parse(to.ObjectId);
                if (n > 0 && n <= teleports.Length)
                {
                    InteractableTeleporter interactable = teleports[n - 1].gameObject.AddComponent<InteractableTeleporter>();
                    interactable.Base = to;
                }
            }
        }
    }
}

//[HarmonyPatch(typeof(ServerListFormatter), nameof(ServerListFormatter.Serialize))]
//public class PlayerCountPatcher
//{
//    static void Prefix(ref JsonWriter writer, ServerListItem value, IJsonFormatterResolver formatterResolver)
//    {
//        if (AdvancedMERTools.Singleton.Config.UseExperimentalFeature)
//        {
//            if (int.TryParse(value.players, out int result))
//                value = new ServerListItem(value.serverId, value.ip, value.port, (result + 1 - ReferenceHub.AllHubs.Count(x => x.Mode == CentralAuth.ClientInstanceMode.DedicatedServer)).ToString(), value.info, value.pastebin, value.version, value.friendlyFire, value.modded, value.whitelist, value.officialCode);
//            else
//                ServerConsole.AddLog("Send folloing message to me. - Mujishung: " + value.players);
//        }
//    }
//}