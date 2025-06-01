//using Exiled.API.Features;
//using Exiled.API.Features.Doors;
//using Exiled.API.Enums;
//using Exiled.Events.EventArgs.Player;
using LabApi;
using ProjectMER.Features;
using ProjectMER.Features.Serializable;
using UnityEngine;

//using System.IO;
//using Utf8Json;
//using System.Reflection.Emit;
//using System.Reflection;
//using PlayerRoles.FirstPersonControl;
//using PlayerRoles.FirstPersonControl.NetworkMessages;
//using Mirror;
//using PlayerRoles;
//using RelativePositioning;

namespace AdvancedMERTools;

public class CustomDoor : AMERTInteractable
{
    public new CDDTO Base { get; private set; }

    public Animator Animator { get; private set; }

    public Door Door { get; private set; }

    protected void Start()
    {
        Base = base.Base as CDDTO;
        AdvancedMERTools.Singleton.CustomDoors.Add(this);
        Animator = EventManager.FindObjectWithPath(transform, Base.Animator).GetComponent<Animator>();

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
        Door = Door.Get(doorGameObject);    // this should create a new Exiled Door using the gameObject
        Door.Transform.parent = this.transform;
        Exiled.Events.Handlers.Player.InteractingDoor += OnInteract;
        Exiled.Events.Handlers.Player.DamagingDoor += OnDestroy;
    }

    public void OnDestroy()
    {
        AdvancedMERTools.Singleton.CustomDoors.Remove(this);
    }

    public void OnInteract(Exiled.Events.EventArgs.Player.InteractingDoorEventArgs ev)
    {
        if (ev.Door != Door || !ev.IsAllowed)
        {
            return;
        }
        Animator.Play(Door.IsOpen ? Base.CloseAnimation : Base.OpenAnimation);
    }

    public void OnDestroy(Exiled.Events.EventArgs.Player.DamagingDoorEventArgs ev)
    {
        if (ev.Door != Door || !ev.IsAllowed)
        {
            return;
        }
        if (ev.Damage >= (Door.Base as Interactables.Interobjects.BreakableDoor).RemainingHealth)
        {
            Animator.Play(Base.BrokenAnimation);
        }
    }

    public void OnLockChange(ushort value)
    {
        if (value == 0)
        {
            Animator.Play(Base.UnlockAnimation);
        }
        else
        {
            Animator.Play(Base.LockAnimation);
        }
    }
}
