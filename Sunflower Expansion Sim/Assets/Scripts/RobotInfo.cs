using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotInfo : MonoBehaviour
{
    public GameObject Grid;

    public int Runtime = 0;
    public bool DoSendData = false;
    public bool DoAdjustPath = false;
    public bool PrioritizeDrone = false;
    public int MaxIterations = 0;
    public int GridSize = 0;
    public int PathLength = 0;
    public int Speed = 0;
    public int RandomSeed = 42;

    public int GetRuntime()
    {
        return Runtime;
    }

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

    public int GetRandomSeed()
    {
        return RandomSeed;
    }

    public bool GetDoAdjustPath()
    {
        return DoAdjustPath;
    }
    
    public bool GetDoSendData()
    {
        return DoSendData;
    }

    public int GetMaxIterations()
    {
        return MaxIterations;
    }

    public bool GetPrioritizeDrone()
    {
        return PrioritizeDrone;
    }
}
