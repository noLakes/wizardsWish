using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Buff : Affect
{
    float duration;

    public Buff(float duration)
    {
        this.duration = duration;
    }

    public override void Invoke(UnitManager target)
    {
        if (duration > 0) // apply temporary buff
        {
            activeRunRoutine = RunRoutine(target);
            Game.Instance.StartCoroutine(activeRunRoutine);
        }
        else // apply permanent buff
        {
            Apply(target);
        }
    }

    protected override IEnumerator RunRoutine(UnitManager target)
    {
        Apply(target);

        yield return new WaitForSeconds(duration);

        Remove(target);
    }

    public abstract void Apply(UnitManager target);
    public virtual void Remove(UnitManager target)
    {
        if (target == null || !target.Unit.isActive) return;
    }
}
