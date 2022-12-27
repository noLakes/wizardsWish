using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AI;

public static class Utility
{
    public static Vector3 GetMouseWorldPosition()
    {
        Vector3 vec = GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
        vec.z = 0f;
        return vec;
    }

    public static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera)
    {
        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
        return worldPosition;
    }

    // generates poisson samples and then copies those to build a list of offset positions equal to unit count
    public static List<Vector2> BuildSampleOffsets(int amount, float radius, Vector2 sampleRegionSize)
    {
        List<Vector2> offsets = new List<Vector2>(amount);

        List<Vector2> poissonSamples = PoissonDiscSampling.GeneratePoints(
            radius, sampleRegionSize);

        float heightOffset = sampleRegionSize.x / 2f;
        float widthOffset = sampleRegionSize.y / 2f;

        for (int i = 0; i < amount && i < poissonSamples.Count; i++)
        {
            offsets.Add(new Vector2(poissonSamples[i].x - heightOffset, poissonSamples[i].y - widthOffset));
        }

        return offsets;
    }

    // returns list of positions with poisson sampling scatter based around given starting vector3
    public static List<Vector3> BuildPoissonPositions(int amount, float radius, Vector2 sampleRegionSize, Vector3 startPosition)
    {
        List<Vector2> offsets = BuildSampleOffsets(amount, radius, sampleRegionSize);
        return OffsetsToPositions(offsets, startPosition);
    }

    // returns list of positions based on starting point with applied offsets
    public static List<Vector3> OffsetsToPositions(List<Vector2> offsets, Vector3 startPosition)
    {
        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < offsets.Count; i++)
        {
            Vector3 newPosition = startPosition + new Vector3(offsets[i].x, 0, offsets[i].y);
            positions.Add(newPosition);
        }

        return positions;
    }

    public static int GetAlphaKeyValue(string keyString)
    {
        if (keyString == "0" || keyString == "à") return 0;
        if (keyString == "1" || keyString == "&") return 1;
        if (keyString == "2" || keyString == "é") return 2;
        if (keyString == "3" || keyString == "\"") return 3;
        if (keyString == "4" || keyString == "'") return 4;
        if (keyString == "5" || keyString == "(") return 5;
        if (keyString == "6" || keyString == "§") return 6;
        if (keyString == "7" || keyString == "è") return 7;
        if (keyString == "8" || keyString == "!") return 8;
        if (keyString == "9" || keyString == "ç") return 9;
        return -1;
    }

    public static Vector3 MiddleOfScreenPointToWorld()
    { return MiddleOfScreenPointToWorld(Camera.main); }

    public static Vector3 MiddleOfScreenPointToWorld(Camera cam)
    {
        RaycastHit hit;
        Ray ray = cam.ScreenPointToRay(0.5f * new Vector2(Screen.width, Screen.height));
        if (Physics.Raycast(
                ray,
                out hit,
                1000f,
                Game.TERRAIN_MASK
            )) return hit.point;
        return Vector3.zero;
    }

    public static Vector3[] ScreenCornersToWorldPoints()
    { return ScreenCornersToWorld(Camera.main); }

    public static Vector3[] ScreenCornersToWorld(Camera cam)
    {
        Vector3[] corners = new Vector3[4];
        RaycastHit hit;
        for (int i = 0; i < 4; i++)
        {
            Ray ray = cam.ScreenPointToRay(new Vector2((i % 2) * Screen.width, (int)(i / 2) * Screen.height));
            if (Physics.Raycast(
                    ray,
                    out hit,
                    1000f,
                    Game.FLAT_TERRAIN_MASK
                )) corners[i] = hit.point;
        }
        return corners;
    }

    static Regex camelCaseRegex = new Regex(@"(?:[a-z]+|[A-Z]+|^)([a-z]|\d)*", RegexOptions.Compiled);
    public static string CapitalizeWords(string str)
    {
        List<string> words = new List<string>();
        MatchCollection matches = camelCaseRegex.Matches(str);
        string word;
        foreach (Match match in matches)
        {
            word = match.Groups[0].Value;
            word = word[0].ToString().ToUpper() + word.Substring(1);
            words.Add(word);
        }
        return string.Join(" ", words);
    }

    // returns distance between units minus the targets size offset
    public static float CalcUnitTargetDistance(UnitManager sourceUnit, UnitManager targetUnit)
    {
        float distance = Vector3.Distance(sourceUnit.transform.position, targetUnit.transform.position);

        distance -= (targetUnit.targetSize / 4);

        return distance;
    }

    // returns a vector position on the source units closest side of the target unit
    public static Vector3 TargetClosestPosition(UnitManager sourceUnit, UnitManager targetUnit)
    {
        Vector3 result = Vector3.MoveTowards(targetUnit.transform.position, sourceUnit.transform.position, ((targetUnit.targetSize / 2)));
        Debug.DrawLine(result, result + Vector3.up * 5f, Color.cyan, 1f);
        return result;
    }

    public static List<Vector3> PositionsAroundTarget(Vector3 target, int howMany, float radius)
    {
        List<Vector3> results = new List<Vector3>();

        float angleSection = Mathf.PI * 2f / howMany;
        for (int i = 0; i < howMany; i++)
        {
            float angle = i * angleSection;
            Vector3 newPos = target + new Vector3(Mathf.Cos(angle), target.y, Mathf.Sin(angle)) * radius;
            results.Add(newPos);
        }

        return results;
    }

    public static Vector3 GetClosePositionWithRadius(Vector3 target, float radius = 2f)
    {
        NavMeshHit hit;

        Vector3 result = Vector3.zero;

        if (NavMesh.SamplePosition(target, out hit, radius, NavMesh.AllAreas))
        {
            result = hit.position;
            Debug.Log("Found near position: " + result);
            Debug.DrawLine(result, result + Vector3.up * 5f, Color.cyan, 1f);
        }
        else
        {
            Debug.Log("No close position");
        }

        return result;
    }

    public static bool GetValidClosePositionWithRadius(CharacterManager sourceCharacter, ref Vector3 target, float radius = 3f)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = target + Random.insideUnitSphere * radius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                if (!sourceCharacter.ValidPathTo(hit.position)) continue;
                target = hit.position;
                Debug.Log("Found near position");
                Debug.DrawLine(target, target + Vector3.up * 5f, Color.cyan, 1f);
                return true;
            }
        }

        Debug.Log("No Valid Position Found");
        return false;
    }

    public static void CollectComponentsInChildren<T>(List<T> accumulator, Transform tr, string avoidTransform = null)
    {
        if (tr.name == avoidTransform) return;

        if (tr.childCount > 0)
        {
            foreach (Transform subTr in tr)
            {
                CollectComponentsInChildren<T>(accumulator, subTr, avoidTransform);
            }
        }

        if (tr.TryGetComponent<T>(out T comp))
        {
            accumulator.Add(comp);
        }

    }

    public static Vector3 RandomPointOnCircleEdge(float radius, Vector3 root)
    {
        float angle = Random.value * (2f * Mathf.PI);
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        Vector3 position = direction * radius;
        return position + root;
    }
}
