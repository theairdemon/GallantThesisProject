using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

public class DroneController : MonoBehaviour
{
    public float SpeedMult = 4.0f;
    public float MaxRobotTime = 4.0f;

    private int Runtime;
    private int CurrentTime;
    private int Speed;
    private Transform[] Robots;
    private List<Vector2>[] RobotPathHistory;
    private List<Vector2>[] RobotPlannedPath;
    private List<int> RobotPathIndices;
    private List<int> RobotPlannedPathIndices;
    private List<int> UnvisitedRobots;
    private int numRobots;
    private int nextRobot;
    private float CurrentRobotTime = 0.0f;
    private bool ReceiveData = true;
    private bool DoSendData = false;
    private bool PrioritizeDrone = false;
    private bool CanMove;
    private string FilePath = "";
    private int MaxIterations;

    // Start is called before the first frame update
    void Start()
    {
        DoSendData = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetDoSendData();
        PrioritizeDrone = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetPrioritizeDrone();
        Runtime = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetRuntime();
        CurrentTime = 0;
        MaxIterations = this.transform.parent.gameObject.GetComponent<RobotInfo>().GetMaxIterations();
        // estimating the speed multiplier in the air vs water
        Speed = (int)SpeedMult * this.transform.parent.gameObject.GetComponent<RobotInfo>().GetSpeed();

        // Grid is child 0, Drone is child 1
        numRobots = this.transform.parent.childCount - 2;
        Robots = new Transform[numRobots];
        RobotPlannedPath = new List<Vector2>[numRobots];
        RobotPathHistory = new List<Vector2>[numRobots];
        RobotPathIndices = new List<int>();
        RobotPlannedPathIndices = new List<int>();
        UnvisitedRobots = new List<int>();
        for (int i = 0; i < numRobots; i++)
        {
            Robots[i] = this.transform.parent.GetChild(i + 2);
            RobotPlannedPath[i] = new List<Vector2>();
            RobotPathHistory[i] = new List<Vector2>();
            RobotPathIndices.Add(0);
            RobotPlannedPathIndices.Add(0);
            UnvisitedRobots.Add(i);
        }
        nextRobot = 0;

        
        if (!PlayerPrefs.HasKey("FilePath"))
        {
            FilePath = "Assets/Outputs/" + numRobots.ToString() + "Robots_" +
                Mathf.Floor((float)(System.DateTime.Now.ToUniversalTime() - System.DateTime.Parse("04/18/2022 00:00:00").ToUniversalTime()).TotalSeconds).ToString() + ".txt";
            PlayerPrefs.SetString("FilePath", FilePath);
        }
        else
        {
            FilePath = PlayerPrefs.GetString("FilePath");
        }

        // Keys: TimesRun and DoSendData
        if (!PlayerPrefs.HasKey("TimesRun"))
            PlayerPrefs.SetInt("TimesRun", 0);

        if (!PlayerPrefs.HasKey("AdjustPath"))
            PlayerPrefs.SetInt("AdjustPath", 0);
        else
        {
            DoSendData = PlayerPrefs.GetInt("AdjustPath") > 0;
            PrioritizeDrone = PlayerPrefs.GetInt("AdjustPath") > 2;
        }            

    }

