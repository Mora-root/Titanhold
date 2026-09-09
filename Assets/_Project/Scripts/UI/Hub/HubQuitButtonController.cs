using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Hub
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class HubQuitButtonController : MonoBehaviour
    {
        [SerializeField] private Button quitButton;

        public Button QuitButton => quitButton;

#if UNITY_EDITOR
        public void ConfigureForEditor(Button configuredButton)
        {
            quitButton = configuredButton;
        }
#endif

        private void Awake()
        {
            quitButton ??= GetComponent<Button>();
        }

        private void OnEnable()
        {
            quitButton ??= GetComponent<Button>();
            quitButton?.onClick.AddListener(Quit);
        }

        private void OnDisable()
        {
            quitButton?.onClick.RemoveListener(Quit);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
