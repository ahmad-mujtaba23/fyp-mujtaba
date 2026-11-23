using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Osmosis
{
    /// The lab's step machine (task 4.8; replaces the reference's 926-line GameObjectsManager switch).
    /// One cycle per strip: strip on ruler -> measure initial -> beaker on bench -> fill -> strip into beaker ->
    /// Fast Forward -> strip back on ruler -> measure final -> record. Each step unlocks only the right shelf slot,
    /// shows only the right ghost, types its instruction on the blackboard, and on completion plays the reference
    /// "correct" feedback. Readings store the student's own measured values.
    public class LabFlow : MonoBehaviour
    {
        public enum Step { StripOnRuler, MeasureInitial, BeakerOnBench, Fill, StripIntoBeaker, FastForward, StripBackOnRuler, MeasureFinal, Done }
        public const int Strips = 3;

        [SerializeField] InstructionBoard board;
        [SerializeField] InventorySlot potatoSlot, beakerSlot;
        [SerializeField] SnapTarget rulerTarget;
        [SerializeField] SnapTarget[] beakerTargets;
        [SerializeField] ConcentrationPanel panel;
        [SerializeField] TimeLapseManager timeLapse;
        [SerializeField] MeasurementTool measure;
        [SerializeField] GameObject feedbackCorrect;
        [SerializeField] PulseScale readingsPulse;          // Readings button invites a tap when all strips are done

        public Step Current { get; private set; }
        public int Cycle { get; private set; }                          // 0-based strip number
        public IReadOnlyList<Reading> Readings => readings;

        readonly List<Reading> readings = new List<Reading>();
        PotatoStripController strip;
        BeakerController beaker;
        float initialRead;

        void OnEnable()
        {
            SnapTarget.Snapped += OnSnapped;
            panel.Filled += OnFilled;
            timeLapse.Finished += OnTimeLapseDone;
        }

        void OnDisable()
        {
            SnapTarget.Snapped -= OnSnapped;
            panel.Filled -= OnFilled;
            timeLapse.Finished -= OnTimeLapseDone;
        }

        void Start() => Enter(Step.StripOnRuler);

        void Enter(Step step)
        {
            Current = step;
            switch (step)
            {
                case Step.StripOnRuler:
                    rulerTarget.Show();
                    potatoSlot.Unlock();
                    Say("Take a potato strip from the shelf and place it on the ruler.");
                    break;
                case Step.MeasureInitial:
                    Say("Read the length of the strip on the ruler and record it.");
                    measure.Begin(strip, "Initial", v => { initialRead = v; Complete(Step.BeakerOnBench); });
                    break;
                case Step.BeakerOnBench:
                    beakerTargets[Cycle].Show();
                    beakerSlot.Unlock();
                    Say("Take a beaker from the shelf and place it on the bench.");
                    break;
                case Step.Fill:
                    panel.Open(beaker);
                    Say("Choose a sugar concentration with the slider, then fill the beaker.");
                    break;
                case Step.StripIntoBeaker:
                    beaker.StripTarget.Show();
                    SetStripDraggable(true);
                    Say("Put the potato strip into the beaker.");
                    break;
                case Step.FastForward:
                    SetStripDraggable(false);
                    timeLapse.Prepare(strip, beaker);
                    Say("Press Fast Forward to let 30 minutes pass.");
                    break;
                case Step.StripBackOnRuler:
                    rulerTarget.Show();
                    SetStripDraggable(true);
                    Say("Take the strip out of the beaker and place it back on the ruler.");
                    break;
                case Step.MeasureFinal:
                    Say("Read the new length of the strip and record it.");
                    measure.Begin(strip, "Final", Record);
                    break;
                case Step.Done:
                    SaveService.Save(new SaveData { readings = new List<Reading>(readings) });
                    board.Show("Well done! You tested " + Strips + " strips.\nOpen Readings to plot your graph.");
                    if (readingsPulse) readingsPulse.enabled = true;
                    break;
            }
        }

        void Say(string instruction) => board.Show($"Strip {Cycle + 1} of {Strips}\n{instruction}");

        void Complete(Step next)
        {
            Sfx.Play(Sfx.Correct);
            if (feedbackCorrect) StartCoroutine(Flash(feedbackCorrect));
            Enter(next);
        }

        void OnSnapped(SnapTarget t, Draggable d)
        {
            switch (Current)
            {
                case Step.StripOnRuler when t == rulerTarget:
                    strip = d.GetComponent<PotatoStripController>();
                    rulerTarget.Hide();
                    SetStripDraggable(false);
                    Complete(Step.MeasureInitial);
                    break;
                case Step.BeakerOnBench when t == beakerTargets[Cycle]:
                    beaker = d.GetComponent<BeakerController>();
                    d.canDrag = false;                               // beakers stay where they are put, and their
                    d.GetComponent<Collider>().enabled = false;      // touch box must not swallow clicks on the strip inside
                    t.Hide();
                    Complete(Step.Fill);
                    break;
                case Step.StripIntoBeaker when t == beaker.StripTarget:
                    t.Hide();
                    Complete(Step.FastForward);
                    break;
                case Step.StripBackOnRuler when t == rulerTarget:
                    rulerTarget.Hide();
                    SetStripDraggable(false);
                    Complete(Step.MeasureFinal);
                    break;
            }
        }

        void OnFilled(BeakerController b)
        {
            if (Current == Step.Fill && b == beaker) Complete(Step.StripIntoBeaker);
        }

        void OnTimeLapseDone(PotatoStripController s)
        {
            if (Current == Step.FastForward && s == strip) Complete(Step.StripBackOnRuler);
        }

        void Record(float finalRead)
        {
            readings.Add(new Reading { concentration = beaker.Concentration, initial = initialRead, final = finalRead });
            StartCoroutine(PutAside(strip.GetComponent<Draggable>(), beaker));
            strip = null;
            Cycle++;
            Complete(Cycle < Strips ? Step.StripOnRuler : Step.Done);
        }

        /// Leave the measured strip on the ruler a moment, then back into its own beaker so the ruler is free.
        static IEnumerator PutAside(Draggable d, BeakerController b)
        {
            d.canDrag = false;
            yield return new WaitForSeconds(1.2f);
            b.StripTarget.Show();
            d.SnapTo(b.StripTarget);
            b.StripTarget.Hide();
        }

        void SetStripDraggable(bool on)
        {
            if (strip) strip.GetComponent<Draggable>().canDrag = on;
        }

        static IEnumerator Flash(GameObject go)
        {
            go.SetActive(true);
            go.transform.DOKill(true);
            go.transform.localScale = Vector3.one * 0.6f;
            go.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(1f);
            go.SetActive(false);
        }
    }
}
