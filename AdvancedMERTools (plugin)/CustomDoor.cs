using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Player;
using System.IO;
using Utf8Json;
using System.Reflection.Emit;
using System.Reflection;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using Mirror;
using PlayerRoles;
using RelativePositioning;
using Exiled.API.Enums;
using ProjectMER.Features;
using ProjectMER.Features.Serializable;
using LabApi;

namespace AdvancedMERTools;

public class CustomDoor : AMERTInteractable
{
    void Start()
    {
        Base = base.Base as CDDTO;
        AdvancedMERTools.Singleton.customDoors.Add(this);
        animator = EventManager.FindObjectWithPath(transform, Base.animator).GetComponent<Animator>();

        SerializableDoor serializableDoor = new()
        {
            // health?
            DoorType = Base.DoorType switch
            {
                DoorType.LightContainmentDoor => ProjectMER.Features.Enums.DoorType.LightContainmentDoor,
                DoorType.HeavyContainmentDoor => ProjectMER.Features.Enums.DoorType.HeavyContainmentDoor,
                DoorType.EntranceDoor => ProjectMER.Features.Enums.DoorType.EntranceDoor,
                _ => ProjectMER.Features.Enums.DoorType.HeavyContainmentDoor,
            },
            // ignored damage sources?
            RequiredPermissions = (Interactables.Interobjects.DoorUtils.DoorPermissionFlags)Base.DoorPermissions,
            Room = Room.Get(RoomType.Surface).Name,     // this seems odd but the original code had Surface so
            Position = transform.position + transform.rotation.eulerAngles + Base.DoorInstallPos,
            Rotation = Quaternion.LookRotation(transform.TransformDirection(Base.DoorInstallRot), Vector3.up).eulerAngles,
            Scale = Base.DoorInstallScl,
        };

        // TODO: I have no idea if this in on the right track lol
        LabApi.Features.Wrappers.Room doorRoom = LabApi.Features.Wrappers.Room.GetRoomAtPosition(serializableDoor.Position);
        GameObject doorGameObject = serializableDoor.SpawnOrUpdateObject(doorRoom);
        door = Door.Get(doorGameObject);    // this should create a new Exiled Door using the gameObject
        door.Transform.parent = this.transform;
        Exiled.Events.Handlers.Player.InteractingDoor += OnInteract;
        Exiled.Events.Handlers.Player.DamagingDoor += OnDestroy;
    }

    void OnDestroy()
    {
        AdvancedMERTools.Singleton.customDoors.Remove(this);
    }

    void OnInteract(Exiled.Events.EventArgs.Player.InteractingDoorEventArgs ev)
    {
        if (ev.Door != door || !ev.IsAllowed)
            return;
        animator.Play(door.IsOpen ? Base.CloseAnimation : Base.OpenAnimation);
    }

    void OnDestroy(Exiled.Events.EventArgs.Player.DamagingDoorEventArgs ev)
    {
        if (ev.Door != door || !ev.IsAllowed)
            return;
        if (ev.Damage >= (door.Base as Interactables.Interobjects.BreakableDoor).RemainingHealth)
            animator.Play(Base.BrokenAnimation);
    }

    public void OnLockChange(ushort value)
    {
        if (value == 0)
            animator.Play(Base.UnlockAnimation);
        else
            animator.Play(Base.LockAnimation);
    }

    public Animator animator;

    public Door door;

    public new CDDTO Base;
}