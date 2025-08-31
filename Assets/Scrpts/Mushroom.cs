using UnityEngine;
using System.Collections;

/// <summary>
/// ���� ������ ���� �� ��ȣ�ۿ��� ����
/// </summary>
public class Mushroom : MonoBehaviour
{
    public enum GrowthStage
    {
        Sprout = 0,    // ���� (����)
        Growing = 1,   // ���� �� (�߰�)  
        Mature = 2     // ���� (��Ȯ ����)
    }

    [Header("Growth Sprites")]
    public Sprite[] growthSprites = new Sprite[3];  // 3�ܰ� ��������Ʈ �迭

    [Header("Current State")]
    public GrowthStage currentStage = GrowthStage.Sprout;
    public float growthProgress = 0f;               // 0~1 ���� �����
    public bool isHarvestable = false;              // ��Ȯ ���� ����

    private MushroomManager manager;
    private SpriteRenderer spriteRenderer;
    private float stageTime = 100f;                 // �� �ܰ躰 �ҿ� �ð�
    private float totalGrowthTime;                  // ��ü ���� �ð� (3�ܰ�)
    private float startTime;                        // ���� ���� �ð�

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Collider2D �߰� (�巡�� ��Ȯ�� ���� �ʿ�)
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
            circleCollider.radius = 0.3f; // ���� ũ�⿡ �°� ����
        }
    }

    /// <summary>
    /// ���� �ʱ�ȭ (MushroomManager���� ȣ��)
    /// </summary>
    public void Initialize(MushroomManager mushroomManager, float growthStageTime)
    {
        manager = mushroomManager;
        stageTime = growthStageTime;
        totalGrowthTime = stageTime * 3; // 3�ܰ��̹Ƿ�
        startTime = Time.time;

        currentStage = GrowthStage.Sprout;
        growthProgress = 0f;
        isHarvestable = false;

        UpdateVisual();

        Debug.Log($"���� �ʱ�ȭ: �ܰ躰 {stageTime}��, �� {totalGrowthTime}��");
    }

    void Update()
    {
        if (!isHarvestable) // ���� �������� �ʾ����� ��� ����
        {
            UpdateGrowth();
        }

        // ��ġ ����
        HandleInput();
    }

    /// <summary>
    /// ���� ������Ʈ
    /// </summary>
    void UpdateGrowth()
    {
        float elapsedTime = Time.time - startTime;
        growthProgress = Mathf.Clamp01(elapsedTime / totalGrowthTime);

        // ���� �ܰ� ���
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

        // �ܰ谡 �ٲ������ ���־� ������Ʈ
        if (newStage != currentStage)
        {
            currentStage = newStage;
            UpdateVisual();

            // ���� �ܰ迡 �����ϸ� ��Ȯ ����
            if (currentStage == GrowthStage.Mature)
            {
                isHarvestable = true;
                Debug.Log("������ �����߽��ϴ�! ��Ȯ ����!");
            }
        }
    }

    /// <summary>
    /// ���־� ������Ʈ
    /// </summary>
    void UpdateVisual()
    {
        if (spriteRenderer != null && growthSprites.Length >= 3)
        {
            int stageIndex = (int)currentStage;
            if (stageIndex < growthSprites.Length && growthSprites[stageIndex] != null)
            {
                spriteRenderer.sprite = growthSprites[stageIndex];

                // ���忡 ���� ũ�� ���� (���û���)
                float scaleMultiplier = 0.5f + (growthProgress * 0.5f); // 0.5 ~ 1.0
                transform.localScale = Vector3.one * scaleMultiplier;
            }
        }
    }

    /// <summary>
    /// ��ġ �Է� ó��
    /// </summary>
    void HandleInput()
    {
        bool inputDetected = false;
        Vector3 inputPosition = Vector3.zero;

        // ����� ��ġ
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                inputDetected = true;
                inputPosition = Camera.main.ScreenToWorldPoint(touch.position);
            }
        }
        // PC ���콺
        else if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
            inputPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        if (inputDetected)
        {
            // ��ġ ��ġ�� �� ���� ��ó���� Ȯ��
            float distance = Vector2.Distance(inputPosition, transform.position);
            if (distance <= 0.5f) // ��ġ �ݰ�
            {
                OnTouched();
            }
        }
    }

    /// <summary>
    /// ������ ��ġ�Ǿ��� ��
    /// </summary>
    void OnTouched()
    {
        if (isHarvestable)
        {
            // ��Ȯ ������ ���� ��ġ �� ��Ȯ
            HarvestMushroom();
        }
        else
        {
            // ���� �������� ���� ���� ��ġ �� ���� ����
            BoostGrowth();
        }
    }

    /// <summary>
    /// ���� ��Ȯ (�ܺο����� ȣ�� �����ϵ��� public���� ����)
    /// </summary>
    public void HarvestMushroom()
    {
        if (!isHarvestable) return; // ��Ȯ �Ұ����ϸ� ����

        Debug.Log("���� ��Ȯ!");

        // ��Ȯ ȿ���� ���
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonSFX(); // �ӽ÷� ��ư�� ���
        }

        // ��Ȯ ȿ�� (������ ũ�� ��ȭ)
        StartCoroutine(HarvestEffect());

        // �Ŵ������� ��Ȯ �˸�
        if (manager != null)
        {
            manager.HarvestMushroom(this);
        }

        // ���� ����
        Destroy(gameObject, 0.3f); // 0.3�� �� ���� (ȿ�� �ð�)
    }

    /// <summary>
    /// ���� ���� (��ġ ��)
    /// </summary>
    void BoostGrowth()
    {
        Debug.Log("���� ���� ����!");

        // 30�� ���� ����
        startTime -= 30f;

        // ��� ���� üũ
        UpdateGrowth();

        // ��ġ �ǵ�� ȿ��
        StartCoroutine(TouchFeedbackEffect());
    }

    /// <summary>
    /// ��Ȯ ȿ�� �ִϸ��̼�
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

            // ũ�Ⱑ Ŀ���ٰ� �����
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
    /// ��ġ �ǵ�� ȿ��
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

            // ��¦ Ŀ���ٰ� �������
            float scale = 1f + (Mathf.Sin(progress * Mathf.PI) * 0.1f);
            transform.localScale = originalScale * scale;

            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// ���� ���� ��ȯ
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

    // ������ ��ġ ���� ǥ��
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f); // ��ġ �ݰ�

        if (isHarvestable)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
        }
    }
}

/// <summary>
/// ���� ���� ����ü
/// </summary>
[System.Serializable]
public struct MushroomInfo
{
    public Mushroom.GrowthStage stage;      // ���� ���� �ܰ�
    public float growthProgress;            // ���� ����� (0~1)
    public bool isHarvestable;              // ��Ȯ ���� ����
    public float timeRemaining;             // �������� ���� �ð� (��)
}