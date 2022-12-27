using UnityEngine;

[CreateAssetMenu(fileName = "Parameters", menuName = "Scriptable Objects/Game Parameters", order = 10)]
public class GameGlobalParameters : GameParameters
{
    public override string GetParametersName() => "Global";

    [Header("Day Night Cycle")]
    public bool enableDayAndNightCycle;
    public float dayLengthInSeconds;
    public float dayInitialRatio;

    [Header("Fog Of War")]
    public bool enableFOV;

    [Header("Starting Structure")]
    public StructureData inititalStructure;

    [Header("Difficulty Settings")]
    public float safetySpawnRadius;
    public int initialEnemySpawnCount;
    public bool spawnTowardMapEdge;
    public int waveCount;
    public float waveSpawnTime;
    public AnimationCurve waveDifficulty;
    public int finalWaveZombieCount;
    [SerializeField, Range(0.0f, 1.0f)]public float enemyWanderChance;
    public float zombieSpeedMod;

    [Header("Resource Settings")]
    public int startingWood;
    public int startingStone;
    public int startingGold;
    public int startingMana;
    public float productionTickTime;
    public float goldOreProductionRange;
    public float woodProductionRange;
    public float stoneProductionRange;

    public delegate int ResourceProductionFunc(float distance);

    [HideInInspector]
    public ResourceProductionFunc woodProductionFunc = (float distance) =>
    {
        return Mathf.CeilToInt(2f * 1f / distance);
    };

    [HideInInspector]
    public ResourceProductionFunc stoneProductionFunc = (float distance) =>
    {
        return Mathf.CeilToInt(8f * 1f / distance);
    };

    [HideInInspector]
    public ResourceProductionFunc goldOreProductionFunc = (float distance) =>
    {
        return Mathf.CeilToInt(40f * 1f / distance);
    };
}