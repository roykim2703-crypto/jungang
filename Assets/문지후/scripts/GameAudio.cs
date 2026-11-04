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
    private AudioSource capturePing;
    private static readonly AudioClip[] captureTones=new AudioClip[8];
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
        capturePing = CreateSource(null, StoneVolume, false);
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
        stone.volume = card.volume = capturePing.volume = StoneVolume;
        PlayerPrefs.SetFloat(StoneKey, StoneVolume);
    }
    public void PlayStone() { if (stoneClip != null) stone.PlayOneShot(stoneClip); }
    public void PlayCardSelection() { if (cardSelectionClip != null) card.PlayOneShot(cardSelectionClip); }
    public void PlayCapturePing(int index)
    {
        int note=Mathf.Abs(index)%captureTones.Length;
        if(captureTones[note]==null)
        {
            const int rate=44100;var samples=new float[rate/4];
            float frequency=880*Mathf.Pow(2,note/12f);
            for(int i=0;i<samples.Length;i++)
            {
                float time=(float)i/rate,attack=Mathf.Clamp01(time/.004f);
                float envelope=attack*Mathf.Exp(-time*20)*Mathf.Clamp01((.25f-time)/.025f);
                samples[i]=(.32f*Mathf.Sin(2*Mathf.PI*frequency*time)+.1f*Mathf.Sin(2*Mathf.PI*frequency*2.5f*time))*envelope;
            }
            var clip=AudioClip.Create("CaptureDing_"+note,samples.Length,1,rate,false);
            clip.hideFlags=HideFlags.HideAndDontSave;clip.SetData(samples,0);captureTones[note]=clip;
        }
        capturePing.PlayOneShot(captureTones[note],.65f);
    }
    public void Save() { PlayerPrefs.Save(); }
    private void OnApplicationPause(bool paused) { if (paused) Save(); }
    private void OnApplicationQuit() { Save(); }
    private void OnDestroy() { if (instance == this) instance = null; }
}
