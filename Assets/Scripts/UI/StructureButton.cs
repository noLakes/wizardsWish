using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class StructureButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private StructureData structureData;

    public void Initialize(StructureData structureData)
    {
        this.structureData = structureData;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EventManager.TriggerEvent("HoverStructureButton", structureData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        EventManager.TriggerEvent("UnhoverStructureButton");
    }
}

