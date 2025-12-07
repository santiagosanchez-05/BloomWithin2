using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TheSeraph : Boss
{
    [Header("Estados internos")]
    private bool isDead = false;
    private bool isVulnerable = true;

    [Header("Componentes")]
    private Transform player;
    private Camera cam;
    private AudioSource audioSource;
    private Animator anim;              // ✅ ANIMATOR
    public Rigidbody2D rb;

    [Header("Control del combate")]
    public float timeBetweenAttacks = 1f;
    private int attackCounter = 0;

    [Header("Teleport Shot Attack")]
    public GameObject bossProjectile;
    public float projectileSpeed = 10f;
    public int teleportShots = 4;

    [Header("Homing Landing Attack")]
    public GameObject homingProjectilePrefab;
    public int homingAmount = 8;
    public float homingSpawnInterval = 0.15f;
    public float fallSpeed = 25f;
    public float homingSpawnHeight = 2f;
    public float homingSpreadX = 2f;

    [Header("Rain Attack")]
    public GameObject rainProjectile;
    public float rainInterval = 0.1f;
    public float rainDuration = 1.8f;
    public float rainRest = 1f;

    [Header("Blink Attack")]
    public float blinkDelay = 0.3f;

    [Header("Idle")]
    public float maxIdleTime = 5f;

    [Header("Contacto")]
    public int contactDamage = 1;
    public float contactCooldown = 1f;
    private bool canContactDamage = true;

    [Header("Audio")]
    public AudioClip tpSound;
    public AudioClip shootSound;
    public AudioClip fallSound;
    public AudioClip deathSound;

    // =========================================================

    void Start()
    {
        cam = Camera.main;
        audioSource = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();               // ✅
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (healthBar != null)
            healthBar.SetMaxHealth(Life);

        anim.SetTrigger("Entry");                      // ✅ ANIM DE ENTRADA
        StartCoroutine(AttackPattern());
    }

    // =========================================================

    IEnumerator AttackPattern()
    {
        yield return EntryState();

        while (Life > 0)
        {
            yield return new WaitForSeconds(timeBetweenAttacks);
            attackCounter++;

            if (attackCounter >= 6)
            {
                attackCounter = 0;
                yield return PauseIdle();
                continue;
            }

            int attack = Random.Range(0, 4);

            if (attack == 0) yield return Teleport4Shot();
            if (attack == 1) yield return HeavenfallHoming();
            if (attack == 2) yield return BlinkAttack();
            if (attack == 3) yield return RainAttack();
        }
    }

    // =========================================================

    IEnumerator EntryState()
    {
        isVulnerable = false;

        Vector3 start = transform.position + Vector3.up * 7f;
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

    IEnumerator PauseIdle()
    {
        isVulnerable = true;
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(Random.Range(1f, maxIdleTime));
    }

    // =========================================================
    // TELEPORT
    // =========================================================
    void TeleportRandom()
    {
        anim.SetTrigger("Teleport");        // ✅ ANIM TELEPORT

        float x = Random.value < 0.5f ? 0.15f : 0.85f;
        float y = Random.Range(0.2f, 0.8f);

        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        transform.position = cam.ViewportToWorldPoint(new Vector3(x, y, dist));

        if (tpSound) audioSource.PlayOneShot(tpSound);
    }

    // =========================================================
    // TELEPORT + DISPARO
    // =========================================================
    IEnumerator Teleport4Shot()
    {
        TeleportRandom();
        yield return new WaitForSeconds(0.25f);

        anim.SetTrigger("Shoot");           // ✅ ANIM DISPARO

        for (int i = 0; i < teleportShots; i++)
        {
            float angle = (360f / teleportShots) * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;

            GameObject p = Instantiate(bossProjectile, transform.position, Quaternion.identity);
            p.GetComponent<Rigidbody2D>().velocity = dir * projectileSpeed;

            Destroy(p, 5f);
        }

        if (shootSound) audioSource.PlayOneShot(shootSound);
        yield return new WaitForSeconds(blinkDelay);
    }

    // =========================================================
    // HEAVENFALL HOMING
    // =========================================================
    IEnumerator HeavenfallHoming()
    {
        anim.SetTrigger("Heavenfall");      // ✅ ANIM CAÍDA
        isVulnerable = false;

        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);

        Vector3 startPos = cam.ViewportToWorldPoint(new Vector3(0.5f, 1.15f, dist));
        transform.position = startPos;

        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(0.4f);

        while (transform.position.y > player.position.y + 1f)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            yield return null;
        }

        if (fallSound) audioSource.PlayOneShot(fallSound);

        for (int i = 0; i < homingAmount; i++)
        {
            float offsetX = Random.Range(-homingSpreadX, homingSpreadX);
            Vector3 spawnPos = new Vector3(
                transform.position.x + offsetX,
                transform.position.y + homingSpawnHeight,
                transform.position.z
            );

            GameObject h = Instantiate(homingProjectilePrefab, spawnPos, Quaternion.identity);
            Destroy(h, 8f);

            yield return new WaitForSeconds(homingSpawnInterval);
        }

        isVulnerable = true;
    }

    // =========================================================
    // RAIN ATTACK
    // =========================================================
    IEnumerator RainAttack()
    {
        anim.SetTrigger("Rain");            // ✅ ANIM LLUVIA

        float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);

        rb.velocity = Vector2.zero;
        transform.position = cam.ViewportToWorldPoint(new Vector3(0.85f, 0.7f, dist));

        float t = 0;
        while (t < rainDuration)
        {
            float rx = Random.Range(0.1f, 0.9f);
            Vector3 spawn = cam.ViewportToWorldPoint(new Vector3(rx, 1.1f, dist));

            GameObject r = Instantiate(rainProjectile, spawn, Quaternion.identity);
            r.GetComponent<Rigidbody2D>().velocity = new Vector2(0, Random.Range(-12f, -7f));
            Destroy(r, 5f);

            t += rainInterval;
            yield return new WaitForSeconds(rainInterval);
        }

        yield return new WaitForSeconds(rainRest);
    }

    // =========================================================
    // BLINK
    // =========================================================
    IEnumerator BlinkAttack()
    {
        anim.SetTrigger("Blink");           // ✅ ANIM BLINK
        TeleportRandom();
        yield return new WaitForSeconds(blinkDelay);
    }

    // =========================================================
    // DAÑO POR CONTACTO
    // =========================================================
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && canContactDamage)
            StartCoroutine(ContactDamage(collision.gameObject));
    }

    IEnumerator ContactDamage(GameObject playerObj)
    {
        canContactDamage = false;

        Player p = playerObj.GetComponent<Player>();
        p?.TakeDamage(contactDamage);

        yield return new WaitForSeconds(contactCooldown);
        canContactDamage = true;
    }

    // =========================================================
    // MUERTE
    // =========================================================
    protected override void Die()
    {
        KillSeraph();
    }
    protected void KillSeraph()
    {
        if (isDead) return;
        isDead = true;

        if (deathSound) audioSource.PlayOneShot(deathSound);

        anim.SetTrigger("Death");           // ✅ ANIM MUERTE

        StopAllCoroutines();

        SceneManager.LoadScene("DerrotaThePaleMatron");

        base.Die();
    }

    // =========================================================
    // DAÑO NORMAL (CUANDO LE PEGAN)
    // =========================================================
    //public override void TakeDamage(int damage)
    //{
    //    base.TakeDamage(damage);

    //    if (anim != null)
    //        anim.SetTrigger("Hurt");        // ✅ ANIM DAÑO
    //}
}
