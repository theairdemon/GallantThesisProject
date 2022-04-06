using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    Transform[] Robots;
    int numRobots;
    int nextRobot;

    private int Speed;

    // Start is called before the first frame update
    void Start()
    {
        Speed = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetSpeed();

        // Grid is child 0, Drone is child 1
        numRobots = this.transform.parent.childCount - 2;
        Robots = new Transform[numRobots];
        for (int i = 0; i < numRobots; i++)
        {
            Robots[i] = this.transform.parent.GetChild(i + 2);
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

        if (this.transform.position.x == globalTarget.x &&
            this.transform.position.z == globalTarget.z)
        {
            nextRobot = (nextRobot + 1) % numRobots;
        }
    }
}
