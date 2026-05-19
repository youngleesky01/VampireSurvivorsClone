using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Vampire
{
    /// <summary>
    /// Tap chest: dodge while moving. Hold until red: rush straight (ignores dodge/exp AI). Release: blue ring + dodge.
    /// </summary>
    public class ChestInputRouter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private PlayerChestSeeker chestSeeker;
        [SerializeField] private LayerMask chestRaycastLayers = ~0;

        [Header("Input")]
        [SerializeField] private float longPressDuration = 0.45f;
        [SerializeField] private bool blockWhenPointerOverUI = true;

        [Header("Selection Ring")]
        [SerializeField] private Sprite selectionRingSprite;
        [SerializeField] private float selectionRingDiameter = 1.35f;

        private Chest pressedChest;
        private float pressStartUnscaledTime;
        private bool trackingPress;
        private bool longPressActive;

        private void Awake()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;
            if (chestSeeker == null)
                chestSeeker = FindFirstObjectByType<PlayerChestSeeker>();
        }

        private void Update()
        {
            if (chestSeeker == null || worldCamera == null)
                return;

            if (TryGetPressDown(out Vector2 pressScreenPos))
            {
                pressedChest = RaycastChest(pressScreenPos);
                if (pressedChest == null && blockWhenPointerOverUI && IsPointerOverUI())
                    return;

                if (pressedChest != null && !pressedChest.IsOpened)
                {
                    trackingPress = true;
                    longPressActive = false;
                    pressStartUnscaledTime = Time.unscaledTime;
                    ShowRing(pressedChest, ChestSeekMode.Dodge);
                }
                else
                {
                    ClearPressState();
                }
            }

            if (trackingPress && pressedChest != null && !pressedChest.IsOpened)
            {
                float held = Time.unscaledTime - pressStartUnscaledTime;
                if (!longPressActive && held >= longPressDuration)
                {
                    longPressActive = true;
                    ShowRing(pressedChest, ChestSeekMode.Rush);
                    chestSeeker.BeginSeek(pressedChest, ChestSeekMode.Rush);
                }
            }

            if (trackingPress && TryGetPressUp(out _))
            {
                if (pressedChest != null && !pressedChest.IsOpened)
                {
                    ShowRing(pressedChest, ChestSeekMode.Dodge);

                    if (longPressActive)
                    {
                        if (chestSeeker.IsSeeking && chestSeeker.TargetChest == pressedChest)
                            chestSeeker.SetSeekMode(ChestSeekMode.Dodge);
                        else
                            chestSeeker.BeginSeek(pressedChest, ChestSeekMode.Dodge);
                    }
                    else
                    {
                        chestSeeker.BeginSeek(pressedChest, ChestSeekMode.Dodge);
                    }
                }
                else if (pressedChest != null)
                {
                    pressedChest.HideTargetRing();
                }

                ClearPressState();
            }

            if (trackingPress && pressedChest != null && pressedChest.IsOpened)
            {
                pressedChest.HideTargetRing();
                chestSeeker.CancelSeek();
                ClearPressState();
            }
        }

        private void ShowRing(Chest chest, ChestSeekMode mode)
        {
            if (chest == null)
                return;

            ChestTargetRing ring = chest.EnsureTargetRing(selectionRingSprite, selectionRingDiameter);
            ring.Show(mode);
        }

        private void ClearPressState()
        {
            trackingPress = false;
            longPressActive = false;
            pressedChest = null;
        }

        private Chest RaycastChest(Vector2 screenPosition)
        {
            Vector3 world = worldCamera.ScreenToWorldPoint(screenPosition);
            world.z = 0f;
            Collider2D hit = Physics2D.OverlapPoint(world, chestRaycastLayers);
            if (hit == null)
                return null;
            return hit.GetComponentInParent<Chest>();
        }

        private static bool TryGetPressDown(out Vector2 screenPosition)
        {
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    screenPosition = touch.position.ReadValue();
                    return true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            screenPosition = default;
            return false;
        }

        private static bool TryGetPressUp(out Vector2 screenPosition)
        {
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasReleasedThisFrame)
                {
                    screenPosition = touch.position.ReadValue();
                    return true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            screenPosition = default;
            return false;
        }

        private static bool IsPointerOverUI()
        {
            if (EventSystem.current == null)
                return false;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                return EventSystem.current.IsPointerOverGameObject(touchId);
            }

            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
