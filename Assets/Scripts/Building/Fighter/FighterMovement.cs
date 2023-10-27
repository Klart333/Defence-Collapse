using UnityEngine;
using rawrdom = UnityEngine.Random;
using System;
using UnityEngine.AI;

public class FighterMovement : MonoBehaviour
{
    [SerializeField]
    private float speed;

    [SerializeField]
    private LayerMask layerMask;

    private FighterMovementState movementState;
    private Fighter fighter;
    private Animator animator;

    private Vector3 lastPos;

    public TargetMovementState TargetState { get; set; }
    public AttackingState AttackingState { get; set; }
    public NavMeshAgent Agent { get; set; }
    public Idle IdleState { get; set; }
    public Building Building => fighter.Building;
    public bool Attacking => fighter.Fighting;
    public float Speed => speed; 
    public float Movement => Vector3.SqrMagnitude(transform.position - lastPos) * Time.deltaTime * 300000; 

    private void Start()
    {
        fighter = GetComponent<Fighter>();
        animator = GetComponent<Animator>();
        Agent = GetComponent<NavMeshAgent>();

        IdleState = new Idle();
        TargetState = new TargetMovementState();
        AttackingState = new AttackingState();

        movementState = IdleState;
        movementState.OnStateEntered(this);

        Building.OnEnterDefense += Building_OnEnterDefense;

        GameEvents.OnFightStarted += OnFightStarted;
    }

    private void OnFightStarted(Vector3 position)
    {
        movementState.OnFightStarted(position);
    }

    private void Building_OnEnterDefense(Vector3 pos)
    {
        SetState(TargetState, pos);
    }

    public void SetState(FighterMovementState state, Vector3? pos = null)
    {
        movementState.OnStateExited();
        movementState = state;
        movementState.OnStateEntered(this);

        if (pos.HasValue)
        {
            movementState.SetTargetPosition(pos.Value);
        }
    }

    private void FixedUpdate()
    {
        movementState.Update();

        animator.SetFloat("Movement", Movement);

        lastPos = transform.position;
    }
}

public abstract class FighterMovementState
{
    public abstract void OnStateEntered(FighterMovement fighter);
    public abstract void OnStateExited();

    public abstract void Update();

    public virtual void OnFightStarted(Vector3 position)
    {

    }

    public virtual void SetTargetPosition(Vector3 pos)
    {

    }
}

public class Idle : FighterMovementState
{
    private FighterMovement fighter;

    private float timer = 14;

    // Stats
    private float updateDirection = 15;
    private float range = 2;

    public override void OnStateEntered(FighterMovement fighter)
    {
        this.fighter = fighter;

        float speed = fighter.Speed / 2.5f;
        fighter.Agent.speed = speed;
    }

    public override void OnStateExited()
    {
        
    }

    public override void Update()
    {
        timer += Time.fixedDeltaTime;
        if (timer >= updateDirection)
        {
            timer = 0;

            Vector3 pos = GetIdleTarget();
            fighter.Agent.SetDestination(pos);
        }
    }

    public Vector3 GetIdleTarget()
    {
        float x = UnityEngine.Random.Range(-range, range);
        float y = UnityEngine.Random.Range(-range, range);

        Vector3 pos = fighter.Building.transform.position + new Vector3(x, 0, y);

        if (Physics.Raycast(pos + Vector3.up * 50, Vector3.down, out RaycastHit hitInfo, 60))
        {
            if (Mathf.Abs(hitInfo.point.y - fighter.transform.position.y) < 0.2f)
            {
                return pos;
            }
        }

        return GetIdleTarget();
    }

    public override void OnFightStarted(Vector3 position)
    {
        if (Vector3.Distance(position, fighter.transform.position) < 10)
        {
            fighter.SetState(fighter.TargetState, position);
        }
    }
}

public class TargetMovementState : FighterMovementState
{
    private FighterMovement fighter;

    public override void OnStateEntered(FighterMovement fighter)
    {
        this.fighter = fighter;

        float speed = fighter.Speed * 2f;
        fighter.Agent.speed = speed;
    }

    public override void OnStateExited()
    {

    }

    public override void Update()
    {
        if (fighter.Attacking)
        {
            fighter.SetState(fighter.AttackingState);
        }
    }

    public override void SetTargetPosition(Vector3 pos)
    {
        base.SetTargetPosition(pos);

        fighter.Agent.SetDestination(pos);
    }
}

public class AttackingState : FighterMovementState
{
    private FighterMovement fighter;
    public override void OnStateEntered(FighterMovement fighter)
    {
        this.fighter = fighter;

        fighter.Agent.isStopped = true;
    }

    public override void OnStateExited()
    {

    }

    public override void Update()
    {

    }
}