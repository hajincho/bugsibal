using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FreezeSkill : MonoBehaviour
{
    public float skillRange = 5f;
    public float freezeDuration = 5f;
    public float cooldown = 14f;
    public KeyCode useKey = KeyCode.F;
    public Color rangeColor = new Color(0f, 0.5f, 1f, 0.3f);
    public Color frozenColor = new Color(0f, 0.5f, 1f, 0.5f);

    [Header("추가 2D 스프라이트")]
    public GameObject extraImagePrefab; // Inspector에서 연결할 이미지 Prefab
    public float extraImageDuration = 1f; // 추가 이미지 유지 시간

    private bool skillOnCooldown = false;
    public Image skillcool;
    CoolDown cooldown4;

    private void Start()
    {
        cooldown4 = skillcool.GetComponent<CoolDown>();
    }
    void Update()
    {
        if (Input.GetKeyDown(useKey) && !skillOnCooldown)
        {
            cooldown4.UseSkill();
            StartCoroutine(UseSkill());
        }
       
    }

    private IEnumerator UseSkill()
    {
        skillOnCooldown = true;

        ShowMeshRange(transform.position, skillRange, rangeColor);

        // 범위 표시용 Circle 생성
        GameObject rangeCircle = new GameObject("FreezeRange");
        rangeCircle.transform.position = new Vector3(transform.position.x, transform.position.y);
        SpriteRenderer rangeSR = rangeCircle.AddComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(1f, 1f));

        rangeSR.sprite = sprite;
        rangeSR.color = rangeColor;
        rangeSR.sortingOrder = 100;
        rangeCircle.transform.localScale = new Vector3(skillRange * 2f, skillRange * 2f);

        // ★ 추가 이미지 소환
        GameObject extraImage = null;
        if (extraImagePrefab != null)
        {
            extraImage = Instantiate(extraImagePrefab);
            extraImage.transform.position = new Vector3(transform.position.x, transform.position.y);
            extraImage.transform.localScale = new Vector3(skillRange, skillRange);

            SpriteRenderer extraSR = extraImage.GetComponent<SpriteRenderer>();
            if (extraSR != null)
                StartCoroutine(FadeIn(extraSR, 0.5f));
                // extraImageDuration 만큼만 유지 후 사라지도록
    if (extraSR != null)
        StartCoroutine(DestroyExtraImage(extraImage, extraSR, extraImageDuration));
        }

        // 적 Freeze 처리
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillRange);
        List<GameObject> frozenOverlays = new List<GameObject>();

        foreach (var hit in hits)
        {
            if (hit == null || hit.tag != "Enemy") continue;

            EnemyMove em = hit.GetComponent<EnemyMove>();
            if (em != null) em.enabled = false;

            Animator animator = hit.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;

            Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.isKinematic = true;
            }

            SpriteRenderer sr = hit.GetComponent<SpriteRenderer>() ?? hit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = Color.cyan;

            // Freeze Overlay 생성
            GameObject overlay = new GameObject("FrozenOverlay");
            overlay.transform.parent = hit.transform;
            overlay.transform.localPosition = Vector3.zero;

            SpriteRenderer overlaySR = overlay.AddComponent<SpriteRenderer>();
            overlaySR.sprite = sprite;
            overlaySR.color = frozenColor;
            overlaySR.sortingOrder = 200;

            if (sr != null)
            {
                Vector3 size = sr.bounds.size;
                overlay.transform.localScale = new Vector3(size.x * 2f, size.y * 2f, 1f);
            }
            else
            {
                overlay.transform.localScale = Vector3.one;
            }

            frozenOverlays.Add(overlay);
        }

        yield return new WaitForSeconds(freezeDuration);

        // 적 원상복구
        foreach (var hit in hits)
        {
            if (hit == null || hit.tag != "Enemy") continue;

            EnemyMove em = hit.GetComponent<EnemyMove>();
            if (em != null) em.enabled = true;

            Animator animator = hit.GetComponent<Animator>();
            if (animator != null) animator.enabled = true;

            Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
            if (rb != null) rb.isKinematic = false;

            SpriteRenderer sr = hit.GetComponent<SpriteRenderer>() ?? hit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;
        }

        foreach (var overlay in frozenOverlays)
            Destroy(overlay);

        Destroy(rangeCircle);

        // ★ 추가 이미지 제거
        if (extraImage != null)
        {
            SpriteRenderer extraSR = extraImage.GetComponent<SpriteRenderer>();
            if (extraSR != null)
                yield return StartCoroutine(FadeOut(extraSR, 0.5f));

            Destroy(extraImage);
        }
    }

    public void ShowMeshRange(Vector3 position, float radius, Color color)
    {
        GameObject rangeMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rangeMesh.transform.position = new Vector3(position.x, position.y, -0.5f);
        rangeMesh.transform.localScale = new Vector3(radius, radius, 0.1f);

        MeshRenderer mr = rangeMesh.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default"));
        mr.material.color = color;

        StartCoroutine(FadeOutMesh(rangeMesh, 0.5f, 1f));
    }

    private IEnumerator FadeOutMesh(GameObject obj, float delay, float duration)
    {
        yield return new WaitForSeconds(delay);

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr == null) yield break;

        Color initialColor = mr.material.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            mr.material.color = new Color(initialColor.r, initialColor.g, initialColor.b, Mathf.Lerp(initialColor.a, 0f, t));
            yield return null;
        }

        Destroy(obj);
    }

    private IEnumerator FadeIn(SpriteRenderer sr, float duration)
    {
        float elapsed = 0f;
        Color c = sr.color;
        c.a = 0f;
        sr.color = c;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / duration);
            sr.color = c;
            yield return null;
        }
    }

    private IEnumerator FadeOut(SpriteRenderer sr, float duration)
    {
        float elapsed = 0f;
        Color c = sr.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(1f - (elapsed / duration));
            sr.color = c;
            yield return null;
        }
    }

    private IEnumerator DestroyExtraImage(GameObject extraImage, SpriteRenderer sr, float duration)
    {
        yield return new WaitForSeconds(duration); // extraImageDuration 만큼 대기

        if (sr != null)
            yield return StartCoroutine(FadeOut(sr, 0.5f));

        if (extraImage != null)
            Destroy(extraImage);
         yield return new WaitForSeconds(cooldown); skillOnCooldown = false;
}
}
