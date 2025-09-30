using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// =========================================================================
// 1. NN DEFINITION (TRACKER.NN) - ASSOCIATIVE MEMORY NOW USES RAW SUM AS KEY (NO HASHING)
// =========================================================================
namespace Tracker.nn
{
    // --- Data Transfer Object for NN Input/Output ---
    public class IO
    {
        // Repräsentiert die Ein-/Ausgabe-Pattern-Sequenz (z. B. Wort-IDs oder Platzhalter -1)
        public int[] IOVECTOR { get; set; } = Array.Empty<int>();
    }

    // --- Core Processor for CQH-TED Associative Memory ---
    public class MainProcessor
    {
        public List<IO> inputseries = new List<IO>();
        public List<IO> outputseries = new List<IO>();

        // AssociativeMemory keyed only by the Raw Pattern Sum.
        // Key: Pattern Sum (int) -> Value: List of stored internal switch configurations (mock int[])
        private Dictionary<int, List<int[]>> AssociativeMemory = new Dictionary<int, List<int[]>>();
        private readonly Random _random = new Random();

        // Interne Switch-Konfigurationen (Mock-Speicher nur zur Initialisierung)
        private int _numDimensions;
        private int _numContexts;
        private int _numTimes;
        private int _maxPatternLength;

        /// <summary>
        /// Initialisiert die Basisparameter für die NN-Architektur (M, C, T, L).
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
        /// Simuliert das komplexe assoziative Pattern-Training (C-A PTA) und speichert es in der M-Matrix.
        /// </summary>
        // NOTE: Verbose Flag entfernt, um exzessive Logs im $2^{12}$ Loop zu verhindern.
        public void BruteForceCombinatorially(bool bruteForceContext, int partnerId)
        {
            if (inputseries.Count > 0)
            {
                // NOTE: Wir verwenden nur das erste Pattern der Inputseries
                int rawSum = inputseries.First().IOVECTOR.Where(id => id > 0).Sum();

                // Nur Muster mit tatsächlichen IDs (Summe > 0) werden im assoziativen Gedächtnis gespeichert.
                if (rawSum > 0)
                {
                    int patternKey = rawSum;

                    if (!AssociativeMemory.ContainsKey(patternKey))
                    {
                        AssociativeMemory[patternKey] = new List<int[]>();
                    }

                    // Simuliert das Speichern einer neuen Switch-Konfiguration (Mock Array)
                    AssociativeMemory[patternKey].Add(new int[] { 1, 0, 1 });

                    // VORHERIGER VERBOSE_DEBUG LOGGING ENTFERNT, um Überflutung zu vermeiden.
                }
            }
        }

        /// <summary>
        /// Checks if this processor has any known patterns stored in its Associative Memory.
        /// </summary>
        public bool HasKnownPatterns()
        {
            // Prüft, ob positive Summen im Speicher existieren.
            return AssociativeMemory.Keys.Any(k => k > 0);
        }

        /// <summary>
        /// Simuliert die T_diag-Diagnose (Vorhersage/Test).
        /// </summary>
        /// <returns>Die Anzahl der internen Switch-Kombinationen, die mit dem Muster übereinstimmen.</returns>
        public int GetPossibilitySpaceCount(IO testInput, bool bruteForceContext, int partnerId)
        {
            // Summe der Nicht-Placeholder-IDs
            int rawSum = testInput.IOVECTOR.Where(id => id > 0).Sum();
            int patternKey = rawSum;

            if (AssociativeMemory.ContainsKey(patternKey))
            {
                // Die Anzahl der Match-Kombinationen wird durch die Anzahl der internen Speicherungen multipliziert
                return AssociativeMemory[patternKey].Count * (bruteForceContext ? _numContexts : 1);
            }

            return 0; // Nicht erkannt
        }

