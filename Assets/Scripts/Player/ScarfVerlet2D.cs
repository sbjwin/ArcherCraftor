using UnityEngine;

namespace ArcherCraftor.Player
{
    /// <summary>
    /// 4~6개 노드 기반의 초경량 절차적 Verlet 물리 스카프 (GameConcept 1절)
    /// 이동 및 대시(Space) 시 바람과 관성에 맞춰 실시간으로 펄럭입니다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ScarfVerlet2D : MonoBehaviour
    {
        [Header("Verlet Scarf Settings")]
        [Range(4, 8)]
        [SerializeField] private int nodeCount = 5;
        [SerializeField] private float segmentLength = 0.22f;
        [SerializeField] private float damping = 0.88f;
        [SerializeField] private float windStrength = 1.6f;
        [SerializeField] private float windFrequency = 4.2f;
        [SerializeField] private int constraintIterations = 3;

        private LineRenderer lineRenderer;
        private Vector3[] currentPositions;
        private Vector3[] previousPositions;
        private Vector3 anchorOffset = new Vector3(0f, 0.1f, 0f);

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = nodeCount;

            currentPositions = new Vector3[nodeCount];
            previousPositions = new Vector3[nodeCount];

            ResetNodes();
        }

        private void OnEnable()
        {
            if (currentPositions != null)
            {
                ResetNodes();
            }
        }

        public void SetColor(Color startColor, Color endColor)
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.startColor = startColor;
            lineRenderer.endColor = endColor;
        }

        private void ResetNodes()
        {
            Vector3 origin = transform.position + anchorOffset;
            for (int i = 0; i < nodeCount; i++)
            {
                Vector3 pos = origin + new Vector3(-i * segmentLength, 0f, 0f);
                currentPositions[i] = pos;
                previousPositions[i] = pos;
            }
            lineRenderer.SetPositions(currentPositions);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            Vector3 origin = transform.position + anchorOffset;
            origin.z = 0f;

            currentPositions[0] = origin;
            previousPositions[0] = origin;

            float time = Time.time;
            Vector3 wind = new Vector3(
                Mathf.Sin(time * windFrequency) * windStrength,
                Mathf.Cos(time * (windFrequency * 0.8f)) * (windStrength * 0.45f) - 0.5f,
                0f);

            for (int i = 1; i < nodeCount; i++)
            {
                Vector3 curr = currentPositions[i];
                Vector3 vel = (curr - previousPositions[i]) * damping;
                previousPositions[i] = curr;
                currentPositions[i] = curr + vel + wind * (dt * dt * 8f);
                currentPositions[i].z = 0f;
            }

            for (int iter = 0; iter < constraintIterations; iter++)
            {
                currentPositions[0] = origin;
                for (int i = 0; i < nodeCount - 1; i++)
                {
                    Vector3 delta = currentPositions[i + 1] - currentPositions[i];
                    float dist = delta.magnitude;
                    if (dist < 0.0001f) continue;

                    float diff = (dist - segmentLength) / dist;
                    if (i == 0)
                    {
                        currentPositions[i + 1] -= delta * diff;
                    }
                    else
                    {
                        currentPositions[i] += delta * (diff * 0.5f);
                        currentPositions[i + 1] -= delta * (diff * 0.5f);
                    }
                }
            }

            lineRenderer.SetPositions(currentPositions);
        }
    }
}
