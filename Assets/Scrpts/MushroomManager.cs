using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// 버섯 성장 및 습도 관리 시스템
/// </summary>
public class MushroomManager : MonoBehaviour
{
    [Header("Mushroom Settings")]
    public GameObject mushroomPrefab;           // 버섯 프리팹
    public Transform spawnCenter;               // 스폰 중심점 (보통 종유석 아래)
    public float spawnRadius = 3f;              // 스폰 반경
    public int maxMushrooms = 10;               // 최대 버섯 개수

    [Header("Growth Settings")]
    public float baseGrowthTime = 300f;         // 기본 성장 시간 (5분)
    public float growthStageTime = 100f;        // 각 단계별 성장 시간 (1분 40초)

    [Header("Spawn Settings")]
    public float baseSpawnInterval = 120f;      // 기본 스폰 간격 (2분)
    public float optimalHumidityMin = 80f;      // 최적 습도 최소값
    public float optimalHumidityMax = 90f;      // 최적 습도 최대값

    [Header("Humidity System")]
    [Range(0f, 100f)]
    public float currentHumidity = 50f;         // 현재 습도
    public float humidityDecayRate = 2f;        // 습도 감소율 (시간당)
    public float humidityPerDrop = 5f;          // 물방울 당 습도 증가량
    public float maxHumidity = 100f;            // 최대 습도

    private List<Mushroom> activeMushrooms = new List<Mushroom>();
    private float nextSpawnTime;
    private const string HUMIDITY_KEY = "cave_humidity";
    private const string LAST_UPDATE_TIME_KEY = "humidity_last_update";

    void Start()
    {
        LoadHumidityData();
        CalculateNextSpawnTime();
    }

    void Update()
    {
        UpdateHumidity();
        CheckMushroom생성();
        CleanupDestroyedMushrooms();
    }

    /// <summary>
    /// 습도 업데이트 (시간에 따른 감소)
    /// </summary>
    void UpdateHumidity()
    {
        // 시간당 습도 감소
        float humidityDecrease = humidityDecayRate * Time.deltaTime / 3600f; // 초당 변환
        currentHumidity = Mathf.Max(0f, currentHumidity - humidityDecrease);

        // 주기적으로 저장 (1초마다)
        if (Time.time % 1f < Time.deltaTime)
        {
            SaveHumidityData();
        }
    }

    /// <summary>
    /// 버섯 생성 체크
    /// </summary>
    void CheckMushroom생성()
    {
        if (Time.time >= nextSpawnTime && activeMushrooms.Count < maxMushrooms)
        {
            TrySpawnMushroom();
            CalculateNextSpawnTime();
        }
    }

    /// <summary>
    /// 버섯 생성 시도
    /// </summary>
    void TrySpawnMushroom()
    {
        Vector3 spawnPosition = GetValidSpawnPosition();
        if (spawnPosition != Vector3.zero)
        {
            GameObject mushroomObj = Instantiate(mushroomPrefab, spawnPosition, Quaternion.identity, transform);
            Mushroom mushroom = mushroomObj.GetComponent<Mushroom>();

            if (mushroom == null)
            {
                mushroom = mushroomObj.AddComponent<Mushroom>();
            }

            mushroom.Initialize(this, growthStageTime);
            activeMushrooms.Add(mushroom);

            Debug.Log($"버섯 생성: 위치({spawnPosition}), 총 개수: {activeMushrooms.Count}");
        }
    }

    /// <summary>
    /// 유효한 스폰 위치 찾기
    /// </summary>
    Vector3 GetValidSpawnPosition()
    {
        if (spawnCenter == null) return Vector3.zero;

        int attempts = 0;
        while (attempts < 10) // 최대 10번 시도
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 candidatePos = spawnCenter.position + new Vector3(randomCircle.x, randomCircle.y*.7f,0);

            // 다른 버섯과 너무 가까운지 체크
            bool tooClose = false;
            foreach (Mushroom existingMushroom in activeMushrooms)
            {
                if (existingMushroom != null && Vector3.Distance(candidatePos, existingMushroom.transform.position) < 0.5f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                return candidatePos;
            }

            attempts++;
        }

        return Vector3.zero; // 적절한 위치를 찾지 못함
    }

    /// <summary>
    /// 다음 스폰 시간 계산 (습도에 따라 조정)
    /// </summary>
    void CalculateNextSpawnTime()
    {
        float humidityMultiplier = GetHumiditySpawnMultiplier();
        float adjustedInterval = baseSpawnInterval / humidityMultiplier;

        nextSpawnTime = Time.time + adjustedInterval;

        Debug.Log($"다음 버섯 스폰: {adjustedInterval:F1}초 후 (습도: {currentHumidity:F1}%, 배율: {humidityMultiplier:F2}x)");
    }

