using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 통합 사운드 매니저 - BGM과 효과음을 모두 관리
/// 싱글톤 패턴으로 어디서든 접근 가능
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    public AudioSource bgmSource;           // BGM 전용 AudioSource
    public AudioSource sfxSource;           // 효과음 전용 AudioSource
    
    [Header("BGM Clips")]
    public AudioClip mainBGM;               // 메인 BGM
    public AudioClip menuBGM;               // 메뉴 BGM (필요시)
    
    [Header("SFX Clips")]
    public AudioClip[] touchSFX;            // 터치 효과음 배열 (랜덤 재생)
    public AudioClip[] waterDropSFX;        // 물방울 효과음 배열 (랜덤 재생)
    public AudioClip growthSFX;             // 종유석 성장 효과음
    public AudioClip[] crackSFX;            // 균열 효과음 배열 (랜덤 재생)
    public AudioClip breakSFX;              // 종유석 파괴 소리
    public AudioClip completeSFX;           // 종유석 완성 소리
    public AudioClip popupSFX;              // 팝업 등장 소리
    public AudioClip buttonSFX;             // 버튼 클릭 소리
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;         // 전체 볼륨
    [Range(0f, 1f)]
    public float bgmVolume = 0.7f;          // BGM 볼륨
    [Range(0f, 1f)]
    public float sfxVolume = 1f;            // 효과음 볼륨
    
    [Header("Settings")]
    public bool isBGMEnabled = true;        // BGM 활성화
    public bool isSFXEnabled = true;        // 효과음 활성화
    public bool fadeInOut = true;           // BGM 페이드 인/아웃 사용
    public float fadeDuration = 1f;         // 페이드 지속 시간
    
    // 오디오 소스 풀 (여러 효과음 동시 재생용)
    private List<AudioSource> sfxPool = new List<AudioSource>();
    private int poolSize = 5;
    
    // 설정 저장 키
    private const string MASTER_VOLUME_KEY = "master_volume";
    private const string BGM_VOLUME_KEY = "bgm_volume";
    private const string SFX_VOLUME_KEY = "sfx_volume";
    private const string BGM_ENABLED_KEY = "bgm_enabled";
    private const string SFX_ENABLED_KEY = "sfx_enabled";
    
    void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        
        InitializeSoundManager();
    }
    
    void Start()
    {
        // 메인 BGM 시작
        PlayBGM(mainBGM);
    }
    
    /// <summary>
    /// 사운드 매니저 초기화
    /// </summary>
    void InitializeSoundManager()
    {
        // AudioSource가 없으면 자동 생성
        if (bgmSource == null)
        {
            GameObject bgmObj = new GameObject("BGM Source");
            bgmObj.transform.SetParent(transform);
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX Source");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        
        // SFX 풀 생성
        CreateSFXPool();
        
        // 저장된 설정 로드
        LoadSettings();
        
        // 볼륨 적용
        ApplyVolumeSettings();
    }
    
    /// <summary>
    /// SFX 오디오 소스 풀 생성
    /// </summary>
    void CreateSFXPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject poolObj = new GameObject($"SFX Pool {i}");
            poolObj.transform.SetParent(transform);
            AudioSource poolSource = poolObj.AddComponent<AudioSource>();
            poolSource.loop = false;
            poolSource.playOnAwake = false;
            sfxPool.Add(poolSource);
        }
    }
    
    #region BGM 관리
    
    /// <summary>
    /// BGM 재생
    /// </summary>
    public void PlayBGM(AudioClip clip, bool fadeIn = true)
    {
        if (!isBGMEnabled || clip == null || bgmSource == null) return;
        
        if (bgmSource.isPlaying && bgmSource.clip == clip) return; // 이미 재생 중
        
        if (fadeIn && fadeInOut)
        {
            StartCoroutine(FadeBGM(clip));
        }
        else
        {
            bgmSource.clip = clip;
            bgmSource.Play();
        }
    }
    
    /// <summary>
    /// BGM 페이드 인/아웃
    /// </summary>
    IEnumerator FadeBGM(AudioClip newClip)
    {
        // 페이드 아웃
        float startVolume = bgmSource.volume;
        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }
        
        // 클립 변경
        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();
        
        // 페이드 인
        while (bgmSource.volume < startVolume)
        {
            bgmSource.volume += startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }
        
        bgmSource.volume = startVolume;
    }
    
    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopBGM(bool fadeOut = true)
    {
        if (bgmSource == null) return;
        
        if (fadeOut && fadeInOut)
        {
            StartCoroutine(FadeOutBGM());
        }
        else
        {
            bgmSource.Stop();
        }
    }
    
    /// <summary>
    /// BGM 페이드 아웃
    /// </summary>
    IEnumerator FadeOutBGM()
    {
        float startVolume = bgmSource.volume;
        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }
        bgmSource.Stop();
        bgmSource.volume = startVolume;
    }
    
    /// <summary>
    /// BGM 일시정지/재개
    /// </summary>
    public void PauseBGM()
    {
        if (bgmSource != null) bgmSource.Pause();
    }
    
    public void ResumeBGM()
    {
        if (bgmSource != null && isBGMEnabled) bgmSource.UnPause();
    }
    
    #endregion
    
    #region 효과음 관리
    
    /// <summary>
    /// 효과음 재생 (일반)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (!isSFXEnabled || clip == null) return;
        
        AudioSource availableSource = GetAvailableSFXSource();
        if (availableSource != null)
        {
            availableSource.clip = clip;
            availableSource.volume = sfxVolume * masterVolume * volume;
            availableSource.Play();
        }
    }
    
    /// <summary>
    /// 배열에서 랜덤으로 효과음 재생
    /// </summary>
    public void PlayRandomSFX(AudioClip[] clips, float volume = 1f)
    {
        if (!isSFXEnabled || clips == null || clips.Length == 0) return;
        
        // null이 아닌 클립만 필터링
        List<AudioClip> validClips = new List<AudioClip>();
        foreach (AudioClip clip in clips)
        {
            if (clip != null) validClips.Add(clip);
        }
        
        if (validClips.Count == 0) return;
        
        // 랜덤 선택
        int randomIndex = Random.Range(0, validClips.Count);
        AudioClip selectedClip = validClips[randomIndex];
        
        PlaySFX(selectedClip, volume);
    }
    
    /// <summary>
    /// 효과음 재생 (미리 정의된 사운드들)
    /// </summary>
    public void PlayTouchSFX() => PlayRandomSFX(touchSFX);
    public void PlayWaterDropSFX() => PlayRandomSFX(waterDropSFX);
    public void PlayGrowthSFX() => PlaySFX(growthSFX);
    public void PlayCrackSFX() => PlayRandomSFX(crackSFX);
    public void PlayBreakSFX() => PlaySFX(breakSFX);
    public void PlayCompleteSFX() => PlaySFX(completeSFX);
    public void PlayPopupSFX() => PlaySFX(popupSFX);
    public void PlayButtonSFX() => PlaySFX(buttonSFX);
    
    /// <summary>
    /// 사용 가능한 SFX 오디오 소스 찾기
    /// </summary>
    AudioSource GetAvailableSFXSource()
    {
        foreach (AudioSource source in sfxPool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        
        // 모든 소스가 사용 중이면 첫 번째 것 사용
        return sfxPool[0];
    }
    
    /// <summary>
    /// 모든 효과음 정지
    /// </summary>
    public void StopAllSFX()
    {
        foreach (AudioSource source in sfxPool)
        {
            source.Stop();
        }
        if (sfxSource != null) sfxSource.Stop();
    }
    
    #endregion
    
    #region 볼륨 및 설정 관리
    
    /// <summary>
    /// 마스터 볼륨 설정
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveSettings();
    }
    
    /// <summary>
    /// BGM 볼륨 설정
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveSettings();
    }
    
    /// <summary>
    /// 효과음 볼륨 설정
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveSettings();
    }
    
    /// <summary>
    /// BGM 활성화/비활성화
    /// </summary>
    public void SetBGMEnabled(bool enabled)
    {
        isBGMEnabled = enabled;
        
        if (enabled)
        {
            if (bgmSource != null && !bgmSource.isPlaying && mainBGM != null)
            {
                PlayBGM(mainBGM);
            }
        }
        else
        {
            StopBGM();
        }
        
        SaveSettings();
    }
    
    /// <summary>
    /// 효과음 활성화/비활성화
    /// </summary>
    public void SetSFXEnabled(bool enabled)
    {
        isSFXEnabled = enabled;
        
        if (!enabled)
        {
            StopAllSFX();
        }
        
        SaveSettings();
    }
    
    /// <summary>
    /// 볼륨 설정 적용
    /// </summary>
    void ApplyVolumeSettings()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume * masterVolume;
        }
        
        // SFX는 재생 시마다 볼륨 적용되므로 여기서는 설정만 저장
    }
    
    /// <summary>
    /// 설정 저장
    /// </summary>
    void SaveSettings()
    {
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.SetInt(BGM_ENABLED_KEY, isBGMEnabled ? 1 : 0);
        PlayerPrefs.SetInt(SFX_ENABLED_KEY, isSFXEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 설정 로드
    /// </summary>
    void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.7f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        isBGMEnabled = PlayerPrefs.GetInt(BGM_ENABLED_KEY, 1) == 1;
        isSFXEnabled = PlayerPrefs.GetInt(SFX_ENABLED_KEY, 1) == 1;
    }
    
    #endregion
    
    #region 디버그 및 유틸리티
    
    /// <summary>
    /// 현재 재생 중인 BGM 정보
    /// </summary>
    public string GetCurrentBGMInfo()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            return $"재생 중: {bgmSource.clip.name}";
        }
        return "BGM 재생 중 아님";
    }
    
    /// <summary>
    /// 사운드 시스템 상태 정보
    /// </summary>
    public string GetSoundSystemInfo()
    {
        return $"Master: {masterVolume:F1}, BGM: {bgmVolume:F1} ({isBGMEnabled}), SFX: {sfxVolume:F1} ({isSFXEnabled})";
    }
    
    // 디버그용 테스트 메서드들
    [ContextMenu("Test All SFX")]
    public void TestAllSFX()
    {
        StartCoroutine(TestSFXSequence());
    }
    
    IEnumerator TestSFXSequence()
    {
        PlayTouchSFX(); yield return new WaitForSeconds(0.5f);
        PlayTouchSFX(); yield return new WaitForSeconds(0.5f); // 두 번 재생해서 랜덤 확인
        PlayWaterDropSFX(); yield return new WaitForSeconds(0.5f);
        PlayWaterDropSFX(); yield return new WaitForSeconds(0.5f); // 두 번 재생해서 랜덤 확인
        PlayGrowthSFX(); yield return new WaitForSeconds(0.5f);
        PlayCrackSFX(); yield return new WaitForSeconds(0.5f);
        PlayCrackSFX(); yield return new WaitForSeconds(0.5f); // 두 번 재생해서 랜덤 확인
        PlayBreakSFX(); yield return new WaitForSeconds(0.5f);
        PlayCompleteSFX(); yield return new WaitForSeconds(0.5f);
        PlayPopupSFX(); yield return new WaitForSeconds(0.5f);
        PlayButtonSFX();
    }
    
    #endregion
}