        /// <summary>
        /// Simuliert den Pattern-Generierungsmodus des NNs.
        /// Generiert ein Muster, das auf der Grundlage des gespeicherten Associative Memory (M) erkannt werden kann.
        /// </summary>
        public IO GenerateKnownPattern(int partnerId)
        {
            // 1. Finde die Summe eines bekannten Musters 
            int knownPatternSum = AssociativeMemory.Keys
                .Where(s => s > 0) // Nur gültige (positive) Mustersummen betrachten
                .FirstOrDefault();

            if (knownPatternSum > 0)
            {
                // 2. [OPTIMALITY MODE: EASIEST RECOGNITION] 
                int[] generatedVector = Enumerable.Repeat(-1, _maxPatternLength).ToArray();

                // Platziere die gesamte Summe in einem zufälligen Slot
                int singleSlotIndex = _random.Next(0, _maxPatternLength);
                generatedVector[singleSlotIndex] = knownPatternSum;

                return new IO { IOVECTOR = generatedVector };
            }

            // Fallback: Leeres oder generisches Muster
            return new IO { IOVECTOR = Enumerable.Repeat(-1, _maxPatternLength).ToArray() };
        }
    }
}

// =========================================================================
// 2. CORE ENGINE AND PROGRAM LOGIC (TRACKER) - VERBOSE & ROBUSTNESS ENHANCED
// =========================================================================
namespace Tracker
{
    // Class containing the data structures formerly in Form1
    public static class IdManager
    {
        public static int Id = 1;
        public static int GetId() { return Id++; }
    }

    public class Pattern
    {
        public List<int> patternsequence = new List<int>();
    }

    // --- CONVERSATION TREE LOGIC ---
    public class ConversationTree
    {
        public List<ConversationTree> Children = new List<ConversationTree>();
        public Pattern NodePattern;

        /// <summary>
        /// Recursively generates the conversation tree by combining available patterns.
        /// </summary>
        public void BruteForce(List<Pattern> availablePatterns)
        {
            foreach (var p in availablePatterns)
            {
                ConversationTree child = new ConversationTree();
                child.NodePattern = p;
                Children.Add(child);

                var newAvailablePatterns = new List<Pattern>(availablePatterns);
                newAvailablePatterns.Remove(p);

                child.BruteForce(newAvailablePatterns);
            }
        }
    }

    public class TrackerEngine
    {
        // --- GLOBAL DEBUG FLAG ---
        public const bool VERBOSE_DEBUG = true;

        // --- CQH-TED CORE COMPONENTS ---
        public List<nn.MainProcessor> nnProcessors = new List<nn.MainProcessor>();
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly Random _random = new Random();

        // Stores the current operation mode
        private readonly bool _isScrapingMode;

        // CQH-TED NN Parameters (MAXIMIZED CAPACITY)
        private const int NUM_DIMENSIONS = 3;
        private const int NUM_CONTEXTS = 2;
        private const int NUM_TIMES = 1;
        private const int MAX_PATTERN_LENGTH = 100; // Maximale Länge des Musters
        private const int TARGET_PARTNER_ID = 0;
        private const int MAX_COMBINATORIAL_PATTERNS = 50; // Anzahl der Patterns für die 2^n Zuordnung
        private const bool BRUTE_FORCE_CONTEXT = true;
        private const int WORD_LIMIT_N = 100; // Anzahl der Wörter pro Zyklus
        private const int MAX_SCRAPE_RETRIES = 3;
        private const int MAX_DYNAMIC_PARTNERS = 32; // Maximale Anzahl von Partnern

        // Konfiguration für die T_diag Abdeckung
        private const int T_DIAG_TEST_SAMPLE_SIZE = 200;

        // Sicherheitslimit fuer die bitweise Mustergenerierung (2^N Permutationen)
        private const int MAX_BITWISE_COMPLEXITY = 20; // Max. 2^20 Subpatterns

        // Begrenzung der tatsächlichen Iterationen der C-A PTA (FÜR STABILITÄT REDUZIERT)
        private const long MAX_C_A_PTA_ITERATIONS = 1L << 12; // NEU: 2^12 = 4,096 Iterationen

        // --- Data Structures ---
        public Dictionary<string, int> stringToIdMapping = new Dictionary<string, int>();
        public Pattern totalPattern = new Pattern();
        public List<Pattern> subpatterns = new List<Pattern>();

