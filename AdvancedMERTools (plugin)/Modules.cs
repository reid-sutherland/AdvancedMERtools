using CustomPlayerEffects;
using Footprinting;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using LabApi.Features.Wrappers;
using Mirror;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Utils;
using ProjectMER.Commands.Modifying.Position;

namespace AdvancedMERTools;

public class ModuleGeneralArguments
{
    public SchematicObject Schematic { get; set; }
    public Player Player { get; set; }
    public Player[] Targets { get; set; }
    public bool TargetCalculated { get; set; }
    public Transform Transform { get; set; }
    public Dictionary<string, Func<object[], string>> Interpolations { get; set; }
    public object[] InterpolationsList { get; set; }
}

[Serializable]
public class AMERTInteractable : NetworkBehaviour
{
    public AMERTDTO Base { get; set; }
    public SchematicObject OSchematic { get; set; }
    public bool Active { get; set; }

    protected virtual void OnDestroy()
    {
        AdvancedMERTools.Singleton.CodeClassPair[OSchematic].Remove(Base.Code);
        AdvancedMERTools.Singleton.AMERTGroup[OSchematic][Base.ScriptGroup].Remove(this);
    }

    public static void AlphaWarhead(WarheadActionType type)
    {
        foreach (WarheadActionType warhead in Enum.GetValues(typeof(WarheadActionType)))
        {
            if (type.HasFlag(warhead))
            {
                switch (warhead)
                {
                    case WarheadActionType.Start:
                        Warhead.Start();
                        break;
                    case WarheadActionType.Stop:
                        Warhead.Stop();
                        break;
                    case WarheadActionType.Lock:
                        Warhead.IsLocked = true;
                        break;
                    case WarheadActionType.UnLock:
                        Warhead.IsLocked = false;
                        break;
                    case WarheadActionType.Disable:
                        Warhead.LeverStatus = false;
                        break;
                    case WarheadActionType.Enable:
                        Warhead.LeverStatus = true;
                        break;
                }
            }
        }
    }
}

////////////////////////////////////////////////
// AMERT DTO Classes
////////////////////////////////////////////////

[Serializable]
public class AMERTDTO
{
    public bool Active { get; set; }
    public string ObjectId { get; set; }
    public int Code { get; set; }
    public string ScriptGroup { get; set; }
    public bool UseScriptValue { get; set; }
}

// Module DTO with common fields
public class MDTO : AMERTDTO
{
    // TODO: It triggers me, but for backwards compatibility's sake, reverting back to certain camel case names for now
    public List<AnimationDTO> AnimationModules { get; set; }
    public WarheadActionType warheadActionType { get; set; }
    public List<MessageModule> MessageModules { get; set; }
    public List<DropItem> dropItems { get; set; }
    public List<Commanding> commandings { get; set; }
    public List<ExplodeModule> ExplodeModules { get; set; }
    public List<EffectGivingModule> effectGivingModules { get; set; }
    public List<AudioModule> AudioModules { get; set; }
    public List<CGNModule> GroovieNoiseToCall { get; set; }
    public List<CFEModule> FunctionToCall { get; set; }
}

public class FMDTO : AMERTDTO
{
    public List<FAnimationDTO> AnimationModules { get; set; }
    public ScriptValue warheadActionType { get; set; }
    public List<FMessageModule> MessageModules { get; set; }
    public List<FDropItem> dropItems { get; set; }
    public List<FCommanding> commandings { get; set; }
    public List<FExplodeModule> ExplodeModules { get; set; }
    public List<FEffectGivingModule> effectGivingModules { get; set; }
    public List<FAudioModule> AudioModules { get; set; }
    public List<FCGNModule> GroovieNoiseToCall { get; set; }
    public List<FCFEModule> FunctionToCall { get; set; }
}

[Serializable]
public class HODTO : MDTO
{
    public float Health { get; set; }
    [Range(0, 100)]
    public int ArmorEfficient { get; set; }
    public DeadType DeadType { get; set; }
    public float DeadActionDelay { get; set; }
    public float ResetHPTo { get; set; }
    public bool DoNotDestroyAfterDeath { get; set; }
    public List<WhitelistWeapon> WhitelistWeapons { get; set; }
}

