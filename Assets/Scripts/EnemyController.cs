using UnityEngine;
using System.Collections;

/*
 * List of selectable enemy types
 */
public enum EnemyType
{
    stationary,
    patrol,
    floating,
    sticky,
    runner
}

/*
 * List of posible states an enemy can be in
 */
public enum EnemyState
{
    idle,
    waitThenAttack,
    sprint
}

public class EnemyController : MonoBehaviour
{
    public EnemyType type;
    public EnemyState _state = EnemyState.idle;// Local variable to represent our state
    private Animator anim;
    public FriendlyGraphics friendlyGraphics;

    // Spawn friendly
    [Header("SPAWNS")]
    public GameObject friendlyPatrol;
    public GameObject friendlyStationary;
    public GameObject friendlyFloating;
    private GameObject spawn;
    private GameObject setSpawn;
    public GameObject enemyDeathEffect;

    // Message
    [Header("MESSAGE")]
    public string message = "";                 // The message the friendly will use after this enemy is defeated

    // Health
    [Header("HEALTH")]
    public float currentHealth = 2f;
    public float invincibilityDuration = 2f;    // length of damage cooldown
    private bool onCoolDown = false;            // Cooldown active or not

	// Spot
	[Header("SOUND")]
	public AudioClip enemyIdleSound;
	public AudioClip attackSound;
	public AudioClip enemyIsHitSound;
	public AudioClip enemyChanged;
	public bool isPlayed;
	private AudioSource _audioSource;
	private bool playOnce = false;

    // Movement
    [Header("MOVEMENT")]
    public float moveSpeed = 1f;                // Amount of velocity
    private bool movingRight = false;           // Simple check to see in what direction the enemy is moving, important for facing.

    // Patrol
    [Header("PATROL")]
    public float collideDistance = 0.5f;        // Distance from enemy to check for a wall.
    public bool edgeDetection = true;           // If checked, it will try to detect the edge of a platform
    private bool collidingWithWall = false;     // If true, it touched a wall and should flip.
    private bool collidingWithGround = true;    // If true, it is not about to fall off an edge

    // Hover
    [Header("HOVER")]
    public float hoverXSwing = 1f;
    public float hoverYSwing = 1f;
    public bool drawFloatPath;
    private Vector3 startPosition;
    private Vector3 leftPosition;
    private Vector3 rightPosition;
    private Vector3 moveTo;
    private float hoverSpeed;                   // Movespeed, used instead of movespeed to be able to reset the value

    // Target (usually the player)
    [Header("TARGET")]
    public string targetLayer = "Player";       // TODO: Make this a list, for players and friendly NPC's
    private GameObject target;

    // Firing Projectiles
    [Header("PROJECTILES")]
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

    // Spot
    [Header("SPOT")]
    public float spotRadius = 3;                // Radius in which a player can be spotted
    public bool drawSpotRadiusGismo = true;     // Visual aid in determening if the spot radius
    private Collider2D[] collisionObjects;
    private bool playerSpotted = false;         // Has the enemy spotted the player?

    // Blinded
    private bool isBlinded = false;
    public float blindedDelay = 3;

    // Sprint
    [Header("SPRINT")]
    public float sprintMinSpeed = 0.05f;

    // Sticky
    [Header("STICKY")]
    [Range(1.0f, 360f)]
    public float stickyFOV = 360f;

    private void Start()
    {
		//sound
		_audioSource = GetComponent<AudioSource>();
		isPlayed = false;

        if (type == EnemyType.floating)
        {
            startPosition = this.transform.position;

            leftPosition = new Vector3((startPosition.x - hoverXSwing), (startPosition.y + hoverYSwing), startPosition.z);
            rightPosition = new Vector3((startPosition.x + hoverXSwing), (startPosition.y + hoverYSwing), startPosition.z);
        }

        anim = GetComponent<Animator>();

        // The sticky has its sprite attached to it's firepoint to look at the player.
        if (type == EnemyType.sticky)
        {
            anim = firePoint.GetComponent<Animator>();
        }
    }

    void FixedUpdate()
    {
        switch (_state)
        {
            case EnemyState.idle:
                if (type == EnemyType.stationary)
                {
                    Idle();
					if (!isPlayed)
						PlaySound();
                }
				else if(type == EnemyType.sticky)
				{
					Idle();
				}
				else if(type == EnemyType.runner)
				{
					Idle();
				}
                else if (type == EnemyType.patrol)
                {
                    Patrol();
					if (!isPlayed)
						PlaySound();
                }
                else if (type == EnemyType.floating)
                {
                    Float();
                }
                break;
            case EnemyState.waitThenAttack:
                WaitThenAttack();
                // To ensure the coroutine is only fired once!
                if (!delayCoroutineStarted)
                    StartCoroutine(FireDelay());
                break;
            case EnemyState.sprint:
                Sprint();
				if (!isPlayed)
					PlaySound();
                break;
        }

        // Hovering enemy always hovers
        if (type == EnemyType.floating)
        {
            Hover();
        }
    }

