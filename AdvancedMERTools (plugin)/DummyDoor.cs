using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using ProjectMER.Features;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedMERTools;

public class DummyDoor : MonoBehaviour
{
    public Animator Animator { get; private set; }

    public SerializableDoor SerializableDoor { get; private set; }

    public Door RealDoor { get; private set; } = null;

    public static readonly Config Config = AdvancedMERTools.Singleton.Config;

    public void Start()
    {
        MEC.Timing.CallDelayed(1f, () =>
        {
            Animator = this.transform.GetChild(0).GetComponent<Animator>();
            if (RealDoor == null)
            {
                // TODO: There's probably a better way to do this
                // DoorObject doesn't exist anymore so try to find MapEditorObjects that are doors
                foreach (MapEditorObject mapEditorObject in FindObjectsByType(typeof(MapEditorObject), FindObjectsSortMode.None))
                {
                    if (SerializableDoor == mapEditorObject.Base)
                    {
                        RealDoor = mapEditorObject.GetComponent<Door>();
                        break;
                    }
                }
                if (RealDoor == null)
                {
                    float distance = float.MaxValue;
                    foreach (Door door in Door.List)
                    {
                        if (distance > Vector3.Distance(door.Position, this.transform.position))
                        {
                            distance = Vector3.Distance(door.Position, transform.position);
                            RealDoor = door;
                        }
                    }
                    if (RealDoor == null)
                    {
                        ServerConsole.AddLog("Failed to find proper door!", ConsoleColor.Red);
                        Destroy(this.gameObject);
                    }
                }
            }
            this.transform.parent = RealDoor.Base.transform;
            this.transform.localEulerAngles = Vector3.zero;
            this.transform.localPosition = Vector3.zero;
            if (RealDoor.Base.Rooms.Length != 0)
            {
                AdvancedMERTools.Singleton.DummyDoors.Remove(this);
                Destroy(this.gameObject);
                return;
            }
            Animator.Play("DoorClose");
        });
    }

    public void OnInteractDoor(bool trigger)
    {
        if (this.RealDoor == null || Animator == null)
        {
            return;
        }
        Animator.Play(trigger ? "DoorOpen" : "DoorClose");
    }

    public void Update()
    {
        if (RealDoor == null)
        {
            return;
        }
        if ((RealDoor as BreakableDoor).IsBroken)
        {
            AdvancedMERTools.Singleton.DummyDoors.Remove(this);
            Destroy(this.gameObject, 0.5f);
        }
    }
}

public class DummyGate : MonoBehaviour, IDoorPermissionRequester
{
    public Animator Animator { get; private set; }

    public GateSerializable GateSerializable { get; private set; }

    public Pickup[] Pickups { get; set; }

    public float Cooldown { get; set; } = 0f;

    private bool isOpened = false;

    // IDoorPermissionRequester
    public DoorPermissionsPolicy PermissionsPolicy { get; set; }
    public string RequesterLogSignature { get; } = "DummyGate";

    public void Start()
    {
        PermissionsPolicy = new()
        {
            RequiredPermissions = GateSerializable.DoorPermissions,
            RequireAll = GateSerializable.RequireAllPermission,
            Bypass2176 = false,
        };

        Pickups = gameObject.GetComponentsInChildren<InventorySystem.Items.Pickups.ItemPickupBase>().Select(x => Pickup.Get(x)).ToArray();
        if (MapUtils.LoadedMaps.Count > 0)
        {
            List<GateSerializable> gates = new();
            foreach (MapSchematic map in MapUtils.LoadedMaps.Values)
            {
                if (AdvancedMERTools.Singleton.Config.Gates.TryGetValue(map.Name, out List<GateSerializable> mapGates))
                {
                    gates.AddRange(mapGates);
                }
            }
            // wtf is this code...
            if (gates.Count > AdvancedMERTools.Singleton.DummyGates.Count)
            {
                GateSerializable = gates[AdvancedMERTools.Singleton.DummyGates.Count];
                if (GateSerializable.IsOpened)
                {
                    MEC.Timing.CallDelayed(3f, () => { IsOpened = true; });
                }
            }
        }

        Animator = this.transform.GetChild(1).GetComponent<Animator>();
        AdvancedMERTools.Singleton.DummyGates.Add(this);
        MEC.Timing.RunCoroutine(Enumerator());
    }

