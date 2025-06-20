using InventorySystem.Items.Pickups;
using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedMERTools;

public class InteractablePickup : AMERTInteractable
{
    public new IPDTO Base { get; set; }

    public Pickup Pickup { get; set; }

    public static readonly Dictionary<string, Func<object[], string>> Formatter = new()
    {
        { "{p_i}", vs => (vs[0] as Player).PlayerId.ToString() },
        { "{p_name}", vs => (vs[0] as Player).Nickname },
        {
            "{p_pos}", vs =>
            {
                Vector3 pos = (vs[0] as Player).Position;
                return $"{pos.x} {pos.y} {pos.z}";
            }
        },
        { "{p_room}", vs => (vs[0] as Player).Room.Name.ToString() },
        { "{p_zone}", vs => (vs[0] as Player).Zone.ToString() },
        { "{p_role}", vs => (vs[0] as Player).Role.ToString() },
        { "{p_item}", vs => (vs[0] as Player).CurrentItem.Type.ToString() },
        {
            "{o_pos}", vs =>
            {
                Vector3 pos = (vs[1] as Pickup).Transform.position;
                return $"{pos.x} {pos.y} {pos.z}";
            }
        },
        { "{o_room}", vs => (vs[1] as Pickup).Room.Name.ToString() },
        { "{o_zone}", vs => (vs[1] as Pickup).Room.Zone.ToString() },
    };

    protected virtual void Start()
    {
        this.Base = base.Base as IPDTO;
        if (gameObject.TryGetComponent<ItemPickupBase>(out var pickupBase))
        {
            Pickup = Pickup.Get(pickupBase);
        }
        if (Pickup != null)
        {
            Log.Debug($"Adding interactable pickup: {gameObject.name} ({OSchematic.Name})");
            AdvancedMERTools.Singleton.InteractablePickups.Add(this);
        }
        else
        {
            Destroy(this);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        AdvancedMERTools.Singleton.InteractablePickups.Remove(this);
    }

    public virtual void RunProcess(Player player, Pickup pickup, out bool remove)
    {
        bool r = false;
        remove = false;
        if (pickup != this.Pickup || !Active)
        {
            return;
        }
        ModuleGeneralArguments args = new()
        {
            Interpolations = Formatter,
            InterpolationsList = new object[] { player },
            Player = player,
            Schematic = OSchematic,
            Transform = this.transform,
            TargetCalculated = false,
        };
        var ipActionExecutors = new Dictionary<IPActionType, Action>
        {
            { IPActionType.Disappear, () => r = true },
            { IPActionType.Explode, () => RandomExecutionModule.Execute(Base.ExplodeModules, args) },
            { IPActionType.PlayAnimation, () => RandomExecutionModule.Execute(Base.AnimationModules, args) },
            { IPActionType.Warhead, () => AlphaWarhead(Base.WarheadActionType) },
            { IPActionType.SendMessage, () => RandomExecutionModule.Execute(Base.MessageModules, args) },
            { IPActionType.DropItems, () => DropItem.Execute(Base.DropItems, args) },
            { IPActionType.SendCommand, () => RandomExecutionModule.Execute(Base.Commandings, args) },
            {
                IPActionType.UpgradeItem, () =>
                {
                    if (player.GameObject.TryGetComponent<Collider>(out Collider col))
                    {
                        var vs = Enumerable.Range(0, 5)
                            .Where(j => Base.Scp914Mode.HasFlag((Scp914Mode)j))
                            .ToList();
                        Scp914.Scp914Upgrader.Upgrade(
                            new Collider[] { col },
                            Scp914.Scp914Mode.Held,
                            (Scp914.Scp914KnobSetting)vs.RandomItem()
                        );
                    }
                }
            },
            { IPActionType.GiveEffect, () => RandomExecutionModule.Execute(Base.EffectGivingModules, args) },
            { IPActionType.PlayAudio, () => RandomExecutionModule.Execute(Base.AudioModules, args) },
            { IPActionType.CallGroovieNoise, () => RandomExecutionModule.Execute(Base.GroovieNoiseToCall, args) },
            { IPActionType.CallFunction, () => RandomExecutionModule.Execute(Base.FunctionToCall, args) },
        };
        foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
        {
            if (Base.ActionType.HasFlag(type) && ipActionExecutors.TryGetValue(type, out var execute))
            {
                Log.Debug($"- IP: executing IPAction: {type}");
                execute();
            }
        }
        remove = r;
    }
}

public class FInteractablePickup : InteractablePickup
{
    public new FIPDTO Base { get; set; }

    protected override void Start()
    {
        this.Base = ((AMERTInteractable)this).Base as FIPDTO;
        if (gameObject.TryGetComponent<ItemPickupBase>(out var pickupBase))
        {
            Pickup = Pickup.Get(pickupBase);
        }
        if (Pickup != null)
        {
            AdvancedMERTools.Singleton.InteractablePickups.Add(this);
        }
        else
        {
            Destroy(this);
        }
    }

    public override void RunProcess(Player player, Pickup pickup, out bool remove)
    {
        bool r = false;
        remove = false;
        if (pickup != this.Pickup || !Active)
        {
            return;
        }
        FunctionArgument args = new FunctionArgument(this, player);
        var ipActionExecutors = new Dictionary<IPActionType, Action>
        {
            { IPActionType.Disappear, () => r = true },
            { IPActionType.Explode, () => FRandomExecutionModule.Execute(Base.ExplodeModules, args) },
            { IPActionType.PlayAnimation, () => FRandomExecutionModule.Execute(Base.AnimationModules, args) },
            { IPActionType.Warhead, () => AlphaWarhead(Base.WarheadActionType.GetValue<WarheadActionType>(args, 0)) },
            { IPActionType.SendMessage, () => FRandomExecutionModule.Execute(Base.MessageModules, args) },
            { IPActionType.DropItems, () => FDropItem.Execute(Base.DropItems, args) },
            { IPActionType.SendCommand, () => FRandomExecutionModule.Execute(Base.Commandings, args) },
            {
                IPActionType.UpgradeItem, () =>
                {
                    if (player.GameObject.TryGetComponent<Collider>(out Collider col))
                    {
                        Scp914Mode mode = Base.Scp914Mode.GetValue<Scp914Mode>(args, 0);
                        var vs = Enumerable.Range(0, 5)
                            .Where(j => mode.HasFlag((Scp914Mode)j))
                            .ToList();
                        Scp914.Scp914Upgrader.Upgrade(
                            new Collider[] { col },
                            Scp914.Scp914Mode.Held,
                            (Scp914.Scp914KnobSetting)vs.RandomItem()
                        );
                    }
                }
            },
            { IPActionType.GiveEffect, () => FRandomExecutionModule.Execute(Base.EffectGivingModules, args) },
            { IPActionType.PlayAudio, () => FRandomExecutionModule.Execute(Base.AudioModules, args) },
            { IPActionType.CallGroovieNoise, () => FRandomExecutionModule.Execute(Base.GroovieNoiseToCall, args) },
            { IPActionType.CallFunction, () => FRandomExecutionModule.Execute(Base.FunctionToCall, args) },
        };
        foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
        {
            if (Base.ActionType.HasFlag(type) && ipActionExecutors.TryGetValue(type, out var execute))
            {
                execute();
            }
        }
        remove = r;
    }
}
