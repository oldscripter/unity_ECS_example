using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [Header("Настройки пула")]
    [SerializeField] private int audioSourcePoolSize = 15; // Размер пула для звуковых эффектов
    [SerializeField] private int musicSourceCount = 1;     // Количество источников для музыки
    [SerializeField] private float maxSoundDistance = 500f;
    [SerializeField] private bool debugMode = false;

    [Header("Настройки звука")]
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;

    [Header("Music")]
    [SerializeField] private AudioClip[] musicTracks;
    [SerializeField] private bool shuffleMusic = true;
    [SerializeField] private bool fadeBetweenTracks = true;
    [SerializeField] private float fadeDuration = 2f;

    // Singleton
    private static AudioManager instance;
    public static AudioManager Instance => instance;

    // Пул AudioSource для SFX
    private List<AudioSource> sfxPool = new List<AudioSource>();
    private int currentSFXIndex = 0;

    // AudioSource для музыки
    private List<AudioSource> musicSources = new List<AudioSource>();
    private int currentMusicIndex = 0;
    private bool isMusicFading = false;
    private float musicFadeProgress = 0f;
    private float musicTargetVolume = 0.5f;

    private void Awake()
    {
        // Singleton паттерн
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioPools();
            InitializeMusic();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioPools()
    {
        // Создаем SFX пул
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f; // 3D звук
            source.maxDistance = maxSoundDistance;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.volume = sfxVolume * masterVolume;
            source.priority = 128;
            sfxPool.Add(source);
        }

        // Создаем Music источники
        for (int i = 0; i < musicSourceCount; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f; // 2D звук
            source.volume = 0f;
            source.priority = 64; // Высокий приоритет для музыки
            musicSources.Add(source);
        }

        if (debugMode)
        {
            Debug.Log($"[AudioManager] Создано {audioSourcePoolSize} SFX источников и {musicSourceCount} Music источников");
        }
    }

    private void InitializeMusic()
    {
        if (musicTracks == null || musicTracks.Length == 0)
        {
            if (debugMode) Debug.LogWarning("[AudioManager] Нет музыкальных треков!");
            return;
        }

        // Начинаем с первого трека
        PlayMusicTrack(0);
    }

    private void Update()
    {
        // Обновляем музыку
        UpdateMusic();
    }

    private void UpdateMusic()
    {
        if (musicSources.Count == 0 || musicTracks.Length == 0) return;

        AudioSource currentSource = musicSources[currentMusicIndex];
        
        // Проверяем, закончился ли трек
        if (!currentSource.isPlaying && !isMusicFading)
        {
            PlayNextMusicTrack();
        }

        // Обработка плавного появления/исчезновения
        if (isMusicFading)
        {
            musicFadeProgress += Time.deltaTime / fadeDuration;
            
            // Затухание текущего трека
            if (musicFadeProgress <= 0.5f)
            {
                float fadeOutProgress = musicFadeProgress * 2f;
                currentSource.volume = Mathf.Lerp(musicVolume * masterVolume, 0f, fadeOutProgress);
            }
            // Появление нового трека
            else
            {
                float fadeInProgress = (musicFadeProgress - 0.5f) * 2f;
                currentSource.volume = Mathf.Lerp(0f, musicVolume * masterVolume, fadeInProgress);
            }

            if (musicFadeProgress >= 1f)
            {
                isMusicFading = false;
                currentSource.volume = musicVolume * masterVolume;
            }
        }
    }

    #region Public SFX Methods

    /// <summary>
    /// Воспроизвести звук в 3D пространстве
    /// </summary>
    public static void PlaySound(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (instance == null)
        {
            Debug.LogError("[AudioManager] AudioManager не найден в сцене!");
            return;
        }

        if (clip == null)
        {
            if (instance.debugMode) Debug.LogWarning("[AudioManager] Попытка воспроизвести null AudioClip");
            return;
        }

        AudioSource source = instance.GetNextAvailableSFXSource();
        if (source == null) return;

        source.transform.position = position;
        source.clip = clip;
        source.volume = volume * instance.sfxVolume * instance.masterVolume;
        source.pitch = pitch;
        source.spatialBlend = 1f;
        source.Play();

        if (instance.debugMode)
        {
            Debug.Log($"[AudioManager] Воспроизведение: {clip.name} в позиции {position}");
        }
    }

    /// <summary>
    /// Воспроизвести звук в 2D (без привязки к позиции)
    /// </summary>
    public static void PlaySound2D(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (instance == null)
        {
            Debug.LogError("[AudioManager] AudioManager не найден в сцене!");
            return;
        }

        if (clip == null)
        {
            if (instance.debugMode) Debug.LogWarning("[AudioManager] Попытка воспроизвести null AudioClip");
            return;
        }

        AudioSource source = instance.GetNextAvailableSFXSource();
        if (source == null) return;

        source.transform.position = Vector3.zero;
        source.clip = clip;
        source.volume = volume * instance.sfxVolume * instance.masterVolume;
        source.pitch = pitch;
        source.spatialBlend = 0f;
        source.Play();

        if (instance.debugMode)
        {
            Debug.Log($"[AudioManager] Воспроизведение 2D звука: {clip.name}");
        }
    }

    /// <summary>
    /// Воспроизвести звук с задержкой
    /// </summary>
    public static void PlaySoundDelayed(AudioClip clip, Vector3 position, float delay, float volume = 1f, float pitch = 1f)
    {
        if (instance == null) return;
        instance.StartCoroutine(instance.PlaySoundDelayedCoroutine(clip, position, delay, volume, pitch));
    }

    private IEnumerator PlaySoundDelayedCoroutine(AudioClip clip, Vector3 position, float delay, float volume, float pitch)
    {
        yield return new WaitForSeconds(delay);
        PlaySound(clip, position, volume, pitch);
    }

    #endregion

    #region Private SFX Methods

    private AudioSource GetNextAvailableSFXSource()
    {
        // Ищем свободный AudioSource
        for (int i = 0; i < sfxPool.Count; i++)
        {
            int index = (currentSFXIndex + i) % sfxPool.Count;
            if (!sfxPool[index].isPlaying)
            {
                currentSFXIndex = (index + 1) % sfxPool.Count;
                return sfxPool[index];
            }
        }

        // Если все заняты, берем следующий по кругу
        AudioSource source = sfxPool[currentSFXIndex];
        currentSFXIndex = (currentSFXIndex + 1) % sfxPool.Count;
        source.Stop();
        
        if (debugMode) Debug.LogWarning($"[AudioManager] Все AudioSource заняты, прерываем: {source.clip?.name}");
        return source;
    }

    #endregion

    #region Music Methods

    /// <summary>
    /// Воспроизвести следующий музыкальный трек
    /// </summary>
    public void PlayNextMusicTrack()
    {
        if (musicTracks.Length == 0) return;

        int nextIndex;
        if (shuffleMusic)
        {
            // Случайный трек, но не тот же самый
            nextIndex = Random.Range(0, musicTracks.Length);
            while (nextIndex == currentMusicIndex && musicTracks.Length > 1)
            {
                nextIndex = Random.Range(0, musicTracks.Length);
            }
        }
        else
        {
            nextIndex = (currentMusicIndex + 1) % musicTracks.Length;
        }

        PlayMusicTrack(nextIndex);
    }

    /// <summary>
    /// Воспроизвести конкретный музыкальный трек
    /// </summary>
    public void PlayMusicTrack(int index)
    {
        if (musicTracks == null || musicTracks.Length == 0 || index >= musicTracks.Length)
        {
            Debug.LogWarning("[AudioManager] Неверный индекс музыкального трека");
            return;
        }

        // Останавливаем текущий трек
        AudioSource currentSource = musicSources[currentMusicIndex];
        
        if (fadeBetweenTracks)
        {
            // Начинаем затухание
            isMusicFading = true;
            musicFadeProgress = 0f;
            
            // Меняем трек на новый
            currentMusicIndex = (currentMusicIndex + 1) % musicSources.Count;
            AudioSource newSource = musicSources[currentMusicIndex];
            newSource.clip = musicTracks[index];
            newSource.volume = 0f;
            newSource.Play();
        }
        else
        {
            // Просто переключаем
            currentSource.Stop();
            currentSource.clip = musicTracks[index];
            currentSource.volume = musicVolume * masterVolume;
            currentSource.Play();
        }

        if (debugMode)
        {
            Debug.Log($"[AudioManager] Воспроизведение музыки: {musicTracks[index].name}");
        }
    }

    /// <summary>
    /// Остановить музыку
    /// </summary>
    public void StopMusic()
    {
        foreach (var source in musicSources)
        {
            source.Stop();
        }
        isMusicFading = false;
    }

    /// <summary>
    /// Возобновить музыку
    /// </summary>
    public void ResumeMusic()
    {
        AudioSource source = musicSources[currentMusicIndex];
        if (!source.isPlaying && source.clip != null)
        {
            source.Play();
        }
    }

    #endregion

    #region Volume Control

    /// <summary>
    /// Установить громкость всех звуков
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    /// <summary>
    /// Установить громкость SFX
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    /// <summary>
    /// Установить громкость музыки
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    private void UpdateAllVolumes()
    {
        // Обновляем SFX
        foreach (var source in sfxPool)
        {
            if (!source.isPlaying)
            {
                source.volume = sfxVolume * masterVolume;
            }
        }

        // Обновляем музыку
        foreach (var source in musicSources)
        {
            if (!isMusicFading)
            {
                source.volume = musicVolume * masterVolume;
            }
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Очистить все звуки
    /// </summary>
    public void StopAllSounds()
    {
        foreach (var source in sfxPool)
        {
            source.Stop();
        }
        StopMusic();
    }

    /// <summary>
    /// Получить случайную вариацию тона
    /// </summary>
    public static float GetRandomPitch(float variation = 0.1f)
    {
        return 1f + Random.Range(-variation, variation);
    }

    #endregion
}