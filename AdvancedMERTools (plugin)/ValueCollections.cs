using InventorySystem.Items.Pickups;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedMERTools;

#pragma warning disable SA1401 // Field should be private

[Serializable]
public class Integer : Value
{
    public int Value;
    public override void OnValidate() { }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class Real : Value
{
    public float Value;
    public override void OnValidate() { }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class Bool : Value
{
    public bool Value;
    public override void OnValidate() { }
    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class String : Value
{
    public string Value;
    [Header("Replace {0}, {1}, ... to assgined values.")]
    public List<ScriptValue> StringInterpolations = new List<ScriptValue> { };
    public override void OnValidate()
    {
        StringInterpolations.ForEach(x => x.OnValidate());
    }

    public override object GetValue(FunctionArgument args)
    {
        string str = (string)Value.Clone();
        for (int i = 0; i < StringInterpolations.Count; i++)
        {
            str = str.Replace("{" + i.ToString() + "}", StringInterpolations[i].GetValue(args).ToString());
        }
        return str;
    }
}

[Serializable]
public class Compare : Value
{
    [Serializable]
    public enum CompareType
    {
        Equal,
        NotEqual,
        Bigger,
        BigOrEqual,
        Less,
        LessOrEqual,
        TypeEqual,
        And,
        Or,
        Xor,
        Not,
    }

    public ScriptValue Value1;
    public CompareType Operator;
    public ScriptValue Value2;

    public override void OnValidate()
    {
        Value1.OnValidate();
        Value2.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        object obj1 = Value1.GetValue(args);
        object obj2 = Value2.GetValue(args);
        if (obj1 == null || obj2 == null)
        {
            return false;
        }
        if (Operator == CompareType.TypeEqual)
        {
            return obj1.GetType() == obj2.GetType();
        }

        if ((obj1 is int || obj1 is float) && (obj2 is int || obj2 is float))
        {
            float v1 = Convert.ToSingle(obj1);
            float v2 = Convert.ToSingle(obj2);
            return Operator switch
            {
                CompareType.Equal => v1 == v2,
                CompareType.NotEqual => v1 != v2,
                CompareType.Bigger => v1 > v2,
                CompareType.BigOrEqual => v1 >= v2,
                CompareType.Less => v1 < v2,
                CompareType.LessOrEqual => v1 <= v2,
                _ => (object)false,
            };
        }
        if (obj1 is bool && Operator == CompareType.Not)
        {
            return !Convert.ToBoolean(obj1);
        }

        if (obj1 is bool && obj2 is bool)
        {
            bool v1 = Convert.ToBoolean(obj1);
            bool v2 = Convert.ToBoolean(obj2);
            return Operator switch
            {
                CompareType.Equal or CompareType.BigOrEqual or CompareType.LessOrEqual => v1 == v2,
                CompareType.NotEqual or CompareType.Xor => v1 != v2,
                CompareType.And => v1 && v2,
                CompareType.Or => v1 || v2,
                _ => (object)false,
            };
        }
        if (Operator == CompareType.Equal || Operator == CompareType.NotEqual)
        {
            if (obj1.GetType() == obj2.GetType())
            {
                bool flag = obj1.Equals(obj2);
                return Operator == CompareType.Equal ? flag : !flag;
            }
        }
        return false;
    }
}

[Serializable]
public class IfThenElse : Value
{
    public ScriptValue Statement;
    public ScriptValue Then;
    public ScriptValue Else;
    public override void OnValidate()
    {
        Statement.OnValidate();
        Then.OnValidate();
        Else.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        return Statement.GetValue(args, false) ? Then.GetValue(args) : Else.GetValue(args);
    }
}

[Serializable]
public class Array : Value
{
    public ScriptValue[] Values;
    public override void OnValidate()
    {
        for (int i = 0; Values != null && i < Values.Length; i++)
        {
            Values[i].OnValidate();
        }
    }

    public override object GetValue(FunctionArgument args)
    {
        return Values.Select(x => x.GetValue(args)).ToArray();
    }
}

[Serializable]
public class Variable : Value
{
    public ScriptValue VariableName;
    public ScriptValue AccessLevel;
    public override void OnValidate()
    {
        VariableName.OnValidate();
        AccessLevel.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        string str = VariableName.GetValue<string>(args, null);
        if (str == null)
        {
            return null;
        }

        return Math.Min(3, Math.Max(0, AccessLevel.GetValue(args, 0))) switch
        {
            0 => args.FunctionVariables.TryGetValue(str, out object value) ? value : null,
            1 => args.Function.ScriptVariables.TryGetValue(str, out object value) ? value : null,
            2 => AdvancedMERTools.Singleton.SchematicVariables[args.Function.OSchematic].TryGetValue(str, out object value) ? value : null,
            3 => AdvancedMERTools.Singleton.RoundVariable.TryGetValue(str, out object value) ? value : null,
            _ => null,
        };
    }
}

[Serializable]
public class Argument : Value
{
    public ScriptValue ArgumentName;
    public override void OnValidate()
    {
        ArgumentName.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        int index = args.Function.ArgumentsName.IndexOf(ArgumentName.GetValue(args, ""));
        return index == -1 || index >= args.Arguments.Count ? 0 : args.Arguments[index];
    }
}

[Serializable]
public class VFunction : Value
{
    public ScriptValue FunctionName;
    public List<ScriptValue> Arguments;
    public override void OnValidate()
    {
        FunctionName.OnValidate();
        Arguments.ForEach(x => x.OnValidate());
    }

    public override object GetValue(FunctionArgument args)
    {
        if (AdvancedMERTools.Singleton.FunctionExecutors[args.Function.OSchematic].TryGetValue(FunctionName.GetValue(args, ""), out FunctionExecutor function))
        {
            return function.Data.Execute(new FunctionArgument { Arguments = this.Arguments.Select(x => x.GetValue(args)).ToList(), Player = args.Player }).Value;
        }
        return null;
    }
}

[Serializable]
public class Vector : Value
{
    public ScriptValue X;
    public ScriptValue Y;
    public ScriptValue Z;
    public override void OnValidate()
    {
        X.OnValidate();
        Y.OnValidate();
        Z.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        return new Vector3(X.GetValue(args, 0f), Y.GetValue(args, 0f), Z.GetValue(args, 0f));
    }
}

[Serializable]
public class NumUnaryOp : Value
{
    [Serializable]
    public enum NumUnaryOpType
    {
        Inverse,
        Reciprocal,
        Absolute,
        Factorial,
        Ceiling,
        Floor,
        Round,
        CommonLog,
        NaturalLog,
        BinaryLog,
        Exponential,
        Sine,
        Cosine,
        Tangent,
        Arcsine,
        Arccosine,
        Arctangent,
        Sigmoid,
        IsPrimeNumber,
    }

    public NumUnaryOpType Operator;
    public ScriptValue Value;

    public override void OnValidate()
    {
        Value.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        object v = Value.GetValue(args);
        if (v == null)
        {
            return 0;
        }

        if (v is int || v is float)
        {
            float f = Convert.ToSingle(v);
            switch (Operator)
            {
                case NumUnaryOpType.Absolute:
                    return Mathf.Abs(f);
                case NumUnaryOpType.Arccosine:
                    return Mathf.Acos(f);
                case NumUnaryOpType.Arcsine:
                    return Mathf.Asin(f);
                case NumUnaryOpType.Arctangent:
                    return Mathf.Atan(f);
                case NumUnaryOpType.BinaryLog:
                    return Mathf.Log(f, 2);
                case NumUnaryOpType.Ceiling:
                    return Mathf.Ceil(f);
                case NumUnaryOpType.CommonLog:
                    return Mathf.Log10(f);
                case NumUnaryOpType.Cosine:
                    return Mathf.Cos(f);
                case NumUnaryOpType.Exponential:
                    return Mathf.Exp(f);
                case NumUnaryOpType.Factorial:
                    return CalcHelper.Fact(f);
                case NumUnaryOpType.Floor:
                    return Mathf.Floor(f);
                case NumUnaryOpType.Inverse:
                    return -f;
                case NumUnaryOpType.IsPrimeNumber:
                    if (Mathf.Round(f) != f)
                    {
                        return false;
                    }
                    for (int i = 2; i * i <= f; i++)
                    {
                        if (f % i == 0)
                        {
                            return false;
                        }
                    }
                    return true;
                case NumUnaryOpType.NaturalLog:
                    return Mathf.Log(f, Mathf.Exp(1));
                case NumUnaryOpType.Reciprocal:
                    if (f == 0)
                    {
                        return 0;
                    }
                    return 1f / f;
                case NumUnaryOpType.Round:
                    return Mathf.Round(f);
                case NumUnaryOpType.Sigmoid:
                    return 1f / (1f + Mathf.Exp(-f));
                case NumUnaryOpType.Sine:
                    return Mathf.Sin(f);
                case NumUnaryOpType.Tangent:
                    return Mathf.Tan(f);
            }
        }
        return 0;
    }
}

[Serializable]
public class NumBinomialOp : Value
{
    [Serializable]
    public enum NumBiOpType
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        RaiseToPower,
        Log,
        Max,
        Min,
        Permutation,
        Combination,
        Random,
        GCD,
        LCM,
    }

    public NumBiOpType Operator;
    public ScriptValue Value1;
    public ScriptValue Value2;

    public override void OnValidate()
    {
        Value1.OnValidate();
        Value2.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        object v1 = Value1.GetValue(args);
        object v2 = Value2.GetValue(args);
        if (v1 == null || v2 == null)
        {
            return 0;
        }

        if ((v1 is int || v1 is float) && (v2 is int || v2 is float))
        {
            float i = Convert.ToSingle(v1);
            float j = Convert.ToSingle(v2);
            switch (Operator)
            {
                case NumBiOpType.Add:
                    return i + j;
                case NumBiOpType.Combination:
                    return CalcHelper.Fact(i) / Mathf.Max(CalcHelper.Fact(i - j) * CalcHelper.Fact(j), 1);
                case NumBiOpType.Divide:
                    if (j == 0)
                    {
                        return 0;
                    }
                    if (v1 is int && v2 is int)
                    {
                        return (int)i / (int)j;
                    }
                    return i / j;
                case NumBiOpType.GCD:
                    return CalcHelper.GCD(i, j);
                case NumBiOpType.LCM:
                    return (int)Mathf.Abs(i * j) / CalcHelper.GCD(i, j);
                case NumBiOpType.Log:
                    return Mathf.Log(j, i);
                case NumBiOpType.Max:
                    return Mathf.Max(i, j);
                case NumBiOpType.Min:
                    return Mathf.Min(i, j);
                case NumBiOpType.Modulo:
                    if (j == 0)
                    {
                        return 0;
                    }
                    return i % j;
                case NumBiOpType.Multiply:
                    return i * j;
                case NumBiOpType.Permutation:
                    return CalcHelper.Fact(i) / Mathf.Max(1, CalcHelper.Fact(i - j));
                case NumBiOpType.RaiseToPower:
                    return Mathf.Pow(i, j);
                case NumBiOpType.Random:
                    if (v1 is int && v2 is int)
                    {
                        return UnityEngine.Random.Range((int)i, (int)j);
                    }
                    return UnityEngine.Random.Range(i, j);
                case NumBiOpType.Subtract:
                    return i - j;
            }
        }
        return 0;
    }
}

public static class CalcHelper
{
    public static int Fact(float v)
    {
        v = Mathf.Round(v);
        int pow = 1;
        for (int i = 2; i <= v; i++)
        {
            pow *= i;
        }
        return pow;
    }

    public static int GCD(float a_, float b_)
    {
        int a = Mathf.RoundToInt(a_);
        int b = Mathf.RoundToInt(b_);
        if (b == 0)
        {
            return 0;
        }
        if (a % b == 0)
        {
            return b;
        }
        return GCD(b, a % b);
    }
}

[Serializable]
public class ArrUnaryOp : Value
{
    [Serializable]
    public enum ArrUnaryOpType
    {
        Length,
        Reverse,
        GetRandomValue,
        Shuffled,
        RemovedNull,
        IsEmpty,
    }

    public ArrUnaryOpType Operator;
    public ScriptValue Array;

    public override void OnValidate()
    {
        Array.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        object v = Array.GetValue(args);
        if (v != null && v is object[])
        {
            object[] val = v as object[];
            switch (Operator)
            {
                case ArrUnaryOpType.GetRandomValue:
                    return val.RandomItem();
                case ArrUnaryOpType.Length:
                    return val.Length;
                case ArrUnaryOpType.RemovedNull:
                    return val.Where(x => x != null).ToArray();
                case ArrUnaryOpType.Reverse:
                    return val.Select((x, y) => (x, y)).OrderBy(x => -x.y).Select(x => x.x).ToArray();
                case ArrUnaryOpType.Shuffled:
                    return val.OrderBy(x => UnityEngine.Random.Range(int.MinValue, int.MaxValue)).ToArray();
                case ArrUnaryOpType.IsEmpty:
                    return val.Length == 0;
            }
        }
        return null;
    }
}

[Serializable]
public class ArrBinomialOp : Value
{
    [Serializable]
    public enum ArrBiOpType
    {
        AppendToArray,
        RemoveFromArray,
        Skip,
        SkipLast,
        FilteredArray,
        MappedArray,
        SortedArray,
        TrueForAll,
        TrueForAny,
        Contains,
        ElementAt,
        IndexOfElement,
        Count,
        Max,
        Min,
        Sum,
        Product,
        Union,
        Intersect,
        DifferenceOfSets,
        Disjoint,
        Repeat,
    }

    public ArrBiOpType Operator;
    public ScriptValue Array;
    public ScriptValue EvaluationRule;

    public override void OnValidate()
    {
        Array.OnValidate();
        EvaluationRule.OnValidate();
    }

    private readonly ArrBiOpType[] prevaluate = new ArrBiOpType[]
    {
        ArrBiOpType.AppendToArray,
        ArrBiOpType.Contains,
        ArrBiOpType.DifferenceOfSets,
        ArrBiOpType.Disjoint,
        ArrBiOpType.ElementAt,
        ArrBiOpType.IndexOfElement,
        ArrBiOpType.Intersect,
        ArrBiOpType.RemoveFromArray,
        ArrBiOpType.Repeat,
        ArrBiOpType.Skip,
        ArrBiOpType.SkipLast,
        ArrBiOpType.Union,
    };

    public override object GetValue(FunctionArgument args)
    {
        object v = Array.GetValue(args);
        if (v != null && v is object[])
        {
            object[] a = v as object[];
            if (prevaluate.Contains(Operator))
            {
                object s = EvaluationRule.GetValue(args);
                if (s == null)
                {
                    return a;
                }

                switch (Operator)
                {
                    case ArrBiOpType.AppendToArray:
                        return a.Append(s).ToArray();
                    case ArrBiOpType.Contains:
                        return a.Contains(s);
                    case ArrBiOpType.DifferenceOfSets:
                        if (s is not object[])
                        {
                            return a;
                        }
                        return a.Except(s as object[]).ToArray();
                    case ArrBiOpType.Disjoint:
                        if (s is not object[])
                        {
                            return a;
                        }
                        return a.Union(s as object[]).Except(a.Intersect(s as object[])).ToArray();
                    case ArrBiOpType.ElementAt:
                        if (s is int || s is float)
                        {
                            int index = Mathf.RoundToInt(Convert.ToSingle(s));
                            index = Math.Min(a.Length - 1, Math.Max(0, index));
                            return a[index];
                        }
                        return a.First();
                    case ArrBiOpType.IndexOfElement:
                        return a.IndexOf(s);
                    case ArrBiOpType.Intersect:
                        if (s is not object[])
                        {
                            return a;
                        }
                        return a.Intersect(s as object[]).ToArray();
                    case ArrBiOpType.RemoveFromArray:
                        return a.Except(new object[] { s }).ToArray();
                    case ArrBiOpType.Repeat:
                        if (s is int || s is float)
                        {
                            int r = Mathf.RoundToInt(Convert.ToSingle(s));
                            List<object> list = new List<object> { };
                            for (int i = 0; i < r; i++)
                            {
                                list.AddRange(a);
                            }
                            return list.ToArray();
                        }
                        return a;
                    case ArrBiOpType.Skip:
                        if (s is int || s is float)
                        {
                            return a.Skip(Mathf.RoundToInt(Convert.ToSingle(s))).ToArray();
                        }
                        return a;
                    case ArrBiOpType.SkipLast:
                        if (s is int || s is float)
                        {
                            a.Reverse();
                            a = a.Skip(Mathf.RoundToInt(Convert.ToSingle(s))).ToArray();
                            a.Reverse();
                            return a;
                        }
                        return a;
                    case ArrBiOpType.Union:
                        if (s is not object[])
                        {
                            return a;
                        }
                        return a.Union(s as object[]).ToArray();
                }
            }
            else
            {
                object[] evaluated = new object[a.Length];
                for (int i = 0; i < a.Length; i++)
                {
                    if (i == 0)
                    {
                        args.Levels.Add((a[i], i));
                    }
                    else
                    {
                        args.Levels[args.Levels.Count - 1] = (a[i], i);
                    }
                    evaluated[i] = EvaluationRule.GetValue(args);
                }
                args.Levels.RemoveAt(args.Levels.Count - 1);
                switch (Operator)
                {
                    case ArrBiOpType.Count:
                        return a.Select((x, y) => (x, y)).Count(x => Convert.ToBoolean(evaluated[x.y]));
                    case ArrBiOpType.FilteredArray:
                        return a.Select((x, y) => (x, y)).Where(x => Convert.ToBoolean(evaluated[x.y])).Select(x => x.x).ToArray();
                    case ArrBiOpType.MappedArray:
                        return evaluated;
                    case ArrBiOpType.Max:
                        int index = 0;
                        float max = float.MinValue;
                        for (int i = 0; i < a.Length; i++)
                        {
                            object ob = evaluated[i];
                            if (ob is int || ob is float)
                            {
                                float ev = Convert.ToSingle(ob);
                                if (ev > max)
                                {
                                    index = i;
                                    max = ev;
                                }
                            }
                        }
                        return a[index];
                    case ArrBiOpType.Min:
                        index = 0;
                        max = float.MaxValue;
                        for (int i = 0; i < a.Length; i++)
                        {
                            object ob = evaluated[i];
                            if (ob is int || ob is float)
                            {
                                float ev = Convert.ToSingle(ob);
                                if (ev < max)
                                {
                                    index = i;
                                    max = ev;
                                }
                            }
                        }
                        return a[index];
                    case ArrBiOpType.Product:
                        float prod = 1;
                        for (int i = 0; i < a.Length; i++)
                        {
                            object ob = evaluated[i];
                            if (ob is int || ob is float)
                            {
                                prod *= Convert.ToSingle(ob);
                            }
                        }
                        return prod;
                    case ArrBiOpType.SortedArray:
                        float[] eva = evaluated.Select(x => x is int || x is float ? Convert.ToSingle(x) : 0).ToArray();
                        return a.Select((x, y) => (x, y)).OrderBy(x => eva[x.y]).Select(x => x.x).ToArray();
                    case ArrBiOpType.Sum:
                        prod = 0;
                        for (int i = 0; i < a.Length; i++)
                        {
                            object ob = evaluated[i];
                            if (ob is int || ob is float)
                            {
                                prod += Convert.ToSingle(ob);
                            }
                        }
                        return prod;
                    case ArrBiOpType.TrueForAll:
                        return a.All(x => x is bool ? Convert.ToBoolean(x) : false);
                    case ArrBiOpType.TrueForAny:
                        return a.Any(x => x is bool ? Convert.ToBoolean(x) : false);
                }
            }
        }
        return 0;
    }
}

[Serializable]
public class VecUnaryOp : Value
{
    [Serializable]
    public enum VecUnaryOpType
    {
        X,
        Y,
        Z,
        Normalized,
        Magnitude,
        Inverse,
        DirectionFromAngles,
        AnglesFromDirection,
    }

    public VecUnaryOpType Operator;
    public ScriptValue Vector;

    public override void OnValidate()
    {
        Vector.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        object var = Vector.GetValue(args);
        Vector3 v = var == null || !(var is Vector3) ? Vector3.zero : (Vector3)var;
        return Operator switch
        {
            VecUnaryOpType.X => v.x,
            VecUnaryOpType.Y => v.y,
            VecUnaryOpType.Z => v.z,
            VecUnaryOpType.Normalized => v == Vector3.zero ? Vector3.zero : v.normalized,
            VecUnaryOpType.Magnitude => v.magnitude,
            VecUnaryOpType.Inverse => new Vector3(-v.x, -v.y, -v.z),
            VecUnaryOpType.DirectionFromAngles => Quaternion.Euler(v) * Vector3.forward,
            VecUnaryOpType.AnglesFromDirection => v == Vector3.zero ? Vector3.zero : Quaternion.LookRotation(v, Vector3.up).eulerAngles,
            _ => Vector3.zero,
        };
    }
}

[Serializable]
public class VecBinomialOp : Value
{
    [Serializable]
    public enum VecBiOpType
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Distance,
        AngleBetweenVectors,
        AngleDifference,
        DotProduct,
        CrossProduct,
        DirectionTowards,
    }

    public VecBiOpType Operator;
    public ScriptValue Value1;
    public ScriptValue Value2;

    public override void OnValidate()
    {
        Value1.OnValidate();
        Value2.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        object var1 = Value1.GetValue(args);
        object var2 = Value2.GetValue(args);
        if (var1 == null || var2 == null)
        {
            return null;
        }

        if (var1 is Vector3 v1 && var2 is Vector3 v2)
        {
            switch (Operator)
            {
                case VecBiOpType.Add:
                    return v1 + v2;
                case VecBiOpType.AngleBetweenVectors:
                    return Vector3.Angle(v1, v2);
                case VecBiOpType.AngleDifference:
                    return new Vector3(Mathf.DeltaAngle(v1.x, v2.x), Mathf.DeltaAngle(v1.y, v2.y), Mathf.DeltaAngle(v1.z, v2.z)).magnitude;
                case VecBiOpType.CrossProduct:
                    return Vector3.Cross(v1, v2);
                case VecBiOpType.DirectionTowards:
                    return (v2 - v1).normalized;
                case VecBiOpType.Distance:
                    return Vector3.Distance(v1, v2);
                case VecBiOpType.Divide:
                    return new Vector3(v1.x / (v2.x == 0 ? 1 : v2.x), v1.y / (v2.y == 0 ? 1 : v2.y), v1.z / (v2.z == 0 ? 1 : v2.z));
                case VecBiOpType.DotProduct:
                    return Vector3.Dot(v1, v2);
                case VecBiOpType.Multiply:
                    return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
                case VecBiOpType.Subtract:
                    return v1 - v2;
            }
        }
        else if ((var1 is int || var1 is float) && var2 is Vector3 v3)
        {
            float val = Convert.ToSingle(var1);
            switch (Operator)
            {
                case VecBiOpType.Multiply:
                    return val * v3;
            }
        }
        else if ((var2 is int || var2 is float) && var1 is Vector3 v4)
        {
            float val = Convert.ToSingle(var2);
            switch (Operator)
            {
                case VecBiOpType.Multiply:
                    return v4 * val;
                case VecBiOpType.Divide:
                    return v4 / val;
            }
        }
        return null;
    }
}

[Serializable]
public class StrUnaryOp : Value
{
    [Serializable]
    public enum StrUnaryOpType
    {
        Length,
        Upper,
        Lower,
        UpperLowerSwitch,
        Strip,
        Trim,
        ToInteger,
        ToReal,
        ToCharArray,
        ToPlayerAsName,
        ToItemAsName,
    }

