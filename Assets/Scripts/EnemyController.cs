using UnityEngine;
using System.Collections;

/*
 * List of selectable enemy types
 */
public enum EnemyType
{
	stationary,
	patrol
}

/*
 * List of posible states an enemy can be in
 */
public enum EnemyState
{
	idle,
	waitThenAttack
}

public class EnemyController : MonoBehaviour
{
    public EnemyType type;
    private EnemyState _state = EnemyState.idle;// Local variable to represent our state
    private Animator anim;

    // Spawn friendly
    public GameObject friendlyPatrol;
    public GameObject friendlyStationary;
    private GameObject spawn;

    // Message
    public string message = "";                 // The message the friendly will use after this enemy is defeated

    // Health
    public float currentHealth = 2f;
    public float invincibilityDuration = 2f;    // length of damage cooldown
    private bool onCoolDown = false;            // Cooldown active or not

    // Patrol
    public float walkSpeed = 1f;                // Amount of velocity
    private bool walkingRight;                  // Simple check to see in what direction the enemy is moving, important for facing.
    public float collideDistance = 0.5f;        // Distance from enemy to check for a wall.
    public bool edgeDetection = true;           // If checked, it will try to detect the edge of a platform
    private bool collidingWithWall = false;     // If true, it touched a wall and should flip.
    private bool collidingWithGround = true;    // If true, it is not about to fall off an edge

    // Target (usually the player)
    public string targetLayer = "Player";       // TODO: Make this a list, for players and friendly NPC's
    private GameObject target;

    // Firing Projectiles
    public Transform firePoint;                 // Point from which the enemy fires
    public GameObject projectilePrefab;         // Projectile
    public float projectileSpeed = 5;           // Speed of the projectile
    public float projectileLifeTime = 2;        // How long the projectile exists before selfdestructing
    private bool delayCoroutineStarted = false;
    public float fireDelay = 3;                 // Time between shots

    // Shoot
    private GameObject projectile;              // Selected projectile, should handle selfdestruct and damage
    private bool playerIsLeft;                  // Simple check to see if the player is left to the enemy, important for facing.
    private bool facingLeft = true;             // For determining which way the player is currently facing.
    private bool readyToFire = false;

    // Blinded
    private bool isBlinded = false;
    public float blindedDelay = 3;

    // Spot
    public float spotRadius = 3;                // Radius in which a player can be spotted
    public bool drawSpotRadiusGismo = true;     // Visual aid in determening if the spot radius
    private Collider2D[] collisionObjects;
    private bool playerSpotted = false;         // Has the enemy spotted the player?

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        switch (_state)
        {
            case EnemyState.idle:
                //delayCoroutineStarted = false;
                if (type == EnemyType.stationary)
                {
                    Idle();
                }
                else if (type == EnemyType.patrol)
                {
                    Patrol();
                }
                break;
            case EnemyState.waitThenAttack:
                WaitThenAttack();
                // To ensure the coroutine is only fired once!
                if (!delayCoroutineStarted)
                    StartCoroutine(FireDelay());
                break;
        }