[Serializable]
public class FHODTO : FMDTO
{
    public ScriptValue Health { get; set; }
    [Range(0, 100)]
    public ScriptValue ArmorEfficient { get; set; }
    public DeadType DeadType { get; set; }
    public ScriptValue DeadActionDelay { get; set; }
    public ScriptValue ResetHPTo { get; set; }
    public ScriptValue DoNotDestroyAfterDeath { get; set; }
    public List<FWhitelistWeapon> WhitelistWeapons { get; set; }
}

[Serializable]
public class ITDTO : MDTO
{
    public TeleportInvokeType InvokeType { get; set; }
    public IPActionType ActionType { get; set; }
}

[Serializable]
public class IODTO : MDTO
{
    public int InputKeyCode { get; set; }
    public float InteractionMaxRange { get; set; }
    public IPActionType ActionType { get; set; }
    public Scp914Mode Scp914Mode { get; set; }
}

[Serializable]
public class FIODTO : FMDTO
{
    public int InputKeyCode { get; set; }
    public ScriptValue InteractionMaxRange { get; set; }
    public IPActionType ActionType { get; set; }
    public ScriptValue Scp914Mode { get; set; }
}

[Serializable]
public class IPDTO : MDTO
{
    public InvokeType InvokeType { get; set; }
    public IPActionType ActionType { get; set; }
    public bool CancelActionWhenActive { get; set; }
    public Scp914Mode Scp914Mode { get; set; }
}

[Serializable]
public class FIPDTO : FMDTO
{
    public InvokeType InvokeType { get; set; }
    public IPActionType ActionType { get; set; }
    public ScriptValue CancelActionWhenActive { get; set; }
    public ScriptValue Scp914Mode { get; set; }
}

[Serializable]
public class CCDTO : MDTO
{
    public ColliderActionType ColliderActionType { get; set; }
    public CollisionType CollisionType { get; set; }
    public DetectType DetectType { get; set; }
    public float ModifyHealthAmount { get; set; }
}

[Serializable]
public class FCCDTO : FMDTO
{
    public ColliderActionType ColliderActionType { get; set; }
    public ScriptValue CollisionType { get; set; }
    public ScriptValue DetectType { get; set; }
    public ScriptValue ModifyHealthAmount { get; set; }
}

[Serializable]
public class GNDTO : AMERTDTO
{
    public List<GMDTO> Settings { get; set; }
}

[Serializable]
public class FGNDTO : AMERTDTO
{
    public List<FGMDTO> Settings { get; set; }
}

[Serializable]
public class CDDTO : AMERTDTO
{
    public string Animator { get; set; }
    public DoorType DoorType { get; set; }
    public string OpenAnimation { get; set; }
    public string CloseAnimation { get; set; }
    public string LockAnimation { get; set; }
    public string UnlockAnimation { get; set; }
    public string BrokenAnimation { get; set; }
    public Vector3 DoorInstallPos { get; set; }
    public Vector3 DoorInstallRot { get; set; }
    public Vector3 DoorInstallScl { get; set; }
    public float DoorHealth { get; set; }
    public DoorPermissionFlags DoorPermissions { get; set; }
    public DoorDamageType DoorDamageType { get; set; }
}

[Serializable]
public class DGDTO
{
    public string ObjectId { get; set; }
    public float Health { get; set; }
    public DoorDamageType DamagableDamageType { get; set; }
    public DoorPermissionFlags DoorPermissions { get; set; }
}

////////////////////////////////////////////////
// Random Execution Modules
////////////////////////////////////////////////

[Serializable]
public class RandomExecutionModule
{
    public float ChanceWeight { get; set; }
    public bool ForceExecute { get; set; }
    public float ActionDelay { get; set; }

    public static RandomExecutionModule GetSingleton<T>()
        where T : RandomExecutionModule, new()
    {
        if (!AdvancedMERTools.Singleton.TypeSingletonPair.TryGetValue(typeof(T), out RandomExecutionModule type))
        {
            AdvancedMERTools.Singleton.TypeSingletonPair.Add(typeof(T), type = new T());
        }
        return type;
    }