	private void PlaySound()
	{
		//loop idle
		isPlayed = true;
		if (type == EnemyType.stationary) 
		{
			_audioSource.clip = enemyIdleSound;
			_audioSource.volume = 0.5f;
			_audioSource.loop = true;
			_audioSource.Play ();
		} 
		else if (type == EnemyType.patrol) 
		{
			_audioSource.clip = enemyIdleSound;
			_audioSource.volume = 0.25f;
			_audioSource.loop = true;
			_audioSource.Play ();
		} 
		else if (type == EnemyType.runner) 
		{
			_audioSource.clip = attackSound;
			_audioSource.volume = 0.25f;
			_audioSource.loop = true;
			_audioSource.Play ();
		}
	}

    /**
     * Idle state
     *
     * In this state, the enemy will wait to spot a player, and then it will go to its attack state.
     */
    private void Idle()
    {
        IsTargetInRange(); // Will set 'playerSpotted' to true if spotted
        if (playerSpotted)
        {
            if (type == EnemyType.runner)
			{
                _state = EnemyState.sprint;
			}
            else
			{
				isPlayed = false;
				_audioSource.Stop ();
				_state = EnemyState.waitThenAttack;
			}
        }

        // Running enemy is most of the time on ice and will be moving even after there is no force
        // applied to it, this stops the running animation if the speed is below minSpeed threshold.
		if (type == EnemyType.runner && Mathf.Abs (GetComponent<Rigidbody2D> ().velocity.x) <= sprintMinSpeed) 
		{
			_audioSource.Stop ();
			isPlayed = false;
			anim.SetBool ("sprint", false);
		}
	}