    // Update is called once per frame
    void Update()
    {
        MoveToRobot();
        // stabilization
        CheckMoveNow();
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
            if (DoSendData)
            {
                if (CurrentRobotTime == Time.deltaTime)
                {
                    TransmitRobotData();
                }
                else if (ReceiveData && CurrentRobotTime >= MaxRobotTime / 2)
                {
                    ReceiveRobotData();
                    ReceiveData = false;
                }
            }
            
            if (CurrentRobotTime >= MaxRobotTime)
            {
                if (!PrioritizeDrone)
                    nextRobot = (nextRobot + 1) % numRobots;
                else
                    nextRobot = PriorityNextTarget();
                CurrentRobotTime = 0.0f;
                ReceiveData = true;
            }
        }
    }

    int PriorityNextTarget()
    {
        if (UnvisitedRobots.Count > 0)
        {
            int idx = 0;
            int closestIdx = 10000;
            int closestRobot = UnvisitedRobots[Random.Range(0, UnvisitedRobots.Count)];
            for (int i = 0; i < UnvisitedRobots.Count; i++)
            {
                idx = UnvisitedRobots[i];
                if (RobotPathHistory[i].Count < 0)
                    continue;

                int PathIdx = RobotPathIndices[nextRobot];
                int otherPathIdx = RobotPathIndices[idx];
                int commonMovesLeft = Mathf.Min((RobotPlannedPath[nextRobot].Count - PathIdx), (RobotPlannedPath[idx].Count - otherPathIdx)) - 1;

                if (commonMovesLeft <= 0)
                    continue;

                for (int j = 1; j < commonMovesLeft; j++)
                {
                    if (PathIdx + j > RobotPlannedPath[nextRobot].Count - 1 || otherPathIdx + j > RobotPlannedPath[idx].Count - 1)
                        break;
                    Vector2 currentRobotMove = RobotPlannedPath[nextRobot][PathIdx + j];
                    Vector2 otherRobotMove = RobotPlannedPath[idx][otherPathIdx + j];

                    // if same location
                    if (currentRobotMove == otherRobotMove && j < commonMovesLeft - 1 && otherPathIdx + j < closestIdx)
                    {
                        closestIdx = otherPathIdx + j;
                        closestRobot = idx;
                    }
                }
            }
            UnvisitedRobots.Remove(closestRobot);
            return closestRobot;
        }
        else
        {
            for (int i = 0; i < numRobots; i++)
                UnvisitedRobots.Add(i);
        }
        return (nextRobot + 1) % numRobots;
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

    void CheckMoveNow()
    {
        CanMove = true;
        for (int i = 0; i < numRobots; i++)
        {
            CanMove = CanMove && !Robots[i].gameObject.GetComponent<RobotController>().GetMoveNow();
        }

        if (CanMove)
        {
            CurrentTime += 1;
            if (CurrentTime > Runtime)
                CompleteSimulation();

            for (int i = 0; i < numRobots; i++)
            {
                Robots[i].gameObject.GetComponent<RobotController>().SetMoveNow(true);
            }
        }
    }

    public void CompleteSimulation()
    {
        //yield return new WaitForSeconds(Runtime); //wait 5 seconds
        //Debug.Log("Ran for " + Runtime.ToString() + " moves");
        float PercentCompletion = this.transform.parent.GetChild(0).gameObject.GetComponent<GridController>().PercentCompletion();
        //Debug.Log("Percent explored: " + PercentCompletion.ToString("F2"));
        //Debug.Log("Robot collisions:");
        int totalCollisions = 0;
        for (int i = 0; i < numRobots; i++)
        {
            totalCollisions += Robots[i].gameObject.GetComponent<RobotController>().GetCollisions();
            //Debug.Log("#" + (i + 1).ToString() + ": " + Robots[i].gameObject.GetComponent<RobotController>().GetCollisions().ToString());
        }
        //Debug.Log("Total Collisions: " + totalCollisions.ToString());

        Debug.Log("Iteration: " + (10 * PlayerPrefs.GetInt("AdjustPath") + PlayerPrefs.GetInt("TimesRun")).ToString() + "\n" +
            "Ran for " + Runtime.ToString() + " moves\n" +
            "Percent explored: " + PercentCompletion.ToString("F2") + "\n" +
            "Total Collisions: " + totalCollisions.ToString());

        
        // File format: numRobots, Runtime, DoSendData, DoAdjustPath, PercentCompletion, totalCollisions 
        // File management help: https://answers.unity.com/questions/1067541/how-to-append-a-string-to-a-file-if-the-file-exist.html 
        //Debug.Log(FilePath);
        if (!File.Exists(FilePath))
        {
            using (StreamWriter write = File.CreateText(FilePath))
            {
                write.WriteLine(numRobots.ToString() + "," +
                    Runtime.ToString() + "," +
                    DoSendData.ToString() + "," +
                    (PlayerPrefs.GetInt("AdjustPath") > 1).ToString() + "," +
                    PrioritizeDrone.ToString() + "," +
                    PercentCompletion.ToString("F2") + "," +
                    totalCollisions.ToString());
                write.Close();
            }
        }
        else
        {
            using (StreamWriter write = File.AppendText(FilePath))
            {
                write.WriteLine(numRobots.ToString() + "," +
                    MaxRobotTime.ToString() + "," +
                    DoSendData.ToString() + "," +
                    (PlayerPrefs.GetInt("AdjustPath") > 1).ToString() + "," +
                    PrioritizeDrone.ToString() + "," +
                    PercentCompletion.ToString("F2") + "," +
                    totalCollisions.ToString());
                write.Close();
            }
        }


        PlayerPrefs.SetInt("TimesRun", PlayerPrefs.GetInt("TimesRun") + 1);
        if (PlayerPrefs.GetInt("TimesRun") >= MaxIterations)
        {
            PlayerPrefs.SetInt("TimesRun", 0);
            PlayerPrefs.SetInt("AdjustPath", PlayerPrefs.GetInt("AdjustPath") + 1);
        }

        if (PlayerPrefs.GetInt("AdjustPath") > 3)
        {
            PlayerPrefs.DeleteAll();
            Debug.Break();
        }            

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
    }
}
