using System.Collections;
using UnityEngine;

public class TheAngel : Boss
{
    [Header("Estados internos")]
    private bool isDead = false;
    private bool isVulnerable = true;

    [Header("Componentes")]
    private Transform player;
    private Camera cam;
    private AudioSource audioSource;
    public Rigidbody2D rb;

    [Header("Control del combate")]
    public float timeBetweenAttacks = 0.9f;
    private int attackCounter = 0;

    [Header("Blink Attack")]
    public int blinkCount = 3;
    public float blinkDelay = 0.28f;
    public GameObject bossProjectile;
    public float projectileSpeed = 10f;
    public int blinkProjectiles = 6;

    [Header("Chase Attack")]
    public float chargeDelay = 0.6f;
    public float chaseSpeed = 18f;

    [Header("Rain Attack")]
    public GameObject rainProjectile;
    public float rainInterval = 0.1f;
    public float rainDuration = 1.7f;

    [Header("Idle Config")]
    public float maxIdleTime = 5f;

    [Header("Contacto")]
    public int contactDamage = 1;
    public float contactCooldown = 1f;
    private bool canContactDamage = true;

    [Header("Audio")]
    public AudioClip tpSound;
    public AudioClip shootSound;
    public AudioClip deathSound;

    void Start()
    {
        cam = Camera.main;
        audioSource = GetComponent<AudioSource>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (healthBar != null)
            healthBar.SetMaxHealth(Life);

        StartCoroutine(AttackPattern());
    }

    IEnumerator AttackPattern()
    {
        yield return EntryState();

        while (Life > 0)
        {
            yield return new WaitForSeconds(timeBetweenAttacks);

            attackCounter++;

            if (attackCounter >= 5)
            {
                attackCounter = 0;
                yield return PauseCenter();
                continue;
            }

            int attack = Random.Range(0, 3);

            if (attack == 0) yield return BlinkAttack();
            if (attack == 1) yield return ChaseAttack();
            if (attack == 2) yield return RainAttack();
        }
    }

    IEnumerator EntryState()
    {
        isVulnerable = false;

        Vector3 start = new Vector3(transform.position.x, transform.position.y + 6f, transform.position.z);
        Vector3 end = transform.position;

        float t = 0;

        while (t < 1f)
        {
            transform.position = Vector3.Lerp(start, end, t);
            t += Time.deltaTime / 2f;
            yield return null;
        }

        isVulnerable = true;
    }

    // =========================================================
    // PAUSA (limitado a 5 segundos MÁXIMO)
    // =========================================================
    IEnumerator PauseCenter()
    {
        isVulnerable = true;

        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        transform.position = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.42f, dist));

        rb.velocity = Vector2.zero;

        // ?? nunca más de 5 segundos
        float idle = Random.Range(1f, maxIdleTime);
        yield return new WaitForSeconds(idle);
    }

    // =========================================================
    // TELETRANSPORTE
    // =========================================================
    void TeleportEdge()
    {
        float x = Random.value < 0.5f ? 0.15f : 0.85f;
        float y = Random.Range(0.25f, 0.75f);

        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        transform.position = cam.ViewportToWorldPoint(new Vector3(x, y, dist));

        if (tpSound) audioSource.PlayOneShot(tpSound);
    }

    IEnumerator BlinkAttack()
    {
        for (int i = 0; i < blinkCount; i++)
        {
            isVulnerable = false;

            TeleportEdge();
            ShootCircle();

            yield return new WaitForSeconds(blinkDelay);

            isVulnerable = true;
        }
    }

    void ShootCircle()
    {
        for (int i = 0; i < blinkProjectiles; i++)
        {
            float angle = (360f / blinkProjectiles) * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;

            GameObject p = Instantiate(bossProjectile, transform.position, Quaternion.identity);
            p.GetComponent<Rigidbody2D>().velocity = dir * projectileSpeed;

            Destroy(p, 5f);
        }

        if (shootSound) audioSource.PlayOneShot(shootSound);
    }

    IEnumerator ChaseAttack()
    {
        if (player == null) yield break;

        isVulnerable = false;

        TeleportEdge();
        yield return new WaitForSeconds(chargeDelay);

        for (int i = 0; i < 60; i++)
        {
            if (player == null) break;

            Vector2 dir = (player.position - transform.position).normalized;
            transform.position += (Vector3)dir * chaseSpeed * Time.deltaTime;

            yield return null;
        }

        isVulnerable = true;
    }

    // =========================================================
    // LLUVIA — YA NO SE QUEDA QUIETO AL FINAL
    // =========================================================
    IEnumerator RainAttack()
    {
        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        transform.position = cam.ViewportToWorldPoint(new Vector3(0.85f, 0.60f, dist));

        rb.velocity = Vector2.zero;

        // ?? quieto máximo 5 segundos antes
        float idle = Random.Range(1f, maxIdleTime);
        yield return new WaitForSeconds(idle);

        float t = 0;
        while (t < rainDuration)
        {
            float x = Random.Range(0.1f, 0.9f);
            Vector3 spawn = cam.ViewportToWorldPoint(new Vector3(x, 1.1f, dist));

            GameObject r = Instantiate(rainProjectile, spawn, Quaternion.identity);
            r.GetComponent<Rigidbody2D>().velocity = new Vector2(0, Random.Range(-10f, -6f));

            Destroy(r, 5f);

            t += rainInterval;
            yield return new WaitForSeconds(rainInterval);
        }

        // ?? inmediatamente después de la lluvia ? TELETRANSPORTA Y SIGUE
        TeleportEdge();
        rb.velocity = Vector2.zero;
    }

    // =========================================================
    // CONTACTO
    // =========================================================
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && canContactDamage)
        {
            StartCoroutine(ContactDamage(collision.gameObject));
        }
    }

    IEnumerator ContactDamage(GameObject playerObj)
    {
        canContactDamage = false;

        Player p = playerObj.GetComponent<Player>();
        if (p != null)
            p.TakeDamage(contactDamage);

        yield return new WaitForSeconds(contactCooldown);
        canContactDamage = true;
    }

    protected void KillAngel()
    {
        if (isDead) return;

        isDead = true;

        if (deathSound) audioSource.PlayOneShot(deathSound);

        StopAllCoroutines();
        Destroy(gameObject, 2f);

        base.Die();
    }
}
