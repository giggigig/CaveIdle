using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 게임 UI 전체 관리 시스템
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Humidity Display")]
    public TextMeshProUGUI humidityText;
    public Slider humiditySlider;
    public Image humidityFillImage;
    public Color lowHumidityColor = Color.red;
    public Color optimalHumidityColor = Color.green;
    public Color highHumidityColor = Color.blue;
    
    [Header("Mushroom Count Display")]
    public TextMeshProUGUI activeMuhroomsText;
    public TextMeshProUGUI totalHarvestedText;
    public TextMeshProUGUI mushroomInventoryText;
    
    [Header("Stalactite Display (StalactiteUIManager Original)")]
    public TextMeshProUGUI stalactiteTitleText;      // "종유석"
    public TextMeshProUGUI lengthText;               // "길이    XX.XXXmm"  
    public TextMeshProUGUI dayText;                  // "나이     N일째"
    public TextMeshProUGUI crackText;                // "균열도   XX"
    public TextMeshProUGUI totalDayText;             // 우측 상단 "N일째"
    
    [Header("System References")]
    public MushroomManager mushroomManager;
    public StalactiteGrowth stalactiteGrowth;
    public InventorySystem inventorySystem;
    
    // Singleton pattern
    public static UIManager Instance;
    
    // Game tracking (from StalactiteUIManager)
    private DateTime gameStartDate;
    private const string GAME_START_DATE_KEY = "game_start_date";
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 자동으로 컴포넌트 찾기
        if (mushroomManager == null)
            mushroomManager = FindObjectOfType<MushroomManager>();
            
        if (stalactiteGrowth == null)
            stalactiteGrowth = FindObjectOfType<StalactiteGrowth>();
            
        if (inventorySystem == null)
            inventorySystem = FindObjectOfType<InventorySystem>();
            
        // 게임 시작일 로드/설정 (from StalactiteUIManager)
        LoadGameStartDate();
        
        // 초기 UI 설정
        if (stalactiteTitleText != null)
            stalactiteTitleText.text = "종유석";
    }
    
    void Update()
    {
        // StalactiteUIManager 원본: 1초마다 UI 업데이트 (성능 최적화)
        if (Time.time % 1f < Time.deltaTime)
        {
            UpdateUI();
        }
    }
    
    /// <summary>
    /// UI 업데이트 (매 프레임)
    /// </summary>
    void UpdateUI()
    {
        UpdateHumidityDisplay();
        UpdateMushroomDisplay();
        UpdateStatsDisplay();
    }
    
    /// <summary>
    /// 습도 표시 업데이트
    /// </summary>
    void UpdateHumidityDisplay()
    {
        if (mushroomManager == null) return;
        
        float humidity = mushroomManager.currentHumidity;
        
        // 습도 텍스트 업데이트
        if (humidityText != null)
        {
            humidityText.text = $"습도: {humidity:F1}%";
        }
        
        // 습도 슬라이더 업데이트
        if (humiditySlider != null)
        {
            humiditySlider.value = humidity / 100f;
        }
        
        // 습도에 따른 색상 변경
        if (humidityFillImage != null)
        {
            Color targetColor;
            if (humidity < mushroomManager.optimalHumidityMin)
            {
                targetColor = lowHumidityColor;
            }
            else if (humidity > mushroomManager.optimalHumidityMax)
            {
                targetColor = highHumidityColor;
            }
            else
            {
                targetColor = optimalHumidityColor;
            }
            
            humidityFillImage.color = targetColor;
        }
    }
    
    /// <summary>
    /// 버섯 관련 표시 업데이트
    /// </summary>
    void UpdateMushroomDisplay()
    {
        // 활성 버섯 개수
        if (activeMuhroomsText != null && mushroomManager != null)
        {
            int activeMushrooms = mushroomManager.transform.childCount;
            activeMuhroomsText.text = $"활성 버섯: {activeMushrooms}/{mushroomManager.maxMushrooms}";
        }
        
        // 총 수확한 버섯 개수
        if (totalHarvestedText != null && inventorySystem != null)
        {
            totalHarvestedText.text = $"총 수확: {inventorySystem.GetTotalHarvestedCount()}";
        }
        
        // 인벤토리 버섯 개수
        if (mushroomInventoryText != null && inventorySystem != null)
        {
            mushroomInventoryText.text = $"보유 버섯: {inventorySystem.GetTotalMushroomCount()}";
        }
    }
    
    /// <summary>
    /// 종유석 관련 표시 업데이트 (StalactiteUIManager 원본 방식 유지)
    /// </summary>
    void UpdateStatsDisplay()
    {
        if (stalactiteGrowth == null) return;
        
        // 종유석 정보 가져오기
        StalactiteInfo info = stalactiteGrowth.GetInfo();
        
        // 길이 표시 (소수점 7자리) - StalactiteUIManager 원본
        if (lengthText != null)
        {
            lengthText.text = $"길이    {info.lengthMM:F7}mm";
        }
        
        // 나이 표시 - StalactiteUIManager 원본
        if (dayText != null)
        {
            dayText.text = $"나이     {info.daysElapsed}일째";
        }
        
        // 균열도 표시 (색상 포함) - StalactiteUIManager 원본
        if (crackText != null)
        {
            string crackColor = GetCrackColor(info.crackLevel);
            crackText.text = $"균열도   <color={crackColor}>{info.crackLevel:F1}</color>";
        }
        
        // 전체 게임 진행일 표시 (우측 상단) - StalactiteUIManager 원본
        if (totalDayText != null)
        {
            DateTime now = DateTime.Now;
            TimeSpan totalElapsed = now - gameStartDate;
            int totalDays = totalElapsed.Days + 1; // 1일째부터 시작
            totalDayText.text = $"{totalDays}일째";
        }
    }
    
    /// <summary>
    /// 버섯 수확 알림 (다른 스크립트에서 호출)
    /// </summary>
    public void OnMushroomHarvested(string mushroomType, int count = 1)
    {
        // TODO: 수확 알림 효과 추가
        Debug.Log($"{mushroomType} 버섯 {count}개 수확!");
    }
    
    /// <summary>
    /// 습도 변화 알림
    /// </summary>
    public void OnHumidityChanged(float newHumidity, float change)
    {
        // TODO: 습도 변화 시각 효과 추가
        Debug.Log($"습도 변화: {change:+F1}% (현재: {newHumidity:F1}%)");
    }
    
    #region StalactiteUIManager Functions
    
    /// <summary>
    /// 균열도에 따른 색상 반환 (from StalactiteUIManager)
    /// </summary>
    string GetCrackColor(float crackLevel)
    {
        if (crackLevel == 0f) return "white";          // 균열 없음: 흰색
        else if (crackLevel < 30f) return "yellow";    // 경미한 균열: 노란색  
        else if (crackLevel < 60f) return "orange";    // 중간 균열: 주황색
        else if (crackLevel < 90f) return "red";       // 심한 균열: 빨간색
        else return "#FF0080";                         // 파괴 직전: 자주색 (위험!)
    }
    
    /// <summary>
    /// 게임 시작일 로드/설정 (from StalactiteUIManager)
    /// </summary>
    void LoadGameStartDate()
    {
        if (PlayerPrefs.HasKey(GAME_START_DATE_KEY))
        {
            long timeBinary = Convert.ToInt64(PlayerPrefs.GetString(GAME_START_DATE_KEY));
            gameStartDate = DateTime.FromBinary(timeBinary);
        }
        else
        {
            gameStartDate = DateTime.Now;
            PlayerPrefs.SetString(GAME_START_DATE_KEY, gameStartDate.ToBinary().ToString());
            PlayerPrefs.Save();
        }
    }
    
    /// <summary>
    /// 다른 종유석으로 타겟 변경 (StalactiteUIManager 원본)
    /// </summary>
    public void SetTarget(StalactiteGrowth newTarget)
    {
        stalactiteGrowth = newTarget;
        UpdateUI();
    }
    
    /// <summary>
    /// UI 강제 새로고침 (StalactiteUIManager 원본)
    /// </summary>
    public void RefreshUI()
    {
        UpdateUI();
    }
    
    /// <summary>
    /// 게임 시작일 리셋 (디버그용, StalactiteUIManager 원본)
    /// </summary>
    [ContextMenu("Reset Game Start Date")]
    public void ResetGameStartDate()
    {
        gameStartDate = DateTime.Now;
        PlayerPrefs.SetString(GAME_START_DATE_KEY, gameStartDate.ToBinary().ToString());
        PlayerPrefs.Save();
    }
    
    #endregion
}