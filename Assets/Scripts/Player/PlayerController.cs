using UnityEngine;
using ArcherCraftor.Combat;
using ArcherCraftor.Crafting;

namespace ArcherCraftor.Player
{
    /// <summary>
    /// 2D 탑다운(XY 평면) 플레이어 이동, 대시 무적, 360도 마우스 조준 및 활 파츠/화살통 발광 제어기
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement (2D XY)")]
        [SerializeField] private float moveSpeed = 6.5f;
        [SerializeField] private float dashSpeed = 16.5f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 0.9f;

        [Header("Enter-the-Gungeon Style Parts")]
        [Tooltip("좌우 반전만 수행하는 몸체 스프라이트")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [Tooltip("마우스 커서를 따라 360도 독립 회전하는 활 피벗")]
        [SerializeField] private Transform bowPivot;
        [Tooltip("조준 상/하 방향에 따라 정렬 순서를 바꿀 활 스프라이트")]
        [SerializeField] private SpriteRenderer bowRenderer;
        [Tooltip("현재 선택된 특수 화살 색상으로 발광하는 등 뒤 화살통 표시기")]
        [SerializeField] private SpriteRenderer quiverGlowRenderer;
        [SerializeField] private int bodyBaseSortingOrder = 10;

        private Rigidbody2D rb;
        private Health playerHealth;
        private Camera mainCamera;

        private Vector2 moveInput;
        private Vector2 aimPoint;
        private Vector2 aimDirection = Vector2.right;
        private Vector2 dashDirection = Vector2.right;

        private float speedMultiplier = 1.0f;
        private float dashTimer;
        private float dashCooldownTimer;
        private bool isDashing;

        public Vector2 AimDirection => aimDirection;
        public Vector2 AimWorldPosition => aimPoint;
        public float AimAngleDegrees => Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        public bool IsDashing => isDashing;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            playerHealth = GetComponent<Health>();
            mainCamera = Camera.main;
        }

        private void Start()
        {
            if (FieldCraftingManager.Instance != null)
            {
                FieldCraftingManager.Instance.OnSelectedArrowChanged += UpdateQuiverGlow;
                UpdateQuiverGlow(FieldCraftingManager.Instance.SelectedSpecialArrow);
            }
        }

        private void OnDestroy()
        {
            if (FieldCraftingManager.Instance != null)
            {
                FieldCraftingManager.Instance.OnSelectedArrowChanged -= UpdateQuiverGlow;
            }
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            HandleInput();
            HandleAimRotation();
        }

        private void FixedUpdate()
        {
            HandleMovement();
        }

        private void HandleInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            moveInput = new Vector2(horizontal, vertical).normalized;

            dashCooldownTimer -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Space) && dashCooldownTimer <= 0f && !isDashing)
            {
                StartDash();
            }
        }

        private void HandleAimRotation()
        {
            if (mainCamera == null) return;

            Vector3 mouseScreen = Input.mousePosition;
            mouseScreen.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(mouseScreen);

            aimPoint = new Vector2(worldPoint.x, worldPoint.y);
            Vector2 dir = aimPoint - (Vector2)transform.position;

            if (dir.sqrMagnitude > 0.0001f)
            {
                aimDirection = dir.normalized;
            }

            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

            if (bowPivot != null)
            {
                bowPivot.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.flipX = aimDirection.x < -0.05f;
                bodyRenderer.sortingOrder = bodyBaseSortingOrder;
            }

            if (bowRenderer != null)
            {
                // 위쪽을 조준할 때는 몸체 뒤로, 아래쪽을 조준할 때는 몸체 앞으로 정렬
                bowRenderer.sortingOrder = aimDirection.y > 0.25f ? bodyBaseSortingOrder - 1 : bodyBaseSortingOrder + 1;
                bowRenderer.flipY = aimDirection.x < 0f;
            }
        }

        private void HandleMovement()
        {
            if (isDashing)
            {
                rb.linearVelocity = dashDirection * dashSpeed;
                dashTimer -= Time.fixedDeltaTime;
                if (dashTimer <= 0f)
                {
                    EndDash();
                }
                return;
            }

            rb.linearVelocity = moveInput * (moveSpeed * speedMultiplier);
        }

        private void StartDash()
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            dashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput : aimDirection;

            if (playerHealth != null)
            {
                playerHealth.SetInvulnerable(true);
            }
        }

        private void EndDash()
        {
            isDashing = false;
            if (playerHealth != null)
            {
                playerHealth.SetInvulnerable(false);
            }
        }

        private void UpdateQuiverGlow(ArrowType specialArrow)
        {
            if (quiverGlowRenderer != null)
            {
                quiverGlowRenderer.color = ArrowVisualProfile.GetArrowColor(specialArrow);
            }
        }

        /// <summary>
        /// 활 차징 등에 따른 이동 속도 배율 제어
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 1.5f);
        }
    }
}
