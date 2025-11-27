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
    public float timeBetweenAttacks = 0.9f; // ?? Más agresivo
    private int attackCounter = 0;

    [Header("Blink Attack")]
    public int blinkCount = 3;
    public float blinkDelay = 0.28f; // ?? Más rápido
    public GameObject bossProjectile;
    public float projectileSpeed = 10f; // ?? Mucho más agresivo
    public int blinkProjectiles = 6;

    [Header("Chase Attack")]
    public float chargeDelay = 0.6f; // ?? menor delay
    public float chaseSpeed = 18f;   // ?? más velocidad

    [Header("Rain Attack")]
    public GameObject rainProjectile;
    public float rainInterval = 0.1f;   // ?? más lluvia
    public float rainDuration = 1.7f;   // ?? más corto pero intenso
    public float rainRest = 2f;         // ?? menos descanso

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

    // =========================================================
    // LOOP PRINCIPAL
    // =========================================================
    IEnumerator AttackPattern()
    {
        yield return EntryState();

        while (Life > 0)
        {
            yield return new WaitForSeconds(timeBetweenAttacks);

            attackCounter++;

            // ?? descanso cada 5 ataques
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

    // =========================================================
    // ENTRADA
    // =========================================================
    IEnumerator EntryState()
    {
        isVulnerable = false;

        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 start = new Vector3(transform.position.x, transform.position.y + 6f, transform.position.z);
        Vector3 end = transform.position;
        float t = 0;

        while (t < 1f)
        {
            transform.position = Vector3.Lerp(start, end, t);
            t += Time.deltaTime * 0.8f;
            yield return null;
        }

        isVulnerable = true;
    }

    // =========================================================
    // PAUSA CENTRAL
    // =========================================================
    IEnumerator PauseCenter()
    {
        isVulnerable = true;

        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        transform.position = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.42f, dist));

        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(1f);
    }

    // =========================================================
    // TELETRANSPORTE — FIX REAL
    // =========================================================
    void TeleportEdge()
    {
        float x = UnityEngine.Random.value < 0.5f ? 0.15f : 0.85f;
        float y = UnityEngine.Random.Range(0.25f, 0.75f);

        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        transform.position = cam.ViewportToWorldPoint(new Vector3(x, y, dist));

        if (tpSound) audioSource.PlayOneShot(tpSound);
    }

    // =========================================================
    // BLINK ATTACK
    // =========================================================
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

    // =========================================================
    // CHASE
    // =========================================================
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
    // LLUVIA
    // =========================================================
    IEnumerator RainAttack()
    {
        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        transform.position = cam.ViewportToWorldPoint(new Vector3(0.85f, 0.60f, dist));
        rb.velocity = Vector2.zero;

        float t = 0;
        while (t < rainDuration)
        {
            float x = UnityEngine.Random.Range(0.1f, 0.9f);
            Vector3 spawn = cam.ViewportToWorldPoint(new Vector3(x, 1.1f, dist));

            GameObject r = Instantiate(rainProjectile, spawn, Quaternion.identity);
            r.GetComponent<Rigidbody2D>().velocity = new Vector2(0, UnityEngine.Random.Range(-10f, -6f));

            Destroy(r, 5f);

            t += rainInterval;
            yield return new WaitForSeconds(rainInterval);
        }

        yield return new WaitForSeconds(rainRest);
    }

    // =========================================================
    // MUERTE
    // =========================================================
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
