using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// 통합 터치 관리 시스템 - 모든 터치 입력을 관리하고 적절한 액션 실행
/// </summary>
public class TouchManager : MonoBehaviour
{
    [Header("Touch Targets")]
    public Transform stalactiteTransform;        // 종유석 Transform
    public float stalactiteTouchRadius = 1f;    // 종유석 터치 반경
    
    [Header("Mushroom Drag Harvest")]
    public float mushroomHarvestRadius = 0.8f;   // 버섯 드래그 수확 반경
    public bool showDragTrail = true;            // 드래그 궤적 표시
    
    [Header("Touch Particles")]
    public ParticleSystem touchParticleSystem;   // 터치 파티클 시스템
    public bool enableTouchParticles = true;     // 터치 파티클 활성화
    
    [Header("Systems")]
    public SimpleWaterDrop waterDropSystem;      // 물방울 시스템
    public StalactiteGrowth stalactiteGrowth;    // 종유석 성장 시스템
    
    [Header("Touch Settings")]
    public bool enableTouch = true;              // 터치 활성화
    public float touchCooldown = 0.05f;          // 터치 쿨타임 (연속 터치 개선)

    [Header("Visual Feedback")]
    public bool enableTouchEffects = true;      // 터치 이펙트 활성화
    public float shakeIntensity = 0.05f;        // 떨림 강도
    public float shakeDuration = 0.3f;          // 떨림 지속 시간
    
    private float lastTouchTime = 0f;           // 마지막 터치 시간
    private Vector3 originalStalactitePosition; // 종유석 원래 위치
    private Camera mainCamera;
    
    // 드래그 수확 관련
    private bool isDragging = false;
    private Vector3 lastDragPosition;
    private List<Mushroom> harvestedMushrooms = new List<Mushroom>();
    private TrailRenderer dragTrail;
    private GameObject trailObject;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // 종유석 원래 위치 저장
        if (stalactiteTransform != null)
        {
            originalStalactitePosition = stalactiteTransform.position;
        }
        
        // 자동으로 컴포넌트 찾기 (할당되지 않은 경우)
        if (waterDropSystem == null)
        {
            waterDropSystem = FindObjectOfType<SimpleWaterDrop>();
        }
        
        if (stalactiteGrowth == null)
        {
            stalactiteGrowth = FindObjectOfType<StalactiteGrowth>();
        }
        
        if (touchParticleSystem == null)
        {
            touchParticleSystem = FindObjectOfType<ParticleSystem>();
        }
        
