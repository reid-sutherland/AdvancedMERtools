using LabApi.Features.Wrappers;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AdvancedMERTools;

public class FunctionExecutor : AMERTInteractable
{
    public FEDTO Data { get; set; }

    public void Start()
    {
        if (AdvancedMERTools.Singleton.FunctionExecutors[OSchematic].ContainsKey(Data.FunctionName))
        {
            Log.Error($"WARNING! There's another function named: {Data.FunctionName}. Overlapped Function Name is not allowed!");
            Destroy(this);
            return;
        }
        Data.OSchematic = OSchematic;
        AdvancedMERTools.Singleton.FunctionExecutors[OSchematic].Add(Data.FunctionName, this);
    }
}

public class FunctionArgument
{
    public List<object> Arguments { get; set; } = new();
    public List<(object, int)> Levels { get; set; } = new();
    public Dictionary<string, object> FunctionVariables { get; set; } = new();
    public FEDTO Function { get; set; }
    public Player Player { get; set; }
    public SchematicObject Schematic { get; set; }
    public Transform Transform { get; set; }

    public FunctionArgument()
    {
    }

    public FunctionArgument(AMERTInteractable interactable, Player player = null)
    {
        Schematic = interactable.OSchematic;
        Transform = interactable.transform;
        Player = player;
    }
}

public class FunctionReturn
{
    public object Value { get; set; }
    public FunctionResult Result { get; set; }

    public static implicit operator FunctionReturn(Task<FunctionReturn> t) => t.Result;
}

public enum FunctionResult
{
    Default,
    FunctionCheck,
    Return,
    Continue,
    Break,
    Wait,
}

[Serializable]
public class FEDTO : ActionsFunctioner
{
    public string FunctionName { get; set; }
    public string[] ArgumentsName { get; set; }
    public SchematicObject OSchematic { get; set; }
    public List<ScriptValue> Conditions { get; set; }
    public Dictionary<string, object> ScriptVariables { get; set; } = new();

    public override FunctionReturn Execute(FunctionArgument args)
    {
        args.Function = this;
        if (!ConditionCheck(args, Conditions))
        {
            return new FunctionReturn();
        }
        return ExecuteActions(args, FunctionResult.Return);
    }
}

[Serializable]
public class ScriptAction
{
    public FunctionType ActionType { get; set; }
    public Function Function { get; set; }

    public FunctionReturn Execute(FunctionArgument args)
    {
        if (Function == null)
        {
            return null;
        }
        if (!EnumToFunc.TryGetValue(ActionType, out _))
        {
            return ActionType switch
            {
                FunctionType.Break => new FunctionReturn { Result = FunctionResult.Break },
                FunctionType.Continue => new FunctionReturn { Result = FunctionResult.Continue },
                _ => new FunctionReturn { Result = FunctionResult.FunctionCheck },
            };
        }
        return Function.Execute(args);
    }

    public void OnValidate()
    {
    }

    public static readonly Dictionary<FunctionType, Type> EnumToFunc = new()
    {
        { FunctionType.If, typeof(If) },
        { FunctionType.ElseIf, typeof(ElseIf) },
        { FunctionType.Else, typeof(Else) },
        { FunctionType.While, typeof(While) },
        { FunctionType.For, typeof(For) },
        { FunctionType.ForEach, typeof(ForEach) },
        { FunctionType.SetVariable, typeof(SetVariable) },
        { FunctionType.Return, typeof(Return) },
        { FunctionType.Wait, typeof(Wait) },
        { FunctionType.CallFunction, typeof(CallFunction) },
        { FunctionType.CallGroovyNoise, typeof(CallGroovyNoise) },
        { FunctionType.PlayAnimation, typeof(PlayAnimation) },
        { FunctionType.SendMessage, typeof(SendMessage) },
        { FunctionType.SendCommand, typeof(SendCommand) },
        { FunctionType.DropItems, typeof(DropItems) },
        { FunctionType.Explode, typeof(Explode) },
        { FunctionType.GiveEffect, typeof(GiveEffect) },
        { FunctionType.PlayAudio, typeof(PlayAudio) },
        { FunctionType.Warhead, typeof(FWarhead) },
        { FunctionType.ChangePlayerValue, typeof(ChangePlayerValue) },
        { FunctionType.PlayerAction, typeof(PlayerAction) },
        { FunctionType.ChangeEntityValue, typeof(ChangeEntityValue) },
    };
}

