using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum OrderType
{
    Attack,
    Move,
    Rally
}

public class OrderManager : MonoBehaviour
{
    public GameObject indicatorPrefab;
    private SelectionManager selectionManager;

    // tracks hit objects
    RaycastHit hit;

    private Vector3 p1;

    void Start()
    {
        selectionManager = GetComponent<SelectionManager>();
    }

    void Update()
    {
        if (Game.Instance.gameIsPaused) return;
        //if (EventSystem.current.IsPointerOverGameObject()) return;

        // when right click initiated
        if (Input.GetMouseButtonDown(1))
        {
            p1 = Input.mousePosition;
        }

        // when right click ends
        if (Input.GetMouseButtonUp(1) && selectionManager.orderState != OrderState.None && Game.SELECTED_UNITS.Count > 0)
        {
            Ray ray = Camera.main.ScreenPointToRay(p1);

            if (Physics.Raycast(ray, out hit, 50000.0f, ~((1 << 2) | (1 << 9) | (1 << 10) | (1 << 11))))
            {

                GameObject hitObject = hit.transform.gameObject;
                //Debug.Log("Right clicked object: " + hitObject.name);

                if (selectionManager.orderState == OrderState.Movement)
                {
                    // handle right click unit
                    if (hitObject.TryGetComponent<CharacterManager>(out CharacterManager character))
                    {
                        // if unit not owned by player
                        if (!character.IsPlayerOwned() && character.Unit.isActive)
                        {
                            PlaceIndicator(character.transform.position, 1f, OrderType.Attack);
                            IssueAttackOrder(character.transform);
                        }
                        else
                        {
                            // handle right clicking owned character
                        }
                    }
                    else if (hitObject.TryGetComponent<StructureManager>(out StructureManager structure))
                    {
                        //Debug.Log("Right clicked a structure");
                        // if unit not owned by player
                        if (!structure.IsPlayerOwned() && structure.Unit.isActive)
                        {
                            PlaceIndicator(structure.transform.position, 1f, OrderType.Attack);
                            IssueAttackOrder(structure.transform);
                        }
                        else
                        {
                            // handle right clicking owned structure
                        }
                    }
                    else
                    {
                        // handle terrain/move order
                        //Debug.Log("Right clicked terrain or something else.");
                        Vector3 target = hit.point;

                        if (Game.SELECTED_UNITS.Count > 1)
                        {
                            //Debug.Log("Moving Multiple");
                            MoveMultipleCharacters(target, new List<UnitManager>(Game.SELECTED_UNITS));
                        }
                        else
                        {
                            UnitManager unitManager = Game.SELECTED_UNITS[0];
                            if (unitManager.IsPlayerOwned())
                            {
                                MoveSingleCharacter(target, Game.SELECTED_UNITS[0]);
                            }
                        }
                        PlaceIndicator(target, 1f, OrderType.Move);
                    }
                }
            }
            else //if we didnt hit something
            {
                // do nothing
            }
        }
    }

    public static void MoveSingleCharacter(Vector3 location, UnitManager unit)
    {
        if (unit.TryGetComponent<CharacterManager>(out CharacterManager character))
        {
            character.behaviorTree.StopCasting();
            character.behaviorTree.StopAttacking();
            character.behaviorTree.ClearData("attackMove");
            character.behaviorTree.SetData("destinationPoint", (object)location);
            character.behaviorTree.Wake();
            character.behaviorTree.SetDirty(true);
        }
        else if (unit.TryGetComponent<StructureManager>(out StructureManager structure))
        {
            //Debug.Log("Moving structure");
            structure.SetRallyPoint(location);
        }
        else
        {
            Debug.Log("Unable to move: " + unit.gameObject.name);
        }
    }

    public static void AttackMoveSingleCharacter(Vector3 location, UnitManager unit)
    {
        if (unit.TryGetComponent<CharacterManager>(out CharacterManager character))
        {
            character.behaviorTree.StopCasting();
            character.behaviorTree.StopAttacking();
            character.StopMoving();
            character.behaviorTree.SetData("attackMove", (object)location);
            character.behaviorTree.Wake();
            character.behaviorTree.SetDirty(true);
        }
        else
        {
            Debug.Log("Unable to attack move: " + unit.gameObject.name);
        }
    }

    public static void AttackMoveMultipleCharacters(Vector3 rootPos, List<UnitManager> characters)
    {
        Vector3[,] positions = SquareFormation.BuildCenteredPositionArray(Game.SELECTED_UNITS.Count, rootPos, 2f);

        int unitCounter = 0;

        for (int i = 0; i < positions.GetLength(0); i++)
        {
            for (int j = 0; j < positions.GetLength(1) && unitCounter < characters.Count; j++)
            {
                UnitManager selectedUnit = characters[unitCounter];

                if (selectedUnit.TryGetComponent<CharacterManager>(out CharacterManager c))
                {
                    c.behaviorTree.StopCasting();
                    c.behaviorTree.StopAttacking();
                    c.StopMoving();
                    c.behaviorTree.SetData("attackMove", (object)positions[i, j]);
                    c.behaviorTree.Wake();
                    c.behaviorTree.SetDirty(true);
                }
                unitCounter++;
            }
        }
    }

    public static void MoveMultipleCharacters(Vector3 rootPos, List<UnitManager> characters)
    {
        Vector3[,] positions = SquareFormation.BuildCenteredPositionArray(Game.SELECTED_UNITS.Count, rootPos, 2f);

        int unitCounter = 0;

        for (int i = 0; i < positions.GetLength(0); i++)
        {
            for (int j = 0; j < positions.GetLength(1) && unitCounter < characters.Count; j++)
            {
                UnitManager selectedUnit = characters[unitCounter];

                if (selectedUnit.TryGetComponent<CharacterManager>(out CharacterManager c))
                {
                    c.behaviorTree.StopCasting();
                    c.behaviorTree.StopAttacking();
                    c.behaviorTree.ClearData("attackMove");
                    c.behaviorTree.SetData("destinationPoint", (object)positions[i, j]);
                    c.behaviorTree.Wake();
                    c.behaviorTree.SetDirty(true);
                }
                unitCounter++;
            }
        }
    }

    public static void IssueAttackOrder(Transform target)
    {
        foreach (UnitManager selected in Game.SELECTED_UNITS)
        {
            if (selected.GetComponent<CharacterBT>() != null && selected.IsPlayerOwned())
            {
                //Debug.Log("ATTACK ORDER ISSUED");
                CharacterManager cManager = (CharacterManager)selected;
                CharacterBT bt = cManager.GetComponent<CharacterBT>();
                cManager.Stop();
                bt.ClearAllData();
                bt.SetData("currentTarget", target);
                bt.Wake();
                bt.SetDirty(true);
            }
        }
    }

    private void PlaceIndicator(Vector3 location, float duration, OrderType type)
    {
        EventManager.TriggerEvent("IndicatorPlaced");
        GameObject g = GameObject.Instantiate(indicatorPrefab, location, Quaternion.identity);

        Color color = Color.magenta;
        switch (type)
        {
            case OrderType.Attack:
                color = Color.red;
                break;
            case OrderType.Move:
                color = Color.green;
                break;
            case OrderType.Rally:
                color = Color.blue;
                break;
        }

        g.GetComponent<Indicator>().Initialize(duration, color);
    }
}