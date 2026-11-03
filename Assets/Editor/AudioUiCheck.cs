using UnityEngine;
using UnityEngine.UI;
public static class AudioUiCheck
{
    [UnityEditor.MenuItem("Tools/Audio/Validate Settings In Play Mode")]
    public static void Main()
    {
        if (!Application.isPlaying) throw new System.InvalidOperationException("Enter Play Mode first.");
        var audio = GameAudio.Instance;
        AudioSettings.Open();
        Slider slider = null;
        foreach (var s in Object.FindObjectsByType<Slider>(FindObjectsInactive.Include))
            if (s.name == "MusicSlider") slider = s;
        var source = audio.GetComponents<AudioSource>()[0];
        float original = audio.MusicVolume;
        try
        {
            slider.value = 0f;
            bool mute = source.volume == 0f;
            slider.value = .62f;
            audio.Save();
            bool linked = Mathf.Approximately(source.volume, .62f);
            bool saved = Mathf.Approximately(PlayerPrefs.GetFloat("Audio.MusicVolume"), .62f);
            var effects = audio.GetComponents<AudioSource>();
            float effectVolume = audio.StoneVolume;
            audio.SetStoneVolume(.5f);
            audio.PlayCardSelection();
            bool card = effects[2].clip != null && effects[2].isPlaying && Mathf.Approximately(effects[2].volume, .5f);
            audio.SetStoneVolume(0f);
            bool effectsMuted = effects[1].volume == 0f && effects[2].volume == 0f;
            audio.SetStoneVolume(effectVolume);
            if (!card || !effectsMuted) throw new System.Exception("Card audio validation failed.");
            Debug.Log("Card selection sound playback and effect volume passed.");
            Canvas.ForceUpdateCanvases();
            if (source.clip == null || !source.isPlaying || !mute || !linked || !saved) throw new System.Exception("Audio settings validation failed.");
            Debug.Log("Audio settings passed: clip=" + (source.clip != null ? source.clip.name : "MISSING") + "; playing=" + source.isPlaying + "; mute=" + mute + "; linked=" + linked + "; saved=" + saved);
        }
        finally { slider.value = original; audio.Save(); }
    }
}
