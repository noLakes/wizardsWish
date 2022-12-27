using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpawnManager : MonoBehaviour
{
    [HideInInspector]
    public List<Vector3> validEnemySpawnPoints;
    private float terrainSize;
    public Dictionary<CharacterData, int> spawnPool;
    StructureManager keyStructure;
    float zombieSpeedMod;

    private void Awake()
    {
        spawnPool = new Dictionary<CharacterData, int>();
    }

    public void Initialize(Terrain terrain)
    {
        terrainSize = terrain.terrainData.size.x;
        validEnemySpawnPoints = GetValidMapSpawnLocations(8f, new Vector2(terrainSize, terrainSize));

        AddToSpawnPool(DataHandler.LoadCharacter("Zombie"), Game.Instance.gameGlobalParameters.finalWaveZombieCount);
        keyStructure = Game.Instance.keyStructure;
        zombieSpeedMod = Game.Instance.gameGlobalParameters.zombieSpeedMod;
    }

    public void SpawnEnemyCount(int amount, CharacterData characterData, bool favorMapEdge = true)
    {
        int spawned = 0;

        while (spawned < amount)
        {
            Vector3 point = validEnemySpawnPoints[Random.Range(0, validEnemySpawnPoints.Count)];

            if (favorMapEdge)
            {
                float dist = Vector3.Distance(Vector3.zero, point);
                float chance = Mathf.Min(0.9f, 0.15f + (dist / 350f));

                if (Random.Range(0f, 1f) < chance)
                {
                    Spawn(point, characterData, 1);
                    spawned++;
                }
            }
            else
            {
                Spawn(point, characterData, 1);
                spawned++;
            }
        }
    }

    public List<UnitManager> SpawnWave(float difficulty, bool moveToCenter = false)
    {
        Vector3 spawnPoint = GetValidMapEdgeSpawn();

        // adjust wave data spawn amounts according to difficulty
        UpdateSpawnPoolData(difficulty);

        List<UnitManager> spawnedCharacters = new List<UnitManager>();

        //string readout = $"WAVE: {Game.Instance.currentWave} DIFFICULTY: {difficulty}";

        // spawn the amount of each character type as stored in the wave data
        foreach (CharacterData charKey in spawnPool.Keys)
        {
            int amountToSpawn = Mathf.CeilToInt(spawnPool[charKey] * difficulty);
            //readout += $"\n{charKey.ToString()}: {amountToSpawn}";
            for (int i = 0; i < amountToSpawn; i++)
            {
                Character c = Spawn(spawnPoint, charKey, 1);
                spawnedCharacters.Add(c.transform.GetComponent<UnitManager>());
            }
        }

        if (moveToCenter)
        {
            if (keyStructure == null) keyStructure = Game.Instance.keyStructure;
            if (spawnedCharacters.Count > 10)
            {
                // move units in batches across many frames
                StartCoroutine(MoveToKeyStructureBatchRoutine(spawnedCharacters));
            }
            else
            {
                foreach (UnitManager unit in spawnedCharacters) MoveToKeyStructure(unit);
            }

        }

        //Debug.Log(readout);
        StartCoroutine(EnableMapDotThroughFogRoutine(spawnedCharacters));
        return spawnedCharacters;
    }

    private IEnumerator EnableMapDotThroughFogRoutine(List<UnitManager> units)
    {
        yield return new WaitForSeconds(0.5f);
        foreach (UnitManager unit in units)
        {
            Transform dot = unit.transform.Find("Cylinder");
            if (dot != null && unit.transform.TryGetComponent<FogRendererToggler>(out var fogToggler))
            {
                fogToggler.RemoveRenderReference(dot);
            }
            yield return null;
        }
    }

    private void MoveToKeyStructure(UnitManager unit)
    {
        Vector3 movePos = Utility.RandomPointOnCircleEdge(keyStructure.targetSize + 1.5f, keyStructure.transform.position);
        unit.behaviorTree.SetDataNextFrame("attackMove", movePos);
    }

    private IEnumerator MoveToKeyStructureBatchRoutine(List<UnitManager> units)
    {
        foreach (UnitManager unit in units)
        {
            MoveToKeyStructure(unit);
            yield return null;
        }
    }

    public Character Spawn(Vector3 location, CharacterData characterData, int playerId)
    {
        Character character = new Character(characterData, playerId);
        character.SetPosition(location);

        if (character.data.name == "Zombie" && zombieSpeedMod > 0.0f) character.transform.GetComponent<NavMeshAgent>().speed += zombieSpeedMod;

        return character;
    }

    public Character SpawnWithDestination(Vector3 location, Vector3 destination, CharacterData characterData, int playerId)
    {
        Character character = Spawn(location, characterData, playerId);
        character.transform.GetComponent<CharacterManager>().behaviorTree.SetDataNextFrame("attackMove", destination);

        if (character.data.name == "Zombie" && zombieSpeedMod > 0.0f) character.transform.GetComponent<NavMeshAgent>().speed += zombieSpeedMod;

        return character;
    }

    private void UpdateSpawnPoolData(float difficulty)
    {
        switch (Game.Instance.currentWave)
        {
            case 1:
                // optionally introduce new enemy types to spawn pool
                // or adjust max values....
                break;
            case 2:

                break;
            case 3:

                break;
            case 4:

                break;
            default:
                break;
        }
    }

    public void AddToSpawnPool(CharacterData character, int max)
    {
        spawnPool[character] = max;
    }

    public void RemoveFromSpawnPool(CharacterData character)
    {
        if (spawnPool.ContainsKey(character)) spawnPool.Remove(character);
    }

    public Vector3 GetValidMapEdgeSpawn()
    {
        bool valid = false;
        Vector3 wavePoint = Vector3.zero;

        while (!valid)
        {
            int seed1 = Random.Range(0, 4);
            float edgeRoll = Random.Range(-350, 351);

            switch (seed1)
            {
                case 0:
                    wavePoint = new Vector3(365f, 0f, edgeRoll);
                    break;
                case 1:
                    wavePoint = new Vector3(-365f, 0f, edgeRoll);
                    break;
                case 2:
                    wavePoint = new Vector3(edgeRoll, 0f, -365f);
                    break;
                case 3:
                    wavePoint = new Vector3(edgeRoll, 0f, 365f);
                    break;
            }

            RaycastHit hit;

            Ray ray = new Ray(wavePoint + Vector3.up * 100f, Vector3.up * -1f);
            Physics.Raycast(ray, out hit, 1000f);

            //if wavepoint valid set valid to true
            if (hit.transform.gameObject == null || hit.point.y != 0f) continue;

            valid = true;
        }

        return wavePoint;
    }

    public List<Vector3> GetValidMapSpawnLocations(float radius, Vector2 sampleRegionSize)
    {
        List<Vector2> spawns = PoissonDiscSampling.GeneratePoints(radius, sampleRegionSize);
        //Debug.Log("spawns generated with radius of " + radius + ": " + spawns.Count);
        List<Vector3> validSpawns = new List<Vector3>();
        Vector3 mapCenter = Vector3.zero;
        RaycastHit hit;

        foreach (Vector2 point in spawns)
        {
            Vector3 rayOrigin = new Vector3(point.x - 350f, 100f, point.y - 350f);

            Ray ray = new Ray(rayOrigin, Vector3.up * -1f);

            Physics.Raycast(ray, out hit, 1000f);

            if (
                hit.transform.gameObject == null ||
                hit.point.y != 0f ||
                Vector3.Distance(mapCenter, hit.point) < Game.Instance.gameGlobalParameters.safetySpawnRadius
                ) continue;
            validSpawns.Add(hit.point);
        }

        return validSpawns;
    }
}
