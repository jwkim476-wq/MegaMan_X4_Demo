using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("설정")]
    public int maxHealth = 3;
    public int contactDamage = 2;
    public GameObject deathEffectPrefab;
    public float flashDuration = 0.1f;

    public Vector3 deathEffectOffset = new Vector3(0, 0.5f, 0);

    // 현재 상태 확인용 (AI가 가져다 씀)
    public bool IsDead { get; private set; }

    int currentHealth;
    SpriteRenderer sr;
    Collider2D col; // 콜라이더 제어용

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>(); // 콜라이더 가져오기
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
        else StartCoroutine(FlashRoutine());
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsDead) return; // 죽었으면 공격 안 함
        TryDamagePlayer(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (IsDead) return;
        TryDamagePlayer(collision.gameObject);
    }

    void TryDamagePlayer(GameObject target)
    {
        if (target.CompareTag("Player"))
        {
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(contactDamage, transform.position.x);
            }
        }
    }

    void Die()
    {
        IsDead = true;

        // 이펙트 생성
        if (deathEffectPrefab)
        {
            Instantiate(deathEffectPrefab, transform.position + deathEffectOffset, Quaternion.identity);
        }

        // 삭제(Destroy) 대신 숨기기
        sr.enabled = false;   // 그림 끄기
        col.enabled = false;  // 충돌 끄기
    }

    // 되살리기 함수 (AI가 호출)
    public void Revive()
    {
        IsDead = false;
        currentHealth = maxHealth;

        sr.enabled = true;    // 그림 켜기
        col.enabled = true;   // 충돌 켜기
        sr.color = Color.white; // 색깔 초기화
    }

    IEnumerator FlashRoutine()
    {
        sr.color = new Color(1f, 0.5f, 0.5f, 1f);
        yield return new WaitForSeconds(flashDuration);
        sr.color = Color.white;
    }
}