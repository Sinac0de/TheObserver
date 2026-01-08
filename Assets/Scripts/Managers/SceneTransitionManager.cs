using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Handles smooth scene transitions with loading screens
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private UnityEngine.UI.Slider progressBar;
    [SerializeField] private UnityEngine.UI.Text progressText;

    [Header("Transition Settings")]
    [SerializeField] private float minLoadingTime = 1f;
    [SerializeField] private AnimationCurve loadingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private AsyncOperation asyncOperation;
    private bool isLoading = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Load a scene asynchronously with loading screen
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        if (isLoading) return;

        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        isLoading = true;

        // Show loading screen
        ShowLoadingScreen();

        // Start loading the scene
        asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;

        float startTime = Time.time;
        float progress = 0f;

        // Simulate progress bar until minimum loading time
        while (Time.time - startTime < minLoadingTime)
        {
            progress = (Time.time - startTime) / minLoadingTime;
            UpdateProgress(progress);
            yield return null;
        }

        // Wait for scene to finish loading (but continue showing progress)
        while (!asyncOperation.isDone)
        {
            // Calculate progress: 90% for loading, 10% for activation
            float calculatedProgress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            UpdateProgress(calculatedProgress);

            if (asyncOperation.progress >= 0.9f)
            {
                // Allow scene activation when fully loaded
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }

        HideLoadingScreen();
        isLoading = false;
    }

    private void UpdateProgress(float progress)
    {
        // Apply curve to make loading feel smoother
        float curvedProgress = loadingCurve.Evaluate(progress);

        if (progressBar != null)
            progressBar.value = curvedProgress;

        if (progressText != null)
            progressText.text = Mathf.RoundToInt(curvedProgress * 100f) + "%";
    }

    private void ShowLoadingScreen()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);
    }

    private void HideLoadingScreen()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}