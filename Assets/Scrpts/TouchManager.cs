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
    public bool enableDragHarvest = true;        // 드래그 수확 활성화
    public bool showDragTrail = true;            // 드래그 궤적 표시

    [Header("Systems")]
    public SimpleWaterDrop waterDropSystem;      // 물방울 시스템
    public StalactiteGrowth stalactiteGrowth;    // 종유석 성장 시스템

    [Header("Touch Settings")]
    public bool enableTouch = true;              // 터치 활성화
    public float touchCooldown = 0.1f;           // 터치 쿨타임 (연속 터치 방지)

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
    private LineRenderer dragTrail;
    private List<Vector3> trailPoints = new List<Vector3>();

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

        // 드래그 궤적용 LineRenderer 생성
        if (showDragTrail)
        {
            CreateDragTrail();
        }
    }

    void Update()
    {
        if (!enableTouch) return;

        // 터치 쿨타임 체크
        if (Time.time - lastTouchTime < touchCooldown) return;

        // 입력 처리
        HandleInput();
    }

    /// <summary>
    /// 터치/클릭 입력 처리
    /// </summary>
    void HandleInput()
    {
        bool inputActive = false;
        bool inputEnded = false;
        Vector3 inputPosition = Vector3.zero;

        // 모바일 터치
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
            {
                inputActive = true;
                inputPosition = GetWorldPosition(touch.position);
                Debug.Log($"터치 감지: 스크린({touch.position}) -> 월드({inputPosition})");
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                inputEnded = true;
                inputPosition = GetWorldPosition(touch.position);
            }
        }
        // PC 마우스
        else if (Input.GetMouseButton(0))
        {
            inputActive = true;
            inputPosition = GetWorldPosition(Input.mousePosition);
            Debug.Log($"마우스 드래그: 스크린({Input.mousePosition}) -> 월드({inputPosition})");
        }
        else if (Input.GetMouseButtonUp(0))
        {
            inputEnded = true;
            inputPosition = GetWorldPosition(Input.mousePosition);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            inputActive = true;
            inputPosition = GetWorldPosition(Input.mousePosition);
            Debug.Log($"마우스 클릭 감지: 스크린({Input.mousePosition}) -> 월드({inputPosition})");
        }

        if (inputActive)
        {
            ProcessTouchInput(inputPosition);
            lastTouchTime = Time.time;
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
            // 드래그 중 - 버섯 수확 체크
            if (enableDragHarvest)
            {
                float dragDistance = Vector3.Distance(currentPosition, lastDragPosition);
                if (dragDistance >= 0.1f) // 최소 드래그 거리
                {
                    HarvestMushroomsInRadius(currentPosition);
                    UpdateDragTrail(currentPosition);
                    lastDragPosition = currentPosition;
                }
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

        // 터치 대상 판별
        TouchTarget target = GetTouchTarget(position);

        switch (target)
        {
            case TouchTarget.Stalactite:
                OnStalactiteTouch(position);
                break;

            case TouchTarget.Mushroom:
                // 버섯 터치는 드래그로만 처리
                if (enableDragHarvest)
                {
                    HarvestMushroomsInRadius(position);
                    if (showDragTrail)
                    {
                        StartDragTrail(position);
                    }
                }
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

            if (showDragTrail && dragTrail != null)
            {
                EndDragTrail();
            }

            Debug.Log("터치/드래그 종료!");
        }
    }

    /// <summary>
    /// 터치 대상 판별
    /// </summary>
    TouchTarget GetTouchTarget(Vector3 worldPosition)
    {
        // 종유석 터치 체크
        if (stalactiteTransform != null)
        {
            float distance = Vector2.Distance(worldPosition, stalactiteTransform.position);
            Debug.Log($"종유석과의 거리: {distance:F2} (터치 반경: {stalactiteTouchRadius})");

            if (distance <= stalactiteTouchRadius)
            {
                Debug.Log("종유석 터치 범위 안에 있음!");
                return TouchTarget.Stalactite;
            }
        }
        else
        {
            Debug.LogError("stalactiteTransform이 null입니다!");
        }

        // 버섯 터치 체크 (드래그 수확용)
        if (enableDragHarvest)
        {
            Collider2D mushroom = Physics2D.OverlapCircle(worldPosition, 0.3f);
            if (mushroom != null && mushroom.GetComponent<Mushroom>() != null)
            {
                return TouchTarget.Mushroom;
            }
        }

        return TouchTarget.EmptySpace;
    }

    /// <summary>
    /// 종유석 터치 시 실행되는 모든 액션
    /// </summary>
    void OnStalactiteTouch(Vector3 touchPosition)
    {
        Debug.Log("종유석 터치!");

        // 1. 터치 효과음 재생
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayTouchSFX();
        }

        // 2. 물방울 떨어뜨리기
        if (waterDropSystem != null)
        {
            waterDropSystem.DropWater();
        }

        // 3. 종유석 성장 촉진 + 균열 증가 (안전한 방식으로)
        if (stalactiteGrowth != null)
        {
            // 현재 생성 시간 확인 후 안전하게 처리
            DateTime currentCreationTime = stalactiteGrowth.creationTime;
            DateTime minDateTime = DateTime.MinValue.AddDays(1); // 최소 안전 시간

            Debug.Log($"현재 생성 시간: {currentCreationTime}, 최소 시간: {minDateTime}");

            // 생성 시간이 너무 이른 경우 (오버플로우 방지)
            if (currentCreationTime <= minDateTime)
            {
                Debug.LogWarning("생성 시간이 너무 이릅니다. 현재 시간으로 리셋합니다.");
                stalactiteGrowth.creationTime = DateTime.Now.AddDays(-1); // 1일 전으로 설정
            }
            else
            {
                // 안전하게 1시간 앞당기기
                try
                {
                    stalactiteGrowth.creationTime = currentCreationTime.AddHours(-1);
                    Debug.Log($"성장 촉진: 1시간 앞당김 ({currentCreationTime} → {stalactiteGrowth.creationTime})");
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    Debug.LogError("시간 계산 오버플로우! 현재 시간으로 리셋합니다.");
                    stalactiteGrowth.creationTime = DateTime.Now.AddDays(-1);
                }
            }

            // 균열 증가
            stalactiteGrowth.AddCrackFromTouch();
        }

        // 4. 시각적 피드백 (떨림 애니메이션)
        if (enableTouchEffects && stalactiteTransform != null)
        {
            StartCoroutine(StalactiteShakeEffect());
        }
    }

    /// <summary>
    /// 빈 공간 터치 시 실행
    /// </summary>
    void OnEmptySpaceTouch(Vector3 touchPosition)
    {
        Debug.Log("빈 공간 터치");

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
    /// 드래그 궤적용 LineRenderer 생성
    /// </summary>
    void CreateDragTrail()
    {
        GameObject trailObj = new GameObject("DragTrail");
        trailObj.transform.SetParent(transform);

        dragTrail = trailObj.AddComponent<LineRenderer>();
        dragTrail.material = new Material(Shader.Find("Sprites/Default"));
        dragTrail.material.color = new Color(1f, 1f, 0f, 0.7f); // 반투명 노란색
        dragTrail.startWidth = 0.1f;
        dragTrail.endWidth = 0.05f;
        dragTrail.useWorldSpace = true;
        dragTrail.sortingOrder = 10;
        dragTrail.enabled = false;
    }

    /// <summary>
    /// 드래그 궤적 시작
    /// </summary>
    void StartDragTrail(Vector3 startPosition)
    {
        if (dragTrail == null) return;

        trailPoints.Clear();
        trailPoints.Add(startPosition);

        dragTrail.enabled = true;
        dragTrail.positionCount = 1;
        dragTrail.SetPosition(0, startPosition);
    }

    /// <summary>
    /// 드래그 궤적 업데이트
    /// </summary>
    void UpdateDragTrail(Vector3 newPosition)
    {
        if (dragTrail == null) return;

        trailPoints.Add(newPosition);

        if (trailPoints.Count > 50)
        {
            trailPoints.RemoveAt(0);
        }

        dragTrail.positionCount = trailPoints.Count;
        for (int i = 0; i < trailPoints.Count; i++)
        {
            dragTrail.SetPosition(i, trailPoints[i]);
        }
    }

    /// <summary>
    /// 드래그 궤적 종료
    /// </summary>
    void EndDragTrail()
    {
        if (dragTrail == null) return;

        StartCoroutine(FadeDragTrail());
    }

    /// <summary>
    /// 드래그 궤적 페이드 아웃
    /// </summary>
    System.Collections.IEnumerator FadeDragTrail()
    {
        if (dragTrail == null) yield break;

        float elapsed = 0f;
        float fadeTime = 0.5f;
        Color originalColor = dragTrail.material.color;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(originalColor.a, 0f, elapsed / fadeTime);

            Color newColor = originalColor;
            newColor.a = alpha;
            dragTrail.material.color = newColor;

            yield return null;
        }

        dragTrail.enabled = false;
        dragTrail.material.color = originalColor;
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
        if (isDragging && enableDragHarvest)
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