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
                ShootHandler shoot = col.gameObject.GetComponent<ShootHandler>();
                if (shoot != null)
                {
                    int readyArrows = shoot.GetCurrentAmmo();
                    int reserveArrows = shoot.GetTotalAmmo();
                    int combinedTotal = readyArrows + reserveArrows;

                    // DEBUG LOG: Tracking exact inventory values on touch
                    Debug.Log($"[ARROW DEBUG] Touched Player! Ready: {readyArrows} | Reserves: {reserveArrows} | Combined: {combinedTotal} (Need < 10 to pick up)");

                    if (combinedTotal < 10)
                    {
                        Debug.Log("[ARROW DEBUG] Space available! Consuming arrow item.");
                        shoot.AddAmmo(1);
                        Destroy(gameObject);
                    }
                    else
                    {
                        Debug.Log("[ARROW DEBUG] Condition FAILED: Inventory is entirely full. Leaving on ground.");
                    }
                }
                else
                {
                    Debug.LogError("[ARROW DEBUG] CRITICAL: Touched an object tagged 'Player', but no ShootHandler component was found on it!");
                }
                break;

            case "Enemy":
                if (GetComponent<SpawnParticleEvent>() != null)
                {
                    GetComponent<SpawnParticleEvent>().SpawnParticle();
                }

                col.gameObject.GetComponent<HealthHandler>().DamageHandler("Arrow", damage);
                Destroy(gameObject);
                break;

            case "NPC":
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
