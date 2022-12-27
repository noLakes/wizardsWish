using UnityEngine;
using System.Collections.Generic;
using BehaviorTree;

public class TaskProduceBuildOrder : Node
{
    StructureManager manager;

    public TaskProduceBuildOrder(StructureManager manager) : base()
    {
        this.manager = manager;
    }

    public override NodeState Evaluate()
    {
        AbilityManager activeBuildOrder = (AbilityManager)root.GetData("activeBuildOrder");

        activeBuildOrder.Trigger();

        root.ClearData("activeBuildOrder");

        // load next build order if it exists
        List<AbilityManager> buildQueueList = (List<AbilityManager>)root.GetData("buildQueue");
        buildQueueList.RemoveAt(0);

        EventManager.TriggerEvent("BuildQueueUpdated", buildQueueList);
        
        state = NodeState.SUCCESS;
        return state;
    }
}