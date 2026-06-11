using System.Collections;
using UnityEngine;

namespace MoaiGolf
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CompositeCollider2D))]
    public sealed class MoaiGolfTitleLogoDrop : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Vector2 landingCenter = new Vector2(9.6f, 3.8f);
        [SerializeField] private float spawnYOffset = 12f;
        [SerializeField] private float targetScale = 1f;
        [SerializeField] private float settleVelocityThreshold = 0.35f;
        [SerializeField] private float settleAngularVelocityThreshold = 24f;

        private bool dropBegun;
        private bool hasSettled;

        public bool HasSettled => hasSettled;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            body ??= GetComponent<Rigidbody2D>();
            Physics2D.gravity = new Vector2(0f, MoaiGolfWorldSettings.GravityY);
            Time.fixedDeltaTime = MoaiGolfWorldSettings.FixedTimestep;
        }

        private void Start()
        {
            StartCoroutine(BeginDropAfterCompositeReady());
        }

        private IEnumerator BeginDropAfterCompositeReady()
        {
            yield return null;
            BeginDrop(landingCenter);
        }

        public void BeginDrop(Vector2 center)
        {
            dropBegun = true;
            hasSettled = false;
            landingCenter = center;
            GetComponent<CompositeCollider2D>()?.GenerateGeometry();
            transform.position = center + Vector2.up * spawnYOffset;
            transform.localScale = Vector3.one * targetScale;
            transform.rotation = Quaternion.identity;

            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 1f;
            body.linearDamping = 0f;
            body.angularDamping = 0.02f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = Random.Range(-80f, 80f);
            body.Sleep();
            body.WakeUp();
            Physics2D.SyncTransforms();
        }

        private void FixedUpdate()
        {
            if (!dropBegun || hasSettled || body.bodyType != RigidbodyType2D.Dynamic)
            {
                return;
            }

            if (body.linearVelocity.sqrMagnitude > settleVelocityThreshold * settleVelocityThreshold
                || Mathf.Abs(body.angularVelocity) > settleAngularVelocityThreshold)
            {
                return;
            }

            hasSettled = true;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            transform.rotation = Quaternion.identity;
        }

    }
}
