using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Tracker.nn;
using Tracker.Filters;

namespace Tracker
{
    public partial class frmMain : Form
    {
        private MainProcessor processor = new MainProcessor();
        private FilterEngine filterEngine = new FilterEngine();

        private Dictionary<string, int> wordToId = new();
        private Pattern totalPattern = new();
        private List<Pattern> subpatterns = new();

        private ListBox lstPatterns;
        private ListView lstSwitches;
        private ComboBox cmbFilters;
        private Button btnApplyFilter;
        private ListBox lstWords;
        private ListBox lstSubpatterns;

        public frmMain()
        {
            InitializeComponent();
            InitControls();
            LoadExampleData();
            UpdateUI();
        }

        private void InitControls()
        {
            this.Text = "Tracker Debugger";
            this.Size = new Size(1100, 600);

            lstPatterns = new ListBox { Top = 10, Left = 10, Width = 350, Height = 400 };
            lstSwitches = new ListView { Top = 10, Left = 370, Width = 200, Height = 400, View = View.Details };
            lstSwitches.Columns.Add("Wort-ID", 80);
            lstSwitches.Columns.Add("Status", 80);

            lstWords = new ListBox { Top = 10, Left = 580, Width = 200, Height = 400 };
            lstSubpatterns = new ListBox { Top = 10, Left = 790, Width = 280, Height = 400 };

            cmbFilters = new ComboBox { Top = 420, Left = 10, Width = 200 };
            cmbFilters.Items.AddRange(new string[] {
                "NoiseFilter",
                "DuplicateRemovalFilter",
                "NormalizationFilter",
                "SlidingWindowFilter",
                "PatternLengthFilter",
                "OutlierRemovalFilter",
                "FrequencyFilter",
                "ContextSwitcherFilter",
                "TreeBuilderFilter"
            });
            cmbFilters.SelectedIndex = 0;

            btnApplyFilter = new Button { Top = 420, Left = 220, Width = 120, Text = "Filter anwenden" };
            btnApplyFilter.Click += BtnApplyFilter_Click;

            this.Controls.Add(lstPatterns);
            this.Controls.Add(lstSwitches);
            this.Controls.Add(lstWords);
            this.Controls.Add(lstSubpatterns);
            this.Controls.Add(cmbFilters);
            this.Controls.Add(btnApplyFilter);
        }

        private void LoadExampleData()
        {
            // Beispielw�rter und IDs
            var words = new[] { "A", "B", "C", "D" };
            foreach (var word in words)
            {
                if (!wordToId.ContainsKey(word))
                    wordToId[word] = IdManager.GetId();
            }

            processor.InitializeSwitches(wordToId.Values);

            // Beispiel-Pattern
            totalPattern.Sequence.AddRange(new[] { wordToId["A"], wordToId["B"], wordToId["C"] });
            totalPattern.Sequence.Add(wordToId["B"]);
            totalPattern.Sequence.Add(wordToId["D"]);

            processor.AddInput(new IO { IOVECTOR = totalPattern.Sequence.ToArray() });
            processor.UpdateSwitches(totalPattern.Sequence.ToArray());

            RecalculateSubpatterns();
        }

        private void UpdateUI()
        {
            lstPatterns.Items.Clear();
            foreach (var io in processor.inputseries)
                lstPatterns.Items.Add(string.Join(", ", io.IOVECTOR));

            lstSwitches.Items.Clear();
            foreach (var kvp in processor.Switches)
            {
                var item = new ListViewItem(kvp.Key.ToString());
                item.SubItems.Add(kvp.Value ? "AN" : "AUS");
                item.BackColor = kvp.Value ? Color.LightGreen : Color.LightCoral;
                lstSwitches.Items.Add(item);
            }

            lstWords.Items.Clear();
            foreach (var kvp in wordToId)
                lstWords.Items.Add($"{kvp.Key} -> {kvp.Value}");

            lstSubpatterns.Items.Clear();
            foreach (var p in subpatterns.Take(50)) // Nur die ersten 50 anzeigen
                lstSubpatterns.Items.Add(string.Join(", ", p.Sequence));
        }

        private void BtnApplyFilter_Click(object sender, EventArgs e)
        {
            PatternFilter filter = cmbFilters.SelectedItem switch
            {
                "NoiseFilter" => new NoiseFilter(1),
                "DuplicateRemovalFilter" => new DuplicateRemovalFilter(),
                "NormalizationFilter" => new NormalizationFilter(),
                "SlidingWindowFilter" => new SlidingWindowFilter(),
                "PatternLengthFilter" => new PatternLengthFilter(),
                "OutlierRemovalFilter" => new OutlierRemovalFilter(),
                "FrequencyFilter" => new FrequencyFilter(),
                "ContextSwitcherFilter" => new ContextSwitcherFilter(),
                "TreeBuilderFilter" => new TreeBuilderFilter(),
                _ => null
            };
            if (filter != null)
            {
                filterEngine.AddFilter(filter);
                for (int i = 0; i < processor.inputseries.Count; i++)
                    processor.inputseries[i] = filterEngine.Process(processor.inputseries[i]);
                UpdateUI();
            }
        }

        private void RecalculateSubpatterns()
        {
            subpatterns.Clear();
            int n = totalPattern.Sequence.Count;
            int maxLen = Math.Min(n, 20);
            for (int i = 1; i < (1 << maxLen); i++)
            {
                var p = new Pattern();
                for (int j = 0; j < maxLen; j++)
                    p.Sequence.Add((i & (1 << j)) != 0 ? totalPattern.Sequence[j] : -1);
                while (p.Sequence.Count < 100)
                    p.Sequence.Add(-1);
                subpatterns.Add(p);
            }
        }
    }
}