    public static List<T> SelectList<T>(List<T> list)
        where T : RandomExecutionModule, new()
    {
        float chance = list.Sum(x => x.ChanceWeight);
        chance = UnityEngine.Random.Range(0f, chance);
        List<T> output = new List<T> { };
        foreach (T element in list)
        {
            if (element.ForceExecute)
            {
                output.Add(element);
            }
            else
            {
                if (chance <= 0)
                {
                    continue;
                }
                chance -= element.ChanceWeight;
                if (chance <= 0)
                {
                    output.Add(element);
                }
            }
        }
        return output;
    }

    public static void Execute<T>(List<T> list, ModuleGeneralArguments args)
        where T : RandomExecutionModule, new()
    {
        SelectList(list).ForEach(x => x.Execute(args));
    }

    public static Player[] GetTargets(SendType type, ModuleGeneralArguments args)
    {
        List<Player> targets = new List<Player> { };
        if (type.HasFlag(SendType.AllExceptAboveOne))
        {
            targets.AddRange(Player.List.Where(x => x != args.Player));
        }
        if (type.HasFlag(SendType.Spectators))
        {
            targets.AddRange(Player.List.Where(x => !x.IsAlive));
        }
        if (type.HasFlag(SendType.Alive))
        {
            targets.AddRange(Player.List.Where(x => x.IsAlive));
        }
        if (type.HasFlag(SendType.Interactor))
        {
            targets.Add(args.Player);
        }

        return targets.Distinct().ToArray();
    }

    public virtual void Execute(ModuleGeneralArguments args) { }
}

[Serializable]
public class FRandomExecutionModule
{
    public ScriptValue ChanceWeight { get; set; }
    public ScriptValue ForceExecute { get; set; }
    public ScriptValue ActionDelay { get; set; }
    private float calcedWeight;

    public static List<T> SelectList<T>(List<T> list, FunctionArgument args)
        where T : FRandomExecutionModule, new()
    {
        float chance = list.Sum(x => x.calcedWeight = x.ChanceWeight.GetValue(args, 0f));
        chance = UnityEngine.Random.Range(0f, chance);
        List<T> output = new List<T> { };
        foreach (T element in list)
        {
            if (element.ForceExecute.GetValue(args, false))
            {
                output.Add(element);
            }
            else
            {
                if (chance <= 0)
                {
                    continue;
                }
                chance -= element.calcedWeight;
                if (chance <= 0)
                {
                    output.Add(element);
                }
            }
        }
        return output;
    }

    public static void Execute<T>(List<T> list, FunctionArgument args)
        where T : FRandomExecutionModule, new()
    {
        SelectList(list, args).ForEach(x => x.Execute(args));
    }

    public virtual void Execute(FunctionArgument args) { }
}

[Serializable]
public class CGNModule : RandomExecutionModule
{
    public int GroovieNoiseId { get; set; }
    public string GroovieNoiseGroup { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            if (AdvancedMERTools.Singleton.CodeClassPair[args.Schematic].TryGetValue(GroovieNoiseId, out AMERTInteractable v))
            {
                v.Active = true;
            }
            if (AdvancedMERTools.Singleton.AMERTGroup[args.Schematic].TryGetValue(GroovieNoiseGroup, out List<AMERTInteractable> vs))
            {
                vs.ForEach(x => x.Active = true);
            }
        });
    }
}

[Serializable]
public class FCGNModule : FRandomExecutionModule
{
    public ScriptValue GroovieNoiseId { get; set; }
    public ScriptValue GroovieNoiseGroup { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            if (AdvancedMERTools.Singleton.CodeClassPair[args.Schematic].TryGetValue(GroovieNoiseId.GetValue(args, 0), out AMERTInteractable v))
            {
                v.Active = true;
            }
            if (AdvancedMERTools.Singleton.AMERTGroup[args.Schematic].TryGetValue(GroovieNoiseGroup.GetValue(args, ""), out List<AMERTInteractable> vs))
            {
                vs.ForEach(x => x.Active = true);
            }
        });
    }
}

