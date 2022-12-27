using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResource
{
    public string name { get; private set; }
    public int amount { get; private set; }

    public GameResource(string name, int initialAmount)
    {
        this.name = name;
        this.amount = initialAmount;
    }

    public void AddAmount(int value)
    {
        amount += value;
        if (amount < 0) amount = 0;
    }
}