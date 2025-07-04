﻿using LabApi.Events;
using LabApi.Events.Arguments.Interfaces;
using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace AdvancedMERTools;

public class InteractableObject : AMERTInteractable
{
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

    protected virtual void Start()
    {
        this.Base = base.Base as IODTO;
        AdvancedMERTools.Singleton.InteractableObjects.Add(this);
        Log.Debug($"Adding InteractableObject: {gameObject.name} ({OSchematic.Name})");
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
        this.Base = ((AMERTInteractable)this).Base as FIODTO;
        AdvancedMERTools.Singleton.InteractableObjects.Add(this);
        Log.Debug($"Adding FInteractableObject: {gameObject.name} ({OSchematic.Name})");
        if (AdvancedMERTools.Singleton.IOkeys.ContainsKey(Base.InputKeyCode))
        {
            AdvancedMERTools.Singleton.IOkeys[Base.InputKeyCode].Add(this);
        }
        else
        {
            AdvancedMERTools.Singleton.IOkeys.Add(Base.InputKeyCode, new List<InteractableObject> { this });
            if (Base.InputKeyCode != (int)ServerSettings.IODefaultKeySetting.SuggestedKey)
            {
                Log.Debug($"-- adding new FIOKeybind setting for key: {(KeyCode)Base.InputKeyCode}");
                ServerSpecificSettingsSync.DefinedSettings = ServerSpecificSettingsSync.DefinedSettings.Append(new SSKeybindSetting(null, $"AMERT - Interactable Object - {(KeyCode)Base.InputKeyCode}", (KeyCode)Base.InputKeyCode)).ToArray();
                ServerSpecificSettingsSync.SendToAll();
            }
        }
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