[Serializable]
public class GMDTO : RandomExecutionModule
{
    public List<int> Targets { get; set; }
    public List<string> TargetGroups { get; set; }
    public bool Enable { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            Targets.ForEach(x =>
            {
                if (AdvancedMERTools.Singleton.CodeClassPair[args.Schematic].TryGetValue(x, out AMERTInteractable v))
                {
                    v.Active = Enable;
                }
            });
            TargetGroups.ForEach(x =>
            {
                if (AdvancedMERTools.Singleton.AMERTGroup[args.Schematic].TryGetValue(x, out List<AMERTInteractable> vs))
                {
                    vs.ForEach(y => y.Active = Enable);
                }
            });
        });
    }
}

[Serializable]
public class FGMDTO : FRandomExecutionModule
{
    public ScriptValue Targets { get; set; }
    public ScriptValue TargetGroups { get; set; }
    public ScriptValue Enable { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            Targets.GetValue(args, new int[] { }).ForEach(x =>
            {
                if (AdvancedMERTools.Singleton.CodeClassPair[args.Schematic].TryGetValue(x, out AMERTInteractable v))
                {
                    v.Active = Enable.GetValue(args, v.Active);
                }
            });
            TargetGroups.GetValue(args, new string[] { }).ForEach(x =>
            {
                if (AdvancedMERTools.Singleton.AMERTGroup[args.Schematic].TryGetValue(x, out List<AMERTInteractable> vs))
                {
                    vs.ForEach(y => y.Active = Enable.GetValue(args, y.Active));
                }
            });
        });
    }
}

[Serializable]
public class EffectGivingModule : RandomExecutionModule
{
    public EffectFlagE EffectFlag { get; set; }
    // TODO: Maybe at some point we could make an enum of all StatusEffectBase names... see EffectType in ValueCollection.cs
    public string EffectType { get; set; }
    public SendType GivingTo { get; set; }
    public byte Intensity { get; set; }
    public float Duration { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            if (!args.TargetCalculated)
            {
                args.Targets = GetTargets(GivingTo, args);
            }
            foreach (Player player in args.Targets)
            {
                if (player.TryGetEffect(EffectType, out StatusEffectBase effect))
                {
                    if (EffectFlag.HasFlag(EffectFlagE.Disable))
                    {
                        player.DisableEffect(effect);
                    }
                    else if (EffectFlag.HasFlag(EffectFlagE.Enable))
                    {
                        byte intensity = Intensity;
                        if (EffectFlag.HasFlag(EffectFlagE.ModifyIntensity))
                        {
                            intensity += effect.Intensity;
                        }
                        player.EnableEffect(effect, intensity, Duration, addDuration: EffectFlag.HasFlag(EffectFlagE.ModifyDuration));
                    }
                }
                else
                {
                    Log.Warn($"Attempted to modify effect '{EffectType}' on player '{player.Nickname}' but the effect does not exist");
                }
            }
        });
    }
}

[Serializable]
public class FEffectGivingModule : FRandomExecutionModule
{
    public ScriptValue EffectFlag { get; set; }
    public ScriptValue EffectType { get; set; }
    public ScriptValue GivingTo { get; set; }
    public ScriptValue Intensity { get; set; }
    public ScriptValue Duration { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            EffectFlagE flag = EffectFlag.GetValue<EffectFlagE>(args, 0);
            string effectName = EffectType.GetValue(args, "");
            foreach (Player player in GivingTo.GetValue(args, new Player[] { }))
            {
                if (player.TryGetEffect(effectName, out StatusEffectBase effect))
                {
                    if (flag.HasFlag(EffectFlagE.Disable))
                    {
                        player.DisableEffect(effect);
                    }
                    else if (flag.HasFlag(EffectFlagE.Enable))
                    {
                        byte intensity = (byte)Intensity.GetValue(args, 0);
                        if (flag.HasFlag(EffectFlagE.ModifyIntensity))
                        {
                            intensity += effect.Intensity;
                        }
                        player.EnableEffect(effect, intensity, Duration.GetValue(args, 0), addDuration: flag.HasFlag(EffectFlagE.ModifyDuration));
                    }
                }
                else
                {
                    Log.Warn($"Attempted to modify effect '{effectName}' on player '{player.Nickname}' but the effect does not exist");
                }
            }
        });
    }
}

