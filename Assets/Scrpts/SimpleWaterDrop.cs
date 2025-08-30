using UnityEngine;
using System.Collections;

/// <summary>
/// 간단한 물방울 떨어뜨리기 시스템
/// </summary>
public class SimpleWaterDrop : MonoBehaviour
{
    [Header("References")]
    public Transform startPoint;        // 종유석 끝 빈 오브젝트
    public Transform endPoint;          // 바닥 포지션
    public GameObject waterDropPrefab;  // 물방울 프리팹
    public Transform stalactite;        // 종유석 (터치 감지용)

    public ParticleSystem DropParticle;//  물 드랍 파티클
    
    [Header("Settings")]
    public float dropDuration = 1f;     // 물방울이 떨어지는 시간
    public float autoDropInterval = 3f; // 자동 떨어지는 간격 (3초)
    
    void Start()
    {
        // 3초마다 자동으로 물방울 떨어뜨리기
        StartCoroutine(AutoDropRoutine());
    }
    
    void Update()
    {
        // 터치/클릭 감지
        HandleInput();
    }
    
    /// <summary>
    /// 3초마다 자동으로 물방울 떨어뜨리기
    /// </summary>
    IEnumerator AutoDropRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoDropInterval);
            DropWater();
        }
    }
    
    /// <summary>
    /// 터치/클릭 입력 처리
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
            // 종유석 터치 확인
            if (IsTouchingStalactite(inputPosition))
            {
                TouchFeedback(); // 터치 피드백 효과
                BoostStalactiteGrowth(); // 성장 촉진 + 균열 증가 
                DropWater(); // 추가 물방울 떨어뜨리기
            }
        }
    }
    
    /// <summary>
    /// 종유석을 터치했는지 확인
    /// </summary>
    bool IsTouchingStalactite(Vector3 worldPos)
    {
        if (stalactite == null) return false;
        
        float distance = Vector2.Distance(worldPos, stalactite.position);
        return distance <= 1f; // 1 유닛 이내면 터치로 인정
    }
    
    /// <summary>
    /// 물방울 떨어뜨리기
    /// </summary>
    public void DropWater()
    {
        if (startPoint == null || endPoint == null || waterDropPrefab == null) return;
        
        // 물방울 생성
        GameObject drop = Instantiate(waterDropPrefab, startPoint.position, Quaternion.identity);
        
        // 애니메이션 시작
        StartCoroutine(DropAnimation(drop));
    }
    
    /// <summary>
    /// 터치 시 종유석 피드백 (떨림 + 길이 증가)
    /// </summary>
    public void TouchFeedback()
    {
        if (stalactite != null)
        {
            StartCoroutine(StalactiteShakeAndGrow());
            BoostStalactiteGrowth(); // 성장 촉진 + 균열 증가
        }
    }
    
    /// <summary>
    /// 물방울 떨어지는 애니메이션
    /// </summary>
    IEnumerator DropAnimation(GameObject drop)
    {
        float elapsedTime = 0f;
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;
        
        while (elapsedTime < dropDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dropDuration;
            
            // 중력 느낌의 가속 (Ease In)
            float easedT = t * t;
            
            drop.transform.position = Vector3.Lerp(startPos, endPos, easedT);
            yield return null;
        }
        
        // 바닥 도달 - 물방울 제거
        Destroy(drop);
        DropParticle.transform.position = drop.transform.position;
        
        // 스플래시 효과 (선택사항)
        OnWaterHitGround(endPos);
    }
    
    /// <summary>
    /// 종유석 떨림 애니메이션
    /// </summary>
    IEnumerator StalactiteShakeAndGrow()
    {
        Vector3 originalPosition = stalactite.position;
        
        // 떨림 설정
        float shakeDuration = 0.3f;
        float shakeIntensity = 0.05f;
        
        float elapsedTime = 0f;
        
        // 떨림 효과만
        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / shakeDuration;
            
            // 떨림 효과 (랜덤 진동)
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shakeIntensity, shakeIntensity),
                Random.Range(-shakeIntensity * 0.3f, shakeIntensity * 0.3f), // Y축은 약간만
                0f
            );
            
            // 점진적으로 떨림 약화
            shakeOffset *= (1f - progress);
            stalactite.position = originalPosition + shakeOffset;
            
            yield return null;
        }
        
        // 원래 위치로 복귀
        stalactite.position = originalPosition;
        
        // 실제 종유석 성장 촉진
        BoostStalactiteGrowth();
    }
    
    /// <summary>
    /// 터치할 때마다 실제 종유석 성장 촉진 (시간을 앞당김)
    /// </summary>
    void BoostStalactiteGrowth()
    {
        // 종유석 컴포넌트 찾기
        StalactiteGrowth stalactiteGrowth = stalactite.GetComponent<StalactiteGrowth>();
        if (stalactiteGrowth != null)
        {
            // 생성 시간을 1시간 앞당기기 (실제 성장 촉진)
            stalactiteGrowth.creationTime = stalactiteGrowth.creationTime.AddHours(-1);
            stalactiteGrowth.AddCrackFromTouch();
            Debug.Log("종유석 성장이 1시간 촉진되었습니다!");
        }
    }

    /// <summary>
    /// 물방울이 바닥에 닿았을 때
    /// </summary>
    void OnWaterHitGround(Vector3 position)
    {
        // 간단한 피드백 (나중에 파티클이나 사운드 추가 가능)
        //Debug.Log("물방울이 바닥에 떨어졌습니다!");

        DropParticle.Play();
        // TODO: 여기에 스플래시 파티클 추가
        // TODO: 여기에 사운드 효과 추가
    }
    
    // 디버그용 - Inspector에서 테스트
    [ContextMenu("Drop Water Now")]
    public void DropWaterNow()
    {
        DropWater();
    }
    
    [ContextMenu("Test Touch Feedback")]
    public void TestTouchFeedback()
    {
        TouchFeedback();
    }
}