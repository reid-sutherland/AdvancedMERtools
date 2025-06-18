using AdminToys;
using InventorySystem.Items.Armor;
using InventorySystem.Items.ThrowableProjectiles;
using LabApi.Features.Wrappers;
using LabApi.Events.Arguments.ServerEvents;
using Mirror;
using PlayerStatsSystem;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedMERTools;

public class Healther : NetworkBehaviour, IDestructible
{
    public List<HealthObject> Parents { get; set; } = new();

    public uint NetworkId
    {
        get
        {
            return base.netId;
        }
    }

    public Vector3 CenterOfMass
    {
        get
        {
            return Vector3.zero;
        }
    }

    public bool Damage(float damage, DamageHandlerBase handler, Vector3 exactHitPos)
    {
        bool hit = false;
        Parents.ForEach(x => hit |= x.Damage(damage, handler, exactHitPos));
        return hit;
    }
}

public class HealthObject : AMERTInteractable, IDestructible
{
    public new HODTO Base { get; set; }

    public bool AnimationEnded { get; set; } = false;

    public float Health { get; set; }

    public bool IsAlive { get; set; } = true;

    protected static Dictionary<ExplosionType, ItemType> ExplosionDic { get; } = new Dictionary<ExplosionType, ItemType>
    {
        { ExplosionType.Grenade, ItemType.GrenadeHE },
        { ExplosionType.Disruptor, ItemType.ParticleDisruptor },
        { ExplosionType.Jailbird, ItemType.Jailbird },
        { ExplosionType.Cola, ItemType.SCP207 },
        { ExplosionType.PinkCandy, ItemType.SCP330 },
        { ExplosionType.SCP018, ItemType.SCP018 },
    };