[Serializable]
public class ExplodeModule : RandomExecutionModule
{
    public bool FFon { get; set; }
    public bool EffectOnly { get; set; }
    public SVector3 LocalPosition { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        ReferenceHub.TryGetLocalHub(out ReferenceHub local);
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            if (EffectOnly)
            {
                ExplosionUtils.ServerSpawnEffect(args.Transform.TransformPoint(LocalPosition), ItemType.GrenadeHE);
            }
            else
            {
                ExplosionUtils.ServerExplode(args.Transform.TransformPoint(LocalPosition), FFon ? new Footprint(local) : new Footprint(args.Player.ReferenceHub), ExplosionType.Grenade);
            }
        });
    }
}

[Serializable]
public class FExplodeModule : FRandomExecutionModule
{
    public ScriptValue FFon { get; set; }
    public ScriptValue EffectOnly { get; set; }
    public ScriptValue LocalPosition { get; set; }

    public override void Execute(FunctionArgument args)
    {
        ReferenceHub.TryGetLocalHub(out ReferenceHub local);
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            if (EffectOnly.GetValue(args, true))
            {
                ExplosionUtils.ServerSpawnEffect(args.Transform.TransformPoint(LocalPosition.GetValue(args, Vector3.zero)), ItemType.GrenadeHE);
            }
            else
            {
                ExplosionUtils.ServerExplode(args.Transform.TransformPoint(LocalPosition.GetValue(args, Vector3.zero)), FFon.GetValue(args, false) ? new Footprint(local) : new Footprint(args.Player.ReferenceHub), ExplosionType.Grenade);
            }
        });
    }
}

[Serializable]
public class AudioModule : RandomExecutionModule
{
    public string AudioName { get; set; }
    [Header("0: Loop")]
    public int PlayCount { get; set; }
    public bool IsSpatial { get; set; }
    public float MaxDistance { get; set; }
    public float MinDistance { get; set; }
    public float Volume { get; set; }
    public SVector3 LocalPlayPosition { get; set; }
    public AudioPlayer AudioPlayer { get; set; }
    private bool loaded;

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            if (!loaded)
            {
                if (!Directory.Exists(AdvancedMERTools.Singleton.Config.AudioFolderPath))
                {
                    ServerConsole.AddLog("Cannot find Audio Folder Directory!", ConsoleColor.Red);
                    return;
                }
                if (!AudioClipStorage.AudioClips.ContainsKey(AudioName))
                {
                    AudioClipStorage.LoadClip(Path.Combine(AdvancedMERTools.Singleton.Config.AudioFolderPath, AudioName), AudioName);
                }

                loaded = true;
            }

            if (AudioPlayer == null)
            {
                AudioPlayer = AudioPlayer.Create($"AudioHandler-{args.Transform.GetHashCode()}-{GetHashCode()}");
                Speaker speaker = AudioPlayer.AddSpeaker("Primary", args.Transform.TransformPoint(LocalPlayPosition), Volume, IsSpatial, MinDistance, MaxDistance);
                AudioPlayer.transform.parent = speaker.transform.parent = args.Transform;
                AudioPlayer.transform.localPosition = speaker.transform.localPosition = LocalPlayPosition;
            }
            if (PlayCount == 0)
            {
                AudioPlayer.AddClip(AudioName, Volume, true, false);
            }
            for (int i = 0; i < PlayCount; i++)
            {
                AudioPlayer.AddClip(AudioName, Volume, false, false);
            }
        });
    }
}