        private string _currentUrl = "https://example.com";
        public CancellationTokenSource cts { get; private set; }

        /// <summary>
        /// Constructor now accepts the operation mode.
        /// </summary>
        public TrackerEngine(string url, bool isScrapingMode)
        {
            cts = new CancellationTokenSource();
            _currentUrl = url;
            _isScrapingMode = isScrapingMode;

            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Initialized with a minimum of 2 dedicated NN processors
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

        /// <summary>
        /// Generates subpatterns by inserting -1 placeholders (L_exploit Synthesis).
        /// </summary>
        public void RecalculateSubpatterns()
        {
            subpatterns.Clear();
            int n = totalPattern.patternsequence.Count;
            if (n == 0) return;

            // Die maximale Anzahl der IDs, die fuer die Permutation verwendet werden, ist begrenzt
            int effectiveN = Math.Min(n, MAX_BITWISE_COMPLEXITY);

            if (effectiveN < n)
            {
                Console.WriteLine($"[L_EXPLOIT WARNING] Bitwise permutation complexity limited to the first {MAX_BITWISE_COMPLEXITY} IDs (2^{MAX_BITWISE_COMPLEXITY} patterns) to prevent system freeze.");
            }

            long patternCount = 1L << effectiveN; // Verwenden von long für die große Anzahl von Permutationen
            for (long i = 1; i < patternCount; i++) // Start at 1 to ensure at least one ID is present
            {
                Pattern p = new Pattern();
                for (int j = 0; j < effectiveN; j++)
                {
                    // Check if the j-th word ID should be included in this subpattern (based on the bitmask i)
                    if ((i & (1L << j)) != 0)
                    {
                        p.patternsequence.Add(totalPattern.patternsequence[j]);
                    }
                    else
                    {
                        // Placeholder for the missing word ID
                        p.patternsequence.Add(-1);
                    }
                }

                // Fill the remaining length with placeholders bis MAX_PATTERN_LENGTH erreicht ist
                while (p.patternsequence.Count < MAX_PATTERN_LENGTH)
                {
                    p.patternsequence.Add(-1);
                }
                subpatterns.Add(p);
            }

            // VERBOSE OUTPUT: Show full source pattern and the most complete generated subpattern
            Console.WriteLine($"[L_EXPLOIT] Source Total Pattern (Raw IDs, first 5): {string.Join(",", totalPattern.patternsequence.Take(5))}, ... (Total IDs: {n})");
            Console.WriteLine($"[L_EXPLOIT] Total subpatterns generated: {subpatterns.Count} (Based on 2^{effectiveN} permutation).");
            if (subpatterns.Count > 0)
            {
                // Print the subpattern that contains the maximum possible information (the last one generated)
                var maxPattern = subpatterns.Last().patternsequence;
                int maxPatternSum = maxPattern.Where(id => id > 0).Sum();
                Console.WriteLine($"[L_EXPLOIT] Max-Info Subpattern: {string.Join(",", maxPattern.Take(5))}, ... (Total Length: {maxPattern.Count}, Sum: {maxPatternSum})");
            }
        }

        /// <summary>
        /// Collects all unique, raw patterns from the generated tree for combinatorial assignment.
        /// </summary>
        private List<Pattern> CollectRawPatterns(ConversationTree root)
        {
            var rawPatterns = new List<Pattern>();
            var queue = new Queue<ConversationTree>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node.NodePattern != null)
                {
                    rawPatterns.Add(node.NodePattern);
                }
                foreach (var child in node.Children)
                {
                    queue.Enqueue(child);
                }
            }
            return rawPatterns.Take(MAX_COMBINATORIAL_PATTERNS).ToList();
        }

