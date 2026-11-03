using UnityEngine;

/// <summary>Serialized audio clips and local preferences, shared across scene changes.</summary>
[DefaultExecutionOrder(-1000)]
public sealed class GameAudio : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioClip stoneClip;
    [SerializeField] private AudioClip cardSelectionClip;
    private const string MusicKey = "Audio.MusicVolume";
    private const string StoneKey = "Audio.StoneVolume";
    private static GameAudio instance;
    private AudioSource music, stone, card;
    public float MusicVolume { get; private set; }
    public float StoneVolume { get; private set; }
    public static GameAudio Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<GameAudio>();
            if (instance == null) throw new System.InvalidOperationException("The scene needs a configured GameAudio component.");
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { instance = null; }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 0.35f));
        StoneVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(StoneKey, 0.75f));
        music = CreateSource(musicClip, MusicVolume, true);
        stone = CreateSource(stoneClip, StoneVolume, false);
        card = CreateSource(cardSelectionClip, StoneVolume, false);
        if (musicClip != null) music.Play();
    }

    private AudioSource CreateSource(AudioClip clip, float volume, bool loop)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = loop;
        source.clip = clip;
        source.volume = volume;
        return source;
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        music.volume = MusicVolume;
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
    }
    public void SetStoneVolume(float value)
    {
        StoneVolume = Mathf.Clamp01(value);
        stone.volume = card.volume = StoneVolume;
        PlayerPrefs.SetFloat(StoneKey, StoneVolume);
    }
    public void PlayStone() { if (stoneClip != null) stone.PlayOneShot(stoneClip); }
    public void PlayCardSelection() { if (cardSelectionClip != null) card.PlayOneShot(cardSelectionClip); }
    public void Save() { PlayerPrefs.Save(); }
    private void OnApplicationPause(bool paused) { if (paused) Save(); }
    private void OnApplicationQuit() { Save(); }
    private void OnDestroy() { if (instance == this) instance = null; }
}
