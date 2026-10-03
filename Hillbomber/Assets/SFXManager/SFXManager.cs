using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [SerializeField] private AudioSource SFXObject;

    [Tooltip("Settings slider to adjust the global volume when using PlaySFX, not necessary to run")]
    [SerializeField] private Slider SFXSlider;
    [Tooltip("Settings slider to adjust the global volume when using PlayMusic, not necessary to run")]
    [SerializeField] private Slider MusicSlider;

    private readonly List<AudioSource> _activeMusicSources = new List<AudioSource>();
    private readonly Dictionary<AudioSource, float> _baseMusicVolumes = new Dictionary<AudioSource, float>();
    private readonly List<AudioSource> _activeSFXSources = new List<AudioSource>();
    private readonly Dictionary<AudioSource, float> _baseSFXVolumes = new Dictionary<AudioSource, float>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (MusicSlider)
        {
            MusicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }
        if (SFXSlider)
        {
            SFXSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }
    }

    private void OnMusicSliderChanged(float newValue)
    {
        foreach (AudioSource source in _activeMusicSources)
        {
            if (source && _baseMusicVolumes.TryGetValue(source, out float baseVolume))
            {
                source.volume = baseVolume * newValue;
            }
        }
    }

    private void OnSFXSliderChanged(float newValue)
    {
        foreach (AudioSource source in _activeSFXSources)
        {
            if (source && _baseSFXVolumes.TryGetValue(source, out float baseVolume))
            {
                source.volume = baseVolume * newValue;
            }
        }
    }

    /// <summary>
    /// Instantiates an audio source at the given transform that plays the clip once,
    /// and destroys the AudioSource after the clip has finished playing.
    /// </summary>
    /// <param name="audioClip">The audio clip to play.</param>
    /// <param name="spawnTransform">Where to spawn the AudioSource GameObject.</param>
    /// <param name="volume">Base volume, scaled by the SFX slider if one exists.</param>
    public void PlaySFX(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        PlayAndReturnSFX(audioClip, spawnTransform, volume);
    }

    /// <summary>
    /// Instantiates an audio source at the given transform that plays the clip once,
    /// returns the AudioSource, and destroys it after the clip is done playing.
    /// </summary>
    /// <param name="audioClip">The audio clip to play.</param>
    /// <param name="spawnTransform">Where to spawn the AudioSource GameObject.</param>
    /// <param name="volume">Base volume, scaled by the SFX slider if one exists.</param>
    /// <returns>The instantiated AudioSource.</returns>
    public AudioSource PlayAndReturnSFX(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        AudioSource audioSource = Instantiate(SFXObject, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClip;

        if (SFXSlider)
        {
            audioSource.volume = volume * SFXSlider.normalizedValue;
        }
        else
        {
            audioSource.volume = volume;
        }

        audioSource.Play();

        _activeSFXSources.Add(audioSource);
        _baseSFXVolumes[audioSource] = volume;

        StartCoroutine(StopSFXAfterDelay(audioSource, audioClip.length));

        return audioSource;
    }

    private IEnumerator StopSFXAfterDelay(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay);
        StopSFXInstance(audioSource);
    }

    public void StopSFXInstance(AudioSource audioSource)
    {
        if (audioSource)
        {
            audioSource.Stop();
            _activeSFXSources.Remove(audioSource);
            _baseSFXVolumes.Remove(audioSource);
            Destroy(audioSource.gameObject);
        }
    }

    public void StopAllSFX()
    {
        foreach (AudioSource audioSource in _activeSFXSources)
        {
            if (audioSource)
            {
                audioSource.Stop();
                Destroy(audioSource.gameObject);
            }
        }
        _activeSFXSources.Clear();
        _baseSFXVolumes.Clear();
    }

    /// <summary>
    /// Instantiates an audio source at the given transform that plays the clip on loop.
    /// </summary>
    /// <param name="audioClip">The audio clip to play.</param>
    /// <param name="spawnTransform">Where to spawn the AudioSource GameObject.</param>
    /// <param name="volume">Base volume, scaled by the music slider if one exists.</param>
    public void PlayMusic(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        PlayAndReturnMusic(audioClip, spawnTransform, volume);
    }

    /// <summary>
    /// Instantiates an audio source at the given transform that plays the clip on loop,
    /// and returns the AudioSource.
    /// </summary>
    /// <param name="audioClip">The audio clip to play.</param>
    /// <param name="spawnTransform">Where to spawn the AudioSource GameObject.</param>
    /// <param name="volume">Base volume, scaled by the music slider if one exists.</param>
    /// <returns>The instantiated AudioSource.</returns>
    public AudioSource PlayAndReturnMusic(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        AudioSource audioSource = Instantiate(SFXObject, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClip;
        audioSource.loop = true;

        if (MusicSlider)
        {
            audioSource.volume = volume * MusicSlider.normalizedValue;
        }
        else
        {
            audioSource.volume = volume;
        }

        audioSource.Play();

        _activeMusicSources.Add(audioSource);
        _baseMusicVolumes[audioSource] = volume;

        return audioSource;
    }

    public void StopMusicInstance(AudioSource audioSource)
    {
        if (audioSource)
        {
            audioSource.Stop();
            _activeMusicSources.Remove(audioSource);
            _baseMusicVolumes.Remove(audioSource);
            Destroy(audioSource.gameObject);
        }
    }

    public void StopAllMusic()
    {
        foreach (AudioSource audioSource in _activeMusicSources)
        {
            if (audioSource)
            {
                audioSource.Stop();
                Destroy(audioSource.gameObject);
            }
        }
        _activeMusicSources.Clear();
        _baseMusicVolumes.Clear();
    }
}