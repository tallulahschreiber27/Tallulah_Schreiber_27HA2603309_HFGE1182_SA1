using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float damage;

    private void OnCollisionEnter(Collision col)
    {
        switch (col.gameObject.tag)
        {
            case "Player":
                col.gameObject.GetComponent<ShootHandler>().AddAmmo(1);
                Destroy(gameObject);
                break;
            case "Enemy":
                // 1. Find the SpawnParticleEvent component on this arrow and trigger it!
                if (GetComponent<SpawnParticleEvent>() != null)
                {
                    GetComponent<SpawnParticleEvent>().SpawnParticle();
                }

                // 2. Original damage and cleanup logic
                col.gameObject.GetComponent<HealthHandler>().DamageHandler("Arrow", damage);
                Destroy(gameObject);
                break;
            case "NPC":
                // Optional: You can trigger particles here too if you want NPC feedback!
                if (GetComponent<SpawnParticleEvent>() != null)
                {
                    GetComponent<SpawnParticleEvent>().SpawnParticle();
                }

                col.gameObject.GetComponent<HealthHandler>().DamageHandler("Arrow", damage);
                Destroy(gameObject);
                break;
            case "Crate":
                col.gameObject.GetComponent<HealthHandler>().DamageHandler("Arrow", damage);
                Destroy(gameObject);
                break;
            default:
                break;
        }
    }
}
