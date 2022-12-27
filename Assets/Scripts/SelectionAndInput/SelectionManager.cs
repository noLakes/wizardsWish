using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    // control group dict
    private Dictionary<int, List<UnitManager>> controlGroup = new Dictionary<int, List<UnitManager>>();

    // tracks hit objects
    RaycastHit hit;

    // tracks if we are currently drag selecting
    bool dragSelect;

    // mouse positions for selection and drag box corner
    private Vector3 p1;

    public RectTransform selectionBox;

    public AimState aimState { get; private set; }
    public OrderState orderState { get; private set; }

    private bool hoveringUI;

    void Start()
    {
        dragSelect = false;
        aimState = AimState.Selecting;
        orderState = OrderState.None;
        hoveringUI = EventSystem.current.IsPointerOverGameObject();
    }

    void Update()
    {
        if (Game.Instance.gameIsPaused) return;

        hoveringUI = EventSystem.current.IsPointerOverGameObject();

        //1. when left mouse button clicked (but not released)
        if (Input.GetMouseButtonDown(0))
        {
            p1 = Input.mousePosition;
        }

        //2. while left mouse button held
        if (Input.GetMouseButton(0) && aimState == AimState.Selecting)
        {
            if ((p1 - Input.mousePosition).magnitude > 40)
            {
                dragSelect = true;
                UpdateSelectionBox(Input.mousePosition);
            }
        }

        //3. when mouse button comes up
        if (Input.GetMouseButtonUp(0))
        {
            // shoot ray to world position from camera
            Ray ray = Camera.main.ScreenPointToRay(p1);

            // handle selecting actions
            if (aimState == AimState.Selecting)
            {
                if (dragSelect == false && !hoveringUI) //single select
                {
                    if (Physics.Raycast(ray, out hit, 50000.0f, Game.ALL_UNIT_MASK))
                    {
                        GameObject target = hit.transform.gameObject;
                        UnitManager targetUnit = target.GetComponent<UnitManager>();

                        if (Input.GetKey(KeyCode.LeftShift)) //inclusive select
                        {
                            if (targetUnit != null && targetUnit.IsPlayerOwned())
                            {
                                DeselectEnemy();
                                Select(target);
                            }
                        }
                        else //exclusive selected
                        {
                            DeselectAll();
                            if (targetUnit != null)
                            {
                                Select(target);
                            }
                        }
                    }
                    else //if we didnt hit something
                    {
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            //do nothing
                        }
                        else
                        {
                            DeselectAll();
                        }
                    }
                }
                else if (dragSelect) //select multiple
                {
                    // deselect all if not holding shift
                    if (!Input.GetKey(KeyCode.LeftShift))
                    {
                        DeselectAll();
                    }

                    Vector2 min = selectionBox.anchoredPosition - (selectionBox.sizeDelta / 2);
                    Vector2 max = selectionBox.anchoredPosition + (selectionBox.sizeDelta / 2);

                    foreach (Unit unit in Game.Instance.UNITS)
                    {
                        if (!unit.isActive) continue;
                        if (unit.owner != Game.Instance.humanPlayerID) continue;

                        Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);

                        if (screenPos.x > min.x && screenPos.x < max.x && screenPos.y > min.y && screenPos.y < max.y)
                        {
                            Select(unit.transform.gameObject);
                        }
                    }
                }

                selectionBox.gameObject.SetActive(false);
                dragSelect = false;
            }
            // handle targeting
            else if (aimState == AimState.Targeting && !hoveringUI)
            {
                if (Physics.Raycast(ray, out hit, 50000.0f, ~((1 << 2) | (1 << 9) | (1 << 10) | (1 << 11))))
                {
                    // generate target data
                    TargetData targetData = new TargetData(hit.point, hit.transform.gameObject);
                    // send data to event listeners (abilities awaiting targets...)
                    EventManager.TriggerEvent("TargetDataSent", targetData);
                    //Debug.Log("Target sent: " + targetData.location + " // " + targetData.target.name);
                }
                else
                {
                    SetAimState(AimState.Selecting);
                }

            }
        }

        // control group check
        if (Input.anyKeyDown)
        {
            int alphaKey = Utility.GetAlphaKeyValue(Input.inputString);
            //Debug.Log(Input.inputString);
            //Debug.Log(alphaKey);
            if (alphaKey != -1)
            {
                //Debug.Log("AlphaKey detected: " + alphaKey);
                if (
                    Input.GetKey(KeyCode.LeftControl) ||
                    Input.GetKey(KeyCode.RightControl) ||
                    Input.GetKey(KeyCode.LeftApple) ||
                    Input.GetKey(KeyCode.RightApple)
                )
                {
                    CreateControlGroup(alphaKey);
                }
                else
                {
                    SelectControlGroup(alphaKey);
                }

            }
        }
    }

    private void UpdateSelectionBox(Vector3 currentMousePosition)
    {
        if (!selectionBox.gameObject.activeInHierarchy)
        {
            selectionBox.gameObject.SetActive(true);
        }
        float width = currentMousePosition.x - p1.x;
        float height = currentMousePosition.y - p1.y;

        selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        selectionBox.anchoredPosition = p1 + new Vector3(width / 2, height / 2, 0);
    }

    //---------------------------------------------
    // selection logic
    // interacts with Game global select list

    private void Select(UnitManager unit)
    {
        // deselect all if selecting a structure
        if (unit.Unit is Structure)
        {
            DeselectAll();
            SetOrderState(OrderState.Movement);
        }
        // deslect all structures if selecting a unit
        else if (unit.Unit is Character)
        {
            DeselectType<Structure>();
        }

        if (!Game.SELECTED_UNITS.Contains(unit))
        {
            Game.SELECTED_UNITS.Add(unit);
            unit.Select();
            SetOrderState(OrderState.Movement);
        }
    }

    private void Select(GameObject target)
    {
        if (target.TryGetComponent<UnitManager>(out UnitManager manager) && manager.Unit.isActive) Select(manager);
        //else Debug.Log("Tried to select non unit: " + target.name);
    }

    private void Deselect(UnitManager unit)
    {
        Game.SELECTED_UNITS.Remove(unit);
        unit.Deselect();
    }

    private void DeselectEnemy()
    {
        for (int i = Game.SELECTED_UNITS.Count; i > 0; i--)
        {
            UnitManager unit = Game.SELECTED_UNITS[i - 1];
            if (!unit.IsPlayerOwned())
            {
                Deselect(unit);
            }
        }
    }

    private bool IsSelected(UnitManager unit)
    {
        return Game.SELECTED_UNITS.Contains(unit);
    }

    private void DeselectType<T>()
    {
        for (int i = Game.SELECTED_UNITS.Count; i > 0; i--)
        {
            UnitManager unit = Game.SELECTED_UNITS[i - 1];
            if (unit.Unit is T)
            {
                Deselect(unit);
            }
        }
    }

    public void DeselectAll()
    {

        List<UnitManager> temp = new List<UnitManager>(Game.SELECTED_UNITS);
        Game.SELECTED_UNITS.Clear();

        foreach (UnitManager unit in temp)
        {
            unit.Deselect();
        }

        SetOrderState(OrderState.None);

        EventManager.TriggerEvent("CancelTargeting");
    }

    // does the selection list contain only one unit type?
    public static bool UniqueSelection()
    {
        if (Game.SELECTED_UNITS.Count == 0) return false;
        if (Game.SELECTED_UNITS.Count == 1) return true;

        string firstUnitCode = Game.SELECTED_UNITS[0].Unit.code;

        for (int i = 1; i < Game.SELECTED_UNITS.Count; i++)
        {
            if (Game.SELECTED_UNITS[i].Unit.code != firstUnitCode) return false;
        }

        return true;
    }

    public void SetAimState(AimState state)
    {
        aimState = state;
    }

    public void SetOrderState(OrderState state)
    {
        orderState = state;
    }

    private void CreateControlGroup(int groupNumber)
    {
        Debug.Log("Creating control group: " + groupNumber);
        if (Game.SELECTED_UNITS.Count == 0)
        {
            if (controlGroup.ContainsKey(groupNumber))
            {
                RemoveControlGroup(groupNumber);
                return;
            }
            List<UnitManager> newGroup = new List<UnitManager>(Game.SELECTED_UNITS);
            controlGroup[groupNumber] = newGroup;
        }
    }

    private void SelectControlGroup(int groupNumber)
    {
        if (controlGroup.ContainsKey(groupNumber))
        {
            Debug.Log("Selecting control group: " + groupNumber);
            DeselectAll();
            foreach (UnitManager unitManager in controlGroup[groupNumber])
            {
                Select(unitManager);
            }
        }
    }

    private void RemoveControlGroup(int groupNumber) => controlGroup.Remove(groupNumber);

    //---------------------------------------
    // listener methods

    private void OnTargetDataSent(object data)
    {
        SetAimState(AimState.Selecting);
    }

    private void OnCancelTargeting()
    {
        SetAimState(AimState.Selecting);
    }

    private void OnTargetingStarted()
    {
        SetAimState(AimState.Targeting);
    }

    private void OnEnable()
    {
        EventManager.AddListener("TargetDataSent", OnTargetDataSent);
        EventManager.AddListener("CancelTargeting", OnCancelTargeting);
        EventManager.AddListener("TargetingStarted", OnTargetingStarted);
        EventManager.AddListener("DeselectAll", DeselectAll);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("TargetDataSent", OnTargetDataSent);
        EventManager.RemoveListener("CancelTargeting", OnCancelTargeting);
        EventManager.RemoveListener("TargetingStarted", OnTargetingStarted);
        EventManager.RemoveListener("DeselectAll", DeselectAll);
    }

    //---------------------------------------

}