[Serializable]
public class FAudioModule : FRandomExecutionModule
{
    public ScriptValue AudioName { get; set; }
    [Header("0: Loop")]
    public ScriptValue PlayCount { get; set; }
    public ScriptValue IsSpatial { get; set; }
    public ScriptValue MaxDistance { get; set; }
    public ScriptValue MinDistance { get; set; }
    public ScriptValue Volume { get; set; }
    public ScriptValue LocalPlayPosition { get; set; }
    public AudioPlayer AudioPlayer { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            string audioName = AudioName.GetValue<string>(args, null);
            if (audioName == null)
            {
                return;
            }
            if (!Directory.Exists(AdvancedMERTools.Singleton.Config.AudioFolderPath))
            {
                ServerConsole.AddLog("Cannot find Audio Folder Directory!", ConsoleColor.Red);
                return;
            }
            if (!AudioClipStorage.AudioClips.ContainsKey(audioName))
            {
                AudioClipStorage.LoadClip(Path.Combine(AdvancedMERTools.Singleton.Config.AudioFolderPath, audioName), audioName);
            }

            Vector3 vector = LocalPlayPosition.GetValue(args, Vector3.zero);
            float vol = Volume.GetValue(args, 1f);
            if (AudioPlayer == null)
            {
                AudioPlayer = AudioPlayer.Create($"AudioHandler-{args.Transform.GetHashCode()}-{GetHashCode()}");
                Speaker speaker = AudioPlayer.AddSpeaker($"Primary-{audioName}", args.Transform.TransformPoint(vector), vol, IsSpatial.GetValue(args, true), MinDistance.GetValue(args, 5f), MaxDistance.GetValue(args, 5f));
            }
            AudioPlayer.SpeakersByName[$"Primary-{audioName}"].transform.parent = args.Transform;
            AudioPlayer.SpeakersByName[$"Primary-{audioName}"].transform.localPosition = vector;
            int count = PlayCount.GetValue(args, 1);
            if (count == 0)
            {
                AudioPlayer.AddClip(audioName, vol, true, false);
            }
            for (int i = 0; i < count; i++)
            {
                AudioPlayer.AddClip(audioName, vol, false, false);
            }
        });
    }
}

[Serializable]
public class MessageModule : RandomExecutionModule
{
    public SendType SendType { get; set; }
    public string MessageContent { get; set; }
    public MessageTypeE MessageType { get; set; }
    public float Duration { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            string content = MessageContent;
            foreach (string v in args.Interpolations.Keys)
            {
                try
                {
                    content = content.Replace(v, args.Interpolations[v](args.InterpolationsList));
                }
                catch (Exception _) { }
            }
            try
            {
                content = ServerConsole.Singleton.NameFormatter.ProcessExpression(content);
            }
            catch (Exception e) { }
            if (!args.TargetCalculated)
            {
                args.Targets = GetTargets(SendType, args);
            }

            if (MessageType == MessageTypeE.Cassie)
            {
                Cassie.Message(content);
            }
            else
            {
                foreach (Player p in args.Targets)
                {
                    if (MessageType == MessageTypeE.BroadCast)
                    {
                        p.SendBroadcast(content, (ushort)Math.Round(Duration));
                    }
                    else
                    {
                        p.SendHint(content, Duration);
                    }
                }
            }
        });
    }
}

[Serializable]
public class FMessageModule : FRandomExecutionModule
{
    public ScriptValue SendType { get; set; }
    public ScriptValue MessageContent { get; set; }
    public ScriptValue MessageType { get; set; }
    public ScriptValue Duration { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            string content = MessageContent.GetValue(args, "");
            MessageTypeE type = MessageType.GetValue(args, MessageTypeE.BroadCast);
            if (type == MessageTypeE.Cassie)
            {
                Cassie.Message(content);
            }
            else
            {
                foreach (Player p in SendType.GetValue(args, new Player[] { }))
                {
                    if (type == MessageTypeE.BroadCast)
                    {
                        p.SendBroadcast(content, (ushort)Math.Round(Duration.GetValue(args, 0f)));
                    }
                    else
                    {
                        p.SendHint(content, Duration.GetValue(args, 0f));
                    }
                }
            }
        });
    }
}

