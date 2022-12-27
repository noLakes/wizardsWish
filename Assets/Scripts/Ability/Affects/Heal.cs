using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heal : Affect
{
    private int amount;

    public Heal(int amount)
    {
        this.amount = amount;
    }

    public override void Invoke(UnitManager target)
    {
        target?.Heal(amount);
    }
}
