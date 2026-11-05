using UnityEngine;
using UnityEngine.UI;
public static class AudioUiCheck
{
    public static string Main()
    {
        var audio = GameAudio.Instance;
        AudioSettings.Open();
        Slider slider = null;
        foreach (var s in Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
            Canvas.ForceUpdateCanvases();
            return "clip=" + (source.clip != null ? source.clip.name : "MISSING") + "; playing=" + source.isPlaying + "; mute=" + mute + "; linked=" + linked + "; saved=" + saved;
        }
        finally { slider.value = original; audio.Save(); }
    }
}
