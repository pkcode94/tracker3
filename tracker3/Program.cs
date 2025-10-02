using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Tracker.nn;
using Tracker.Filters;

namespace Tracker
{
    public static class IdManager
    {
        private static int _id = 1;
        public static int GetId() => _id++;
    }

    public class Pattern
    {
        public List<int> Sequence { get; } = new();
    }

    public class ConversationTree
    {
        public List<ConversationTree> Children { get; } = new();
        public Pattern NodePattern { get; set; }

        public void BuildTree(List<Pattern> patterns)
        {
            foreach (var pattern in patterns)
            {
                var child = new ConversationTree { NodePattern = pattern };
                Children.Add(child);
                var remaining = patterns.Where(p => p != pattern).ToList();
                child.BuildTree(remaining);
            }
        }
    }

    public class FilterEngine
    {
        private readonly List<PatternFilter> _filters = new();

        public void AddFilter(PatternFilter filter) => _filters.Add(filter);

        public IO Process(IO input)
        {
            foreach (var filter in _filters)
                input = filter.Apply(input);
            return input;
        }
    }

    public class TrackerEngine
    {
        private const int MaxPatternLength = 100;
        private const int MAX_SCRAPE_RETRIES = 3;
        private readonly HttpClient httpClient = new();
        private readonly List<nn.MainProcessor> _processors = new();
        private readonly Dictionary<string, int> _wordToId = new();
        private readonly Pattern _totalPattern = new();
        private readonly List<Pattern> _subpatterns = new();
        private readonly Random _random = new();
        private readonly string _url;
        private readonly bool _scrapingMode;
        public CancellationTokenSource Cancellation { get; } = new();

        public TrackerEngine(string url, bool scrapingMode)
        {
            _url = url;
            _scrapingMode = scrapingMode;
            for (int i = 0; i < 4; i++)
                _processors.Add(new nn.MainProcessor());
        }

