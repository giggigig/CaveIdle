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
    public GameObject stalactiteInfoPanel; // ���õ� ������ ���� �г�

    [Header("Info Panel Settings")]
    public float infoPanelDisplayTime = 3f; // ���� �г� ǥ�� �ð�

    private List<StalactiteGrowthSystem> allStalactites = new List<StalactiteGrowthSystem>();
    private StalactiteGrowthSystem selectedStalactite = null;
    private float infoPanelTimer = 0f;

    void Start()
    {
        // �ʱ�ȭ
        if (stalactiteInfoPanel != null)
            stalactiteInfoPanel.SetActive(false);

        // ��� ������ ã��
        RefreshStalactiteList();

        // UI ������Ʈ
        UpdateUI();
    }

    void Update()
    {
        // ���� �г� Ÿ�̸� ó��
        if (stalactiteInfoPanel != null && stalactiteInfoPanel.activeSelf)
        {
            infoPanelTimer -= Time.deltaTime;
            if (infoPanelTimer <= 0f)
            {
                HideInfoPanel();
            }
        }

        // ��ġ/Ŭ�� ó��
        HandleInput();

        // UI ������Ʈ (1�ʸ���)
        if (Time.time % 1f < Time.deltaTime)
        {
            RefreshStalactiteList();
            UpdateUI();
        }
    }

    void HandleInput()
    {
        // ����� ��ġ �Ǵ� PC ���콺 Ŭ�� ó��
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
            // ��ġ/Ŭ�� ��ġ���� ���� ����� ������ ã��
            StalactiteGrowthSystem closestStalactite = FindClosestStalactite(inputPosition);

            if (closestStalactite != null)
            {
                float distance = Vector2.Distance(inputPosition, closestStalactite.transform.position);
                if (distance <= 1f) // 1 ���� �̳�
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

        // ������ �ǵ�� ȿ�� (�ɼ�)
        StartCoroutine(StalactiteFeedback(stalactite));
    }

    System.Collections.IEnumerator StalactiteFeedback(StalactiteGrowthSystem stalactite)
    {
        // ��� ũ�� Ű���
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
        // ���� ���� ��� ������ ã��
        StalactiteGrowthSystem[] stalactites = FindObjectsOfType<StalactiteGrowthSystem>();

        // ����Ʈ ������Ʈ (null ����)
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

            stalactiteCountText.text = $"������: {totalCount}�� (�ϼ�: {matureCount}��)";
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

    // ���� �޼���� (�ٸ� ��ũ��Ʈ���� ȣ�� ����)
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

    // ����׿�
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