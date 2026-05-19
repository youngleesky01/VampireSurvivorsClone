using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Vampire
{
    /// <summary>
    /// Test AI: dodge nearby monsters, seek exp gems, and stop moving while the level-up menu is open.
    /// Toggle with the configured key (default: T). Level-up skill selection still requires the player.
    /// </summary>
    public class AutoPlayController : MonoBehaviour
    {
        [Header("References (auto-filled if empty)")]
        [SerializeField] private Character character;
        [SerializeField] private EntityManager entityManager;
        [SerializeField] private AbilitySelectionDialog abilitySelectionDialog;
        [SerializeField] private PlayerChestSeeker chestSeeker;
        [SerializeField] private PlayerInput playerInput;

        [Header("Control")]
        [SerializeField] private bool autoPlayEnabledOnStart = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.T;
        [SerializeField] private bool disablePlayerInputWhileAuto = true;

        [Header("Dodge")]
        [SerializeField] private float dangerRadius = 2.5f;
        [SerializeField] private float criticalRadius = 0.85f;
        [SerializeField] private float dodgeWeight = 2.5f;
        [SerializeField] private float criticalDodgeWeight = 6f;

        [Header("Collect")]
        [SerializeField] private float gemSeekRadius = 12f;
        [SerializeField] private float collectWeight = 1.35f;
        [SerializeField] private float gemValueExponent = 1.15f;

        [Header("Movement")]
        [SerializeField] private float directionSmoothing = 12f;

        private Vector2 smoothedDirection;
        private bool autoPlayEnabled;

        public bool AutoPlayEnabled => autoPlayEnabled;

        private void Awake()
        {
            ResolveReferences();
            autoPlayEnabled = autoPlayEnabledOnStart;
            ApplyInputState();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                autoPlayEnabled = !autoPlayEnabled;
                ApplyInputState();
                if (!autoPlayEnabled)
                    character.Move(Vector2.zero);
            }
        }

        private void FixedUpdate()
        {
            if (!autoPlayEnabled || character == null || entityManager == null)
                return;

            if (abilitySelectionDialog != null && abilitySelectionDialog.MenuOpen)
            {
                character.Move(Vector2.zero);
                character.StopWalkAnimation();
                return;
            }

            // While seeking a chest (especially rush), do not dodge monsters or chase exp gems.
            if (chestSeeker != null && chestSeeker.IsSeeking)
                return;

            Vector2 desiredDirection = ComputeDesiredDirection();
            if (directionSmoothing > 0f)
                smoothedDirection = Vector2.Lerp(smoothedDirection, desiredDirection, directionSmoothing * Time.fixedDeltaTime);
            else
                smoothedDirection = desiredDirection;

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

        private Vector2 ComputeDesiredDirection()
        {
            Vector2 playerPos = character.Position;
            Vector2 dodgeVector = ComputeDodgeVector(playerPos);
            Vector2 collectVector = ComputeCollectVector(playerPos);

            if (dodgeVector.sqrMagnitude < 0.0001f && collectVector.sqrMagnitude < 0.0001f)
                return Vector2.zero;

            if (dodgeVector.sqrMagnitude > 0.0001f && collectVector.sqrMagnitude > 0.0001f)
            {
                float dodgeStrength = dodgeVector.magnitude;
                float collectScale = dodgeStrength > criticalDodgeWeight * 0.5f ? 0.15f : 1f;
                return dodgeVector + collectVector * collectScale;
            }

            return dodgeVector + collectVector;
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

        private Vector2 ComputeCollectVector(Vector2 playerPos)
        {
            ExpGem bestGem = null;
            float bestScore = float.MinValue;

            foreach (ExpGem gem in entityManager.ActiveExpGems)
            {
                if (gem == null || !gem.gameObject.activeInHierarchy || !IsGemCollectable(gem))
                    continue;

                Vector2 offset = (Vector2)gem.transform.position - playerPos;
                float distance = offset.magnitude;
                if (distance > gemSeekRadius || distance < 0.05f)
                    continue;

                float value = Mathf.Pow((int)gem.GemType, gemValueExponent);
                float score = value / (distance + 0.5f);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestGem = gem;
                }
            }

            if (bestGem == null)
                return Vector2.zero;

            Vector2 toGem = (Vector2)bestGem.transform.position - playerPos;
            return toGem.normalized * collectWeight;
        }

        private static bool IsGemCollectable(ExpGem gem)
        {
            Collider2D col = gem.GetComponent<Collider2D>();
            return col != null && col.enabled;
        }

        private void ResolveReferences()
        {
            if (character == null)
                character = GetComponent<Character>();
            if (character == null)
                character = FindFirstObjectByType<Character>();

            if (entityManager == null)
                entityManager = FindFirstObjectByType<EntityManager>();

            if (abilitySelectionDialog == null)
                abilitySelectionDialog = FindFirstObjectByType<AbilitySelectionDialog>();

            if (chestSeeker == null)
                chestSeeker = FindFirstObjectByType<PlayerChestSeeker>();

            if (playerInput == null && character != null)
                playerInput = character.GetComponent<PlayerInput>();
        }

        private void ApplyInputState()
        {
            if (!disablePlayerInputWhileAuto || playerInput == null)
                return;

            playerInput.enabled = !autoPlayEnabled;
        }

        private void OnDisable()
        {
            if (playerInput != null)
                playerInput.enabled = true;
        }
    }
}
