using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Affect
{
    protected IEnumerator activeRunRoutine;

    public virtual void Invoke(UnitManager target)
    {
        activeRunRoutine = RunRoutine(target);
        Game.Instance.StartCoroutine(activeRunRoutine);
    }

    protected virtual IEnumerator RunRoutine(UnitManager target)
    {
        yield return null;
    }

    public static Affect ParseAffect(string affectSeed)
    {
        string[] seed = affectSeed.Split("/");

        Affect affect = seed[0] switch
        {
            "dmg" => new Dmg(int.Parse(seed[1])),
            "dot" => new DoT(int.Parse(seed[1]), float.Parse(seed[2]), int.Parse(seed[3])),
            "heal" => new Heal(int.Parse(seed[1])),
            "health+" => new BuffHealth(int.Parse(seed[1]), float.Parse(seed[2])),
            "attack+" => new BuffAttack(int.Parse(seed[1]), float.Parse(seed[2])),
            "attackRate+" => new BuffAttackRate(float.Parse(seed[1]), float.Parse(seed[2])),
            "range+" => new BuffRange(float.Parse(seed[1]), float.Parse(seed[2])),
            "speed+" => new BuffSpeed(float.Parse(seed[1]), float.Parse(seed[2])),
            _ => new Affect()
        };

        return affect;
    }

    public static List<Affect> ParseAffect(List<string> affectSeeds)
    {
        List<Affect> parsedAffects = new List<Affect>();

        foreach (string seed in affectSeeds) parsedAffects.Add(ParseAffect(seed));

        return parsedAffects;
    }

    public static void Apply(UnitManager target, Affect affect)
    {
        affect.Invoke(target);
    }

    public static void Apply(UnitManager target, List<Affect> affects)
    {
        foreach (Affect affect in affects) Affect.Apply(target, affect);
    }

    public static void Apply(Vector3 location, float aoeRadius, List<Affect> affects, AbilityData ability, UnitManager caster)
    {
        Collider[] hitColliders = Physics.OverlapSphere(location, aoeRadius, Game.ALL_UNIT_MASK);

        foreach(Collider c in hitColliders)
        {
            if(c.gameObject.TryGetComponent<UnitManager>(out UnitManager unit))
            {
                // check for correct target type before applying!
                if (AbilityManager.ValidTargetForAbility(caster, unit, ability))
                {
                    foreach(Affect a in affects) a.Invoke(unit);
                }
            }
        }
    }

    public static void Apply(UnitManager target, List<string> affectSeeds)
    {
        foreach (string affectSeed in affectSeeds)
        {
            Debug.Log("Applying: " + affectSeed + " to " + target.Unit.code);
            ParseAffect(affectSeed).Invoke(target);
        }
    }
}