    public StrUnaryOpType Operator;
    public ScriptValue String;

    public override void OnValidate()
    {
        String.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        string str = String.GetValue(args, "");
        switch (Operator)
        {
            case StrUnaryOpType.Length:
                return str.Length;
            case StrUnaryOpType.Lower:
                return str.ToLower();
            case StrUnaryOpType.Strip:
                return str.Replace(" ", "");
            case StrUnaryOpType.ToCharArray:
                return str.ToCharArray();
            case StrUnaryOpType.ToInteger:
                return int.TryParse(str, out int v_i) ? v_i : 0;
            case StrUnaryOpType.ToReal:
                return float.TryParse(str, out float v_f) ? v_f : 0f;
            case StrUnaryOpType.Trim:
                return str.Trim();
            case StrUnaryOpType.Upper:
                return str.ToUpper();
            case StrUnaryOpType.UpperLowerSwitch:
                char[] vs = str.ToCharArray();
                for (int i = 0; i < vs.Length; i++)
                {
                    if (vs[i] >= 'A' && vs[i] <= 'Z')
                    {
                        vs[i] = (char)(vs[i] - 'A' + 'a');
                    }
                    else if (vs[i] >= 'a' && vs[i] <= 'z')
                    {
                        vs[i] = (char)(vs[i] - 'a' + 'A');
                    }
                }
                return new string(vs);
            case StrUnaryOpType.ToPlayerAsName:
                if (Player.List.Any(x => x.Nickname == str))
                {
                    return Player.List.First(x => x.Nickname == str);
                }
                return null;
            case StrUnaryOpType.ToItemAsName:
                if (Enum.TryParse(str, out ItemType type))
                {
                    return type;
                }
                return null;
        }
        return str;
    }
}

[Serializable]
public class StrBinomialOp : Value
{
    [Serializable]
    public enum StrBinomialOpType
    {
        Add,
        Repeat,
        Contains,
        Split,
        Skip,
        SkipLast,
    }