    protected static Dictionary<string, Func<object[], string>> Formatter { get; } = new Dictionary<string, Func<object[], string>>
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
                Vector3 pos = (vs[1] as Transform).position;
                return $"{pos.x} {pos.y} {pos.z}";
            }
        },
        { "{o_room}", vs => Room.GetRoomAtPosition((vs[1] as Transform).position).Name.ToString() },
        { "{o_zone}", vs => Room.GetRoomAtPosition((vs[1] as Transform).position).Zone.ToString() },
        { "{damage}", vs => (vs[2] as float?).ToString() },
    };

    public uint NetworkId
    {
        get
        {
            return base.netId;
        }
    }

    public Vector3 CenterOfMass
    {
        get
        {
            return Vector3.zero;
        }
    }

    protected virtual void Start()
    {
        this.Base = base.Base as HODTO;
        Health = Base.Health;
        Register();
    }

    protected void Register()
    {
        Log.Debug($"Registering HealthObject: {gameObject.name} ({OSchematic.Name})");
        this.transform.GetComponentsInChildren<AdminToys.PrimitiveObjectToy>().ForEach(x =>
        {
            if (x.gameObject.TryGetComponent<Healther>(out Healther healther))
            {
                healther.Parents.Add(this);
            }
            else
            {
                healther = x.gameObject.AddComponent<Healther>();
                healther.Parents.Add(this);
            }
        });
        AdvancedMERTools.Singleton.HealthObjects.Add(this);
    }

    protected void Update()
    {
        if (Health <= 0 && this.Base.DeadType.HasFlag(DeadType.DynamicDisappearing) && !AnimationEnded)
        {
            this.transform.localScale = Vector3.Lerp(this.transform.localScale, Vector3.zero, Time.deltaTime);
            if (this.transform.localScale.magnitude <= 0.1f)
            {
                Destroy();
            }
        }
    }

    protected virtual void Destroy()
    {
        AnimationEnded = true;
        if (Base.DoNotDestroyAfterDeath)
        {
            return;
        }
        Destroy(this.gameObject);
    }

    public virtual bool Damage(float damage, DamageHandlerBase handler, Vector3 pos)
    {
        if (!IsAlive || !Active)
        {
            return false;
        }

        Player attacker = null;
        AttackerDamageHandler damageHandler = handler as AttackerDamageHandler;
        if (damageHandler != null)
        {
            attacker = Player.Get(damageHandler.Attacker.PlayerId);
            FirearmDamageHandler firearm = handler as FirearmDamageHandler;
            ExplosionDamageHandler explosion = handler as ExplosionDamageHandler;
            if (firearm != null)
            {
                if (Base.whitelistWeapons.Count == 0 || Base.whitelistWeapons.Any(x =>
                {
                    if (Item.TryGet(attacker.CurrentItem.Base, out Item item))
                    {
                        return item.Base.ItemId.SerialNumber == x.CustomItemId;
                    }
                    else
                    {
                        return attacker.CurrentItem.Type == x.ItemType;
                    }
                }))
                {
                    FieldInfo info = typeof(FirearmDamageHandler).GetField("_penetration", BindingFlags.NonPublic | BindingFlags.Instance);
                    damage = BodyArmorUtils.ProcessDamage(Base.ArmorEfficient, damage, Mathf.RoundToInt((float)info.GetValue(firearm) * 100f));
                }
                else
                {
                    return false;
                }
            }
            if (explosion != null)
            {
                if (Base.whitelistWeapons.Count != 0 && !Base.whitelistWeapons.Any(x =>
                {
                    if (ExplosionDic.TryGetValue(explosion.ExplosionType, out ItemType item))
                    {
                        return item == x.ItemType;
                    }
                    return false;
                }))
                {
                    return false;
                }
            }
        }
        CheckDead(attacker, damage);
        return true;
    }

    public virtual void OnProjectileExploded(ProjectileExplodedEventArgs ev)
    {
        if (!IsAlive || !Active)
        {
            return;
        }
        if (Base.whitelistWeapons.Count != 0 && Base.whitelistWeapons.Find(x => x.CustomItemId == 0 && x.ItemType == ItemType.GrenadeHE) == null)
        {
            return;
        }

        if (ev.TimedGrenade.Type == ItemType.GrenadeHE)
        {
            float distance = Vector3.Distance(this.transform.position, ev.Position);
            FieldInfo info = typeof(ExplosionGrenade).GetField("_playerDamageOverDistance", BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.Instance);
            float damage = ((AnimationCurve)info.GetValue(ev.TimedGrenade.Base as ExplosionGrenade)).Evaluate(distance);
            damage = BodyArmorUtils.ProcessDamage(Base.ArmorEfficient, damage, 50);
            CheckDead(ev.Player, damage);
        }
    }

    public virtual void CheckDead(Player player, float damage)
    {
        Health -= damage;
        Hitmarker.SendHitmarkerDirectly(player.ReferenceHub, damage / 10f);
        if (Health <= 0)
        {
            HODTO clone = new()
            {
                Health = this.Base.Health,
                ArmorEfficient = this.Base.ArmorEfficient,
                DeadType = this.Base.DeadType,
                ObjectId = this.Base.ObjectId,
            };
            IsAlive = false;
            ModuleGeneralArguments args = new()
            {
                Interpolations = Formatter,
                InterpolationsList = new object[] { player, transform },
                Player = player,
                Schematic = OSchematic,
                Transform = transform,
                TargetCalculated = false,
            };
            AMERTEvents.OnHealthObjectDead(new HealthObjectDeadEventArgs(clone, player));

            MEC.Timing.CallDelayed(Base.DeadActionDelay, () =>
            {
                var deadTypeExecutors = new Dictionary<DeadType, Action>
                {
                    { DeadType.Disappear, () => Destroy(this.gameObject, 0.1f) },
                    {
                        DeadType.GetRigidbody, () =>
                        {
                            MakeNonStatic(gameObject);
                            this.gameObject.AddComponent<Rigidbody>();
                        }
                    },
                    { DeadType.DynamicDisappearing, () => MakeNonStatic(gameObject) },
                    { DeadType.Explode, () => ExplodeModule.Execute(Base.ExplodeModules, args) },
                    {
                        DeadType.ResetHP, () =>
                        {
                            Health = Base.ResetHPTo == 0 ? Base.Health : Base.ResetHPTo;
                            IsAlive = true;
                        }
                    },
                    { DeadType.PlayAnimation, () => AnimationDTO.Execute(Base.AnimationModules, args) },
                    { DeadType.Warhead, () => AlphaWarhead(Base.warheadActionType) },
                    { DeadType.SendMessage, () => MessageModule.Execute(Base.MessageModules, args) },
                    { DeadType.DropItems, () => DropItem.Execute(Base.dropItems, args) },
                    { DeadType.SendCommand, () => Commanding.Execute(Base.commandings, args) },
                    { DeadType.GiveEffect, () => EffectGivingModule.Execute(Base.effectGivingModules, args) },
                    { DeadType.PlayAudio, () => AudioModule.Execute(Base.AudioModules, args) },
                    { DeadType.CallGroovieNoise, () => CGNModule.Execute(Base.GroovieNoiseToCall, args) },
                    { DeadType.CallFunction, () => CFEModule.Execute(Base.FunctionToCall, args) },
                };
                foreach (DeadType type in Enum.GetValues(typeof(DeadType)))
                {
                    if (Base.DeadType.HasFlag(type) && deadTypeExecutors.TryGetValue(type, out var execute))
                    {
                        Log.Debug($"- HO: executing DeadAction: {type}");
                        execute();
                    }
                }
            });
        }
    }

    public void MakeNonStatic(GameObject game)
    {
        foreach (AdminToyBase adminToyBase in game.transform.GetComponentsInChildren<AdminToyBase>())
        {
            adminToyBase.enabled = true;
        }
    }
}