[Serializable]
public class AnimationDTO : RandomExecutionModule
{
    public Animator Animator { get; set; }
    [HideInInspector]
    public string AnimatorAdress { get; set; }
    public string AnimationName { get; set; }
    public AnimationTypeE AnimationType { get; set; }
    public ParameterTypeE ParameterType { get; set; }
    public string ParameterName { get; set; }
    [Header("If parameter type is bool or trigger, input 0 for false, and input 1 for true.")]
    public string ParameterValue { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            if (Animator == null)
            {
                if (!AMERTEventHandlers.FindObjectWithPath(args.Schematic.GetComponentInParent<SchematicObject>().transform, AnimatorAdress).TryGetComponent(out Animator animator))
                {
                    ServerConsole.AddLog("Cannot find appopriate animator!");
                    return;
                }
                Animator = animator;
            }
            if (AnimationType == AnimationTypeE.Start)
            {
                Animator.Play(AnimationName);
                Animator.speed = 1f;
            }
            else if (AnimationType == AnimationTypeE.Stop)
            {
                Animator.speed = 0f;
            }
            else
            {
                switch (ParameterType)
                {
                    case ParameterTypeE.Bool:
                        Animator.SetBool(ParameterName, ParameterValue == "1");
                        break;
                    case ParameterTypeE.Float:
                        Animator.SetFloat(ParameterName, float.Parse(ParameterValue));
                        break;
                    case ParameterTypeE.Integer:
                        Animator.SetInteger(ParameterName, int.Parse(ParameterValue));
                        break;
                    case ParameterTypeE.Trigger:
                        Animator.SetTrigger(ParameterName);
                        break;
                }
            }
        });
    }
}

[Serializable]
public class FAnimationDTO : FRandomExecutionModule
{
    public Animator Animator { get; set; }
    [HideInInspector]
    public string AnimatorAdress { get; set; }
    public ScriptValue AnimationName { get; set; }
    public ScriptValue AnimationType { get; set; }
    public ScriptValue ParameterType { get; set; }
    public ScriptValue ParameterName { get; set; }
    [Header("If parameter type is bool or trigger, input 0 for false, and input 1 for true.")]
    public ScriptValue ParameterValue { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            if (Animator == null)
            {
                if (!AMERTEventHandlers.FindObjectWithPath(args.Schematic.GetComponentInParent<SchematicObject>().transform, AnimatorAdress).TryGetComponent(out Animator animator))
                {
                    ServerConsole.AddLog("Cannot find appopriate animator!");
                    return;
                }
                Animator = animator;
            }
            AnimationTypeE type = AnimationType.GetValue(args, AnimationTypeE.Start);
            if (type == AnimationTypeE.Start)
            {
                Animator.Play(AnimationName.GetValue(args, ""));
                Animator.speed = 1f;
            }
            else if (type == AnimationTypeE.Stop)
            {
                Animator.speed = 0f;
            }
            else
            {
                string pm = ParameterName.GetValue<string>(args, null);
                if (pm == null)
                {
                    return;
                }

                switch (ParameterType.GetValue(args, ParameterTypeE.Integer))
                {
                    case ParameterTypeE.Bool:
                        Animator.SetBool(pm, ParameterValue.GetValue(args, "1") == "1");
                        break;
                    case ParameterTypeE.Float:
                        Animator.SetFloat(pm, ParameterValue.GetValue(args, 0f));
                        break;
                    case ParameterTypeE.Integer:
                        Animator.SetInteger(pm, ParameterValue.GetValue(args, 0));
                        break;
                    case ParameterTypeE.Trigger:
                        Animator.SetTrigger(pm);
                        break;
                }
            }
        });
    }
}

[Serializable]
public class DropItem : RandomExecutionModule
{
    public ItemType ItemType { get; set; }
    public uint CustomItemId { get; set; }
    public int Count { get; set; }
    public SVector3 DropLocalPosition { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            // TODO: Not sure how to do CustomItems in LabAPI yet, for now just use Item
            Vector3 position = args.Transform.TransformPoint(DropLocalPosition);
            if (CustomItemId != 0 && Item.TryGet((ushort)CustomItemId, out Item customItem))
            {
                for (int i = 0; i < Count; i++)
                {
                    Pickup customPickup = Pickup.Create(ItemType, position);
                    //OnCustomItemCreated(customPickup.Serial);
                    customPickup.Spawn();
                }
            }
            else
            {
                if (!InventoryItemLoader.AvailableItems.TryGetValue(ItemType, out ItemBase itemBase) || itemBase.PickupDropModel == null)
                {
                    return;
                }
                for (int i = 0; i < Count; i++)
                {
                    ItemPickupBase itemPickupBase = UnityEngine.Object.Instantiate<ItemPickupBase>(itemBase.PickupDropModel, position, args.Transform.rotation);
                    itemPickupBase.NetworkInfo = new PickupSyncInfo(ItemType, itemBase.Weight, 0, false);
                    NetworkServer.Spawn(itemPickupBase.gameObject);
                }
            }
        });
    }
}

