using System.Collections.Generic;
using UnityEngine;
using ArcherCraftor.Player;

namespace ArcherCraftor.Vision
{
    /// <summary>
    /// 《Escape from Duckov》 스타일 2D XY 평면 시야각(FOV) 원뿔 메시 및 장애물 그림자 생성기
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FieldOfView : MonoBehaviour
    {
        [Header("FOV Settings (2D XY)")]
        [SerializeField] private float viewRadius = 15f;
        [Range(0f, 360f)]
        [SerializeField] private float viewAngle = 100f;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float meshResolution = 1.0f;
        [SerializeField] private int edgeResolveIterations = 4;
        [SerializeField] private float edgeDstThreshold = 0.5f;

        private Mesh viewMesh;
        private MeshFilter viewMeshFilter;
        private PlayerController playerController;

        private readonly List<Vector3> viewPoints = new List<Vector3>(128);
        private Vector3[] vertices = System.Array.Empty<Vector3>();
        private int[] triangles = System.Array.Empty<int>();

        public float ViewRadius => viewRadius;
        public float ViewAngle => viewAngle;

        private void Start()
        {
            viewMesh = new Mesh { name = "FOV_2D_Mesh" };
            viewMeshFilter = GetComponent<MeshFilter>();
            viewMeshFilter.mesh = viewMesh;

            playerController = GetComponentInParent<PlayerController>();

            if (TryGetComponent<MeshRenderer>(out var mr))
            {
                mr.sortingLayerName = "FOV";
                mr.sortingOrder = 0;
            }
        }

        private void LateUpdate()
        {
            DrawFieldOfView();
        }

        private void DrawFieldOfView()
        {
            int stepCount = Mathf.Max(4, Mathf.RoundToInt(viewAngle * meshResolution));
            float stepAngleSize = viewAngle / stepCount;
            float baseAimAngle = playerController != null ? playerController.AimAngleDegrees : 0f;

            viewPoints.Clear();
            ViewCastInfo oldViewCast = default;

            for (int i = 0; i <= stepCount; i++)
            {
                float angle = baseAimAngle - viewAngle * 0.5f + stepAngleSize * i;
                ViewCastInfo newViewCast = ViewCast(angle);

                if (i > 0)
                {
                    bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                    if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
                    {
                        EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                        if (edge.pointA != Vector3.zero) viewPoints.Add(edge.pointA);
                        if (edge.pointB != Vector3.zero) viewPoints.Add(edge.pointB);
                    }
                }

                viewPoints.Add(newViewCast.point);
                oldViewCast = newViewCast;
            }

            int vertexCount = viewPoints.Count + 1;
            int triangleIndexCount = Mathf.Max(0, (vertexCount - 2) * 3);

            if (vertices.Length != vertexCount)
            {
                vertices = new Vector3[vertexCount];
            }
            if (triangles.Length != triangleIndexCount)
            {
                triangles = new int[triangleIndexCount];
            }

            vertices[0] = Vector3.zero;
            for (int i = 0; i < vertexCount - 1; i++)
            {
                vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);
                vertices[i + 1].z = 0f;

                if (i < vertexCount - 2)
                {
                    triangles[i * 3] = 0;
                    // XY 평면(Z = -10 카메라 방향)에서 앞면이 보이도록 시계 방향 감기
                    triangles[i * 3 + 1] = i + 2;
                    triangles[i * 3 + 2] = i + 1;
                }
            }

            viewMesh.Clear();
            viewMesh.vertices = vertices;
            viewMesh.triangles = triangles;
            viewMesh.RecalculateNormals();
        }

        private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
        {
            float minAngle = minViewCast.angle;
            float maxAngle = maxViewCast.angle;
            Vector3 minPoint = Vector3.zero;
            Vector3 maxPoint = Vector3.zero;

            for (int i = 0; i < edgeResolveIterations; i++)
            {
                float angle = (minAngle + maxAngle) * 0.5f;
                ViewCastInfo newViewCast = ViewCast(angle);

                bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
                {
                    minAngle = angle;
                    minPoint = newViewCast.point;
                }
                else
                {
                    maxAngle = angle;
                    maxPoint = newViewCast.point;
                }
            }

            return new EdgeInfo(minPoint, maxPoint);
        }

        private ViewCastInfo ViewCast(float globalAngle)
        {
            Vector2 dir = DirFromAngle2D(globalAngle);
            Vector2 origin = transform.position;
            int mask = obstacleLayer.value != 0 ? obstacleLayer.value : Physics2D.DefaultRaycastLayers;

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, viewRadius, mask);
            if (hit.collider != null && (obstacleLayer.value != 0 || hit.collider.CompareTag("Obstacle")))
            {
                return new ViewCastInfo(true, new Vector3(hit.point.x, hit.point.y, 0f), hit.distance, globalAngle);
            }

            Vector2 endPoint = origin + dir * viewRadius;
            return new ViewCastInfo(false, new Vector3(endPoint.x, endPoint.y, 0f), viewRadius, globalAngle);
        }

        public static Vector2 DirFromAngle2D(float angleInDegrees)
        {
            float rad = angleInDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        public struct ViewCastInfo
        {
            public bool hit;
            public Vector3 point;
            public float dst;
            public float angle;

            public ViewCastInfo(bool hit, Vector3 point, float dst, float angle)
            {
                this.hit = hit;
                this.point = point;
                this.dst = dst;
                this.angle = angle;
            }
        }

        public struct EdgeInfo
        {
            public Vector3 pointA;
            public Vector3 pointB;

            public EdgeInfo(Vector3 pointA, Vector3 pointB)
            {
                this.pointA = pointA;
                this.pointB = pointB;
            }
        }
    }
}