    public StrBinomialOpType Operator;
    public ScriptValue String;
    public ScriptValue EvaluationRule;

    public override void OnValidate()
    {
        String.OnValidate();
        EvaluationRule.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        object v1 = String.GetValue(args);
        object v2 = EvaluationRule.GetValue(args);
        if (v1 == null || v1 is not string)
        {
            return null;
        }

        string str = Convert.ToString(v1);
        if (v2 is int || v2 is float)
        {
            float f = Convert.ToSingle(v2);
            switch (Operator)
            {
                case StrBinomialOpType.Repeat:
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int i = 0; i < f; i++)
                    {
                        sb.Append(str);
                    }
                    return sb.ToString();
                case StrBinomialOpType.Skip:
                    return new string(str.Skip(Mathf.RoundToInt(f)).ToArray());
                case StrBinomialOpType.SkipLast:
                    return new string(str.Reverse().Skip(Mathf.RoundToInt(f)).Reverse().ToArray());
            }
        }
        else if (v2 is string)
        {
            string st = Convert.ToString(v2);
            switch (Operator)
            {
                case StrBinomialOpType.Add:
                    return str + st;
                case StrBinomialOpType.Contains:
                    return str.Contains(st);
                case StrBinomialOpType.Split:
                    return str.Split(new string[] { st }, int.MaxValue, StringSplitOptions.None);
            }
        }
        return str;
    }
}