[Serializable]
public class FDropItem : FRandomExecutionModule
{
    public ScriptValue ItemType { get; set; }
    public ScriptValue CustomItemId { get; set; }
    public ScriptValue Count { get; set; }
    public ScriptValue DropLocalPosition { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            Vector3 position = args.Transform.TransformPoint(DropLocalPosition.GetValue(args, Vector3.zero));
            int u = CustomItemId.GetValue(args, 0);
            int c = Count.GetValue(args, 1);
            if (u != 0 && Item.TryGet((ushort)u, out Item customItem))
            {
                for (int i = 0; i < c; i++)
                {
                    Pickup customPickup = Pickup.Create(ItemType.GetValue(args, global::ItemType.None), position);
                    //OnCustomItemCreated(customPickup.Serial);
                    customPickup.Spawn();
                }
            }
            else
            {
                ItemType value = ItemType.GetValue(args, global::ItemType.KeycardJanitor);
                if (!InventoryItemLoader.AvailableItems.TryGetValue(value, out ItemBase itemBase) || itemBase.PickupDropModel == null)
                {
                    return;
                }
                for (int i = 0; i < c; i++)
                {
                    ItemPickupBase itemPickupBase = UnityEngine.Object.Instantiate<ItemPickupBase>(itemBase.PickupDropModel, position, args.Transform.rotation);
                    itemPickupBase.NetworkInfo = new PickupSyncInfo(value, itemBase.Weight, 0, false);
                    NetworkServer.Spawn(itemPickupBase.gameObject);
                }
            }
        });
    }
}

[Serializable]
public class CFEModule : RandomExecutionModule
{
    public string FunctionName { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            if (AdvancedMERTools.Singleton.FunctionExecutors[args.Schematic].TryGetValue(FunctionName, out FunctionExecutor function))
            {
                function.Data.Execute(new FunctionArgument { Player = args.Player });
            }
        });
    }
}

[Serializable]
public class FCFEModule : FRandomExecutionModule
{
    public ScriptValue FunctionName { get; set; }
    public List<ScriptValue> FunctionArguments { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            if (AdvancedMERTools.Singleton.FunctionExecutors[args.Schematic].TryGetValue(FunctionName.GetValue(args, ""), out FunctionExecutor function))
            {
                function.Data.Execute(new FunctionArgument { Arguments = FunctionArguments.Select(x => x.GetValue(args)).ToList(), Player = args.Player, Schematic = args.Schematic });
            }
        });
    }
}

[Serializable]
public class Commanding : RandomExecutionModule
{
    public string CommandContext { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            string content = CommandContext;
            foreach (string v in args.Interpolations.Keys)
            {
                try
                {
                    content = content.Replace(v, args.Interpolations[v](args.InterpolationsList));
                }
                catch (Exception) { }
            }
            content = ServerConsole.Singleton.NameFormatter.ProcessExpression(content);
            AdvancedMERTools.ExecuteCommand(content);
        });
    }
}

[Serializable]
public class FCommanding : FRandomExecutionModule
{
    public ScriptValue CommandContext { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            AdvancedMERTools.ExecuteCommand(CommandContext.GetValue(args, ""));
        });
    }
}

////////////////////////////////////////////////
// Misc. Utility Classes
////////////////////////////////////////////////

[Serializable]
public class SVector3
{
    public float x;
    public float y;
    public float z;

    public static implicit operator Vector3(SVector3 sVector) => new(sVector.x, sVector.y, sVector.z);
}

[Serializable]
public class GateSerializable
{
    public DoorPermissionFlags DoorPermissions { get; set; }
    public bool RequireAllPermission { get; set; }
    public bool IsLocked { get; set; }
    public bool IsOpened { get; set; }
}

[Serializable]
public class WhitelistWeapon
{
    public ItemType ItemType { get; set; }
    public uint CustomItemId { get; set; }
}

[Serializable]
public class FWhitelistWeapon : Value
{
    public ScriptValue ItemType { get; set; }
    public ScriptValue CustomItemId { get; set; }

    public override void OnValidate()
    {
        ItemType.OnValidate();
        CustomItemId.OnValidate();
    }
}
