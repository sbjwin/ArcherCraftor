using UnityEngine;

namespace ArcherCraftor.Crafting
{
    /// <summary>
    /// 2D 월드에 드롭/배치된 채집 재료 오브젝트. 플레이어가 근접하면 자석처럼 끌려와 자동 획득됩니다.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class PickupResource2D : MonoBehaviour
    {
        [Header("Resource Settings")]
        [SerializeField] private ResourceType resourceType = ResourceType.FlintStone;
        [SerializeField] private int amount = 1;

        [Header("Magnet Pull")]
        [Tooltip("플레이어를 감지해 자석처럼 끌려오기 시작하는 반경")]
        [SerializeField] private float magnetRadius = 3.2f;
        [SerializeField] private float pullSpeed = 11f;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobFrequency = 3.5f;

        private Transform playerTarget;
        private Vector3 basePosition;
        private float bobOffset;
        private bool isCollected;

        public void Configure(ResourceType type, int quantity = 1)
        {
            resourceType = type;
            amount = Mathf.Max(1, quantity);

            if (TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.color = ArrowVisualProfile.GetResourceColor(resourceType);
            }
        }

        private void Start()
        {
            basePosition = transform.position;
            bobOffset = Random.Range(0f, Mathf.PI * 2f);

            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        private void Update()
        {
            if (isCollected || playerTarget == null) return;

            Vector2 toPlayer = (Vector2)(playerTarget.position - transform.position);
            float distSq = toPlayer.sqrMagnitude;

            if (distSq <= magnetRadius * magnetRadius)
            {
                Vector3 nextPos = Vector3.MoveTowards(transform.position, playerTarget.position, pullSpeed * Time.deltaTime);
                nextPos.z = 0f;
                transform.position = nextPos;
            }
            else
            {
                float bob = Mathf.Sin(Time.time * bobFrequency + bobOffset) * bobAmplitude;
                transform.position = new Vector3(basePosition.x, basePosition.y + bob, 0f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected) return;
            if (!other.CompareTag("Player")) return;

            isCollected = true;
            FieldCraftingManager.Instance?.AddResource(resourceType, amount);
            Destroy(gameObject);
        }
    }
}
