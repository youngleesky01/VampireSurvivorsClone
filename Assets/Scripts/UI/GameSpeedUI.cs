using UnityEngine;
using TMPro;

namespace Vampire
{
    /// <summary>
    /// UI / hotkeys for 1x–5x game speed. Hook buttons to the public OnSpeed* methods.
    /// </summary>
    public class GameSpeedUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI speedLabel;
        [SerializeField] private bool enableHotkeys = true;

        private void Update()
        {
            if (!enableHotkeys || GameTimeController.Instance == null)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                GameTimeController.Instance.SetSpeed1x();
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                GameTimeController.Instance.SetSpeed2x();
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                GameTimeController.Instance.SetSpeed3x();
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
                GameTimeController.Instance.SetSpeed4x();
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
                GameTimeController.Instance.SetSpeed5x();
            else if (Input.GetKeyDown(KeyCode.BackQuote))
                GameTimeController.Instance.CycleSpeed();

            RefreshLabel();
        }

        private void OnEnable()
        {
            RefreshLabel();
        }

        public void OnSpeed1x() => SetSpeedAndRefresh(0);
        public void OnSpeed2x() => SetSpeedAndRefresh(1);
        public void OnSpeed3x() => SetSpeedAndRefresh(2);
        public void OnSpeed4x() => SetSpeedAndRefresh(3);
        public void OnSpeed5x() => SetSpeedAndRefresh(4);
        public void OnCycleSpeed()
        {
            if (GameTimeController.Instance != null)
                GameTimeController.Instance.CycleSpeed();
            RefreshLabel();
        }

        private void SetSpeedAndRefresh(int index)
        {
            if (GameTimeController.Instance != null)
                GameTimeController.Instance.SetSpeedIndex(index);
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (speedLabel == null || GameTimeController.Instance == null)
                return;

            float speed = GameTimeController.Instance.CurrentSpeed;
            speedLabel.text = speed <= 1.01f ? "1x" : $"{speed:0}x";
        }
    }
}