[Serializable]
public class ArrayEvaluateHelper : Value
{
    [Serializable]
    public enum AEVHelperType
    {
        CurrentEvaluateElement,
        CurrentEvaluateIndex,
    }

    public AEVHelperType Value;
    [Header("Increase with its nested level from 0.")]
    public ScriptValue AccessLevel;

    public override void OnValidate()
    {
        AccessLevel.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        object v = AccessLevel.GetValue(args);
        int a = v == null || !(v is int || v is float) ? 0 : Mathf.RoundToInt(Convert.ToSingle(v));
        a = Math.Min(args.Levels.Count - 1, Math.Max(0, a));
        return Value == AEVHelperType.CurrentEvaluateElement ? args.Levels[a].Item1 : args.Levels[a].Item2;
    }
}

[Serializable]
public class ConstValue : Value
{
    [Serializable]
    public enum ConstValueType
    {
        PI,
        E,
        GoldenRatio,
        C,
        TheAnswerToLifeTheUniverseAndEverything,
    }

    public ConstValueType Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value switch
        {
            ConstValueType.C => 299792458,
            ConstValueType.E => Mathf.Exp(1),
            ConstValueType.GoldenRatio => (1f + (float)Math.Pow(5, 0.5)) / 2f,
            ConstValueType.PI => Mathf.PI,
            _ => (object)42,
        };
    }
}

