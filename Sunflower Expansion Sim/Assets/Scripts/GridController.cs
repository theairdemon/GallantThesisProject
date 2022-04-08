using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
    public GameObject[] Robots;
    public GameObject GridNode;
    public GameObject ObstacleObject;
    public int NumObstacles;
    public Material Unknown;
    public Material Searched;
    public Material Obstacle;

    private int GridSize;
    public int[][] SearchGrid;
    public GameObject[][] GridObjects;

    int RobotCurrentX;
    int RobotCurrentZ;

    // Start is called before the first frame update
    void Start()
    {
        GridSize = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetGridSize();
        SearchGrid = new int[GridSize][];
        GridObjects = new GameObject[GridSize][];
        for (int i = 0; i < GridSize; i++)
        {
            SearchGrid[i] = new int[GridSize];
            GridObjects[i] = new GameObject[GridSize];
            for (int j = 0; j < GridSize; j++)
            {
                SearchGrid[i][j] = 0;
                Vector3 GridNodeLocation = new Vector3(i, 1, j) + this.transform.parent.position;
                GridObjects[i][j] = Instantiate(GridNode, GridNodeLocation, Quaternion.identity);
                GridObjects[i][j].GetComponent<Renderer>().material = Unknown;
            }
        }

        SpawnRandomObstacles();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateGrid();
    }

    void UpdateGrid()
    {
        for (int i = 0; i < Robots.Length; i++)
        {
            for (int j = 0; j < GridSize; j++)
            {
                for (int k = 0; k < GridSize; k++)
                {
                    if (Robots[i].GetComponent<RobotController>().GetGridValue(j, k) == 1)
                        GridObjects[j][k].GetComponent<Renderer>().material = Searched;
                    else if (Robots[i].GetComponent<RobotController>().GetGridValue(j, k) == 2)
                        GridObjects[j][k].GetComponent<Renderer>().material = Obstacle;
                }
            }
        }
    }

    void SpawnRandomObstacles()
    {
        for (int i = 0; i < NumObstacles; i++)
        {
            int RandomX = (int)Random.Range(0, GridSize);
            int RandomZ = (int)Random.Range(0, GridSize);
            Vector3 RandomLocation = new Vector3(RandomX, 1, RandomZ) + this.transform.parent.position;
            Instantiate(ObstacleObject, RandomLocation, Quaternion.identity);
        }
    }
}
