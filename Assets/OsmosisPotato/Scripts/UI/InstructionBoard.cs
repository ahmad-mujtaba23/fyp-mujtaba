using System.Collections;
using TMPro;
using UnityEngine;

namespace Osmosis
{
    /// Port of InstructionsController: types a step onto the blackboard with the typing loop,
    /// then pulses the board for a few seconds. Text comes from the step list instead of a
    /// hard-coded static array. Uses maxVisibleCharacters, so typing allocates nothing per frame.
    public class InstructionBoard : MonoBehaviour
    {
        [SerializeField] TMP_Text text;                 // TextMeshPro (world) or TextMeshProUGUI
        [SerializeField] float secondsPerChar = 0.03f;
        [SerializeField] float pulseSeconds = 3f;
        [SerializeField] PulseScale pulse;              // optional, on the board object
        Coroutine running;

        public void Show(string instruction)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Type(instruction));
        }

        IEnumerator Type(string instruction)
        {
            if (pulse) pulse.enabled = false;
            text.text = instruction;
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();
            int total = text.textInfo.characterCount;

            Sfx.Play(Sfx.Typing);
            var wait = new WaitForSeconds(secondsPerChar);
            for (int i = 1; i <= total; i++)
            {
                text.maxVisibleCharacters = i;
                yield return wait;
            }
            Sfx.Stop(Sfx.Typing);

            if (pulse)
            {
                pulse.enabled = true;
                yield return new WaitForSeconds(pulseSeconds);
                pulse.enabled = false;
            }
            running = null;
        }

        void OnDisable() => Sfx.Stop(Sfx.Typing);
    }
}
