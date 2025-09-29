// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Http;

Console.WriteLine("Hello, World!");

namespace Tracker
{
    public class CoreEngine
    {
        // --- CQH-TED CORE COMPONENTS ---
        public List<nn.MainProcessor> nnProcessors = new List<nn.MainProcessor>();
        private CancellationTokenSource cts = new CancellationTokenSource();
        private static readonly HttpClient httpClient = new HttpClient();

        // Configuration
        private readonly string _targetUrl;
        private const int WORD_LIMIT_N = 3;
        private const bool BRUTE_FORCE_CONTEXT = true;

        // NN Parameters 
        private const int NUM_DIMENSIONS = 3;
        private const int NUM_CONTEXTS = 2;
        private const int NUM_TIMES = 1;
        private const int MAX_PATTERN_LENGTH = 10;
        private const int TARGET_PARTNER_ID = 0;
        private const int MAX_COMBINATORIAL_PATTERNS = 10;

        // --- Data Structures ---
        public static class idmanager
        {
            public static int id = 1;
            public static int getid() { return id++; }
        }

        public Dictionary<string, int> stringtoidmapping = new Dictionary<string, int>();

        public class pattern
        {
            public List<int> patternsequence = new List<int>();
        }
        public pattern totalpattern = new pattern();
        public List<pattern> subpatterns = new List<pattern>();

        /// <summary>
        /// Defines the conversation structure used for generating pattern combinations.
        /// </summary>
        public class conversationtree
        {
            public List<conversationtree> children = new List<conversationtree>();
            public pattern nodepattern;

            public void bruteforce(List<pattern> availablepatterns)
            {
                // Limiting recursion depth/complexity for safety
                if (availablepatterns.Count > 5) return;

                foreach (var p in availablepatterns)
                {
                    conversationtree child = new conversationtree();
                    child.nodepattern = p;
                    children.Add(child);

                    var newavailablepatterns = new List<pattern>(availablepatterns);
                    newavailablepatterns.Remove(p);

                    child.bruteforce(newavailablepatterns);
                }
            }
        }

        public CoreEngine(string targetUrl)
        {
            _targetUrl = targetUrl;
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Initialize two dedicated NN processors
            for (int i = 0; i < 2; i++)
            {
                var processor = new nn.MainProcessor();
                processor.InitializeSwitches(
                    NUM_DIMENSIONS,
                    NUM_CONTEXTS,
                    NUM_TIMES,
                    MAX_PATTERN_LENGTH
                );
                nnProcessors.Add(processor);
            }
        }

        // --- Pattern Generation Logic (L_exploit Synthesis) ---

        /// <summary>
        /// Generates subpatterns by inserting -1 placeholders (L_exploit Synthesis).
        /// </summary>
        public void recalculatesubpatternswithminusoneforplaceholderkeepallsetssamesizesfillemptyspaceswithminusone()
        {
            subpatterns.Clear();
            int n = totalpattern.patternsequence.Count;
            if (n == 0) return;

            int effectiveN = Math.Min(n, MAX_PATTERN_LENGTH);

            int patterncount = 1 << effectiveN;
            for (int i = 1; i < patterncount; i++)
            {
                pattern p = new pattern();
                for (int j = 0; j < effectiveN; j++)
                {
                    if ((i & (1 << j)) != 0)
                    {
                        p.patternsequence.Add(totalpattern.patternsequence[j]);
                    }
                    else
                    {
                        p.patternsequence.Add(-1);
                    }
                }

                // Pad the pattern to the required MAX_PATTERN_LENGTH
                while (p.patternsequence.Count < MAX_PATTERN_LENGTH)
                {
                    p.patternsequence.Add(-1);
                }
                subpatterns.Add(p);
            }
        }

