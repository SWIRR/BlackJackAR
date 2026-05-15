using System.Collections;
using UnityEngine;

public class DealerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator dealerAnimator;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip playerWinClip;
    [SerializeField] private AudioClip playerLossClip;
    [SerializeField] private AudioClip roundStartClip;

    [Header("Idle random animations")]
    [SerializeField] private bool enableRandomIdleAnimations = true;
    [SerializeField] private float minIdleDelay = 5f;
    [SerializeField] private float maxIdleDelay = 10f;

    private static readonly int WaveTrigger = Animator.StringToHash("Wave");
    private static readonly int ThumbsUpTrigger = Animator.StringToHash("ThumbsUp");
    private static readonly int HeadGrabTrigger = Animator.StringToHash("HeadGrab");
    private static readonly int IdleHeadMoveTrigger = Animator.StringToHash("IdleHeadMove");

    private Coroutine idleRoutine;

    private void Awake()
    {
        if (dealerAnimator == null)
        {
            dealerAnimator = GetComponentInChildren<Animator>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = GetComponentInChildren<AudioSource>();
            }
        }
    }

    private void OnEnable()
    {
        if (enableRandomIdleAnimations)
        {
            idleRoutine = StartCoroutine(RandomIdleAnimationRoutine());
        }
    }

    private void OnDisable()
    {
        if (idleRoutine != null)
        {
            StopCoroutine(idleRoutine);
            idleRoutine = null;
        }
    }

    public void PlayWave()
    {
        PlayTrigger(WaveTrigger);
        PlayRoundStartSound();
    }

    public void PlayThumbsUp()
    {
        PlayTrigger(ThumbsUpTrigger);
        PlayPlayerWinSound();
    }

    public void PlayHeadGrab()
    {
        PlayTrigger(HeadGrabTrigger);
        PlayPlayerLossSound();
    }

    public void PlayIdleHeadMove()
    {
        PlayTrigger(IdleHeadMoveTrigger);
    }

    private void PlayPlayerWinSound()
    {
        PlaySound(playerWinClip, "Brak przypisanego dŸwiêku wygranej gracza.");
    }

    private void PlayPlayerLossSound()
    {
        PlaySound(playerLossClip, "Brak przypisanego dŸwiêku przegranej gracza.");
    }

    private void PlayRoundStartSound()
    {
        PlaySound(roundStartClip, "Brak przypisanego dŸwiêku rozpoczêcia rundy.");
    }

    private void PlaySound(AudioClip clip, string missingClipMessage)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("[DealerAnimationController] Brak AudioSource.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("[DealerAnimationController] " + missingClipMessage);
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private IEnumerator RandomIdleAnimationRoutine()
    {
        while (true)
        {
            float delay = Random.Range(minIdleDelay, maxIdleDelay);
            yield return new WaitForSeconds(delay);

            if (dealerAnimator == null)
                continue;

            if (!IsInIdleState())
                continue;

            PlayIdleHeadMove();
        }
    }

    private bool IsInIdleState()
    {
        AnimatorStateInfo stateInfo = dealerAnimator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("DealerIdle");
    }

    private void PlayTrigger(int triggerHash)
    {
        if (dealerAnimator == null)
        {
            Debug.LogWarning("[DealerAnimationController] Brak przypisanego Animatora krupiera.");
            return;
        }

        dealerAnimator.SetTrigger(triggerHash);
    }
}