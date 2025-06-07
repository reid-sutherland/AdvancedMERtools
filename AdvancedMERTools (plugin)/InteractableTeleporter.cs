using LabApi.Features.Wrappers;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedMERTools;

public class InteractableTeleporter : AMERTInteractable
{
    public new ITDTO Base { get; set; }

    public TeleportObject Teleport { get; set; }

    public static readonly Dictionary<string, Func<object[], string>> Formatter = new ()
    {
        { "{p_i}", vs => (vs[0] as Player).UserId },
        { "{p_name}", vs => (vs[0] as Player).Nickname },
        {
            "{p_pos}", vs =>
            {
                Vector3 pos = (vs[0] as Player).Position;
                return string.Format("{0} {1} {2}", pos.x, pos.y, pos.z);
            }
        },
        { "{p_room}", vs => (vs[0] as Player).Room.Name.ToString() },
        { "{p_zone}", vs => (vs[0] as Player).Zone.ToString() },
        { "{p_role}", vs => (vs[0] as Player).Role.ToString() },
        { "{p_item}", vs => (vs[0] as Player).CurrentItem.Type.ToString() },
        {
            "{o_pos}", vs =>
            {
                Vector3 pos = (vs[1] as TeleportObject).transform.position;
                return string.Format("{0} {1} {2}", pos.x, pos.y, pos.z);
            }
        },
        { "{o_room}", vs => Room.GetRoomAtPosition((vs[1] as TeleportObject).transform.position).Name.ToString() },
        { "{o_zone}", vs => Room.GetRoomAtPosition((vs[1] as TeleportObject).transform.position).Zone.ToString() },
    };

    protected virtual void Start()
    {
        this.Base = base.Base as ITDTO;
        if (transform.TryGetComponent<TeleportObject>(out TeleportObject tpObject))
        {
            Teleport = tpObject;
            AdvancedMERTools.Singleton.InteractableTeleporters.Add(this);
        }
        else
        {
            Destroy(this);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        AdvancedMERTools.Singleton.InteractableTeleporters.Remove(this);
    }

    public void OnTriggerEnter(Collider collider)
    {
        if (Player.TryGet(collider.gameObject, out Player player) && Base.InvokeType.HasFlag(TeleportInvokeType.Collide))
        {
            RunProcess(player);
        }
    }

    public void RunProcess(Player player)
    {
        if (!Active)
        {
            return;
        }

        ModuleGeneralArguments args = new()
        {
            Interpolations = Formatter,
            InterpolationsList = new object[] { player },
            Player = player,
            Schematic = OSchematic,
            Transform = transform,
            TargetCalculated = false,
        };
        var actionExecutors = new Dictionary<IPActionType, Action>
        {
            { IPActionType.Disappear, () => Destroy(gameObject, 0.1f) },
            { IPActionType.Explode, () => ExplodeModule.Execute(Base.ExplodeModules, args) },
            { IPActionType.PlayAnimation, () => AnimationDTO.Execute(Base.AnimationModules, args) },
            { IPActionType.Warhead, () => AlphaWarhead(Base.WarheadActionType) },
            { IPActionType.SendMessage, () => MessageModule.Execute(Base.MessageModules, args) },
            { IPActionType.DropItems, () => DropItem.Execute(Base.DropItems, args) },
            { IPActionType.SendCommand, () => Commanding.Execute(Base.Commandings, args) },
            { IPActionType.GiveEffect, () => EffectGivingModule.Execute(Base.EffectGivingModules, args) },
            { IPActionType.PlayAudio, () => AudioModule.Execute(Base.AudioModules, args) },
            { IPActionType.CallGroovieNoise, () => CGNModule.Execute(Base.GroovieNoiseToCall, args) },
            { IPActionType.CallFunction, () => CFEModule.Execute(Base.FunctionToCall, args) },
        };
        foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
        {
            if (Base.ActionType.HasFlag(type) && actionExecutors.TryGetValue(type, out var execute))
            {
                execute();
            }
        }
    }
}
