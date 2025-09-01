using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 인벤토리 시스템 - 수집한 버섯들을 저장하고 관리
/// </summary>
public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int maxInventorySize = 100;
    
    [Header("Mushroom Types")]
    public MushroomType[] availableMushroomTypes;
    
    // 인벤토리 데이터
    private Dictionary<string, int> mushroomInventory = new Dictionary<string, int>();
    private int totalHarvestedCount = 0;
    
    // 이벤트
    public event Action<string, int> OnMushroomAdded;
    public event Action<string, int> OnMushroomRemoved;
    public event Action<int> OnTotalHarvestedChanged;
    
    // Singleton
    public static InventorySystem Instance;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        LoadInventory();
    }
    
    /// <summary>
    /// 버섯 추가
    /// </summary>
    public bool AddMushroom(string mushroomTypeName, int count = 1)
    {
        if (GetTotalMushroomCount() + count > maxInventorySize)
        {
            Debug.LogWarning("인벤토리가 가득 참!");
            return false;
        }
        
        if (mushroomInventory.ContainsKey(mushroomTypeName))
        {
            mushroomInventory[mushroomTypeName] += count;
        }
        else
        {
            mushroomInventory[mushroomTypeName] = count;
        }
        
        totalHarvestedCount += count;
        
        // 이벤트 발생
        OnMushroomAdded?.Invoke(mushroomTypeName, count);
        OnTotalHarvestedChanged?.Invoke(totalHarvestedCount);
        
        Debug.Log($"{mushroomTypeName} {count}개 추가됨. 총 보유: {mushroomInventory[mushroomTypeName]}개");
        
        SaveInventory();
        return true;
    }
    
    /// <summary>
    /// 버섯 제거/사용
    /// </summary>
    public bool RemoveMushroom(string mushroomTypeName, int count = 1)
    {
        if (!mushroomInventory.ContainsKey(mushroomTypeName))
        {
            Debug.LogWarning($"{mushroomTypeName} 버섯이 인벤토리에 없음!");
            return false;
        }
        
        if (mushroomInventory[mushroomTypeName] < count)
        {
            Debug.LogWarning($"{mushroomTypeName} 버섯이 부족함! 보유: {mushroomInventory[mushroomTypeName]}, 필요: {count}");
            return false;
        }
        
        mushroomInventory[mushroomTypeName] -= count;
        
        if (mushroomInventory[mushroomTypeName] <= 0)
        {
            mushroomInventory.Remove(mushroomTypeName);
        }
        
        // 이벤트 발생
        OnMushroomRemoved?.Invoke(mushroomTypeName, count);
        
        Debug.Log($"{mushroomTypeName} {count}개 제거됨");
        
        SaveInventory();
        return true;
    }
    
    /// <summary>
    /// 특정 버섯 보유 개수 확인
    /// </summary>
    public int GetMushroomCount(string mushroomTypeName)
    {
        return mushroomInventory.ContainsKey(mushroomTypeName) ? mushroomInventory[mushroomTypeName] : 0;
    }
    
    /// <summary>
    /// 총 버섯 개수
    /// </summary>
    public int GetTotalMushroomCount()
    {
        int total = 0;
        foreach (var kvp in mushroomInventory)
        {
            total += kvp.Value;
        }
        return total;
    }
    
    /// <summary>
    /// 총 수확 개수
    /// </summary>
    public int GetTotalHarvestedCount()
    {
        return totalHarvestedCount;
    }
    
    /// <summary>
    /// 인벤토리 전체 목록
    /// </summary>
    public Dictionary<string, int> GetInventoryData()
    {
        return new Dictionary<string, int>(mushroomInventory);
    }
    
    /// <summary>
    /// 버섯 타입 정보 가져오기
    /// </summary>
    public MushroomType GetMushroomType(string typeName)
    {
        foreach (var type in availableMushroomTypes)
        {
            if (type.typeName == typeName)
                return type;
        }
        return null;
    }
    
    /// <summary>
    /// 버섯 사용 (효과 적용)
    /// </summary>
    public bool UseMushroom(string mushroomTypeName, int count = 1)
    {
        if (!RemoveMushroom(mushroomTypeName, count))
            return false;
            
        MushroomType mushroomType = GetMushroomType(mushroomTypeName);
        if (mushroomType != null && mushroomType.effects != null)
        {
            // 효과 적용
            foreach (var effect in mushroomType.effects)
            {
                ApplyMushroomEffect(effect, count);
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 버섯 효과 적용
    /// </summary>
    void ApplyMushroomEffect(MushroomEffect effect, int count = 1)
    {
        MushroomManager manager = FindObjectOfType<MushroomManager>();
        StalactiteGrowth stalactite = FindObjectOfType<StalactiteGrowth>();
        
        switch (effect.effectType)
        {
            case EffectType.HumidityBoost:
                if (manager != null)
                {
                    manager.currentHumidity = Mathf.Min(100f, manager.currentHumidity + effect.value * count);
                    Debug.Log($"습도 {effect.value * count} 증가!");
                }
                break;
                
            case EffectType.GrowthAcceleration:
                if (stalactite != null)
                {
                    stalactite.creationTime = stalactite.creationTime.AddMinutes(-effect.value * count);
                    Debug.Log($"성장 {effect.value * count}분 가속!");
                }
                break;
                
            case EffectType.TimeSkip:
                // TODO: 시간 건너뛰기 구현
                Debug.Log($"시간 {effect.value * count}분 건너뛰기!");
                break;
        }
    }
    
    /// <summary>
    /// 인벤토리 저장
    /// </summary>
    void SaveInventory()
    {
        // JSON으로 인벤토리 데이터 저장
        string jsonData = JsonUtility.ToJson(new SerializableInventory(mushroomInventory, totalHarvestedCount));
        PlayerPrefs.SetString("InventoryData", jsonData);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 인벤토리 로드
    /// </summary>
    void LoadInventory()
    {
        string jsonData = PlayerPrefs.GetString("InventoryData", "");
        if (!string.IsNullOrEmpty(jsonData))
        {
            try
            {
                SerializableInventory savedData = JsonUtility.FromJson<SerializableInventory>(jsonData);
                mushroomInventory = savedData.GetInventoryDictionary();
                totalHarvestedCount = savedData.totalHarvested;
                
                Debug.Log($"인벤토리 로드 완료. 총 수확: {totalHarvestedCount}, 보유: {GetTotalMushroomCount()}");
            }
            catch (Exception e)
            {
                Debug.LogError($"인벤토리 로드 실패: {e.Message}");
            }
        }
    }
    
    // 디버그용
    [ContextMenu("Add Test Mushrooms")]
    public void AddTestMushrooms()
    {
        AddMushroom("Common", 5);
        AddMushroom("Rare", 2);
        AddMushroom("Epic", 1);
    }
    
    [ContextMenu("Clear Inventory")]
    public void ClearInventory()
    {
        mushroomInventory.Clear();
        totalHarvestedCount = 0;
        SaveInventory();
        Debug.Log("인벤토리 초기화됨");
    }
}

/// <summary>
/// 직렬화 가능한 인벤토리 데이터
/// </summary>
[Serializable]
public class SerializableInventory
{
    public string[] mushroomNames;
    public int[] mushroomCounts;
    public int totalHarvested;
    
    public SerializableInventory(Dictionary<string, int> inventory, int totalHarvested)
    {
        this.totalHarvested = totalHarvested;
        
        mushroomNames = new string[inventory.Count];
        mushroomCounts = new int[inventory.Count];
        
        int index = 0;
        foreach (var kvp in inventory)
        {
            mushroomNames[index] = kvp.Key;
            mushroomCounts[index] = kvp.Value;
            index++;
        }
    }
    
    public Dictionary<string, int> GetInventoryDictionary()
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        
        for (int i = 0; i < mushroomNames.Length; i++)
        {
            result[mushroomNames[i]] = mushroomCounts[i];
        }
        
        return result;
    }
}