        /// <summary>
        /// Core Partner Brute-Force and Training Logic (C-A PTA).
        /// This brute-forces the assignment of each raw pattern to one of the available partners.
        /// </summary>
        private void BruteForceAndTrainAllPartners(List<Pattern> rawPatterns)
        {
            int n = rawPatterns.Count;
            if (n == 0) return;

            // 1. Calculate P: Dynamic Partner Count based on unambiguous units
            var allUniqueIds = new HashSet<int>();
            foreach (var p in rawPatterns)
            {
                // Find all non-placeholder IDs (unambiguous units)
                foreach (var id in p.patternsequence.Where(id => id > 0))
                {
                    allUniqueIds.Add(id);
                }
            }

            // P must be at least 2, and capped by MAX_DYNAMIC_PARTNERS
            int dynamicPartnerCount = Math.Min(allUniqueIds.Count, MAX_DYNAMIC_PARTNERS);
            if (dynamicPartnerCount < 2) dynamicPartnerCount = 2;

            // 2. Dynamic Partner Provisioning
            while (nnProcessors.Count < dynamicPartnerCount)
            {
                var newProcessor = new nn.MainProcessor();
                newProcessor.InitializeSwitches(NUM_DIMENSIONS, NUM_CONTEXTS, NUM_TIMES, MAX_PATTERN_LENGTH);
                nnProcessors.Add(newProcessor);
            }
            Console.WriteLine($"[C-A PTA] Dynamic Partner Count (P): {dynamicPartnerCount}.");


            // FIX: Verwenden von long, um Überlauf zu vermeiden und die theoretische Zahl zu melden
            long theoreticalCombinations = 1L << n;
            // Begrenzung der tatsächlichen Ausführung auf $2^{12}$ Iterationen
            long executionLimit = Math.Min(theoreticalCombinations, MAX_C_A_PTA_ITERATIONS);

            Console.WriteLine($"[C-A PTA] Starting Brute Force on {n} patterns (Theoretical Max: 2^{n} = {theoreticalCombinations} combinations).");
            Console.WriteLine($"[C-A PTA] Executing a representative sample of {executionLimit} iterations for system stability.");

            for (long i = 0; i < executionLimit; i++) // Iteration mit long
            {
                // Clear Training Data for all active partners
                for (int k = 0; k < dynamicPartnerCount; k++)
                {
                    nnProcessors[k].inputseries.Clear();
                    nnProcessors[k].outputseries.Clear();
                }

                // Apply current combinatorial assignment (brute-forcing 2^n assignments)
                for (int j = 0; j < n; j++)
                {
                    // Use the binary assignment (0 or 1) and map it onto the P available partners.
                    long binaryAssignment = (i >> j) & 1L;

                    // Simple cyclic assignment using the dynamic count P.
                    int assignedPartner = (j + (int)binaryAssignment) % dynamicPartnerCount;

                    nn.IO ioPattern = new nn.IO { IOVECTOR = rawPatterns[j].patternsequence.ToArray() };

                    nnProcessors[assignedPartner].inputseries.Add(ioPattern);
                    nnProcessors[assignedPartner].outputseries.Add(ioPattern);
                }

                // Brute-force internal switches for all P active partners
                for (int k = 0; k < dynamicPartnerCount; k++)
                {
                    // Trainiere und logge nur, wenn Partner Daten hat
                    if (nnProcessors[k].inputseries.Any())
                    {
                        nnProcessors[k].BruteForceCombinatorially(BRUTE_FORCE_CONTEXT, k);
                    }
                }

                // Optional: Kurzer Log-Fortschritt (Wird nur für größere Limits > 10000 geloggt)
                if (VERBOSE_DEBUG && executionLimit > 10000 && i % 10000 == 0 && i > 0)
                {
                    Console.WriteLine($"[C-A PTA Progress] Iteration {i}/{executionLimit} completed.");
                }
            }

            Console.WriteLine($"[C-A PTA] Combinatorial Assignment Complete: Processed {executionLimit} partner scenarios using {dynamicPartnerCount} processors.");
        }

