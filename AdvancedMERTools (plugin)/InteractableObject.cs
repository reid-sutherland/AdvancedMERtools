using AdminToys;
using LabApi.Events;
using LabApi.Events.Arguments.Interfaces;
using LabApi.Features.Wrappers;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace AdvancedMERTools;

public class InteractableObject : AMERTInteractable
{
    public const KeyCode InteractableToyKeycode = KeyCode.E;
    public new IODTO Base { get; set; }

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
    };

    protected void SpawnInteractableToy(AdminToys.PrimitiveObjectToy primitiveObjectToy)
    {
        InteractableToy interactableToy = InteractableToy.Create(primitiveObjectToy.transform, false);
        switch (primitiveObjectToy.PrimitiveType)
        {
            case PrimitiveType.Plane:
                interactableToy.Shape = InvisibleInteractableToy.ColliderShape.Box;
                interactableToy.Transform.localScale = new Vector3(interactableToy.Transform.localScale.x * 10, 0.01f, interactableToy.Transform.localScale.z * 10);
                break;
            case PrimitiveType.Quad:
                interactableToy.Shape = InvisibleInteractableToy.ColliderShape.Box;
                interactableToy.Transform.localScale = new Vector3(interactableToy.Transform.localScale.x, interactableToy.Transform.localScale.y, 0.01f);
                break;
            case PrimitiveType.Cube:
                interactableToy.Shape = InvisibleInteractableToy.ColliderShape.Box;
                break;
            case PrimitiveType.Sphere:
                interactableToy.Shape = InvisibleInteractableToy.ColliderShape.Sphere;
                break;
            case PrimitiveType.Capsule:
                interactableToy.Shape = InvisibleInteractableToy.ColliderShape.Capsule;
                break;
            default:
                interactableToy.Destroy();
                return;
        }

        interactableToy.Transform.localScale = Vector3.one * 1.1f;
        interactableToy.OnInteracted += p => RunProcess(p);
        interactableToy.Spawn();

        if (AdvancedMERTools.Singleton.Config.InteractableObjectDebug)
        {
            LabApi.Features.Wrappers.PrimitiveObjectToy indicator = LabApi.Features.Wrappers.PrimitiveObjectToy.Create(transform, false);
            indicator.Type = primitiveObjectToy.PrimitiveType;
            indicator.Transform.localScale = Vector3.one * 1.1f;
            indicator.Flags = PrimitiveFlags.Visible;
            indicator.Color = new Color(1, 1, 1, 0.2f);
            indicator.Spawn();
        }
    }

    protected virtual void Start()
    {
        Base = base.Base as IODTO;
        Log.Debug($"Adding InteractableObject: {gameObject.name} ({OSchematic.Name})");

        Register();
    }

    protected virtual void Register()
    {
        AdvancedMERTools.Singleton.InteractableObjects.Add(this);
        if (Base.InputKeyCode == (int)InteractableToyKeycode)
        {
            if (TryGetComponent<AdminToys.PrimitiveObjectToy>(out var component))
            {
                SpawnInteractableToy(component);
            }

            foreach (AdminToys.PrimitiveObjectToy primitiveObjectToy in GetComponentsInChildren<AdminToys.PrimitiveObjectToy>())
            {
                Timing.CallDelayed(1f, () => SpawnInteractableToy(primitiveObjectToy));
            }

            return;
        }
        if (AdvancedMERTools.Singleton.IOkeys.ContainsKey(Base.InputKeyCode))
        {
            AdvancedMERTools.Singleton.IOkeys[Base.InputKeyCode].Add(this);
        }
        else
        {
            AdvancedMERTools.Singleton.IOkeys.Add(Base.InputKeyCode, new List<InteractableObject> { this });
            if (Base.InputKeyCode != (int)ServerSettings.IODefaultKeySetting.SuggestedKey)
            {
                Log.Debug($"-- adding new IOKeybind setting for key: {(KeyCode)Base.InputKeyCode}");
                ServerSpecificSettingsSync.DefinedSettings = ServerSpecificSettingsSync.DefinedSettings.Append(new SSKeybindSetting(null, $"AMERT - Interactable Object - {(KeyCode)Base.InputKeyCode}", (KeyCode)Base.InputKeyCode)).ToArray();
                ServerSpecificSettingsSync.SendToAll();
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        AdvancedMERTools.Singleton.InteractableObjects.Remove(this);
    }

    public virtual void RunProcess(Player player)
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
            Transform = this.transform,
            TargetCalculated = false,
        };
        var actionExecutors = new Dictionary<IPActionType, Action>
        {
            { IPActionType.Disappear, () => Destroy(this.gameObject, 0.1f) },
            { IPActionType.Explode, () => ExplodeModule.Execute(Base.ExplodeModules, args) },
            { IPActionType.PlayAnimation, () => AnimationDTO.Execute(Base.AnimationModules, args) },
            { IPActionType.Warhead, () => AlphaWarhead(Base.warheadActionType) },
            { IPActionType.SendMessage, () => MessageModule.Execute(Base.MessageModules, args) },
            { IPActionType.DropItems, () => DropItem.Execute(Base.dropItems, args) },
            { IPActionType.SendCommand, () => Commanding.Execute(Base.commandings, args) },
            {
                IPActionType.UpgradeItem, () =>
                {
                    if (player.GameObject.TryGetComponent<Collider>(out Collider col))
                    {
                        List<int> vs = new List<int>();
                        for (int j = 0; j < 5; j++)
                        {
                            if (Base.Scp914Mode.HasFlag((Scp914Mode)j))
                            {
                                vs.Add(j);
                            }
                        }
                        Scp914.Scp914Upgrader.Upgrade(
                            new Collider[] { col },
                            Scp914.Scp914Mode.Held,
                            (Scp914.Scp914KnobSetting)vs.RandomItem()
                        );
                    }
                }
            },
            { IPActionType.GiveEffect, () => EffectGivingModule.Execute(Base.effectGivingModules, args) },
            { IPActionType.PlayAudio, () => AudioModule.Execute(Base.AudioModules, args) },
            { IPActionType.CallGroovieNoise, () => CGNModule.Execute(Base.GroovieNoiseToCall, args) },
            { IPActionType.CallFunction, () => CFEModule.Execute(Base.FunctionToCall, args) },
        };
        foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
        {
            if (Base.ActionType.HasFlag(type) && actionExecutors.TryGetValue(type, out var execute))
            {
                Log.Debug($"- IO: executing IOAction: {type}");
                execute();
            }
        }

        PlayerIOInteracted.InvokeEvent(new PlayerIOInteractedEventArgs(player));
    }

    public class PlayerIOInteractedEventArgs : EventArgs, IPlayerEvent
    {
        public Player Player { get; }

        public PlayerIOInteractedEventArgs(Player player)
        {
            Player = player;
        }
    }

    // From external code, register a handler to this event to trigger when a player interacts with the object
    public event LabEventHandler<PlayerIOInteractedEventArgs> PlayerIOInteracted;

    public void OnPlayerIOInteracted(PlayerIOInteractedEventArgs ev)
    {
        PlayerIOInteracted.InvokeEvent(ev);
    }
}

