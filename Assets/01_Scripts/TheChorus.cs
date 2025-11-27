using System.Collections;
using UnityEngine;

public class TheChorus : Boss
{
    [Header("Movimiento")]
    public float moveSpeed = 3f;
    public float jumpForce = 8f;
    public float chaseRange = 10f;
    public Rigidbody2D rb;
    private Transform player;
    private bool canAttack = true;
    private bool canMove = true;
    private bool facingRight = true;

    [Header("Daño por contacto")]
    public int contactDamage = 1;
    public float knockbackForce = 6f;
    private bool canDealContactDamage = true;
    public float contactCooldown = 1.5f;

    [Header("Ataques")]
    public GameObject bodyProjectilePrefab;
    public Transform shootPoint;
    public GameObject debrisPrefab;
    public GameObject roarWavePrefab;
    public GameObject impactEffect;
    public float timeBetweenAttacks = 2f;

    private bool isGrounded;
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Animación")]
    public Animator animator;

    [Header("Audio")]
    public AudioClip BossMusic;
    public AudioClip jumpSound;
    public AudioClip projectileSound;
    public AudioClip debrisSound;
    public AudioClip roarSound;
    public AudioClip leapSmashSound;
    private AudioSource audioSource;

    [Header("Muerte / Puerta")]
    public GameObject deathEffectPrefab;
    public GameObject Door;
    private bool isDead = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            Debug.LogWarning("⚠️ TheChorus: No se encontró el objeto con tag 'Player'.");

        if (healthBar != null)
            healthBar.SetMaxHealth(Life);

        if (animator == null)
            animator = GetComponent<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            Debug.LogWarning("⚠️ TheChorus: No hay componente AudioSource en este GameObject.");

        // 🎵 Música del boss
        if (BossMusic != null && audioSource != null)
        {
            audioSource.clip = BossMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

        StartCoroutine(AttackPattern());
    }

    void FixedUpdate()
    {
        if (Life <= 0 || !canMove) return;
        MovementLogic();
    }

    void MovementLogic()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance < chaseRange)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(dir.x * moveSpeed, rb.velocity.y);

            animator.SetBool("isMoving", true);

            if (dir.x > 0 && !facingRight) Flip();
            else if (dir.x < 0 && facingRight) Flip();
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            animator.SetBool("isMoving", false);
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    IEnumerator AttackPattern()
    {
        while (Life > 0)
        {
            yield return new WaitForSeconds(timeBetweenAttacks);
            if (!canAttack) continue;

            canAttack = false;
            canMove = false;

            int randomAttack = Random.Range(0, 5);

            switch (randomAttack)
            {
                case 0: yield return StartCoroutine(JumpAttack()); break;
                case 1: yield return StartCoroutine(BodyProjectileAttack()); break;
                case 2: yield return StartCoroutine(DebrisAttack()); break;
                case 3: yield return StartCoroutine(RoarAttack()); break;
                case 4: yield return StartCoroutine(LeapSmashAttack()); break;
            }

            canMove = true;
            canAttack = true;
        }
    }

    IEnumerator LeapSmashAttack()
    {
        animator.SetTrigger("LeapSmash");
        SafePlay(leapSmashSound);

        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(0.4f);

        Vector2 targetDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 jumpVector = targetDir + Vector2.up * 1.2f;
        rb.velocity = jumpVector * jumpForce * 1.2f;

        yield return new WaitUntil(() => isGrounded);

        if (impactEffect) Instantiate(impactEffect, transform.position, Quaternion.identity);
        if (roarWavePrefab) Instantiate(roarWavePrefab, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
                hit.GetComponent<Player>()?.TakeDamage(1);
        }

        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator JumpAttack()
    {
        animator.SetTrigger("Jump");
        SafePlay(jumpSound);

        yield return new WaitForSeconds(0.2f);
        Vector2 jumpDir = ((Vector2)player.position - (Vector2)transform.position).normalized + Vector2.up * 0.5f;
        rb.velocity = jumpDir * jumpForce;
        yield return new WaitForSeconds(1f);
    }

    IEnumerator BodyProjectileAttack()
    {
        animator.SetTrigger("Shoot");
        yield return new WaitForSeconds(0.3f);

        if (bodyProjectilePrefab == null || shootPoint == null)
        {
            Debug.LogWarning("⚠️ BodyProjectileAttack: Falta prefab o shootPoint.");
            yield break;
        }

        for (int i = 0; i < 4; i++)
        {
            GameObject proj = Instantiate(bodyProjectilePrefab, shootPoint.position, Quaternion.identity);
            Vector2 dir = (player.position - shootPoint.position).normalized;
            Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
            if (rbProj != null)
                rbProj.velocity = dir * 8f;

            SafePlay(projectileSound);
            yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator DebrisAttack()
    {
        animator.SetTrigger("Debris");
        SafePlay(debrisSound);

        rb.velocity = Vector2.up * (jumpForce * 1.5f);
        yield return new WaitForSeconds(0.7f);

        if (debrisPrefab == null)
        {
            Debug.LogWarning("⚠️ Falta asignar debrisPrefab.");
            yield break;
        }

        for (int i = 0; i < 6; i++)
        {
            float randX = Random.Range(-8f, 8f);
            Vector2 spawnPos = new Vector2(transform.position.x + randX, transform.position.y + 10f);
            Instantiate(debrisPrefab, spawnPos, Quaternion.identity);
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator RoarAttack()
    {
        animator.SetTrigger("Roar");
        SafePlay(roarSound);

        yield return new WaitForSeconds(0.5f);
        if (roarWavePrefab) Instantiate(roarWavePrefab, transform.position, Quaternion.identity);
    }

    void SafePlay(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
        else if (clip == null)
            Debug.LogWarning("⚠️ Se intentó reproducir un clip de audio no asignado.");
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && canDealContactDamage)
            StartCoroutine(ContactDamage(collision.gameObject));
    }

    IEnumerator ContactDamage(GameObject playerObj)
    {
        canDealContactDamage = false;
        Player p = playerObj.GetComponent<Player>();
        if (p != null)
        {
            p.TakeDamage(contactDamage);
            Vector2 dir = (p.transform.position - transform.position).normalized;
            Rigidbody2D prb = p.GetComponent<Rigidbody2D>();
            if (prb != null)
                prb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(contactCooldown);
        canDealContactDamage = true;
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("💀 The Chorus ha sido derrotado.");

        canMove = false;
        canAttack = false;
        rb.velocity = Vector2.zero;

        animator?.SetTrigger("Die");
        audioSource?.Stop();
        if (healthBar?.gameObject != null)
            healthBar.gameObject.SetActive(false);

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(2.5f);

        if (Door != null)
        {
            Door.SetActive(true);
            Debug.Log("🚪 Puerta activada tras derrotar a The Chorus");
        }

        if (deathEffectPrefab)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        base.Die();
    }
}