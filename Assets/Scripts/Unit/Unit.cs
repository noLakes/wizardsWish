using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Unit
{
    protected string uid;
    public UnitData data { get; protected set; }
    public string code { get; protected set; }
    public Transform transform;
    public Transform mesh;
    public int owner { get; protected set; }
    public int health;
    public int maxHealth;
    public int attackDamage;
    public float attackRange;
    public float attackRate;
    public int level { get; protected set; }
    public int maxLevel { get; protected set; }
    public int xp { get; protected set; }
    public bool isActive { get; protected set; }
    public bool canBuy { get => data.CanBuy(); }
    public Dictionary<InGameResource, int> production;
    public bool isMaxLevel { get => level == maxLevel; }

    // ability fields
    public Dictionary<string, AbilityManager> generalAbilityManagers { get; protected set; }
    public Dictionary<string, AbilityManager> abilityManagers { get; protected set; }
    // --------------

    public Unit(UnitData initialData, int owner) : this(initialData, owner, new List<ResourceValue>() { }) { }
    public Unit(UnitData initialData, int owner, List<ResourceValue> production)
    {
        uid = System.Guid.NewGuid().ToString();
        data = initialData;
        code = data.code;
        this.owner = owner;
        health = data.health;
        maxHealth = data.health;
        attackDamage = data.attackDamage;
        attackRange = data.attackRange;
        attackRate = data.attackRate;
        level = 1;
        maxLevel = 1;
        xp = 0;

        if (data.level2Buffs.Count > 0) maxLevel++;
        if (data.level3Buffs.Count > 0) maxLevel++;
        if (data.level4Buffs.Count > 0) maxLevel++;

        this.production = production.ToDictionary(rv => rv.code, rv => rv.amount);

        GameObject g = GameObject.Instantiate(data.prefab) as GameObject;
        transform = g.transform;
        mesh = transform.Find("Mesh");
        transform.parent = Game.Instance.UNITS_CONTAINER;
        g.GetComponent<UnitManager>().Initialize(this);

        // set field of view size
        transform.Find("FOV").transform.localScale = new Vector3(data.fieldOfView * 2 + 6.5f, data.fieldOfView * 2 + 6.5f, 0f);

        // set up abilities from scriptable data
        abilityManagers = new Dictionary<string, AbilityManager>();
        AbilityManager am;
        foreach (AbilityData ability in data.abilities)
        {
            am = g.AddComponent<AbilityManager>();
            am.Initialize(ability, g);
            abilityManagers.Add(am.ability.code, am);
        }
    }

    public void SetOwner(int owner) => this.owner = owner;

    public void ClearOwner() => owner = -1;

    public virtual void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void LevelUp()
    {
        if (level == maxLevel) return;

        level += 1;

        switch (level)
        {
            case 2:
                // add level 2 buffs
                Affect.Apply(transform.GetComponent<UnitManager>(), data.level2Buffs);
                break;
            case 3:
                // add level 3 buffs
                Affect.Apply(transform.GetComponent<UnitManager>(), data.level3Buffs);
                break;
            case 4:
                // add level 4 buffs
                Affect.Apply(transform.GetComponent<UnitManager>(), data.level4Buffs);
                break;
            default:
                break;
        }

        Debug.Log($"Level up to level {level}!");
    }

    public void AddXp(int amount)
    {
        if (level == maxLevel) return;

        //Debug.Log(data.unitName + " gained " + amount + " xp");
        xp += amount;

        if (xp >= 100)
        {
            xp = 0;
            LevelUp();
        }
    }

    public void AddAbility(AbilityManager abilityManager)
    {
        if (!abilityManagers.ContainsKey(abilityManager.abilityName))
        {
            abilityManagers.Add(abilityManager.abilityName, abilityManager);
        }
    }

    public void RemoveAbility(AbilityManager abilityManager)
    {
        if (abilityManagers.ContainsKey(abilityManager.ability.code))
        {
            abilityManagers.Remove(abilityManager.ability.code);
        }
    }

    // for debug and manual/forced triggering
    public void TriggerAbility(string name, TargetData tData = null)
    {
        Debug.Log("Triggering " + name);
        if (GetAbility(name))
        {
            GetAbility(name).Trigger(tData);
        }
        else Debug.Log(code + " " + uid + " does not have the triggered ability: " + name);
    }

    public AbilityManager GetAbility(string abilityCode)
    {
        AbilityManager ability = null;

        if (generalAbilityManagers.ContainsKey(abilityCode))
        {
            ability = generalAbilityManagers[abilityCode];
        }
        else if (abilityManagers.ContainsKey(abilityCode))
        {
            ability = abilityManagers[abilityCode];
        }

        return ability;
    }

    public override string ToString()
    {
        return "{ code: " + code + ", uid: " + uid + " }";
    }

    public virtual void Place(bool pay = true)
    {
        // remove 'is trigger' from box collider to allow for collisions with units
        transform.GetComponent<BoxCollider>().isTrigger = false;

        if (pay)
        {
            // spend resources for producing this structure
            foreach (ResourceValue value in data.cost)
            {
                Game.GAME_RESOURCES[value.code].AddAmount(-value.amount);
            }
            EventManager.TriggerEvent("ResourcesChanged");
        }

        if (owner == Game.Instance.humanPlayerID)
        {
            // set FOV
            transform.GetComponent<UnitManager>().ToggleFOV(Game.Instance.gameGlobalParameters.enableFOV);
        }

        //EventManager.TriggerEvent("ProductionRateChanged");
    }

    public void ProduceResources()
    {
        foreach (KeyValuePair<InGameResource, int> resource in production)
        {
            Game.GAME_RESOURCES[resource.Key].AddAmount(resource.Value);
        }
    }

    public void Activate() => isActive = true;

    public void Deactivate(bool remove = true)
    {
        isActive = false;
        if (remove) Game.Instance.UNITS.Remove(this);
    }

    public Dictionary<InGameResource, int> ComputeProduction()
    {
        if (data.canProduce.Length == 0) return null;

        GameGlobalParameters globalParams = Game.Instance.gameGlobalParameters;
        GamePlayersParameters playerParams = Game.Instance.gamePlayersParameters;
        Vector3 pos = transform.position;

        if (data.canProduce.Contains(InGameResource.Gold))
        {
            production[InGameResource.Gold] = data.baseGoldProduction;
        }

        if (data.canProduce.Contains(InGameResource.GoldOre))
        {
            int goldOreScore =
                Physics.OverlapSphere(pos, globalParams.goldOreProductionRange, Game.GOLD_MASK)
                .Select((c) => globalParams.goldOreProductionFunc(Vector3.Distance(pos, c.transform.position)))
                .Sum();

            goldOreScore = Mathf.Min(20, goldOreScore);

            /*
            if (production.ContainsKey(InGameResource.Gold))
            {
                production[InGameResource.Gold] += goldOreScore;
            }
            
            else
            {
                production[InGameResource.Gold] = goldOreScore;
            }
            */

            production[InGameResource.Gold] = goldOreScore;
        }

        if (data.canProduce.Contains(InGameResource.Wood))
        {
            // original
            int treesScore =
                Physics.OverlapSphere(pos, globalParams.woodProductionRange, Game.TREE_MASK)
                .Select((c) => globalParams.woodProductionFunc(Vector3.Distance(pos, c.transform.position)))
                .Sum();

            treesScore = Mathf.Min(12, treesScore);

            production[InGameResource.Wood] = treesScore;

        }

        if (data.canProduce.Contains(InGameResource.Stone))
        {
            int stoneScore =
                Physics.OverlapSphere(pos, globalParams.woodProductionRange, Game.STONE_MASK)
                .Select((c) => globalParams.stoneProductionFunc(Vector3.Distance(pos, c.transform.position)))
                .Sum();

            stoneScore = Mathf.Min(10, stoneScore);

            production[InGameResource.Stone] = stoneScore;
        }

        if (data.canProduce.Contains(InGameResource.Mana))
        {
            production[InGameResource.Mana] = data.baseManaProduction;
        }

        return production;
    }

    public void IncreaseProductionRate(InGameResource resource, int amount)
    {
        if (data.canProduce.Contains(resource))
        {
            if (production.ContainsKey(resource))
            {
                production[resource] += amount;
            }
            else
            {
                production[resource] = amount;
            }

            EventManager.TriggerEvent("ProductionRateChanged");
        }
    }
}
