using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainProjectile : MonoBehaviour
{
    public int damage = 1;  // daño al jugador

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Si toca al jugador ? hacer daño
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }
        }
        if (other.CompareTag("Ground"))
        {
            // Se destruye al tocar cualquier cosa
            Destroy(gameObject);
        }
    }
}