using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CrossTheRoadAgent : Agent
{
    [SerializeField]
    private float speed = 50.0f;

    [SerializeField, Tooltip("This is the offset amount from the local agent position the agent will move on every step")]
    private float stepAmount = 1.0f;

    [SerializeField]
    private Material successMaterial;

    [SerializeField]
    private Material failureMaterial;

    private CrossTheRoadGoal goal = null;

    private float overallReward = 0;
    private float overallSteps = 0;
    private Vector3 moveTo = Vector3.zero;
    private static Dictionary<int, Vector3> originalPositions = new Dictionary<int, Vector3>();
    private Rigidbody agentRigidbody;
    private bool moveInProgress = false;
    private int direction = 0;

    private int successCount = 0;  // Success counter
    private int failureCount = 0;  // Failure counter
    private const int maxEpisodes = 100;  // Maximum number of episodes

    public enum MoveToDirection
    {
        Idle,
        Left,
        Right,
        Forward
    }

    private MoveToDirection moveToDirection = MoveToDirection.Idle;

    void Awake()
    {
        goal = transform.parent.GetComponentInChildren<CrossTheRoadGoal>();
        agentRigidbody = GetComponent<Rigidbody>();

        // Store the original position for this agent instance
        if (!originalPositions.ContainsKey(GetInstanceID()))
        {
            originalPositions[GetInstanceID()] = transform.localPosition;
        }
    }

    public override void OnEpisodeBegin()
    {
        if (CompletedEpisodes >= maxEpisodes)
        {
            Debug.Log("Maximum episodes reached. Stopping.");
            Debug.Log($"Successes: {successCount}, Failures: {failureCount}");
            Application.Quit();  // Close the application after 100 runs
        }

        ResetAgent();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 3 observations - x, y, z
        sensor.AddObservation(transform.localPosition);

        // 3 observations - x, y, z
        sensor.AddObservation(goal.transform.localPosition);
    }

    void Update()
    {
        if (!moveInProgress)
            return;

        transform.localPosition = Vector3.MoveTowards(transform.localPosition, moveTo, Time.deltaTime * speed);

        if (Vector3.Distance(transform.localPosition, moveTo) <= 0.00001f)
        {
            moveInProgress = false;
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (moveInProgress)
            return;

        direction = actionBuffers.DiscreteActions[0];

        switch (direction)
        {
            case 0: // idle
                moveTo = transform.localPosition;
                moveToDirection = MoveToDirection.Idle;
                break;
            case 1: // left
                moveTo = new Vector3(transform.localPosition.x - stepAmount, transform.localPosition.y, transform.localPosition.z);
                moveToDirection = MoveToDirection.Left;
                moveInProgress = true;
                break;
            case 2: // right
                moveTo = new Vector3(transform.localPosition.x + stepAmount, transform.localPosition.y, transform.localPosition.z);
                moveToDirection = MoveToDirection.Right;
                moveInProgress = true;
                break;
            case 3: // forward
                moveTo = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z + stepAmount);
                moveToDirection = MoveToDirection.Forward;
                moveInProgress = true;
                break;
        }
    }

    public void GivePoints()
    {
        AddReward(1.0f);
        successCount++;  // Increment success counter
        UpdateStats();

        Debug.Log($"Episode {CompletedEpisodes}: Success! Total Successes: {successCount}");

        StartCoroutine(SwapGroundMaterial(successMaterial, 0.5f));
        EndEpisode();
    }

    public void TakeAwayPoints()
    {
        AddReward(-0.025f);
        failureCount++;  // Increment failure counter
        UpdateStats();

        Debug.Log($"Episode {CompletedEpisodes}: Failure. Total Failures: {failureCount}");

        StartCoroutine(SwapGroundMaterial(failureMaterial, 0.5f));
        EndEpisode();
    }

    private void ResetAgent()
    {
        // Reset this agent's position to its original position
        if (originalPositions.TryGetValue(GetInstanceID(), out Vector3 originalPosition))
        {
            transform.localPosition = moveTo = originalPosition;
            transform.localRotation = Quaternion.identity;
            agentRigidbody.velocity = Vector3.zero;
            moveInProgress = false;
        }
    }

    private void UpdateStats()
    {
        overallReward += GetCumulativeReward();
        overallSteps += StepCount;

        Debug.Log($"Cumulative Reward: {overallReward.ToString("F2")}, Total Steps: {overallSteps}");
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        //idle
        discreteActionsOut[0] = 0;

        //move left
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            discreteActionsOut[0] = 1;
        }

        //move right
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            discreteActionsOut[0] = 2;
        }

        //move forward
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            discreteActionsOut[0] = 3;
        }
    }

    private IEnumerator SwapGroundMaterial(Material material, float duration)
    {
        // Implement this method based on your requirements
        yield return new WaitForSeconds(duration);
    }
}
