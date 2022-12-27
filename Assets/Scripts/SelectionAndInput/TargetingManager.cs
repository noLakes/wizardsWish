using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetData
{
    public Vector3 location { get; }
    public GameObject target { get; }

    public TargetData(Vector3 location, GameObject target = null)
    {
        this.location = location;
        this.target = target;
    }
}

public class TargetingManager : MonoBehaviour
{
    private SelectionManager selectionManager;
    public AbilityData targetingAbility { get; private set; }
    public bool targeting { get; private set; }

    public GameObject AreaTargetingPrefab;

    private void Awake()
    {
        selectionManager = GetComponent<SelectionManager>();
        targetingAbility = null;
        targeting = false;
    }

    public void SetTargetingAbility(AbilityData ability)
    {
        targeting = true;
        EventManager.TriggerEvent("TargetingStarted");
        targetingAbility = ability;

        GameObject targetPreview = GameObject.Instantiate(AreaTargetingPrefab);
        AreaTargetingPreview p = targetPreview.GetComponent<AreaTargetingPreview>();

        if (targetingAbility.targetType.ToString().Contains("AREA"))
        {
            //GameObject targetPreview = GameObject.Instantiate(AreaTargetingPrefab);
            p.SetRadius(targetingAbility.targetingRadius);
        }
        else
        {
            p.SetRadius(0.2f);
        }

        switch (ability.targetAlignment)
        {
            case TargetAlignment.ANY:
                p.SetColor(Color.yellow);
                break;
            case TargetAlignment.HOSTILE:
                p.SetColor(Color.red);
                break;
            case TargetAlignment.FRIENDLY:
                p.SetColor(Color.green);
                break;
        }
    }

    public void ClearTargetingAbility()
    {
        targetingAbility = null;
        targeting = false;
    }

    // receive target data and push to abilities awaiting targets
    private void OnTargetDataSent(object data)
    {
        if (targetingAbility.cost.Count > 0)
        {
            //Debug.Log("Casting ability with cost");
            Game.SELECTED_UNITS[0].TryCastAbility(targetingAbility.code, (TargetData)data);
        }
        else
        {
            //Debug.Log("Casting ability with NO cost");
            foreach (UnitManager unit in Game.SELECTED_UNITS)
            {
                unit.TryCastAbility(targetingAbility.code, (TargetData)data);
            }
        }

        ClearTargetingAbility();
    }

    private void OnCancelTargeting()
    {
        ClearTargetingAbility();
    }

    private void OnEnable()
    {
        EventManager.AddListener("TargetDataSent", OnTargetDataSent);
        EventManager.AddListener("CancelTargeting", OnCancelTargeting);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("TargetDataSent", OnTargetDataSent);
        EventManager.RemoveListener("CancelTargeting", OnCancelTargeting);
    }
}
