using UnityEngine;
using System;

public class StalactiteGrowthSystem : MonoBehaviour
{
    [Header("Growth Settings")]
    public float growthTimeInDays = 30f; // 1개월 = 30일

    [Header("Sprite References")]
    public Sprite stalactiteSprite; // 종유석 스프라이트 1개

    [Header("Spawn Settings")]
    public GameObject stalactitePrefab; // 새로 생성할 종유석 프리팹
    public Transform spawnParent; // 생성 위치 부모 오브젝트
    public float spawnRadius = 3f; // 생성 반경

    // 성장 단계 (단순화)
    public enum GrowthStage
    {
        Growing = 0,    // 성장 중
        Mature = 1      // 완전 성장 (새 종유석 생성 가능)
    }

    [Header("Current State")]
    public GrowthStage currentStage = GrowthStage.Growing;
    public float currentGrowthProgress = 0f; // 0~1 사이 값
    public float currentScale = 0.1f; // 현재 크기 (0.1~2.0)

    private SpriteRenderer spriteRenderer;
    private DateTime creationTime;
    private DateTime lastUpdateTime;
    private bool hasSpawnedChild = false;

    // PlayerPrefs 키들
    private string creationTimeKey;
    private string progressKey;
    private string stageKey;
    private string scaleKey;
    private string hasSpawnedKey;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 고유 키 생성 (오브젝트 인스턴스 ID 기반)
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
        // 실시간으로 성장 업데이트 (최적화를 위해 1초마다만)
        if (Time.time % 1f < Time.deltaTime)
        {
            UpdateGrowth();
        }
    }

    void UpdateGrowth()
    {
        DateTime currentTime = DateTime.Now;

        // 생성 시간부터 현재까지의 경과 시간 계산
        TimeSpan totalElapsed = currentTime - creationTime;
        double totalDays = totalElapsed.TotalDays;

        // 전체 진행률 계산 (0~1)
        currentGrowthProgress = (float)(totalDays / growthTimeInDays);
        currentGrowthProgress = Mathf.Clamp01(currentGrowthProgress);

        // 단계 업데이트
        if (currentGrowthProgress >= 1f)
        {
            currentStage = GrowthStage.Mature;

            // 완전 성장 시 새로운 종유석 생성
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

        // 크기 계산 (0.1 -> 2.0으로 20배 성장)
        currentScale = Mathf.Lerp(0.1f, 2.0f, currentGrowthProgress);

        UpdateSprite();
        SaveGrowthData();
    }

    void UpdateSprite()
    {
        if (stalactiteSprite != null)
        {
            spriteRenderer.sprite = stalactiteSprite;
            // 스케일로 크기 조절
            transform.localScale = Vector3.one * currentScale;
        }
    }

    void SpawnNewStalactite()
    {
        if (stalactitePrefab == null || spawnParent == null) return;

        // 랜덤 위치 생성 (현재 위치 기준 반경 내)
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, 0, randomOffset.y);

        // 새로운 종유석 생성
        GameObject newStalactite = Instantiate(stalactitePrefab, spawnPosition, Quaternion.identity, spawnParent);

        Debug.Log($"새로운 종유석이 생성되었습니다! 위치: {spawnPosition}");
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
        // 생성 시간 로드 (없으면 현재 시간으로 설정)
        if (PlayerPrefs.HasKey(creationTimeKey))
        {
            long creationTimeBinary = Convert.ToInt64(PlayerPrefs.GetString(creationTimeKey));
            creationTime = DateTime.FromBinary(creationTimeBinary);
        }
        else
        {
            creationTime = DateTime.Now;
        }

        // 기타 데이터 로드
        currentGrowthProgress = PlayerPrefs.GetFloat(progressKey, 0f);
        currentStage = (GrowthStage)PlayerPrefs.GetInt(stageKey, 0);
        currentScale = PlayerPrefs.GetFloat(scaleKey, 0.1f);
        hasSpawnedChild = PlayerPrefs.GetInt(hasSpawnedKey, 0) == 1;
    }

    // 디버그용 메서드들
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

    // 현재 상태 정보 출력
    public string GetGrowthInfo()
    {
        TimeSpan elapsed = DateTime.Now - creationTime;
        return $"단계: {currentStage}\n" +
               $"크기: {currentScale:F2}\n" +
               $"진행률: {(currentGrowthProgress * 100):F1}%\n" +
               $"경과 시간: {elapsed.Days}일 {elapsed.Hours}시간";
    }

    void OnDrawGizmosSelected()
    {
        // 새 종유석 생성 반경 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}