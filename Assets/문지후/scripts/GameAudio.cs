using UnityEngine;

/// <summary>Local audio preferences and playback, shared across scene changes.</summary>
public sealed class GameAudio : MonoBehaviour
{
    private const string MusicKey = "Audio.MusicVolume";
    private const string StoneKey = "Audio.StoneVolume";
    private static GameAudio instance;
    private AudioSource music, stone;
    private AudioClip musicClip, stoneClip;
    public float MusicVolume { get; private set; }
    public float StoneVolume { get; private set; }
    public static GameAudio Instance
    {
        get
        {
            if (instance == null) new GameObject("GameAudio").AddComponent<GameAudio>();
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { instance = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() { _ = Instance; }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 0.35f));
        StoneVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(StoneKey, 0.75f));
        music = gameObject.AddComponent<AudioSource>();
        stone = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = stone.playOnAwake = false;
        music.spatialBlend = stone.spatialBlend = 0f;
        music.loop = true;
        music.volume = MusicVolume;
        stone.volume = StoneVolume;
        // Self-contained starter sounds; no network or external audio dependency.
        musicClip = CreateMusic();
        stoneClip = CreateStone();
        music.clip = musicClip;
        music.Play();
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
        stone.volume = StoneVolume;
        PlayerPrefs.SetFloat(StoneKey, StoneVolume);
    }
    public void PlayStone() { stone.PlayOneShot(stoneClip); }
    public void Save() { PlayerPrefs.Save(); }
    private void OnApplicationPause(bool paused) { if (paused) Save(); }
    private void OnApplicationQuit() { Save(); }
    private void OnDestroy()
    {
        if (instance != this) return;
        instance = null;
        if (musicClip != null) Destroy(musicClip);
        if (stoneClip != null) Destroy(stoneClip);
    }

    private static AudioClip CreateStone()
    {
        const int rate = 22050;
        var samples = new float[rate / 7];
        var noise = new System.Random(731);
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            float attack = Mathf.Min(1f, t * 2000f);
            samples[i] = attack * (0.48f * Mathf.Sin(2f * Mathf.PI * 920f * t) * Mathf.Exp(-75f * t)
                + 0.25f * Mathf.Sin(2f * Mathf.PI * 1530f * t) * Mathf.Exp(-95f * t)
                + 0.2f * ((float)noise.NextDouble() * 2f - 1f) * Mathf.Exp(-180f * t));
        }
        var clip = AudioClip.Create("Stone placement", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateMusic()
    {
        const int rate = 22050;
        const float beat = 0.5f;
        // A quiet original pentatonic phrase, with complete decay before the loop boundary.
        int[] notes = { 60, 64, 67, 69, 67, 64, 62, 64, 57, 60, 64, 67, 64, 62, 60, 55 };
        var samples = new float[rate * 10];
        for (int n = 0; n < notes.Length; n++)
        {
            float hz = 440f * Mathf.Pow(2f, (notes[n] - 69) / 12f);
            int start = Mathf.RoundToInt(n * beat * rate);
            for (int j = 0; j < rate * 2 && start + j < samples.Length; j++)
            {
                float t = j / (float)rate;
                float envelope = Mathf.Min(1f, t / 0.025f) * Mathf.Exp(-4f * t) * Mathf.Clamp01((2f - t) / 0.1f);
                samples[start + j] += 0.22f * envelope * (Mathf.Sin(2f * Mathf.PI * hz * t)
                    + 0.2f * Mathf.Sin(4f * Mathf.PI * hz * t));
            }
        }
        var clip = AudioClip.Create("Quiet board melody", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
