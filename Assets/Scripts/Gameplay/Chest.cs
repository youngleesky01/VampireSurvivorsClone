using UnityEngine;
using System.Collections;

namespace Vampire
{
    public class Chest : MonoBehaviour
    {
        protected ChestBlueprint chestBlueprint; 
        protected EntityManager entityManager;
        protected Character playerCharacter;
        protected ZPositioner zPositioner;
        protected Transform chestItemsParent;
        protected SpriteRenderer spriteRenderer;
        protected bool opened = false;
        public bool IsOpened => opened;

        public ChestTargetRing TargetRing => GetComponent<ChestTargetRing>();

        public ChestTargetRing EnsureTargetRing(Sprite ringSprite, float ringDiameter)
        {
            ChestTargetRing ring = GetComponent<ChestTargetRing>();
            if (ring == null)
                ring = gameObject.AddComponent<ChestTargetRing>();
            ring.Configure(ringSprite, ringDiameter);
            return ring;
        }

        public void ShowTargetRing(ChestSeekMode mode)
        {
            TargetRing?.Show(mode);
        }

        public void HideTargetRing()
        {
            TargetRing?.Hide();
        }

        public void Init(EntityManager entityManager, Character playerCharacter, Transform chestItemsParent)
        {
            this.entityManager = entityManager;
            this.playerCharacter = playerCharacter;
            this.chestItemsParent = chestItemsParent;
            (zPositioner = gameObject.AddComponent<ZPositioner>()).Init(playerCharacter.transform);
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Setup(ChestBlueprint chestBlueprint)
        {
            this.chestBlueprint = chestBlueprint;
            transform.localScale = Vector3.one;
            spriteRenderer.sprite = chestBlueprint.closedChest;
            opened = false;
            HideTargetRing();
            StartCoroutine(Appear());
        }

        private void SpawnLoot(Loot<GameObject> loot, bool openedByPlayer = true)
        {
            GameObject item = Instantiate(loot.item, chestItemsParent);
            item.transform.position = transform.position;
            transform.position += Vector3.back*0.001f;  // Nudge the collectable in front of the chest
            Collectable collectable = item.GetComponent<Collectable>();
            collectable.Init(entityManager, playerCharacter);
            Coin coin = collectable as Coin;
            if (coin != null)
                coin.Setup(transform.position, loot.coinType, true, true);
            else
                collectable.Setup(true, true);
            // Collect the item immediately if opened by player
            if (openedByPlayer)
                collectable.Collect(Collectable.CollectionMode.FromChest);
        }

        public void OpenChest(bool openedByPlayer = true)
        {
            TryOpen(openedByPlayer, bypassAutoPlayGuard: false);
        }

        /// <summary>Called when the player manually sought this chest and arrived.</summary>
        public void OpenChestFromManualSeek()
        {
            TryOpen(openedByPlayer: true, bypassAutoPlayGuard: true);
        }

        private void TryOpen(bool openedByPlayer, bool bypassAutoPlayGuard)
        {
            if (opened)
                return;

            if (openedByPlayer && !bypassAutoPlayGuard && !IsPlayerAllowedToOpenChest())
                return;

            opened = true;
            HideTargetRing();
            StartCoroutine(Open(openedByPlayer));
        }

        // This is some truly atrocious code: tread with caution.
        private IEnumerator Open(bool openedByPlayer = true)
        {
            spriteRenderer.sprite = chestBlueprint.openingChest;
            bool spawnLoot = !chestBlueprint.abilityChest || !entityManager.AbilitySelectionDialog.HasAvailableAbilities();
            if (spawnLoot)
                SpawnLoot(chestBlueprint.lootTable.DropLootObject(), openedByPlayer);
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.sprite = chestBlueprint.openChest;
            if (!spawnLoot)
                entityManager.AbilitySelectionDialog.Open(false);
            yield return new WaitForSeconds(0.15f);
            float t = 0;
            while (t < 1.0f)
            {
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, EasingUtils.EaseOutQuart(t));
                t += Time.deltaTime*2;
                yield return null;
            }
            entityManager.DespawnChest(this);
        }

        private IEnumerator Appear()
        {
            GetComponent<Collider2D>().enabled = false;
            float t = 0;
            while (t < 1.0f)
            {
                transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, EasingUtils.EaseInQuart(t));
                t += Time.deltaTime*2;
                yield return null;
            }
            transform.localScale = Vector3.one;
            GetComponent<Collider2D>().enabled = true;
        }

        void OnCollisionEnter2D(Collision2D col)
        {
            if (IsPlayerCollider(col.collider))
                OpenChest();
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            if (IsPlayerCollider(col))
                OpenChest();
        }

        void OnTriggerStay2D(Collider2D col)
        {
            if (IsPlayerCollider(col))
                OpenChest();
        }

        private bool IsPlayerAllowedToOpenChest()
        {
            if (playerCharacter == null)
                return false;

            AutoPlayController autoPlay = playerCharacter.GetComponent<AutoPlayController>();
            if (autoPlay == null)
                autoPlay = FindFirstObjectByType<AutoPlayController>();

            if (autoPlay == null || !autoPlay.AutoPlayEnabled)
                return true;

            PlayerChestSeeker seeker = playerCharacter.GetComponent<PlayerChestSeeker>();
            if (seeker == null)
                seeker = FindFirstObjectByType<PlayerChestSeeker>();

            return seeker != null && seeker.IsManuallyCommanded;
        }

        private bool IsPlayerCollider(Collider2D col)
        {
            if (opened || playerCharacter == null || col == null)
                return false;

            Character character = col.GetComponentInParent<Character>();
            return character != null && character == playerCharacter;
        }
    }
}
