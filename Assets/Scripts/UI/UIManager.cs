using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    // main ui panels
    public Transform startMenuPanel;
    public Transform gameplayUiPanel;

    // game over panels
    public Transform defeatPanel;
    public Transform victoryPanel;

    private StructurePlacer structurePlacer;
    public TargetingManager targetingManager;

    public Transform structureButtonContainer;
    public GameObject structureButtonPrefab;

    // game settings
    public GameObject gameSettingsPanel;
    public Transform gameSettingsMenuParent;
    public Text gameSettingsContentName;
    public Transform gameSettingsContentParent;
    public GameObject gameSettingsMenuButtonPrefab;
    public GameObject gameSettingsParameterPrefab;
    public GameObject sliderPrefab;
    public GameObject togglePrefab;
    private Dictionary<string, GameParameters> gameParameters;

    public Transform resourceDisplayContainer;
    public GameObject resourceDisplayPrefab;

    public GameObject resourceCostPrefab;
    public RectTransform placedStructureProductionPreview;

    // info panel
    public GameObject infoPanel;
    private Text infoPanelTitleText;
    private Text infoPanelDescriptionText;
    private Text infoPanelCooldownText;
    private Transform infoPanelCostContainer;

    private Dictionary<string, Button> structureButtons;
    private Dictionary<InGameResource, Text> resourceTexts;

    // unit tile selection display (for multiple types selected)
    public Transform selectedUnitsTileContainer;
    public GameObject selectedUnitTilePrefab;

    // selected unit info display
    public Transform unitInfoPanel;
    public Transform selectedUnitProductionContainer;

    // selected unit build queue display
    public Transform buildQueueueContainer;
    public GameObject buildQueueItemPrefab;

    // abilities
    public Transform abilityButtonContainer;
    public GameObject abilityButtonPrefab;

    // wave timer
    public Text waveSpawnTimerText;

    // debug
    //public Text debugPanelText;

    public Color invalidTextColor;

    public Dictionary<InGameResource, int> resourceProductionRates =
        new Dictionary<InGameResource, int>()
        {
            {InGameResource.Wood, 0},
            {InGameResource.Stone, 0},
            {InGameResource.Gold, 0},
            {InGameResource.Mana, 0}
        };

    private void Awake()
    {
        startMenuPanel.gameObject.SetActive(true);
        gameplayUiPanel.gameObject.SetActive(false);

        structurePlacer = GetComponent<StructurePlacer>();
        structureButtons = new Dictionary<string, Button>();

        // create buttons for each structure type
        for (int i = 0; i < Game.STRUCTURE_DATA.Length; i++)
        {
            if (Game.STRUCTURE_DATA[i].code == "wizardsTower") continue;
            GameObject button = GameObject.Instantiate(structureButtonPrefab, structureButtonContainer);
            button.GetComponent<StructureButton>().Initialize(Game.STRUCTURE_DATA[i]);
            string code = Game.STRUCTURE_DATA[i].code;
            button.name = code;
            button.transform.Find("Text").GetComponent<Text>().text = Game.STRUCTURE_DATA[i].unitName;
            Button b = button.GetComponent<Button>();
            AddBuildButtonListener(b, i);

            structureButtons[code] = b;
            if (!Game.STRUCTURE_DATA[i].CanBuy())
            {
                b.interactable = false;
            }
        }

        ShowBuildMenu();

        // setup resource display
        resourceTexts = new Dictionary<InGameResource, Text>();
        foreach (KeyValuePair<InGameResource, GameResource> pair in Game.GAME_RESOURCES)
        {
            GameObject display = Instantiate(resourceDisplayPrefab, resourceDisplayContainer);
            display.name = pair.Key.ToString();
            display.transform.Find("Icon").GetComponent<Image>().sprite = Resources.Load<Sprite>($"Textures/GameResources/{pair.Key}");
            resourceTexts[pair.Key] = display.transform.Find("Text").GetComponent<Text>();
            SetResourceText(pair.Key, pair.Value.amount);
        }

        placedStructureProductionPreview.gameObject.SetActive(false);

        // setup info panel
        Transform infoPanelTransform = infoPanel.transform;
        infoPanelTitleText = infoPanelTransform.Find("Content/Title").GetComponent<Text>();
        infoPanelDescriptionText = infoPanelTransform.Find("Content/Description").GetComponent<Text>();
        infoPanelCooldownText = infoPanelTransform.Find("Content/Cooldown").GetComponent<Text>();
        infoPanelCostContainer = infoPanelTransform.Find("Content/ResourceCosts");
        ShowInfoPanel(false);

        // setup settings menu
        gameSettingsPanel.SetActive(false);
        GameParameters[] gameParametersList = Resources.LoadAll<GameParameters>("Scriptable Objects/Parameters");
        gameParameters = new Dictionary<string, GameParameters>();

        foreach (GameParameters p in gameParametersList) gameParameters[p.GetParametersName()] = p;

        SetupGameSettingsPanel();
    }

    public void Launch()
    {
        startMenuPanel.gameObject.SetActive(false);
        gameplayUiPanel.gameObject.SetActive(true);
    }

    public void GameOver(bool win)
    {
        if (win)
        {
            victoryPanel.gameObject.SetActive(true);
        }
        else
        {
            defeatPanel.gameObject.SetActive(true);
        }
    }

    public void ShowAbilityMenu()
    {
        structureButtonContainer.gameObject.SetActive(false);
        abilityButtonContainer.gameObject.SetActive(true);
    }

    public void ShowBuildMenu()
    {
        abilityButtonContainer.gameObject.SetActive(false);
        structureButtonContainer.gameObject.SetActive(true);
    }

    private void AddBuildButtonListener(Button b, int i)
    {
        b.onClick.AddListener(() => structurePlacer.SelectPlacedStructure(i));
    }

    private void SetResourceText(InGameResource resource, int value)
    {
        resourceTexts[resource].text = value.ToString();
    }

    private void OnResourcesChanged()
    {
        foreach (KeyValuePair<InGameResource, GameResource> pair in Game.GAME_RESOURCES)
        {
            SetResourceText(pair.Key, pair.Value.amount);
        }
        CheckStructureButtons();
        CheckAbilityButtons();
    }

    private void CheckStructureButtons()
    {
        for (int i = 0; i < Game.STRUCTURE_DATA.Length; i++)
        {
            if (Game.STRUCTURE_DATA[i].code == "wizardsTower") continue;
            StructureData data = Game.STRUCTURE_DATA[i];
            structureButtons[data.code].interactable = data.CanBuy();
        }
    }

    private void CheckAbilityButtons()
    {
        foreach (Transform abilityButton in abilityButtonContainer)
        {
            AbilityData data = abilityButton.GetComponent<AbilityButton>().abilityData;
            abilityButton.GetComponent<Button>().interactable = data.CanBuy();
        }
    }

    private void OnHoverStructureButton(object data)
    {
        SetInfoPanel((UnitData)data);
        ShowInfoPanel(true);
    }

    private void OnUnhoverStructureButton()
    {
        ShowInfoPanel(false);
    }

    private void OnHoverAbilityButton(object data)
    {
        SetInfoPanel((AbilityData)data);
        ShowInfoPanel(true);
    }

    private void OnUnhoverAbilityButton()
    {
        ShowInfoPanel(false);
    }

    private void OnSelectUnit(object data)
    {
        if (!abilityButtonContainer.gameObject.activeInHierarchy) ShowAbilityMenu();

        Unit unit = (Unit)data;
        if (SelectionManager.UniqueSelection())
        {
            //Debug.Log("Unique selection");
            SetActiveUnitPanel(unit);
            SetBuildQueue(unit.transform.GetComponent<UnitManager>());
        }
        else
        {
            //Debug.Log("Multi selection");
            ToggleUniqueUnitSelection(false);
            ClearAbilityButtons();
            // load shared ability buttons
            LoadAbilityButtons(unit.generalAbilityManagers);
            SetBuildQueue();
        }

        AddSelectedUnitToUIList(unit);
    }

    private void OnDeselectUnit(object data)
    {
        Unit unit = (Unit)data;
        RemoveSelectedUnitFromUIList(unit.code);

        if (SelectionManager.UniqueSelection())
        {
            SetActiveUnitPanel(Game.SELECTED_UNITS[0].Unit);
            SetBuildQueue(unit.transform.GetComponent<UnitManager>());
        }
        else
        {
            if (Game.SELECTED_UNITS.Count == 0) ClearAbilityButtons();
            ToggleUniqueUnitSelection(false);
            ShowBuildMenu();
            SetBuildQueue();
        }
    }

    public void AddSelectedUnitToUIList(Unit unit)
    {
        // if there is another unit of the same type already selected
        // increase the counter
        Transform alreadyInstantiatedChild = selectedUnitsTileContainer.Find(unit.code);

        if (alreadyInstantiatedChild != null)
        {
            Text t = alreadyInstantiatedChild.Find("Count").GetComponent<Text>();
            int count = int.Parse(t.text);
            t.text = (count + 1).ToString();
        }
        // else create a brand new counter initialized with a count of 1
        else
        {
            GameObject g = GameObject.Instantiate(selectedUnitTilePrefab, selectedUnitsTileContainer);
            g.name = unit.code;
            Transform t = g.transform;
            t.Find("Count").GetComponent<Text>().text = "1";
            t.Find("Name").GetComponent<Text>().text = unit.data.unitName;
        }
    }

    public void RemoveSelectedUnitFromUIList(string code)
    {
        Transform listItem = selectedUnitsTileContainer.Find(code);

        if (listItem == null) return;

        Text t = listItem.Find("Count").GetComponent<Text>();
        int count = int.Parse(t.text);
        count -= 1;

        if (count == 0)
            Destroy(listItem.gameObject);
        else
            t.text = count.ToString();
    }

    private void AddUnitAbilityButtonListener(Button b, UnitManager unitManager, string abilityName)
    {
        b.onClick.AddListener(() => unitManager.TryCastAbility(abilityName));
    }

    private void AddUnitAbilityTargetingListener(Button b, AbilityData data)
    {
        b.onClick.AddListener(() => targetingManager.SetTargetingAbility(data));
    }

    private void SetActiveUnitPanel(Unit unit)
    {
        //debugPanelText.text = "";

        bool unitIsMine = unit.owner == Game.Instance.humanPlayerID;

        // enable active unit panel
        ToggleUniqueUnitSelection(true);

        // clear any existing ability buttons
        ClearAbilityButtons();

        // clear production from previous selection
        foreach (Transform child in selectedUnitProductionContainer) Destroy(child.gameObject);

        if (unitIsMine)
        {
            LoadAbilityButtons(unit.generalAbilityManagers);

            // add ability buttons with appropriate listener actions
            if (unit.abilityManagers.Count > 0) LoadAbilityButtons(unit.abilityManagers);
            else Debug.Log(unit.code + " has no abilities to trigger");
        }

        Transform t = unitInfoPanel;
        t.Find("Name").GetComponent<Text>().text = unit.data.unitName;
        t.Find("Description").GetComponent<Text>().text = unit.data.description;
        if (Game.SELECTED_UNITS.Count == 1)
        {
            // populate panel with unit info
            t.Find("Health").GetComponent<Text>().text = $"HP: {unit.health}/{unit.maxHealth}";
            t.Find("Attack").GetComponent<Text>().text = "ATK: " + unit.attackDamage.ToString();
            t.Find("Level").GetComponent<Text>().text = $"LvL:  {unit.level}";
            if (!unit.isMaxLevel) t.Find("Experience").GetComponent<Text>().text = $"XP:  {unit.xp}/100";

            if (unit.production.Count > 0)
            {
                GameObject g; Transform tr;
                foreach (KeyValuePair<InGameResource, int> resource in unit.production)
                {
                    g = Instantiate(
                        resourceCostPrefab, selectedUnitProductionContainer);
                    tr = g.transform;
                    tr.Find("Text").GetComponent<Text>().text = $"+{resource.Value}";
                    tr.Find("Icon").GetComponent<Image>().sprite = Resources.Load<Sprite>($"Textures/GameResources/{resource.Key}");
                }
            }
        }
        else
        {
            // clear specific unit info ?
            t.Find("Health").GetComponent<Text>().text = "";
            t.Find("Attack").GetComponent<Text>().text = "";
            t.Find("Level").GetComponent<Text>().text = "";
            t.Find("Experience").GetComponent<Text>().text = "";
        }

        /*
        //debug
        Transform utr = unit.transform;
        UnitManager um = utr.GetComponent<UnitManager>();

        BehaviorTree.Tree bt = um.behaviorTree;
        string debugOutput = "";
        debugOutput += $"pos: {utr.position}";
        debugOutput += $"\ntarget: {bt.GetData("currentTarget")}";

        object target = bt.GetData("currentTarget");
        if (target != null)
        {
            Transform targetTransform = (Transform)target;
            float targetDistance = Vector3.Distance(utr.position, targetTransform.position);
            debugOutput += $"\ntargetDist: {targetDistance}/{unit.data.fieldOfView}";
            debugOutput += $"\nattackRange: {unit.attackRange}";

            if (utr.TryGetComponent<CharacterManager>(out CharacterManager cm))
            {
                bool pathToTarget = cm.ValidPathTo(targetTransform.position);
                debugOutput += $"\npathToTarget: {pathToTarget}";
                debugOutput += $"\nagentDest: {cm.navMeshAgent.destination}";
            }
        }

        debugOutput += $"\ndestination: {bt.GetData("destinationPoint")}";
        debugOutput += $"\nattackMove: {bt.GetData("attackMove")}";

        debugPanelText.text = debugOutput;
        */
    }

    private void OnRefreshSelectedUnitInfo()
    {
        if (SelectionManager.UniqueSelection())
        {
            SetActiveUnitPanel(Game.SELECTED_UNITS[0].Unit);
        }
    }

    private void LoadAbilityButtons(Dictionary<string, AbilityManager> abilities)
    {
        GameObject g; Transform tr; Button b; AbilityManager am;

        foreach (string key in abilities.Keys)
        {
            g = GameObject.Instantiate(
                abilityButtonPrefab, abilityButtonContainer);
            tr = g.transform;
            b = g.GetComponent<Button>();

            am = abilities[key];
            g.GetComponent<AbilityButton>().Initialize(am.ability);
            am.SetButton(b);
            tr.Find("Text").GetComponent<Text>().text = am.abilityName;
            //Debug.Log("Adding ability button for: " + am.abilityName);
            // add target then trigger listener if targeting is needed
            if (am.targetRequired)
            {
                AddUnitAbilityTargetingListener(b, am.ability);
            }
            else // add trigger listener
            {
                if (am.cost.Count > 0)
                {
                    AddUnitAbilityButtonListener(b, Game.SELECTED_UNITS[0], key);
                }
                else
                {
                    foreach (UnitManager m in Game.SELECTED_UNITS)
                    {
                        AddUnitAbilityButtonListener(b, m, key);
                    }
                }
            }

            if (b.interactable) b.interactable = am.ability.CanBuy();
        }
    }

    private void ClearAbilityButtons()
    {
        //Debug.Log("Clearing ability buttons");
        foreach (Transform child in abilityButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void ToggleUniqueUnitSelection(bool isUniqueSelection)
    {
        // hide list display
        selectedUnitsTileContainer.gameObject.SetActive(!isUniqueSelection);
        // show unique display
        unitInfoPanel.gameObject.SetActive(isUniqueSelection);
    }

    public void SetInfoPanel(UnitData data)
    {
        // update texts
        if (data.code != "") infoPanelTitleText.text = data.code;

        if (data.description != "") infoPanelDescriptionText.text = data.description;

        // clear resource costs and reinstantiate new ones
        foreach (Transform child in infoPanelCostContainer) Destroy(child.gameObject);

        if (data.cost.Count > 0)
        {
            GameObject g; Transform t;

            foreach (ResourceValue resource in data.cost)
            {
                g = GameObject.Instantiate(resourceCostPrefab, infoPanelCostContainer);
                t = g.transform;
                t.Find("Text").GetComponent<Text>().text = resource.amount.ToString();
                t.Find("Icon").GetComponent<Image>().sprite = Resources.Load<Sprite>($"Textures/GameResources/{resource.code}");

                // set text to invalid color if cannot afford this resource
                if (Game.GAME_RESOURCES[resource.code].amount < resource.amount) t.Find("Text").GetComponent<Text>().color = invalidTextColor;
            }
        }
    }

    public void SetInfoPanel(AbilityData data)
    {
        if (data.name != "") infoPanelTitleText.text = data.name;

        if (data.description != "") infoPanelDescriptionText.text = data.description;

        if (data.cooldown > 0) infoPanelCooldownText.text = $"Cooldown: {data.cooldown}s";
        else infoPanelCooldownText.text = "";

        // clear resource costs
        foreach (Transform child in infoPanelCostContainer) Destroy(child.gameObject);

        if (data.cost.Count > 0)
        {
            GameObject g; Transform t;

            foreach (ResourceValue resource in data.cost)
            {
                g = GameObject.Instantiate(resourceCostPrefab, infoPanelCostContainer);
                t = g.transform;
                t.Find("Text").GetComponent<Text>().text = resource.amount.ToString();
                t.Find("Icon").GetComponent<Image>().sprite = Resources.Load<Sprite>($"Textures/GameResources/{resource.code}");

                // set text to invalid color if cannot afford this resource
                if (Game.GAME_RESOURCES[resource.code].amount < resource.amount) t.Find("Text").GetComponent<Text>().color = invalidTextColor;
            }
        }
    }

    public void ShowInfoPanel(bool show)
    {
        infoPanel.SetActive(show);
    }

    private void OnPauseGame()
    {
        ToggleGameSettingsPanel(true);
    }

    private void OnResumeGame()
    {
        ToggleGameSettingsPanel(false);
    }

    public void ToggleGameSettingsPanel(bool status)
    {
        gameSettingsPanel.SetActive(status);
    }

    private void SetupGameSettingsPanel()
    {
        GameObject g; string n;
        List<string> availableMenus = new List<string>();
        foreach (GameParameters parameters in gameParameters.Values)
        {
            // ignore game parameters assets that don't have
            // any parameter to show
            if (parameters.fieldsToShowInGame.Count == 0) continue;

            g = GameObject.Instantiate(
                gameSettingsMenuButtonPrefab, gameSettingsMenuParent);
            n = parameters.GetParametersName();
            g.transform.Find("Text").GetComponent<Text>().text = n;
            AddGameSettingsPanelMenuListener(g.GetComponent<Button>(), n);
            availableMenus.Add(n);
        }

        // if possible, set the first menu as the currently active one
        if (availableMenus.Count > 0)
            SetGameSettingsContent(availableMenus[0]);
    }

    private void AddGameSettingsPanelMenuListener(Button b, string menu)
    {
        b.onClick.AddListener(() => SetGameSettingsContent(menu));
    }

    private void SetGameSettingsContent(string menu)
    {
        gameSettingsContentName.text = menu;

        foreach (Transform child in gameSettingsContentParent)
            Destroy(child.gameObject);

        GameParameters parameters = gameParameters[menu];
        System.Type ParametersType = parameters.GetType();
        GameObject gWrapper, gEditor;
        RectTransform rtWrapper, rtEditor;
        int i = 0;
        float contentWidth = 534f;
        float parameterNameWidth = 200f;
        float fieldHeight = 32f;

        foreach (string fieldName in parameters.fieldsToShowInGame)
        {
            gWrapper = GameObject.Instantiate(
                gameSettingsParameterPrefab, gameSettingsContentParent);
            gWrapper.transform.Find("Text").GetComponent<Text>().text =
                Utility.CapitalizeWords(fieldName);

            gEditor = null;
            FieldInfo field = ParametersType.GetField(fieldName);
            if (field.FieldType == typeof(bool))
            {
                gEditor = Instantiate(togglePrefab);
                Toggle t = gEditor.GetComponent<Toggle>();
                t.isOn = (bool)field.GetValue(parameters);

                t.onValueChanged.AddListener(delegate
                {
                    OnGameSettingsToggleValueChanged(parameters, field, fieldName, t);
                });
            }
            else if (field.FieldType == typeof(int) || field.FieldType == typeof(float))
            {
                bool isRange = System.Attribute.IsDefined(field, typeof(RangeAttribute), false);
                if (isRange)
                {
                    RangeAttribute attr = (RangeAttribute)System.Attribute.GetCustomAttribute(field, typeof(RangeAttribute));
                    gEditor = Instantiate(sliderPrefab);
                    Slider s = gEditor.GetComponent<Slider>();
                    s.minValue = attr.min;
                    s.maxValue = attr.max;
                    s.wholeNumbers = field.FieldType == typeof(int);
                    s.value = field.FieldType == typeof(int)
                        ? (int)field.GetValue(parameters)
                        : (float)field.GetValue(parameters);

                    s.onValueChanged.AddListener(delegate
                    {
                        OnGameSettingsSliderValueChanged(parameters, field, fieldName, s);
                    });
                }
            }
            rtWrapper = gWrapper.GetComponent<RectTransform>();
            rtWrapper.anchoredPosition = new Vector2(0f, -i * fieldHeight);
            rtWrapper.sizeDelta = new Vector2(contentWidth, fieldHeight);

            if (gEditor != null)
            {
                gEditor.transform.SetParent(gWrapper.transform);
                rtEditor = gEditor.GetComponent<RectTransform>();
                rtEditor.anchoredPosition = new Vector2((parameterNameWidth + 16f), 0f);
                rtEditor.sizeDelta = new Vector2(rtWrapper.sizeDelta.x - (parameterNameWidth + 16f), fieldHeight);
            }

            i++;
        }

        RectTransform rt = gameSettingsContentParent.GetComponent<RectTransform>();
        Vector2 size = rt.sizeDelta;
        size.y = i * fieldHeight;
        rt.sizeDelta = size;
    }

    private void UpdateProductionRates()
    {
        resourceProductionRates.Clear();

        foreach (Unit unit in Game.Instance.UNITS)
        {
            if (unit.owner == Game.Instance.humanPlayerID && unit.isActive)
            {
                foreach (KeyValuePair<InGameResource, int> prod in unit.production)
                {
                    if (resourceProductionRates.ContainsKey(prod.Key))
                    {
                        resourceProductionRates[prod.Key] += prod.Value;
                    }
                    else
                    {
                        resourceProductionRates[prod.Key] = prod.Value;
                    }
                }
            }
        }

        UpdateProductionRateText();
    }

    private void UpdateProductionRateText()
    {
        foreach (InGameResource key in resourceProductionRates.Keys)
        {
            Text rateText = resourceDisplayContainer.Find(key.ToString()).Find("Rate").GetComponent<Text>();
            int rate = resourceProductionRates[key];

            if (rate > 0)
            {
                rateText.text = "+" + rate;
            }
            else
            {
                rateText.text = "";
            }
        }
    }

    private void SetBuildQueue(List<AbilityManager> buildQueue = null)
    {
        if (buildQueue == null)
        {
            buildQueueueContainer.gameObject.SetActive(false);
        }
        else
        {
            // enable build que 
            buildQueueueContainer.gameObject.SetActive(true);

            // clear build que
            if (buildQueueueContainer.childCount > 0)
            {
                for (int i = buildQueueueContainer.childCount - 1; i > -1; i--)
                {
                    Object.Destroy(buildQueueueContainer.GetChild(i).gameObject);
                }
            }

            // add new build queue
            foreach (AbilityManager buildItem in buildQueue)
            {
                GameObject g = GameObject.Instantiate(buildQueueItemPrefab, buildQueueueContainer);
                g.transform.Find("Text").GetComponent<Text>().text = buildItem.ability.unitReference.unitName;
            }
        }
    }

    private void SetBuildQueue(UnitManager unit)
    {
        List<AbilityManager> buildQueue = (List<AbilityManager>)unit.behaviorTree.GetData("buildQueue");
        SetBuildQueue(buildQueue);
    }

    private void OnProductionRateChanged()
    {
        UpdateProductionRates();
    }

    private void OnGameSettingsToggleValueChanged(
        GameParameters parameters,
        FieldInfo field,
        string gameParameter,
        Toggle change
    )
    {
        field.SetValue(parameters, change.isOn);
        EventManager.TriggerEvent($"UpdateGameParameter:{gameParameter}", change.isOn);
    }

    private void OnGameSettingsSliderValueChanged(
        GameParameters parameters,
        FieldInfo field,
        string gameParameter,
        Slider change
    )
    {
        if (field.FieldType == typeof(int))
            field.SetValue(parameters, (int)change.value);
        else
            field.SetValue(parameters, change.value);
        EventManager.TriggerEvent($"UpdateGameParameter:{gameParameter}", change.value);
    }

    private void OnUpdatePlacedStructureProduction(object data)
    {
        object[] values = (object[])data;
        Dictionary<InGameResource, int> production = (Dictionary<InGameResource, int>)values[0];
        Vector3 pos = (Vector3)values[1];

        // clear current list
        foreach (Transform child in placedStructureProductionPreview.gameObject.transform)
            Destroy(child.gameObject);

        // add one "resource cost" prefab per resource
        GameObject g;
        Transform t;
        foreach (KeyValuePair<InGameResource, int> pair in production)
        {
            g = GameObject.Instantiate(
                resourceCostPrefab,
                placedStructureProductionPreview.transform);
            t = g.transform;
            t.Find("Text").GetComponent<Text>().text = $"+{pair.Value}";
            t.Find("Icon").GetComponent<Image>().sprite = Resources.Load<Sprite>($"Textures/GameResources/{pair.Key}");
        }

        // resize container to fit the right number of lines
        placedStructureProductionPreview.sizeDelta = new Vector2(80, 24 * production.Count);

        // place container top-right of the "phantom" building
        /*
        placedStructureProductionPreview.anchoredPosition =
            (Vector2)Camera.main.WorldToScreenPoint(pos)
            + Vector2.right * 40f
            + Vector2.up * 10f;
        */
        placedStructureProductionPreview.transform.position = (Vector2)Camera.main.WorldToScreenPoint(pos)
            + Vector2.right * placedStructureProductionPreview.sizeDelta.x
            + Vector2.up * 10f;
    }

    private void OnPlaceStructureOn()
    {
        placedStructureProductionPreview.gameObject.SetActive(true);
    }

    private void OnPlaceStructureOff()
    {
        placedStructureProductionPreview.gameObject.SetActive(false);
    }

    private void OnWaveSpawnTimerUpdate(object data)
    {
        string timerText = (string)data;

        waveSpawnTimerText.text = timerText;
    }

    private void OnBuildQueueUpdated(object data)
    {
        // set build queue for selected unit to represent one passed here
        List<AbilityManager> buildQueue = (List<AbilityManager>)data;

        SetBuildQueue(buildQueue);
    }

    private void OnRemoveBuildQueueItem(object data)
    {
        Debug.Log("SHENGUS");

        // check if selected unit panel is active and a structure is selected
        if (Game.SELECTED_UNITS.Count == 1 && Game.SELECTED_UNITS[0] is StructureManager)
        {
            // get selected unit build queue, remove index, update
            int index = (int)data;
            StructureManager s = (StructureManager)Game.SELECTED_UNITS[0];
            StructureBT bt = (StructureBT)s.behaviorTree;
            bt.StopBuilding(index);

            List<AbilityManager> buildQueue = (List<AbilityManager>)s.behaviorTree.GetData("buildQueue");

            SetBuildQueue(buildQueue);
        }
        else
        {
            Debug.Log("DDENGUS");
        }
    }

    private void OnEnable()
    {
        EventManager.AddListener("PauseGame", OnPauseGame);
        EventManager.AddListener("ResumeGame", OnResumeGame);

        EventManager.AddListener("ResourcesChanged", OnResourcesChanged);

        EventManager.AddListener("ProductionRateChanged", OnProductionRateChanged);

        EventManager.AddListener("UpdateSelectedUnitPanel", OnRefreshSelectedUnitInfo);

        EventManager.AddListener("HoverStructureButton", OnHoverStructureButton);
        EventManager.AddListener("UnhoverStructureButton", OnUnhoverStructureButton);

        EventManager.AddListener("HoverAbilityButton", OnHoverAbilityButton);
        EventManager.AddListener("UnhoverAbilityButton", OnUnhoverAbilityButton);

        EventManager.AddListener("SelectUnit", OnSelectUnit);
        EventManager.AddListener("DeselectUnit", OnDeselectUnit);

        EventManager.AddListener("UpdatePlacedStructureProduction", OnUpdatePlacedStructureProduction);
        EventManager.AddListener("PlaceStructureOn", OnPlaceStructureOn);
        EventManager.AddListener("PlaceStructureOff", OnPlaceStructureOff);

        EventManager.AddListener("UpdateWaveSpawnTimer", OnWaveSpawnTimerUpdate);

        EventManager.AddListener("BuildQueueUpdated", OnBuildQueueUpdated);
        EventManager.AddListener("RemoveBuildQueueItem", OnRemoveBuildQueueItem);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("PauseGame", OnPauseGame);
        EventManager.RemoveListener("ResumeGame", OnResumeGame);

        EventManager.RemoveListener("ResourcesChanged", OnResourcesChanged); ;

        EventManager.RemoveListener("ProductionRateChanged", OnProductionRateChanged);

        EventManager.RemoveListener("UpdateSelectedUnitPanel", OnRefreshSelectedUnitInfo);

        EventManager.RemoveListener("HoverStructureButton", OnHoverStructureButton);
        EventManager.RemoveListener("UnhoverStructureButton", OnUnhoverStructureButton);

        EventManager.RemoveListener("HoverAbilityButton", OnHoverAbilityButton);
        EventManager.RemoveListener("UnhoverAbilityButton", OnUnhoverAbilityButton);

        EventManager.RemoveListener("SelectUnit", OnSelectUnit);
        EventManager.RemoveListener("DeselectUnit", OnDeselectUnit);

        EventManager.RemoveListener("UpdatePlacedStructureProduction", OnUpdatePlacedStructureProduction);
        EventManager.RemoveListener("PlaceStructureOn", OnPlaceStructureOn);
        EventManager.RemoveListener("PlaceStructureOff", OnPlaceStructureOff);

        EventManager.RemoveListener("UpdateWaveSpawnTimer", OnWaveSpawnTimerUpdate);

        EventManager.RemoveListener("BuildQueueUpdated", OnBuildQueueUpdated);
        EventManager.RemoveListener("RemoveBuildQueueItem", OnRemoveBuildQueueItem);
    }

}
