using UnityEngine;
using TMPro;
using System;

/// <summary>
/// Phase 1: 종유석 정보를 표시하는 UI 매니저 (해파리 키우기 스타일)
/// </summary>
public class StalactiteUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI stalactiteTitleText;      // "종유석"
    public TextMeshProUGUI lengthText;               // "길이    XX.XXXmm"
    public TextMeshProUGUI dayText;                  // "나이     N일째"
    public TextMeshProUGUI crackText;                // "균열도   XX"
    public TextMeshProUGUI totalDayText;             // 우측 상단 "N일째"

    [Header("Target")]
    public StalactiteGrowth targetStalactite;       // 표시할 종유석

    private DateTime gameStartDate;
    private const string GAME_START_DATE_KEY = "game_start_date";

    void Start()
    {
        // 게임 시작일 로드/설정
        LoadGameStartDate();

        // 초기 UI 설정
        if (stalactiteTitleText != null)
            stalactiteTitleText.text = "종유석";

        UpdateUI();
    }

    void Update()
    {
        // 1초마다 UI 업데이트 (성능 최적화)
        if (Time.time % 1f < Time.deltaTime)
        {
            UpdateUI();
        }
    }

    /// <summary>
    /// 균열도에 따른 색상 반환
    /// </summary>
    string GetCrackColor(float crackLevel)
    {
        if (crackLevel == 0f) return "white";          // 균열 없음: 흰색
        else if (crackLevel < 30f) return "yellow";    // 경미한 균열: 노란색  
        else if (crackLevel < 60f) return "orange";    // 중간 균열: 주황색
        else if (crackLevel < 90f) return "red";       // 심한 균열: 빨간색
        else return "#FF0080";                         // 파괴 직전: 자주색 (위험!)
    }

    void UpdateUI()
    {
        if (targetStalactite == null) return;

        // 종유석 정보 가져오기
        StalactiteInfo info = targetStalactite.GetInfo();

        // 길이 표시 (소수점 7자리)
        if (lengthText != null)
        {
            lengthText.text = $"길이    {info.lengthMM:F7}mm";
           // Debug.Log($"UI 업데이트: 길이 = {info.lengthMM:F7}mm");
        }

        // 나이 표시
        if (dayText != null)
        {
            dayText.text = $"나이     {info.daysElapsed}일째";
        }

        // 균열도 표시
        if (crackText != null)
        {
            string crackColor = GetCrackColor(info.crackLevel);
            crackText.text = $"균열도   <color={crackColor}>{info.crackLevel:F1}</color>";
        }

        // 전체 게임 진행일 표시 (우측 상단)
        if (totalDayText != null)
        {
            DateTime now = DateTime.Now;
            TimeSpan totalElapsed = now - gameStartDate;
            int totalDays = totalElapsed.Days + 1; // 1일째부터 시작
            totalDayText.text = $"{totalDays}일째";
        }
    }

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
    /// 다른 종유석으로 타겟 변경
    /// </summary>
    public void SetTarget(StalactiteGrowth newTarget)
    {
        targetStalactite = newTarget;
        UpdateUI();
    }

    /// <summary>
    /// UI 강제 새로고침
    /// </summary>
    public void RefreshUI()
    {
        UpdateUI();
    }

    // 디버그용
    [ContextMenu("Reset Game Start Date")]
    public void ResetGameStartDate()
    {
        gameStartDate = DateTime.Now;
        PlayerPrefs.SetString(GAME_START_DATE_KEY, gameStartDate.ToBinary().ToString());
        PlayerPrefs.Save();
    }
}