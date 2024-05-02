using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField]
    private float moveSpeed = 1;

    [Header("Less important stats")]
    [SerializeField]
    private float turnSpeed = 0.05f;

    [SerializeField]
    private LayerMask layerMask;

    private NavMeshAgent agent;
    private EnemyAttacker attacker;
    private EnemyAnimator animator;

    private Vector3 currentTarget;

    private void OnEnable()
    {
        attacker = GetComponent<EnemyAttacker>();
        animator = GetComponent<EnemyAnimator>();
        agent = GetComponent<NavMeshAgent>();

        agent.speed = moveSpeed;
    }

    private void Update()
    {
        if (agent.remainingDistance < agent.stoppingDistance)
        {
            StartAttacking();
        }
    }

    public void FindNewTarget()
    {

    }

    public void SetPathTarget(Vector3 target)
    {
        if (attacker.Attacking)
        {
            return;
        }

        currentTarget = target;

        UpdateNavAgent();
    }

    private void UpdateNavAgent()
    {
        animator.Move();
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(currentTarget);
        }
        else
        {
            Debug.LogError("Enemy not on navmesh");
        }
    }

    private void Building_OnDeath(Building building)
    {
        StopAttacking();
        FindNewTarget();
    }

    private void StartAttacking()
    {
        animator.StopMoving();
        attacker.StartAttacking();
    }

    private void StopAttacking()
    {
        attacker.StopAttacking();
        animator.Move();
    }
}