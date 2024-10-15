using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FootpathWalk : Agent
{
    [SerializeField]
    private float speed = 50.0f;

    [SerializeField, Tooltip("This is the offset amount from the local agent position the agent will move on every step")]
    private float stepAmount = 1.0f;

    [SerializeField]
    private TextMeshProUGUI rewardValue = null;

    [SerializeField]
    private TextMeshProUGUI episodesValue = null;

    [SerializeField]
    private TextMeshProUGUI stepValue = null;

    [SerializeField]
    private Material successMaterial;

    [SerializeField]
    private Material failureMaterial;

    [SerializeField]
    private Vector3 crosswalkMinBound;

    [SerializeField]
    private Vector3 crosswalkMaxBound;

    private CrossTheRoadGoal goal = null;

    private float overallReward = 0;

    private float overallSteps = 0;

    private Vector3 moveTo = Vector3.zero;

    private static Dictionary<int, Vector3> originalPositions = new Dictionary<int, Vector3>();

    private Rigidbody agentRigidbody;

    private bool moveInProgress = false;

    private int direction = 0;

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

        // Ensure the agent stays within the crosswalk boundaries
        if (!IsWithinCrosswalk(moveTo))
        {
            moveInProgress = false;
            moveTo = transform.localPosition; // Revert to original position if move is invalid
            AddReward(-0.1f); // Penalize for attempting to move out of crosswalk
        }
    }

    private bool IsWithinCrosswalk(Vector3 position)
    {
        return position.x >= crosswalkMinBound.x && position.x <= crosswalkMaxBound.x &&
               position.z >= crosswalkMinBound.z && position.z <= crosswalkMaxBound.z;
    }

    public void GivePoints()
    {
        AddReward(1.0f);

        UpdateStats();

        StartCoroutine(SwapGroundMaterial(successMaterial, 0.5f));

        ResetAgent();
    }

    public void TakeAwayPoints()
    {
        AddReward(-0.025f);

        UpdateStats();

        StartCoroutine(SwapGroundMaterial(failureMaterial, 0.5f));

        ResetAgent();
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
        rewardValue.text = $"{overallReward.ToString("F2")}";
        episodesValue.text = $"{CompletedEpisodes}";
        stepValue.text = $"{overallSteps}";
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