[Serializable]
public enum FunctionType
{
    If,
    ElseIf,
    Else,
    While,
    For,
    ForEach,
    Break,
    Continue,
    SetVariable,
    Return,
    Wait,
    CallFunction,
    CallGroovyNoise,
    PlayAnimation,
    SendMessage,
    SendCommand,
    DropItems,
    Explode,
    GiveEffect,
    PlayAudio,
    Warhead,
    ChangePlayerValue,
    PlayerAction,
    ChangeEntityValue,
}

[Serializable]
public class Function
{
    public virtual void OnValidate() { }
    public virtual FunctionReturn Execute(FunctionArgument args)
    {
        return new FunctionReturn();
    }
}

[Serializable]
public class DFunction : Function
{
    public override void OnValidate()
    {
    }
}

[Serializable]
public class ActionsFunctioner : Function
{
    public List<ScriptAction> Actions { get; set; }

    public override void OnValidate()
    {
        Actions.ForEach(x => x.OnValidate());
    }

    protected bool ConditionCheck(FunctionArgument args, object obj_)
    {
        if (obj_ is ScriptValue value)
        {
            object obj = value.GetValue(args);
            return obj != null && obj is bool && Convert.ToBoolean(obj);
        }
        else if (obj_ is List<ScriptValue> list)
        {
            return list.TrueForAll(x =>
            {
                object obj = x.GetValue(args);
                if (obj == null || obj is not bool)
                {
                    return false;
                }
                return Convert.ToBoolean(obj);
            });
        }
        return false;
    }

    protected async Task<FunctionReturn> ExecuteActions(FunctionArgument args, FunctionResult result = FunctionResult.Default)
    {
        bool ifActed = false;
        for (int i = 0; i < Actions.Count; i++)
        {
            if (Actions[i].ActionType == FunctionType.ElseIf || Actions[i].ActionType == FunctionType.Else)
            {
                if (ifActed)
                {
                    continue;
                }
            }
            else
            {
                ifActed = false;
            }
            FunctionReturn v = Actions[i].Execute(args);
            switch (v.Result)
            {
                case FunctionResult.Break:
                case FunctionResult.Continue:
                case FunctionResult.Return:
                    return v;
                case FunctionResult.Wait:
                    // TODO: Wait function is a no-op :)
                    //await Task.Delay(Mathf.RoundToInt(Convert.ToSingle(v.value) * 1000f));
                    break;
            }
            if (v.Result == FunctionResult.FunctionCheck)
            {
                switch (Actions[i].ActionType)
                {
                    case FunctionType.If:
                    case FunctionType.ElseIf:
                        ifActed = Convert.ToBoolean(v.Value);
                        break;
                }
            }
        }
        return new FunctionReturn { Result = result, Value = true };
    }
}

[Serializable]
public class ScriptValue
{
    public ValueType ValueType { get; set; }
    public Value Value { get; set; }

    public object GetValue(FunctionArgument args)
    {
        if (Value == null)
        {
            return null;
        }
        if (!EnumToV.TryGetValue(ValueType, out _))
        {
            switch (ValueType)
            {
                case ValueType.ZeroVector:
                    return Vector3.zero;
                case ValueType.EmptyArray:
                    return new object[] { };
            }
        }
        return Value.GetValue(args);
    }

    public T GetValue<T>(FunctionArgument args, T def)
    {
        object obj = GetValue(args);
        if (obj == null)
        {
            return def;
        }
        if ((typeof(int) == typeof(T) || typeof(float) == typeof(T)) && (obj is int || obj is float))
        {
            if (typeof(int) == typeof(T))
            {
                return (T)(object)(obj is int ? Convert.ToInt32(obj) : Mathf.RoundToInt(Convert.ToSingle(obj)));
            }
            else
            {
                return (T)(object)Convert.ToSingle(obj);
            }
        }
        if (obj is T t)
        {
            return t;
        }
        return def;
    }

    public void OnValidate()
    {
    }