public class FInteractableObject : InteractableObject
{
    public new FIODTO Base { get; set; }

    protected override void Start()
    {
        Base = ((AMERTInteractable)this).Base as FIODTO;
        Log.Debug($"Adding FInteractableObject: {gameObject.name} ({OSchematic.Name})");

        Register();
    }

    public override void RunProcess(Player player)
    {
        if (!Active)
        {
            return;
        }

        FunctionArgument args = new FunctionArgument(this, player);
        var actionExecutors = new Dictionary<IPActionType, Action>
        {
            { IPActionType.Disappear, () => Destroy(this.gameObject, 0.1f) },
            { IPActionType.Explode, () => FExplodeModule.Execute(Base.ExplodeModules, args) },
            { IPActionType.PlayAnimation, () => FAnimationDTO.Execute(Base.AnimationModules, args) },
            { IPActionType.Warhead, () => AlphaWarhead(Base.warheadActionType.GetValue<WarheadActionType>(args, 0)) },
            { IPActionType.SendMessage, () => FMessageModule.Execute(Base.MessageModules, args) },
            { IPActionType.DropItems, () => FDropItem.Execute(Base.dropItems, args) },
            { IPActionType.SendCommand, () => FCommanding.Execute(Base.commandings, args) },
            { IPActionType.UpgradeItem, () =>
                {
                    if (player.GameObject.TryGetComponent<Collider>(out Collider col))
                    {
                        List<int> vs = new List<int>();
                        Scp914Mode mode = Base.Scp914Mode.GetValue<Scp914Mode>(args, 0);
                        for (int j = 0; j < 5; j++)
                        {
                            if (mode.HasFlag((Scp914Mode)j))
                            {
                                vs.Add(j);
                            }
                        }
                        Scp914.Scp914Upgrader.Upgrade(
                            new Collider[] { col },
                            Scp914.Scp914Mode.Held,
                            (Scp914.Scp914KnobSetting)vs.RandomItem()
                        );
                    }
                }
            },
            { IPActionType.GiveEffect, () => FEffectGivingModule.Execute(Base.effectGivingModules, args) },
            { IPActionType.PlayAudio, () => FAudioModule.Execute(Base.AudioModules, args) },
            { IPActionType.CallGroovieNoise, () => FCGNModule.Execute(Base.GroovieNoiseToCall, args) },
            { IPActionType.CallFunction, () => FCFEModule.Execute(Base.FunctionToCall, args) },
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
