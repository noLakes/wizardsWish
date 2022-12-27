using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffRange : Buff
{
    float amount;
    float duration;

    public BuffRange(float amount, float duration) : base(duration)
    {
        this.amount = amount;
        this.duration = duration;
    }

    public override void Apply(UnitManager target)
    {
        target.Unit.attackRange += amount;
    }

    public override void Remove(UnitManager target)
    {
        base.Remove(target);
        target.Unit.attackRange -= amount;
    }
}
