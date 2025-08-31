using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// ���� ��ġ ���� �ý��� - ��� ��ġ �Է��� �����ϰ� ������ �׼� ����
/// </summary>
public class TouchManager : MonoBehaviour
{
    [Header("Touch Targets")]
    public Transform stalactiteTransform;        // ������ Transform
    public float stalactiteTouchRadius = 1f;    // ������ ��ġ �ݰ�

    [Header("Mushroom Drag Harvest")]
    public float mushroomHarvestRadius = 0.8f;   // ���� �巡�� ��Ȯ �ݰ�
    public bool enableDragHarvest = true;        // �巡�� ��Ȯ Ȱ��ȭ
    public bool showDragTrail = true;            // �巡�� ���� ǥ��

    [Header("Systems")]
    public SimpleWaterDrop waterDropSystem;      // ����� �ý���
    public StalactiteGrowth stalactiteGrowth;    // ������ ���� �ý���

    [Header("Touch Settings")]
    public bool enableTouch = true;              // ��ġ Ȱ��ȭ
    public float touchCooldown = 0.1f;           // ��ġ ��Ÿ�� (���� ��ġ ����)

    [Header("Visual Feedback")]
    public bool enableTouchEffects = true;      // ��ġ ����Ʈ Ȱ��ȭ
    public float shakeIntensity = 0.05f;        // ���� ����
    public float shakeDuration = 0.3f;          // ���� ���� �ð�

    private float lastTouchTime = 0f;           // ������ ��ġ �ð�
    private Vector3 originalStalactitePosition; // ������ ���� ��ġ
    private Camera mainCamera;

    // �巡�� ��Ȯ ����
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

        // ������ ���� ��ġ ����
        if (stalactiteTransform != null)
        {
            originalStalactitePosition = stalactiteTransform.position;
        }

        // �ڵ����� ������Ʈ ã�� (�Ҵ���� ���� ���)
        if (waterDropSystem == null)
        {
            waterDropSystem = FindObjectOfType<SimpleWaterDrop>();
        }

        if (stalactiteGrowth == null)
        {
            stalactiteGrowth = FindObjectOfType<StalactiteGrowth>();
        }