public class FHealthObject : HealthObject
{
    public new FHODTO Base { get; set; }

    protected override void Start()
    {
        this.Base = ((AMERTInteractable)this).Base as FHODTO;
        Health = Base.Health.GetValue(new FunctionArgument(this), 100f);
        Register();
    }

    protected override void Destroy()
    {
        AnimationEnded = true;
        if (Base.DoNotDestroyAfterDeath.GetValue(new FunctionArgument(this), false))
        {
            return;
        }
        Destroy(this.gameObject);
    }

    public override bool Damage(float damage, DamageHandlerBase handler, Vector3 pos)
    {
        if (!IsAlive || !Active)
        {
            return false;
        }

        Player attacker = null;
        AttackerDamageHandler damageHandler = handler as AttackerDamageHandler;
        if (damageHandler != null)
        {
            attacker = Player.Get(damageHandler.Attacker.PlayerId);
            FunctionArgument args = new FunctionArgument(this, attacker);
            FirearmDamageHandler firearm = handler as FirearmDamageHandler;
            ExplosionDamageHandler explosion = handler as ExplosionDamageHandler;
            if (firearm != null)
            {
                if (Base.whitelistWeapons.Count == 0 || Base.whitelistWeapons.Any(x =>
                {
                    if (Item.TryGet(attacker.CurrentItem.Base, out Item item))
                    {
                        return item.Base.ItemId.SerialNumber == x.CustomItemId.GetValue(args, 0);
                    }
                    else
                    {
                        return attacker.CurrentItem.Type == x.ItemType.GetValue(args, ItemType.None);
                    }
                }))
                {
                    FieldInfo info = typeof(FirearmDamageHandler).GetField("_penetration", BindingFlags.NonPublic | BindingFlags.Instance);
                    damage = BodyArmorUtils.ProcessDamage(Base.ArmorEfficient.GetValue(args, 0), damage, Mathf.RoundToInt((float)info.GetValue(firearm) * 100f));
                }
                else
                {
                    return false;
                }
            }
            if (explosion != null)
            {
                if (Base.whitelistWeapons.Count != 0 && !Base.whitelistWeapons.Any(x =>
                {
                    if (ExplosionDic.TryGetValue(explosion.ExplosionType, out ItemType item))
                    {
                        return item == x.ItemType.GetValue(args, ItemType.None);
                    }
                    return false;
                }))
                {
                    return false;
                }
            }
        }
        CheckDead(attacker, damage);
        return true;
    }

