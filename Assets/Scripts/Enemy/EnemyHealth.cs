using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("설정")]
    public int maxHealth = 3;
    public int contactDamage = 2; // 플레이어에게 줄 데미지
    public GameObject deathEffectPrefab;
    public float flashDuration = 0.1f;

    public Vector3 deathEffectOffset = new Vector3(0, 0.5f, 0);

    int currentHealth;
    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
    }

    // 1. 총알 맞기 (Bullet.cs에서 호출)
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
        else StartCoroutine(FlashRoutine());
    }

    // =============================================================
    // 플레이어 공격 (충돌 & 트리거 모두 감지)
    // =============================================================

    // 경우 1: 적이나 플레이어가 "Is Trigger"가 꺼져 있어서 부딪힐 때
    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    // 경우 2: 적이나 플레이어가 "Is Trigger"가 켜져 있어서 겹칠 때
    private void OnTriggerStay2D(Collider2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    // 공통 로직: 플레이어 태그 확인하고 데미지 주기
    void TryDamagePlayer(GameObject target)
    {
        Debug.Log($"[충돌 감지됨] 부딪힌 대상: {target.name}, 태그: {target.tag}");
        if (target.CompareTag("Player"))
        {
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // 플레이어에게 데미지 + 넉백 방향(내 X위치) 전달
                playerHealth.TakeDamage(contactDamage, transform.position.x);
            }
        }
    }
    // =============================================================

    void Die()
    {
        if (deathEffectPrefab) Instantiate(deathEffectPrefab, transform.position + deathEffectOffset, Quaternion.identity);
        Destroy(gameObject);
    }

    IEnumerator FlashRoutine()
    {
        sr.color = new Color(1f, 0.5f, 0.5f, 1f);
        yield return new WaitForSeconds(flashDuration);
        sr.color = Color.white;
    }
}