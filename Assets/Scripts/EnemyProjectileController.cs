using UnityEngine;
using System.Collections;

public class EnemyProjectileController : MonoBehaviour {
    public GameObject enemyDeathEffect;
	private AudioClip projectileHits;
	private Vector3 positie;

    // Use this for initialization
    void Start()
    {
        // EnemyProjectile layer should ignore itself, not collide
        // TODO: Make sure it doesn't collide with player projectile
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("EnemyProjectile"), LayerMask.NameToLayer("EnemyProjectile"));
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("EnemyProjectile"));

		projectileHits = gameObject.GetComponent<AudioSource> ().clip;
		positie = new Vector3(0.0f, 0.0f, 0.0f);
    }

    void onTriggerEnter2D(Collision2D collided)
    {
        //If it collides with anything, destroy projectile, if the object dies too fast, error, that's why, check
        if (this.gameObject)
        {
            AudioSource.PlayClipAtPoint(projectileHits, positie, 0.5f);
        }

        //If collides with player
        if (collided.gameObject.tag == "Player" || collided.gameObject.tag == "Friendly")
        {
            Instantiate(enemyDeathEffect, collided.transform.position, collided.transform.rotation);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collided)
    {
        //If it collides with anything, destroy projectile, if the object dies too fast, error, that's why, check
        if (this.gameObject)
        {
            AudioSource.PlayClipAtPoint(projectileHits, positie, 0.5f);
        }

        //If collides with player
        if (collided.gameObject.tag == "Player" || collided.gameObject.tag == "Friendly")
        {
            Instantiate(enemyDeathEffect, collided.transform.position, collided.transform.rotation);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}
