using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotController : MonoBehaviour
{
    private int GridSize;
    public int[][] SearchGrid;

    public int currentX;
    public int currentZ;

    private int PathLength;
    public Vector2[] PlannedPath;

    private int Speed;


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
        PlannedPath = new Vector2[PathLength];
        //CreateRandomPath();
        CreateSemiRandomPath();

        Speed = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetSpeed();
    }

    // Update is called once per frame
    void Update()
    {
        RandomPath_NoObstacles();
        MoveAlongPath_NoObstacles();
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
        if (nextLocation.x < 0 || nextLocation.x > GridSize - 1 || nextLocation.y < 0 || nextLocation.y > GridSize - 1)
            return NextLocation(Random.Range(0, 4), currentLocation);

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
            //PlannedPath[PathLength - 1] = NextLocation(Random.Range(0, 4), PlannedPath[PathLength - 2]);
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
