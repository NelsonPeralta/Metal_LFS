﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Hellhound : MonoBehaviour
{

    public NavMeshAgent nma;
    public Animator anim;
    public SwarmMode swarmMode;
    public Hitboxes hitboxes;
    public AIMeleeTrigger meleeTrigger;
    public SimpleAILineOfSight simpleLOS;
    public DestroyableObject shield;
    public GameObject motionTrackerDot;
    //public AIFieldOfVision fov;

    [Header("Hellhound Settings")]
    public float DefaultHealth;
    public float Health = 100;
    public int points;
    public float defaultSpeed;
    public int damage;
    public bool hasBeenMeleedRecently;

    public Transform target;


    public float defaultAttackCooldown;
    public float meleeAttackCooldown;
    public bool IsInMeleeRange;
    public bool isReadyToAttack;
    public bool isDead;
    public bool isAttacking;
    public bool shieldIsBroken;

    [Header("Animation Bools")]
    public bool isIdle;
    public bool isRunning;
    public bool isGuarding; // For Hitbox

    [Header("Ammo Packs")]
    public GameObject smallAmmoPack;
    public GameObject heavyAmmoPack;
    public GameObject powerAmmoPack;
    public GameObject grenadeAmmoPack;

    [Header("Weapons")]
    public GameObject[] droppableWeapons;

    [Header("Attack FX")]
    public GameObject fireAttack;
    public GameObject crystalWall;
    public GameObject crystallWallSpawnPoint;

    [Header("Player Switching")]
    public Transform lastPlayerWhoShot;
    public bool otherPlayerShot;
    public float targetSwitchCountdownDefault;
    public float targetSwitchCountdown;
    public float targetSwitchResetCountdown;
    public bool targetSwitchReady;
    public bool targetSwitchStarted;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] audioClips;
    public AudioClip[] attackClips;
    public AudioClip[] deathClips;

    [Header("Other")]
    public GameObject Aura;
    public GameObject smoke;

    // Start is called before the first frame update
    void OnEnable()
    {
        ResetHellhound();
    }

    // Update is called once per frame
    void Update()
    {
        HealthCheck();
        Movement();
        Attack();
        AttackCooldown();
        AnimationCheck();
        TargetSwitchCountdown();
    }

    void HealthCheck()
    {
        if (Health > 0)
        {
            if (target != null)
            {
                if (target.gameObject.GetComponent<PlayerProperties>().Health > 0)
                {
                    nma.SetDestination(target.position);
                }
                else if (target.gameObject.GetComponent<PlayerProperties>().Health <= 0)
                {
                    target = null;
                }

                if (swarmMode != null)
                {
                    if (swarmMode.editMode)
                    {
                        nma.speed = 0.01f;
                    }
                }
            }
            else
            {
                LookForNewRandomPlayer();
            }
        }

        if (Health <= 0 && !isDead)
        {
            nma.speed = 0;
            StartCoroutine(Die());
            isDead = true;
        }
    }

    void Movement()
    {
        if (!isDead)
        {
            if (!IsInMeleeRange)
            {
                nma.speed = defaultSpeed;
                anim.SetBool("Run Forward", true);
            }

            if (IsInMeleeRange && !isReadyToAttack)
            {
                nma.speed = 0;
                anim.SetBool("Run Forward", false);
            }

            if (target == null)
            {
                nma.speed = 0;
                anim.SetBool("Run Forward", false);
            }

            if (anim.GetCurrentAnimatorStateInfo(0).IsName("Run Forward"))
            {
                motionTrackerDot.SetActive(true);
            }
            else
            {
                motionTrackerDot.SetActive(false);
            }
        }
    }

    void Attack()
    {
        if (IsInMeleeRange && isReadyToAttack && !isDead)
        {
            if (meleeTrigger.pProperties != null)
            {
                if (!meleeTrigger.pProperties.isDead)
                {
                    meleeTrigger.pProperties.BleedthroughDamage(damage, false, 99);
                    int randomAnimation = Random.Range(1, 3);
                    if (randomAnimation == 1)
                        anim.Play("Bite Attack");
                    else if (randomAnimation == 2)
                        anim.Play("Claw Attack");
                    nma.velocity = Vector3.zero;

                    int randomSound = Random.Range(0, attackClips.Length - 1);
                    audioSource.clip = audioClips[randomSound];
                    audioSource.Play();
                    //var fireBird = Instantiate(fireAttack, gameObject.transform.position + new Vector3(0, 1f, 0), gameObject.transform.rotation);

                    isReadyToAttack = false;
                }
            }
        }
    }

    void AttackCooldown()
    {
        if (!isReadyToAttack)
        {
            meleeAttackCooldown -= Time.deltaTime;

            if (meleeAttackCooldown <= 0)
            {
                isReadyToAttack = true;
                meleeAttackCooldown = defaultAttackCooldown;
            }
        }
    }

    IEnumerator Die()
    {
        nma.enabled = false;
        anim.Play("Take Damage");
        StartCoroutine(SpawnSmoke());

        int randomSound = Random.Range(0, deathClips.Length - 1);
        audioSource.clip = deathClips[randomSound];
        audioSource.Play();

        if (swarmMode != null)
        {
            swarmMode.hellhoundsAlive -= 1;
        }

        foreach (AIHitbox hitbox in hitboxes.AIHitboxes)
        {
            //hitbox.gameObject.layer = 23; //Ground
            hitbox.gameObject.SetActive(false);
        }

        motionTrackerDot.SetActive(false);

        if (lastPlayerWhoShot)
        {
            lastPlayerWhoShot.GetComponent<AllPlayerScripts>().announcer.AddToMultiKill();
            TransferPoints();
        }
        DropRandomAmmoPack();
        //DropRandomWeapon();

        target = null;

        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }

    void AnimationCheck()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Run Forward"))
        {
            nma.speed = defaultSpeed;
            isRunning = true;
        }
        else
        {
            isRunning = false;
        }

        /*
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Die") || anim.GetCurrentAnimatorStateInfo(0).IsName("Idle") ||
            anim.GetCurrentAnimatorStateInfo(0).IsName("Attack") || anim.GetCurrentAnimatorStateInfo(0).IsName("Shield Break"))
        {
            nma.speed = 0;
            nma.velocity = Vector3.zero;
        }
        */
    }


    void DropRandomAmmoPack()
    {
        int ChanceToDrop = Random.Range(1, 11);

        if (ChanceToDrop <= 6)
        {
            Instantiate(smallAmmoPack, gameObject.transform.position, gameObject.transform.rotation);
        }

        if (ChanceToDrop >= 7)
        {
            Instantiate(heavyAmmoPack, gameObject.transform.position, gameObject.transform.rotation);
        }
    }

    void DropRandomWeapon()
    {
        int ChanceToDrop = Random.Range(0, 10);

        if (ChanceToDrop <= 5)
        {
            int randomInt = Random.Range(0, droppableWeapons.Length - 1);
            GameObject weapon = Instantiate(droppableWeapons[randomInt], gameObject.transform.position + new Vector3(0, 0.5f, 0), gameObject.transform.rotation);
            weapon.gameObject.name = weapon.name.Replace("(Clone)", "");

            Destroy(weapon, 60);
        }
    }

    public void TargetSwitch(GameObject playerWhoShotLast)
    {
        if (target != null)
        {
            if (playerWhoShotLast.name != target.gameObject.name)
            {
                if (lastPlayerWhoShot != playerWhoShotLast)
                {
                    targetSwitchCountdown = targetSwitchCountdownDefault;
                    otherPlayerShot = true;
                    lastPlayerWhoShot = playerWhoShotLast.transform;
                }
            }
            else if (playerWhoShotLast.name == target.gameObject.name)
            {
                targetSwitchCountdown = targetSwitchCountdownDefault;
                otherPlayerShot = false;
                lastPlayerWhoShot = playerWhoShotLast.transform;
            }
        }
        else
        {
            target = playerWhoShotLast.gameObject.transform;
            lastPlayerWhoShot = playerWhoShotLast.transform;
            nma.SetDestination(target.position);
        }
    }

    void TargetSwitchCountdown()
    {
        if (otherPlayerShot)
        {
            targetSwitchCountdown -= Time.deltaTime;

            if (targetSwitchCountdown <= 0)
            {
                if (targetSwitchReady)
                {
                    targetSwitchReady = false;
                    target = lastPlayerWhoShot.transform;
                    nma.SetDestination(target.position);
                    targetSwitchCountdown = targetSwitchCountdownDefault;
                    StartCoroutine(TargetSwitchReset());
                }
                otherPlayerShot = false;
                targetSwitchStarted = false;
            }
        }
    }

    IEnumerator TargetSwitchReset()
    {
        yield return new WaitForSeconds(targetSwitchCountdown);

        targetSwitchReady = true;
    }

    IEnumerator PlaySound()
    {
        yield return new WaitForSeconds(5f);

        int playSound = Random.Range(0, 2);

        if (playSound == 0)
        {
            int randomSound = Random.Range(0, audioClips.Length);

            if (!isDead)
            {
                audioSource.clip = audioClips[randomSound];
                audioSource.Play();
            }
        }

        StartCoroutine(PlaySound());
    }

    public IEnumerator MeleeReset()
    {
        yield return new WaitForEndOfFrame();

        hasBeenMeleedRecently = false;
    }

    IEnumerator SpawnSmoke()
    {
        yield return new WaitForSeconds(.4f);

        var smoke1 = Instantiate(smoke, transform.position + new Vector3(0, 0.5f, 0), transform.rotation);

        Destroy(smoke1, 5f);
    }

    void TransferPoints()
    {
        if (lastPlayerWhoShot.gameObject != null)
        {
            if (lastPlayerWhoShot.gameObject.GetComponent<PlayerPoints>() != null)
            {
                PlayerPoints pPoints = lastPlayerWhoShot.gameObject.GetComponent<PlayerPoints>();

                pPoints.swarmPoints = pPoints.swarmPoints + points;
                pPoints.swarmPointsText.text = pPoints.swarmPoints.ToString();
            }
        }
    }

    public void TransferDamageToPoints(int points)
    {
        if (lastPlayerWhoShot.gameObject != null)
        {
            if (lastPlayerWhoShot.gameObject.GetComponent<PlayerPoints>() != null)
            {
                PlayerPoints pPoints = lastPlayerWhoShot.gameObject.GetComponent<PlayerPoints>();

                pPoints.swarmPoints = pPoints.swarmPoints + points;
                pPoints.swarmPointsText.text = pPoints.swarmPoints.ToString();
            }
        }
    }

    void LookForNewRandomPlayer()
    {
        if (swarmMode != null)
        {
            target = swarmMode.NewTargetFromSwarmScript();
        }
    }

    void ResetHellhound()
    {
        nma.enabled = true;
        nma.speed = defaultSpeed;
        StartCoroutine(PlaySound());

        Health = DefaultHealth;
        isDead = false;
        IsInMeleeRange = false;
        isReadyToAttack = true;

        foreach (AIHitbox hitbox in hitboxes.AIHitboxes)
            hitbox.gameObject.SetActive(true);

        motionTrackerDot.SetActive(true);

        meleeAttackCooldown = 0;
        lastPlayerWhoShot = null;
        otherPlayerShot = false;
        targetSwitchCountdown = targetSwitchCountdownDefault;
        targetSwitchReady = true;
    }
}