    /**
     * Patrol script
     *
     * enemy will walk untill the collidingWithWall linecast hits a collider, then walk the other way
     * or (if checked) will detect if the enemy is to hit the edge of a platform.
     *
     * Patroling enemys will resume to patrol after it shot at the player, as the attack state
     * will reset the timer. The first time the patroling enemy spots an enemy, the timer will
     * already have passed and it will immediately go into the attack state.
     */
    private void Patrol()
    {
        anim.SetFloat("speed", moveSpeed);
        GetComponent<Rigidbody2D>().velocity = new Vector2(moveSpeed, GetComponent<Rigidbody2D>().velocity.y);

        FaceDirectionOfWalking();

        collidingWithWall = Physics2D.Linecast(
            new Vector2((this.transform.position.x + collideDistance), (this.transform.position.y - (GetComponent<SpriteRenderer>().bounds.size.y / 4))),
            new Vector2((this.transform.position.x + collideDistance), (this.transform.position.y + (GetComponent<SpriteRenderer>().bounds.size.y / 2))),
            ~(
                (1 << LayerMask.NameToLayer(targetLayer)) +
                (1 << LayerMask.NameToLayer("EnemyProjectile")) +
                (1 << LayerMask.NameToLayer("PlayerProjectile")) +
                (1 << LayerMask.NameToLayer("Foreground"))
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
                    (1 << LayerMask.NameToLayer("PlayerProjectile")) +
                    (1 << LayerMask.NameToLayer("Foreground"))
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
            moveSpeed *= -1;
            collideDistance *= -1;
        }

        if (!isBlinded)
        {
            // Will set 'playerSpotted' to true if spotted
            IsTargetInRange();
            if (playerSpotted)
            {
				_audioSource.Stop ();
                _state = EnemyState.waitThenAttack;
            }
        }
    }

    /**
     * Float state
     *
     * Reset the movespeed if it was set by the waitThenAttack state, then
     * if not blinded, spot the player.
     */
    private void Float()
    {
        hoverSpeed = moveSpeed;
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
     * Hover
     *
     * In this script the floating enemy will hover in a V shape.
     * start/left/right position is set in the Start() method.
     */
    void Hover()
    {
        float step = hoverSpeed * Time.deltaTime;
        if (this.transform.position == startPosition && movingRight)
        {
            moveTo = rightPosition;
        }
        else if (this.transform.position == startPosition && !movingRight)
        {
            moveTo = leftPosition;
        }
        else if (this.transform.position == leftPosition)
        {
            movingRight = true;
            moveTo = startPosition;
        }
        else if (this.transform.position == rightPosition)
        {
            movingRight = false;
            moveTo = startPosition;
        }

        this.transform.position = Vector3.MoveTowards(this.transform.position, moveTo, step);
    }

    /**
     * This enemy will make a mad dash towards the player.
     * 
     * FacePlayer() will flip the enemy in the direction of the player, making it so that the
     * force will always propel the enemy towards the player.
     * 
     * Once it initiates the sprint the enemy will keep charging towards the player untill
     * the BlindSprint() time has passed, then it will check to see if the player is still
     * visable and if not will return to the idle state.
     */
    private void Sprint()
    {
        if (type == EnemyType.runner && Mathf.Abs(GetComponent<Rigidbody2D>().velocity.x) >= sprintMinSpeed)
		{
            anim.SetBool("sprint", true);
		}

        FacePlayer();
        GetComponent<Rigidbody2D>().AddForce(transform.right * (-moveSpeed * 5));

        if (!delayCoroutineStarted) // Trigger only once!
            StartCoroutine(BlindSprint());

        if (!isBlinded)
        {
            IsTargetInRange();
            if (!playerSpotted)
                _state = EnemyState.idle;
        }
    }

    /**
     * Called in Sprint()
     */
    IEnumerator BlindSprint()
    {
        delayCoroutineStarted = true;
        isBlinded = true;
        yield return new WaitForSeconds(blindedDelay);
        isBlinded = false;
        delayCoroutineStarted = false;
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
        if (type == EnemyType.patrol)
        {
            anim.SetFloat("speed", 0);
			isPlayed = false;
            GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0);
        }
        // Floating enemy will move slower
        else if (type == EnemyType.floating)
        {
            hoverSpeed = moveSpeed - (moveSpeed / 3);
            AimAtPlayer();
        }
        else if (type == EnemyType.sticky)
        {
			if(!playOnce)
			{
				AudioSource.PlayClipAtPoint(enemyIdleSound, startPosition);
				playOnce = true;
			}
            AimAtPlayer();
        }

        // Sticky enemy rotates it's head and should not flip
        if (type != EnemyType.sticky)
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
     * 
     * Called in WaitThenAttack state in Update()
     */
    IEnumerator FireDelay()
    {
        delayCoroutineStarted = true;
        readyToFire = false;
        yield return new WaitForSeconds(fireDelay);
        readyToFire = true;

        if (type == EnemyType.patrol || type == EnemyType.floating)
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

            if (collisionObjects.Length > 1)
            {
                foreach (Collider2D spottedObject in collisionObjects)
                {
                    // If there are multiple targets, prioritise the player
                    if (spottedObject.gameObject.layer == LayerMask.NameToLayer(targetLayer))
                    {
                        target = spottedObject.gameObject;
                        break;
                    }
                }
            }

            // Sticky enemy needs to check if the player is in view
            if (type == EnemyType.sticky)
            {
                playerSpotted = CanSeeObject(target);
            }
            else
            {
                playerSpotted = true;
            }
        }
        else
        {
            playerSpotted = false;
        }
    }

    /**
     * CanSeeObject, used to prevent an sticky from seeing though a wall
     *
     * The distance between the enemy and the spottedObject is taken care
     * of by the IsTargetInRange() method.
     * Objects given to this method will be within the spotRadius.
     */
    protected bool CanSeeObject(GameObject spottedObject)
    {
        Vector3 rayDirection = spottedObject.transform.position - transform.position;

        if ((Vector3.Angle(rayDirection, -this.transform.up)) <= (stickyFOV * 0.5f)) // half fieldOfView gives the desired result
        {
            Debug.DrawRay(transform.position, rayDirection, Color.yellow);

            return true;
        }
        else
        {
            return false;
        }
    }

    /**
     * This method makes sure the enemy will be facing the direction it is going in
     */
    private void FaceDirectionOfWalking()
    {
        if (GetComponent<Rigidbody2D>().velocity.x > 0)
        {
            movingRight = true;
        }
        else
        {
            movingRight = false;
        }
        if (movingRight && facingLeft)
        {
            Flip();
        }
        else if (!movingRight && !facingLeft)
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

        if (type == EnemyType.patrol || type == EnemyType.stationary)
        {
            //Changes the speed to negative, making it fire the other way
            projectileSpeed = -projectileSpeed;
        }
        else if (type == EnemyType.runner)
        {
            moveSpeed *= -1;
        }
    }

    /**
     * Calculates the rotation of the firepoint to point to the player.
     * 
     * Used by the floating enemy and the sticky enemy.
     * The sticky enemy has it's sprites rotation linked with the firepoint.
     */
    private void AimAtPlayer()
    {
        if (target != null)
        {
            Vector3 targetLocation = target.transform.position;

            float AngleRad = Mathf.Atan2(targetLocation.y - firePoint.transform.position.y, targetLocation.x - firePoint.transform.position.x);
            float AngleDeg = (180 / Mathf.PI) * AngleRad;
            firePoint.transform.rotation = Quaternion.Euler(0, 0, AngleDeg);
        }
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
        if (anim != null)
        {
            anim.SetTrigger("attacktrigger");
        }
		AudioSource.PlayClipAtPoint (attackSound, startPosition);

        projectile = (GameObject)Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        if (type == EnemyType.floating || type == EnemyType.sticky)
        {
            if (target != null)
            {
                projectile.transform.localScale *= -1;
                projectile.transform.rotation = firePoint.transform.rotation;
                Vector2 force = (Vector2)target.transform.position - (Vector2)this.transform.position;
                projectile.GetComponent<Rigidbody2D>().AddForce(force.normalized * (projectileSpeed * 30));

                Debug.DrawRay(transform.position, force, Color.yellow);
            }
        }
        else
        {
            if (!facingLeft)
            {
                projectile.transform.localScale *= -1;
            }
            projectile.GetComponent<Rigidbody2D>().velocity = new Vector2((projectileSpeed * -1), GetComponent<Rigidbody2D>().velocity.y);
        }
        Destroy(projectile, projectileLifeTime);
    }

    /**
     * Take damage when hit with the players projectile. When this entity gets hit
     * it will get a period in which it can not be hurt ('onCoolDown'), granting
     * it invincibility for a short period of time.
     * 
     * Called in LetterProjectileController.cs
     * Called in LetterProjectile2Controller.cs
     * Called in LetterProjectile3Controller.cs
     */
    public void TakeDamage()
    {
        if (!onCoolDown)
        {
            StartCoroutine(coolDownDMG());
            currentHealth -= 1;
            if (currentHealth <= 0)
                EnemyDeath();
        }
    }

    /**
     * Sets the delay when this entity can get hurt again and triggers the 
     * 'isHit' animation.
     * 
     * Called in TakeDamage()
     */
    IEnumerator coolDownDMG()
    {
        onCoolDown = true;
        anim.SetBool("isHit", true);

		AudioSource.PlayClipAtPoint(enemyIsHitSound, startPosition);

        yield return new WaitForSeconds(invincibilityDuration);
        onCoolDown = false;
        anim.SetBool("isHit", false);
    }

    /**
     * Enemy death
     *
     * When an enemy dies, it will be replaced with a friendly of the same type.
     */
    void EnemyDeath()
    {
        // Set the friendly spawn type
        if (type == EnemyType.patrol)
        {
            setSpawn = friendlyPatrol;
        }
        else if (type == EnemyType.stationary)
        {
            setSpawn = friendlyStationary;
        }
        else if (type == EnemyType.floating || type == EnemyType.sticky || type == EnemyType.runner)
        {
            setSpawn = friendlyFloating;
        }
        // Instantiate friendly
        spawn = Instantiate(setSpawn, this.transform.position, Quaternion.identity) as GameObject; // Reset rotation for sticky enemy can be rotated
		if (setSpawn != friendlyFloating) {
			spawn.GetComponent<FriendlyController> ().friendlyGraphics = friendlyGraphics;
		}

		if (enemyChanged != null) {
			AudioSource.PlayClipAtPoint (enemyChanged, startPosition);
		}

        // If there is a message, it should be send to the friendly
        if (!string.IsNullOrEmpty(message) || setSpawn != friendlyFloating)
        {
            spawn.SendMessage("GetMessage", message);
        }

        // Spawn the dove, facing the same direction as the enemy
        if ((type == EnemyType.floating || type == EnemyType.sticky) && !facingLeft)
        {
            spawn.transform.localScale = new Vector3(spawn.transform.localScale.x * -1, spawn.transform.localScale.y, spawn.transform.localScale.z);
        }

        // Instantiate death effect
        Instantiate(enemyDeathEffect, this.transform.position, this.transform.rotation);

        GameControl.control.enemiesDefeated++; // Analytics

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
     * OnDrawGiszmos
     *
     * drawSpotRadiusGismo:
     * Draws a circle gizmo to show the field of view or 'agro' range of an enemy
     *
     * Patrol:
     * Patroling enemy's show the distance it will check for a wall.
     *
     * edgeDetection:
     * Displays the raycast for the edgedetection
     *
     * drawFloatPath:
     * Shows the path the floating enemies take.
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

        if (type == EnemyType.floating)
        {
            if (drawFloatPath)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(
                    new Vector2((startPosition.x - hoverXSwing), (startPosition.y + hoverYSwing)),
                    new Vector2(startPosition.x, startPosition.y)
                );
                Gizmos.DrawLine(
                    new Vector2((startPosition.x + hoverXSwing), (startPosition.y + hoverYSwing)),
                    new Vector2(startPosition.x, startPosition.y)
                );
            }
        }
    }
}