    public static readonly Dictionary<ValueType, Type> EnumToV = new()
    {
        { ValueType.Integer, typeof(Integer) },
        { ValueType.Real, typeof(Real) },
        { ValueType.Bool, typeof(Bool) },
        { ValueType.String, typeof(String) },
        { ValueType.Compare, typeof(Compare) },
        { ValueType.IfThenElse, typeof(IfThenElse) },
        { ValueType.Array, typeof(Array) },
        { ValueType.Variable, typeof(Variable) },
        { ValueType.Argument, typeof(Argument) },
        { ValueType.Function, typeof(VFunction) },
        { ValueType.Vector, typeof(Vector) },
        { ValueType.NumUnaryOp, typeof(NumUnaryOp) },
        { ValueType.NumBinomialOp, typeof(NumBinomialOp) },
        { ValueType.ArrUnaryOp, typeof(ArrUnaryOp) },
        { ValueType.ArrBinomialOp, typeof(ArrBinomialOp) },
        { ValueType.VecUnaryOp, typeof(VecUnaryOp) },
        { ValueType.VecBinomialOp, typeof(VecBinomialOp) },
        { ValueType.StrUnaryOp, typeof(StrUnaryOp) },
        { ValueType.StrBinomialOp, typeof(StrBinomialOp) },
        { ValueType.ArrayEvaluateHelper, typeof(ArrayEvaluateHelper) },
        { ValueType.ConstValue, typeof(ConstValue) },
        { ValueType.EvaluateOnce, typeof(EvaluateOnce) },
        { ValueType.CollisionType, typeof(VCollisionType) },
        { ValueType.CollisionDetectTarget, typeof(CollisionDetectTarget) },
        { ValueType.EffectActionType, typeof(EffectActionType) },
        { ValueType.EffectName, typeof(EffectName) },
        { ValueType.TeleportInvokeType, typeof(VTeleportInvokeType) },
        { ValueType.WarheadActionType, typeof(VWarheadActionType) },
        { ValueType.AnimationActionType, typeof(AnimationActionType) },
        { ValueType.ParameterType, typeof(VParameterType) },
        { ValueType.MessageType, typeof(VMessageType) },
        { ValueType.PlayerArray, typeof(PlayerArray) },
        { ValueType.Scp914Mode, typeof(VScp914Mode) },
        { ValueType.ItemType, typeof(VItemType) },
        { ValueType.RoleType, typeof(VRoleType) },
        { ValueType.PlayerUnaryOp, typeof(PlayerUnaryOp) },
        { ValueType.SingleTarget, typeof(SingleTarget) },
        { ValueType.ItemUnaryOp, typeof(ItemUnaryOp) },
        { ValueType.EntityUnaryOp, typeof(EntityUnaryOp) },
        { ValueType.EntityBinomialOp, typeof(EntityBinomialOp) },
    };
}

[Serializable]
public enum ValueType
{
    Integer,
    Real,
    Bool,
    String,
    Null,
    Compare,
    IfThenElse,
    EmptyArray,
    Array,
    Variable,
    Argument,
    Function,
    ZeroVector,
    Vector,
    NumUnaryOp,
    NumBinomialOp,
    ArrUnaryOp,
    ArrBinomialOp,
    VecUnaryOp,
    VecBinomialOp,
    StrUnaryOp,
    StrBinomialOp,
    ArrayEvaluateHelper,
    ConstValue,
    EvaluateOnce,
    CollisionType,
    CollisionDetectTarget,
    EffectActionType,
    // TODO: Does EffectType -> EffectName affect existing jsons?
    EffectName,
    TeleportInvokeType,
    WarheadActionType,
    AnimationActionType,
    ParameterType,
    MessageType,
    PlayerArray,
    Scp914Mode,
    ItemType,
    RoleType,
    SingleTarget,
    ItemUnaryOp,
    PlayerUnaryOp,
    EntityUnaryOp,
    EntityBinomialOp,
}

[Serializable]
public class Value
{
    public virtual void OnValidate() { }
    public virtual object GetValue(FunctionArgument args)
    {
        return null;
    }
}

[Serializable]
public class DValue : Value
{
    public override void OnValidate()
    {
    }
}