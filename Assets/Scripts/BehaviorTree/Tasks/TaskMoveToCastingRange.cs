using UnityEngine;

using BehaviorTree;

public class TaskMoveToCastingRange : Node
{
    CharacterManager manager;
    Vector3 lastMoveTowardPosition;

    public TaskMoveToCastingRange(CharacterManager manager) : base()
    {
        this.manager = manager;
        lastMoveTowardPosition = Vector3.zero;
    }

    public override NodeState Evaluate()
    {
        AbilityManager castingAbility = (AbilityManager)root.GetData("castingAbility");
        TargetData tData = (TargetData)root.GetData("castingTargetData");

        Vector3 rawPosition;
        Vector3 moveTowardPosition;
        Transform target = tData.target.transform;

        if (castingAbility.ability.targetType == TargetType.POINT || castingAbility.ability.targetType == TargetType.AREA)
        {
            moveTowardPosition = tData.location;
            rawPosition = moveTowardPosition;
        }
        else
        {
            moveTowardPosition = Utility.TargetClosestPosition(manager, target.GetComponent<UnitManager>());
            rawPosition = target.position;
        }

        if (moveTowardPosition != lastMoveTowardPosition)
        {
            manager.TryMove(moveTowardPosition);
            lastMoveTowardPosition = moveTowardPosition;
        }

        // check if the agent has moved in to range
        float d = Vector3.Distance(manager.transform.position, rawPosition);
        if (d <= castingAbility.ability.range)
        {
            Debug.Log("Moved within range. Distance: " + d + " Range: " + castingAbility.ability.range);
            root.ClearData("currentTarget");
            root.ClearData("destinationPoint");
            manager.StopMoving();
            state = NodeState.SUCCESS;
            return state;
        }

        state = NodeState.RUNNING;
        return state;
    }
}