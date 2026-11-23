using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Osmosis
{
    public static class Scenes
    {
        // Prefixed: this practical is merged with other practicals into one project.
        public const string Menu = "Osmosis_Menu", Guidance = "Osmosis_Guidance", Lab = "Osmosis_Lab";
    }

    /// Port of the reference LevelLoader (cross-fade then load), with two fixes:
    /// scenes are loaded by name (the reference's "Again" wrapped to the Menu via buildIndex+1 % 3),
    /// and the fade overlay builds itself, so no LevelLoader prefab is needed per scene.
    public class SceneLoader : MonoBehaviour
    {
        const float FadeTime = 0.4f;

        static SceneLoader instance;
        CanvasGroup fade;
        bool loading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            Application.targetFrameRate = 60;
            EnhancedTouchSupport.Enable();
        }

        public static void Load(string scene)
        {
            if (instance == null)
            {
                var go = new GameObject("[SceneLoader]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<SceneLoader>();
                instance.BuildOverlay();
            }
            if (!instance.loading) instance.StartCoroutine(instance.Run(scene));
        }

        public static void Reload() => Load(SceneManager.GetActiveScene().name);

        void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            fade = gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 0f;
            fade.blocksRaycasts = false;
            gameObject.AddComponent<GraphicRaycaster>();

            var img = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(transform, false);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.GetComponent<Image>().color = Color.black;
        }

        IEnumerator Run(string scene)
        {
            loading = true;
            fade.blocksRaycasts = true;
            yield return Fade(0f, 1f);
            yield return SceneManager.LoadSceneAsync(scene);
            yield return Fade(1f, 0f);
            fade.blocksRaycasts = false;
            loading = false;
        }

        IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
            {
                fade.alpha = Mathf.Lerp(from, to, t / FadeTime);
                yield return null;
            }
            fade.alpha = to;
        }
    }
}
