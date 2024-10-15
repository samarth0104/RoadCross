using UnityEngine;

public class CrossTheRoadGoal : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponent<CrossTheRoadAgent>();
        if (agent != null)
        {
            Debug.Log("Points earned as road was crossed");
            agent.GivePoints();

        }
    }
}
