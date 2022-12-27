using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffHealth : Buff
{
    int amount;
    float duration;

    public BuffHealth(int amount, float duration) : base(duration)
    {
        this.amount = amount;
        this.duration = duration;
    }

    public override void Apply(UnitManager target)
    {
        target.ChangeMaxHealth(amount);
    }

    public override void Remove(UnitManager target)
    {
        base.Remove(target);
        target.ChangeMaxHealth(-amount);
    }
}
