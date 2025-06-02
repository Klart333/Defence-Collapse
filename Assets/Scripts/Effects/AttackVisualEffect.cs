using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.VFX;
using DG.Tweening;
using UnityEngine;
using Gameplay;
using VFX;

public class AttackVisualEffect : PooledMonoBehaviour
{
    [Title("Visual Effect")]
    [SerializeField]
    private bool hasVisualEffect;

    [SerializeField, ShowIf(nameof(hasVisualEffect))]
    private VisualEffect visualEffect;

    [SerializeField, ShowIf(nameof(hasVisualEffect))]
    private bool setVisualEffectToGameSpeed;

    private IGameSpeed gameSpeed;
    
    private Vector3 originalScale;
    
    public IVisualEffectTarget VisualEffectTarget { get; private set; }

    private void Awake()
    {
        originalScale = transform.localScale;
        VisualEffectTarget = GetComponentInChildren<IVisualEffectTarget>();
        
        GetGameSpeed().Forget();
    }

    private async UniTaskVoid GetGameSpeed()
    {
        gameSpeed = await GameSpeedManager.Get();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        Reset();
    }

    private void Reset()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        transform.DOKill();
        transform.localScale = originalScale;
    }

    private void Update()
    {
        if (setVisualEffectToGameSpeed && !Mathf.Approximately(visualEffect.playRate, gameSpeed.Value))
        {
            visualEffect.playRate = gameSpeed.Value;
        }
    }

    public AttackVisualEffect Spawn(Vector3 spawnPosition, Vector3 originPosition, Vector3 targetPosition, Quaternion rot, float scale, float lifetime = 1)
    {
        AttackVisualEffect spawned = GetAtPosAndRot<AttackVisualEffect>(spawnPosition, rot);
        spawned.transform.localScale *= scale;
        spawned.Delay.Lifetime = lifetime;

        spawned.VisualEffectTarget?.SetTarget(originPosition, targetPosition);

        return spawned;
    }
}
