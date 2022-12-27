using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffSpeed : Buff
{
    float amount;
    float duration;

    public BuffSpeed(float amount, float duration) : base(duration)
    {
        this.amount = amount;
        this.duration = duration;
    }

    public override void Apply(UnitManager target)
    {
        CharacterManager cTarget = (CharacterManager)target;
        cTarget.navMeshAgent.speed += amount;
    }

    public override void Remove(UnitManager target)
    {
        base.Remove(target);
        CharacterManager cTarget = (CharacterManager)target;
        if (cTarget.navMeshAgent == null) return;
        cTarget.navMeshAgent.speed -= amount;
    }
}
