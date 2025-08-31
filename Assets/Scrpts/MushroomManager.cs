using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// ���� ���� �� ���� ���� �ý���
/// </summary>
public class MushroomManager : MonoBehaviour
{
    [Header("Mushroom Settings")]
    public GameObject mushroomPrefab;           // ���� ������
    public Transform spawnCenter;               // ���� �߽��� (���� ������ �Ʒ�)
    public float spawnRadius = 3f;              // ���� �ݰ�
    public int maxMushrooms = 10;               // �ִ� ���� ����

    [Header("Growth Settings")]
    public float baseGrowthTime = 300f;         // �⺻ ���� �ð� (5��)
    public float growthStageTime = 100f;        // �� �ܰ躰 ���� �ð� (1�� 40��)

    [Header("Spawn Settings")]
    public float baseSpawnInterval = 120f;      // �⺻ ���� ���� (2��)
    public float optimalHumidityMin = 80f;      // ���� ���� �ּҰ�
    public float optimalHumidityMax = 90f;      // ���� ���� �ִ밪

    [Header("Humidity System")]
    [Range(0f, 100f)]
    public float currentHumidity = 50f;         // ���� ����
    public float humidityDecayRate = 2f;        // ���� ������ (�ð���)
    public float humidityPerDrop = 5f;          // ����� �� ���� ������
    public float maxHumidity = 100f;            // �ִ� ����

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
        CheckMushroom����();
        CleanupDestroyedMushrooms();
    }

    /// <summary>
    /// ���� ������Ʈ (�ð��� ���� ����)
    /// </summary>
    void UpdateHumidity()
    {
        // �ð��� ���� ����
        float humidityDecrease = humidityDecayRate * Time.deltaTime / 3600f; // �ʴ� ��ȯ
        currentHumidity = Mathf.Max(0f, currentHumidity - humidityDecrease);

        // �ֱ������� ���� (1�ʸ���)
        if (Time.time % 1f < Time.deltaTime)
        {
            SaveHumidityData();
        }
    }

    /// <summary>
    /// ���� ���� üũ
    /// </summary>
    void CheckMushroom����()
    {
        if (Time.time >= nextSpawnTime && activeMushrooms.Count < maxMushrooms)
        {
            TrySpawnMushroom();
            CalculateNextSpawnTime();
        }
    }

    /// <summary>
    /// ���� ���� �õ�
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

            Debug.Log($"���� ����: ��ġ({spawnPosition}), �� ����: {activeMushrooms.Count}");
        }
    }

    /// <summary>
    /// ��ȿ�� ���� ��ġ ã��
    /// </summary>
    Vector3 GetValidSpawnPosition()
    {
        if (spawnCenter == null) return Vector3.zero;

        int attempts = 0;
        while (attempts < 10) // �ִ� 10�� �õ�
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 candidatePos = spawnCenter.position + new Vector3(randomCircle.x, randomCircle.y*.7f,0);

            // �ٸ� ������ �ʹ� ������� üũ
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

        return Vector3.zero; // ������ ��ġ�� ã�� ����
    }

    /// <summary>
    /// ���� ���� �ð� ��� (������ ���� ����)
    /// </summary>
    void CalculateNextSpawnTime()
    {
        float humidityMultiplier = GetHumiditySpawnMultiplier();
        float adjustedInterval = baseSpawnInterval / humidityMultiplier;

        nextSpawnTime = Time.time + adjustedInterval;

        Debug.Log($"���� ���� ����: {adjustedInterval:F1}�� �� (����: {currentHumidity:F1}%, ����: {humidityMultiplier:F2}x)");
    }

    /// <summary>
    /// ������ ���� ���� �� ���� ���
    /// </summary>
    float GetHumiditySpawnMultiplier()
    {
        if (currentHumidity >= optimalHumidityMin && currentHumidity <= optimalHumidityMax)
        {
            // ���� ���� ����: 3�� ���� ����
            return 3f;
        }
        else if (currentHumidity >= 60f && currentHumidity < optimalHumidityMin)
        {
            // ������ ����: 1.5�� ���� ����
            return 1.5f;
        }
        else if (currentHumidity > optimalHumidityMax && currentHumidity <= 95f)
        {
            // ���� ����: 2�� ���� ���� (������ ����)
            return 2f;
        }
        else
        {
            // �����ϰų� �ʹ� ����: �⺻ �ӵ�
            return 1f;
        }
    }

    /// <summary>
    /// ������ ���� ���� ���� (SimpleWaterDrop���� ȣ��)
    /// </summary>
    public void AddHumidityFromWaterDrop()
    {
        currentHumidity = Mathf.Min(maxHumidity, currentHumidity + humidityPerDrop);
        Debug.Log($"������ ���� ����: +{humidityPerDrop} �� {currentHumidity:F1}%");
    }

    /// <summary>
    /// ���� ��Ȯ (�������� ȣ��)
    /// </summary>
    public void HarvestMushroom(Mushroom mushroom)
    {
        if (activeMushrooms.Contains(mushroom))
        {
            activeMushrooms.Remove(mushroom);
            Debug.Log($"���� ��Ȯ! ���� ����: {activeMushrooms.Count}");

            // TODO: ���⿡ ���� ���� ī���� ���� ���� �߰�
            // InventoryManager.AddMushroom(1);
        }
    }

    /// <summary>
    /// �ı��� ������ ����
    /// </summary>
    void CleanupDestroyedMushrooms()
    {
        activeMushrooms.RemoveAll(mushroom => mushroom == null);
    }

    /// <summary>
    /// ���� ������ ����
    /// </summary>
    void SaveHumidityData()
    {
        PlayerPrefs.SetFloat(HUMIDITY_KEY, currentHumidity);
        PlayerPrefs.SetString(LAST_UPDATE_TIME_KEY, DateTime.Now.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ���� ������ �ε�
    /// </summary>
    void LoadHumidityData()
    {
        currentHumidity = PlayerPrefs.GetFloat(HUMIDITY_KEY, 50f); // �⺻�� 50%

        if (PlayerPrefs.HasKey(LAST_UPDATE_TIME_KEY))
        {
            try
            {
                long lastUpdateBinary = Convert.ToInt64(PlayerPrefs.GetString(LAST_UPDATE_TIME_KEY));
                DateTime lastUpdateTime = DateTime.FromBinary(lastUpdateBinary);
                DateTime now = DateTime.Now;

                // �������� �ð� ������ ���� ���� ���
                TimeSpan offlineTime = now - lastUpdateTime;
                if (offlineTime.TotalMinutes > 1) // 1�� �̻� ���������̾��� ����
                {
                    float offlineHours = (float)offlineTime.TotalHours;
                    float humidityLoss = humidityDecayRate * offlineHours;
                    currentHumidity = Mathf.Max(0f, currentHumidity - humidityLoss);

                    Debug.Log($"�������� ���� ����: {offlineHours:F1}�ð� �� -{humidityLoss:F1}% �� ���� {currentHumidity:F1}%");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"���� ������ �ε� ����: {e.Message}");
            }
        }
    }

    /// <summary>
    /// ���� ���� ���� ��ȯ
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

    // ������ ���� ���� ǥ��
    void OnDrawGizmos()
    {
        if (spawnCenter != null)
        {
            // ���� ���� ǥ��
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnCenter.position, spawnRadius);

            // �߽��� ǥ��
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnCenter.position, 0.1f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (spawnCenter != null)
        {
            // ���� �� �� �ڼ��� ���� ǥ��
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnCenter.position, spawnRadius * 0.5f);

            // ���� ������ ��ġ ǥ��
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

    // ����׿�
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
/// ���� ���� ����ü
/// </summary>
[System.Serializable]
public struct HumidityInfo
{
    public float currentHumidity;    // ���� ����
    public float spawnMultiplier;    // ���� ����
    public int mushroomCount;        // ���� ���� ����
    public float nextSpawnIn;        // ���� �������� �ð�
}