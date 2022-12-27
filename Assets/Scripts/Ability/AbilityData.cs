using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AbilityType
{
    INSTANTIATE_CHARACTER,
    MOVE_ORDER,
    ATTACK_MOVE,
    ATTACK_ORDER,
    SPELL,
    PASSIVE,
    WARP
}

public enum TargetType
{
    NONE,
    SELF,
    OTHER,
    AREA,
    POINT
}

public enum TargetKind
{
    NONE,
    UNIT,
    CHARACTER,
    STRUCTURE
}

public enum TargetAlignment
{
    ANY,
    HOSTILE,
    FRIENDLY
}

[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability", order = 4)]
public class AbilityData : ScriptableObject
{
    public string code;
    public string abilityName;
    public string description;
    public List<ResourceValue> cost;
    public AbilityType type;
    public TargetType targetType;
    public TargetKind targetKind;
    public TargetAlignment targetAlignment;
    public UnitData unitReference;
    public float castTime;
    public float cooldown;
    public float range;
    public float targetingRadius;
    public List<string> targetAffects;
    public List<string> casterAffects;
    public List<string> projectileAffects;
    public GameObject projectilePrefab;
    public Sprite sprite;

    public bool targetRequired
    {
        get
        {
            return targetType != TargetType.SELF && targetType != TargetType.NONE;
        }
    }

    public void Trigger(GameObject source, TargetData tData = null)
    {
        // error if tData data required and missing
        if (targetRequired && tData == null)
        {
            Debug.Log("NO TARGET GIVEN FOR: " + abilityName);
            return;
        }

        UnitManager sourceManager = source.GetComponent<UnitManager>();

        // apply caster affects if there are any
        if (casterAffects.Count > 0) Affect.Apply(sourceManager, Affect.ParseAffect(casterAffects));

        switch (type)
        {
            case AbilityType.INSTANTIATE_CHARACTER:
                {
                    BoxCollider coll = source.GetComponent<BoxCollider>();

                    /*
                    Vector3 instantiationPosition = new Vector3(
                        source.transform.position.x - coll.size.x,
                        source.transform.position.y,
                        source.transform.position.z - coll.size.z
                    );
                    */

                    Vector3 instantiationPosition = source.transform.position + Vector3.back * coll.size.x;

                    CharacterData d = (CharacterData)unitReference;
                    Character c = new Character(d, sourceManager.Unit.owner);
                    //c.transform.position = instantiationPosition;
                    c.transform.GetComponent<NavMeshAgent>().Warp(instantiationPosition);

                    c.ComputeProduction();

                    // move to rally point
                    if (source.TryGetComponent<StructureManager>(out var sm))
                    {
                        c.transform.GetComponent<CharacterManager>().behaviorTree.SetDataNextFrame("destinationPoint", Utility.RandomPointOnCircleEdge(2f, sm.rallyPoint));
                    }
                }
                break;
            case AbilityType.MOVE_ORDER:
                {
                    if (source.TryGetComponent<CharacterManager>(out CharacterManager cManager))
                    {
                        cManager.behaviorTree.ClearData("attackMove");
                        cManager.behaviorTree.SetData("destinationPoint", (object)tData.location);
                    }
                }
                break;
            case AbilityType.ATTACK_ORDER:
                {
                    if (source.TryGetComponent<CharacterManager>(out CharacterManager cManager))
                    {
                        cManager.behaviorTree.ClearData("attackMove");
                        cManager.behaviorTree.SetData("currentTarget", (object)tData.target.transform);
                    }
                }
                break;
            case AbilityType.ATTACK_MOVE:
                {
                    if (source.TryGetComponent<CharacterManager>(out CharacterManager cManager))
                    {
                        cManager.behaviorTree.SetData("attackMove", (object)tData.location);
                        //cManager.behaviorTree.SetData("destinationPoint", (object)tData.location);
                    }
                }
                break;
            case AbilityType.SPELL:
                {
                    // new approach
                    switch (targetType)
                    {
                        case TargetType.SELF:
                        {
                            if (targetAffects.Count > 0) Affect.Apply(sourceManager, targetAffects);
                        }
                        break;
                        case TargetType.OTHER:
                        {
                            if (projectilePrefab != null)
                            {
                                Projectile.Spawn(projectilePrefab, source.transform.position, Quaternion.identity, Affect.ParseAffect(projectileAffects), tData.target.transform);
                            }
                            else if (tData.target.TryGetComponent<UnitManager>(out UnitManager tgt))
                            {
                                Affect.Apply(tgt, targetAffects);
                            }
                        }
                        break;
                        case TargetType.AREA:
                        {
                            if (projectilePrefab != null)
                            {
                                Projectile.Spawn(this, sourceManager, Quaternion.identity, null, tData.location);
                            }
                            else
                            {
                                Affect.Apply(tData.location, targetingRadius, Affect.ParseAffect(targetAffects), this, sourceManager);
                            }
                        }
                        break;
                        case TargetType.POINT:
                        {
                            if (projectilePrefab != null)
                            {
                                // not yet implemented
                            }
                            else
                            {
                                // not yet implemented
                            }
                        }
                        break;
                        default:
                        {
                            // not yet implemented
                        }
                        break;
                    }
                    
                    sourceManager.Unit.AddXp(1);
                }
                break;
            case AbilityType.WARP:
                {
                    Affect.Apply(sourceManager, casterAffects);
                    source.GetComponent<NavMeshAgent>().Warp(new Vector3(tData.location.x, source.transform.position.y, tData.location.z));
                }
                break;
            default:
                break;
        }
    }

    public bool CanBuy()
    {
        foreach (ResourceValue resource in cost)
            if (Game.GAME_RESOURCES[resource.code].amount < resource.amount)
                return false;
        return true;
    }
}