        // 드래그 궤적용 LineRenderer 생성
        if (showDragTrail)
        {
            CreateDragTrail();
        }
    }
    
    void Update()
    {
        if (!enableTouch) return;
        
        // 터치 쿨타임 체크 (드래그 중에는 쿨타임 무시하여 연속터치 개선)
        if (!isDragging && Time.time - lastTouchTime < touchCooldown) return;
        
        // 입력 처리
        HandleInput();
    }
    
    /// <summary>
    /// 터치/클릭 입력 처리 (개선된 로직)
    /// </summary>
    void HandleInput()
    {
        bool inputActive = false;
        bool inputEnded = false;
        bool inputBegan = false;
        Vector3 inputPosition = Vector3.zero;
        
        // 모바일 터치 우선 처리
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPosition = GetWorldPosition(touch.position);
            
            if (touch.phase == TouchPhase.Began)
            {
                inputBegan = true;
                inputActive = true;
                Debug.Log($"터치 시작: 스크린({touch.position}) -> 월드({inputPosition})");
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                inputActive = true;
                Debug.Log($"터치 이동: 스크린({touch.position}) -> 월드({inputPosition})");
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                inputEnded = true;
                Debug.Log("터치 종료");
            }
        }
        // PC 마우스 (터치가 없을 때만)
        else
        {
            inputPosition = GetWorldPosition(Input.mousePosition);
            
            if (Input.GetMouseButtonDown(0))
            {
                inputBegan = true;
                inputActive = true;
                Debug.Log($"마우스 클릭 시작: 스크린({Input.mousePosition}) -> 월드({inputPosition})");
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                inputActive = true;
                Debug.Log($"마우스 드래그: 스크린({Input.mousePosition}) -> 월드({inputPosition})");
            }
            else if (Input.GetMouseButtonUp(0))
            {
                inputEnded = true;
                Debug.Log("마우스 클릭 종료");
            }
        }
        
        // 입력 처리
        if (inputActive)
        {
            ProcessTouchInput(inputPosition);
            // 터치 쿨타임은 터치 시작 시에만 적용
            if (inputBegan)
            {
                lastTouchTime = Time.time;
            }
        }
        else if (inputEnded)
        {
            EndTouch(inputPosition);
        }
    }
    
    /// <summary>
    /// 스크린 좌표를 월드 좌표로 변환
    /// </summary>
    Vector3 GetWorldPosition(Vector3 screenPosition)
    {
        if (mainCamera != null)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
            worldPos.z = 0f; // 2D 게임이므로 Z는 0
            return worldPos;
        }
        return Vector3.zero;
    }
    
    /// <summary>
    /// 터치 입력 처리 (드래그 지원)
    /// </summary>
    void ProcessTouchInput(Vector3 currentPosition)
    {
        if (!isDragging)
        {
            // 첫 터치 - 드래그 시작 또는 단일 터치
            StartTouch(currentPosition);
        }
        else
        {
            // 드래그 중 - 버섯 수확 체크 및 트레일 업데이트
            float dragDistance = Vector3.Distance(currentPosition, lastDragPosition);
            if (dragDistance >= 0.001f) // 최소 드래그 거리 (더 부드럽게)
            {
                HarvestMushroomsInRadius(currentPosition);
                UpdateDragTrail(currentPosition);
                lastDragPosition = currentPosition;
            }
        }
    }
    
    /// <summary>
    /// 터치 시작 처리
    /// </summary>
    void StartTouch(Vector3 position)
    {
        isDragging = true;
        lastDragPosition = position;
        harvestedMushrooms.Clear();
        
        // 터치 파티클 재생 (터치 시작 시에만)
        PlayTouchParticle(position);
        
        // 드래그 트레일 시작 (모든 터치에서)
        if (showDragTrail)
        {
            StartDragTrail(position);
        }
        
        // 터치 대상 판별
        TouchTarget target = GetTouchTarget(position);
        
        switch (target)
        {
            case TouchTarget.Stalactite:
                OnStalactiteTouch(position);
                break;
                
            case TouchTarget.Mushroom:
                // 버섯 터치는 드래그로만 처리
                HarvestMushroomsInRadius(position);
                break;
                
            case TouchTarget.EmptySpace:
                OnEmptySpaceTouch(position);
                break;
        }
    }
    
    /// <summary>
    /// 터치 종료 처리
    /// </summary>
    void EndTouch(Vector3 position)
    {
        if (isDragging)
        {
            isDragging = false;
            harvestedMushrooms.Clear();
            
            // 터치 종료 시 즉시 트레일 클리어
            if (showDragTrail && trailObject != null && trailObject.activeSelf)
            {
                dragTrail.Clear(); // 즉시 트레일 클리어
                EndDragTrail();
                Debug.Log("터치 종료: 트레일 클리어됨");
            }
            
            Debug.Log("터치/드래그 종료!");
        }
    }
    
    /// <summary>
    /// 터치 대상 판별 (Collider + Raycast 기반 - 더 정확한 감지)
    /// </summary>
    TouchTarget GetTouchTarget(Vector3 worldPosition)
    {
        // Physics2D Raycast를 사용한 종유석 터치 체크
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero, 0f);
        
        if (hit.collider != null)
        {
            // StalactiteGrowth 컴포넌트 확인
            StalactiteGrowth stalactiteComponent = hit.collider.GetComponent<StalactiteGrowth>();
            if (stalactiteComponent != null)
            {
                Debug.Log($"종유석 Raycast 터치 감지! 오브젝트: {hit.collider.name}");
                return TouchTarget.Stalactite;
            }
            
            // 태그로도 확인 (이중 체크)
            if (hit.collider.CompareTag("Stalactite"))
            {
                Debug.Log($"종유석 태그 터치 감지! 오브젝트: {hit.collider.name}");
                return TouchTarget.Stalactite;
            }
        }
        
        // OverlapPoint로도 체크 (추가 보완)
        Collider2D overlapped = Physics2D.OverlapPoint(worldPosition);
        if (overlapped != null)
        {
            if (overlapped.GetComponent<StalactiteGrowth>() != null || overlapped.CompareTag("Stalactite"))
            {
                Debug.Log($"종유석 OverlapPoint 터치 감지! 오브젝트: {overlapped.name}");
                return TouchTarget.Stalactite;
            }
        }
        
        // 버섯 터치 체크 (드래그 수확용)
        Collider2D mushroom = Physics2D.OverlapCircle(worldPosition, 0.3f);
        if (mushroom != null && mushroom.GetComponent<Mushroom>() != null)
        {
            return TouchTarget.Mushroom;
        }
        
        return TouchTarget.EmptySpace;
    }
    
    /// <summary>
    /// 종유석 터치 시 실행되는 모든 액션 (SimpleWaterDrop으로 위임)
    /// </summary>
    void OnStalactiteTouch(Vector3 touchPosition)
    {
        Debug.Log("종유석 터치! (TouchManager에서 감지)");
        
        // TouchManager의 터치 효과음 재생
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayTouchSFX();
        }
        
        // SimpleWaterDrop에서 종유석 터치 처리 (물방울, 성장 촉진, 떨림 등)
        if (waterDropSystem != null)
        {
            waterDropSystem.OnStalactiteTouched();
        }
        
        // TouchManager 고유의 균열 증가 처리
        if (stalactiteGrowth != null)
        {
            stalactiteGrowth.AddCrackFromTouch();
        }
    }

    /// <summary>
    /// 빈 공간 터치 시 실행
    /// </summary>
    void OnEmptySpaceTouch(Vector3 touchPosition)
    {
        Debug.Log("빈 공간 터치");

        PlayTouchParticle(touchPosition);
        // 빈 공간 터치 시 액션 (나중에 추가 가능)
        // 예: 배경 터치 효과, 파티클 생성 등
    }
    
    #region 드래그 수확 시스템
    
    /// <summary>
    /// 반경 내 버섯들 수확
    /// </summary>
    void HarvestMushroomsInRadius(Vector3 center)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, mushroomHarvestRadius);
        
        foreach (Collider2D col in colliders)
        {
            Mushroom mushroom = col.GetComponent<Mushroom>();
            if (mushroom != null && mushroom.isHarvestable && !harvestedMushrooms.Contains(mushroom))
            {
                harvestedMushrooms.Add(mushroom);
                mushroom.HarvestMushroom();
                
                Debug.Log($"드래그 수확: {mushroom.name}");
            }
        }
    }
    
    /// <summary>
    /// 드래그 궤적용 TrailRenderer 생성
    /// </summary>
    void CreateDragTrail()
    {
        trailObject = new GameObject("DragTrail");
        trailObject.transform.SetParent(transform);
        trailObject.transform.position = Vector3.zero;
        
        dragTrail = trailObject.AddComponent<TrailRenderer>();
        
        // TrailRenderer 설정
        dragTrail.material = new Material(Shader.Find("Sprites/Default"));
        dragTrail.material.color = new Color(1f, 1f, 0f, 0.3f); // 노란색
        dragTrail.startWidth = 0.15f;
        dragTrail.endWidth = 0.05f;
        dragTrail.time = 0.2f; // 트레일 지속 시간
        dragTrail.minVertexDistance = 0.001f; // 최소 정점 간격 (더 부드럽게)
        dragTrail.autodestruct = false;
        dragTrail.sortingOrder = 5; // UI보다 아래, 게임오브젝트보다 위
        
        // 처음에는 비활성화
        trailObject.SetActive(false);
        
        Debug.Log("TrailRenderer 생성 완료");
    }
    
    /// <summary>
    /// 드래그 궤적 시작
    /// </summary>
    void StartDragTrail(Vector3 startPosition)
    {
        if (trailObject == null) return;
        
        // 먼저 트레일을 완전히 초기화
        dragTrail.Clear();
        trailObject.transform.position = startPosition;
        
        // 한 프레임 후 활성화하여 이전 트레일과의 연결 방지
        StartCoroutine(ActivateTrailAfterFrame());
        
        Debug.Log($"드래그 트레일 시작: {startPosition}");
    }
    
    /// <summary>
    /// 한 프레임 후 트레일 활성화
    /// </summary>
    System.Collections.IEnumerator ActivateTrailAfterFrame()
    {
        if (trailObject != null)
        {
            trailObject.SetActive(false);
            yield return null; // 한 프레임 대기
            trailObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 드래그 궤적 업데이트
    /// </summary>
    void UpdateDragTrail(Vector3 newPosition)
    {
        if (trailObject == null) return;
        
        trailObject.transform.position = newPosition;
    }
    
    /// <summary>
    /// 드래그 궤적 종료
    /// </summary>
    void EndDragTrail()
    {
        if (trailObject == null) return;
        
        // TrailRenderer는 자동으로 사라지므로 일정 시간 후 비활성화
        StartCoroutine(DisableTrailAfterDelay());
    }
    
    /// <summary>
    /// 트레일이 자연스럽게 사라진 후 오브젝트 비활성화
    /// </summary>
    System.Collections.IEnumerator DisableTrailAfterDelay()
    {
        // 트레일 시간만큼 대기 (자연스럽게 사라질 때까지)
        yield return new WaitForSeconds(dragTrail.time + 0.1f);
        
        if (trailObject != null)
        {
            trailObject.SetActive(false);
        }
    }
    
    #endregion
    
    #region 터치 파티클 시스템
    
    /// <summary>
    /// 터치 위치에 파티클 재생
    /// </summary>
    void PlayTouchParticle(Vector3 position)
    {
        if (!enableTouchParticles || touchParticleSystem == null) return;
        
        // 파티클 시스템을 터치 위치로 이동
        touchParticleSystem.transform.position = position;
        
        // 파티클 재생
        touchParticleSystem.Play();
        
        Debug.Log($"터치 파티클 재생: {position}");
    }
    
    /// <summary>
    /// 터치 파티클 시스템 설정
    /// </summary>
    public void SetTouchParticleSystem(ParticleSystem particleSystem)
    {
        touchParticleSystem = particleSystem;
    }
    
    #endregion
    
    /// <summary>
    /// 종유석 떨림 효과
    /// </summary>
    IEnumerator StalactiteShakeEffect()
    {
        if (stalactiteTransform == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / shakeDuration;
            
            // 떨림 효과 (랜덤 진동)
            Vector3 shakeOffset = new Vector3(
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                UnityEngine.Random.Range(-shakeIntensity * 0.3f, shakeIntensity * 0.3f), // Y축은 약간만
                0f
            );
            
            // 점진적으로 떨림 약화
            shakeOffset *= (1f - progress);
            stalactiteTransform.position = originalStalactitePosition + shakeOffset;
            
            yield return null;
        }
        
        // 원래 위치로 복귀
        stalactiteTransform.position = originalStalactitePosition;
    }
    
    /// <summary>
    /// 터치 활성화/비활성화
    /// </summary>
    public void SetTouchEnabled(bool enabled)
    {
        enableTouch = enabled;
        Debug.Log($"터치 시스템: {(enabled ? "활성화" : "비활성화")}");
    }
    
    /// <summary>
    /// 종유석 터치 반경 설정
    /// </summary>
    public void SetStalactiteTouchRadius(float radius)
    {
        stalactiteTouchRadius = Mathf.Max(0.1f, radius);
    }
    
    /// <summary>
    /// 시스템 참조 업데이트 (런타임에서 변경 가능)
    /// </summary>
    public void UpdateSystemReferences()
    {
        if (waterDropSystem == null)
        {
            waterDropSystem = FindObjectOfType<SimpleWaterDrop>();
        }
        
        if (stalactiteGrowth == null)
        {
            stalactiteGrowth = FindObjectOfType<StalactiteGrowth>();
        }
    }
    
    // 기즈모로 터치 반경 표시
    void OnDrawGizmosSelected()
    {
        if (stalactiteTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(stalactiteTransform.position, stalactiteTouchRadius);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(stalactiteTransform.position, 0.1f);
        }
        
        // 드래그 수확 반경 표시
        if (isDragging)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lastDragPosition, mushroomHarvestRadius);
        }
    }
    
    // 디버그용
    [ContextMenu("Test Stalactite Touch")]
    public void TestStalactiteTouch()
    {
        if (stalactiteTransform != null)
        {
            OnStalactiteTouch(stalactiteTransform.position);
        }
    }
}

/// <summary>
/// 터치 대상 타입
/// </summary>
public enum TouchTarget
{
    Stalactite,    // 종유석
    Mushroom,      // 버섯
    EmptySpace,    // 빈 공간
    UI             // UI 요소 (나중에 추가)
}