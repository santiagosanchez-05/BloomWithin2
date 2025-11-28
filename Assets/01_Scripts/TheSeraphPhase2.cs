using System.Collections;
using UnityEngine;

public class TheSeraphPhase2 : Boss
{
    [Header("Estados internos")]
    private bool isDead = false;
    private bool isVulnerable = true;

    [Header("Componentes")]
    private Transform player;
    private Rigidbody2D rb;
    private Camera cam;
    private AudioSource audioSource;

    [Header("Stats Fase 2")]
    public float timeBetweenAttacks = 0.7f;    // Más rápido
    public float speedMultiplier = 1.5f;       // 50% más veloz
    private int attackCounter = 0;

    [Header("Teleport Shot Attack (Mejorado)")]
    public GameObject bossProjectile;
    public float projectileSpeed = 14f;
    public int teleportShots = 8;              // Ahora 8 proyectiles

    [Header("Homing Attack Mejorado")]
    public GameObject homingProjectilePrefab;
    public int homingAmount = 12;
    public float homingSpawnInterval = 0.1f;
    public float homingSpawnHeight = 3f;
    public float homingSpread = 3f;
    public float fallSpeed = 30f;

    [Header("Rain Attack")]
    public GameObject rainProjectile;
    public float rainInterval = 0.08f;
    public float rainDuration = 2f;

    [Header("Teleport Stalker (NUEVO ATAQUE)")]
    public float stalkerWarningTime = 1f;
    public float stalkerSafeDistance = 1.8f;

    [Header("Blink")]
    public float blinkDelay = 0.2f;

    [Header("Idle")]
    public float maxIdle = 3f;

    [Header("Contacto")]
    public int contactDamage = 1;
    public float contactCooldown = 1f;
    private bool canContactDamage = true;

    [Header("Audio FX")]
    public AudioClip tpSound;
    public AudioClip shootSound;
    public AudioClip slamSound;
    public AudioClip stalkerWarningSound;
    public AudioClip deathSound;

    void Start()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // VIDA 50% MÁS
        Life = Mathf.RoundToInt(Life * 1.5f);

        if (healthBar != null)
            healthBar.SetMaxHealth(Life);

        StartCoroutine(AttackPattern());
    }

    IEnumerator AttackPattern()
    {
        while (Life > 0)
        {
            yield return new WaitForSeconds(timeBetweenAttacks);
            attackCounter++;

            if (attackCounter >= 7)
            {
                attackCounter = 0;
                yield return Idle();
                continue;
            }

            int attack = Random.Range(0, 4);

            if (attack == 0) yield return TeleportShot8();
            if (attack == 1) yield return HeavenfallHoming();
            if (attack == 2) yield return RainAttack();
            if (attack == 3) yield return TeleportStalker();   // ? NUEVO ATAQUE
        }
    }

    // =========================================================
    // DESCANSO
    // =========================================================
    IEnumerator Idle()
    {
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(Random.Range(0.5f, maxIdle));
    }

    // =========================================================
    // TELETRANSPORTE RANDOM
    // =========================================================
    void TeleportRandom()
    {
        float x = Random.value < 0.5f ? 0.2f : 0.8f;
        float y = Random.Range(0.25f, 0.75f);

        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        transform.position = cam.ViewportToWorldPoint(new Vector3(x, y, dist));

        if (tpSound) audioSource.PlayOneShot(tpSound);
    }

    // =========================================================
    // ATAQUE 1: 8 DISPAROS
    // =========================================================
    IEnumerator TeleportShot8()
    {
        TeleportRandom();
        yield return new WaitForSeconds(0.15f);

        for (int i = 0; i < teleportShots; i++)
        {
            float ang = (360f / teleportShots) * i;
            Vector2 dir = Quaternion.Euler(0, 0, ang) * Vector2.right;

            GameObject p = Instantiate(bossProjectile, transform.position, Quaternion.identity);
            p.GetComponent<Rigidbody2D>().velocity = dir * projectileSpeed;
            Destroy(p, 5f);
        }

        if (shootSound) audioSource.PlayOneShot(shootSound);
    }

    // =========================================================
    // ATAQUE 2: CAÍDA + HOMING
    // =========================================================
    IEnumerator HeavenfallHoming()
    {
        isVulnerable = false;

        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 highPos = cam.ViewportToWorldPoint(new Vector3(0.5f, 1.2f, dist));

        transform.position = highPos;
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(0.3f);

        while (transform.position.y > player.position.y + 1f)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            yield return null;
        }

        rb.velocity = Vector2.zero;

        if (slamSound) audioSource.PlayOneShot(slamSound);

        for (int i = 0; i < homingAmount; i++)
        {
            float offset = Random.Range(-homingSpread, homingSpread);
            Vector3 spawnPos = new Vector3(transform.position.x + offset, transform.position.y + homingSpawnHeight, transform.position.z);

            GameObject h = Instantiate(homingProjectilePrefab, spawnPos, Quaternion.identity);
            Destroy(h, 8f);

            yield return new WaitForSeconds(homingSpawnInterval);
        }

        isVulnerable = true;
    }

    // =========================================================
    // ATAQUE 3: LLUVIA
    // =========================================================
    IEnumerator RainAttack()
    {
        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        transform.position = cam.ViewportToWorldPoint(new Vector3(0.85f, 0.7f, dist));

        float t = 0f;
        while (t < rainDuration)
        {
            float rx = Random.Range(0.1f, 0.9f);
            Vector3 spawn = cam.ViewportToWorldPoint(new Vector3(rx, 1.1f, dist));

            GameObject r = Instantiate(rainProjectile, spawn, Quaternion.identity);
            r.GetComponent<Rigidbody2D>().velocity = new Vector2(0, Random.Range(-16f, -9f));
            Destroy(r, 5f);

            t += rainInterval;
            yield return new WaitForSeconds(rainInterval);
        }
    }

    // =========================================================
    // ATAQUE 4: TELEPORT STALKER (NUEVO)
    // =========================================================
    IEnumerator TeleportStalker()
    {
        if (player == null) yield break;

        // ? ADVERTENCIA: vibra por 1 segundo
        float timer = 0f;
        Vector3 originalPos = transform.position;

        if (stalkerWarningSound)
            audioSource.PlayOneShot(stalkerWarningSound);

        while (timer < stalkerWarningTime)
        {
            float shake = Mathf.Sin(Time.time * 50f) * 0.1f;
            transform.position = originalPos + new Vector3(shake, shake, 0);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;

        // Teletransportarse al jugador, pero con distancia segura
        Vector3 target = player.position;
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 safePos = player.position - dir * stalkerSafeDistance;

        transform.position = safePos;

        if (tpSound) audioSource.PlayOneShot(tpSound);

        yield return new WaitForSeconds(0.2f);
    }

    // =========================================================
    // DAÑO POR CONTACTO
    // =========================================================
    private void OnCollisionStay2D(Collision2D c)
    {
        if (c.gameObject.CompareTag("Player") && canContactDamage)
            StartCoroutine(ContactDamage(c.gameObject));
    }

    IEnumerator ContactDamage(GameObject p)
    {
        canContactDamage = false;
        p.GetComponent<Player>()?.TakeDamage(contactDamage);
        yield return new WaitForSeconds(contactCooldown);
        canContactDamage = true;
    }

    // =========================================================
    // MUERTE
    // =========================================================
    protected void KillSeraphPhase2()
    {
        if (isDead) return;
        isDead = true;

        if (deathSound) audioSource.PlayOneShot(deathSound);

        Destroy(gameObject, 2f);
        base.Die();
    }
}
