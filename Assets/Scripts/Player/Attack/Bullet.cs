using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public float lifeTime = 2f;
    public int damage = 1;
    public GameObject hitEffect;
    public bool isPlayerBullet = true; // 플레이어 총알인지 체크

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.velocity = transform.right * speed;
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 발사한 본인(플레이어)과는 충돌 안 함
        if (isPlayerBullet && other.CompareTag("Player")) return;
        if (!isPlayerBullet && other.CompareTag("Enemy")) return;
        if (other.isTrigger) return; // 트리거끼리 충돌 무시

        // 1. 적을 맞췄을 때 (플레이어 총알인 경우)
        if (isPlayerBullet && other.CompareTag("Enemy"))
        {
            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            DestroyBullet();
        }
        // 2. 플레이어를 맞췄을 때
        else if (!isPlayerBullet && other.CompareTag("Player"))
        {
            PlayerHealth player = other.GetComponent<PlayerHealth>();
            if (player != null)
            {
                // 총알의 X 위치를 넘겨줘서 넉백 방향 계산
                player.TakeDamage(damage, transform.position.x);
            }
            DestroyBullet();
        }
        // 3. 벽에 맞았을 때
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            DestroyBullet();
        }
    }

    void DestroyBullet()
    {
        if (hitEffect) Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}