    /// <summary>
    /// 습도에 따른 스폰 빈도 배율 계산
    /// </summary>
    float GetHumiditySpawnMultiplier()
    {
        if (currentHumidity >= optimalHumidityMin && currentHumidity <= optimalHumidityMax)
        {
            // 최적 습도 범위: 3배 빠른 스폰
            return 3f;
        }
        else if (currentHumidity >= 60f && currentHumidity < optimalHumidityMin)
        {
            // 적당한 습도: 1.5배 빠른 스폰
            return 1.5f;
        }
        else if (currentHumidity > optimalHumidityMax && currentHumidity <= 95f)
        {
            // 습도 과다: 2배 빠른 스폰 (여전히 좋음)
            return 2f;
        }
        else
        {
            // 건조하거나 너무 습함: 기본 속도
            return 1f;
        }
    }

    /// <summary>
    /// 물방울로 인한 습도 증가 (SimpleWaterDrop에서 호출)
    /// </summary>
    public void AddHumidityFromWaterDrop()
    {
        currentHumidity = Mathf.Min(maxHumidity, currentHumidity + humidityPerDrop);
        Debug.Log($"물방울로 습도 증가: +{humidityPerDrop} → {currentHumidity:F1}%");
    }

    /// <summary>
    /// 버섯 수확 (버섯에서 호출)
    /// </summary>
    public void HarvestMushroom(Mushroom mushroom)
    {
        if (activeMushrooms.Contains(mushroom))
        {
            activeMushrooms.Remove(mushroom);
            Debug.Log($"버섯 수확! 남은 개수: {activeMushrooms.Count}");

            // TODO: 여기에 버섯 수집 카운터 증가 로직 추가
            // InventoryManager.AddMushroom(1);
        }
    }

    /// <summary>
    /// 파괴된 버섯들 정리
    /// </summary>
    void CleanupDestroyedMushrooms()
    {
        activeMushrooms.RemoveAll(mushroom => mushroom == null);
    }

    /// <summary>
    /// 습도 데이터 저장
    /// </summary>
    void SaveHumidityData()
    {
        PlayerPrefs.SetFloat(HUMIDITY_KEY, currentHumidity);
        PlayerPrefs.SetString(LAST_UPDATE_TIME_KEY, DateTime.Now.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 습도 데이터 로드
    /// </summary>
    void LoadHumidityData()
    {
        currentHumidity = PlayerPrefs.GetFloat(HUMIDITY_KEY, 50f); // 기본값 50%

        if (PlayerPrefs.HasKey(LAST_UPDATE_TIME_KEY))
        {
            try
            {
                long lastUpdateBinary = Convert.ToInt64(PlayerPrefs.GetString(LAST_UPDATE_TIME_KEY));
                DateTime lastUpdateTime = DateTime.FromBinary(lastUpdateBinary);
                DateTime now = DateTime.Now;

                // 오프라인 시간 동안의 습도 감소 계산
                TimeSpan offlineTime = now - lastUpdateTime;
                if (offlineTime.TotalMinutes > 1) // 1분 이상 오프라인이었을 때만
                {
                    float offlineHours = (float)offlineTime.TotalHours;
                    float humidityLoss = humidityDecayRate * offlineHours;
                    currentHumidity = Mathf.Max(0f, currentHumidity - humidityLoss);

                    Debug.Log($"오프라인 습도 감소: {offlineHours:F1}시간 → -{humidityLoss:F1}% → 현재 {currentHumidity:F1}%");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"습도 데이터 로드 실패: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 현재 습도 정보 반환
    /// </summary>
    public HumidityInfo GetHumidityInfo()
    {
        return new HumidityInfo
        {
            currentHumidity = currentHumidity,
            spawnMultiplier = GetHumiditySpawnMultiplier(),
            mushroomCount = activeMushrooms.Count,
            nextSpawnIn = Mathf.Max(0f, nextSpawnTime - Time.time)
        };
    }

    // 기즈모로 스폰 범위 표시
    void OnDrawGizmos()
    {
        if (spawnCenter != null)
        {
            // 스폰 범위 표시
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnCenter.position, spawnRadius);

            // 중심점 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnCenter.position, 0.1f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (spawnCenter != null)
        {
            // 선택 시 더 자세한 정보 표시
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnCenter.position, spawnRadius * 0.5f);

            // 기존 버섯들 위치 표시
            Gizmos.color = Color.red;
            foreach (Mushroom mushroom in activeMushrooms)
            {
                if (mushroom != null)
                {
                    Gizmos.DrawWireCube(mushroom.transform.position, Vector3.one * 0.3f);
                }
            }
        }
    }

    // 디버그용
    [ContextMenu("Spawn Mushroom Now")]
    public void SpawnMushroomNow()
    {
        TrySpawnMushroom();
    }

    [ContextMenu("Add Humidity")]
    public void AddHumidity()
    {
        AddHumidityFromWaterDrop();
    }

    [ContextMenu("Reset Humidity")]
    public void ResetHumidity()
    {
        currentHumidity = 50f;
        SaveHumidityData();
    }
}

/// <summary>
/// 습도 정보 구조체
/// </summary>
[System.Serializable]
public struct HumidityInfo
{
    public float currentHumidity;    // 현재 습도
    public float spawnMultiplier;    // 스폰 배율
    public int mushroomCount;        // 현재 버섯 개수
    public float nextSpawnIn;        // 다음 스폰까지 시간
}