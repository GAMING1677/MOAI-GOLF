using UnityEngine;

namespace MoaiGolf
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MoaiGolfWorldBoundsBounce : MonoBehaviour
    {
        private Rigidbody2D body;
        private MoaiGolfBounceSfx bounceSfx;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bounceSfx = GetComponent<MoaiGolfBounceSfx>();
        }

        private void FixedUpdate()
        {
            var position = body.position;
            var velocity = body.linearVelocity;

            if (position.x < MoaiGolfWorldSettings.WorldLeft && velocity.x < 0f)
            {
                body.position = new Vector2(MoaiGolfWorldSettings.WorldLeft, position.y);
                body.linearVelocity = new Vector2(-velocity.x, velocity.y);
                bounceSfx?.TryPlayBounce(Mathf.Abs(velocity.x));
            }
            else if (position.x > MoaiGolfWorldSettings.WorldRight && velocity.x > 0f)
            {
                body.position = new Vector2(MoaiGolfWorldSettings.WorldRight, position.y);
                body.linearVelocity = new Vector2(-velocity.x, velocity.y);
                bounceSfx?.TryPlayBounce(Mathf.Abs(velocity.x));
            }
        }
    }
}
