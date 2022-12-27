using UnityEngine;

using BehaviorTree;

public class TaskAttack : Node
{
    UnitManager manager;

    public TaskAttack(UnitManager manager) : base()
    {
        this.manager = manager;
    }

    public override NodeState Evaluate()
    {
        object currentTarget = root.GetData("currentTarget");
        manager.Attack((Transform) currentTarget);
        state = NodeState.SUCCESS;
        return state;
    }
}