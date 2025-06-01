//using Exiled.API.Features;
//using Exiled.API.Features.Pickups;
//using Exiled.API.Features.Items;
//using Exiled.CustomItems;
using InventorySystem.Items.ThrowableProjectiles;
using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedMERTools;

public class CustomCollider : AMERTInteractable
{
    public new CCDTO Base { get; private set; }

    public MeshCollider MeshCollider { get; private set; }

    public Transform OriginalTransform { get; private set; }

    protected virtual void Start()
    {
        this.Base = base.Base as CCDTO;
        Register();
    }

    protected void Register()
    {
        AdvancedMERTools.Singleton.CustomColliders.Add(this);
        CustomCollider[] customColliders = gameObject.GetComponents<CustomCollider>();
        if (customColliders.Length > 1 && customColliders[0] != this)
        {
            MEC.Timing.CallDelayed(0.1f, () =>
            {
                MeshCollider = customColliders[0].MeshCollider;
            });
            return;
        }
        Vector3[] vs = new Vector3[] { transform.position, transform.eulerAngles };
        transform.position = Vector3.zero;
        transform.eulerAngles = Vector3.zero;

        MeshFilter[] meshFilters = transform.GetComponentsInChildren<MeshFilter>();
        MeshCollider = gameObject.AddComponent<MeshCollider>();
        CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            combineInstances[i].mesh = meshFilters[i].sharedMesh;
            combineInstances[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combineInstances);
        MeshCollider.sharedMesh = mesh;
        MeshCollider.convex = true;
        MeshCollider.isTrigger = true;
        Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;

        transform.GetComponentsInChildren<AdminToys.PrimitiveObjectToy>().ForEach(x =>
        {
            PrimitiveObjectToy.Get(x).Flags = AdminToys.PrimitiveFlags.None;
            //x.gameObject.SetActive(false);
            //NetworkServer.Destroy(x.gameObject);
        });
        //meshCollider.contactOffset = Base.ContactOffSet;

        transform.position = vs[0];
        transform.eulerAngles = vs[1];
    }

    protected void OnTriggerEnter(Collider collider)
    {
        if (Base.CollisionType.HasFlag(CollisionType.OnEnter))
            RunProcess(collider);
    }

    protected void OnTriggerExit(Collider collider)
    {
        if (Base.CollisionType.HasFlag(CollisionType.OnExit))
            RunProcess(collider);
    }

    protected void OnTriggerStay(Collider collider)
    {
        if (Base.CollisionType.HasFlag(CollisionType.OnStay))
            RunProcess(collider);
    }

    public virtual void RunProcess(Collider collider)
    {
        if (!Active)
        {
            return;
        }

        bool flag = false;
        Player target = null;
        Pickup pickup = Pickup.Get(collider.gameObject);
        if (Base.DetectType.HasFlag(DetectType.Pickup) && pickup != null)
        {
            target = pickup.PreviousOwner;
            flag = true;
        }
        if (Base.DetectType.HasFlag(DetectType.Player) && Player.TryGet(collider, out target))
        {
            flag = target.Role.Base.ActiveTime > 0.25f;
        }
        ThrownProjectile projectile = collider.GetComponentInParent<ThrownProjectile>();
        if (Base.DetectType.HasFlag(DetectType.Projectile) && projectile != null)
        {
            target = Player.Get(projectile.PreviousOwner);
            flag = true;
        }
        if (!flag)
        {
            return;
        }

        ModuleGeneralArguments args = new ModuleGeneralArguments { Player = target, TargetCalculated = false, Transform = this.transform, Schematic = OSchematic, Interpolations = Formatter, InterpolationsList = new object[] { target, gameObject } };
        var colliderActionExecutors = new Dictionary<ColliderActionType, Action>
        {
            {
                ColliderActionType.ModifyHealth, () =>
                {
                    if (target != null)
                    {
                        if (Base.ModifyHealthAmount > 0)
                        {
                            target.Heal(Base.ModifyHealthAmount);
                        }
                        else
                        {
                            target.Hurt(-1 * Base.ModifyHealthAmount);
                        }
                    }
                }
            },
            { ColliderActionType.Explode, () => ExplodeModule.Execute(Base.ExplodeModules, args) },
            { ColliderActionType.PlayAnimation, () => AnimationDTO.Execute(Base.AnimationModules, args) },
            { ColliderActionType.Warhead, () => AlphaWarhead(Base.warheadActionType) },
            { ColliderActionType.SendMessage, () => MessageModule.Execute(Base.MessageModules, args) },
            { ColliderActionType.SendCommand, () => Commanding.Execute(Base.commandings, args) },
            { ColliderActionType.GiveEffect, () => EffectGivingModule.Execute(Base.effectGivingModules, args) },
            { ColliderActionType.PlayAudio, () => AudioModule.Execute(Base.AudioModules, args) },
            { ColliderActionType.CallGroovieNoise, () => CGNModule.Execute(Base.GroovieNoiseToCall, args) },
            { ColliderActionType.CallFunction, () => CFEModule.Execute(Base.FunctionToCall, args) },
        };
        foreach (ColliderActionType type in Enum.GetValues(typeof(ColliderActionType)))
        {
            if (Base.ColliderActionType.HasFlag(type) && colliderActionExecutors.TryGetValue(type, out var execute))
            {
                execute();
            }
        }
    }

