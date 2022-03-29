using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
    public GameObject[] Robots;
    public GameObject GridNode;
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

        for (int i = 0; i < Robots.Length; i++)
        {
            RobotCurrentX = Robots[i].GetComponent<RobotController>().currentX;
            RobotCurrentZ = Robots[i].GetComponent<RobotController>().currentZ;
            SearchGrid[RobotCurrentX][RobotCurrentZ] = Robots[i].GetComponent<RobotController>().SearchGrid[RobotCurrentX][RobotCurrentZ];
            GridObjects[RobotCurrentX][RobotCurrentZ].GetComponent<Renderer>().material = Searched;
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < Robots.Length; i++)
        {
            RobotCurrentX = Robots[i].GetComponent<RobotController>().currentX;
            RobotCurrentZ = Robots[i].GetComponent<RobotController>().currentZ;
            SearchGrid[RobotCurrentX][RobotCurrentZ] = Robots[i].GetComponent<RobotController>().SearchGrid[RobotCurrentX][RobotCurrentZ];

            if (SearchGrid[RobotCurrentX][RobotCurrentZ] == 1)
                GridObjects[RobotCurrentX][RobotCurrentZ].GetComponent<Renderer>().material = Searched;
            else if (SearchGrid[RobotCurrentX][RobotCurrentZ] == 2)
                GridObjects[RobotCurrentX][RobotCurrentZ].GetComponent<Renderer>().material = Obstacle;
        }
    }
}
