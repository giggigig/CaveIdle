using UnityEngine;
using System;

/// <summary>
/// 버섯 종류 및 특성 정의
/// </summary>
[Serializable]
public class MushroomType
{
    public string typeName;             // 버섯 이름
    public string description;          // 설명
    public Sprite icon;                 // 아이콘
    public Rarity rarity;              // 희귀도
    public Color rarityColor;          // 희귀도 색상
    
    [Header("Spawn Settings")]
    public float spawnChance = 50f;     // 스폰 확률 (%)
    public float optimalHumidityMin = 70f;  // 최적 습도 최소
    public float optimalHumidityMax = 90f;  // 최적 습도 최대
    
    [Header("Effects")]
    public MushroomEffect[] effects;    // 버섯 효과들
    
    [Header("Value")]
    public int baseValue = 1;          // 기본 가치 (상점에서의 가격)
    public int sellPrice = 1;          // 판매 가격
}

/// <summary>
/// 버섯 희귀도
/// </summary>
public enum Rarity
{
    Common = 0,     // 일반 (흰색)
    Uncommon = 1,   // 보통 (초록색)
    Rare = 2,       // 희귀 (파란색)
    Epic = 3,       // 에픽 (보라색)
    Legendary = 4   // 전설 (주황색)
}

/// <summary>
/// 버섯 효과 종류
/// </summary>
[Serializable]
public class MushroomEffect
{
    public EffectType effectType;
    public float duration;          // 지속 시간 (초)
    public float value;            // 효과 수치
    public string description;     // 효과 설명
}

/// <summary>
/// 효과 타입
/// </summary>
public enum EffectType
{
    None = 0,
    HumidityBoost = 1,      // 습도 증가
    GrowthAcceleration = 2,  // 성장 가속
    SpawnRateBoost = 3,     // 스폰율 증가
    HarvestMultiplier = 4,  // 수확 배수
    TimeSkip = 5           // 시간 건너뛰기
}