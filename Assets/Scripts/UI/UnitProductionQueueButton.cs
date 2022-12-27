using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitProductionQueueButton : MonoBehaviour
{   
    public void RemoveProductionItemFromQueue()
    {
        int index = 0;

        foreach(Transform child in transform.parent)
        {
            if(child == this.transform)
            {
                break;
            }

            index ++;
        }

        EventManager.TriggerEvent("RemoveBuildQueueItem", index);
    }
}
