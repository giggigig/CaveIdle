using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

#if UNITY_ANDROID || UNITY_IOS
using Unity.Notifications.Android;
using Unity.Notifications.iOS;
#endif

/// <summary>
/// 범용 게임 이벤트 알림 시스템 (오프라인 성장, 종유석 파괴, 완성 등)
/// </summary>
public class GameNotificationManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject notificationPopup;         // 범용 알림 팝업 창
    public TextMeshProUGUI titleText;            // 팝업 제목 "오프라인 성장" / "종유석 파괴!" 등
    public TextMeshProUGUI mainText;             // 메인 메시지
    public TextMeshProUGUI subText;              // 서브 메시지 (선택사항)
    public Button confirmButton;                 // "확인" 버튼
    public Image iconImage;                      // 아이콘 이미지 (선택사항)
    
    [Header("Icons")]
    public Sprite offlineIcon;                   // 오프라인 아이콘
    public Sprite brokenIcon;                    // 종유석 파괴 아이콘  
    public Sprite completeIcon;                  // 종유석 완성 아이콘
    public Sprite defaultIcon;                   // 기본 아이콘
    
    [Header("Target")]
    public StalactiteGrowth stalactiteGrowth;    // 종유석 참조
    
    [Header("Notification Settings")]
    public bool enableNotifications = true;      // 알림 활성화 여부
    
    private const string LAST_CLOSE_TIME_KEY = "last_app_close_time";
    private bool hasShownNotificationPopup = false;
    
    void Start()
    {
        // 앱 시작 시 오프라인 성장 체크
        CheckOfflineGrowth();
        
        // 확인 버튼 이벤트
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(CloseNotificationPopup);
        }
        
        // 알림 권한 요청
        if (enableNotifications)
        {
            RequestNotificationPermission();
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 앱이 백그라운드로 갈 때
            OnAppGoingBackground();
        }
        else
        {
            // 앱이 포그라운드로 올 때
            OnAppComingForeground();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // 포커스 잃을 때 (앱 닫기, 다른 앱으로 이동)
            OnAppGoingBackground();
        }
        else
        {
            // 포커스 얻을 때
            OnAppComingForeground();
        }
    }
    
    void OnAppGoingBackground()
    {
        Debug.Log("앱이 백그라운드로 이동 - 시간 저장 및 알림 스케줄링");
        
        // 현재 시간 저장
        SaveAppCloseTime();
        
        // 푸시 알림 스케줄링
        if (enableNotifications)
        {
            ScheduleGrowthNotifications();
        }
    }
    
    void OnAppComingForeground()
    {
        Debug.Log("앱이 포그라운드로 복귀 - 오프라인 성장 계산");
        
        // 오프라인 성장 체크 (팝업 중복 방지)
        if (!hasShownNotificationPopup)
        {
            CheckOfflineGrowth();
        }
        
        // 예약된 알림 취소
        CancelScheduledNotifications();
    }
    
    /// <summary>
    /// 앱 종료 시간 저장
    /// </summary>
    void SaveAppCloseTime()
    {
        DateTime now = DateTime.Now;
        PlayerPrefs.SetString(LAST_CLOSE_TIME_KEY, now.ToBinary().ToString());
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 오프라인 성장 계산 및 팝업 표시
    /// </summary>
    void CheckOfflineGrowth()
    {
        if (!PlayerPrefs.HasKey(LAST_CLOSE_TIME_KEY))
        {
            Debug.Log("첫 실행이거나 오프라인 데이터 없음");
            return;
        }
        
        // 마지막 종료 시간 로드
        long lastCloseBinary = Convert.ToInt64(PlayerPrefs.GetString(LAST_CLOSE_TIME_KEY));
        DateTime lastCloseTime = DateTime.FromBinary(lastCloseBinary);
        DateTime now = DateTime.Now;
        
        TimeSpan offlineTime = now - lastCloseTime;
        
        // 최소 1분 이상 꺼져있었을 때만 팝업
        if (offlineTime.TotalMinutes < 1)
        {
            Debug.Log("오프라인 시간이 너무 짧음 (1분 미만)");
            return;
        }
        
        // 오프라인 성장 계산
        CalculateOfflineGrowth(offlineTime);
    }
    
    /// <summary>
    /// 오프라인 성장량 계산 및 팝업 표시
    /// </summary>
    void CalculateOfflineGrowth(TimeSpan offlineTime)
    {
        if (stalactiteGrowth == null) return;
        
        // 성장 전 상태 저장
        float beforeLength = stalactiteGrowth.currentLength;
        float beforeCrack = stalactiteGrowth.crackLevel;
        
        // 종유석 성장 업데이트 (자동으로 계산됨)
        stalactiteGrowth.UpdateGrowth();
        
        // 성장 후 상태
        float afterLength = stalactiteGrowth.currentLength;
        float afterCrack = stalactiteGrowth.crackLevel;
        
        // 성장량 계산
        float lengthGrown = afterLength - beforeLength;
        float crackRecovered = beforeCrack - afterCrack; // 균열은 감소가 좋은 것
        
        // 오프라인 성장 팝업 표시
        ShowOfflineGrowthPopup(offlineTime, lengthGrown, crackRecovered);
    }
    
    /// <summary>
    /// 오프라인 성장 팝업 표시
    /// </summary>
    void ShowOfflineGrowthPopup(TimeSpan offlineTime, float lengthGrown, float crackRecovered)
    {
        string timeString = FormatTimeSpan(offlineTime);
        string mainMessage = "";
        string subMessage = "";
        
        if (lengthGrown > 0)
        {
            mainMessage = $"종유석이 {lengthGrown:F2}mm 자랐습니다!";
        }
        else
        {
            mainMessage = "종유석이 조금 자랐습니다!";
        }
        
        if (crackRecovered > 0)
        {
            subMessage = $"균열도가 {crackRecovered:F1} 회복되었습니다";
        }
        
        ShowNotification(
            title: "오프라인 성장",
            message: $"{timeString} 동안 자리를 비웠습니다\n{mainMessage}",
            subMessage: subMessage,
            icon: offlineIcon
        );
    }
    
    /// <summary>
    /// 범용 알림 팝업 표시
    /// </summary>
    public void ShowNotification(string title, string message, string subMessage = "", Sprite icon = null)
    {
        if (notificationPopup == null) return;
        
        hasShownNotificationPopup = true;
        notificationPopup.SetActive(true);
        
        // 제목 설정
        if (titleText != null)
        {
            titleText.text = title;
        }
        
        // 메인 메시지 설정
        if (mainText != null)
        {
            mainText.text = message;
        }
        
        // 서브 메시지 설정
        if (subText != null)
        {
            if (!string.IsNullOrEmpty(subMessage))
            {
                subText.text = subMessage;
                subText.gameObject.SetActive(true);
            }
            else
            {
                subText.gameObject.SetActive(false);
            }
        }
        
        // 아이콘 설정
        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }
        
        Debug.Log($"알림 팝업 표시: {title} - {message}");
    }
    
    /// <summary>
    /// 종유석 파괴 알림
    /// </summary>
    public void ShowStalactiteBreakNotification()
    {
        ShowNotification(
            title: "종유석 파괴!",
            message: "균열이 너무 많아 종유석이 부서졌습니다 💥",
            subMessage: "처음부터 다시 키워보세요!",
            icon: brokenIcon
        );
    }
    
    /// <summary>
    /// 종유석 완성 알림
    /// </summary>
    public void ShowStalactiteCompleteNotification()
    {
        ShowNotification(
            title: "축하합니다!",
            message: "종유석이 완전히 성장했습니다! ✨",
            subMessage: "새로운 종유석이 곧 생겨날 거예요",
            icon: completeIcon
        );
    }
    
    /// <summary>
    /// 커스텀 알림 (다른 스크립트에서 호출 가능)
    /// </summary>
    public void ShowCustomNotification(string title, string message, string subMessage = "")
    {
        ShowNotification(title, message, subMessage, defaultIcon);
    }
    
    /// <summary>
    /// 시간을 읽기 쉬운 형태로 포맷
    /// </summary>
    string FormatTimeSpan(TimeSpan time)
    {
        if (time.TotalDays >= 1)
        {
            return $"{time.Days}일 {time.Hours}시간 {time.Minutes}분";
        }
        else if (time.TotalHours >= 1)
        {
            return $"{time.Hours}시간 {time.Minutes}분";
        }
        else
        {
            return $"{time.Minutes}분";
        }
    }
    
    /// <summary>
    /// 알림 팝업 닫기
    /// </summary>
    public void CloseNotificationPopup()
    {
        if (notificationPopup != null)
        {
            notificationPopup.SetActive(false);
        }
        hasShownNotificationPopup = false;
    }
    
    #region 푸시 알림 시스템
    
    /// <summary>
    /// 알림 권한 요청
    /// </summary>
    void RequestNotificationPermission()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.RequestPermission();
