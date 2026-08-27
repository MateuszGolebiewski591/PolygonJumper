using UnityEngine;
//using Unity;

public class AudioManager : MonoBehaviour
{
    [Header("Variables")]
    [SerializeField] private float pitchVariation = 0.1f;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioSource longLastingSFX;
    [SerializeField] private AudioSource SFX;
    [SerializeField] private AudioSource JumpSFX;
    [SerializeField] private AudioSource PulseSFX;
    [SerializeField] private AudioSource DashSFX;


    [Header("Audio Clips")]
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip pulseSound;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip powerSound;
    [SerializeField] private AudioClip dischargeSound;
    [SerializeField] private AudioClip portalSound;
    [SerializeField] private AudioClip levelCompleteSound;

    private float jumpPitch;
    private float pulsePitch;
    private float dashPitch;
    public static AudioManager Instance {get; set;}

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        jumpPitch = JumpSFX.pitch;
        pulsePitch = PulseSFX.pitch;
        dashPitch = DashSFX.pitch;
    }
    void Start()
    {
        backgroundMusicSource.clip = backgroundMusicClip;
        backgroundMusicSource.Play();
    }

    public void PlayJumpSound()
    {
        JumpSFX.pitch = jumpPitch + Random.Range(-pitchVariation, pitchVariation);
        JumpSFX.PlayOneShot(jumpSound);
    }

    public void PlayPulseSound()
    {
        PulseSFX.pitch = pulsePitch + Random.Range(-pitchVariation, pitchVariation);
        PulseSFX.PlayOneShot(pulseSound);
    }

    public void PlayDashSound()
    {
        DashSFX.pitch = dashPitch + Random.Range(-pitchVariation, pitchVariation);
        DashSFX.PlayOneShot(dashSound);
    }

    public void PlayDeathSound()
    {
        SFX.PlayOneShot(deathSound);
    }

    public void PlayDischargeSound()
    {
        SFX.PlayOneShot(dischargeSound);
    }

    public void StartPowerSound()
    {
        longLastingSFX.clip = powerSound;
        longLastingSFX.Play();
    }

    public void EndPowerSound()
    {
        longLastingSFX.Stop();   
    }

    public void PlayPortalSound()
    {
        SFX.PlayOneShot(portalSound);
    }

    public void PlayLevelCompleteSound()
    {
        SFX.PlayOneShot(levelCompleteSound);
    }
}