        // Collects all unique, raw patterns from the generated tree for combinatorial assignment.
        private List<pattern> CollectRawPatterns(conversationtree root)
        {
            var rawPatterns = new List<pattern>();
            var queue = new Queue<conversationtree>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node.nodepattern != null)
                {
                    rawPatterns.Add(node.nodepattern);
                }
                foreach (var child in node.children)
                {
                    queue.Enqueue(child);
                }
            }
            // Limit the patterns used for combinatorial brute-forcing
            return rawPatterns.Take(MAX_COMBINATORIAL_PATTERNS).ToList();
        }

        // --- Core Partner Brute-Force and Training Logic (C-A PTA) ---

        private void BruteForceAndTrainAllPartners(List<pattern> rawPatterns)
        {
            int n = rawPatterns.Count;
            if (n == 0) return;

            int assignmentCombinations = 1 << n;

            for (int i = 0; i < assignmentCombinations; i++)
            {
                // 1. Clear Training Data for ALL dedicated NNs
                nnProcessors[0].inputseries.Clear();
                nnProcessors[0].outputseries.Clear();
                nnProcessors[1].inputseries.Clear();
                nnProcessors[1].outputseries.Clear();

                // 2. Apply current combinatorial assignment (bitmask 'i')
                for (int j = 0; j < n; j++)
                {
                    int assignedPartner = (i >> j) & 1; // 0 or 1
                    nn.IO ioPattern = new nn.IO { IOVECTOR = rawPatterns[j].patternsequence.ToArray() };

                    // Assign this pattern to the assigned partner's NN
                    nnProcessors[assignedPartner].inputseries.Add(ioPattern);
                    nnProcessors[assignedPartner].outputseries.Add(ioPattern);
                }

                // 3. C-A PTA Training: Brute-force internal switches for BOTH dedicated NNs
                nnProcessors[0].BruteForceCombinatorially(BRUTE_FORCE_CONTEXT);
                nnProcessors[1].BruteForceCombinatorially(BRUTE_FORCE_CONTEXT);
            }

            Console.WriteLine($"[LOG] Combinatorial Assignment Complete: Processed {assignmentCombinations} partner scenarios (2^{n}).");
        }

        // --- Core Asynchronous Reverse Engineering Loop ---

        public async Task RunEngineAsync()
        {
            Console.WriteLine($"\n--- CQH-TED Engine START (N={WORD_LIMIT_N} Constraint) ---");
            Console.WriteLine($"Context Brute-Force Status: {(BRUTE_FORCE_CONTEXT ? "Active" : "Disabled (Context 0 Only)")}");

            // Set up a background task for input (e.g., stopping the engine)
            Task inputTask = Task.Run(() => ListenForStop());

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Continuous_Reverse_Engineering_Cycle(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("\n--- CQH-TED Engine STOPPED ---");
                    break;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[LOOP ERROR] Engine Cycle Failed: {ex.Message}");
                    Console.ResetColor();
                    // Do not break loop unless it's a critical, non-recoverable error.
                    await Task.Delay(2000, cts.Token);
                }

                // Throttle the loop slightly
                await Task.Delay(500, cts.Token);
            }
        }

        private void ListenForStop()
        {
            Console.WriteLine("\n[INFO] Type 'stop' and press Enter to halt the engine.");
            while (true)
            {
                string input = Console.ReadLine();
                if (input != null && input.Trim().Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    cts.Cancel();
                    break;
                }
            }
        }

        private async Task Continuous_Reverse_Engineering_Cycle(CancellationToken token)
        {
            // 1. Pattern Tracking (Scraping)
            List<string> newWords = await ScrapeWebPageAsync(_targetUrl, WORD_LIMIT_N);

            if (newWords.Count == 0)
            {
                Console.WriteLine("[TRACK] No new words scraped in this cycle.");
                return;
            }

            foreach (var word in newWords)
            {
                int newid = idmanager.getid();
                stringtoidmapping[word] = newid;
                totalpattern.patternsequence.Add(newid);
                Console.WriteLine($"[TRACK] Added Word '{word}' -> ID {newid}. Total pattern length: {totalpattern.patternsequence.Count}");
            }

            // 2. L_exploit Synthesis 
            recalculatesubpatternswithminusoneforplaceholderkeepallsetssamesizesfillemptyspaceswithminusone();
            conversationtree tree = new conversationtree();
            tree.bruteforce(new List<pattern>(subpatterns));

            var rawPatterns = CollectRawPatterns(tree);

            // 3. C-A PTA Training (BRUTE-FORCE ASSOCIATIVE ENCODING & PARTNER ASSIGNMENT)
            BruteForceAndTrainAllPartners(rawPatterns);

            // 4. T_diag Diagnosis (Prediction/Testing)
            if (subpatterns.Count > 0)
            {
                int[] testPattern = subpatterns.Last().patternsequence.ToArray();
                nn.IO testInput = new nn.IO { IOVECTOR = testPattern };

                int matchCount = nnProcessors[TARGET_PARTNER_ID].GetPossibilitySpaceCount(testInput, BRUTE_FORCE_CONTEXT);
                bool patternRecognized = matchCount > 0;

                Console.WriteLine($"\n[{DateTime.Now.ToLongTimeString()}] Cycle Complete.");
                Console.WriteLine($"[DIAG] Test Pattern (P): {string.Join(",", testPattern)}");
                Console.WriteLine($"[DIAG] Target Partner ID Query: {TARGET_PARTNER_ID}");

                if (patternRecognized)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  -> RECOGNIZED: Pattern found in Associative Memory (M).");
                    Console.WriteLine($"  -> Found in {matchCount} internal switch combinations.");
                    Console.WriteLine("!!! CONTAINMENT TRIGGERED (Pattern Recognized) !!!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  -> UNKNOWN: No pattern match found in M for this Partner.");
                    Console.ResetColor();
                }
            }
        }

        private async Task<List<string>> ScrapeWebPageAsync(string url, int limit)
        {
            var words = new List<string>();
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string htmlContent = await response.Content.ReadAsStringAsync();

                // Mock scraping logic: look for tokens suggesting words based on the old UI design
                string[] rawTokens = htmlContent.Split(new string[] { "<a href=\"/?word=" }, StringSplitOptions.RemoveEmptyEntries);

                var uniqueWords = new HashSet<string>();
                foreach (string token in rawTokens)
                {
                    string cleanedToken = token.Split(new string[] { "\">" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    cleanedToken = cleanedToken.Trim().ToLowerInvariant();

                    if (cleanedToken.Length > 0 && uniqueWords.Add(cleanedToken))
                    {
                        words.Add(cleanedToken);
                    }
                }
                Console.WriteLine($"[SCRAPE] Successfully scraped {words.Count} potential words from {url}.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARN] Scraping failed for {url}: {ex.Message}. Falling back to simulated words.");
                Console.ResetColor();

                // Fallback to simulated words if network call fails
                words = new List<string> {
                    $"SimW{idmanager.id + 1}", $"SimW{idmanager.id + 2}", $"SimW{idmanager.id + 3}",
                    $"SimW{idmanager.id + 4}", $"SimW{idmanager.id + 5}", $"SimW{idmanager.id + 6}",
                    $"SimW{idmanager.id + 7}", $"SimW{idmanager.id + 8}", $"SimW{idmanager.id + 9}"
                };
            }

            // Select only a limited number of words
            List<string> result = words.Take(limit).ToList();
            return result;
        }
    }
}