    public override void OnProjectileExploded(ProjectileExplodedEventArgs ev)
    {
        if (!IsAlive || !Active)
        {
            return;
        }
        FunctionArgument args = new(this, ev.Player);
        if (Base.whitelistWeapons.Count != 0 && Base.whitelistWeapons.Find(x => x.CustomItemId.GetValue(args, 0) == 0 && x.ItemType.GetValue(args, ItemType.None) == ItemType.GrenadeHE) == null)
        {
            return;
        }

        if (ev.TimedGrenade.Type == ItemType.GrenadeHE)
        {
            float distance = Vector3.Distance(this.transform.position, ev.Position);
            FieldInfo info = typeof(ExplosionGrenade).GetField("_playerDamageOverDistance", BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.Instance);
            float damage = ((AnimationCurve)info.GetValue(ev.TimedGrenade.Base as ExplosionGrenade)).Evaluate(distance);
            damage = BodyArmorUtils.ProcessDamage(Base.ArmorEfficient.GetValue(args, 0), damage, 50);
            CheckDead(ev.Player, damage);
        }
    }

    public override void CheckDead(Player player, float damage)
    {
        Health -= damage;
        Hitmarker.SendHitmarkerDirectly(player.ReferenceHub, damage / 10f);
        if (Health <= 0)
        {
            IsAlive = false;
            FunctionArgument args = new FunctionArgument(this, player);
            MEC.Timing.CallDelayed(Base.DeadActionDelay.GetValue(args, 0f), () =>
            {
                var deadTypeExecutors = new Dictionary<DeadType, Action>
                {
                    { DeadType.Disappear, () => Destroy(this.gameObject, 0.1f) },
                    {
                        DeadType.GetRigidbody, () =>
                        {
                            MakeNonStatic(gameObject);
                            this.gameObject.AddComponent<Rigidbody>();
                        }
                    },
                    { DeadType.DynamicDisappearing, () => MakeNonStatic(gameObject) },
                    { DeadType.Explode, () => FExplodeModule.Execute(Base.ExplodeModules, args) },
                    {
                        DeadType.ResetHP, () =>
                        {
                            float rHealth = Base.ResetHPTo.GetValue(args, 0f);
                            Health = rHealth == 0 ? Base.Health.GetValue(args, 100f) : rHealth;
                            IsAlive = true;
                        }
                    },
                    { DeadType.PlayAnimation, () => FAnimationDTO.Execute(Base.AnimationModules, args) },
                    { DeadType.Warhead, () => AlphaWarhead(Base.warheadActionType.GetValue<WarheadActionType>(args, 0)) },
                    { DeadType.SendMessage, () => FMessageModule.Execute(Base.MessageModules, args) },
                    { DeadType.DropItems, () => FDropItem.Execute(Base.dropItems, args) },
                    { DeadType.SendCommand, () => FCommanding.Execute(Base.commandings, args) },
                    { DeadType.GiveEffect, () => FEffectGivingModule.Execute(Base.effectGivingModules, args) },
                    { DeadType.PlayAudio, () => FAudioModule.Execute(Base.AudioModules, args) },
                    { DeadType.CallGroovieNoise, () => FCGNModule.Execute(Base.GroovieNoiseToCall, args) },
                    { DeadType.CallFunction, () => FCFEModule.Execute(Base.FunctionToCall, args) },
                };
                foreach (DeadType type in Enum.GetValues(typeof(DeadType)))
                {
                    if (Base.DeadType.HasFlag(type) && deadTypeExecutors.TryGetValue(type, out var execute))
                    {
                        execute();
                    }
                }
            });
        }
    }
}
