using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class StructurePlacer : MonoBehaviour
{
    private Structure placedStructure = null;

    private Ray ray;
    private RaycastHit raycastHit;
    private Vector3 lastPlacementPosition;

    public bool activePlacing { get => placedStructure != null; }

    private void Awake()
    {
        placedStructure = new Structure(
            Game.Instance.gameGlobalParameters.inititalStructure,
            Game.Instance.humanPlayerID
            );
        placedStructure.SetPosition(Game.Instance.startPosition);
        // link the data into the manager
        placedStructure.transform.GetComponent<StructureManager>().Initialize(placedStructure);
        Game.Instance.keyStructure = placedStructure.transform.GetComponent<StructureManager>();
        PlaceStructure(false, false);
        
        
        // make sure we have no building selected when the player starts
        // to play
        CancelPlacedStructure();
    }

    void Update()
    {
        if (Game.Instance.gameIsPaused) return;

        if (placedStructure != null)
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                CancelPlacedStructure();
                return;
            }

            ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out raycastHit, 1000f, Game.TERRAIN_MASK))
            {
                placedStructure.SetPosition(raycastHit.point);

                if (lastPlacementPosition != raycastHit.point)
                {
                    placedStructure.CheckValidPlacement();
                }

                if (Random.Range(0, 3) == 0)
                {
                    placedStructure.ComputeProduction();
                    EventManager.TriggerEvent("UpdatePlacedStructureProduction", new object[] { placedStructure.production, raycastHit.point });
                }

                lastPlacementPosition = raycastHit.point;
            }

            if (Input.GetMouseButtonDown(0) && placedStructure.HasValidPlacement && !EventSystem.current.IsPointerOverGameObject())
            {
                //Debug.Log("PLACING");
                PlaceStructure();
            }
        }
    }

    public void SelectPlacedStructure(int structureDataIndex)
    {
        PreparePlacedStructure(structureDataIndex);
    }

    void PreparePlacedStructure(int structureDataIndex)
    {
        EventManager.TriggerEvent("PlaceStructureOn");
        EventManager.TriggerEvent("CancelTargeting");
        // destroy the previous "phantom" if there is one
        if (placedStructure != null && !placedStructure.isFixed) Destroy(placedStructure.transform.gameObject);

        Structure structure = new Structure(Game.STRUCTURE_DATA[structureDataIndex], Game.Instance.humanPlayerID);

        // link the data to the manager
        structure.transform.GetComponent<StructureManager>().Initialize(structure);

        placedStructure = structure;
        lastPlacementPosition = Vector3.zero;
    }

    public void CancelPlacedStructure()
    {
        EventManager.TriggerEvent("PlaceStructureOff");

        if (placedStructure != null)
        {
            // destroy the "phantom" Structure
            placedStructure.Deactivate();
            Destroy(placedStructure.transform.gameObject);
            placedStructure = null;
        }
    }

    private void PlaceStructure(bool prepareAnother = true, bool pay = true)
    {
        placedStructure.ComputeProduction();

        placedStructure.Place(pay);

        if (prepareAnother)
        {
            // check if another of the same type can be built
            if (placedStructure.canBuy)
            {
                PreparePlacedStructure(placedStructure.DataIndex);
            }
            else
            {
                placedStructure = null;
                EventManager.TriggerEvent("PlaceStructureOff");
            }
        }
        else
        {
            placedStructure = null;
            EventManager.TriggerEvent("PlaceStructureOff");
        }


        // update ui after spending resources
        EventManager.TriggerEvent("ResourcesChanged");

        // update production text
        EventManager.TriggerEvent("ProductionRateChanged");
    }

}
