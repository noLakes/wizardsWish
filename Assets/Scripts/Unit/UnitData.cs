using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unit", menuName = "Scriptable Objects/Unit", order = 1)]
public class UnitData : ScriptableObject
{
    public string code;
    public string unitName;
    public string description;
    public int health;
    public GameObject prefab;
    public float exclusiveRadius;
    
    [Header("Attack")]
    public int attackDamage;
    public float attackRange;
    public float attackRate;
    public GameObject attackProjectile = null;
    public List<string> attackOnHitAffects;

    [Header("Resources")]
    public List<ResourceValue> cost;
    public int baseGoldProduction;
    public int baseManaProduction;
    public InGameResource[] canProduce;

    public List<AbilityData> abilities = new List<AbilityData>();

    public float fieldOfView;

    public List<string> level2Buffs;
    public List<string> level3Buffs;
    public List<string> level4Buffs;
    
    public bool CanBuy()
    {
        foreach (ResourceValue resource in cost)
            if (Game.GAME_RESOURCES[resource.code].amount < resource.amount)
                return false;
        return true;
    }
}