    static readonly Dictionary<string, Func<object[], string>> Formatter = new Dictionary<string, Func<object[], string>>
    {
        { "{p_i}", vs => (vs[0] as Player).Id.ToString() },
        { "{p_name}", vs => (vs[0] as Player).Nickname.ToString() },
        { "{p_pos}", vs => { Vector3 pos = (vs[0] as Player).Transform.position; return $"{pos.x} {pos.y} {pos.z}"; } },
        { "{p_room}", vs => (vs[0] as Player).CurrentRoom.RoomName.ToString() },
        { "{p_zone}", vs => (vs[0] as Player).Zone.ToString() },
        { "{p_role}", vs => (vs[0] as Player).Role.Type.ToString() },
        { "{p_item}", vs => (vs[0] as Player).CurrentItem.Type.ToString() },
        { "{o_pos}", vs => { Vector3 pos = (vs[1] as GameObject).transform.position; return $"{pos.x} {pos.y} {pos.z}"; } },
        // TODO: Idk what this is for but RoomIdUtils is supposed to be in "Assembly-CSharp" so just skip for now
        //{ "{o_room}", vs => RoomIdUtils.RoomAtPosition((vs[1] as GameObject).transform.position).Name.ToString() },
        //{ "{o_zone}", vs => RoomIdUtils.RoomAtPosition((vs[1] as GameObject).transform.position).Zone.ToString() },
    };

    protected void OnDestroy()
    {
        AdvancedMERTools.Singleton.CustomColliders.Remove(this);
    }
}

public class FCustomCollider : CustomCollider
{
    public new FCCDTO Base { get; private set; }

    protected override void Start()
    {
        Base = ((AMERTInteractable)this).Base as FCCDTO;
        Register();
    }

    public override void RunProcess(Collider collider)
    {
        if (!Active)
        {
            return;
        }

        bool flag = false;
        Player target = null;
        Pickup pickup = Pickup.Get(collider.gameObject);
        CollisionType collision = Base.DetectType.GetValue<CollisionType>(new FunctionArgument(this), 0);
        if (collision.HasFlag(DetectType.Pickup) && pickup != null)
        {
            target = pickup.PreviousOwner;
            flag = true;
        }
        if (collision.HasFlag(DetectType.Player) && Player.TryGet(collider, out target))
        {
            flag = target.Role.Base.ActiveTime > 0.25f;
        }
        ThrownProjectile projectile = collider.GetComponentInParent<ThrownProjectile>();
        if (collision.HasFlag(DetectType.Projectile) && projectile != null)
        {
            target = Player.Get(projectile.PreviousOwner);
            flag = true;
        }
        if (!flag)
        {
            return;
        }

        FunctionArgument args = new FunctionArgument(this, target);
        var colliderActionExecutors = new Dictionary<ColliderActionType, Action>
        {
            {
                ColliderActionType.ModifyHealth, () =>
                {
                    if (target != null)
                    {
                        float amount = Base.ModifyHealthAmount.GetValue(args, 0f);
                        if (amount > 0)
                        {
                            target.Heal(amount);
                        }
                        else
                        {
                            target.Hurt(-amount);
                        }
                    }
                }
            },
            { ColliderActionType.Explode, () => FExplodeModule.Execute(Base.ExplodeModules, args) },
            { ColliderActionType.PlayAnimation, () => FAnimationDTO.Execute(Base.AnimationModules, args) },
            { ColliderActionType.Warhead, () => AlphaWarhead(Base.warheadActionType.GetValue<WarheadActionType>(args, 0)) },
            { ColliderActionType.SendMessage, () => FMessageModule.Execute(Base.MessageModules, args) },
            { ColliderActionType.SendCommand, () => FCommanding.Execute(Base.commandings, args) },
            { ColliderActionType.GiveEffect, () => FEffectGivingModule.Execute(Base.effectGivingModules, args) },
            { ColliderActionType.PlayAudio, () => FAudioModule.Execute(Base.AudioModules, args) },
            { ColliderActionType.CallGroovieNoise, () => FCGNModule.Execute(Base.GroovieNoiseToCall, args) },
            { ColliderActionType.CallFunction, () => FCFEModule.Execute(Base.FunctionToCall, args) },
        };
        foreach (ColliderActionType type in Enum.GetValues(typeof(ColliderActionType)))
        {
            if (Base.ColliderActionType.HasFlag(type) && colliderActionExecutors.TryGetValue(type, out var execute))
            {
                execute();
            }
        }
    }
}
