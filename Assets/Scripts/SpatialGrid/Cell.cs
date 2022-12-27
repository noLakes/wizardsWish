using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    // top left corner of cell, treated as initial point
    public Vector3 root { get; private set; }

    // key for cell location in parent grid dictionary - generated based on position
    public (int x, int z) key { get; private set; }

    // agents currently in this cells bounds
    public List<GameObject> agents { get; private set; }

    // parent grid
    private SpatialGrid grid;

    // cached list of neighbor cells
    public List<Cell> neighbors { get; private set; }

    // offsets that can be applied to this cells key to generate keys for neighbors 
    public static (int x, int z)[] neighborOffsets = new (int x, int z)[8]
    {
        (-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0), (1, -1), (0, -1), (-1, -1)
    };

    // cached cell corners used for bounds checks
    public Vector3 tl; // top left
    public Vector3 tr; // top right
    public Vector3 br; // bottom right
    public Vector3 bl; // bottom left

    public Cell((int x, int z) key, SpatialGrid grid, GameObject firstAgent = null)
    {
        this.key = key;

        // convert key back into vector2 position for root assignment
        root = new Vector3(key.x * grid.cellWidth, 0f, key.z * grid.cellWidth);

        this.grid = grid;
        tl = root;
        tr = new Vector3(root.x + grid.cellWidth, 0f, root.z);
        br = new Vector3(root.x + grid.cellWidth, 0f, root.z - grid.cellWidth);
        bl = new Vector3(root.x, 0f, root.z - grid.cellWidth);

        agents = new List<GameObject>();
        if (firstAgent) agents.Add(firstAgent);

        neighbors = new List<Cell>();
        // get neighboring cell references
        MeetNeighbors();
    }

    // fills GridAgent neighbor list with this cells agents and neighboring cell agents until limit is reached
    public void CollectNeighborAgentsInRange(List<GameObject> list, int limit, Vector3 pos, float range)
    {
        // collect in range agents from this cell
        CollectAgentsInRange(list, limit, pos, range);

        // collect agents from neighbor cells until list is full or all are traversed
        for (int i = 0; i < neighbors.Count && list.Count < limit; i++)
        {
            neighbors[i].CollectAgentsInRange(list, limit, pos, range);
        }
    }

    // collect in range agents from this cell
    public void CollectAgentsInRange(List<GameObject> list, int limit, Vector3 pos, float range)
    {
        for (int i = 0; i < agents.Count && list.Count < limit; i++)
        {
            if (Vector3.Distance(agents[i].transform.position, pos) <= range) list.Add(agents[i]);
        }
    }

    public void CollectNeighborTagInRange(List<GameObject> list, string tag, Vector3 pos, float range)
    {
        // collect trees from this cell
        CollectTagInRange(list, tag, pos, range);

        // collect agents from neighbor cells until list is full or all are traversed
        for (int i = 0; i < neighbors.Count; i++)
        {
            neighbors[i].CollectTagInRange(list, tag, pos, range);
        }
    }

    public void CollectTagInRange(List<GameObject> list, string tag, Vector3 pos, float range)
    {
        foreach(GameObject go in agents)
        {
            if (go.tag == tag && Vector3.Distance(go.transform.position, pos) <= range)
            {
                list.Add(go);
            }
        }
    }

    // check if position is inside cell bounds
    public bool InBounds(Vector3 pos)
    {
        return pos.x > root.x && pos.x < tr.x && pos.z < root.z && pos.z > bl.z;
    }

    // collect references to neighbor cells and supply them with reference to this cell
    private void MeetNeighbors()
    {
        for (int i = 0; i < neighborOffsets.Length; i++)
        {
            var nKey = (key.x + neighborOffsets[i].x, key.z + neighborOffsets[i].z);
            if (grid.grid.ContainsKey(nKey))
            {
                Cell newNeighbor = grid.grid[nKey];
                neighbors.Add(newNeighbor);
                newNeighbor.AddNeighbor(this);
            }
        }
    }

    public void AddNeighbor(Cell cell) => neighbors.Add(cell);
}
