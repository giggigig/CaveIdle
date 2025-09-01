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
    public ParticleSystem splashParticle; // 스플래시 파티클 (선택사항)

    [Header("Splash Settings")]
    public bool enableSplashParticle = true; // 스플래시 파티클 활성화
    public float splashDuration = 0.5f;      // 스플래시 지속 시간
    public int splashParticleCount = 10;     // 파티클 개수
    public Transform stalactite;        // 종유석 (터치 감지용)

    [Header("Settings")]
    public float dropDuration = 1f;     // 물방울이 떨어지는 시간
    public float autoDropInterval = 3f; // 자동 떨어지는 간격 (3초)

    void Start()
    {
        // 3초마다 자동으로 물방울 떨어뜨리기
        StartCoroutine(AutoDropRoutine());
    }

    // TouchManager에서 터치를 관리하므로 Update에서 터치 감지 제거

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
    /// TouchManager에서 호출되는 종유석 터치 처리 (터치 감지는 TouchManager에서 담당)
    /// </summary>
    public void OnStalactiteTouched()
    {
        TouchFeedback(); // 터치 피드백 효과
        BoostStalactiteGrowth(); // 성장 촉진 + 균열 증가 
        DropWater(); // 추가 물방울 떨어뜨리기
    }

    /// <summary>
    /// 물방울 떨어뜨리기
    /// </summary>
    public void DropWater()
    {
        if (startPoint == null || endPoint == null || waterDropPrefab == null) return;

        // 물방울 생성
        GameObject drop = Instantiate(waterDropPrefab, startPoint.position, Quaternion.identity);
        SoundManager.Instance.PlayWaterDropSFX();
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
            // 생성 시간을 10분 앞당기기 (성장 촉진 수치 조정)
            stalactiteGrowth.creationTime = stalactiteGrowth.creationTime.AddMinutes(-10);

            Debug.Log("종유석 성장이 10분 촉진되었습니다!");
        }
    }

    /// <summary>
    /// 물방울이 바닥에 닿았을 때
    /// </summary>
    void OnWaterHitGround(Vector3 position)
    {
        Debug.Log("물방울이 바닥에 떨어졌습니다!");

        // 습도 증가 (MushroomManager에 알림)
        MushroomManager mushroomManager = FindObjectOfType<MushroomManager>();
        if (mushroomManager != null)
        {
            mushroomManager.AddHumidityFromWaterDrop();
        }

        // 스플래시 파티클 재생
        if (enableSplashParticle)
        {
            PlaySplashParticle(position);
        }

        // 스플래시 효과음 (나중에 추가 가능)
        // if (SoundManager.Instance != null)
        // {
        //     SoundManager.Instance.PlaySplashSFX();
        // }
    }

    /// <summary>
    /// 스플래시 파티클 재생
    /// </summary>
    void PlaySplashParticle(Vector3 position)
    {
        if (splashParticle != null)
        {
            // 파티클 시스템이 할당되어 있는 경우
            PlayAssignedParticleSystem(position);
        }
        else
        {
            // 파티클 시스템이 없으면 간단한 스플래시 생성
            PlaySimpleSplashEffect(position);
        }
    }

    /// <summary>
    /// 할당된 파티클 시스템 재생
    /// </summary>
    void PlayAssignedParticleSystem(Vector3 position)
    {
        // 파티클 시스템 위치 설정
        splashParticle.transform.position = position;

        // 파티클 개수 설정
        var emission = splashParticle.emission;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, splashParticleCount)
        });

        // 파티클 재생
        splashParticle.Play();

        Debug.Log($"스플래시 파티클 재생: {position}, 파티클 수: {splashParticleCount}");
    }

    /// <summary>
    /// 간단한 스플래시 효과 (파티클 시스템이 없을 때)
    /// </summary>
    void PlaySimpleSplashEffect(Vector3 position)
    {
        // 물방울 프리팹을 이용한 간단한 스플래시 효과
        if (waterDropPrefab != null)
        {
            StartCoroutine(CreateSimpleSplash(position));
        }
    }

    /// <summary>
    /// 간단한 스플래시 효과 애니메이션
    /// </summary>
    System.Collections.IEnumerator CreateSimpleSplash(Vector3 centerPosition)
    {
        int dropCount = 5; // 스플래시 물방울 개수
        GameObject[] splashDrops = new GameObject[dropCount];

        // 여러 개의 작은 물방울 생성
        for (int i = 0; i < dropCount; i++)
        {
            GameObject splashDrop = Instantiate(waterDropPrefab, centerPosition, Quaternion.identity);

            // 크기를 작게 조절
            splashDrop.transform.localScale = Vector3.one * 0.3f;

            splashDrops[i] = splashDrop;

            // 랜덤한 방향으로 튕김
            StartCoroutine(AnimateSplashDrop(splashDrop, centerPosition, i));
        }

        // 스플래시 지속 시간 후 정리
        yield return new WaitForSeconds(splashDuration);

        foreach (GameObject drop in splashDrops)
        {
            if (drop != null)
            {
                Destroy(drop);
            }
        }
    }

    /// <summary>
    /// 개별 스플래시 물방울 애니메이션
    /// </summary>
    System.Collections.IEnumerator AnimateSplashDrop(GameObject drop, Vector3 startPos, int index)
    {
        if (drop == null) yield break;

        // 랜덤한 방향과 거리
        float angle = (360f / 5f) * index + Random.Range(-30f, 30f); // 각도 분산
        float distance = Random.Range(0.3f, 0.8f); // 튕김 거리

        Vector3 targetPos = startPos + new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
            Random.Range(0.1f, 0.3f), // 위로 살짝 튕김
            0f
        );

        float animTime = 0f;

        while (animTime < splashDuration && drop != null)
        {
            animTime += Time.deltaTime;
            float progress = animTime / splashDuration;

            // 포물선 운동 (위로 튕겼다가 아래로)
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);
            currentPos.y = Mathf.Lerp(startPos.y, targetPos.y, progress) +
                          Mathf.Sin(progress * Mathf.PI) * 0.2f; // 포물선 효과

            drop.transform.position = currentPos;

            // 점점 투명해짐 (SpriteRenderer가 있다면)
            SpriteRenderer sr = drop.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color color = sr.color;
                color.a = Mathf.Lerp(1f, 0f, progress);
                sr.color = color;
            }

            yield return null;
        }
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