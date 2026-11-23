using UnityEngine;

namespace ArcherCraftor.Core
{
    /// <summary>
    /// 2D 직교(Orthographic) 탑다운 카메라 추적 + 마우스 조준 방향 Aim Peek
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class TopDownCameraFollow : MonoBehaviour
    {
        [Header("Target & 2D Offset")]
        [SerializeField] private Transform target;
        [SerializeField] private float cameraZ = -10f;
        [SerializeField] private float orthographicSize = 8.5f;
        [SerializeField] private float smoothSpeed = 9f;

        [Header("Aim Peek (조준 방향 시야 확장)")]
        [Tooltip("마우스 커서 방향으로 카메라를 살짝 이동시키는 배율")]
        [SerializeField] private float aimPeekMultiplier = 0.22f;
        [SerializeField] private float maxPeekDistance = 3.5f;

        private Camera cam;
        private Vector3 currentVelocity;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, cameraZ);

            if (cam != null)
            {
                Vector3 mouseScreen = Input.mousePosition;
                mouseScreen.z = Mathf.Abs(cameraZ);
                Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
                Vector2 aimOffset = ((Vector2)mouseWorld - (Vector2)target.position) * aimPeekMultiplier;
                aimOffset = Vector2.ClampMagnitude(aimOffset, maxPeekDistance);
                desiredPosition.x += aimOffset.x;
                desiredPosition.y += aimOffset.y;
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / smoothSpeed);
        }
    }
}
