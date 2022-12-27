using UnityEngine;

using BehaviorTree;

public class TaskStopMoving : Node
{
    CharacterManager manager;

    public TaskStopMoving(CharacterManager manager) : base()
    {
        this.manager = manager;
    }

    public override NodeState Evaluate()
    {
        manager.StopMoving();

        state = NodeState.SUCCESS;
        return state;
    }
}