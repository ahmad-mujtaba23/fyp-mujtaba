using TMPro;
using UnityEngine;

namespace Osmosis
{
    /// Settings popup (task 2.2): music and sound effects on/off. Nothing else (YAGNI).
    public class SettingsView : MonoBehaviour
    {
        [SerializeField] TMP_Text musicLabel, soundLabel;

        void OnEnable() => Refresh();

        public void ToggleMusic() { Sfx.MusicOn = !Sfx.MusicOn; Refresh(); }
        public void ToggleSound() { Sfx.EffectsOn = !Sfx.EffectsOn; Refresh(); }

        void Refresh()
        {
            musicLabel.text = Sfx.MusicOn ? "Music: ON" : "Music: OFF";
            soundLabel.text = Sfx.EffectsOn ? "Sound: ON" : "Sound: OFF";
        }
    }
}
