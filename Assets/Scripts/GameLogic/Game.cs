using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public enum InGameResource
{
    Gold,
    GoldOre,
    Wood,
    Stone,
    Mana
}

[RequireComponent(typeof(AIManager))]
public class Game : MonoBehaviour
{
    public static Game Instance { get; private set; }
    public Vector3 startPosition;

    public static int TERRAIN_MASK = 1 << 8;
    public static int FLAT_TERRAIN_MASK = 1 << 11;
    // unit mask may be redundant
    public static int UNIT_MASK = 1 << 3;
    public static int PLAYER_UNIT_MASK = 1 << 6;
    public static int ENEMY_UNIT_MASK = 1 << 7;
    public static int TREE_MASK = 1 << 12;
    public static int STONE_MASK = 1 << 13;
    public static int FOOD_MASK = 1 << 14;
    public static int GOLD_MASK = 1 << 16;
    public static LayerMask ALL_UNIT_MASK;

    [HideInInspector]
    public bool gameIsPaused;

    public GamePlayersParameters gamePlayersParameters;
    public int humanPlayerID => gamePlayersParameters.myPlayerId;

    public static StructureData[] STRUCTURE_DATA;
    public static CharacterData[] CHARACTER_DATA;
    public static AbilityData[] GENERAL_CHARACTER_ABILITY_DATA;
    public static AbilityData[] GENERAL_STRUCTURE_ABILITY_DATA;
    public static AbilityData[] SPECIAL_ABILITY_DATA;

    public GameGlobalParameters gameGlobalParameters;
    public GameObject fogOfWar;

    [HideInInspector]
    public AIManager aiManager { get; private set; }

    public SpawnManager spawnManager;

    public Material fovMaterial;

    public Transform UNITS_CONTAINER;
    public List<Unit> UNITS;
    public static List<UnitManager> SELECTED_UNITS = new List<UnitManager>();
    public StructureManager keyStructure;

    public Terrain playAreaTerrain;

    public Color playerSilhouetteColor;
    public Color enemySilhouetteColor;

    public int totalWaves { get; private set; }
    public int currentWave { get; private set; }
    private int wavesSpawned;
    private float nextWaveTimer;
    private string wavePreface;
    private List<UnitManager> currentWaveUnits;
    private StructurePlacer structurePlacer;
    public SelectionManager selectionManager;

    [HideInInspector]
    public float producingRate;
    public float currentProductionTick;

    public static float terrainMaxHeight;

