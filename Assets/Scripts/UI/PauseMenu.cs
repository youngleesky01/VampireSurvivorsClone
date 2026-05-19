using UnityEngine;
using UnityEngine.UI;

namespace Vampire
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private Image pauseButton;
        [SerializeField] private Sprite pauseSprite, playSprite;
        [SerializeField] private GameObject pauseMenu;
        private bool paused = false;
        private bool timeIsFrozen = false;

        public bool TimeIsFrozen { set => timeIsFrozen = value; }

        public void PlayPause()
        {
            if (paused = !paused)
            {
                if (!timeIsFrozen)
                    GameTimeController.PushFreezeSafe();
                pauseButton.sprite = playSprite;
                pauseMenu.SetActive(true);
            }
            else
            {
                if (!timeIsFrozen)
                    GameTimeController.PopFreezeSafe();
                pauseButton.sprite = pauseSprite;
                pauseMenu.SetActive(false);
            }
        }
    }
}