        if (currentHealth <= 0)
        {
            EnemyDeath();
        }
    }

    /**
     * Idle state
     *
     * In this state, the enemy will wait to spot a player, and then it will go to its attack state.
     * Patroling enemys will resume to patrol after it shot at the player, as the attack state
     * will reset the timer. The first time the patroling enemy spots an enemy, the timer will
     * already have passed and it will immediately go into the attack state.
     */
    private void Idle()
    {
        // Will set 'playerSpotted' to true if spotted
        IsTargetInRange();
        if (playerSpotted)
        {
            _state = EnemyState.waitThenAttack;
        }
    }

    /**
     * Patrol script for enemy,
     * will walk untill the collidingWithWall linecast hits a collider, then walk the other way
     * or (if checked) will detect if the enemy is to hit the edge of a platform
     */
    private void Patrol()
    {
        anim.SetFloat("speed", walkSpeed);
        GetComponent<Rigidbody2D>().velocity = new Vector2(walkSpeed, GetComponent<Rigidbody2D>().velocity.y);

        FaceDirectionOfWalking();

        collidingWithWall = Physics2D.Linecast(
            new Vector2((this.transform.position.x + collideDistance), (this.transform.position.y - (GetComponent<SpriteRenderer>().bounds.size.y / 4))),
            new Vector2((this.transform.position.x + collideDistance), (this.transform.position.y + (GetComponent<SpriteRenderer>().bounds.size.y / 2))),
            ~(
                (1 << LayerMask.NameToLayer(targetLayer)) +
                (1 << LayerMask.NameToLayer("EnemyProjectile")) +
                (1 << LayerMask.NameToLayer("PlayerProjectile"))
            ) // Collide with all layers, except the targetlayer and the projectiles
        );

        if (edgeDetection)
        {
            collidingWithGround = Physics2D.Linecast(
                new Vector2(this.transform.position.x, this.transform.position.y),
                new Vector2((this.transform.position.x + collideDistance), (this.transform.position.y - (GetComponent<SpriteRenderer>().bounds.size.y))),
                ~(
                    (1 << this.gameObject.layer) +
                    (1 << LayerMask.NameToLayer("EnemyProjectile")) +
                    (1 << LayerMask.NameToLayer("PlayerProjectile"))
                ) // Collide with all layers, except the targetlayer and the projectiles
            );
        }
        else
        {
            collidingWithGround = true;
        }

        if (collidingWithWall || !collidingWithGround)
        {
            //Debug.Log(this.name + " hit a wall, now walking the other way.");
            walkSpeed *= -1;
            collideDistance *= -1;
        }

        if (!isBlinded)
        {
            // Will set 'playerSpotted' to true if spotted
            IsTargetInRange();
            if (playerSpotted)
            {
                _state = EnemyState.waitThenAttack;
            }
        }
    }

    /**
     * In this method the enemy will stop to stare at the player, then after the delay,
     * it will shoot in the direction of the player.
     *
     * Bool readyToFire is triggered in the FireDelay coroutine.
     */
    private void WaitThenAttack()
    {
        // Patroling enemy needs to stop moving before shooting.
        // This enemy will resume patrol in the idle state
        if (type == EnemyType.patrol)
        {
            anim.SetFloat("speed", 0);
            GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0);
        }
        FacePlayer();
        if (readyToFire)
        {
            Shoot();
            readyToFire = false;
            _state = EnemyState.idle;
        }
    }

    /**
     * Provides a short delay before shooting and blinds the patrolling enemy,
     * so that it will continue to patrol after shooting.
     */
    IEnumerator FireDelay()
    {
        delayCoroutineStarted = true;
        readyToFire = false;
        yield return new WaitForSeconds(fireDelay);
        readyToFire = true;

        if (type == EnemyType.patrol)
        {
            isBlinded = true;
            yield return new WaitForSeconds(blindedDelay);
            isBlinded = false;
        }
        delayCoroutineStarted = false;
    }

    /**
     * Checks to see if an entity of the "Player" layer has entered the range of the enemy.
     *
     * Gets a list colliders that collided with the overlapcircle and uses the first result to
     * become the target of the enemy. This is so that you don't have to manually add the target to every enemy
     * and will help when multiplayer is implemented
     */
    private void IsTargetInRange()
    {
        collisionObjects = Physics2D.OverlapCircleAll(
            this.transform.position,
            spotRadius,
            (
                (1 << LayerMask.NameToLayer(targetLayer)) +
                (1 << LayerMask.NameToLayer("Friendly"))
            )
        );

        if (collisionObjects.Length > 0)
        {
            target = collisionObjects[0].gameObject;

            // If there are multiple targets, prioritise the player
            if (collisionObjects.Length > 1)
            {
                foreach (Collider2D spottedObject in collisionObjects)
                {
                    if (spottedObject.gameObject.layer == LayerMask.NameToLayer(targetLayer))
                    {
                        target = spottedObject.gameObject;
                    }
                }
            }

            playerSpotted = true;
        }
        else
        {
            playerSpotted = false;
        }
    }

    /**
     * This method makes sure the enemy will be facing the direction it is going in
     */
    private void FaceDirectionOfWalking()
    {
        if (GetComponent<Rigidbody2D>().velocity.x > 0)
        {
            walkingRight = true;
        }
        else
        {
            walkingRight = false;
        }
        if (walkingRight && facingLeft)
        {
            Flip();
        }
        else if (!walkingRight && !facingLeft)
        {
            Flip();
        }
    }

    /**
     * Script to make the enemy face the player
     */
    private void FacePlayer()
    {
        //Player could be destroyed
        if (target != null)
        {
            playerIsLeft = target.transform.position.x < this.transform.position.x;

            if (!playerIsLeft && facingLeft)
            {
                Flip();
            }
            else if (playerIsLeft && !facingLeft)
            {
                Flip();
            }
        }
    }

    /**
     * Flips the sprite of the enemy the other way around so it will face left/right.
     *
     * Used by both FacePlayer() and FaceDirectionOfWalking().
     */
    private void Flip()
    {
        // Switch the way the player is labelled as facing.
        facingLeft = !facingLeft;

        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;

        //Changes the speed to negative, making it fire the other way
        projectileSpeed = -projectileSpeed;
    }

    /**
     * Shoots a projectile in the direction the enemy is facing.
     *
     * Auto destructs after lifetime has ended.
     * Projectile should have a script attached to destruct it on collision.
     * Should also trigger the attack animation.
     */
    private void Shoot()
    {
        //hasShot = true;
        if (anim != null)
        {
            anim.SetTrigger("attacktrigger");
        }

        projectile = (GameObject)Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        projectile.GetComponent<Rigidbody2D>().velocity = new Vector2((projectileSpeed * -1), GetComponent<Rigidbody2D>().velocity.y);
        if (!facingLeft)
        {
            projectile.transform.localScale *= -1;
        }
        Destroy(projectile, projectileLifeTime);
    }

    /**
     * Take damage when hit with the players projectile. When this entity gets hit
     * it will get a period in which it can not be hurt ('onCoolDown'), granting
     * it invincibility for a short period of time.
     */
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "PlayerProjectile")
        {
            if (!onCoolDown && currentHealth > 0)
            {
                StartCoroutine(coolDownDMG());
                Debug.Log(this.gameObject.name + ": Au!");
                currentHealth -= 1;
                anim.SetTrigger("isHit");
            }
        }
    }

    /**
     * Sets the delay when this entity can get hurt again.
     */
    IEnumerator coolDownDMG()
    {
        onCoolDown = true;
        yield return new WaitForSeconds(invincibilityDuration);
        onCoolDown = false;
    }

    /**
     * Enemy death
     *
     * When an enemy dies, it will be replaced with a friendly of the same type.
     */
    void EnemyDeath()
    {
        Debug.Log(this.gameObject.name + ": 'Yay! Ik ben nu vriendelijk!'");
        if (type == EnemyType.patrol)
        {
            spawn = Instantiate(friendlyPatrol, this.transform.position, this.transform.rotation) as GameObject;
        }
        else if (type == EnemyType.stationary)
        {
            spawn = Instantiate(friendlyStationary, this.transform.position, this.transform.rotation) as GameObject;
        }
        spawn.SendMessage("GetMessage", message);
        Destroy(this.gameObject);
    }

    /**
     * Get the message of the friendly after it has been defeated.
     *
     * This is to save the message between transformations.
     */
    void GetMessage(string messageGet)
    {
        message = messageGet;
    }

    /**
     * Draws a circle gizmo to show the field of view or 'agro' range of an enemy
     */
    private void OnDrawGizmos()
    {
        if (drawSpotRadiusGismo)
        {
            Gizmos.color = Color.red;
            //Use the same vars you use to draw your Overlap SPhere to draw your Wire Sphere.
            Gizmos.DrawWireSphere(this.transform.position, spotRadius);
        }

        // Draws the collision for the patrol enemies
        if (type == EnemyType.patrol)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                new Vector2((this.transform.position.x + collideDistance), (this.transform.position.y - (GetComponent<SpriteRenderer>().bounds.size.y / 4))),
                new Vector2((this.transform.position.x + collideDistance), (this.transform.position.y + (GetComponent<SpriteRenderer>().bounds.size.y / 2)))
            );

            if (edgeDetection)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(
                    new Vector2(this.transform.position.x, this.transform.position.y),
                    new Vector2((this.transform.position.x + collideDistance), (this.transform.position.y - (GetComponent<SpriteRenderer>().bounds.size.y)))
                );
            }
        }
    }
}
