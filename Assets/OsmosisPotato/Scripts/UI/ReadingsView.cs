using System.Globalization;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Osmosis
{
    /// Readings window (M5): table of the student's readings, animated graph, and once all strips are done the
    /// isotonic question. A correct answer marks the point on the graph, opens the result card and saves.
    public class ReadingsView : MonoBehaviour
    {
        public const float IsotonicTolerance = 2f;          // % sugar

        [SerializeField] PopupPanel popup;
        [SerializeField] TMP_Text[] cells;                  // row-major, LabFlow.Strips rows x 5 columns
        [SerializeField] Image[] rowBackgrounds;
        [SerializeField] GraphView graph;
        [SerializeField] GameObject question;
        [SerializeField] Slider guess;                       // full-height line dragged across the graph (0–50 %)
        [SerializeField] TMP_Text guessLabel, message;
        [SerializeField] Button check;
        [SerializeField] PopupPanel resultCard;
        [SerializeField] TMP_Text resultBody;
        [SerializeField] LabFlow flow;
        [SerializeField] ReadingsBadge badge;
        [SerializeField] PulseScale buttonPulse;

        int shownRows;
        bool hasEstimate;
        float estimate;

        void Awake() => guess.onValueChanged.AddListener(_ => ShowGuess());

        /// The student's reading of the crossing, to the nearest 0.5 %.
        public float Guess => Mathf.Round(guess.value * 2f) / 2f;
        void ShowGuess() => guessLabel.text = Guess.ToString("0.0", CultureInfo.InvariantCulture) + " %";

        /// Readings button target.
        public void Open()
        {
            if (badge) badge.Clear();
            if (buttonPulse) buttonPulse.enabled = false;
            popup.Open();
            Refresh();
        }

        void Refresh()
        {
            var r = flow.Readings;
            int cols = 5;
            for (int row = 0; row < cells.Length / cols; row++)
            {
                bool has = row < r.Count;
                for (int c = 0; c < cols; c++) cells[row * cols + c].text = has ? Cell(r[row], c) : "";
                if (has && row >= shownRows)                     // new row: brief highlight (reference outline flash)
                {
                    rowBackgrounds[row].DOKill();
                    rowBackgrounds[row].color = new Color(1f, 0.93f, 0.55f);
                    rowBackgrounds[row].DOColor(Color.white, 1.2f).SetUpdate(true);
                }
            }
            shownRows = r.Count;
            graph.Plot(r);

            bool done = flow.Current == LabFlow.Step.Done;
            question.SetActive(done);
            guess.gameObject.SetActive(done);
            if (!done) return;
            var arr = new Reading[r.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = r[i];
            hasEstimate = OsmosisSolver.TryIsotonic(arr, out estimate);
            check.interactable = hasEstimate;
            guess.interactable = hasEstimate;
            guess.value = OsmosisSolver.MaxPercent / 2f;
            ShowGuess();
            message.text = hasEstimate ? "" :
                "Your readings never cross zero. Next time test one weak (below 10 %) and one strong solution.";
        }

        static string Cell(Reading x, int c)
        {
            var inv = CultureInfo.InvariantCulture;
            switch (c)
            {
                case 0: return x.concentration.ToString("0", inv);
                case 1: return x.initial.ToString("0.0", inv);
                case 2: return x.final.ToString("0.0", inv);
                case 3: return x.Change.ToString("+0.0;-0.0;0.0", inv);
                default: return (x.Change / x.initial * 100f).ToString("+0.0;-0.0;0.0", inv);
            }
        }

        /// Close (X) button target.
        public void Close() => popup.Close();

        /// Check button target: where does the line cross zero?
        public void CheckIsotonic()
        {
            if (!hasEstimate) return;
            float said = Guess;
            if (Mathf.Abs(said - estimate) > IsotonicTolerance + 1e-4f)
            {
                message.text = "Look again: drag the line to where the graph crosses the zero line.";
                Sfx.Play(Sfx.Wrong);
                return;
            }
            Sfx.Play(Sfx.Correct);
            guess.interactable = false;
            graph.MarkIsotonic(estimate);
            message.text = "";
            var data = SaveService.Load();
            data.readings = new System.Collections.Generic.List<Reading>(flow.Readings);
            data.hasBestIsotonic = true;
            data.bestIsotonic = said;
            SaveService.Save(data);
            resultBody.text = ResultText(said);
            resultCard.Open();
        }

        string ResultText(float said)
        {
            var sb = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;
            foreach (var x in flow.Readings)
                sb.Append(x.concentration.ToString("0", inv)).Append(" %:  ")
                  .Append(x.Change.ToString("+0.0;-0.0;0.0", inv)).Append(" mm  ").Append(Observation(x.Change)).Append('\n');
            sb.Append("\nIsotonic point: you said ").Append(said.ToString("0.#", inv))
              .Append(" %, your graph gives ").Append(estimate.ToString("0.#", inv)).Append(" %.\n")
              .Append("Real potato cell sap is about ").Append(OsmosisSolver.IsotonicPercent.ToString("0", inv)).Append(" % sugar (0.3 M).");
            return sb.ToString();
        }

        public static string Observation(float changeMm) =>
            changeMm > 0.5f ? "Turgid (gained water)" :
            changeMm < -0.5f ? "Flaccid / plasmolysed (lost water)" : "No change (isotonic)";
    }
}
