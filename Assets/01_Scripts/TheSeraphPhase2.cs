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
    private Animator animator;

    [Header("Stats Fase 2")]
    public float timeBetweenAttacks = 0.7f;
    public float speedMultiplier = 1.5f;
    private int attackCounter = 0;

    [Header("Teleport Shot Attack")]
    public GameObject bossProjectile;
    public float projectileSpeed = 14f;
    public int teleportShots = 8;

    [Header("Homing Attack")]
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

    [Header("Teleport Stalker")]
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

    // =========================================================
    // START
    // =========================================================
    void Start()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        Life = Mathf.RoundToInt(Life * 1.5f);

        if (healthBar != null)
            healthBar.SetMaxHealth(Life);

        StartCoroutine(AttackPattern());
    }

    // =========================================================
    // PATRÓN DE ATAQUES
    // =========================================================
    IEnumerator AttackPattern()
    {
        while (Life > 0)
        {
            yield return new WaitForSeconds(timeBetweenAttacks);
            attackCounter++;

            if (attackCounter >= 7)
            {
                attackCounter = 0;
                animator.SetTrigger("Idle");
                yield return Idle();
                continue;
            }

            int attack = Random.Range(0, 4);

            if (attack == 0) yield return TeleportShot8();
            if (attack == 1) yield return HeavenfallHoming();
            if (attack == 2) yield return RainAttack();
            if (attack == 3) yield return TeleportStalker();
        }
    }

    // =========================================================
    // IDLE
    // =========================================================
    IEnumerator Idle()
    {
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(Random.Range(0.5f, maxIdle));
    }

    // =========================================================
    // TELEPORT RANDOM
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
    // ATAQUE 1 - TELEPORT SHOT
    // =========================================================
    IEnumerator TeleportShot8()
    {
        animator.SetTrigger("TeleportShot");
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
    // ATAQUE 2 - HEAVENFALL + HOMING
    // =========================================================
    IEnumerator HeavenfallHoming()
    {
        animator.SetTrigger("Heavenfall");
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
            Vector3 spawnPos = new Vector3(
                transform.position.x + offset,
                transform.position.y + homingSpawnHeight,
                transform.position.z
            );

            GameObject h = Instantiate(homingProjectilePrefab, spawnPos, Quaternion.identity);
            Destroy(h, 8f);

            yield return new WaitForSeconds(homingSpawnInterval);
        }

        isVulnerable = true;
        animator.SetTrigger("Idle");
    }

    // =========================================================
    // ATAQUE 3 - RAIN
    // =========================================================
    IEnumerator RainAttack()
    {
        animator.SetTrigger("Rain");

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

        animator.SetTrigger("Idle");
    }

    // =========================================================
    // ATAQUE 4 - TELEPORT STALKER
    // =========================================================
    IEnumerator TeleportStalker()
    {
        if (player == null) yield break;

        animator.SetTrigger("Stalker");

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

        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 safePos = player.position - dir * stalkerSafeDistance;
        transform.position = safePos;

        if (tpSound) audioSource.PlayOneShot(tpSound);

        yield return new WaitForSeconds(0.2f);
        animator.SetTrigger("Idle");
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
    // DAÑO
    // =========================================================
    //public override void TakeDamage(int dmg)
    //{
    //    if (!isVulnerable || isDead) return;

    //    base.TakeDamage(dmg);

    //    if (animator != null)
    //        animator.SetTrigger("Hurt");

    //    if (Life <= 0)
    //        KillSeraphPhase2();
    //}

    // =========================================================
    // MUERTE
    // =========================================================
    protected void KillSeraphPhase2()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null)
            animator.SetTrigger("Death");

        if (deathSound)
            audioSource.PlayOneShot(deathSound);

        Destroy(gameObject, 2f);
        base.Die();
    }
}
