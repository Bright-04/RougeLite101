using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Sword : Weapon //  inherits Weapon so EquipmentManager works
{
    [SerializeField] private float attackCooldown = 0.5f;
    private float nextAttackTime = 0f;

    [SerializeField] private GameObject slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;
    [SerializeField] private Transform weaponCollider;

    private PlayerControls playerControls;
    private Animator myAnimator;
    private PlayerController playerController;

    private GameObject slashAnim;

    [SerializeField] private Transform weaponHolder;

    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        myAnimator = GetComponent<Animator>();
        playerControls = new PlayerControls();

        weaponHolder = transform.parent; // because Sword is instantiated under WeaponHolder
    }


    private void OnEnable() => playerControls.Enable();
    private void OnDisable() => playerControls.Disable();

    private void Start()
    {
        playerControls.Combat.Attack.started += _ => Attack();
    }

    private void Update()
    {
        FollowPlayerDirection();
    }

    public override void Use()
    {
        Attack();
    }

    private void Attack()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        myAnimator.SetTrigger("Attack");
        weaponCollider.gameObject.SetActive(true);

        slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
        slashAnim.transform.parent = this.transform.parent;
    }

    public void DoneAttackingAnimEvent()
    {
        weaponCollider.gameObject.SetActive(false);
    }

    public void SwingUpFlipAnimEvent()
    {
        if (slashAnim == null) return;

        slashAnim.transform.rotation = Quaternion.Euler(-180, 0, 0);
        if (playerController.FacingLeft)
        {
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    public void SwingDownFlipAnimEvent()
    {
        if (slashAnim == null) return;

        slashAnim.transform.rotation = Quaternion.Euler(0, 0, 0);
        if (playerController.FacingLeft)
        {
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    private void FollowPlayerDirection()
    {
        if (playerController == null) Debug.LogError("Sword: playerController is NULL!", this);
        if (weaponCollider == null) Debug.LogError("Sword: weaponCollider is NULL!", this);

        if (playerController != null && playerController.FacingLeft)
        {
            // Flip the weapon holder on X scale instead of rotating on Y axis
            // This maintains position while flipping the sprite
            Vector3 newScale = weaponHolder.localScale;
            newScale.x = -Mathf.Abs(newScale.x); // Ensure it's negative (flipped)
            weaponHolder.localScale = newScale;
            
            // Keep collider rotation for proper hit detection
            weaponCollider.transform.localRotation = Quaternion.Euler(0, -180, 0);
        }
        else
        {
            // Reset to normal scale
            Vector3 newScale = weaponHolder.localScale;
            newScale.x = Mathf.Abs(newScale.x); // Ensure it's positive (not flipped)
            weaponHolder.localScale = newScale;
            
            weaponCollider.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }
}