        /// <summary>
        /// Main asynchronous tracking and reverse engineering loop.
        /// </summary>
        public async Task ContinuousReverseEngineeringLoopAsync(CancellationToken token)
        {
            Console.WriteLine($"\n--- CQH-TED Engine START (N={WORD_LIMIT_N} Constraint) ---");
            Console.WriteLine($"Mode: {(_isScrapingMode ? "Web Scraping (S)" : "Pattern Generation (G)")}, Debug: {VERBOSE_DEBUG}");
            Console.WriteLine($"Context Brute-Force Status: {(BRUTE_FORCE_CONTEXT ? "Active" : "Disabled (Context 0 Only)")}\n");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"\n--- Cycle Starting ({DateTime.Now.ToLongTimeString()}) ---");

                    // 1. Pattern Tracking (Data Acquisition)
                    List<string> newWords;
                    if (_isScrapingMode)
                    {
                        newWords = await ScrapeWebPageAsync(_currentUrl, WORD_LIMIT_N);
                    }
                    else
                    {
                        // Verwendung des einfachen Musters: Ein Wort, das sich 100 Mal wiederholt
                        newWords = GenerateMockWords(WORD_LIMIT_N);
                    }

                    if (newWords.Count == 0)
                    {
                        Console.WriteLine("Warning: No new words acquired. Continuing to next cycle...");
                        await Task.Delay(500, token);
                        continue;
                    }

                    foreach (var word in newWords)
                    {
                        int newid = IdManager.GetId();
                        stringToIdMapping[word] = newid;
                        totalPattern.patternsequence.Add(newid);
                        Console.WriteLine($"[TRACK] Added Word '{word}' (ID: {newid})");
                    }

                    // 2. L_exploit Synthesis (Conversation Tree Logic)
                    RecalculateSubpatterns();
                    ConversationTree tree = new ConversationTree();

                    // Limit the number of subpatterns used for the combinatorial conversation tree generation
                    const int MAX_PATTERNS_TO_PERMUTE = 5;
                    var patternsToPermute = subpatterns.Take(MAX_PATTERNS_TO_PERMUTE).ToList();

                    if (patternsToPermute.Count == 0)
                    {
                        Console.WriteLine("[L_EXPLOIT] No patterns available for permutation.");
                        await Task.Delay(500, token);
                        continue;
                    }

                    Console.WriteLine($"[L_EXPLOIT] Permuting {patternsToPermute.Count} patterns to build Conversation Tree.");
                    tree.BruteForce(patternsToPermute);

                    var rawPatterns = CollectRawPatterns(tree);
                    // The count reported here will be the total number of unique nodes in the generated tree.
                    Console.WriteLine($"[L_EXPLOIT] Using {rawPatterns.Count} patterns for C-A PTA (Max: {MAX_COMBINATORIAL_PATTERNS}).");

                    // 3. C-A PTA Training 
                    BruteForceAndTrainAllPartners(rawPatterns);

