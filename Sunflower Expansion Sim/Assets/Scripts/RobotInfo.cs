using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotInfo : MonoBehaviour
{
    public GameObject Grid;

    public int GridSize = 0;
    public int PathLength = 0;
    public int Speed = 0;

    public GameObject GetGrid()
    {
        return Grid;
    }

    public int GetGridSize()
    {
        return GridSize;
    }

    public int GetPathLength()
    {
        return PathLength;
    }

    public int GetSpeed()
    {
        return Speed;
    }
}