namespace Tracker.nn
{
    // --- Data Transfer Object for NN Input/Output ---
    public class IO
    {
        // Represents the input/output pattern sequence (e.g., word IDs or placeholder -1)
        public int[] IOVECTOR { get; set; } = Array.Empty<int>();
    }

    // --- Core Processor for CQH-TED Associative Memory ---
    public class MainProcessor
    {
        // Training data series used for brute-forcing switch combinations (C-A PTA)
        public List<IO> inputseries = new List<IO>();
        public List<IO> outputseries = new List<IO>();

        // Internal switch configurations (Mock storage for initialization only)
        private int _numDimensions;
        private int _numContexts;
        private int _numTimes;
        private int _maxPatternLength;

        /// <summary>
        /// Initializes the basic parameters for the NN architecture (M, C, T, L).
        /// </summary>
        public void InitializeSwitches(int numDimensions, int numContexts, int numTimes, int maxPatternLength)
        {
            _numDimensions = numDimensions;
            _numContexts = numContexts;
            _numTimes = numTimes;
            _maxPatternLength = maxPatternLength;
            Console.WriteLine($"[NN] Initialized Processor with D={numDimensions}, C={numContexts}, T={numTimes}, L={maxPatternLength}");
        }

        /// <summary>
        /// Simulates the Complex Associative Pattern Training (C-A PTA).
        /// This method brute-forces the internal switches based on inputseries.
        /// </summary>
        /// <param name="bruteForceContext">If true, iterates across all contexts (C_i).</param>
        public void BruteForceCombinatorially(bool bruteForceContext)
        {
            // Implementation of the combinatorial switch assignment and training.
            // Placeholder: This method is currently a stub to allow compilation.
        }

        /// <summary>
        /// Simulates the T_diag Diagnosis (Prediction/Testing) phase.
        /// Queries the associative memory (M) to see if a pattern is recognized.
        /// </summary>
        /// <returns>The number of internal switch combinations that matched the pattern (0 means UNKNOWN/not recognized).</returns>
        public int GetPossibilitySpaceCount(IO testInput, bool bruteForceContext)
        {
            // Placeholder: Returns a positive random number if there's data, simulating a match.
            if (inputseries.Count > 0)
            {
                // Ensures a non-zero count to simulate 'Pattern Recognized' sometimes.
                return bruteForceContext ? new Random().Next(1, 100) : 1;
            }

            return 0; // Not recognized
        }
    }
}
