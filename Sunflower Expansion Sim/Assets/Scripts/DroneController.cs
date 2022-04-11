using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    public float SpeedMult = 4.0f;
    public float MaxRobotTime = 4.0f;

    private int Runtime;
    private int Speed;
    private Transform[] Robots;
    private List<Vector2>[] RobotPathHistory;
    private List<Vector2>[] RobotPlannedPath;
    private List<int> RobotPathIndices;
    private List<int> RobotPlannedPathIndices;
    private int numRobots;
    private int nextRobot;
    private float CurrentRobotTime = 0.0f;
    private bool ReceiveData = true;

    // Start is called before the first frame update
    void Start()
    {
        Runtime = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetRuntime();
        // estimating the speed multiplier in the air vs water
        Speed = (int)SpeedMult * this.transform.parent.gameObject.GetComponent<RobotInfo>().GetSpeed();

        // Grid is child 0, Drone is child 1
        numRobots = this.transform.parent.childCount - 2;
        Robots = new Transform[numRobots];
        RobotPlannedPath = new List<Vector2>[numRobots];
        RobotPathHistory = new List<Vector2>[numRobots];
        RobotPathIndices = new List<int>();
        RobotPlannedPathIndices = new List<int>();
        for (int i = 0; i < numRobots; i++)
        {
            Robots[i] = this.transform.parent.GetChild(i + 2);
            RobotPlannedPath[i] = new List<Vector2>();
            RobotPathHistory[i] = new List<Vector2>();
            RobotPathIndices.Add(0);
            RobotPlannedPathIndices.Add(0);
        }
        nextRobot = 0;

        StartCoroutine(CompleteSimulation());
    }

    IEnumerator CompleteSimulation()
    {
        yield return new WaitForSeconds(Runtime); //wait 5 seconds
        //Application.Quit();
        Debug.Log("Ran for " + Runtime.ToString() + " seconds");
        Debug.Log("Percent Explored:");
        Debug.Log(this.transform.parent.GetChild(0).gameObject.GetComponent<GridController>().PercentCompletion().ToString("F2"));
        Debug.Log("Robot collisions:");
        for (int i = 0; i < numRobots; i++)
        {
            Debug.Log("#" + (i + 1).ToString() + ": " + Robots[i].gameObject.GetComponent<RobotController>().GetCollisions().ToString());
        }
        Debug.Break();
    }

    // Update is called once per frame
    void Update()
    {
        MoveToRobot();
    }

    void MoveToRobot()
    {
        Vector3 globalTarget = Robots[nextRobot].position;
        globalTarget.y = this.transform.position.y;
        this.transform.position = Vector3.MoveTowards(this.transform.position, globalTarget, Speed * Time.deltaTime);

        // if the drone is at the robot, go here
        if (this.transform.position.x == globalTarget.x &&
            this.transform.position.z == globalTarget.z)
        {
            CurrentRobotTime += Time.deltaTime;
            if (CurrentRobotTime == Time.deltaTime)
            {
                TransmitRobotData();
            }
            else if (ReceiveData && CurrentRobotTime >= MaxRobotTime / 2)
            {
                ReceiveRobotData();
                ReceiveData = false;
            }
            else if (CurrentRobotTime >= MaxRobotTime)
            {
                nextRobot = (nextRobot + 1) % numRobots;
                CurrentRobotTime = 0.0f;
                ReceiveData = true;
            }
        }
    }

    void TransmitRobotData()
    {
        for (int i = 0; i < numRobots; i++)
        {
            if (i == nextRobot)
                continue;
            Robots[nextRobot].gameObject.GetComponent<RobotController>().UpdateGrid(Robots[i].gameObject, RobotPathHistory[i],
                RobotPlannedPath[i], RobotPlannedPathIndices[i]);
        }   
    }

    void ReceiveRobotData()
    {
        RobotPlannedPath[nextRobot] = Robots[nextRobot].gameObject.GetComponent<RobotController>().GetPlannedPath();
        RobotPathHistory[nextRobot] = Robots[nextRobot].gameObject.GetComponent<RobotController>().GetPathHistory(RobotPathIndices[nextRobot]);
        RobotPathIndices[nextRobot] = RobotPathHistory[nextRobot].Count - 1;
        RobotPlannedPathIndices[nextRobot] = Robots[nextRobot].gameObject.GetComponent<RobotController>().GetPathIndex();
    }
}
