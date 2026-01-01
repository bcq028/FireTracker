using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace T1;

/// <summary>
/// Attributes: 数值型，资源型（资源型往往伴随一个数值型的资源上限）
/// 例如攻击力就是数值，生命值就是资源，生命上限就是生命值对应的资源上限
/// </summary>
/// <param name="value"></param>
public class GameplayAttribute(float value)
{
    private float _baseValue = value;
    private float _currentValue = value;
    private readonly List<StatModifier> _modifiers = new();
    
    public void AddModifier(StatModifier mod)
    {
        _modifiers.Add(mod);
    }

    public void RemoveModifiersFromSource(object source)
    {
        _modifiers.RemoveAll(m => m.Source == source);
    }

    private float CalculateValue()
    {
        float finalValue = BaseValue;
        var overrideMod = _modifiers.FirstOrDefault(m => m.Op == ModifierOp.Override);
        if (overrideMod != null) return overrideMod.Value;

        // 2. 算 Add (加法)
        float sumAdd = 0;
        foreach (var mod in _modifiers.Where(m => m.Op == ModifierOp.Add))
            sumAdd += mod.Value;
        
        finalValue += sumAdd;

        // 3. 算 Multiply (乘法)
        foreach (var mod in _modifiers.Where(m => m.Op == ModifierOp.Multiply))
            finalValue *= mod.Value;

        return finalValue;
    }

    public float BaseValue
    {
        get => _baseValue;
        set
        {
            _baseValue = value;
            float newCurrentVal = CalculateValue();
            if (!Mathf.IsEqualApprox(_currentValue, newCurrentVal))
            {
                _currentValue = newCurrentVal;
                OnChanged?.Invoke(_currentValue, _baseValue);
            }
            _currentValue = CalculateValue();
        }
    }
    public static implicit operator float(GameplayAttribute attribute) => attribute._currentValue;
    public event Action<float, float> OnChanged;
}

public partial class AttributeSet : Node
{
    private readonly Dictionary<string, GameplayAttribute> _attributeMap = new();

    public void RegisterAttribute(string name, float baseValue)
    {
        var attr = new GameplayAttribute(baseValue);
        _attributeMap[name] = attr;
    }

    public GameplayAttribute GetAttribute(string name)
    {
        if (_attributeMap.TryGetValue(name, out var attr)) return attr;
        GD.PrintErr($"[AttributeSet] 尝试访问未注册的属性: {name}");
        return null;
    }
    
    public async void ApplyEffect(GameplayEffect spec, object source)
    {
        var validMods = spec.Modifiers
            .Select(m => (Attr: GetAttribute(m.AttrName), Info: m))
            .Where(x => x.Attr != null)
            .ToList();

        if (spec.DurationType == EffectDurationType.Instant)
        {
            foreach (var (attr, info) in validMods)
            {
                attr.BaseValue += info.Value; 
            }
            return;
        }

        if (spec.DurationType == EffectDurationType.Infinite)
        {
            
            var appliedModifiers = new List<(GameplayAttribute Attr, StatModifier Mod)>();

            foreach (var (attr, info) in validMods)
            {
                var mod = new StatModifier { Value = info.Value, Op = info.Op, Source = source };
                attr.AddModifier(mod);
                appliedModifiers.Add((attr, mod));
            }
        }
        // TODO: support Duration type
    }
}