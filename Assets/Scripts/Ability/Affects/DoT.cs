using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoT : Affect
{
    int damage;
    float tick;
    int count;

    public DoT(int damage, float tick, int count)
    {
        this.damage = damage;
        this.tick = tick;
        this.count = count;
    }

    protected override IEnumerator RunRoutine(UnitManager target)
    {
        int tickCount = 0;

        while (tickCount < count)
        {
            target?.Damage(damage);
            yield return new WaitForSeconds(tick);
            tickCount++;
        }
    }
}
