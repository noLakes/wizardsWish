using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialGrid
{
    public float cellWidth { get; private set; }

    // actual data structure for sorting cells by position
    public Dictionary<(int x, int z), Cell> grid { get; }

    public SpatialGrid(float cellWidth, int setupWidth = 0)
    {
        this.cellWidth = cellWidth;
        grid = new Dictionary<(int x, int z), Cell>();

        // initialize grid cells in a square with width == setupWidth if supplied
        if(setupWidth > 0) SetupCells(setupWidth);
    }

    // place agent into grid based on position - create new cell if needed
    public void AddAgent(GameObject agent)
    {
        var key = GetKey(agent.transform.position);
        Cell cell;

        if (grid.ContainsKey(key))
        {
            cell = grid[key];
            cell.agents.Add(agent);
        }
        else
        {
            cell = new Cell(key, this, agent);
            grid[key] = cell;
        }

        //agent.currentCell = cell;
    }

    public void RemoveAgent(GameObject agent)
    {
        var key = GetKey(agent.transform.position);
        grid[key].agents.Remove(agent);
    }

    // converts a Vector2 position to a grid key 
    // divides posiitonal floats by cell width to produce root position coordinates
    // uses FloorToInt/CeilToInt to convert floats to ints that can be used as simplified keys
    //public (int x, int y) GetKey(Vector2 pos) => (Mathf.FloorToInt(pos.x / cellWidth), Mathf.CeilToInt(pos.y / cellWidth));
    public (int x, int z) GetKey(Vector3 pos) => (Mathf.FloorToInt(pos.x / cellWidth), Mathf.CeilToInt(pos.z / cellWidth));
    public (int x, int z) GetKey(float x, float z) => (Mathf.FloorToInt(x / cellWidth), Mathf.CeilToInt(z / cellWidth));

    private void SetupCells(int width)
    {
        float xStart = -((width / 2) * cellWidth);
        float zStart = (width / 2) * cellWidth;
        
        for(int z = 0; z < width; z ++)
        {
            for(int x = 0; x < width; x ++)
            {
                float xPos = xStart + (x * cellWidth);
                float zPos = zStart - (z * cellWidth);
                var key = GetKey(xPos, zPos);

                grid[key] = new Cell(key, this);
            }
        }
    }

    public void Reset()
    {
        foreach(Cell cell in grid.Values)
        {
            cell.agents.Clear();
        }
    }
}
