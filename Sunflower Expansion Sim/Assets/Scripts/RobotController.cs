using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotController : MonoBehaviour
{
    private int Speed;
    private int GridSize;
    public int[][] SearchGrid;

    public int currentX;
    public int currentZ;

    private int PathLength;
    public List<Vector2> PlannedPath;
    List<Vector2> PathHistory;

    // Start is called before the first frame update
    void Start()
    {
        GridSize = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetGridSize();
        SearchGrid = new int[GridSize][];
        for (int i = 0; i < GridSize; i++)
        {
            SearchGrid[i] = new int[GridSize];
            for (int j = 0; j < GridSize; j++)
            {
                SearchGrid[i][j] = 0;               // 0 will represent unsearched areas
            }
        }
        currentX = Mathf.RoundToInt(this.transform.localPosition.x);
        currentZ = Mathf.RoundToInt(this.transform.localPosition.z);
        SearchGrid[currentX][currentZ] = 1;     // 1 will represent searched, non-obstacle areas

        PathLength = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetPathLength();
        PlannedPath = new List<Vector2>();
        //CreateRandomPath();
        //CreateSemiRandomPath();
        PlannedPath.Add(new Vector2(currentX, currentZ));
        PlannedPath[1] = new Vector2(currentX, currentZ);
        PathHistory = new List<Vector2>();
        PathHistory.Add(PlannedPath[0]);

        Speed = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetSpeed();
    }

    // Update is called once per frame
    void Update()
    {
        //RandomPath_NoObstacles();
        SpiralPath_NoObstacles();
        //SimpleSLAM();
        MoveAlongPath_NoObstacles();
    }

    public int GetGridValue(int x, int z)
    {
        return SearchGrid[x][z];
    }

    public List<Vector2> GetPathHistory()
    {
        return PathHistory;
    }

    public void UpdateGrid(GameObject otherRobot, List<Vector2> otherPathHistory)
    {
        for (int i = 0; i < otherPathHistory.Count; i++)
        {
            int otherX = (int)otherPathHistory[i].x;
            int otherZ = (int)otherPathHistory[i].y;
            SearchGrid[otherX][otherZ] = otherRobot.GetComponent<RobotController>().GetGridValue(otherX, otherZ);
        }
    }

    List<Vector2> AstarReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
    {
        Stack<Vector2> TotalPath = new Stack<Vector2>();
        TotalPath.Push(current);
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            TotalPath.Push(current);
        }
        List<Vector2> ListPath = new List<Vector2>(TotalPath);
        return ListPath;
    }

    void MoveAlongPath_NoObstacles()
    {
        int PlannedX = (int) PlannedPath[0].x;
        int PlannedZ = (int) PlannedPath[0].y;
        Vector3 localTarget = new Vector3(PlannedX, this.transform.localPosition.y, PlannedZ);
        Vector3 globalTarget = localTarget + this.transform.parent.position;
        this.transform.position = Vector3.MoveTowards(this.transform.position, globalTarget, Speed * Time.deltaTime);
    }

    void CreateRandomPath()
    {
        int direction = Random.Range(0, 4);
        PlannedPath[0] = NextLocation(direction, new Vector2(currentX, currentZ));
        for (int i = 1; i < PathLength; i++)
        {
            direction = Random.Range(0, 4);
            PlannedPath[i] = NextLocation(direction, PlannedPath[i - 1]);
        }
    }

    private int[] Spiral_Order = { 3, 0, 2, 1 };
    private int Spiral_Idx = 0;
    private int Spiral_Total_Moves = 1;
    private int Spiral_Count_Moves = 0;
    private bool Spiral_Change_Moves = false;
    void SpiralPath_NoObstacles()
    {
        int PlannedX = (int)PlannedPath[0].x;
        int PlannedZ = (int)PlannedPath[0].y;

        if (this.transform.localPosition.x == PlannedX &&
            this.transform.localPosition.z == PlannedZ)
        {
            currentX = PlannedX;
            currentZ = PlannedZ;
            SearchGrid[currentX][currentZ] = 1;
            PathHistory.Add(new Vector2(currentX, currentZ));

            PlannedPath[0] = PlannedPath[1];
            PlannedPath[1] = NextLocation(Spiral_Order[Spiral_Idx], PlannedPath[0]);
            Spiral_Count_Moves += 1;

            if (Spiral_Count_Moves == Spiral_Total_Moves ||
                (PlannedPath[1].x < 0 || PlannedPath[1].x > GridSize - 1 || PlannedPath[1].y < 0 || PlannedPath[1].y > GridSize - 1))
            {
                Spiral_Count_Moves = 0;
                Spiral_Idx = (Spiral_Idx + 1) % 4;
                //Debug.Log(Spiral_Order[Spiral_Idx]);

                if (PlannedPath[1].x < 0 || PlannedPath[1].x > GridSize - 1 || PlannedPath[1].y < 0 || PlannedPath[1].y > GridSize - 1)
                {
                    PlannedPath[1] = NextLocation(Spiral_Order[Spiral_Idx], PlannedPath[0]);
                }
                else
                {
                    // update number of moves for the direction
                    if (!Spiral_Change_Moves)
                    {
                        Spiral_Change_Moves = true;
                    }
                    else if (Spiral_Change_Moves)
                    {
                        Spiral_Change_Moves = false;
                        Spiral_Total_Moves += 1;
                    }
                }                
            }
        }
    }

    private Vector2 NextLocation(int direction, Vector2 currentLocation)
    {
        Vector2 nextLocation = currentLocation;
        if (direction == 0)
            nextLocation.x -= 1;
        else if (direction == 1)
            nextLocation.x += 1;
        else if (direction == 2)
            nextLocation.y -= 1;
        else if (direction == 3)
            nextLocation.y += 1;

        // Error handling to avoid leaving the grid
        //if (nextLocation.x < 0 || nextLocation.x > GridSize - 1 || nextLocation.y < 0 || nextLocation.y > GridSize - 1)
        //{
        //    return NextLocation(Spiral_Order[(direction + 1) % 4], currentLocation);
        //}

        return nextLocation;
    }


    void RandomPath_NoObstacles()
    {
        int PlannedX = (int)PlannedPath[0].x;
        int PlannedZ = (int)PlannedPath[0].y;

        if (this.transform.localPosition.x == PlannedX && 
            this.transform.localPosition.z == PlannedZ)
        {
            currentX = PlannedX;
            currentZ = PlannedZ;
            SearchGrid[currentX][currentZ] = 1;
            for (int i = 1; i < PathLength; i++)
            {
                PlannedPath[i - 1] = PlannedPath[i];
            }
            PlannedPath[PathLength - 1] = NextLocation(Random.Range(0, 4), PlannedPath[PathLength - 2]);
        }
    }

    void CreateSemiRandomPath()
    {
        int direction = Random.Range(0, 4);
        PlannedPath[0] = NextLocation(direction, new Vector2(currentX, currentZ));

        for (int i = 1; i < PathLength; i++)
        {
            direction = Random.Range(0, 4);
            PlannedPath[i] = NextLocation(direction, PlannedPath[i - 1]);
        }
    }
}
