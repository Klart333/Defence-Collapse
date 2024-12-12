using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Title("Stats")]
    [SerializeField]
    private EnemyData enemyData;

    [Title("Ground Collision")]
    [SerializeField]
    private LayerMask groundMask;

    [Title("Movement")]
    [SerializeField]
    private float turnSpeed = 2;

    private readonly RaycastHit[] results = new RaycastHit[1];

    private EnemyAttacker attacker;
    private EnemyAnimator animator;
    private EnemyHealth health;

    private Vector3 direction;

    private bool shouldMove = true;
    private int unitIndex = 0;

    private async void OnEnable()
    {
        attacker = GetComponent<EnemyAttacker>();
        health = GetComponent<EnemyHealth>();
        animator = GetComponent<EnemyAnimator>();

        //agent.speed = enemyData.Stats.MovementSpeed.Value;

        health.OnDeath += Health_OnDeath;

        await UniTask.WaitUntil(() => PathManager.Instance != null);
        PathManager.Instance.GetPathInformation += PathManagerGetPathInformation;
    }

    private void OnDisable()
    {
        PathManager.Instance.GetPathInformation -= PathManagerGetPathInformation; ;

        health.OnDeath -= Health_OnDeath;
    }

    private void PathManagerGetPathInformation()
    {
        PathManager.Instance.UnitCounts[unitIndex] += 1; // Replace 1 with weight
    }

    private void Update()
    {
        if (!health.Health.Alive) return;

        Vector3 rayPos = transform.position + Vector3.up;
        int count = Physics.RaycastNonAlloc(rayPos, Vector3.down, results, 2, groundMask); // VERY SLOW! DONT NEED EVERY FRAME, IF IT HASN'T CHANGED IN A WHILE STOP SHOOTING RAYS THE WORLD IS BASICALLY FLAT
        for (int i = 0; i < count; i++)
        {
            transform.position = results[i].point;
            transform.rotation = Quaternion.LookRotation(direction, results[i].normal);
        }

        if (shouldMove)
        {
            Move();
        }
    }

    private void Move()
    {
        Vector2 pos = transform.position.XZ();
        unitIndex = PathManager.Instance.GetIndex(pos);
        Vector2 dir = PathManager.Instance.Directions[unitIndex];
        direction = (direction + dir.ToXyZ() * turnSpeed * Time.deltaTime).normalized;

        transform.position += enemyData.Stats.MovementSpeed.Value * Time.deltaTime * direction;
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

    private void Health_OnDeath(EnemyHealth obj)
    {
        StopMoving();
    }

    private void StopMoving()
    {
        shouldMove = false;
    }
}