    private IEnumerator<float> Enumerator()
    {
        yield return MEC.Timing.WaitUntilTrue(() => Round.IsRoundStarted);
        MEC.Timing.CallDelayed(0.3f, Apply);
        yield break;
    }

    public void Apply()
    {
        // NOTE: This was already commented out by AMERT
        //ReferenceHub hub = AdvancedMERTools.MakeAudio(out int id);
        //MEC.Timing.CallDelayed(0.35f, () =>
        //{
        //    hub.authManager.UserId = "ID_Dedicated";
        //    //Central Core.
        //    hub.authManager.NetworkSyncedUserId = "ID_Dedicated";
        //    hub.nicknameSync.DisplayName = $"{id}-Gate";
        //    hub.characterClassManager.GodMode = true;
        //    hub.transform.localScale = Vector3.one * -0.01f;
        //    foreach (Player player in Player.List)
        //    {
        //        Server.SendSpawnMessage.Invoke(null, new object[]
        //        {
        //            hub.netIdentity,
        //            player.Connection
        //        });
        //    }
        //    FirstPersonMovementModule module = (hub.roleManager.CurrentRole as FpcStandardRoleBase).FpcModule;
        //    //Preventer.
        //    module.Position = this.transform.position - Vector3.up * 0.1f;
        //    module.Motor.ReceivedPosition = new RelativePosition(this.transform.position - Vector3.up * 0.1f);
        //    module.Noclip.IsActive = true;
        //});
        //MEC.Timing.CallDelayed(0.45f, () =>
        //{
        //    PropertyInfo info = typeof(CentralAuth.PlayerAuthenticationManager).GetProperty("InstanceMode");
        //    info.SetValue(hub.authManager, CentralAuth.ClientInstanceMode.DedicatedServer);
        //    //hub.authManager.UserId = null;
        //});
        //audioPlayer = AudioPlayerBase.Get(hub);
    }

    public void Update()
    {
        if (Cooldown >= 0)
        {
            Cooldown -= Time.deltaTime;
        }
    }

    public void OnSearchingPickup(PlayerSearchingPickupEventArgs ev)
    {
        if (Cooldown > 0)
        {
            return;
        }

        bool toggleOpen = false;
        if (Pickups.Contains(ev.Pickup))
        {
            ev.IsAllowed = false;
            if (GateSerializable != null)
            {
                if (!GateSerializable.IsLocked && CheckPermission(ev.Player, GateSerializable.DoorPermissions))
                {
                    toggleOpen = true;
                }
            }
            else
            {
                toggleOpen = true;
            }
        }
        if (toggleOpen)
        {
            IsOpened = !IsOpened;
        }
    }

    public bool CheckPermission(Player player, DoorPermissionFlags doorPermission)
    {
        if (doorPermission == DoorPermissionFlags.None)
        {
            // LOL: in AMERT, this returned true, so no permissions => open xD
            return false;
        }
        if (player != null)
        {
            if (player.IsBypassEnabled)
            {
                return true;
            }
            if (player.IsSCP)
            {
                return doorPermission.HasFlag(DoorPermissionFlags.ScpOverride);
            }
            if (player.CurrentItem == null)
            {
                return false;
            }
            if (player.CurrentItem is KeycardItem keycard)
            {
                DoorPermissionFlags keycardPermissions = keycard.Base.GetPermissions(this);
                if (GateSerializable != null && GateSerializable.RequireAllPermission)
                {
                    return (keycardPermissions & doorPermission) == doorPermission;
                }
                return (keycardPermissions & doorPermission) > DoorPermissionFlags.None;
            }
        }
        return false;
    }

    public bool IsOpened
    {
        get
        {
            return isOpened;
        }
        set
        {
            if (Animator != null)
            {
                //audioPlayer.CurrentPlay = Path.Combine(Path.Combine(Paths.Configs, "Music"), value ? "GateOpen.ogg" : "GateClose.ogg");
                //audioPlayer.Loop = false;
                //audioPlayer.Play(-1);
                Animator.Play(value ? "GateOpen" : "GateClose");
            }
            Cooldown = 3f;
            isOpened = value;
        }
    }
}
