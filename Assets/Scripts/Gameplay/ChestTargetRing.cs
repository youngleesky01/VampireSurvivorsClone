using UnityEngine;

namespace Vampire
{
    /// <summary>
    /// Ring highlight around a chest: blue = tapped / dodge, red = long-press / rush.
    /// </summary>
    public class ChestTargetRing : MonoBehaviour
    {
        [SerializeField] private Sprite ringSprite;
        [SerializeField] private float ringDiameter = 1.35f;
        [SerializeField] private int lineSegments = 56;
        [SerializeField] private float lineRadius = 0.62f;
        [SerializeField] private float lineWidth = 0.07f;
        [SerializeField] private Color dodgeColor = new Color(0.35f, 0.72f, 1f, 0.92f);
        [SerializeField] private Color rushColor = new Color(1f, 0.28f, 0.28f, 0.95f);
        [SerializeField] private int sortingOrderOffset = -1;

        private SpriteRenderer spriteRing;
        private LineRenderer lineRing;
        private bool useSpriteRing;
        private ChestSeekMode? currentMode;

        public bool IsVisible => currentMode.HasValue;

        public void Configure(Sprite sprite, float diameter)
        {
            if (sprite != null)
            {
                ringSprite = sprite;
                ringDiameter = diameter;
            }
            EnsureVisual();
        }

        public void Show(ChestSeekMode mode)
        {
            EnsureVisual();
            currentMode = mode;
            Color color = mode == ChestSeekMode.Rush ? rushColor : dodgeColor;

            if (useSpriteRing && spriteRing != null)
            {
                spriteRing.enabled = true;
                spriteRing.color = color;
                if (lineRing != null)
                    lineRing.enabled = false;
            }
            else if (lineRing != null)
            {
                lineRing.enabled = true;
                lineRing.startColor = color;
                lineRing.endColor = color;
                if (spriteRing != null)
                    spriteRing.enabled = false;
            }
        }

        public void Hide()
        {
            currentMode = null;
            if (spriteRing != null)
                spriteRing.enabled = false;
            if (lineRing != null)
                lineRing.enabled = false;
        }

        private void EnsureVisual()
        {
            if (spriteRing != null || lineRing != null)
                return;

            if (ringSprite != null)
                CreateSpriteRing();
            else
                CreateLineRing();
        }

        private void CreateSpriteRing()
        {
            useSpriteRing = true;
            var ringObject = new GameObject("TargetRing");
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = Vector3.zero;

            spriteRing = ringObject.AddComponent<SpriteRenderer>();
            spriteRing.sprite = ringSprite;
            spriteRing.color = dodgeColor;

            Chest chest = GetComponent<Chest>();
            if (chest != null)
            {
                SpriteRenderer chestSprite = chest.GetComponentInChildren<SpriteRenderer>();
                if (chestSprite != null)
                {
                    spriteRing.sortingLayerID = chestSprite.sortingLayerID;
                    spriteRing.sortingOrder = chestSprite.sortingOrder + sortingOrderOffset;
                }
            }

            float spriteSize = Mathf.Max(spriteRing.sprite.bounds.size.x, spriteRing.sprite.bounds.size.y);
            if (spriteSize > 0.001f)
            {
                float scale = ringDiameter / spriteSize;
                ringObject.transform.localScale = Vector3.one * scale;
            }
        }

        private void CreateLineRing()
        {
            useSpriteRing = false;
            var ringObject = new GameObject("TargetRing");
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = Vector3.zero;

            lineRing = ringObject.AddComponent<LineRenderer>();
            lineRing.useWorldSpace = false;
            lineRing.loop = true;
            lineRing.positionCount = lineSegments;
            lineRing.widthMultiplier = lineWidth;
            lineRing.numCapVertices = 4;
            lineRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRing.receiveShadows = false;
            lineRing.material = new Material(Shader.Find("Sprites/Default"));

            for (int i = 0; i < lineSegments; i++)
            {
                float angle = i / (float)lineSegments * Mathf.PI * 2f;
                lineRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * lineRadius, Mathf.Sin(angle) * lineRadius, 0f));
            }

            Chest chest = GetComponent<Chest>();
            if (chest != null)
            {
                SpriteRenderer chestSprite = chest.GetComponentInChildren<SpriteRenderer>();
                if (chestSprite != null)
                {
                    lineRing.sortingLayerID = chestSprite.sortingLayerID;
                    lineRing.sortingOrder = chestSprite.sortingOrder + sortingOrderOffset;
                }
            }
        }

        private void OnDisable()
        {
            Hide();
        }
    }
}
