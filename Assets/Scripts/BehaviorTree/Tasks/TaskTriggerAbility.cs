using UnityEngine;

using BehaviorTree;

public class TaskTriggerAbility : Node
{
    UnitManager manager;

    public TaskTriggerAbility(UnitManager manager) : base()
    {
        this.manager = manager;
    }

    public override NodeState Evaluate()
    {
        AbilityManager castingAbility = (AbilityManager)root.GetData("castingAbility");
        TargetData castingTargetData = (TargetData)root.GetData("castingTargetData");

        castingAbility.Trigger(castingTargetData);
        Debug.Log("Triggered ability from TaskTrigger");
        // clear casting variables to break out of casting tree
        root.ClearData("castingAbility");
        root.ClearData("castingTargetData");
        
        state = NodeState.SUCCESS;
        return state;
    }
}