#elif UNITY_IOS
        StartCoroutine(RequestIOSPermission());
#endif
    }
    
#if UNITY_IOS
    System.Collections.IEnumerator RequestIOSPermission()
    {
        var request = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound, true);
        yield return request;
        Debug.Log($"iOS 알림 권한 요청 결과: {request.Granted}");
    }
#endif
    
    /// <summary>
    /// 성장 알림 스케줄링
    /// </summary>
    void ScheduleGrowthNotifications()
    {
        // 기존 알림 취소
        CancelScheduledNotifications();
        
#if UNITY_ANDROID
        // Android 알림 채널 생성
        var channel = new AndroidNotificationChannel()
        {
            Id = "cave_growth",
            Name = "종유석 성장 알림",
            Importance = Importance.Default,
            Description = "종유석이 자라고 있어요!",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        
        // 1시간 후 알림
        var notification1h = new AndroidNotification();
        notification1h.Title = "동굴 키우기";
        notification1h.Text = "종유석이 자라고 있어요! 🗿";
        notification1h.FireTime = DateTime.Now.AddHours(1);
        notification1h.SmallIcon = "icon_small";
        AndroidNotificationCenter.SendNotification(notification1h, "cave_growth");
        
        // 6시간 후 알림
        var notification6h = new AndroidNotification();
        notification6h.Title = "동굴 키우기";
        notification6h.Text = "종유석이 많이 자랐을 거예요! 확인해보세요 ✨";
        notification6h.FireTime = DateTime.Now.AddHours(6);
        notification6h.SmallIcon = "icon_small";
        AndroidNotificationCenter.SendNotification(notification6h, "cave_growth");
        
#elif UNITY_IOS
        // 1시간 후 알림
        var notification1h = new iOSNotification()
        {
            Identifier = "cave_growth_1h",
            Title = "동굴 키우기",
            Body = "종유석이 자라고 있어요! 🗿",
            ShowInForeground = false,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = "cave_category",
            ThreadIdentifier = "cave_thread",
            Trigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = new TimeSpan(1, 0, 0),
                Repeats = false
            }
        };
        iOSNotificationCenter.ScheduleNotification(notification1h);
        
        // 6시간 후 알림
        var notification6h = new iOSNotification()
        {
            Identifier = "cave_growth_6h",
            Title = "동굴 키우기",
            Body = "종유석이 많이 자랐을 거예요! 확인해보세요 ✨",
            ShowInForeground = false,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = "cave_category",
            ThreadIdentifier = "cave_thread",
            Trigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = new TimeSpan(6, 0, 0),
                Repeats = false
            }
        };
        iOSNotificationCenter.ScheduleNotification(notification6h);
#endif
        
        Debug.Log("성장 알림 스케줄링 완료");
    }
    
    /// <summary>
    /// 예약된 알림 취소
    /// </summary>
    void CancelScheduledNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
        Debug.Log("예약된 알림 모두 취소");
    }
    
    #endregion
    
    // 디버그용 - 각종 알림 테스트
    [ContextMenu("Test Offline Popup")]
    public void TestOfflinePopup()
    {
        TimeSpan testTime = TimeSpan.FromHours(3); // 3시간 테스트
        ShowOfflineGrowthPopup(testTime, 2.5f, 1.2f);
    }
    
    [ContextMenu("Test Break Notification")]
    public void TestBreakNotification()
    {
        ShowStalactiteBreakNotification();
    }
    
    [ContextMenu("Test Complete Notification")]
    public void TestCompleteNotification()
    {
        ShowStalactiteCompleteNotification();
    }
}