using System.Collections.Generic;
using UnityEngine;

namespace Vampire
{
    /// <summary>
    /// Moves the player toward a chest. Dodge mode avoids monsters; Rush mode goes straight.
    /// </summary>
    public class PlayerChestSeeker : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Character character;
        [SerializeField] private EntityManager entityManager;
        [SerializeField] private AbilitySelectionDialog abilitySelectionDialog;

        [Header("Arrival")]
        [SerializeField] private float openDistance = 0.65f;

        [Header("Dodge (short tap)")]
        [SerializeField] private float dangerRadius = 2.5f;
        [SerializeField] private float criticalRadius = 0.85f;
        [SerializeField] private float dodgeWeight = 2.5f;
        [SerializeField] private float criticalDodgeWeight = 6f;
        [SerializeField] private float chestPullWeight = 1.6f;

        [Header("Movement")]
        [SerializeField] private float directionSmoothing = 14f;

        private Chest targetChest;
        private ChestSeekMode seekMode;
        private Vector2 smoothedDirection;
        private bool isSeeking;
        private bool manuallyCommanded;

        public bool IsSeeking => isSeeking;
        public bool IsManuallyCommanded => manuallyCommanded;
        public bool IsRushSeeking => isSeeking && seekMode == ChestSeekMode.Rush;
        public Chest TargetChest => targetChest;

        private void Awake()
        {
            if (character == null)
                character = GetComponent<Character>();
            if (entityManager == null)
                entityManager = FindFirstObjectByType<EntityManager>();
            if (abilitySelectionDialog == null)
                abilitySelectionDialog = FindFirstObjectByType<AbilitySelectionDialog>();
        }

        public void BeginSeek(Chest chest, ChestSeekMode mode)
        {
            if (chest == null || chest.IsOpened || character == null)
                return;

            if (abilitySelectionDialog != null && abilitySelectionDialog.MenuOpen)
                return;

            if (GameTimeController.Instance != null && GameTimeController.Instance.IsFrozen)
                return;

            if (isSeeking && targetChest == chest)
            {
                SetSeekMode(mode);
                return;
            }

            targetChest = chest;
            seekMode = mode;
            isSeeking = true;
            manuallyCommanded = true;
            smoothedDirection = Vector2.zero;
            targetChest.ShowTargetRing(mode);
        }

        public void SetSeekMode(ChestSeekMode mode)
        {
            if (!isSeeking || targetChest == null)
                return;

            manuallyCommanded = true;
            seekMode = mode;
            smoothedDirection = Vector2.zero;
            targetChest.ShowTargetRing(mode);
        }

        public void CancelSeek()
        {
            if (targetChest != null)
                targetChest.HideTargetRing();
            isSeeking = false;
            manuallyCommanded = false;
            targetChest = null;
            if (character != null)
            {
                character.Move(Vector2.zero);
                character.StopWalkAnimation();
            }
        }

        private void FixedUpdate()
        {
            if (!isSeeking)
                return;

            if (ShouldCancel())
            {
                CancelSeek();
                return;
            }

            Vector2 playerPos = character.Position;
            Vector2 toChest = (Vector2)targetChest.transform.position - playerPos;
            float distance = toChest.magnitude;

            if (distance <= openDistance)
            {
                OpenTargetChest();
                return;
            }

            Vector2 desired = seekMode == ChestSeekMode.Rush
                ? ComputeRushDirection(toChest)
                : ComputeDodgeSeekDirection(playerPos, toChest);

            float smoothing = seekMode == ChestSeekMode.Rush ? 0f : directionSmoothing;
            if (smoothing > 0f)
                smoothedDirection = Vector2.Lerp(smoothedDirection, desired, smoothing * Time.fixedDeltaTime);
            else
                smoothedDirection = desired;

            if (smoothedDirection.sqrMagnitude > 0.01f)
            {
                character.Move(smoothedDirection.normalized);
                character.StartWalkAnimation();
            }
            else
            {
                character.Move(Vector2.zero);
                character.StopWalkAnimation();
            }
        }

        private bool ShouldCancel()
        {
            if (character == null || entityManager == null)
                return true;
            if (targetChest == null || !targetChest.gameObject.activeInHierarchy || targetChest.IsOpened)
                return true;
            if (abilitySelectionDialog != null && abilitySelectionDialog.MenuOpen)
                return true;
            if (GameTimeController.Instance != null && GameTimeController.Instance.IsFrozen)
                return true;
            return false;
        }

        private void OpenTargetChest()
        {
            character.Move(Vector2.zero);
            character.StopWalkAnimation();

            if (targetChest != null && !targetChest.IsOpened)
                targetChest.OpenChestFromManualSeek();

            CancelSeek();
        }

        private Vector2 ComputeRushDirection(Vector2 toChest)
        {
            return toChest.normalized;
        }

        private Vector2 ComputeDodgeSeekDirection(Vector2 playerPos, Vector2 toChest)
        {
            Vector2 dodge = ComputeDodgeVector(playerPos);
            Vector2 pull = toChest.normalized * chestPullWeight;

            if (dodge.sqrMagnitude < 0.0001f)
                return pull;

            float dodgeStrength = dodge.magnitude;
            float pullScale = dodgeStrength > criticalDodgeWeight * 0.5f ? 0.2f : 1f;
            return dodge + pull * pullScale;
        }

        private Vector2 ComputeDodgeVector(Vector2 playerPos)
        {
            Vector2 dodge = Vector2.zero;
            List<ISpatialHashGridClient> nearby = entityManager.Grid.FindNearbyInRadius(playerPos, dangerRadius);

            foreach (ISpatialHashGridClient client in nearby)
            {
                if (client is not Monster monster || !monster)
                    continue;

                Vector2 away = playerPos - monster.Position;
                float distance = away.magnitude;
                if (distance < 0.05f)
                    away = Random.insideUnitCircle.normalized;
                else
                    away /= distance;

                float t = 1f - Mathf.Clamp01(distance / dangerRadius);
                float weight = distance <= criticalRadius ? criticalDodgeWeight : dodgeWeight;
                dodge += away * (t * t * weight);
            }

            return dodge;
        }
    }
}
