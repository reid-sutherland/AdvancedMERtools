using LabApi.Events;
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
        Log.Debug($"Adding interactable object: {gameObject.name} ({OSchematic.Name})");
        if (AdvancedMERTools.Singleton.IOkeys.ContainsKey(Base.InputKeyCode))
        {
            AdvancedMERTools.Singleton.IOkeys[Base.InputKeyCode].Add(this);
        }
        else
        {
            ServerSpecificSettingsSync.DefinedSettings = ServerSpecificSettingsSync.DefinedSettings.Append(new SSKeybindSetting(null, $"AMERT - Interactable Object - {(KeyCode)Base.InputKeyCode}", (KeyCode)Base.InputKeyCode)).ToArray();
            ServerSpecificSettingsSync.SendToAll();
            AdvancedMERTools.Singleton.IOkeys.Add(Base.InputKeyCode, new List<InteractableObject> { this });
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
            { IPActionType.Explode, () => RandomExecutionModule.Execute(Base.ExplodeModules, args) },
            { IPActionType.PlayAnimation, () => RandomExecutionModule.Execute(Base.AnimationModules, args) },
            { IPActionType.Warhead, () => AlphaWarhead(Base.warheadActionType) },
            { IPActionType.SendMessage, () => RandomExecutionModule.Execute(Base.MessageModules, args) },
            { IPActionType.DropItems, () => DropItem.Execute(Base.dropItems, args) },
            { IPActionType.SendCommand, () => RandomExecutionModule.Execute(Base.commandings, args) },
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
            { IPActionType.GiveEffect, () => RandomExecutionModule.Execute(Base.effectGivingModules, args) },
            { IPActionType.PlayAudio, () => RandomExecutionModule.Execute(Base.AudioModules, args) },
            { IPActionType.CallGroovieNoise, () => RandomExecutionModule.Execute(Base.GroovieNoiseToCall, args) },
            { IPActionType.CallFunction, () => RandomExecutionModule.Execute(Base.FunctionToCall, args) },
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
        if (AdvancedMERTools.Singleton.IOkeys.ContainsKey(Base.InputKeyCode))
        {
            AdvancedMERTools.Singleton.IOkeys[Base.InputKeyCode].Add(this);
        }
        else
        {
            List<ServerSpecificSettingBase> original = ServerSpecificSettingsSync.DefinedSettings.ToList();
            int index = original.FindIndex(x => x is SSGroupHeader && x.Label.Equals("AMERT Keybinds"));
            bool flag = false;
            SSKeybindSetting key = new SSKeybindSetting(null, $"AMERT - Interactable Object - {(KeyCode)Base.InputKeyCode}", (KeyCode)Base.InputKeyCode);
            if (index == -1)
            {
                original.Add(new SSGroupHeader("AMERT Keybinds"));
            }
            else
            {
                for (index++; index < original.Count; index++)
                {
                    if (original[index].Label == null || !original[index].Label.StartsWith("AMERT"))
                    {
                        flag = true;
                        original.Insert(index, key);
                        break;
                    }
                }
            }
            if (!flag)
            {
                original.Add(key);
            }

            ServerSpecificSettingsSync.DefinedSettings = original.ToArray();
            ServerSpecificSettingsSync.SendToAll();
            AdvancedMERTools.Singleton.IOkeys.Add(Base.InputKeyCode, new List<InteractableObject> { this });
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
            { IPActionType.Explode, () => FRandomExecutionModule.Execute(Base.ExplodeModules, args) },
            { IPActionType.PlayAnimation, () => FRandomExecutionModule.Execute(Base.AnimationModules, args) },
            { IPActionType.Warhead, () => AlphaWarhead(Base.warheadActionType.GetValue<WarheadActionType>(args, 0)) },
            { IPActionType.SendMessage, () => FRandomExecutionModule.Execute(Base.MessageModules, args) },
            { IPActionType.DropItems, () => FDropItem.Execute(Base.dropItems, args) },
            { IPActionType.SendCommand, () => FRandomExecutionModule.Execute(Base.commandings, args) },
            {
                IPActionType.UpgradeItem, () =>
                {
                    if (player.GameObject.TryGetComponent<Collider>(out Collider col))
                    {
                        List<int> vs = new();
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
            { IPActionType.GiveEffect, () => FRandomExecutionModule.Execute(Base.effectGivingModules, args) },
            { IPActionType.PlayAudio, () => FRandomExecutionModule.Execute(Base.AudioModules, args) },
            { IPActionType.CallGroovieNoise, () => FRandomExecutionModule.Execute(Base.GroovieNoiseToCall, args) },
            { IPActionType.CallFunction, () => FRandomExecutionModule.Execute(Base.FunctionToCall, args) },
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
