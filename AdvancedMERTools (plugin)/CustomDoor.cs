using LabApi.Features.Wrappers;
using LabApi.Events.Handlers;
using LabApi.Events.Arguments.PlayerEvents;
using MapGeneration;
using ProjectMER.Features.Serializable;
using System.Linq;
using UnityEngine;

namespace AdvancedMERTools;

public class CustomDoor : AMERTInteractable
{
    public new CDDTO Base { get; set; }

    public Animator Animator { get; private set; }

    public Door Door { get; private set; }

    protected void Start()
    {
        Base = base.Base as CDDTO;
        AdvancedMERTools.Singleton.CustomDoors.Add(this);
        Animator = AMERTEventHandlers.FindObjectWithPath(transform, Base.Animator).GetComponent<Animator>();

        SerializableDoor serializableDoor = new()
        {
            // health?
            DoorType = (ProjectMER.Features.Enums.DoorType)(int)Base.DoorType,
            // ignored damage sources?
            RequiredPermissions = Base.DoorPermissions,
            Room = Room.Get(FacilityZone.Surface).ToList().FirstOrDefault().Name.ToString(),        // this seems odd but the original code had Surface so
            Position = transform.position + transform.rotation.eulerAngles + Base.DoorInstallPos,
            Rotation = Quaternion.LookRotation(transform.TransformDirection(Base.DoorInstallRot), Vector3.up).eulerAngles,
            Scale = Base.DoorInstallScl,
        };

        // TODO: I have no idea if this in on the right track lol
        Room doorRoom = Room.GetRoomAtPosition(serializableDoor.Position);
        GameObject doorGameObject = serializableDoor.SpawnOrUpdateObject(doorRoom);
        Door = doorGameObject.GetComponent<Door>();  // this should create a new Exiled Door using the gameObject
        Door.Transform.parent = transform;

        PlayerEvents.InteractingDoor += OnInteractingDoor;
        PlayerEvents.DamagingShootingTarget += OnDamagingDoor;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        AdvancedMERTools.Singleton.CustomDoors.Remove(this);
    }

    public void OnInteractingDoor(PlayerInteractingDoorEventArgs ev)
    {
        if (!ev.IsAllowed || !ev.Door.Equals(Door))
        {
            return;
        }
        Animator.Play(Door.IsOpened ? Base.CloseAnimation : Base.OpenAnimation);
    }

    public void OnDamagingDoor(PlayerDamagingShootingTargetEventArgs ev)
    {
        if (!ev.IsAllowed || !ev.ShootingTarget.GameObject.Equals(Door.GameObject))
        {
            return;
        }
        if (ev.ShootingTarget.IsDestroyed)
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
