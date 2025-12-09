using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("설정")]
    public int maxHealth = 16;
    public float invincibilityDuration = 1.5f;
    public Vector2 knockbackForce = new Vector2(3f, 3f);

    [Header("연결")]
    public Image healthBarImage;
    public GameObject deathEffectPrefab;

    int currentHealth;
    bool isInvincible;
    bool isDead;

    PlayerController playerController;
    SpriteRenderer sr;
    Animator anim;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
        UpdateUI();
    }

    void Update()
    {
        // 빈사 상태 체크 (애니메이터 전달)
        if (!isDead && anim != null)
        {
            bool isLow = ((float)currentHealth / maxHealth) <= 0.3f;
            anim.SetBool("IsLowHP", isLow);
        }
    }

    public void TakeDamage(int damage, float damageOriginX)
    {
        if (isDead || isInvincible) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        UpdateUI();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // 플레이어는 맞으면 넉백 + 경직 발생
        if (playerController != null)
        {
            playerController.OnHurt(damageOriginX, knockbackForce, 0.3f);
        }

        StartCoroutine(InvincibilityRoutine());
    }

    void Die()
    {
        isDead = true;
        anim.SetBool("IsDead", true); // 사망 애니메이션
        if (playerController != null) playerController.OnDie(); // 조작 정지
        if (deathEffectPrefab) Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
    }

    void UpdateUI()
    {
        if (healthBarImage) healthBarImage.fillAmount = (float)currentHealth / maxHealth;
    }

    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float timer = 0;
        while (timer < invincibilityDuration)
        {
            sr.color = new Color(1, 1, 1, 0.3f);
            yield return new WaitForSeconds(0.05f);
            sr.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            timer += 0.1f;
        }
        sr.color = Color.white;
        isInvincible = false;
    }
}