        // �巡�� ������ LineRenderer ����
        if (showDragTrail)
        {
            CreateDragTrail();
        }
    }

    void Update()
    {
        if (!enableTouch) return;

        // ��ġ ��Ÿ�� üũ
        if (Time.time - lastTouchTime < touchCooldown) return;

        // �Է� ó��
        HandleInput();
    }

    /// <summary>
    /// ��ġ/Ŭ�� �Է� ó��
    /// </summary>
    void HandleInput()
    {
        bool inputActive = false;
        bool inputEnded = false;
        Vector3 inputPosition = Vector3.zero;

        // ����� ��ġ
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
            {
                inputActive = true;
                inputPosition = GetWorldPosition(touch.position);
                Debug.Log($"��ġ ����: ��ũ��({touch.position}) -> ����({inputPosition})");
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                inputEnded = true;
                inputPosition = GetWorldPosition(touch.position);
            }
        }
        // PC ���콺
        else if (Input.GetMouseButton(0))
        {
            inputActive = true;
            inputPosition = GetWorldPosition(Input.mousePosition);
            Debug.Log($"���콺 �巡��: ��ũ��({Input.mousePosition}) -> ����({inputPosition})");
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
            Debug.Log($"���콺 Ŭ�� ����: ��ũ��({Input.mousePosition}) -> ����({inputPosition})");
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
    /// ��ũ�� ��ǥ�� ���� ��ǥ�� ��ȯ
    /// </summary>
    Vector3 GetWorldPosition(Vector3 screenPosition)
    {
        if (mainCamera != null)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
            worldPos.z = 0f; // 2D �����̹Ƿ� Z�� 0
            return worldPos;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// ��ġ �Է� ó�� (�巡�� ����)
    /// </summary>
    void ProcessTouchInput(Vector3 currentPosition)
    {
        if (!isDragging)
        {
            // ù ��ġ - �巡�� ���� �Ǵ� ���� ��ġ
            StartTouch(currentPosition);
        }
        else
        {
            // �巡�� �� - ���� ��Ȯ üũ
            if (enableDragHarvest)
            {
                float dragDistance = Vector3.Distance(currentPosition, lastDragPosition);
                if (dragDistance >= 0.1f) // �ּ� �巡�� �Ÿ�
                {
                    HarvestMushroomsInRadius(currentPosition);
                    UpdateDragTrail(currentPosition);
                    lastDragPosition = currentPosition;
                }
            }
        }
    }

    /// <summary>
    /// ��ġ ���� ó��
    /// </summary>
    void StartTouch(Vector3 position)
    {
        isDragging = true;
        lastDragPosition = position;
        harvestedMushrooms.Clear();

        // ��ġ ��� �Ǻ�
        TouchTarget target = GetTouchTarget(position);

        switch (target)
        {
            case TouchTarget.Stalactite:
                OnStalactiteTouch(position);
                break;

            case TouchTarget.Mushroom:
                // ���� ��ġ�� �巡�׷θ� ó��
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
    /// ��ġ ���� ó��
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

            Debug.Log("��ġ/�巡�� ����!");
        }
    }

    /// <summary>
    /// ��ġ ��� �Ǻ�
    /// </summary>
    TouchTarget GetTouchTarget(Vector3 worldPosition)
    {
        // ������ ��ġ üũ
        if (stalactiteTransform != null)
        {
            float distance = Vector2.Distance(worldPosition, stalactiteTransform.position);
            Debug.Log($"���������� �Ÿ�: {distance:F2} (��ġ �ݰ�: {stalactiteTouchRadius})");

            if (distance <= stalactiteTouchRadius)
            {
                Debug.Log("������ ��ġ ���� �ȿ� ����!");
                return TouchTarget.Stalactite;
            }
        }
        else
        {
            Debug.LogError("stalactiteTransform�� null�Դϴ�!");
        }

        // ���� ��ġ üũ (�巡�� ��Ȯ��)
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
    /// ������ ��ġ �� ����Ǵ� ��� �׼�
    /// </summary>
    void OnStalactiteTouch(Vector3 touchPosition)
    {
        Debug.Log("������ ��ġ!");

        // 1. ��ġ ȿ���� ���
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayTouchSFX();
        }

        // 2. ����� ����߸���
        if (waterDropSystem != null)
        {
            waterDropSystem.DropWater();
        }

        // 3. ������ ���� ���� + �տ� ���� (������ �������)
        if (stalactiteGrowth != null)
        {
            // ���� ���� �ð� Ȯ�� �� �����ϰ� ó��
            DateTime currentCreationTime = stalactiteGrowth.creationTime;
            DateTime minDateTime = DateTime.MinValue.AddDays(1); // �ּ� ���� �ð�

            Debug.Log($"���� ���� �ð�: {currentCreationTime}, �ּ� �ð�: {minDateTime}");

            // ���� �ð��� �ʹ� �̸� ��� (�����÷ο� ����)
            if (currentCreationTime <= minDateTime)
            {
                Debug.LogWarning("���� �ð��� �ʹ� �̸��ϴ�. ���� �ð����� �����մϴ�.");
                stalactiteGrowth.creationTime = DateTime.Now.AddDays(-1); // 1�� ������ ����
            }
            else
            {
                // �����ϰ� 1�ð� �մ���
                try
                {
                    stalactiteGrowth.creationTime = currentCreationTime.AddHours(-1);
                    Debug.Log($"���� ����: 1�ð� �մ�� ({currentCreationTime} �� {stalactiteGrowth.creationTime})");
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    Debug.LogError("�ð� ��� �����÷ο�! ���� �ð����� �����մϴ�.");
                    stalactiteGrowth.creationTime = DateTime.Now.AddDays(-1);
                }
            }

            // �տ� ����
            stalactiteGrowth.AddCrackFromTouch();
        }

        // 4. �ð��� �ǵ�� (���� �ִϸ��̼�)
        if (enableTouchEffects && stalactiteTransform != null)
        {
            StartCoroutine(StalactiteShakeEffect());
        }
    }

    /// <summary>
    /// �� ���� ��ġ �� ����
    /// </summary>
    void OnEmptySpaceTouch(Vector3 touchPosition)
    {
        Debug.Log("�� ���� ��ġ");

        // �� ���� ��ġ �� �׼� (���߿� �߰� ����)
        // ��: ��� ��ġ ȿ��, ��ƼŬ ���� ��
    }

    #region �巡�� ��Ȯ �ý���

    /// <summary>
    /// �ݰ� �� ������ ��Ȯ
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

                Debug.Log($"�巡�� ��Ȯ: {mushroom.name}");
            }
        }
    }

    /// <summary>
    /// �巡�� ������ LineRenderer ����
    /// </summary>
    void CreateDragTrail()
    {
        GameObject trailObj = new GameObject("DragTrail");
        trailObj.transform.SetParent(transform);

        dragTrail = trailObj.AddComponent<LineRenderer>();
        dragTrail.material = new Material(Shader.Find("Sprites/Default"));
        dragTrail.material.color = new Color(1f, 1f, 0f, 0.7f); // ������ �����
        dragTrail.startWidth = 0.1f;
        dragTrail.endWidth = 0.05f;
        dragTrail.useWorldSpace = true;
        dragTrail.sortingOrder = 10;
        dragTrail.enabled = false;
    }

    /// <summary>
    /// �巡�� ���� ����
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
    /// �巡�� ���� ������Ʈ
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
    /// �巡�� ���� ����
    /// </summary>
    void EndDragTrail()
    {
        if (dragTrail == null) return;

        StartCoroutine(FadeDragTrail());
    }

    /// <summary>
    /// �巡�� ���� ���̵� �ƿ�
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
    /// ������ ���� ȿ��
    /// </summary>
    IEnumerator StalactiteShakeEffect()
    {
        if (stalactiteTransform == null) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / shakeDuration;

            // ���� ȿ�� (���� ����)
            Vector3 shakeOffset = new Vector3(
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                UnityEngine.Random.Range(-shakeIntensity * 0.3f, shakeIntensity * 0.3f), // Y���� �ణ��
                0f
            );

            // ���������� ���� ��ȭ
            shakeOffset *= (1f - progress);
            stalactiteTransform.position = originalStalactitePosition + shakeOffset;

            yield return null;
        }

        // ���� ��ġ�� ����
        stalactiteTransform.position = originalStalactitePosition;
    }

    /// <summary>
    /// ��ġ Ȱ��ȭ/��Ȱ��ȭ
    /// </summary>
    public void SetTouchEnabled(bool enabled)
    {
        enableTouch = enabled;
        Debug.Log($"��ġ �ý���: {(enabled ? "Ȱ��ȭ" : "��Ȱ��ȭ")}");
    }

    /// <summary>
    /// ������ ��ġ �ݰ� ����
    /// </summary>
    public void SetStalactiteTouchRadius(float radius)
    {
        stalactiteTouchRadius = Mathf.Max(0.1f, radius);
    }

    /// <summary>
    /// �ý��� ���� ������Ʈ (��Ÿ�ӿ��� ���� ����)
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

    // ������ ��ġ �ݰ� ǥ��
    void OnDrawGizmosSelected()
    {
        if (stalactiteTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(stalactiteTransform.position, stalactiteTouchRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(stalactiteTransform.position, 0.1f);
        }

        // �巡�� ��Ȯ �ݰ� ǥ��
        if (isDragging && enableDragHarvest)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lastDragPosition, mushroomHarvestRadius);
        }
    }

    // ����׿�
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
/// ��ġ ��� Ÿ��
/// </summary>
public enum TouchTarget
{
    Stalactite,    // ������
    Mushroom,      // ����
    EmptySpace,    // �� ����
    UI             // UI ��� (���߿� �߰�)
}