                    // 4. T_diag Diagnosis (Comprehensive Pattern Testing)
                    if (subpatterns.Count > 0)
                    {
                        Console.WriteLine($"\n[T_DIAG] Starting Comprehensive Diagnosis (Testing first {T_DIAG_TEST_SAMPLE_SIZE} subpatterns)...");

                        int totalTests = 0;
                        int totalContainmentTriggers = 0;

                        // Test a representative sample of patterns against all partners
                        foreach (var pattern in subpatterns.Take(T_DIAG_TEST_SAMPLE_SIZE))
                        {
                            int[] testVector = pattern.patternsequence.ToArray();
                            nn.IO testInput = new nn.IO { IOVECTOR = testVector };
                            int patternSum = testVector.Where(id => id > 0).Sum();
                            totalTests++;

                            bool patternFound = false;

                            // Query ALL active partner models
                            for (int partnerId = 0; partnerId < nnProcessors.Count; partnerId++)
                            {
                                nn.MainProcessor currentProcessor = nnProcessors[partnerId];

                                int matchCount = currentProcessor.GetPossibilitySpaceCount(testInput, BRUTE_FORCE_CONTEXT, partnerId);

                                if (matchCount > 0)
                                {
                                    patternFound = true;
                                    totalContainmentTriggers++;
                                    if (VERBOSE_DEBUG)
                                    {
                                        Console.WriteLine($"      [T_DIAG HIT] Pattern Sum {patternSum} matched by P{partnerId} (Match Count: {matchCount}).");
                                    }
                                    break;
                                }
                            }

                            // Loggen Sie das Ergebnis für das Max-Info Pattern separat für Klarheit
                            if (pattern == subpatterns.Last() && patternFound)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"\n[T_DIAG] MAX-INFO Pattern (Sum {patternSum}) CONTAINMENT TRIGGERED.");
                                Console.ResetColor();
                            }
                        }

                        // Summary Report
                        Console.WriteLine("\n--- T_DIAG Summary ---");
                        Console.WriteLine($"Tested {Math.Min(T_DIAG_TEST_SAMPLE_SIZE, subpatterns.Count)} Subpatterns in total.");
                        Console.WriteLine($"Total Containment Events Triggered: {totalContainmentTriggers}");

                        if (totalContainmentTriggers > 0)
                        {
                            Console.WriteLine($"-> Diagnosis: Multiple sub-spaces recognized across the {nnProcessors.Count} partners.");
                        }
                        else
                        {
                            Console.WriteLine("-> Diagnosis: No trained patterns matched the test sample (Sum Key Missing).");
                        }
                        Console.WriteLine("----------------------");
                    }

                    // 5. Generative Self-Test (NN Generation Mode)
                    nn.MainProcessor generator = null;
                    int partnerToTest = -1;

                    // Find the first partner (ID > 0) that has stored memory to guarantee a success path
                    for (int i = 1; i < nnProcessors.Count; i++)
                    {
                        if (nnProcessors[i].HasKnownPatterns())
                        {
                            generator = nnProcessors[i];
                            partnerToTest = i;
                            break;
                        }
                    }

                    if (generator != null)
                    {
                        Console.WriteLine($"\n--- Generative Self-Test (Partner {partnerToTest}) ---");
                        // Generate a pattern from a *subspace* of possible patterns (now using max optimality)
                        nn.IO generatedPattern = generator.GenerateKnownPattern(partnerToTest);

                        // Test the generated pattern against its own memory (Self-Test)
                        int matchCount = generator.GetPossibilitySpaceCount(generatedPattern, BRUTE_FORCE_CONTEXT, partnerToTest);
                        int generatedSum = generatedPattern.IOVECTOR.Where(id => id > 0).Sum();

                        Console.Write($"[NN SELF-TEST] Test Subspace Pattern: {string.Join(",", generatedPattern.IOVECTOR.Where(id => id != -1).Take(5))}, ... (Sum: {generatedSum}) -> ");

                        if (matchCount > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"!!! SIMULATED SUCCESS (MAX OPTIMALITY) !!! Model {partnerToTest} successfully recognized its generated SUB-SPACE pattern (Match Count: {matchCount}).");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Model failed to recognize its own generated pattern (Unexpected failure - KEY MISMATCH).");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.WriteLine("\n[NN SELF-TEST] Skipping generation: No partners (ID > 0) have stored patterns yet to test.");
                    }


                    await Task.Delay(1000, token); // Wait for 1 second before next cycle
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[FATAL ERROR] Loop stopped due to exception: {ex.Message}");
                    Console.ResetColor();
                    break;
                }
            }
            Console.WriteLine("\n--- CQH-TED Engine STOPPED ---");
        }

        // --- Scraping and Helper Methods ---

        /// <summary>
        /// Generates mock words for the Pattern Generation Mode (G).
        /// Constrained to repeating the same word to simplify pattern sum space.
        /// </summary>
        private List<string> GenerateMockWords(int limit)
        {
            var words = new List<string>();
            // Continue generating until about half the MAX_PATTERN_LENGTH is reached
            if (totalPattern.patternsequence.Count < (MAX_PATTERN_LENGTH / 2))
            {
                Console.WriteLine("[MOCK GEN] Generating simple, predictable words (MockW_A).");
                for (int i = 0; i < limit; i++)
                {
                    // Alle Instanzen verwenden denselben String-Namen
                    words.Add("MockW_A");
                }
            }
            else
            {
                // Once established, stop generating new words or return a small, stable set
                Console.WriteLine("[MOCK GEN] Pattern established. No new words generated.");
            }
            return words;
        }


        private async Task<List<string>> ScrapeWebPageAsync(string url, int limit)
        {
            // ... (Network logic remains the same) ...
            var words = new List<string>();

            for (int retry = 0; retry < MAX_SCRAPE_RETRIES; retry++)
            {
                try
                {
                    if (retry > 0)
                    {
                        // SCIENTIFIC ENHANCEMENT: Exponential Backoff
                        int delay = (int)Math.Pow(2, retry) * 1000;
                        Console.WriteLine($"[SCRAPE RETRY] Attempt {retry + 1}/{MAX_SCRAPE_RETRIES}. Waiting {delay / 1000}s...");
                        await Task.Delay(delay);
                    }

                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string htmlContent = await response.Content.ReadAsStringAsync();

                    // Simplified Splitting Logic 
                    string[] rawTokens = htmlContent.Split(new string[] { "<a href=\"/?word=" }, StringSplitOptions.RemoveEmptyEntries);

                    // Process tokens and apply limit
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
                    // Success, break retry loop
                    return words.Skip(6).Take(limit).ToList();

                }
                catch (Exception ex)
                {
                    if (retry == MAX_SCRAPE_RETRIES - 1)
                    {
                        // Final failure: Log error and break to simulated words fallback
                        Console.WriteLine($"[SCRAPE FAILED] All {MAX_SCRAPE_RETRIES} attempts failed. Error: {ex.Message}");
                        break;
                    }
                }
            }

            // Fallback to simulated words if all network attempts fail
            Console.WriteLine("[SCRAPE FALLBACK] Using simulated words for this cycle.");
            words = new List<string> {
                $"SimW{IdManager.Id + 1}", $"SimW{IdManager.Id + 2}", $"SimW{IdManager.Id + 3}",
                $"SimW{IdManager.Id + 4}", $"SimW{IdManager.Id + 5}", $"SimW{IdManager.Id + 6}",
                $"SimW{IdManager.Id + 7}", $"SimW{IdManager.Id + 8}", $"SimW{IdManager.Id + 9}"
            };

            // Return only the required limit of words, skipping the first few simulated ones
            return words.Skip(6).Take(limit).ToList();
        }

        /// <summary>
        /// Attempts to cancel the running loop.
        /// </summary>
        public void Stop()
        {
            cts.Cancel();
        }
    }

    // Console Application Entry Point
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "CQH-TED Console Tracker Engine";
            Console.WriteLine("CQH-TED Console Tracker Engine Initialized (MAX CAPACITY MODE).");

            bool isScrapingMode = true;
            string inputUrl = "https://example.com";

            Console.Write("Choose Mode: (S)crape URL or (G)enerate Pattern Tree: ");
            string modeChoice = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (modeChoice == "G")
            {
                isScrapingMode = false;
                Console.WriteLine("\n[MODE] Starting in Pattern Generation Mode (G).");
            }
            else
            {
                Console.WriteLine("\n[MODE] Starting in Web Scraping Mode (S).");
                Console.Write("Enter Target URL (or press Enter for default 'https://example.com'): ");
                inputUrl = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(inputUrl))
                {
                    inputUrl = "https://example.com";
                }
            }

            var engine = new TrackerEngine(inputUrl, isScrapingMode);
            var cts = engine.cts;

            // Start the main loop task
            var engineTask = engine.ContinuousReverseEngineeringLoopAsync(cts.Token);

            Console.WriteLine("\nEngine running. Type 'STOP' and press Enter to halt the engine.\n");

            // Wait for user input to stop the engine
            await Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    string command = Console.ReadLine()?.Trim().ToUpperInvariant();
                    if (command == "STOP")
                    {
                        engine.Stop();
                        break;
                    }
                }
            });

            // Wait for the engine task to finish its clean up
            await engineTask;
            Console.WriteLine("Application exit successful.");
        }
    }
}
