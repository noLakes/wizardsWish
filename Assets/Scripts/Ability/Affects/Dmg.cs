using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dmg : Affect
{
    private int amount;

    public Dmg(int amount)
    {
        this.amount = amount;
    }

    public override void Invoke(UnitManager target)
    {
        target?.Damage(amount);
    }
}
