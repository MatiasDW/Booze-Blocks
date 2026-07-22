using BoozeBlocks.Distractions;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Horde
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class KidUnit : MonoBehaviour
    {
        private const int ObstacleLayerMask = 1 << 7;

        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(0.05f)] private float steeringInterval = 0.25f;
        [SerializeField, Min(0f)] private float separationRadius = 1.1f;
        [SerializeField, Min(0f)] private float separationWeight = 1.25f;
        [SerializeField, Min(0f)] private float contactDistance = 1.35f;
        [SerializeField, Min(0f)] private float obstacleProbeDistance = 1.6f;
        [SerializeField, Min(0f)] private float obstacleAvoidanceWeight = 1.8f;

        private PlayerVitals playerTarget;
        private AttractionSource attraction;
        private KidSpatialGrid spatialGrid;
        private Vector3 moveDirection;
        private Vector3 knockbackDirection;
        private float steeringTimer;
        private float knockbackRemaining;
        private float stunnedUntil;
        private int unitId;

        public Vector3 Position => transform.position;

        private void Awake()
        {
            // Gameplay contact is counted centrally; individual units never enter the PhysX solver.
            GetComponent<CapsuleCollider>().enabled = false;
        }

        private void OnEnable()
        {
            steeringTimer = Random.Range(0f, steeringInterval);
        }

        private void OnDisable()
        {
            ReleaseAttraction();
            moveDirection = Vector3.zero;
            knockbackRemaining = 0f;
            stunnedUntil = 0f;
        }

        private void Update()
        {
            if (knockbackRemaining > 0f)
            {
                float step = Mathf.Min(knockbackRemaining, 8f * Time.deltaTime);
                transform.position += knockbackDirection * step;
                knockbackRemaining -= step;
                return;
            }

            if (Time.time < stunnedUntil) return;
            if (playerTarget == null || playerTarget.Model.IsEliminated)
            {
                moveDirection = Vector3.zero;
                return;
            }

            steeringTimer -= Time.deltaTime;
            if (steeringTimer <= 0f)
            {
                steeringTimer = steeringInterval;
                RefreshDirection();
            }

            if (moveDirection.sqrMagnitude <= 0.001f) return;
            transform.position += moveDirection * (moveSpeed * Time.deltaTime);
            float wobble = Mathf.Sin(Time.time * 8f + unitId * 0.73f) * 9f;
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up)
                * Quaternion.Euler(0f, 0f, wobble);
        }

        public void Initialize(int id, KidSpatialGrid grid)
        {
            unitId = id;
            spatialGrid = grid;
        }

        public void Activate(Vector3 position, PlayerVitals target)
        {
            transform.SetPositionAndRotation(position, Quaternion.identity);
            playerTarget = target;
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            playerTarget = null;
            gameObject.SetActive(false);
        }

        public void SetPlayerTarget(PlayerVitals target)
        {
            playerTarget = target;
        }

        public void KnockAway(Vector3 origin, float distance, float stunDuration)
        {
            Vector3 away = Vector3.ProjectOnPlane(transform.position - origin, Vector3.up);
            if (away.sqrMagnitude <= 0.001f) away = transform.forward;
            knockbackDirection = away.normalized;
            knockbackRemaining = Mathf.Max(knockbackRemaining, distance);
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + Mathf.Max(0f, stunDuration));
            ReleaseAttraction();
        }

        public bool TryGetPressureTarget(out PlayerVitals target)
        {
            target = playerTarget;
            if (target == null || target.Model.IsEliminated || attraction != null ||
                knockbackRemaining > 0f || Time.time < stunnedUntil) return false;
            float contactDistanceSquared = contactDistance * contactDistance;
            return (transform.position - target.transform.position).sqrMagnitude <= contactDistanceSquared;
        }

        private void RefreshDirection()
        {
            AttractionSource nextAttraction = AttractionRegistry.ClaimBest(transform.position, unitId);
            if (nextAttraction != attraction)
            {
                ReleaseAttraction();
                attraction = nextAttraction;
            }

            Vector3 targetPosition = attraction != null
                ? attraction.transform.position
                : playerTarget.transform.position;
            Vector3 seek = Vector3.ProjectOnPlane(targetPosition - transform.position, Vector3.up).normalized;
            Vector3 separation = spatialGrid?.CalculateSeparation(this, separationRadius) ?? Vector3.zero;
            Vector3 avoidance = CalculateObstacleAvoidance(seek);
            moveDirection = (seek + separation * separationWeight + avoidance * obstacleAvoidanceWeight).normalized;
        }

        private Vector3 CalculateObstacleAvoidance(Vector3 seek)
        {
            if (seek.sqrMagnitude <= 0.001f || obstacleProbeDistance <= 0f) return Vector3.zero;
            Vector3 origin = transform.position + Vector3.up * 0.45f;
            if (!Physics.SphereCast(origin, 0.25f, seek, out RaycastHit hit, obstacleProbeDistance,
                    ObstacleLayerMask, QueryTriggerInteraction.Ignore)) return Vector3.zero;

            Vector3 tangent = Vector3.Cross(Vector3.up, hit.normal).normalized;
            if ((unitId & 1) == 0) tangent = -tangent;
            return tangent + hit.normal * 0.35f;
        }

        private void ReleaseAttraction()
        {
            if (attraction != null) attraction.Release(unitId);
            attraction = null;
        }
    }
}
