using UnityEngine;

namespace MoaiGolf
{
    [DisallowMultipleComponent]
    public sealed class MoaiGolfAngleArrowVisual : MonoBehaviour
    {
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private int sortingOrder = 6;

        public SpriteRenderer SpriteRenderer => spriteRenderer;

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            ApplyVisual();
        }

        private void OnValidate()
        {
            ApplyVisual();
        }

        public void ApplyVisual()
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                return;
            }

            if (arrowSprite != null)
            {
                spriteRenderer.sprite = arrowSprite;
            }

            spriteRenderer.color = tint;
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }
}
