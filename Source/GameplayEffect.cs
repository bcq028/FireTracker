using System.Collections.Generic;

namespace T1;

public enum EffectDurationType
{
    Instant,
    Duration,
    Infinite
}

public enum ModifierOp
{
    Add,
    Multiply,
    Override
}

public class StatModifier
{
    public float Value;
    public ModifierOp Op;
    public object Source;
}

public class GameplayEffect
{
    public EffectDurationType DurationType = EffectDurationType.Instant;
    public float Duration;
    public List<(string AttrName, float Value, ModifierOp Op)> Modifiers = new();
}