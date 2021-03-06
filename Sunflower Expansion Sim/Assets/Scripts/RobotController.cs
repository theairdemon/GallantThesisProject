using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotController : MonoBehaviour
{
    public int RandomModifer = 0;
    private int Speed;
    private int GridSize;
    public int[][] SearchGrid;

    private int currentX;
    private int currentZ;

    // Path variables
    private int PathLength;
    public List<Vector2> PlannedPath;
    List<Vector2> PathHistory;
    List<Vector2> GoalLocations;
    public int PathIdx = 0;

    // Information received from drone about other robots
    List<List<Vector2>> OtherPlannedPaths;
    List<GameObject> OtherRobots;
    List<int> OtherIndices;

    bool DoAdjustPath;
    int NumRobotCollisions;
    public bool MoveNow;

    System.Random rand;
    int RandSeed;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey("AdjustPath"))
            DoAdjustPath = PlayerPrefs.GetInt("AdjustPath") > 1;
        else
            DoAdjustPath = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetDoAdjustPath();

        MoveNow = true;
        RandSeed = RandomModifer + this.transform.parent.gameObject.GetComponent<RobotInfo>().GetRandomSeed();
        rand = new System.Random(RandSeed);
        NumRobotCollisions = 0;

        // SearchGrid
        GridSize = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetGridSize();
        SearchGrid = new int[GridSize][];
        GoalLocations = new List<Vector2>();
        for (int i = 0; i < GridSize; i++)
        {
            SearchGrid[i] = new int[GridSize];
            for (int j = 0; j < GridSize; j++)
            {
                SearchGrid[i][j] = 0;               // 0 will represent unsearched areas
                GoalLocations.Add(new Vector2(i, j));
            }
        }
        currentX = Mathf.RoundToInt(this.transform.localPosition.x);
        currentZ = Mathf.RoundToInt(this.transform.localPosition.z);
        SearchGrid[currentX][currentZ] = 1;     // 1 will represent searched, non-obstacle areas

        // Robot States
        OtherPlannedPaths = new List<List<Vector2>>();
        OtherRobots = new List<GameObject>();
        OtherIndices = new List<int>();

        // PlannedPath
        PathLength = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetPathLength();
        PlannedPath = new List<Vector2>();
        PlannedPath.Add(new Vector2(currentX, currentZ));
        for (int i = 0; i < PathLength; i++)
        {
            Vector2 RandomGoal = GetRandomGoal();
            PlannedPath.AddRange(AstarSearch(PlannedPath[PlannedPath.Count - 1], RandomGoal));
        }
        PathHistory = new List<Vector2>();
        PathHistory.Add(PlannedPath[0]);

        Speed = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetSpeed();
    }

    void Update()
    {
        RunAstarSearch();
        MoveAlongPath_Astar();
    }

    // ===================================
    // GRID UPDATES
    // ===================================
    public int GetGridValue(int x, int z)
    {
        return SearchGrid[x][z];
    }

    public List<Vector2> GetPathHistory(int StartingIndex)
    {
        return PathHistory.GetRange(StartingIndex, PathHistory.Count - StartingIndex);
    }

    public List<Vector2> GetPlannedPath()
    {
        return PlannedPath;
    }

    public int GetPathIndex()
    {
        return PathIdx;
    }

    public int GetCollisions()
    {
        return NumRobotCollisions;
    }

    public bool GetMoveNow()
    {
        return MoveNow;
    }

    public void SetMoveNow(bool newValue)
    {
        MoveNow = newValue;
    }

    public void UpdateGrid(GameObject otherRobot, List<Vector2> otherPathHistory, List<Vector2> otherPlannedPath, int otherIdx)
    {
        // Store robots, plannedpaths, and get their index
        int robotIdx;
        if (!OtherRobots.Contains(otherRobot))
        {
            OtherRobots.Add(otherRobot);
            OtherIndices.Add(otherIdx);
            robotIdx = OtherRobots.Count - 1;
            OtherPlannedPaths.Add(otherPlannedPath);
        }
        else
        {
            robotIdx = OtherRobots.IndexOf(otherRobot);
            OtherIndices[robotIdx] = otherIdx;
            OtherPlannedPaths[robotIdx] = otherPlannedPath;
        }            
        
        // Store the most recent path history
        for (int i = 0; i < otherPathHistory.Count; i++)
        {
            int otherX = (int)otherPathHistory[i].x;
            int otherZ = (int)otherPathHistory[i].y;
            SearchGrid[otherX][otherZ] = Mathf.Max(1, SearchGrid[otherX][otherZ]);
            if (GoalLocations.Contains(new Vector2(otherX, otherZ)))
                GoalLocations.Remove(new Vector2(otherX, otherZ));
        }

        if (DoAdjustPath)
            AdjustPath();
    }

    // ===================================
    // OBSTACLES
    // ===================================
    private void OnTriggerEnter(Collider collider)
    {
        if (PathHistory.Count <= 1)
            return;

        int StartX = (int)PathHistory[PathHistory.Count - 1].x;
        int StartZ = (int)PathHistory[PathHistory.Count - 1].y;
        int PlannedX = (int)PlannedPath[PathIdx].x;
        int PlannedZ = (int)PlannedPath[PathIdx].y;

        if (collider.gameObject.tag == "Obstacle")
        {
            this.transform.parent.GetChild(0).gameObject.GetComponent<GridController>().ChangeGrid(PlannedX, PlannedZ, 2);
            SearchGrid[PlannedX][PlannedZ] = 2;
            if (PathHistory[PathHistory.Count - 1] != new Vector2(PlannedX, PlannedZ))
                PathHistory.Add(new Vector2(PlannedX, PlannedZ));
            if (GoalLocations.Contains(new Vector2(PlannedX, PlannedZ)))
                GoalLocations.Remove(new Vector2(PlannedX, PlannedZ));
            if (PathIdx < PlannedPath.Count - 2)
            {
                // remove the collision location
                PlannedPath.RemoveAt(PathIdx);
                // insert new search at current index
                PlannedPath.InsertRange(PathIdx, AstarSearch(PlannedPath[PathIdx - 1], PlannedPath[PathIdx]));
                // remove double of previous location
                PlannedPath.RemoveAt(PathIdx - 1);
            }            
        }
        else
        {
            NumRobotCollisions++;
            if (PathIdx < PlannedPath.Count - 2)
            {
                // remove the collision location
                PlannedPath.RemoveAt(PathIdx);
                // insert new search at current index
                PlannedPath.InsertRange(PathIdx, AstarSearch(PlannedPath[PathIdx - 1], PlannedPath[PathIdx]));
                // remove double of previous location
                PlannedPath.RemoveAt(PathIdx - 1);
            }
        }

        if (DoAdjustPath)
            AdjustPath();
    }

    // ===================================
    // ANYTIME PLANNING
    // Source: Anytime Planning for Decentralized Multirobot 
    // Active Information Gathering, Schlotfeldt et. al.
    //
    // This algorithm is a modified version of ImprovePath() from
    // the paper, since we cannot actually be in constant communication
    // between our robots in this system
    // ===================================
    void AdjustPath()
    {
        int otherPathIdx, commonMovesLeft;
        for (int i = 0; i < OtherIndices.Count; i++)
        {
            otherPathIdx = OtherIndices[i];
            commonMovesLeft = Mathf.Min((PlannedPath.Count - PathIdx), (OtherPlannedPaths[i].Count - otherPathIdx)) - 1;

            if (commonMovesLeft <= 0)
                continue;

            for (int j = 1; j < commonMovesLeft; j++)
            {
                if (PathIdx + j > PlannedPath.Count - 1 || otherPathIdx + j > OtherPlannedPaths[i].Count - 1)
                    break;
                Vector2 currentRobotMove = PlannedPath[PathIdx + j];
                Vector2 otherRobotMove = OtherPlannedPaths[i][otherPathIdx + j];

                // if same location
                if (currentRobotMove == otherRobotMove && j < commonMovesLeft - 1)
                {
                    //Debug.Log(currentRobotMove.ToString() + " in " + j.ToString() + " moves");
                    // remove the collision location
                    PlannedPath.RemoveAt(PathIdx + j);
                    // temporarily set collision location as an obstacle
                    int tempGridValue = SearchGrid[(int)otherRobotMove.x][(int)otherRobotMove.y];
                    SearchGrid[(int)otherRobotMove.x][(int)otherRobotMove.y] = 2;
                    // insert new search at current index
                    PlannedPath.InsertRange(PathIdx + j, AstarSearch(PlannedPath[PathIdx + j - 1], PlannedPath[PathIdx + j]));
                    // remove double of previous location
                    PlannedPath.RemoveAt(PathIdx + j - 1);
                    // reset location value at collision location
                    SearchGrid[(int)otherRobotMove.x][(int)otherRobotMove.y] = tempGridValue;
                }
            }
        }
    }

    void MoveOtherIndices()
    {
        for (int i = 0; i < OtherIndices.Count; i++)
        {
            OtherIndices[i] = Mathf.Min(OtherPlannedPaths[i].Count - 1, OtherIndices[i] + 1);
        }
    }

    // ===================================
    // A* SEARCH
    // ===================================
    void RunAstarSearch()
    {
        int PlannedX = (int)PlannedPath[PathIdx].x;
        int PlannedZ = (int)PlannedPath[PathIdx].y;
        Vector3 localTarget = new Vector3(PlannedX, 1, PlannedZ);
        if (this.transform.localPosition == localTarget)
        {
            MoveNow = false;
            PathIdx += 1;
            MoveOtherIndices();

            if (GoalLocations.Contains(new Vector2(PlannedX, PlannedZ)))
                GoalLocations.Remove(new Vector2(PlannedX, PlannedZ));
            if (SearchGrid[PlannedX][PlannedZ] != 2)
            {
                this.transform.parent.GetChild(0).gameObject.GetComponent<GridController>().ChangeGrid(PlannedX, PlannedZ, 1);
                SearchGrid[PlannedX][PlannedZ] = 1;
            }                

            if (PathHistory[PathHistory.Count - 1] != new Vector2(PlannedX, PlannedZ))
                PathHistory.Add(new Vector2(PlannedX, PlannedZ));

            if (PathIdx == PlannedPath.Count - 1)
            {
                PlannedPath = new List<Vector2>();
                PlannedPath.Add(new Vector2(PlannedX, PlannedZ));
                for (int i = 0; i < PathLength; i++)
                {
                    Vector2 RandomGoal = GetRandomGoal();
                    PlannedPath.AddRange(AstarSearch(PlannedPath[PlannedPath.Count - 1], RandomGoal));
                }
                PathIdx = 0;
                //MoveOtherIndices();
                if (DoAdjustPath)
                    AdjustPath();
            }
        }
    }

    void MoveAlongPath_Astar()
    {
        if (MoveNow)
        {
            int PlannedX = (int)PlannedPath[PathIdx].x;
            int PlannedZ = (int)PlannedPath[PathIdx].y;
            Vector3 localTarget = new Vector3(PlannedX, 1, PlannedZ);
            Vector3 globalTarget = localTarget + this.transform.parent.position;
            this.transform.position = Vector3.MoveTowards(this.transform.position, globalTarget, Speed * Time.deltaTime);
        }        
    }

    Vector2 GetRandomGoal()
    {
        rand = new System.Random(RandSeed);
        if (GoalLocations.Count > 0)
        {
            return GoalLocations[rand.Next(GoalLocations.Count)];
        }

        int RandomX = rand.Next(GridSize);
        int RandomZ = rand.Next(GridSize);
        return new Vector2(RandomX, RandomZ);
    }

    List<Vector2> AstarReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
    {
        Stack<Vector2> TotalPath = new Stack<Vector2>();
        TotalPath.Push(current);
        while (cameFrom.ContainsKey(current))
        {
            //SearchGrid[(int)current.x][(int)current.y] = 1;
            current = cameFrom[current];
            TotalPath.Push(current);
        }
        List<Vector2> ListPath = new List<Vector2>(TotalPath);
        return ListPath;
    }

    List<Vector2> AstarSearch(Vector2 start, Vector2 goal)
    {
        SortedList<float, Vector2> toExplore = new SortedList<float, Vector2>();
        toExplore.Add(0, start);

        Dictionary<Vector2, Vector2> cameFrom = new Dictionary<Vector2, Vector2>();

        Dictionary<Vector2, int> gScore = new Dictionary<Vector2, int>();
        gScore[start] = 0;
        Dictionary<Vector2, float> fScore = new Dictionary<Vector2, float>();
        fScore[start] = Manhattan(start, goal);

        while (toExplore.Count > 0)
        {
            Vector2 current = toExplore.Values[0];
            if (current == goal)
                return AstarReconstructPath(cameFrom, current);
            toExplore.RemoveAt(0);

            for (int i = 0; i < 4; i++)
            {
                Vector2 neighbor = NextLocation(i, current);
                if (neighbor.x < 0 || neighbor.x > GridSize - 1 || neighbor.y < 0 || neighbor.y > GridSize - 1)
                    continue;
                else if (SearchGrid[(int)neighbor.x][(int)neighbor.y] == 2)
                    continue;

                int tempGScore = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor))
                    gScore[neighbor] = 10000;
                
                if (tempGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tempGScore;
                    // fScore = gScore + heuristic + try to avoid (when possible) going over places you've already been
                    fScore[neighbor] = tempGScore + Manhattan(neighbor, goal) 
                        + (SearchGrid[(int)neighbor.x][(int)neighbor.y] * 10000);
                    while (toExplore.ContainsKey(fScore[neighbor]))
                        fScore[neighbor] += 0.001f;
                    toExplore.Add(fScore[neighbor], neighbor);
                }
            }
        }

        return new List<Vector2>();
    }

    int Manhattan(Vector2 point1, Vector2 point2)
    {
        return ((int) (Mathf.Abs(point1.x - point2.x) + Mathf.Abs(point1.y - point2.y)));
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

        return nextLocation;
    }

    // ===================================
    // BEFORE A* WAS ADDED
    // ===================================
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
        int direction = rand.Next(4);
        PlannedPath[0] = NextLocation(direction, new Vector2(currentX, currentZ));
        for (int i = 1; i < PathLength; i++)
        {
            direction = rand.Next(4);
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
            PlannedPath[PathLength - 1] = NextLocation(rand.Next(4), PlannedPath[PathLength - 2]);
        }
    }

    void CreateSemiRandomPath()
    {
        int direction = rand.Next(4);
        PlannedPath[0] = NextLocation(direction, new Vector2(currentX, currentZ));
        
        for (int i = 1; i < PathLength; i++)
        {
            direction = rand.Next(4);
            PlannedPath[i] = NextLocation(direction, PlannedPath[i - 1]);
        }
    }
}
