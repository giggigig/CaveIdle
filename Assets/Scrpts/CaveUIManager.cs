using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CaveUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Canvas mainCanvas;
    public TextMeshProUGUI stalactiteCountText;
    public TextMeshProUGUI selectedStalactiteInfoText;
    public GameObject stalactiteInfoPanel; // 선택된 종유석 정보 패널

    [Header("Info Panel Settings")]
    public float infoPanelDisplayTime = 3f; // 정보 패널 표시 시간

    private List<StalactiteGrowthSystem> allStalactites = new List<StalactiteGrowthSystem>();
    private StalactiteGrowthSystem selectedStalactite = null;
    private float infoPanelTimer = 0f;

    void Start()
    {
        // 초기화
        if (stalactiteInfoPanel != null)
            stalactiteInfoPanel.SetActive(false);

        // 모든 종유석 찾기
        RefreshStalactiteList();

        // UI 업데이트
        UpdateUI();
    }

    void Update()
    {
        // 정보 패널 타이머 처리
        if (stalactiteInfoPanel != null && stalactiteInfoPanel.activeSelf)
        {
            infoPanelTimer -= Time.deltaTime;
            if (infoPanelTimer <= 0f)
            {
                HideInfoPanel();
            }
        }

        // 터치/클릭 처리
        HandleInput();

        // UI 업데이트 (1초마다)
        if (Time.time % 1f < Time.deltaTime)
        {
            RefreshStalactiteList();
            UpdateUI();
        }
    }

    void HandleInput()
    {
        // 모바일 터치 또는 PC 마우스 클릭 처리
        bool inputDetected = false;
        Vector3 inputPosition = Vector3.zero;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                inputDetected = true;
                inputPosition = Camera.main.ScreenToWorldPoint(touch.position);
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
            inputPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        if (inputDetected)
        {
            // 터치/클릭 위치에서 가장 가까운 종유석 찾기
            StalactiteGrowthSystem closestStalactite = FindClosestStalactite(inputPosition);

            if (closestStalactite != null)
            {
                float distance = Vector2.Distance(inputPosition, closestStalactite.transform.position);
                if (distance <= 1f) // 1 유닛 이내
                {
                    SelectStalactite(closestStalactite);
                }
            }
        }
    }

    StalactiteGrowthSystem FindClosestStalactite(Vector3 position)
    {
        StalactiteGrowthSystem closest = null;
        float closestDistance = float.MaxValue;

        foreach (var stalactite in allStalactites)
        {
            if (stalactite == null) continue;

            float distance = Vector2.Distance(position, stalactite.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = stalactite;
            }
        }

        return closest;
    }

    void SelectStalactite(StalactiteGrowthSystem stalactite)
    {
        selectedStalactite = stalactite;
        ShowInfoPanel();

        // 간단한 피드백 효과 (옵션)
        StartCoroutine(StalactiteFeedback(stalactite));
    }

    System.Collections.IEnumerator StalactiteFeedback(StalactiteGrowthSystem stalactite)
    {
        // 잠깐 크기 키우기
        Vector3 originalScale = stalactite.transform.localScale;
        stalactite.transform.localScale = originalScale * 1.1f;

        yield return new WaitForSeconds(0.1f);

        stalactite.transform.localScale = originalScale;
    }

    void ShowInfoPanel()
    {
        if (stalactiteInfoPanel != null && selectedStalactite != null)
        {
            stalactiteInfoPanel.SetActive(true);
            infoPanelTimer = infoPanelDisplayTime;
            UpdateSelectedStalactiteInfo();
        }
    }

    void HideInfoPanel()
    {
        if (stalactiteInfoPanel != null)
        {
            stalactiteInfoPanel.SetActive(false);
        }
        selectedStalactite = null;
    }

    void RefreshStalactiteList()
    {
        // 현재 씬의 모든 종유석 찾기
        StalactiteGrowthSystem[] stalactites = FindObjectsOfType<StalactiteGrowthSystem>();

        // 리스트 업데이트 (null 제거)
        allStalactites = stalactites.Where(s => s != null).ToList();
    }

    void UpdateUI()
    {
        UpdateStalactiteCount();

        if (selectedStalactite != null && stalactiteInfoPanel != null && stalactiteInfoPanel.activeSelf)
        {
            UpdateSelectedStalactiteInfo();
        }
    }

    void UpdateStalactiteCount()
    {
        if (stalactiteCountText != null)
        {
            int totalCount = allStalactites.Count;
            int matureCount = allStalactites.Count(s => s.currentStage == StalactiteGrowthSystem.GrowthStage.Mature);

            stalactiteCountText.text = $"종유석: {totalCount}개 (완성: {matureCount}개)";
        }
    }

    void UpdateSelectedStalactiteInfo()
    {
        if (selectedStalactiteInfoText != null && selectedStalactite != null)
        {
            string info = selectedStalactite.GetGrowthInfo();
            selectedStalactiteInfoText.text = info;
        }
    }

    // 공개 메서드들 (다른 스크립트에서 호출 가능)
    public void ForceUpdateUI()
    {
        RefreshStalactiteList();
        UpdateUI();
    }

    public int GetTotalStalactiteCount()
    {
        return allStalactites.Count;
    }

    public int GetMatureStalactiteCount()
    {
        return allStalactites.Count(s => s.currentStage == StalactiteGrowthSystem.GrowthStage.Mature);
    }

    public List<StalactiteGrowthSystem> GetAllStalactites()
    {
        return new List<StalactiteGrowthSystem>(allStalactites);
    }

    // 디버그용
    [ContextMenu("Force All Mature")]
    public void ForceAllMature()
    {
        foreach (var stalactite in allStalactites)
        {
            if (stalactite != null)
            {
                stalactite.ForceMature();
            }
        }
    }

    [ContextMenu("Reset All Growth")]
    public void ResetAllGrowth()
    {
        foreach (var stalactite in allStalactites)
        {
            if (stalactite != null)
            {
                stalactite.ResetGrowth();
            }
        }
    }
}