        public async Task RunAsync(CancellationToken token)
        {
            int iteration = 0;
            while (!token.IsCancellationRequested)
            {
                iteration++;
                Console.WriteLine($"\n--- Iteration {iteration} ---");
                var words = _scrapingMode
                    ? await ScrapeWebPageAsync(_url, MaxPatternLength)
                    : GenerateMockWords(MaxPatternLength);

                Console.WriteLine($"Wörter ({words.Count}): {string.Join(", ", words)}");

                foreach (var word in words)
                {
                    if (!_wordToId.ContainsKey(word))
                    {
                        _wordToId[word] = IdManager.GetId();
                        Console.WriteLine($"Neues Wort erkannt: '{word}' -> ID {_wordToId[word]}");
                    }
                    _totalPattern.Sequence.Add(_wordToId[word]);
                }

                Console.WriteLine($"Pattern-Länge: {_totalPattern.Sequence.Count}");
                Console.WriteLine("Pattern-IDs: " + string.Join(", ", _totalPattern.Sequence));

                RecalculateSubpatterns();
                Console.WriteLine($"Subpattern-Anzahl: {_subpatterns.Count}");

                // Noise in die Zeitreihe einfügen
                var noisyVector = _totalPattern.Sequence
                    .Select(id => id + (int)Math.Round((_random.NextDouble() - 0.5) * 2)) // ±1 Noise
                    .ToArray();

                Console.WriteLine("Noisy Pattern-IDs: " + string.Join(", ", noisyVector));

                // Muster als IO in Prozessoren einspeisen (Trainingsschritt)
                var ioNoisy = new IO { IOVECTOR = noisyVector };
                foreach (var processor in _processors)
                {
                    processor.inputseries.Add(ioNoisy);
                }

                // Für die Prediction das unveränderte Pattern verwenden
                var ioClean = new IO { IOVECTOR = _totalPattern.Sequence.ToArray() };

                for (int i = 0; i < _processors.Count; i++)
                {
                    Console.WriteLine($"Prozessor {i}: gespeicherte Patterns = {_processors[i].GetStoredPatternCount()}");
                    if (_processors[i].HasKnownPatterns())
                    {
                        var sums = _processors[i].GetRecognizedPatternSums().ToList();
                        Console.WriteLine($"Prozessor {i}: Pattern-Summen = {string.Join(", ", sums)}");
                        Console.WriteLine($"Prozessor {i}: Prediction (clean) = [{string.Join(", ", ioClean.IOVECTOR)}]");
                        
                        var cleanWords = TranslateIdsToWords(ioClean.IOVECTOR);
                        Console.WriteLine($"Prozessor {i}: Prediction (clean, Wörter) = [{string.Join(", ", cleanWords)}]");

                        // Wahrscheinlichkeit berechnen und anzeigen
                        double probability = 0;
                        if (_processors[i].GetStoredPatternCount() > 0)
                        {
                            // Beispiel: Anteil der IDs im clean-Pattern, die im gespeicherten Pattern vorkommen
                            var storedIds = _processors[i].inputseries.SelectMany(io => io.IOVECTOR).ToHashSet();
                            int matchCount = ioClean.IOVECTOR.Count(id => storedIds.Contains(id));
                            probability = (double)matchCount / ioClean.IOVECTOR.Length;
                        }
                        Console.WriteLine($"Prozessor {i}: Wahrscheinlichkeit (clean) = {probability:P2}");
                    }
                    else
                    {
                        Console.WriteLine($"Prozessor {i}: Keine bekannten Patterns für Prediction.");
                    }
                }

                // Neue Methode in TrackerEngine zum Berechnen einer Wahrscheinlichkeitsmatrix
                double[,] CalculateTransitionProbabilityMatrix()
                {
                    // Alle gespeicherten Muster aus allen Prozessoren zusammenfassen
                    var allPatterns = _processors.SelectMany(p => p.inputseries).Select(io => io.IOVECTOR).ToList();
                    if (allPatterns.Count == 0) return new double[0, 0];

                    // Alle IDs, die vorkommen
                    var uniqueIds = allPatterns.SelectMany(vec => vec).Distinct().OrderBy(id => id).ToList();
                    int n = uniqueIds.Count;
                    var idIndex = uniqueIds.Select((id, idx) => (id, idx)).ToDictionary(x => x.id, x => x.idx);

                    // Matrix initialisieren: [von][zu]
                    double[,] matrix = new double[n, n];
                    int[,] counts = new int[n, n];

                    // Übergänge zählen
                    foreach (var vec in allPatterns)
                    {
                        for (int i = 0; i < vec.Length - 1; i++)
                        {
                            int from = vec[i];
                            int to = vec[i + 1];
                            if (idIndex.ContainsKey(from) && idIndex.ContainsKey(to))
                                counts[idIndex[from], idIndex[to]]++;
                        }
                    }

                    // Zeilenweise normalisieren (Wahrscheinlichkeit von jedem "von" zu jedem "zu")
                    for (int i = 0; i < n; i++)
                    {
                        int rowSum = 0;
                        for (int j = 0; j < n; j++)
                            rowSum += counts[i, j];
                        for (int j = 0; j < n; j++)
                            matrix[i, j] = rowSum > 0 ? (double)counts[i, j] / rowSum : 0;
                    }

                    return matrix;
                }

                var probMatrix = CalculateTransitionProbabilityMatrix();
                if (probMatrix.GetLength(0) > 0)
                {
                    var uniqueIds = _processors.SelectMany(p => p.inputseries).SelectMany(io => io.IOVECTOR).Distinct().OrderBy(id => id).ToList();
                    Console.WriteLine("Wahrscheinlichkeitsmatrix (Übergang von ID zu ID):");
                    Console.Write("     ");
                    foreach (var id in uniqueIds)
                        Console.Write($"{id,5}");
                    Console.WriteLine();
                    for (int i = 0; i < probMatrix.GetLength(0); i++)
                    {
                        Console.Write($"{uniqueIds[i],5}");
                        for (int j = 0; j < probMatrix.GetLength(1); j++)
                            Console.Write($"{probMatrix[i, j],5:F2}");
                        Console.WriteLine();
                    }
                }

                await Task.Delay(1000, token);
            }
        }

