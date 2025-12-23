using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using NaughtyAttributes;

namespace ApprovalMonster.Core
{
    public class SceneNavigator : MonoBehaviour
    {
        public static SceneNavigator Instance { get; private set; }



        [Header("UI Panels")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject mainGamePanel;
        [SerializeField] private GameObject resultPanel;
        
        [Header("Fade")]
        [SerializeField] private Image fadePanel;
        [SerializeField] private float fadeDuration = 0.5f;

        public long LastGameScore { get; set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad is not strictly needed for single scene if everything is in one root, 
                // but keeping it safe if we change minds. For now, let's keep it but it might warn if root.
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Initial State: Title
            ShowTitle();
        }

        public void GoToMain()
        {
            Debug.Log("[SceneNavigator] GoToMain called.");
            StartCoroutine(TransitionRoutine(mainGamePanel));
        }

        public void GoToTitle()
        {
             StartCoroutine(TransitionRoutine(titlePanel));
        }
        
        public void GoToResult()
        {
             StartCoroutine(TransitionRoutine(resultPanel));
        }
        
        // Direct method for initialization if needed
        public void ShowTitle()
        {
            if(fadePanel != null) fadePanel.gameObject.SetActive(false);
            SetPanel(titlePanel);
        }

        private void SetPanel(GameObject activePanel)
        {
            if (titlePanel != null) titlePanel.SetActive(titlePanel == activePanel);
            if (mainGamePanel != null) mainGamePanel.SetActive(mainGamePanel == activePanel);
            if (resultPanel != null) resultPanel.SetActive(resultPanel == activePanel);
        }

        private IEnumerator TransitionRoutine(GameObject targetPanel)
        {
            if (fadePanel != null)
            {
                fadePanel.gameObject.SetActive(true);
                fadePanel.DOFade(1f, fadeDuration);
                yield return new WaitForSeconds(fadeDuration);
            }
            
            // 1. Activate Panel First (so hierarchies are active and Awake runs correctly)
            SetPanel(targetPanel);
            
            // 2. Logic Trigger
            if (targetPanel == mainGamePanel)
            {
                // Reset Game
                GameManager.Instance.ResetGame();
                GameManager.Instance.StartGame();
            }

            if (fadePanel != null)
            {
                fadePanel.DOFade(0f, fadeDuration).OnComplete(() => fadePanel.gameObject.SetActive(false));
            }
        }
    }
}
