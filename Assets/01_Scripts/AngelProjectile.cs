using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngelProjectile : MonoBehaviour
{
    public int damage = 1; // daño que hace al jefe

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Daño al jefe
        if (other.CompareTag("Player"))
        {
            Player boss = other.GetComponent<Player>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
            }

            Destroy(gameObject); // destruye la bala
        }

        // Si choca con el suelo u otro objeto
        if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
