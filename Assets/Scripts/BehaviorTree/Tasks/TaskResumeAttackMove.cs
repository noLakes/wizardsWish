using UnityEngine;

using BehaviorTree;

public class TaskResumeAttackMove : Node
{
    public override NodeState Evaluate()
    {   
        object attackMoveDestination = root.GetData("attackMove");

        root.SetData("destinationPoint", attackMoveDestination);

        state = NodeState.SUCCESS;
        return state;
    }
}
