using UnityEngine;

namespace Osmosis
{
    /// Port of MainMenuHandler + ExitMenuHandler + ExperimentStarter: the scene-level button
    /// targets, wired from button OnClick in the Inspector. One component, any scene.
    public class MenuActions : MonoBehaviour
    {
        // Main menu
        public void Play()
        {
            if (!Sfx.IsPlaying(Sfx.Music)) Sfx.Play(Sfx.Music);
            SceneLoader.Load(Scenes.Guidance);
        }

        public void Quit() => Application.Quit();

        // Exit dialog "Yes", Result window "Quit"
        public void ToMenu() => SceneLoader.Load(Scenes.Menu);

        // Guidance "Got it": reference stops the music once the lab starts
        public void StartLab()
        {
            Sfx.Stop(Sfx.Music);
            SceneLoader.Load(Scenes.Lab);
        }

        // Result window "Again": restarts the lab (reference bug sent this to the Menu)
        public void Again() => SceneLoader.Reload();
    }
}