    public static Dictionary<InGameResource, GameResource> GAME_RESOURCES =
        new Dictionary<InGameResource, GameResource>()
        {
            {InGameResource.Wood, new GameResource("Wood", 0)},
            {InGameResource.Stone, new GameResource("Stone", 0)},
            {InGameResource.Gold, new GameResource("Gold", 0)},
            {InGameResource.Mana, new GameResource("Mana", 0)}
        };

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There is more than one instance!");
            return;
        }

        Instance = this;

        gameIsPaused = false;

        DataHandler.LoadGameData();

        // setup combined layer mask for all units
        ALL_UNIT_MASK = LayerMask.GetMask("Units") | LayerMask.GetMask("EnemyUnits") | LayerMask.GetMask("PlayerUnits");

        // apply game params on load
        GetComponent<DayAndNightCycler>().enabled = gameGlobalParameters.enableDayAndNightCycle;
        fogOfWar.SetActive(gameGlobalParameters.enableFOV);

        GetStartPosition();

        UNITS = new List<Unit>();

        aiManager = GetComponent<AIManager>();
        aiManager.Initialize();

        terrainMaxHeight = playAreaTerrain.terrainData.size.y;

        // start resource tick coroutine
        producingRate = gameGlobalParameters.productionTickTime;
        currentProductionTick = producingRate;

        // setup spawn manager
        spawnManager = GetComponent<SpawnManager>();
        spawnManager.Initialize(playAreaTerrain);

        totalWaves = gameGlobalParameters.waveCount;
        nextWaveTimer = gameGlobalParameters.waveSpawnTime;
        currentWave = 1;
        wavePreface = $"Wave {currentWave}:";
        currentWaveUnits = null;
        structurePlacer = GetComponent<StructurePlacer>();

        GAME_RESOURCES[InGameResource.Wood].AddAmount(gameGlobalParameters.startingWood);
        GAME_RESOURCES[InGameResource.Stone].AddAmount(gameGlobalParameters.startingStone);
        GAME_RESOURCES[InGameResource.Gold].AddAmount(gameGlobalParameters.startingGold);
        GAME_RESOURCES[InGameResource.Mana].AddAmount(gameGlobalParameters.startingMana);

        gameIsPaused = true;
    }

    void Start()
    {
        if (gameGlobalParameters.initialEnemySpawnCount > 0)
        {
            spawnManager.SpawnEnemyCount
            (
                gameGlobalParameters.initialEnemySpawnCount,
                DataHandler.LoadCharacter("Zombie"),
                gameGlobalParameters.spawnTowardMapEdge
            );
        }

        spawnManager.Spawn(new Vector3(0f, 0f, 0f), DataHandler.LoadCharacter("Ranger"), humanPlayerID);
        spawnManager.Spawn(new Vector3(-5f, 0f, 0f), DataHandler.LoadCharacter("Ranger"), humanPlayerID);
        spawnManager.Spawn(new Vector3(5f, 0f, 0f), DataHandler.LoadCharacter("Ranger"), humanPlayerID);

        //Structure s =  new Structure(DataHandler.LoadStructure("1_Quarry"), 2);
        //s.SetPosition(new Vector3(20f, 0f, 0f));
    }

    private void Update()
    {
        if (gameIsPaused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Resume();
            }
            return;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // deselect units
                if (SELECTED_UNITS.Count > 0)
                {
                    // deselect all
                    EventManager.TriggerEvent("DeselectAll");
                }
                // cancel placing
                else if (structurePlacer.activePlacing)
                {
                    structurePlacer.CancelPlacedStructure();
                }
                // cancel targeting
                else if (selectionManager.aimState == AimState.Targeting)
                {
                    EventManager.TriggerEvent("CancelTargeting");
                }
                else
                {
                    Pause();
                }
            }

            if (keyStructure == null) GameOver(false);

            TickProduction();

            if (wavesSpawned < totalWaves)
            {
                TickWaveTimer();
            }
            else if (totalWaves != 0)
            {
                // check win?
                if (currentWaveUnits.Count == 0)
                {
                    GameOver(true);
                }

                currentWaveUnits.RemoveAll(unit => !unit);
            }
        }
    }

    private void TickProduction()
    {
        currentProductionTick -= Time.deltaTime;

        if (currentProductionTick <= 0f)
        {
            //Debug.Log("RESOURCE TICK");
            EventManager.TriggerEvent("ProduceResources");
            EventManager.TriggerEvent("ResourcesChanged");
            currentProductionTick = producingRate;
        }
    }

    private void TickWaveTimer()
    {
        nextWaveTimer -= Time.deltaTime;

        TimeSpan waveSpawnTime = TimeSpan.FromSeconds(nextWaveTimer);
        //format and update text in ui via event
        EventManager.TriggerEvent("UpdateWaveSpawnTimer", $"{wavePreface} {waveSpawnTime.ToString(@"mm\:ss")}");

        if (nextWaveTimer <= 0f)
        {
            float difficultyRating = gameGlobalParameters.waveDifficulty.Evaluate((float)currentWave / (float)totalWaves);

            // spawn wave
            currentWaveUnits = spawnManager.SpawnWave(difficultyRating, true);
            wavesSpawned++;

            // increment wave count if less than max
            if (currentWave < totalWaves)
            {
                currentWave++;
                nextWaveTimer = gameGlobalParameters.waveSpawnTime;
                wavePreface = currentWave == totalWaves ? "Final Wave:" : $"Wave {currentWave}:";
            }
            else
            {
                EventManager.TriggerEvent("UpdateWaveSpawnTimer", "FINAL WAVE");
            }
        }
    }

    private void GetStartPosition()
    {
        startPosition = Utility.MiddleOfScreenPointToWorld();
    }

    private void GameOver(bool win)
    {
        GetComponent<UIManager>().GameOver(win);
        gameIsPaused = true;
    }

    public void Launch()
    {
        Resume();
        GetComponent<UIManager>().Launch();
    }

    public void Reset()
    {
        SceneManager.LoadScene("Main");
    }


    public void Pause()
    {
        gameIsPaused = true;
        EventManager.TriggerEvent("PauseGame");
    }

    public void Resume()
    {
        gameIsPaused = false;
        EventManager.TriggerEvent("ResumeGame");
    }

    public void Quit() => Application.Quit();

    private void OnUpdateDayAndNightCycle(object data)
    {
        bool dayAndNightIsOn = (bool)data;
        GetComponent<DayAndNightCycler>().enabled = dayAndNightIsOn;
    }

    private void OnUpdateFOV(object data)
    {
        bool fovIsOn = (bool)data;
        fogOfWar.SetActive(fovIsOn);

        foreach (Unit unit in UNITS)
        {
            if (unit.owner == humanPlayerID) unit.transform.GetComponent<UnitManager>().ToggleFOV((bool)data);
        }
    }

    private void OnEnable()
    {
        EventManager.AddListener("UpdateGameParameter:enableDayAndNightCycle", OnUpdateDayAndNightCycle);
        EventManager.AddListener("UpdateGameParameter:enableFOV", OnUpdateFOV);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("UpdateGameParameter:enableDayAndNightCycle", OnUpdateDayAndNightCycle);
        EventManager.RemoveListener("UpdateGameParameter:enableFOV", OnUpdateFOV);
    }

    private void OnApplicationQuit()
    {
#if !UNITY_EDITOR
        DataHandler.SaveGameData();
#endif
    }
}
