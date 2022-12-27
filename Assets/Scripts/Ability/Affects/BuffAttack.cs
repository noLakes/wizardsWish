using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffAttack : Buff
{
    int amount;
    float duration;

    public BuffAttack(int amount, float duration) : base(duration)
    {
        this.amount = amount;
        this.duration = duration;
    }

    public override void Apply(UnitManager target)
    {
        target.ChangeAttack(amount);
    }

    public override void Remove(UnitManager target)
    {
        base.Remove(target);
        target.ChangeAttack(-amount);
    }
}
