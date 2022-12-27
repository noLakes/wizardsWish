using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : Unit
{
    public Character(CharacterData initialData, int owner) : this(initialData, owner, new List<ResourceValue>() { }) { }
    public Character(CharacterData initialData, int owner, List<ResourceValue> production) : base(initialData, owner, production)
    {
        // set up general character ability managers
        generalAbilityManagers = new Dictionary<string, AbilityManager>();
        AbilityManager am; GameObject g = transform.gameObject;
        foreach (AbilityData ability in Game.GENERAL_CHARACTER_ABILITY_DATA)
        {
            am = g.AddComponent<AbilityManager>();
            am.Initialize(ability, g);
            generalAbilityManagers.Add(am.ability.code , am);
        }
        
        Game.Instance.UNITS.Add(this);
    }

    public override void SetPosition(Vector3 position)
    {
        transform.GetComponent<CharacterManager>().navMeshAgent.Warp(position);
    }
}
