using UnityEngine;
using System;

public class StalactiteGrowthSystem : MonoBehaviour
{
    [Header("Growth Settings")]
    public float growthTimeInDays = 30f; // 1���� = 30��

    [Header("Sprite References")]
    public Sprite stalactiteSprite; // ������ ��������Ʈ 1��

    [Header("Spawn Settings")]
    public GameObject stalactitePrefab; // ���� ������ ������ ������
    public Transform spawnParent; // ���� ��ġ �θ� ������Ʈ
    public float spawnRadius = 3f; // ���� �ݰ�

    // ���� �ܰ� (�ܼ�ȭ)
    public enum GrowthStage
    {
        Growing = 0,    // ���� ��
        Mature = 1      // ���� ���� (�� ������ ���� ����)
    }

    [Header("Current State")]
    public GrowthStage currentStage = GrowthStage.Growing;
    public float currentGrowthProgress = 0f; // 0~1 ���� ��
    public float currentScale = 0.1f; // ���� ũ�� (0.1~2.0)

    private SpriteRenderer spriteRenderer;
    private DateTime creationTime;
    private DateTime lastUpdateTime;
    private bool hasSpawnedChild = false;

    // PlayerPrefs Ű��
    private string creationTimeKey;
    private string progressKey;
    private string stageKey;
    private string scaleKey;
    private string hasSpawnedKey;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // ���� Ű ���� (������Ʈ �ν��Ͻ� ID ���)
        string uniqueId = gameObject.GetInstanceID().ToString();
        creationTimeKey = "stalactite_creation_" + uniqueId;
        progressKey = "stalactite_progress_" + uniqueId;
        stageKey = "stalactite_stage_" + uniqueId;
        scaleKey = "stalactite_scale_" + uniqueId;
        hasSpawnedKey = "stalactite_spawned_" + uniqueId;

        LoadGrowthData();
        UpdateGrowth();
        UpdateSprite();
    }

    void Update()
    {
        // �ǽð����� ���� ������Ʈ (����ȭ�� ���� 1�ʸ��ٸ�)
        if (Time.time % 1f < Time.deltaTime)
        {
            UpdateGrowth();
        }
    }

    void UpdateGrowth()
    {
        DateTime currentTime = DateTime.Now;

        // ���� �ð����� ��������� ��� �ð� ���
        TimeSpan totalElapsed = currentTime - creationTime;
        double totalDays = totalElapsed.TotalDays;

        // ��ü ����� ��� (0~1)
        currentGrowthProgress = (float)(totalDays / growthTimeInDays);
        currentGrowthProgress = Mathf.Clamp01(currentGrowthProgress);

        // �ܰ� ������Ʈ
        if (currentGrowthProgress >= 1f)
        {
            currentStage = GrowthStage.Mature;

            // ���� ���� �� ���ο� ������ ����
            if (!hasSpawnedChild)
            {
                SpawnNewStalactite();
                hasSpawnedChild = true;
            }
        }
        else
        {
            currentStage = GrowthStage.Growing;
        }

        // ũ�� ��� (0.1 -> 2.0���� 20�� ����)
        currentScale = Mathf.Lerp(0.1f, 2.0f, currentGrowthProgress);

        UpdateSprite();
        SaveGrowthData();
    }

    void UpdateSprite()
    {
        if (stalactiteSprite != null)
        {
            spriteRenderer.sprite = stalactiteSprite;
            // �����Ϸ� ũ�� ����
            transform.localScale = Vector3.one * currentScale;
        }
    }

    void SpawnNewStalactite()
    {
        if (stalactitePrefab == null || spawnParent == null) return;

        // ���� ��ġ ���� (���� ��ġ ���� �ݰ� ��)
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, 0, randomOffset.y);

        // ���ο� ������ ����
        GameObject newStalactite = Instantiate(stalactitePrefab, spawnPosition, Quaternion.identity, spawnParent);

        Debug.Log($"���ο� �������� �����Ǿ����ϴ�! ��ġ: {spawnPosition}");
    }

    void SaveGrowthData()
    {
        PlayerPrefs.SetString(creationTimeKey, creationTime.ToBinary().ToString());
        PlayerPrefs.SetFloat(progressKey, currentGrowthProgress);
        PlayerPrefs.SetInt(stageKey, (int)currentStage);
        PlayerPrefs.SetFloat(scaleKey, currentScale);
        PlayerPrefs.SetInt(hasSpawnedKey, hasSpawnedChild ? 1 : 0);
        PlayerPrefs.Save();
    }

    void LoadGrowthData()
    {
        // ���� �ð� �ε� (������ ���� �ð����� ����)
        if (PlayerPrefs.HasKey(creationTimeKey))
        {
            long creationTimeBinary = Convert.ToInt64(PlayerPrefs.GetString(creationTimeKey));
            creationTime = DateTime.FromBinary(creationTimeBinary);
        }
        else
        {
            creationTime = DateTime.Now;
        }

        // ��Ÿ ������ �ε�
        currentGrowthProgress = PlayerPrefs.GetFloat(progressKey, 0f);
        currentStage = (GrowthStage)PlayerPrefs.GetInt(stageKey, 0);
        currentScale = PlayerPrefs.GetFloat(scaleKey, 0.1f);
        hasSpawnedChild = PlayerPrefs.GetInt(hasSpawnedKey, 0) == 1;
    }

    // ����׿� �޼����
    [ContextMenu("Force Mature")]
    public void ForceMature()
    {
        creationTime = DateTime.Now.AddDays(-growthTimeInDays);
        UpdateGrowth();
    }

    [ContextMenu("Reset Growth")]
    public void ResetGrowth()
    {
        creationTime = DateTime.Now;
        currentGrowthProgress = 0f;
        currentStage = GrowthStage.Growing;
        currentScale = 0.1f;
        hasSpawnedChild = false;
        UpdateGrowth();
        UpdateSprite();
    }

    // ���� ���� ���� ���
    public string GetGrowthInfo()
    {
        TimeSpan elapsed = DateTime.Now - creationTime;
        return $"�ܰ�: {currentStage}\n" +
               $"ũ��: {currentScale:F2}\n" +
               $"�����: {(currentGrowthProgress * 100):F1}%\n" +
               $"��� �ð�: {elapsed.Days}�� {elapsed.Hours}�ð�";
    }

    void OnDrawGizmosSelected()
    {
        // �� ������ ���� �ݰ� ǥ��
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}