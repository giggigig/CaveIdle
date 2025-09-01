using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 상점 시스템 - 버섯으로 아이템 구매
/// </summary>
public class ShopSystem : MonoBehaviour
{
    [Header("Shop Items")]
    public ShopItem[] availableItems;
    
    [Header("Shop Settings")]
    public bool shopUnlocked = true;
    
    // 구매 이벤트
    public event Action<ShopItem, int> OnItemPurchased;
    
    // Singleton
    public static ShopSystem Instance;
    
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
    
    /// <summary>
    /// 아이템 구매
    /// </summary>
    public bool PurchaseItem(string itemId, int quantity = 1)
    {
        ShopItem item = GetShopItem(itemId);
        if (item == null)
        {
            Debug.LogError($"상점에 {itemId} 아이템이 없습니다!");
            return false;
        }
        
        if (!CanAfford(item, quantity))
        {
            Debug.LogWarning($"{item.itemName} {quantity}개를 구매할 버섯이 부족합니다!");
            return false;
        }
        
        // 비용 지불
        if (PayCost(item, quantity))
        {
            // 아이템 효과 적용
            ApplyItemEffect(item, quantity);
            
            // 이벤트 발생
            OnItemPurchased?.Invoke(item, quantity);
            
            Debug.Log($"{item.itemName} {quantity}개 구매 완료!");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 구매 가능 여부 확인
    /// </summary>
    public bool CanAfford(ShopItem item, int quantity = 1)
    {
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null) return false;
        
        foreach (var cost in item.costs)
        {
            int required = cost.amount * quantity;
            int available = inventory.GetMushroomCount(cost.mushroomType);
            
            if (available < required)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 비용 지불
    /// </summary>
    bool PayCost(ShopItem item, int quantity = 1)
    {
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null) return false;
        
        // 모든 비용 지불
        foreach (var cost in item.costs)
        {
            int required = cost.amount * quantity;
            if (!inventory.RemoveMushroom(cost.mushroomType, required))
            {
                Debug.LogError($"비용 지불 실패: {cost.mushroomType} {required}개");
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 아이템 효과 적용
    /// </summary>
    void ApplyItemEffect(ShopItem item, int quantity = 1)
    {
        switch (item.itemType)
        {
            case ShopItemType.HumidityBooster:
                ApplyHumidityBoost(item.effectValue * quantity);
                break;
                
            case ShopItemType.GrowthAccelerator:
                ApplyGrowthAcceleration(item.effectValue * quantity);
                break;
                
            case ShopItemType.SpawnRateBooster:
                ApplySpawnRateBoost(item.effectValue, item.effectDuration * quantity);
                break;
                
            case ShopItemType.MaxHumidityUpgrade:
                ApplyMaxHumidityUpgrade(item.effectValue * quantity);
                break;
                
            case ShopItemType.InventoryExpansion:
                ApplyInventoryExpansion((int)(item.effectValue * quantity));
                break;
                
            case ShopItemType.AutoHarvester:
                ApplyAutoHarvester(item.effectDuration * quantity);
                break;
        }
    }
    
    void ApplyHumidityBoost(float amount)
    {
        MushroomManager manager = FindObjectOfType<MushroomManager>();
        if (manager != null)
        {
            manager.currentHumidity = Mathf.Min(100f, manager.currentHumidity + amount);
            Debug.Log($"습도 {amount} 증가! 현재: {manager.currentHumidity}%");
        }
    }
    
    void ApplyGrowthAcceleration(float minutes)
    {
        StalactiteGrowth stalactite = FindObjectOfType<StalactiteGrowth>();
        if (stalactite != null)
        {
            stalactite.creationTime = stalactite.creationTime.AddMinutes(-minutes);
            Debug.Log($"종유석 성장 {minutes}분 가속!");
        }
    }
    
    void ApplySpawnRateBoost(float multiplier, float duration)
    {
        // TODO: 스폰 율 부스트 임시 효과 구현
        Debug.Log($"스폰 율 {multiplier}x 부스트 {duration}초간 적용!");
        StartCoroutine(TemporarySpawnRateBoost(multiplier, duration));
    }
    
    void ApplyMaxHumidityUpgrade(float increase)
    {
        // TODO: 최대 습도 업그레이드 구현
        Debug.Log($"최대 습도 {increase} 증가!");
    }
    
    void ApplyInventoryExpansion(int slots)
    {
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory != null)
        {
            inventory.maxInventorySize += slots;
            Debug.Log($"인벤토리 {slots}칸 확장! 현재: {inventory.maxInventorySize}");
        }
    }
    
    void ApplyAutoHarvester(float duration)
    {
        // TODO: 자동 수확기 임시 효과 구현
        Debug.Log($"자동 수확기 {duration}초간 활성화!");
        StartCoroutine(AutoHarvesterEffect(duration));
    }
    
    /// <summary>
    /// 임시 스폰 율 부스트
    /// </summary>
    System.Collections.IEnumerator TemporarySpawnRateBoost(float multiplier, float duration)
    {
        MushroomManager manager = FindObjectOfType<MushroomManager>();
        if (manager == null) yield break;
        
        float originalInterval = manager.baseSpawnInterval;
        manager.baseSpawnInterval /= multiplier;
        
        yield return new WaitForSeconds(duration);
        
        manager.baseSpawnInterval = originalInterval;
        Debug.Log("스폰 율 부스트 효과 종료");
    }
    
    /// <summary>
    /// 자동 수확기 효과
    /// </summary>
    System.Collections.IEnumerator AutoHarvesterEffect(float duration)
    {
        float elapsedTime = 0f;
        float harvestInterval = 2f; // 2초마다 수확
        
        while (elapsedTime < duration)
        {
            // 성숙한 버섯들을 자동 수확
            Mushroom[] mushrooms = FindObjectsOfType<Mushroom>();
            foreach (var mushroom in mushrooms)
            {
                if (mushroom.isHarvestable)
                {
                    mushroom.HarvestMushroom();
                }
            }
            
            yield return new WaitForSeconds(harvestInterval);
            elapsedTime += harvestInterval;
        }
        
        Debug.Log("자동 수확기 효과 종료");
    }
    
    /// <summary>
    /// 상점 아이템 가져오기
    /// </summary>
    public ShopItem GetShopItem(string itemId)
    {
        foreach (var item in availableItems)
        {
            if (item.itemId == itemId)
                return item;
        }
        return null;
    }
    
    /// <summary>
    /// 모든 상점 아이템 목록
    /// </summary>
    public ShopItem[] GetAllItems()
    {
        return availableItems;
    }
    
    // 디버그용
    [ContextMenu("Test Purchase")]
    public void TestPurchase()
    {
        if (availableItems.Length > 0)
        {
            PurchaseItem(availableItems[0].itemId, 1);
        }
    }
}

/// <summary>
/// 상점 아이템 정보
/// </summary>
[Serializable]
public class ShopItem
{
    public string itemId;
    public string itemName;
    public string description;
    public Sprite icon;
    
    [Header("Item Settings")]
    public ShopItemType itemType;
    public float effectValue;           // 효과 수치
    public float effectDuration;        // 지속 시간 (초, 영구 효과면 0)
    public bool isConsumable = true;    // 소모품 여부
    
    [Header("Cost")]
    public ItemCost[] costs;            // 구매 비용
    
    [Header("Unlock Conditions")]
    public bool isUnlocked = true;
    public int requiredStalactiteLength = 0;
    public int requiredTotalHarvest = 0;
}

/// <summary>
/// 아이템 비용 정보
/// </summary>
[Serializable]
public class ItemCost
{
    public string mushroomType;
    public int amount;
}

/// <summary>
/// 상점 아이템 타입
/// </summary>
public enum ShopItemType
{
    HumidityBooster = 0,        // 습도 증가
    GrowthAccelerator = 1,      // 성장 가속
    SpawnRateBooster = 2,       // 스폰 율 증가 (임시)
    MaxHumidityUpgrade = 3,     // 최대 습도 증가 (영구)
    InventoryExpansion = 4,     // 인벤토리 확장 (영구)
    AutoHarvester = 5          // 자동 수확기 (임시)
}