using UnityEngine;
using UnityEngine.SceneManagement;

namespace CircuitPuzzle.Core
{
    /// <summary>Scene-local navigation for the foundation screens.</summary>
    public sealed class SceneNavigation : MonoBehaviour
    {
        private bool isLoading;

        // Buttons pass the scene asset path from their persistent UnityEvent.
        public void LoadScene(string scenePath)
        {
            if (isLoading)
                return;

            if (string.IsNullOrEmpty(scenePath) || !Application.CanStreamedLevelBeLoaded(scenePath))
            {
                Debug.LogError($"Scene is not enabled in the build scene list: {scenePath}", this);
                return;
            }

            isLoading = true;
            if (SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single) == null)
                isLoading = false;
        }
    }
}
