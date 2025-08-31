using UnityEngine;
using System.Collections;

/// <summary>
/// 개별 버섯의 성장 및 상호작용을 관리
/// </summary>
public class Mushroom : MonoBehaviour
{
    public enum GrowthStage
    {
        Sprout = 0,    // 새싹 (시작)
        Growing = 1,   // 성장 중 (중간)  
        Mature = 2     // 성숙 (수확 가능)
    }

    [Header("Growth Sprites")]
    public Sprite[] growthSprites = new Sprite[3];  // 3단계 스프라이트 배열

    [Header("Current State")]
    public GrowthStage currentStage = GrowthStage.Sprout;
    public float growthProgress = 0f;               // 0~1 성장 진행률
    public bool isHarvestable = false;              // 수확 가능 여부

    private MushroomManager manager;
    private SpriteRenderer spriteRenderer;
    private float stageTime = 100f;                 // 각 단계별 소요 시간
    private float totalGrowthTime;                  // 전체 성장 시간 (3단계)
    private float startTime;                        // 성장 시작 시간

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Collider2D 추가 (드래그 수확을 위해 필요)
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
            circleCollider.radius = 0.3f; // 버섯 크기에 맞게 조정
        }
    }

    /// <summary>
    /// 버섯 초기화 (MushroomManager에서 호출)
    /// </summary>
    public void Initialize(MushroomManager mushroomManager, float growthStageTime)
    {
        manager = mushroomManager;
        stageTime = growthStageTime;
        totalGrowthTime = stageTime * 3; // 3단계이므로
        startTime = Time.time;

        currentStage = GrowthStage.Sprout;
        growthProgress = 0f;
        isHarvestable = false;

        UpdateVisual();

        Debug.Log($"버섯 초기화: 단계별 {stageTime}초, 총 {totalGrowthTime}초");
    }

    void Update()
    {
        if (!isHarvestable) // 아직 성숙하지 않았으면 계속 성장
        {
            UpdateGrowth();
        }

        // 터치 감지
        HandleInput();
    }

    /// <summary>
    /// 성장 업데이트
    /// </summary>
    void UpdateGrowth()
    {
        float elapsedTime = Time.time - startTime;
        growthProgress = Mathf.Clamp01(elapsedTime / totalGrowthTime);

        // 현재 단계 계산
        GrowthStage newStage;
        if (growthProgress < 0.33f)
        {
            newStage = GrowthStage.Sprout;
        }
        else if (growthProgress < 0.66f)
        {
            newStage = GrowthStage.Growing;
        }
        else
        {
            newStage = GrowthStage.Mature;
        }

        // 단계가 바뀌었으면 비주얼 업데이트
        if (newStage != currentStage)
        {
            currentStage = newStage;
            UpdateVisual();

            // 성숙 단계에 도달하면 수확 가능
            if (currentStage == GrowthStage.Mature)
            {
                isHarvestable = true;
                Debug.Log("버섯이 성숙했습니다! 수확 가능!");
            }
        }
    }

    /// <summary>
    /// 비주얼 업데이트
    /// </summary>
    void UpdateVisual()
    {
        if (spriteRenderer != null && growthSprites.Length >= 3)
        {
            int stageIndex = (int)currentStage;
            if (stageIndex < growthSprites.Length && growthSprites[stageIndex] != null)
            {
                spriteRenderer.sprite = growthSprites[stageIndex];

                // 성장에 따른 크기 조절 (선택사항)
                float scaleMultiplier = 0.5f + (growthProgress * 0.5f); // 0.5 ~ 1.0
                transform.localScale = Vector3.one * scaleMultiplier;
            }
        }
    }

    /// <summary>
    /// 터치 입력 처리
    /// </summary>
    void HandleInput()
    {
        bool inputDetected = false;
        Vector3 inputPosition = Vector3.zero;

        // 모바일 터치
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                inputDetected = true;
                inputPosition = Camera.main.ScreenToWorldPoint(touch.position);
            }
        }
        // PC 마우스
        else if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
            inputPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        if (inputDetected)
        {
            // 터치 위치가 이 버섯 근처인지 확인
            float distance = Vector2.Distance(inputPosition, transform.position);
            if (distance <= 0.5f) // 터치 반경
            {
                OnTouched();
            }
        }
    }

    /// <summary>
    /// 버섯이 터치되었을 때
    /// </summary>
    void OnTouched()
    {
        if (isHarvestable)
        {
            // 수확 가능한 버섯 터치 시 수확
            HarvestMushroom();
        }
        else
        {
            // 아직 성숙하지 않은 버섯 터치 시 성장 촉진
            BoostGrowth();
        }
    }

    /// <summary>
    /// 버섯 수확 (외부에서도 호출 가능하도록 public으로 변경)
    /// </summary>
    public void HarvestMushroom()
    {
        if (!isHarvestable) return; // 수확 불가능하면 무시

        Debug.Log("버섯 수확!");

        // 수확 효과음 재생
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonSFX(); // 임시로 버튼음 사용
        }

        // 수확 효과 (간단한 크기 변화)
        StartCoroutine(HarvestEffect());

        // 매니저에게 수확 알림
        if (manager != null)
        {
            manager.HarvestMushroom(this);
        }

        // 버섯 제거
        Destroy(gameObject, 0.3f); // 0.3초 후 제거 (효과 시간)
    }

    /// <summary>
    /// 성장 촉진 (터치 시)
    /// </summary>
    void BoostGrowth()
    {
        Debug.Log("버섯 성장 촉진!");

        // 30초 성장 촉진
        startTime -= 30f;

        // 즉시 성장 체크
        UpdateGrowth();

        // 터치 피드백 효과
        StartCoroutine(TouchFeedbackEffect());
    }

    /// <summary>
    /// 수확 효과 애니메이션
    /// </summary>
    IEnumerator HarvestEffect()
    {
        Vector3 originalScale = transform.localScale;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // 크기가 커졌다가 사라짐
            float scale = Mathf.Lerp(1f, 1.5f, progress);
            float alpha = Mathf.Lerp(1f, 0f, progress);

            transform.localScale = originalScale * scale;

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }

            yield return null;
        }
    }

    /// <summary>
    /// 터치 피드백 효과
    /// </summary>
    IEnumerator TouchFeedbackEffect()
    {
        Vector3 originalScale = transform.localScale;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // 살짝 커졌다가 원래대로
            float scale = 1f + (Mathf.Sin(progress * Mathf.PI) * 0.1f);
            transform.localScale = originalScale * scale;

            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// 버섯 정보 반환
    /// </summary>
    public MushroomInfo GetInfo()
    {
        return new MushroomInfo
        {
            stage = currentStage,
            growthProgress = growthProgress,
            isHarvestable = isHarvestable,
            timeRemaining = isHarvestable ? 0f : (totalGrowthTime - (Time.time - startTime))
        };
    }

    // 기즈모로 터치 범위 표시
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f); // 터치 반경

        if (isHarvestable)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
        }
    }
}

/// <summary>
/// 버섯 정보 구조체
/// </summary>
[System.Serializable]
public struct MushroomInfo
{
    public Mushroom.GrowthStage stage;      // 현재 성장 단계
    public float growthProgress;            // 성장 진행률 (0~1)
    public bool isHarvestable;              // 수확 가능 여부
    public float timeRemaining;             // 성숙까지 남은 시간 (초)
}