using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilityManager : MonoBehaviour
{
    public AbilityData ability;
    GameObject source;
    public UnitManager unitManager { get; private set; }
    Button button;
    public bool ready;
    public bool targetRequired { get => ability.targetRequired; }
    public bool hasRange { get => ability.range > 0; }
    public string abilityName { get => ability.abilityName; }
    public List<ResourceValue> cost { get => ability.cost; }
    public bool CanBuy { get => ability.CanBuy(); }
    private IEnumerator activeCooldownRoutine = null;

    public void Initialize(AbilityData ability, GameObject source)
    {
        this.ability = ability;
        this.source = source;
        unitManager = GetComponent<UnitManager>();
        ready = true;

        if (ability.type == AbilityType.INSTANTIATE_CHARACTER)
        {
            ability.cost = ability.unitReference.cost;
        }
    }

    public void Trigger(TargetData tData = null)
    {
        if (!ready)
        {
            return;
        }
        ability.Trigger(source, tData);
        activeCooldownRoutine = CooldownRoutine(tData);
        StartCoroutine(activeCooldownRoutine);
    }

    public void SetButton(Button button)
    {
        this.button = button;
        button.interactable = ready;
    }

    private IEnumerator CooldownRoutine(TargetData tData)
    {
        SetReady(false);
        yield return new WaitForSeconds(ability.cooldown);
        SetReady(true);
        activeCooldownRoutine = null;
    }

    private void SetReady(bool ready)
    {
        this.ready = ready;
        if (button != null) button.interactable = ready;
    }

    public bool ValidTargetData(TargetData tData)
    {
        bool validKind = true;
        bool validAlignment = true;
        bool validType = true;

        if (ability.targetType == TargetType.AREA) return validKind && validAlignment && validType;

        // checks if unit was targeted
        if(ability.targetType == TargetType.OTHER)
        {
            validType = tData.target != null && tData.target.GetComponent<UnitManager>() != null;
        }

        if (ability.targetAlignment == TargetAlignment.HOSTILE)
        {
            validAlignment = !unitManager.IsFriendly(tData.target);
        }
        else if (ability.targetAlignment == TargetAlignment.FRIENDLY)
        {
            validAlignment = unitManager.IsFriendly(tData.target);
        }

        if (ability.targetKind == TargetKind.CHARACTER)
        {
            //validKind = tData.target.GetComponent<UnitManager>().Unit is Character;
            
            /*
            if(tData.target.TryGetComponent<UnitManager>(out UnitManager uMan))
            {
                validKind = uMan.Unit is Character;
            }
            else validKind = false;
            */

            UnitManager uMan = tData.target.GetComponent<UnitManager>();
            validKind = uMan != null && uMan.Unit is Character;
        }
        else if (ability.targetKind == TargetKind.STRUCTURE)
        {
            //validKind = tData.target.GetComponent<UnitManager>().Unit is Structure;

            UnitManager uMan = tData.target.GetComponent<UnitManager>();
            validKind = uMan != null && uMan.Unit is Structure;
        }

        return validKind && validAlignment && validType;
    }

    public bool InCastingRange(TargetData tData)
    {
        bool inRange = true;

        if (ability.targetRequired)
        {
            if (ability.targetType == TargetType.POINT || ability.targetType == TargetType.AREA)
            {
                // check distance between this and point
                inRange = Vector3.Distance(transform.position, tData.location) <= ability.range;
            }
            else
            {
                // check distance between this and target
                float d = Utility.CalcUnitTargetDistance(unitManager, tData.target.GetComponent<UnitManager>());
                inRange = d <= ability.range;
            }
        }

        return inRange;
    }

    public static bool ValidTargetForAbility(UnitManager caster, UnitManager target, AbilityData ability)
    {
        bool validKind = true;
        bool validAlignment = true;

        if (ability.targetAlignment == TargetAlignment.HOSTILE)
        {
            validAlignment = !caster.IsFriendly(target);
        }
        else if (ability.targetAlignment == TargetAlignment.FRIENDLY)
        {
            validAlignment = caster.IsFriendly(target);
        }

        if (ability.targetKind == TargetKind.CHARACTER)
        {
            validKind = target.GetComponent<UnitManager>().Unit is Character;
        }
        else if (ability.targetKind == TargetKind.STRUCTURE)
        {
            validKind = target.GetComponent<UnitManager>().Unit is Structure;
        }

        return validKind && validAlignment;
    }
}
