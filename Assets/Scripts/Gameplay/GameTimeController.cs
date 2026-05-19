using UnityEngine;

namespace Vampire
{
    /// <summary>
    /// Central time-scale control: game speed (1x–5x) and freeze (pause / level-up menu).
    /// </summary>
    public class GameTimeController : MonoBehaviour
    {
        public static GameTimeController Instance { get; private set; }

        [SerializeField] private float[] speedMultipliers = { 1f, 2f, 3f, 4f, 5f };
        [SerializeField] private int defaultSpeedIndex = 0;

        private int currentSpeedIndex;
        private int freezeCount;

        public float CurrentSpeed => speedMultipliers[currentSpeedIndex];
        public int CurrentSpeedIndex => currentSpeedIndex;
        public bool IsFrozen => freezeCount > 0;
        public int SpeedOptionCount => speedMultipliers.Length;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            currentSpeedIndex = Mathf.Clamp(defaultSpeedIndex, 0, speedMultipliers.Length - 1);
            freezeCount = 0;
            ApplyTimeScale();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1f;
            }
        }

        public void SetSpeedIndex(int index)
        {
            currentSpeedIndex = Mathf.Clamp(index, 0, speedMultipliers.Length - 1);
            ApplyTimeScale();
        }

        public void SetSpeed1x() => SetSpeedIndex(0);
        public void SetSpeed2x() => SetSpeedIndex(1);
        public void SetSpeed3x() => SetSpeedIndex(2);
        public void SetSpeed4x() => SetSpeedIndex(3);
        public void SetSpeed5x() => SetSpeedIndex(4);

        public void CycleSpeed()
        {
            SetSpeedIndex((currentSpeedIndex + 1) % speedMultipliers.Length);
        }

        public void PushFreeze()
        {
            freezeCount++;
            ApplyTimeScale();
        }

        public void PopFreeze()
        {
            freezeCount = Mathf.Max(0, freezeCount - 1);
            ApplyTimeScale();
        }

        public void ApplyTimeScale()
        {
            Time.timeScale = freezeCount > 0 ? 0f : CurrentSpeed;
        }

        /// <summary>Call before loading another scene so time scale does not leak.</summary>
        public void ResetForSceneLoad()
        {
            freezeCount = 0;
            currentSpeedIndex = Mathf.Clamp(defaultSpeedIndex, 0, speedMultipliers.Length - 1);
            Time.timeScale = 1f;
        }

        public static void PushFreezeSafe()
        {
            if (Instance != null)
                Instance.PushFreeze();
            else
                Time.timeScale = 0f;
        }

        public static void PopFreezeSafe()
        {
            if (Instance != null)
                Instance.PopFreeze();
            else
                Time.timeScale = 1f;
        }
    }
}