        private List<string> GenerateMockWords(int count)
        {
            var result = new List<string>();
            for (int i = 0; i < count; i++)
                result.Add(i % 2 == 0 ? "A" : "B");
            return result;
        }

        private async Task<List<string>> GetWordsFromWebAsync(string url, int count)
        {
            // Dummy-Implementierung für Demo-Zwecke
            await Task.Delay(100);
            return Enumerable.Range(1, count).Select(i => $"Word{i}").ToList();
        }

        private async Task<List<string>> ScrapeWebPageAsync(string url, int limit)
        {
            var words = new List<string>();

            for (int retry = 0; retry < MAX_SCRAPE_RETRIES; retry++)
            {
                try
                {
                    if (retry > 0)
                    {
                        int delay = (int)Math.Pow(2, retry) * 1000;
                        Console.WriteLine($"[SCRAPE RETRY] Versuch {retry + 1}/{MAX_SCRAPE_RETRIES}. Warte {delay / 1000}s...");
                        await Task.Delay(delay);
                    }

                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string htmlContent = await response.Content.ReadAsStringAsync();

                    string[] rawTokens = htmlContent.Split(new string[] { "<a href=\"/?word=" }, StringSplitOptions.RemoveEmptyEntries);

                    var uniqueWords = new HashSet<string>();
                    foreach (string token in rawTokens)
                    {
                        string cleanedToken = token.Split(new string[] { "\">" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        if (cleanedToken.Length > 0)
                        {
                            if (uniqueWords.Add(cleanedToken))
                            {
                                words.Add(cleanedToken);
                            }
                        }
                    }
                    return words.Skip(6).Take(limit).ToList();
                }
                catch (Exception ex)
                {
                    if (retry == MAX_SCRAPE_RETRIES - 1)
                    {
                        Console.WriteLine($"[SCRAPE FAILED] Alle {MAX_SCRAPE_RETRIES} Versuche fehlgeschlagen. Fehler: {ex.Message}");
                        break;
                    }
                }
            }
            // Fallback: Simulierte Wörter
            return GenerateMockWords(limit);
        }

        private void RecalculateSubpatterns()
        {
            _subpatterns.Clear();
            int n = _totalPattern.Sequence.Count;
            for (int i = 1; i < (1 << Math.Min(n, 20)); i++)
            {
                var p = new Pattern();
                for (int j = 0; j < Math.Min(n, 20); j++)
                    p.Sequence.Add((i & (1 << j)) != 0 ? _totalPattern.Sequence[j] : -1);
                while (p.Sequence.Count < MaxPatternLength)
                    p.Sequence.Add(-1);
                _subpatterns.Add(p);
            }
        }

        private List<string> TranslateIdsToWords(IEnumerable<int> ids)
        {
            var idToWord = _wordToId.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            var words = new List<string>();
            foreach (var id in ids)
            {
                if (idToWord.TryGetValue(id, out var word))
                    words.Add(word);
                else
                    words.Add($"[Unbekannt:{id}]");
            }
            return words;
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("CQH-TED Tracker Engine gestartet.");
            Console.Write("Modus wählen: (S)crape oder (G)enerate: ");
            var mode = Console.ReadLine()?.Trim().ToUpperInvariant();
            bool scraping = mode != "G";
            string url = "";

            if (scraping)
            {
                Console.Write("Bitte geben Sie die URL zum Scrapen ein: ");
                url = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(url))
                {
                    Console.WriteLine("Keine URL eingegeben. Standard-URL wird verwendet.");
                    url = "https://example.com";
                }
            }

            var engine = new TrackerEngine(url, scraping);
            var task = engine.RunAsync(engine.Cancellation.Token);

            Console.WriteLine("Geben Sie 'STOP' ein, um zu beenden.");
            while (true)
            {
                if (Console.ReadLine()?.Trim().ToUpperInvariant() == "STOP")
                {
                    engine.Cancellation.Cancel();
                    break;
                }
            }
            await task;
            Console.WriteLine("Beendet.");
        }
    }
}


namespace Tracker.nn
{
    public class IO
    {
        public int[] IOVECTOR { get; set; }
    }

