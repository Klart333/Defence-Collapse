using System;
using Buildings.District;
using Cysharp.Threading.Tasks;
using Gameplay;
using UnityEngine.UI;
using UnityEngine;

public class UIStartWave : MonoBehaviour
{
    private static readonly int Interactable = Animator.StringToHash("Interactable");
    private static readonly int Flip = Animator.StringToHash("Flip");

    [SerializeField]
    private Button waveButton;
    
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private AnimationClip unFlipAnimation;

    private bool inWave;
    
    private void OnEnable()
    {
        Events.OnWaveEnded += OnWaveEnded;
    }

    private void OnDisable()
    {
        Events.OnWaveEnded -= OnWaveEnded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            Events.OnWaveEnded?.Invoke();
        }
    }

    private void OnWaveEnded()
    {
        animator.SetBool(Interactable, true);

        SetInteractableAfterDelay().Forget();
        PersistantGameStats.CurrentPersistantGameStats.WaveCount++;

        inWave = false;
    }

    private async UniTaskVoid SetInteractableAfterDelay()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(unFlipAnimation.length));
        waveButton.interactable = true;
    }
    
    public void StartWave()
    {
        if (inWave) 
        {
            return;
        }

        inWave = true;

        Events.OnWaveStarted?.Invoke();

        waveButton.interactable = false;
        animator.SetTrigger(Flip);
        animator.SetBool(Interactable, false);
    }
}