[Serializable]
public class EvaluateOnce : Value
{
    public ScriptValue Value;
    private bool flag = false;
    private object valued = null;

    public override void OnValidate()
    {
        Value.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        if (flag)
        {
            return valued;
        }

        flag = true;
        return valued = Value.GetValue(args);
    }
}

[Serializable]
public class VCollisionType : Value
{
    public CollisionType Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class CollisionDetectTarget : Value
{
    public DetectType Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class EffectActionType : Value
{
    public EffectFlagE Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class VEffectType : Value
{
    // TODO: Maybe at some point we could make an enum of all StatusEffectBase names...
    //       For now it's just a placeholder that's really just a string
    //public string Value;
    public EffectType Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class VTeleportInvokeType : Value
{
    public TeleportInvokeType Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class VWarheadActionType : Value
{
    public WarheadActionType Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class AnimationActionType : Value
{
    public AnimationTypeE Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class VParameterType : Value
{
    public ParameterTypeE Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class VMessageType : Value
{
    public MessageTypeE Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class PlayerArray : Value
{
    public enum PlayerArrayType
    {
        AllPlayers,
        AlivePlayers,
        Scps,
        ScpsExcludeScp0492,
        Mtfs,
        Chaos,
        Dclass,
        Scientist,
        Spectators,
        Guards,
        FoundationSide,
        AntiFoundationSide,
        Humans,
    }

    public PlayerArrayType ArrayType;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return ArrayType switch
        {
            PlayerArrayType.AlivePlayers => Player.List.Where(x => x.IsAlive).ToArray(),
            PlayerArrayType.AllPlayers => Player.List.ToArray(),
            PlayerArrayType.AntiFoundationSide => Player.List.Where(x => x.Team.GetFaction() == Faction.FoundationEnemy).ToArray(),
            PlayerArrayType.Chaos => Player.List.Where(x => x.Team == Team.ChaosInsurgency).ToArray(),
            PlayerArrayType.Dclass => Player.List.Where(x => x.Team == Team.ClassD).ToArray(),
            PlayerArrayType.FoundationSide => Player.List.Where(x => x.Team.GetFaction() == Faction.FoundationStaff).ToArray(),
            PlayerArrayType.Guards => Player.List.Where(x => x.Role == RoleTypeId.FacilityGuard).ToArray(),
            PlayerArrayType.Humans => Player.List.Where(x => x.IsHuman).ToArray(),
            PlayerArrayType.Mtfs => Player.List.Where(x => x.Team == Team.FoundationForces && x.Role != RoleTypeId.FacilityGuard).ToArray(),
            PlayerArrayType.Scientist => Player.List.Where(x => x.Team == Team.Scientists).ToArray(),
            PlayerArrayType.Scps => Player.List.Where(x => x.Team == Team.SCPs).ToArray(),
            PlayerArrayType.ScpsExcludeScp0492 => Player.List.Where(x => x.Team == Team.SCPs && x.Role != RoleTypeId.Scp0492).ToArray(),
            PlayerArrayType.Spectators => Player.List.Where(x => x.Role == RoleTypeId.Spectator).ToArray(),
            _ => null,
        };
    }
}

[Serializable]
public class SingleTarget : Value
{
    public enum SingleTargetType
    {
        EventPlayer,
        SchematicEntity,
        ScriptEntity,
    }

    public SingleTargetType TargetType;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return TargetType switch
        {
            SingleTargetType.EventPlayer => args.Player,
            SingleTargetType.SchematicEntity => args.Schematic.gameObject,
            SingleTargetType.ScriptEntity => args.Transform.gameObject,
            _ => null,
        };
    }
}

[Serializable]
public class VScp914Mode : Value
{
    public Scp914Mode Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class VItemType : Value
{
    public ItemType Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class VRoleType : Value
{
    public RoleTypeId Value;

    public override void OnValidate()
    {
    }

    public override object GetValue(FunctionArgument args)
    {
        return Value;
    }
}

[Serializable]
public class ItemUnaryOp : Value
{
    [Serializable]
    public enum ItemUnaryOpType
    {
        ItemType,
        Entity,
        Owner,
        PrevOwner,
    }

    public ScriptValue ItemOrPickup;
    public ItemUnaryOpType Operator;

    public override void OnValidate()
    {
        ItemOrPickup.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        Item item = ItemOrPickup.GetValue<Item>(args, null);
        if (item != null)
        {
            return Operator switch
            {
                ItemUnaryOpType.ItemType => item.Type,
                ItemUnaryOpType.Owner => item.CurrentOwner,
                _ => null,
            };
        }
        ItemPickupBase pickup = ItemOrPickup.GetValue<ItemPickupBase>(args, null);
        if (pickup != null)
        {
            return Operator switch
            {
                ItemUnaryOpType.ItemType => pickup.Info.ItemId,
                ItemUnaryOpType.PrevOwner => pickup.PreviousOwner,
                _ => null,
            };
        }
        return null;
    }
}

[Serializable]
public class PlayerUnaryOp : Value
{
    [Serializable]
    public enum PlayerUnaryOpType
    {
        AHP,
        Cuffer,
        CurrentItem,
        CurrentSpectatingPlayers,
        CustomInfo,
        CustomName,
        DisplayNickname,
        GroupName,
        HP,
        HumeShield,
        Id,
        IsAlive,
        IsCHI,
        IsCuffed,
        IsDead,
        IsFoundationSide,
        IsFFon,
        IsHuman,
        IsInPocketDimension,
        IsInventoryEmpty,
        IsInventoryFull,
        IsJumping,
        IsNPC,
        IsNTF,
        IsReloading,
        IsScp,
        IsSpeaking,
        IsTutorial,
        IsUsingStamina,
        Items,
        MaxAHP,
        MaxHP,
        MaxHumeShield,
        Position,
        Role,
        FacingDirection,
        Scale,
        Stamina,
        EntityObject,
        UniqueRole,
        Velocity,
    }

    public PlayerUnaryOpType Operator;
    public ScriptValue Player;

    public override void OnValidate()
    {
        Player.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        Player p = Player.GetValue<Player>(args, null);
        if (p == null)
        {
            return null;
        }

        // TODO: Some of these definitely need to be addressed
        return Operator switch
        {
            PlayerUnaryOpType.AHP => p.ArtificialHealth,
            PlayerUnaryOpType.Cuffer => null,                                                   // LabAPI has cuffing events but no cuffed/cuffer status on player
                                                                                                //return p.Cuffer;
            PlayerUnaryOpType.CurrentItem => p.CurrentItem,
            PlayerUnaryOpType.CurrentSpectatingPlayers => LabApi.Features.Wrappers.Player.List.Where(x => x.Role == RoleTypeId.Spectator).ToArray(),
            PlayerUnaryOpType.CustomInfo => p.CustomInfo,
            PlayerUnaryOpType.CustomName => p.ReferenceHub.nicknameSync.CombinedName,           // Not 100% sure if this is what's intended by CustomName
            PlayerUnaryOpType.DisplayNickname => p.Nickname,
            PlayerUnaryOpType.EntityObject => p.GameObject,
            PlayerUnaryOpType.FacingDirection => p.Camera.forward,
            PlayerUnaryOpType.GroupName => p.GroupName,
            PlayerUnaryOpType.HP => p.Health,
            PlayerUnaryOpType.HumeShield => p.HumeShield,
            PlayerUnaryOpType.Id => p.PlayerId,
            PlayerUnaryOpType.IsAlive => p.IsAlive,
            PlayerUnaryOpType.IsCHI => false,                                                   // No idea what CHI is
            PlayerUnaryOpType.IsCuffed => false,                                                // See cuff above
            PlayerUnaryOpType.IsDead => !p.IsAlive,
            PlayerUnaryOpType.IsFFon => Server.FriendlyFire,
            PlayerUnaryOpType.IsFoundationSide => p.Team.GetFaction() == Faction.FoundationStaff,
            PlayerUnaryOpType.IsHuman => p.IsHuman,
            PlayerUnaryOpType.IsInPocketDimension => p.Room.Name == RoomName.Pocket,
            PlayerUnaryOpType.IsInventoryEmpty => p.IsWithoutItems,
            PlayerUnaryOpType.IsInventoryFull => p.IsInventoryFull,
            PlayerUnaryOpType.IsJumping => false,                                               // Has events but no player flag
            PlayerUnaryOpType.IsNPC => p.IsNpc,
            PlayerUnaryOpType.IsNTF => p.IsNTF,
            PlayerUnaryOpType.IsReloading => false,                                             // Has events but no player flag
                                                                                                //return p.CurrentItem != null && p.CurrentItem.Category == ItemCategory.Firearm && (p.CurrentItem as FirearmItem).IsReloading;
            PlayerUnaryOpType.IsScp => p.IsSCP,
            PlayerUnaryOpType.IsSpeaking => p.IsSpeaking,
            PlayerUnaryOpType.IsTutorial => p.IsTutorial,
            PlayerUnaryOpType.IsUsingStamina => false,                                          // No events or player flag, must be somewhere in builtin game code
            PlayerUnaryOpType.Items => p.Items.ToArray(),
            PlayerUnaryOpType.MaxAHP => p.MaxArtificialHealth,
            PlayerUnaryOpType.MaxHP => p.MaxHealth,
            PlayerUnaryOpType.MaxHumeShield => p.MaxHumeShield,
            PlayerUnaryOpType.Position => p.Position,
            PlayerUnaryOpType.Role => p.Role,
            PlayerUnaryOpType.Scale => p.GameObject.transform.localScale,
            PlayerUnaryOpType.Stamina => p.StaminaRemaining,
            PlayerUnaryOpType.UniqueRole => p.Role,                                             // UniqueRole?
                                                                                                //return p.Role.GetRoleBase().RoleName;
            PlayerUnaryOpType.Velocity => p.Velocity,
            _ => p,
        };
    }
}

[Serializable]
public class EntityUnaryOp : Value
{
    [Serializable]
    public enum EntityUnaryOpType
    {
        Name,
        Position,
        Rotation,
        Scale,
        Parent,
        IsActive,
        ChildCount,
        ToPlayer,
        ToItemPickup,
    }

    public EntityUnaryOpType Operator;
    public ScriptValue Entity;

    public override void OnValidate()
    {
        Entity.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        GameObject game = Entity.GetValue<GameObject>(args, null);
        return Operator switch
        {
            EntityUnaryOpType.ChildCount => game.transform.childCount,
            EntityUnaryOpType.IsActive => game.activeInHierarchy,
            EntityUnaryOpType.Name => game.name,
            EntityUnaryOpType.Parent => game.transform.parent,
            EntityUnaryOpType.Position => game.transform.position,
            EntityUnaryOpType.Rotation => game.transform.rotation.eulerAngles,
            EntityUnaryOpType.Scale => game.transform.localScale,
            EntityUnaryOpType.ToItemPickup => game.GetComponent<Pickup>(),
            EntityUnaryOpType.ToPlayer => Player.Get(game),
            _ => game,
        };
    }
}

[Serializable]
public class EntityBinomialOp : Value
{
    [Serializable]
    public enum EntityBinomialOpType
    {
        GetChildAt,
        WorldToLocal,
        LocalToWorld,
    }

    public EntityBinomialOpType Operator;
    public ScriptValue Entity;
    public ScriptValue Value;

    public override void OnValidate()
    {
        Entity.OnValidate();
        Value.OnValidate();
    }

    public override object GetValue(FunctionArgument args)
    {
        GameObject game = Entity.GetValue<GameObject>(args, null);
        if (game == null)
        {
            return null;
        }

        switch (Operator)
        {
            case EntityBinomialOpType.GetChildAt:
                int t = Value.GetValue(args, 0);
                return game.transform.GetChild(t);
            case EntityBinomialOpType.LocalToWorld:
                Vector3 vec = Value.GetValue(args, Vector3.zero);
                return game.transform.TransformPoint(vec);
            case EntityBinomialOpType.WorldToLocal:
                vec = Value.GetValue(args, Vector3.zero);
                return game.transform.InverseTransformPoint(vec);
        }
        return null;
    }
}

#pragma warning restore SA1401 // Field should be private