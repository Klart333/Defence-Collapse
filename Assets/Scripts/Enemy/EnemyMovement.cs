using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    [Header("Walking Offset")]
    [SerializeField]
    private float groundOffset = 0.1f;

    [SerializeField]
    private Vector2 horizontalOffsetRange;

    private NavMeshAgent agent;
    private BuildingManager buildingManager;
    private Building currentTargetBuilding;
    private EnemyAttacker attacker;
    private EnemyAnimator animator;
    private EnemyHealth health;

    private Vector3 currentTarget;

    private float horizontalOffset = 0;

    private void OnEnable()
    {
        horizontalOffset = UnityEngine.Random.Range(horizontalOffsetRange.x, horizontalOffsetRange.y);

        buildingManager = FindObjectOfType<BuildingManager>();
        attacker = GetComponent<EnemyAttacker>();
        animator = GetComponent<EnemyAnimator>();
        health = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();

        GameEvents.OnEnemyPathUpdated += OnPathUpdated;
        GameEvents.OnFightStarted += OnFightStarted;
        GameEvents.OnFightEnded += OnFightEnded;

        agent.speed = moveSpeed;
    }

    private void Start()
    {
        health.Health.OnDeath += Health_OnDeath;
    }

    private void Health_OnDeath(GameObject obj)
    {
        StopAllCoroutines();
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyPathUpdated -= OnPathUpdated;
        GameEvents.OnFightStarted -= OnFightStarted;
        GameEvents.OnFightEnded -= OnFightEnded;

        if (currentTargetBuilding != null)
        {
            currentTargetBuilding.OnDeath -= Building_OnDeath;
        }
    }

    private void Update()
    {
        if (agent.remainingDistance < agent.stoppingDistance)
        {
            StartAttacking();
        }
    }

    private void OnPathUpdated(Vector3 newPos)
    {
        float distToNew = Vector3.Distance(transform.position, newPos);
        float distToOld = Vector3.Distance(transform.position, currentTarget);

        if (distToNew < distToOld)
        {
            UpdatePath();
        }
    }

    private void OnFightStarted(Vector3 pos)
    {
        float distToNew = Vector3.Distance(transform.position, pos);
        float distToOld = Vector3.Distance(transform.position, currentTarget);

        if (distToNew < distToOld * 1.5f)
        {
            currentTarget = pos;
            UpdateNavAgent();
        }
    }


    private void OnFightEnded(Vector3 position)
    {
        if (Vector3.Distance(position, transform.position) < 2)
        {
            StopAttacking();
            UpdatePath();
        }
    }

    public void UpdatePath()
    {
        if (attacker.Attacking)
        {
            return;
        }

        if (currentTargetBuilding != null)
        {
            currentTargetBuilding.OnDeath -= Building_OnDeath;
            currentTargetBuilding.DeregisterAttackingEnemy(this);
        }

        currentTarget = buildingManager.GetClosestHouse(transform.position, out currentTargetBuilding, out int buildingIndex); ;

        if (currentTargetBuilding != null)
        {
            currentTargetBuilding.OnDeath += Building_OnDeath;
            currentTargetBuilding.RegisterAttackingEnemy(this);
        }

        UpdateNavAgent();

        //moveCoroutine = StartCoroutine(MovementRoutine(path, buildingIndex == -1));
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
        UpdatePath();
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

/*
    private List<Vector3> GetClosest(out int buildingIndex)
    {
        Vector3 closest = buildingManager.GetClosestHouse(transform.position, out currentTargetBuilding, out buildingIndex);
        List<Vector3> path = EnemyPathFinding.FindPath(transform.position, closest, EnemyPathFinding.Map);

        // Retry the the pathfinding until we find somehting that works
        if (path == null || path[0] == null)
        {
            int tries = 0;
            List<int> blackList = new List<int>();
            while ((path == null || path[0] == null) && tries++ < 10)
            {
                blackList.Add(buildingIndex);
                closest = buildingManager.GetClosestHouse(transform.position, out currentTargetBuilding, out buildingIndex, blackList);
                path = EnemyPathFinding.FindPath(transform.position, closest, EnemyPathFinding.Map);
            }
        }

        return path;
    }
 * 
 * 
 * private IEnumerator MovementRoutine(List<Vector3> path, bool isCastle)
{
    yield return null;
    animator.Move();

    float distToPath = Vector3.Distance(path[path.Count - 1], transform.position);
    float speed = moveSpeed / distToPath;

    yield return Move(transform.position, path[path.Count - 1], speed);

    for (int i = path.Count - 2 ; i > 0 + (isCastle ? 1 : 0); i--)
    {
        yield return Move(transform.position, path[i], speed);
    }

    float t = 0;
    Vector3 dir = (path[0] - transform.position).normalized;
    Quaternion targetRotation = Quaternion.LookRotation(dir, Vector3.up);
    while (t < 1.0f)
    {
        t += Time.deltaTime * 10;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
    }

    StartAttacking();
}

private IEnumerator Move(Vector3 startPos, Vector3 targetPos, float speed)
{
    float t = 0;

    Vector3 dir = (targetPos - startPos).normalized;
    Vector3 left = Quaternion.AngleAxis(90, Vector3.up) * dir;

    Vector3 upOffset = Vector3.up * groundOffset;
    Vector3 leftOffset = left * horizontalOffset;

    targetPos = targetPos + upOffset + leftOffset;
    dir = (targetPos - startPos).normalized;
    Quaternion targetRotation = Quaternion.LookRotation(dir, Vector3.up);

    while (t <= 1.0f)
    {
        t += Time.deltaTime * speed;

        transform.position = Vector3.Lerp(startPos, targetPos, t);
        if (t < 0.3f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }

        yield return null;
    }
}*/
