using UnityEngine;
using UnityEngine.AI;

public class UnitMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float stoppingDistance = 0.5f;
    
    private NavMeshAgent navAgent;
    private Vector3 targetPosition;
    private bool hasTarget = false;
    
    private void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = stoppingDistance;
        }
    }
    
    public void MoveTo(Vector3 position)
    {
        targetPosition = position;
        hasTarget = true;
        
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(position);
        }
    }
    
    private void Update()
    {
        if (navAgent != null && hasTarget)
        {
            if (navAgent.remainingDistance <= stoppingDistance)
            {
                hasTarget = false;
                Debug.Log("Unit reached destination!");
            }
        }
    }
}