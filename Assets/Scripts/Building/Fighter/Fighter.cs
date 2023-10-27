using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Fighter : MonoBehaviour
{
    [SerializeField]
    private FighterAttackData attackData;

    private Collider[] hitResults;

    private FighterAnimator animator;
    private FighterMovement movement;

    private float attackTimer = 0;

    public Building Building { get; set; }
    public bool Fighting { get; set; }

    private void Start()
    {
        hitResults = new Collider[4];
        animator = GetComponent<FighterAnimator>();
        movement = GetComponent<FighterMovement>();

        GetComponentInChildren<IHealth>().OnDeath += Fighter_OnDeath;
    }

    private async void Fighter_OnDeath(GameObject obj)
    {
        Fighting = false;

        await Task.Delay(2000);

        Destroy(gameObject);
    }

    public void StartFighting()
    {
        FightManager.Instance.JoinFight(this);
        Fighting = true;
    }

    private void Update()
    {
        if (Fighting)
        {
            attackTimer += Time.deltaTime;

            if (attackTimer >= 1.0f / attackData.AttackSpeed)
            {
                attackTimer = 0;

                if (!Attack())
                {

                }
            }
        }
    }

    private bool Attack()
    {
        animator.Attack();

        Vector3 pos = transform.position + transform.forward * attackData.AttackRadius;
        Physics.OverlapSphereNonAlloc(pos, attackData.AttackRadius, hitResults, attackData.LayerMask);

        int damageDones = 0;
        for (int i = 0; i < hitResults.Length; i++)
        {
            if (hitResults[i] == null)
            {
                continue;
            }

            if (hitResults[i].transform.parent.TryGetComponent<IHealth>(out IHealth health))
            {
                health.TakeDamage(attackData.Damage);

                if (!attackData.Splash || ++damageDones >= attackData.MaxTargets)
                {
                    break;
                }
            }
        }

        return damageDones > 0;
    }
}
