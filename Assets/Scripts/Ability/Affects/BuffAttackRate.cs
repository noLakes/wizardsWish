using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffAttackRate : Buff
{
    float amount;
    float duration;

    public BuffAttackRate(float amount, float duration) : base(duration)
    {
        this.amount = amount;
        this.duration = duration;
    }

    public override void Apply(UnitManager target)
    {
        target.ChangeAttackRate(amount);
    }

    public override void Remove(UnitManager target)
    {
        base.Remove(target);
        target.ChangeAttackRate(-amount);
    }
}