    public class MainProcessor
    {
        public List<IO> inputseries = new();
        public List<IO> outputseries = new();
        public Dictionary<int, bool> Switches { get; private set; } = new();

        private void Log(string msg, ConsoleColor color = ConsoleColor.Gray)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[MainProcessor] {msg}");
            Console.ForegroundColor = oldColor;
        }

        public void InitializeSwitches(IEnumerable<int> wordIds)
        {
            Switches = wordIds.ToDictionary(id => id, id => false);
            Log($"Switches initialisiert: {string.Join(", ", Switches.Keys)}", ConsoleColor.Cyan);
        }

        public void UpdateSwitches(int[] inputPattern)
        {
            foreach (var id in Switches.Keys.ToList())
                Switches[id] = inputPattern.Contains(id);
            Log("Switches aktualisiert.", ConsoleColor.Yellow);
        }

        public void DisplaySwitches(Dictionary<int, string> idToWord = null)
        {
            Console.WriteLine("Aktueller Switch-Zustand:");
            foreach (var kvp in Switches)
            {
                string word = idToWord != null && idToWord.TryGetValue(kvp.Key, out var w) ? w : kvp.Key.ToString();
                var color = kvp.Value ? ConsoleColor.Green : ConsoleColor.Red;
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine($"Wort-ID {kvp.Key} ({word}): {(kvp.Value ? "AN" : "AUS")}");
                Console.ForegroundColor = oldColor;
            }
        }

        public void AddInput(IO io)
        {
            if (io == null || io.IOVECTOR == null)
            {
                Log("Ungültiges IO-Objekt übergeben!", ConsoleColor.Red);
                return;
            }
            inputseries.Add(io);
            Log($"Neues IO hinzugefügt: [{string.Join(", ", io.IOVECTOR)}]", ConsoleColor.Magenta);
        }

        public void BruteForceCombinatorially(bool bruteForceContext, int partnerId, Dictionary<int, string> idToWord = null)
        {
            if (!bruteForceContext || Switches.Count == 0)
            {
                Log("Brute-Force-Kontext nicht aktiviert oder keine Switches vorhanden.", ConsoleColor.Red);
                return;
            }

            Log("Brute-Force-Suche nach Switch-Triggern:", ConsoleColor.Blue);
            foreach (var switchId in Switches.Keys)
            {
                string word = idToWord != null && idToWord.TryGetValue(switchId, out var w) ? w : switchId.ToString();
                var matchingPatterns = inputseries.Where(io => io.IOVECTOR.Contains(switchId)).ToList();

                if (matchingPatterns.Count > 0)
                {
                    Log($"Switch für Wort-ID {switchId} ({word}) wird durch {matchingPatterns.Count} Pattern(s) aktiviert:", ConsoleColor.Green);
                    foreach (var io in matchingPatterns)
                    {
                        Log($"  Pattern: [{string.Join(", ", io.IOVECTOR)}]", ConsoleColor.DarkGreen);
                    }
                }
                else
                {
                    Log($"Switch für Wort-ID {switchId} ({word}) wurde durch kein Pattern aktiviert.", ConsoleColor.Red);
                }
            }
            Log("Brute-Force-Suche abgeschlossen.\n", ConsoleColor.Blue);
        }

        public void InitializeSwitches(int numDimensions, int numContexts, int numTimes, int maxPatternLength)
        {
            // Initialisierungscode hier
        }

        public int GetPossibilitySpaceCount(IO input, bool bruteForceContext, int partnerId)
        {
            // Erkennungscode hier
            return 0;
        }

        public int GetStoredPatternCount()
        {
            return inputseries.Count;
        }

        public IEnumerable<int> GetRecognizedPatternSums()
        {
            return inputseries.Select(io => io.IOVECTOR.Where(id => id > 0).Sum());
        }

        public IO GenerateKnownPattern(int partnerId)
        {
            return inputseries.FirstOrDefault() ?? new IO { IOVECTOR = new int[0] };
        }

        public bool HasKnownPatterns()
        {
            return inputseries.Any();
        }

        public bool IsPatternRecognizedTimeInvariant(int[] pattern)
        {
            // Zeitinvariante Erkennung
            return false;
        }
    }
}

