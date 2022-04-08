using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    public float SpeedMult = 4.0f;
    private float MaxRobotTime = 4.0f;
    
    private int Speed;
    private Transform[] Robots;
    private List<Vector2>[] RobotPathHistory;
    private int numRobots;
    private int nextRobot;
    private float CurrentRobotTime = 0.0f;
    private bool ReceiveData = true;

    // Start is called before the first frame update
    void Start()
    {
        // estimating about 2x the speed in the air vs water
        Speed = (int)SpeedMult * this.transform.parent.gameObject.GetComponent<RobotInfo>().GetSpeed();

        // Grid is child 0, Drone is child 1
        numRobots = this.transform.parent.childCount - 2;
        Robots = new Transform[numRobots];
        RobotPathHistory = new List<Vector2>[numRobots];
        for (int i = 0; i < numRobots; i++)
        {
            Robots[i] = this.transform.parent.GetChild(i + 2);
            RobotPathHistory[i] = new List<Vector2>();
        }
        nextRobot = 0;
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
            Robots[nextRobot].gameObject.GetComponent<RobotController>().UpdateGrid(Robots[i].gameObject, RobotPathHistory[i]);
        }
    }

    void ReceiveRobotData()
    {
        RobotPathHistory[nextRobot] = Robots[nextRobot].gameObject.GetComponent<RobotController>().GetPathHistory();
    }
}
