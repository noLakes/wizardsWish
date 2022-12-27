using BehaviorTree;

public class TaskProduceResources : Node
{
    private Unit unit;

    public TaskProduceResources(UnitManager manager) : base()
    {
        unit = manager.Unit;
    }

    public override NodeState Evaluate()
    {
        unit.ProduceResources();
        state = NodeState.SUCCESS;
        return state;
    }
}