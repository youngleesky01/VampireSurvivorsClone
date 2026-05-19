using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.UI;

namespace Vampire
{
    /// <summary>
    /// Touch interactable joystick. Utilizes IPointer interfaces to ensure
    /// make touches interacting with other UI elements easier to handle.
    /// </summary>
    public class TouchJoystick : OnScreenControl, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private bool enableTouchMovement = false;
        [SerializeField] private bool permanent = false;
        [SerializeField] private bool showJoystickVisual = false;
        [SerializeField] private float joystickRadius;
        [SerializeField] private RectTransform joystick, joystickBounds;
        [SerializeField] private UnityEvent<Vector2> onJoystickMoved;
        [SerializeField] private UnityEvent onStartTouch, onEndTouch;
        [InputControl(layout = "Vector2")]
        [SerializeField]
        private string m_ControlPath;
        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

        private RectTransform controlRect;
        private Image touchCaptureImage;
        private bool beingTouched;

        public bool BeingTouched { get => beingTouched; }
        public bool TouchMovementEnabled => enableTouchMovement;

        void Awake()
        {
            controlRect = GetComponent<RectTransform>();
            touchCaptureImage = GetComponent<Image>();
            ApplyTouchMovementState();
        }

        private void OnValidate()
        {
            if (controlRect == null)
                controlRect = GetComponent<RectTransform>();
            if (touchCaptureImage == null)
                touchCaptureImage = GetComponent<Image>();
            ApplyTouchMovementState();
        }

        public void SetTouchMovementEnabled(bool enabled)
        {
            enableTouchMovement = enabled;
            ApplyTouchMovementState();
        }

        private void ApplyTouchMovementState()
        {
            if (!enableTouchMovement && beingTouched)
                EndTouch();

            if (touchCaptureImage != null)
                touchCaptureImage.raycastTarget = enableTouchMovement;

            ApplyJoystickVisualState();
        }

        private void ApplyJoystickVisualState()
        {
            if (!enableTouchMovement || !showJoystickVisual)
            {
                permanent = false;
                if (joystick != null)
                    joystick.gameObject.SetActive(false);
                if (joystickBounds != null)
                    joystickBounds.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (!enableTouchMovement || !beingTouched)
                return;

            if (Time.timeScale > 0)
            {
                Vector2 touchPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(controlRect, Input.mousePosition, null, out touchPosition);
                UpdateTouch(touchPosition);
            }
            else
            {
                EndTouch();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!enableTouchMovement)
                return;

            Vector2 touchPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(controlRect, eventData.position, null, out touchPosition);
            StartTouch(permanent ? joystick.localPosition : touchPosition);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!enableTouchMovement)
                return;

            EndTouch();
        }

        private Vector2 initialTouchPosition;

        public void StartTouch(Vector2 touchPosition)
        {
            if (!enableTouchMovement || Time.timeScale <= 0)
                return;

            beingTouched = true;
            initialTouchPosition = touchPosition;
            joystick.localPosition = initialTouchPosition;
            joystickBounds.localPosition = initialTouchPosition;
            joystickBounds.sizeDelta = Vector2.one * joystickRadius * 2;
            if (showJoystickVisual)
            {
                joystick.gameObject.SetActive(true);
                joystickBounds.gameObject.SetActive(true);
            }
            onStartTouch.Invoke();
        }

        public void UpdateTouch(Vector2 touchPosition)
        {
            if (!enableTouchMovement)
                return;

            Vector2 joystickDelta = touchPosition - initialTouchPosition;
            Vector2 moveDirection = joystickDelta.normalized;
            joystick.localPosition = joystickDelta.magnitude > joystickRadius
                ? initialTouchPosition + moveDirection * joystickRadius
                : touchPosition;
            onJoystickMoved.Invoke(moveDirection);
        }

        public void EndTouch()
        {
            if (!beingTouched)
                return;

            joystick.localPosition = joystickBounds.localPosition;
            if (enableTouchMovement && showJoystickVisual)
            {
                joystick.gameObject.SetActive(permanent);
                joystickBounds.gameObject.SetActive(permanent);
            }
            else
            {
                if (joystick != null)
                    joystick.gameObject.SetActive(false);
                if (joystickBounds != null)
                    joystickBounds.gameObject.SetActive(false);
            }

            onJoystickMoved.Invoke(Vector2.zero);
            onEndTouch.Invoke();
            beingTouched = false;
        }
    }
}
