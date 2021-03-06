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

    System.Random rand;

    // Start is called before the first frame update
    void Start()
    {
        //Random.InitState(this.transform.parent.gameObject.GetComponent<RobotInfo>().GetRandomSeed());
        rand = new System.Random(this.transform.parent.gameObject.GetComponent<RobotInfo>().GetRandomSeed());
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
        
    }

    public void ChangeGrid(int j, int k, int z)
    {
        SearchGrid[j][k] = z;
        if (z == 1)
            GridObjects[j][k].GetComponent<Renderer>().material = Searched;
        else if (z == 2)
            GridObjects[j][k].GetComponent<Renderer>().material = Obstacle;
    }

    public float PercentCompletion()
    {
        float totalGridNodes = GridSize * GridSize;
        float numExplored = 0;
        for (int j = 0; j < GridSize; j++)
        {
            for (int k = 0; k < GridSize; k++)
            {
                if (SearchGrid[j][k] != 0)
                    numExplored += 1;
            }
        }
        float percentExplored = (numExplored / totalGridNodes) * 100;
        return percentExplored;
    }

    void SpawnRandomObstacles()
    {
        for (int i = 0; i < NumObstacles; i++)
        {
            int RandomX = rand.Next(GridSize);
            int RandomZ = rand.Next(GridSize);
            Vector3 RandomLocation = new Vector3(RandomX, 1, RandomZ) + this.transform.parent.position;
            Instantiate(ObstacleObject, RandomLocation, Quaternion.identity);
        }
    }
}
