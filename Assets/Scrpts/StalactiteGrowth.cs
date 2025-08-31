using UnityEngine;
using System;

/// <summary>
/// Phase 1: 기본 종유석 성장 시스템 + 균열 시스템
/// </summary>
public class StalactiteGrowth : MonoBehaviour
{
    [Header("Growth Settings")]
    public float growthTimeInDays = 30f;     // 30일 만에 완전 성장
    public Vector2 minScale = new Vector2(0.7f, 0.5f);   // 시작 크기 (x, y)
    public Vector2 maxScale = new Vector2(2f, 1.5f);     // 최대 크기 (x, y)

    [Header("Crack System")]
    public int maxCrackLevel = 99;           // 최대 균열도
    public float crackRecoveryHours = 1f;    // 균열 1 회복하는데 걸리는 시간 (시간)
    public float crackPerTouch = 0.5f;       // 터치당 균열 증가량

    [Header("Current State")]
    public float currentLength = 0f;         // 현재 길이 (mm)
    public Vector2 currentScale;             // 현재 크기 (x, y)
    public float crackLevel = 0f;            // 균열도 (0~99, 소수점 가능)
    public DateTime creationTime;            // 생성 시간
    public DateTime lastTouchTime;           // 마지막 터치 시간

    private const string CREATION_TIME_KEY = "stalactite_creation_time";
    private const string CRACK_LEVEL_KEY = "stalactite_crack_level";
    private const string LAST_TOUCH_KEY = "stalactite_last_touch";
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        LoadData();
        UpdateGrowthInternal();
    }

    void Update()
    {
        // 1초마다 성장 업데이트
        if (Time.time % 1f < Time.deltaTime)
        {
            UpdateGrowthInternal();
        }
    }

    /// <summary>
    /// 성장 업데이트 (외부에서 호출 가능)
    /// </summary>
    public void UpdateGrowth()
    {
        UpdateGrowthInternal();
    }

    /// <summary>
    /// 실제 성장 계산 로직
    /// </summary>
    void UpdateGrowthInternal()
    {
        // 현재 시간과 생성 시간 차이 계산
        DateTime now = DateTime.Now;
        TimeSpan elapsed = now - creationTime;
        double totalDays = elapsed.TotalDays;

        // 균열 자연 회복 계산
        UpdateCrackRecovery(now);

        // 성장 진행률 (0~1)
        float growthProgress = (float)(totalDays / growthTimeInDays);
        growthProgress = Mathf.Clamp01(growthProgress);

        // 완성 체크 (새로 완성되었을 때만)
        bool wasComplete = currentLength >= 29.9f; // 거의 완성 상태

        // 길이 계산 (mm 단위) - 실제 저장되는 값
        float newLength = growthProgress * 30f; // 30일 → 30mm

        // 길이가 변했으면 로그 출력
        if (Mathf.Abs(newLength - currentLength) > 0.001f)
        {
            Debug.Log($"길이 변화: {currentLength:F6}mm → {newLength:F6}mm (차이: {newLength - currentLength:F6}mm)");
            currentLength = newLength;
        }
        else
        {
            Debug.Log($"길이 변화 없음: {currentLength:F6}mm (진행률: {growthProgress:F8})");
        }

        // 완성 알림 (새로 완성되었을 때만)
        bool isNowComplete = currentLength >= 29.9f;
        if (!wasComplete && isNowComplete)
        {
            OnStalactiteComplete();
        }

        // 크기 계산 (X, Y 각각 다르게)
        currentScale = Vector2.Lerp(minScale, maxScale, growthProgress);

        // 스케일 적용 (Vector2를 Vector3로 변환, Z는 1 유지)
        transform.localScale = new Vector3(currentScale.x, currentScale.y, 1f);

        // 데이터 저장
        SaveData();
    }

    /// <summary>
    /// 균열 자연 회복 계산
    /// </summary>
    void UpdateCrackRecovery(DateTime now)
    {
        if (crackLevel <= 0f) return;

        TimeSpan timeSinceLastTouch = now - lastTouchTime;
        double hoursSinceTouch = timeSinceLastTouch.TotalHours;

        // 회복할 균열량 계산
        float crackToRecover = (float)(hoursSinceTouch / crackRecoveryHours);

        if (crackToRecover > 0f)
        {
            crackLevel = Mathf.Max(0f, crackLevel - crackToRecover);
            lastTouchTime = now.AddHours(-((hoursSinceTouch % crackRecoveryHours))); // 나머지 시간 보존
            Debug.Log($"균열 회복: -{crackToRecover:F1}, 현재 균열도: {crackLevel:F1}");
        }
    }

    /// <summary>
    /// 터치로 인한 균열 증가 (물방울 시스템에서 호출)
    /// </summary>
    public void AddCrackFromTouch()
    {
        crackLevel += crackPerTouch;
        lastTouchTime = DateTime.Now;

        Debug.Log($"터치로 균열 증가: +{crackPerTouch:F1}, 현재 균열도: {crackLevel:F1}");

        // 균열도 한계 체크
        if (crackLevel >= maxCrackLevel)
        {
            BreakStalactite();
        }

        SaveData();
    }

    /// <summary>
    /// 종유석 파괴 (균열도 한계 도달 시)
    /// </summary>
    void BreakStalactite()
    {
        Debug.Log("종유석이 파괴되었습니다! 처음부터 다시 시작합니다.");

        // 처음 상태로 리셋
        creationTime = DateTime.Now;
        lastTouchTime = DateTime.Now;
        crackLevel = 0f;
        currentLength = 0f;

        // 즉시 업데이트
        UpdateGrowthInternal();

        // 파괴 알림 표시
        OnStalactiteBreak();
    }

    /// <summary>
    /// 종유석 파괴 시 호출되는 이벤트
    /// </summary>
    void OnStalactiteBreak()
    {
        // 알림 매니저 찾아서 파괴 알림 표시
        GameNotificationManager notificationManager = FindObjectOfType<GameNotificationManager>();
        if (notificationManager != null)
        {
            notificationManager.ShowStalactiteBreakNotification();
        }

        Debug.Log("💥 종유석 파괴 이펙트!");
    }

    /// <summary>
    /// 종유석 완성 시 호출되는 이벤트
    /// </summary>
    void OnStalactiteComplete()
    {
        // 알림 매니저 찾아서 완성 알림 표시
        GameNotificationManager notificationManager = FindObjectOfType<GameNotificationManager>();
        if (notificationManager != null)
        {
            notificationManager.ShowStalactiteCompleteNotification();
        }

        Debug.Log("✨ 종유석 완성!");
    }

    /// <summary>
    /// UI에서 사용할 정보 반환 (실시간 계산)
    /// </summary>
    public StalactiteInfo GetInfo()
    {
        DateTime now = DateTime.Now;
        TimeSpan elapsed = now - creationTime;

        // 디버그: 시간 정보 출력
        Debug.Log($"현재 시간: {now:HH:mm:ss}, 생성 시간: {creationTime:HH:mm:ss}, 경과: {elapsed.TotalHours:F2}시간");

        // 균열 자연 회복 계산도 실시간으로
        float oldCrackLevel = crackLevel;
        UpdateCrackRecovery(now);

        // 균열도가 변했으면 저장
        if (oldCrackLevel != crackLevel)
        {
            SaveData();
        }

        // 실시간으로 다시 계산
        double totalDays = elapsed.TotalDays;
        float growthProgress = (float)(totalDays / growthTimeInDays);
        growthProgress = Mathf.Clamp01(growthProgress);

        // 실시간 길이 계산
        float realtimeLength = growthProgress * 30f; // 30일 → 30mm

        // 실시간 크기 계산
        Vector2 realtimeScale = Vector2.Lerp(minScale, maxScale, growthProgress);

       // Debug.Log($"성장 진행률: {growthProgress:F6} ({totalDays:F3}일 / {growthTimeInDays}일), 길이: {realtimeLength:F6}mm");

        return new StalactiteInfo
        {
            lengthMM = realtimeLength,        // 실시간 계산된 길이
            daysElapsed = elapsed.Days + 1,   // 1일째부터 시작
            scaleX = realtimeScale.x,         // 실시간 계산된 X 크기
            scaleY = realtimeScale.y,         // 실시간 계산된 Y 크기
            crackLevel = crackLevel           // 균열도는 저장된 값 사용
        };
    }

    void SaveData()
    {
        Debug.Log($"데이터 저장: 길이={currentLength:F6}mm, 균열도={crackLevel:F2}");
        PlayerPrefs.SetString(CREATION_TIME_KEY, creationTime.ToBinary().ToString());
        PlayerPrefs.SetString(CRACK_LEVEL_KEY, crackLevel.ToString());
        PlayerPrefs.SetString(LAST_TOUCH_KEY, lastTouchTime.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    void LoadData()
    {
        // 생성 시간 로드
        if (PlayerPrefs.HasKey(CREATION_TIME_KEY))
        {
            try
            {
                long timeBinary = Convert.ToInt64(PlayerPrefs.GetString(CREATION_TIME_KEY));
                DateTime loadedTime = DateTime.FromBinary(timeBinary);

                // 시간 유효성 검사
                DateTime minTime = DateTime.MinValue.AddDays(1);
                DateTime maxTime = DateTime.MaxValue.AddDays(-1);

                if (loadedTime < minTime || loadedTime > maxTime)
                {
                    Debug.LogWarning($"로드된 시간이 유효하지 않습니다: {loadedTime}. 현재 시간으로 설정합니다.");
                    creationTime = DateTime.Now;
                }
                else
                {
                    creationTime = loadedTime;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"시간 로드 실패: {e.Message}. 현재 시간으로 설정합니다.");
                creationTime = DateTime.Now;
            }
        }
        else
        {
            creationTime = DateTime.Now;
        }

        // 균열도 로드
        string crackLevelStr = PlayerPrefs.GetString(CRACK_LEVEL_KEY, "0");
        if (!float.TryParse(crackLevelStr, out crackLevel))
        {
            crackLevel = 0f;
        }

        // 마지막 터치 시간 로드
        if (PlayerPrefs.HasKey(LAST_TOUCH_KEY))
        {
            try
            {
                long touchTimeBinary = Convert.ToInt64(PlayerPrefs.GetString(LAST_TOUCH_KEY));
                lastTouchTime = DateTime.FromBinary(touchTimeBinary);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"터치 시간 로드 실패: {e.Message}. 현재 시간으로 설정합니다.");
                lastTouchTime = DateTime.Now;
            }
        }
        else
        {
            lastTouchTime = DateTime.Now;
        }

        Debug.Log($"데이터 로드 완료 - 생성: {creationTime}, 균열: {crackLevel}, 터치: {lastTouchTime}");
    }

    // 디버그용: 성장 가속
    [ContextMenu("Force 1 Day Growth")]
    public void ForceOneDayGrowth()
    {
        creationTime = creationTime.AddDays(-1);
        UpdateGrowthInternal();
    }

    [ContextMenu("Reset Growth")]
    public void ResetGrowth()
    {
        creationTime = DateTime.Now;
        lastTouchTime = DateTime.Now;
        crackLevel = 0f;
        UpdateGrowthInternal();
    }

    [ContextMenu("Add Crack")]
    public void AddCrack()
    {
        AddCrackFromTouch();
    }

    [ContextMenu("Debug Current Time")]
    public void DebugCurrentTime()
    {
        DateTime now = DateTime.Now;
        TimeSpan elapsed = now - creationTime;
        Debug.Log($"현재 시간: {now}");
        Debug.Log($"생성 시간: {creationTime}");
        Debug.Log($"경과 시간: {elapsed.TotalDays:F2}일 ({elapsed.TotalHours:F1}시간)");
        Debug.Log($"현재 길이: {currentLength:F3}mm");
        Debug.Log($"균열도: {crackLevel:F1}");
    }
}

/// <summary>
/// 종유석 정보를 담는 구조체
/// </summary>
[System.Serializable]
public struct StalactiteInfo
{
    public float lengthMM;      // 길이 (mm)
    public int daysElapsed;     // 경과 일수
    public float scaleX;        // 현재 X 크기
    public float scaleY;        // 현재 Y 크기
    public float crackLevel;    // 균열도 (0~99, 소수점 가능)
}