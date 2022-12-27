using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SquareFormation
{
    public static Vector3[,] BuildSquarePositionArray(int positionCount)
    {
        int width = 1;
        int height = 1;

        while(true)
        {
            if(width * height >= positionCount) break;
            width ++;
            if(width * height >= positionCount) break;
            height ++;
        }

        return new Vector3[width, height];
    }

    public static Vector3[,] BuildSquarePositionArray(int positionCount, Vector3 rootPosition)
    {
        Vector3[,] positions = BuildSquarePositionArray(positionCount);

        for(int i = 0; i < positions.GetLength(0); i ++)
        {
            for(int j = 0; j < positions.GetLength(1); j ++)
            {
                positions[i, j] = rootPosition;
            }
        }

        return positions;
    }
    
    private static Vector3[,] OffsetSquarePositionArray(Vector3[,] positions, float offset)
    {
        for(int i = 0; i < positions.GetLength(0); i ++)
        {
            for(int j = 0; j < positions.GetLength(1); j ++)
            {
                if(i == 0 && j == 0) continue;

                positions[i, j] += new Vector3(i * offset, 0, j * offset);
            }
        }

        return positions;
    }

    private static Vector3[,] CenterSquarePositionArray(Vector3[,] positions, int unitCount)
    {
        float squareWidth = Vector3.Distance(positions[0, 0], positions[0, positions.GetLength(1) - 1]);

        int lastRowCount = unitCount % positions.GetLength(1);

        for(int i = 0; i < positions.GetLength(0); i ++)
        {   
            if(i == positions.GetLength(0) - 1 && lastRowCount != 0)
            {
                for(int j = 0; j < lastRowCount; j ++)
                {
                    float sliceWidth;

                    if(lastRowCount % 2 == 0 && positions.GetLength(1) % 2 == 0)
                    {
                        sliceWidth = squareWidth / (positions.GetLength(1) - 1);
                    }
                    else
                    {
                        sliceWidth = (squareWidth / lastRowCount) / 2;
                    }
                    

                    positions[i, j] -= new Vector3(squareWidth / 2, 0, squareWidth / 2);
                    positions[i, j] += new Vector3(0, 0, sliceWidth);
                }
            }
            else
            {
                for(int j = 0; j < positions.GetLength(1); j ++)
                {
                    positions[i, j] -= new Vector3(squareWidth / 2, 0, squareWidth / 2);
                }
            }
        }

        return positions;
    }

    public static Vector3[,] BuildCenteredPositionArray(int unitCount, Vector3 target, float cellSeparation)
    {
        Vector3[,] positions = BuildSquarePositionArray(unitCount, target);

        positions = OffsetSquarePositionArray(positions, cellSeparation);
        
        return CenterSquarePositionArray(positions, unitCount);
    }

}
