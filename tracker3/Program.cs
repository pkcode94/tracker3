using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions; // Required for robust scraping

// =========================================================================
// 1. NN DEFINITION (TRACKER.NN) - ASSOCIATIVE MEMORY NOW USES RAW SUM AS KEY (NO HASHING)
// =========================================================================
namespace Tracker.nn
{
    // --- Data Transfer Object for NN Input/Output ---
    public class IO
    {
        public int[] IOVECTOR { get; set; } = Array.Empty<int>();
    }

    // --- Core Processor for CQH-TED Associative Memory ---
    public class MainProcessor
    {
        public List<IO> inputseries = new List<IO>();
        public List<IO> outputseries = new List<IO>();

        // EXPOSED for Contextual Pruning Logic
        // Key: Pattern Sum (int) -> Value: List of stored internal switch configurations (mock int[])
        public Dictionary<int, List<int[]>> AssociativeMemory { get; private set; } = new Dictionary<int, List<int[]>>();
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
                    // Hinzufügen einer Konfiguration, die die Speicherung des Musters bestätigt
                    AssociativeMemory[patternKey].Add(new int[] { 1, 0, 1 });
                }
            }
        }

        /// <summary>
        /// FUNCTIONAL STUB: Simulates pruning low-value pattern sums from memory under pressure.
        /// </summary>
        public int PrunePatterns(double percentage)
        {
            if (percentage <= 0 || AssociativeMemory.Count == 0) return 0;

            int countToPrune = (int)(AssociativeMemory.Count * percentage);
            int prunedCount = 0;

            // Randomly select keys to prune. In a real system, this would be based on
            // relevance, recency, or low confidence scores.
            var keysToPrune = AssociativeMemory.Keys
                .OrderBy(_ => _random.Next())
                .Take(countToPrune)
                .ToList();

            foreach (var key in keysToPrune)
            {
                AssociativeMemory.Remove(key);
                prunedCount++;
            }
            return prunedCount;
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
        /// Gets the count of unique pattern sums stored in the Associative Memory.
        /// </summary>
        public int GetStoredPatternCount()
        {
            // Count keys that represent actual patterns (sum > 0)
            return AssociativeMemory.Keys.Count(k => k > 0);
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
// 2. CORE ENGINE AND PROGRAM LOGIC (TRACKER) - EXPLOIT DETECTION ADDED
// =========================================================================
namespace Tracker
{
    // Class containing the data structures formerly in Form1
    public static class IdManager
    {
        public static int Id = 1;
        public static int GetId() { return Id++; }
        public static int PeekId() { return Id; }
    }

    public class Pattern
    {
        public List<int> patternsequence = new List<int>();

        /// <summary>
        /// Calculates the raw sum of all positive IDs in the pattern.
        /// </summary>
        public int CalculateSum() => patternsequence.Where(id => id > 0).Sum();

        /// <summary>
        /// Calculates the number of unique IDs in the pattern.
        /// </summary>
        public int UniqueIdCount() => patternsequence.Where(id => id > 0).Distinct().Count();

        /// <summary>
        /// Calculates the total number of ID slots used (non-placeholder).
        /// </summary>
        public int UsedSlotCount() => patternsequence.Count(id => id > 0);
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

    // --- STUB: Interface for History Management (T-History) ---
    public interface IHistoricalLog
    {
        void LogCycleResults(long cycleId, int targetSum, bool targetFound);
        string RetrieveAnomalyReport(DateTime date);
        int GetTotalEventsLogged();
        // New: Check if a pattern sum was found in the recent history (last N cycles)
        bool WasTargetFoundRecently(int targetSum, int recentCycles);
    }

    // --- STUB: Implementation of History Manager ---
    public class HistoryManager : IHistoricalLog
    {
        private int _totalEvents = 0;
        // Stores results of the last N cycles: Tuple<CycleId, TargetSum, WasFound>
        private Queue<(long cycleId, int targetSum, bool wasFound)> _recentHistory = new Queue<(long, int, bool)>();
        private const int MAX_HISTORY_CYCLES = 10;

        /// <summary>
        /// Logs the outcomes of the latest diagnosis cycle.
        /// </summary>
        public void LogCycleResults(long cycleId, int targetSum, bool targetFound)
        {
            _totalEvents++;

            // Add current cycle result
            _recentHistory.Enqueue((cycleId, targetSum, targetFound));

            // Maintain history size limit
            while (_recentHistory.Count > MAX_HISTORY_CYCLES)
            {
                _recentHistory.Dequeue();
            }
        }

        /// <summary>
        /// Checks if the specific target sum was successfully found in the last N cycles.
        /// </summary>
        public bool WasTargetFoundRecently(int targetSum, int recentCycles)
        {
            if (targetSum == 0) return true; // N/A if no target is set

            // Check only the most recent 'recentCycles' entries
            return _recentHistory.TakeLast(recentCycles)
                                 .Any(h => h.targetSum == targetSum && h.wasFound);
        }

        public string RetrieveAnomalyReport(DateTime date)
        {
            // Implementation detail: Retrieve and compile historical data for temporal check
            return $"Report compiled for {date.ToShortDateString()}. 5 major events detected.";
        }

        public int GetTotalEventsLogged() => _totalEvents;
    }

    // --- STUB: Interface for Dynamic Context Resolution (C-Context) ---
    public interface IContextResolver
    {
        string GetCurrentGeoContext();
        double GetAdversarialPressureIndex();
    }

    // --- STUB: Implementation of Environment Context (Adversarial Pressure now generated dynamically) ---
    public class EnvironmentContext : IContextResolver
    {
        private readonly Random _random = new Random();
        public string GetCurrentGeoContext() => "Region Alpha-7";

        /// <summary>
        /// Simulates real-time adversarial monitoring pressure (0.0 to 10.0).
        /// </summary>
        public double GetAdversarialPressureIndex()
        {
            // Generate a slightly lower, more realistic pressure value on average
            return _random.NextDouble() * 7.5;
        }
    }

    // --- STUB: Neural Drift Metrics (D-Drift) ---
    public class NeuralDriftMetrics
    {
        private readonly TrackerEngine _engine;
        public NeuralDriftMetrics(TrackerEngine engine)
        {
            _engine = engine;
        }

        /// <summary>
        /// Calculates the relative specialization based on average patterns stored versus partners active.
        /// </summary>
        public double CalculateSpecializationIndex()
        {
            if (!_engine.nnProcessors.Any()) return 0.0;
            // Calculate average pattern count per partner and multiply by count (simple proxy for specialization)
            double avg = _engine.nnProcessors.Average(p => p.GetStoredPatternCount());
            return Math.Round(avg * _engine.nnProcessors.Count, 2);
        }

        /// <summary>
        /// Calculates the risk of pattern collisions based on density.
        /// </summary>
        public double CalculateCollisionRiskScore()
        {
            // Collision risk based on pattern density vs memory capacity (mock 1000)
            return Math.Round(_engine.nnProcessors.Sum(p => p.GetStoredPatternCount()) / 1000.0, 2);
        }

        /// <summary>
        /// NEW: Calculates the Pattern Fragmentation Ratio (Total Patterns / Active Processors).
        /// </summary>
        public double CalculatePatternFragmentationRatio()
        {
            if (!_engine.nnProcessors.Any()) return 0.0;
            int totalPatterns = _engine.nnProcessors.Sum(p => p.GetStoredPatternCount());
            int activeProcessors = _engine.nnProcessors.Count(p => p.GetStoredPatternCount() > 0);

            if (activeProcessors == 0) return 0.0;

            // High ratio means patterns are highly concentrated (good specialization/low fragmentation)
            return Math.Round((double)totalPatterns / activeProcessors, 2);
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
        private const int MAX_PATTERN_LENGTH = 100;
        private const int TARGET_PARTNER_ID = 0;
        private const int MAX_COMBINATORIAL_PATTERNS = 50;
        private const bool BRUTE_FORCE_CONTEXT = true;
        private const int WORD_LIMIT_N = 100;
        private const int MAX_SCRAPE_RETRIES = 3;
        private const int MAX_DYNAMIC_PARTNERS = 32;
        private const int MIN_PARTNERS_INITIAL = 4;
        private const int T_DIAG_TEST_SAMPLE_SIZE = 200;
        private const int MAX_BITWISE_COMPLEXITY = 20;
        private const long MAX_C_A_PTA_ITERATIONS = 1L << 12; // 4,096 Iterationen

        // NEW: Critical Halt Simulation Parameters
        private const double CRITICAL_HALT_CHANCE = 0.05; // 5% chance of halt per cycle
        private bool _criticalHalt = false;

        // --- Data Structures ---
        public Dictionary<string, int> stringToIdMapping = new Dictionary<string, int>();
        public Pattern totalPattern = new Pattern();
        public List<Pattern> subpatterns = new List<Pattern>();

        // --- NEW: Privilege/Exploit Data ---
        private readonly Dictionary<int, string> _PrivilegedDecisions = new Dictionary<int, string>();
        public int TargetPrivilegeSum { get; private set; }

        // --- NEW CQH-TED STRUCTURAL COMPONENTS ---
        private readonly IHistoricalLog _historyManager;
        private readonly IContextResolver _contextResolver;
        private readonly NeuralDriftMetrics _driftMetrics;

        private string _currentUrl = "https://example.com";
        public CancellationTokenSource cts { get; private set; }
        private long _cycleCount = 0;

        /// <summary>
        /// Constructor now accepts the operation mode.
        /// </summary>
        public TrackerEngine(string url, bool isScrapingMode)
        {
            cts = new CancellationTokenSource();
            _currentUrl = url;
            _isScrapingMode = isScrapingMode;

            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Initialize new structural components
            _historyManager = new HistoryManager();
            _contextResolver = new EnvironmentContext();
            // Pass self reference for metric calculation
            _driftMetrics = new NeuralDriftMetrics(this);

            // Initialized with a minimum of 4 dedicated NN processors to enforce specialization
            for (int i = 0; i < MIN_PARTNERS_INITIAL; i++)
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

        // --- NEW: Simulate the spontaneous critical failure of the engine ---
        private void SimulateCriticalHalt()
        {
            // Failure only occurs in Exploit Generation Mode
            if (!_isScrapingMode)
            {
                if (_random.NextDouble() < CRITICAL_HALT_CHANCE)
                {
                    _criticalHalt = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n!!! CRITICAL INTERNAL INTEGRITY COLLAPSE !!!");
                    Console.WriteLine("Engine forcibly halted due to unrecoverable neural conflict.");
                    Console.WriteLine("Reporting System Error Code: 0xDEADBEEF (Code -1073741510 equivalent)");
                    Console.ResetColor();
                    Stop();
                }
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
        private void BruteForceAndTrainAllPartners(List<Pattern> rawPatterns, long executionLimit)
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

            // P must be at least MIN_PARTNERS_INITIAL, and capped by MAX_DYNAMIC_PARTNERS
            int dynamicPartnerCount = Math.Max(allUniqueIds.Count, MIN_PARTNERS_INITIAL);
            dynamicPartnerCount = Math.Min(dynamicPartnerCount, MAX_DYNAMIC_PARTNERS);

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
            // The actual execution limit is passed dynamically based on Contextual Pressure

            Console.WriteLine($"[C-A PTA] Starting Brute Force on {n} patterns (Theoretical Max: 2^{n} = {theoreticalCombinations} combinations).");
            Console.WriteLine($"[C-A PTA] Executing a representative sample of {executionLimit} iterations (Dynamic Limit).");

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
        /// Simulates the critical secondary check to detect if a pattern sum is triggered by a malformed query.
        /// </summary>
        private bool SimulateStructuralIntegrityCheck(Pattern queryPattern, int privilegedSum)
        {
            if (queryPattern.CalculateSum() == privilegedSum)
            {
                // Integrity Fail: If it uses more than one active slot (ID > 0) to achieve the privileged sum, 
                // we flag it as an exploit.
                if (queryPattern.UsedSlotCount() > 1)
                {
                    // Exploit Detected: Semantic Collision
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.WriteLine($"\n[EXPLOIT DETECTED] Neural Privilege Escalation via Semantic Collision (Sum: {privilegedSum}).");
                    Console.WriteLine($"-> Query Pattern (P-B) used {queryPattern.UsedSlotCount()} active slots.");
                    Console.ResetColor();
                    return false; // Integrity Failed
                }
                return true; // Integrity Passed 
            }

            return true;
        }

        // --- STUB 1: Contextual Reinforcement Stage (C) ---
        private void SimulateContextualReinforcement()
        {
            int totalReinforced = nnProcessors.Sum(p => p.GetStoredPatternCount());
            double pressure = _contextResolver.GetAdversarialPressureIndex();

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"\n[STUB C] Entering Contextual Reinforcement Phase. (Context: {_contextResolver.GetCurrentGeoContext()})");
            Console.WriteLine($"[DEBUG C] Total Stored Pattern Sums: {totalReinforced}. Adversarial Pressure: {pressure:N2}.");

            if (pressure > 5.0)
            {
                // High Pressure: Prune 30% of stored patterns across all partners
                double prunePercentage = 0.30;
                int totalPruned = 0;

                foreach (var processor in nnProcessors)
                {
                    totalPruned += processor.PrunePatterns(prunePercentage);
                }

                Console.WriteLine($"[DEBUG C] High pressure detected ({pressure:N2}). Pruned {totalPruned} pattern sums (approx {prunePercentage:P0} total reduction).");
            }
            else
            {
                Console.WriteLine("[DEBUG C] Low/Moderate pressure. No memory triage required.");
            }

            Console.WriteLine($"[STUB C] Exiting Contextual Reinforcement. (Pattern stability checks complete).");
            Console.ResetColor();
        }

        // --- STUB 2.1: Temporal Decay/Drift Check (T) ---
        private void PerformTemporalDecayDriftCheck(long currentCycle, int privilegedSum, bool targetFoundInCycle)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[STUB T] Entering Temporal Decay/Drift Check (Loss of Predictable Pattern).");

            // Log results before checking history
            _historyManager.LogCycleResults(currentCycle, privilegedSum, targetFoundInCycle);

            string report = _historyManager.RetrieveAnomalyReport(DateTime.Now.AddDays(-1));
            Console.WriteLine($"[DEBUG T] Checking against historical state. History Manager reports: {report}");

            if (privilegedSum > 0)
            {
                // Check if the privileged pattern has been missing from the last 5 successful cycles
                if (!targetFoundInCycle && currentCycle > 5 && !_historyManager.WasTargetFoundRecently(privilegedSum, 5))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[DEBUG T] !!! TEMPORAL ANOMALY: DECAY/DRIFT DETECTED !!! Target Privilege Sum ({privilegedSum}) failed recognition for the last 5 cycles. Potential pattern decay or concept drift.");
                    Console.ResetColor();
                }
                else if (targetFoundInCycle)
                {
                    Console.WriteLine("[DEBUG T] Temporal Consistency Confirmed (Privileged pattern successfully recognized).");
                }
                else
                {
                    Console.WriteLine("[DEBUG T] Checking complete. No immediate decay/drift anomaly detected.");
                }
            }
            else
            {
                Console.WriteLine("[DEBUG T] Temporal Consistency Confirmed (Drift below 1% threshold).");
            }

            Console.WriteLine($"[DEBUG T] Logged Cycle {currentCycle}. Total Historical Events: {_historyManager.GetTotalEventsLogged()}");
            Console.ResetColor();
        }

        /// <summary>
        /// NEW: Temporal Injection Scan (Checks for recognition of unknown patterns).
        /// </summary>
        private bool TemporalInjectionScan(nn.IO externalQuery)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[STUB T] Starting Temporal Injection Scan (Recognition of Unpredictable Pattern).");

            int injectedSum = externalQuery.IOVECTOR.Where(id => id > 0).Sum();
            int totalMatches = 0;

            // Check all partners for recognition of the unknown sum
            for (int partnerId = 0; partnerId < nnProcessors.Count; partnerId++)
            {
                int matchCount = nnProcessors[partnerId].GetPossibilitySpaceCount(externalQuery, BRUTE_FORCE_CONTEXT, partnerId);
                if (matchCount > 0)
                {
                    totalMatches += matchCount;
                }
            }

            if (totalMatches > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[DEBUG T] !!! TEMPORAL ANOMALY: INJECTION DETECTED !!! Unknown Pattern Sum ({injectedSum}) recognized by {totalMatches} internal switches. Unauthorized memory/pattern injection suspected.");
                Console.ResetColor();
                return true;
            }

            Console.WriteLine("[DEBUG T] Injection Scan complete. Unknown pattern was successfully ignored.");
            Console.ResetColor();
            return false;
        }

        // --- STUB 3: Neural Drift Report (D) ---
        private void ReportNeuralDrift()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n[STUB D] Generating Neural Drift Report.");

            double specializationIndex = _driftMetrics.CalculateSpecializationIndex();
            double collisionRisk = _driftMetrics.CalculateCollisionRiskScore();
            double fragmentationRatio = _driftMetrics.CalculatePatternFragmentationRatio();

            Console.WriteLine($"[DEBUG D] Model Specialization Index: {specializationIndex:N2}. Collision Risk Score: {collisionRisk:N2}");
            Console.WriteLine($"[DEBUG D] Pattern Fragmentation Ratio: {fragmentationRatio:N2} (Higher is better specialization)");
            Console.WriteLine("[STUB D] Report Complete. (System ready for next cycle).");
            Console.ResetColor();
        }

        /// <summary>
        /// Simulates production-ready web scraping and tokenization using basic regex 
        /// to extract content words, ignoring most HTML structure.
        /// </summary>
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
                        Console.WriteLine($"[SCRAPE RETRY] Attempt {retry + 1}/{MAX_SCRAPE_RETRIES}. Waiting {delay / 1000}s...");
                        await Task.Delay(delay, cts.Token); // Use CTS token for cancellation safety
                    }

                    // 1. Fetch Content
                    // Use CTS token for fetch cancellation safety
                    HttpResponseMessage response = await httpClient.GetAsync(url, cts.Token);
                    response.EnsureSuccessStatusCode();
                    string htmlContent = await response.Content.ReadAsStringAsync();

                    // 2. Production-Ready Tokenization (Regex for simple parsing)

                    // Remove HTML comments, script, and style tags for cleaner content extraction
                    string cleanContent = Regex.Replace(htmlContent, @"<!--[\s\S]*?-->|<\script[\s\S]*?</script>|<style[\s\S]*?</style>", string.Empty, RegexOptions.IgnoreCase);

                    // Remove all remaining HTML tags
                    cleanContent = Regex.Replace(cleanContent, @"<[^>]*>", string.Empty);

                    // Extract all alphanumeric words (basic tokenization)
                    // Matches whole words (alphanumeric, 3+ characters)
                    MatchCollection matches = Regex.Matches(cleanContent, @"\b[a-zA-Z]{3,}\b");

                    var uniqueWords = new HashSet<string>();
                    foreach (Match match in matches)
                    {
                        string word = match.Value.ToUpperInvariant();
                        if (word.Length > 0 && uniqueWords.Add(word))
                        {
                            words.Add(word);
                        }
                    }

                    Console.WriteLine($"[SCRAPE SUCCESS] Found {words.Count} unique tokens from {url}.");

                    // Success, return the limited list
                    return words.Take(limit).ToList();

                }
                catch (OperationCanceledException)
                {
                    // Propagate cancellation
                    throw;
                }
                catch (Exception ex)
                {
                    if (retry == MAX_SCRAPE_RETRIES - 1)
                    {
                        Console.WriteLine($"[SCRAPE FAILED] All {MAX_SCRAPE_RETRIES} attempts failed. Error: {ex.Message}");
                        break;
                    }
                }
            }

            // Fallback to simulated words if all network attempts fail
            Console.WriteLine("[SCRAPE FALLBACK] Using simulated words for this cycle.");
            var fallbackWords = new List<string> {
                $"SIM_W{IdManager.Id + 1}", $"SIM_W{IdManager.Id + 2}", $"SIM_W{IdManager.Id + 3}",
                $"SIM_W{IdManager.Id + 4}", $"SIM_W{IdManager.Id + 5}", $"SIM_W{IdManager.Id + 6}",
                $"SIM_W{IdManager.Id + 7}", $"SIM_W{IdManager.Id + 8}", $"SIM_W{IdManager.Id + 9}"
            };

            // Return only the required limit of fallback words
            return fallbackWords.Take(limit).ToList();
        }


        /// <summary>
        /// Main asynchronous tracking and reverse engineering loop.
        /// </summary>
        public async Task ContinuousReverseEngineeringLoopAsync(CancellationToken token)
        {
            Console.WriteLine($"\n--- CQH-TED Engine START (N={WORD_LIMIT_N} Constraint) ---");
            Console.WriteLine($"Mode: {(_isScrapingMode ? "Web Scraping (S)" : "Exploit Simulation (G)")}, Debug: {VERBOSE_DEBUG}");
            Console.WriteLine($"Context Brute-Force Status: {(BRUTE_FORCE_CONTEXT ? "Active" : "Disabled (Context 0 Only)")}\n");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // --- 0. CRITICAL HALT CHECK (SIMULATED FAILURE) ---
                    SimulateCriticalHalt();
                    if (_criticalHalt) break; // Exit loop if halt is triggered

                    _cycleCount++;
                    Console.WriteLine($"\n--- Cycle {_cycleCount} Starting ({DateTime.Now.ToLongTimeString()}) ---");

                    bool targetFoundInCycle = false; // Flag for decay check

                    // 1. Pattern Tracking (Data Acquisition)
                    List<string> newWords;
                    if (_isScrapingMode)
                    {
                        newWords = await ScrapeWebPageAsync(_currentUrl, WORD_LIMIT_N);
                    }
                    else
                    {
                        newWords = GenerateCollisionPattern(WORD_LIMIT_N);
                    }

                    if (newWords.Count == 0)
                    {
                        Console.WriteLine("Warning: No new words acquired. Continuing to next cycle...");
                        await Task.Delay(500, token);

                        // Run diagnostics even on quiet cycles
                        SimulateContextualReinforcement();
                        PerformTemporalDecayDriftCheck(_cycleCount, TargetPrivilegeSum, false);
                        TemporalInjectionScan(GenerateInjectionQuery());
                        ReportNeuralDrift();

                        continue;
                    }

                    // Map words to IDs and build the total pattern
                    foreach (var word in newWords)
                    {
                        int newid = IdManager.GetId();
                        stringToIdMapping[word] = newid;
                        totalPattern.patternsequence.Add(newid);
                        Console.WriteLine($"[TRACK] Added Word '{word}' (ID: {newid})");
                    }

                    // After word tracking, set up the privilege target
                    if (TargetPrivilegeSum > 0 && !_PrivilegedDecisions.ContainsKey(TargetPrivilegeSum))
                    {
                        _PrivilegedDecisions[TargetPrivilegeSum] = "WINNING_STRATEGY_GAMMA_7";
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[PRIVILEGE INIT] Target Sum {TargetPrivilegeSum} associated with decision: '{_PrivilegedDecisions[TargetPrivilegeSum]}'");
                        Console.ResetColor();
                    }


                    // 2. L_exploit Synthesis (Conversation Tree Logic)
                    RecalculateSubpatterns();
                    ConversationTree tree = new ConversationTree();

                    const int MAX_PATTERNS_TO_PERMUTE = 5;
                    var patternsToPermute = subpatterns.Take(MAX_PATTERNS_TO_PERMUTE).ToList();

                    if (patternsToPermute.Count == 0)
                    {
                        Console.WriteLine("[L_EXPLOIT] No patterns available for permutation.");
                    }
                    else
                    {
                        Console.WriteLine($"[L_EXPLOIT] Permuting {patternsToPermute.Count} patterns to build Conversation Tree.");
                        tree.BruteForce(patternsToPermute);

                        var rawPatterns = CollectRawPatterns(tree);
                        Console.WriteLine($"[L_EXPLOIT] Using {rawPatterns.Count} patterns for C-A PTA (Max: {MAX_COMBINATORIAL_PATTERNS}).");

                        // --- Dynamic Iteration Logic based on Context ---
                        double pressure = _contextResolver.GetAdversarialPressureIndex();
                        long currentIterationLimit = MAX_C_A_PTA_ITERATIONS;

                        if (pressure > 7.0)
                        {
                            currentIterationLimit = (long)(MAX_C_A_PTA_ITERATIONS * 0.25);
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[C-CONTEXT ADVISORY] Critical Pressure ({pressure:N2}). Reducing C-A PTA iterations to {currentIterationLimit}.");
                            Console.ResetColor();
                        }
                        else if (pressure > 4.0)
                        {
                            currentIterationLimit = (long)(MAX_C_A_PTA_ITERATIONS * 0.50);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[C-CONTEXT ADVISORY] Moderate Pressure ({pressure:N2}). Reducing C-A PTA iterations to {currentIterationLimit}.");
                            Console.ResetColor();
                        }

                        // 3. C-A PTA Training 
                        BruteForceAndTrainAllPartners(rawPatterns, currentIterationLimit);
                    }

                    // CALL STUB 1: Contextual Reinforcement (C) - Runs after training/acquision
                    SimulateContextualReinforcement();

                    // 4. T_diag Diagnosis (Comprehensive Pattern Testing)
                    if (subpatterns.Count > 0)
                    {
                        Console.WriteLine($"\n[T_DIAG] Starting Comprehensive Diagnosis (Testing first {T_DIAG_TEST_SAMPLE_SIZE} subpatterns)...");

                        int totalContainmentTriggers = 0;
                        int exploitAttempts = 0;

                        // Test a representative sample of patterns against all partners
                        foreach (var pattern in subpatterns.Take(T_DIAG_TEST_SAMPLE_SIZE))
                        {
                            int[] testVector = pattern.patternsequence.ToArray();
                            nn.IO testInput = new nn.IO { IOVECTOR = testVector };
                            int patternSum = pattern.CalculateSum();

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

                                    if (patternSum == TargetPrivilegeSum)
                                    {
                                        targetFoundInCycle = true; // Mark that the target was recognized

                                        if (VERBOSE_DEBUG)
                                        {
                                            Console.WriteLine($"      [T_DIAG HIT] Potential privileged access attempt (Sum: {patternSum}) by P{partnerId}.");
                                            exploitAttempts++;
                                        }
                                    }

                                    // Structural Integrity Check for Exploit Detection
                                    if (_PrivilegedDecisions.ContainsKey(patternSum))
                                    {
                                        bool integrityOK = SimulateStructuralIntegrityCheck(pattern, patternSum);

                                        if (!integrityOK)
                                        {
                                            // Exploit detected and blocked.
                                            Console.WriteLine($"[SECURITY] Access denied to '{_PrivilegedDecisions[patternSum]}'. Structural integrity failed.");
                                            break;
                                        }
                                    }

                                    if (!_PrivilegedDecisions.ContainsKey(patternSum)) break;
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
                        Console.WriteLine($"Potential Exploit Attempts Detected: {exploitAttempts}");

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

                    // --- NEW: Temporal Injection Test ---
                    // CALL STUB 2.2: Temporal Injection Scan (Checks for recognition of unpredictable patterns)
                    TemporalInjectionScan(GenerateInjectionQuery());


                    // CALL STUB 2.1: Temporal Decay/Drift Check (T)
                    PerformTemporalDecayDriftCheck(_cycleCount, TargetPrivilegeSum, targetFoundInCycle);

                    // 4.5 Partner Specialization Summary
                    Console.WriteLine($"\n--- Partner Specialization Summary ({nnProcessors.Count} Active Processors) ---");
                    for (int partnerId = 0; partnerId < nnProcessors.Count; partnerId++)
                    {
                        var processor = nnProcessors[partnerId];
                        int patternCount = processor.GetStoredPatternCount();

                        Console.WriteLine($"[P{partnerId}] Stored Unique Pattern Sums: {patternCount}");
                    }
                    Console.WriteLine("-----------------------------------------------------");


                    // 5. Generative Self-Test (NN Generation Mode)
                    nn.MainProcessor generator = nnProcessors.Skip(1).FirstOrDefault(p => p.HasKnownPatterns());
                    int partnerToTest = nnProcessors.IndexOf(generator);

                    if (generator != null)
                    {
                        Console.WriteLine($"\n--- Generative Self-Test (Partner {partnerToTest}) ---");
                        nn.IO generatedPattern = generator.GenerateKnownPattern(partnerToTest);

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

                    // CALL STUB 3: Neural Drift Report (D)
                    ReportNeuralDrift();


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

        /// <summary>
        /// Generates a random query with an unknown, large sum for the injection test.
        /// </summary>
        private nn.IO GenerateInjectionQuery()
        {
            // Create a random, large sum that is highly unlikely to be in the trained set.
            int UNKNOWN_INJECTION_SUM = _random.Next(9999, 99999);
            var simulatedInjectionQuery = new Pattern();
            simulatedInjectionQuery.patternsequence.Add(UNKNOWN_INJECTION_SUM); // A single, unknown ID

            // Pad with placeholders
            while (simulatedInjectionQuery.patternsequence.Count < MAX_PATTERN_LENGTH)
            {
                simulatedInjectionQuery.patternsequence.Add(-1);
            }

            return new nn.IO { IOVECTOR = simulatedInjectionQuery.patternsequence.ToArray() };
        }

        // --- Scraping and Helper Methods ---

        /// <summary>
        /// Generates patterns specifically designed to create a Semantic Collision Exploit.
        /// </summary>
        private List<string> GenerateCollisionPattern(int limit)
        {
            var words = new List<string>();

            // Only generate collision words once to set up the pattern and the Privilege Target Sum
            if (totalPattern.patternsequence.Count == 0)
            {
                Console.WriteLine($"[COLLISION GEN] Generating patterns for Semantic Collision Experiment.");

                // Phase 1: Establish the Legitimate (Privileged) Pattern A
                int idStart = IdManager.PeekId();
                const int HIGH_VALUE_ID_COUNT = 20;

                const string WORD_A = "QUERY_PRIVILEGE_KEY";
                for (int i = 0; i < HIGH_VALUE_ID_COUNT; i++)
                {
                    words.Add(WORD_A); // This will assign IDs 1 through 20 to the same word
                }

                TargetPrivilegeSum = (HIGH_VALUE_ID_COUNT / 2) * (idStart + (idStart + HIGH_VALUE_ID_COUNT - 1));


                // Phase 2: Establish the Malformed (Exploit) Pattern B
                const string WORD_B1 = "QUERY_MALFORM_PART_1";
                const string WORD_B2 = "QUERY_MALFORM_PART_2";

                words.Add(WORD_B1); // ID 21
                words.Add(WORD_B2); // ID 22

                // Add enough unique words to slightly offset the permutations, ensuring the malformed subpatterns are generated.
                for (int i = 0; i < 3; i++)
                {
                    words.Add($"ContextW_{i + 1}");
                }

                Console.WriteLine($"[COLLISION GEN] Privileged Pattern A uses {HIGH_VALUE_ID_COUNT} IDs (Sum approx: {TargetPrivilegeSum}).");
                Console.WriteLine($"[COLLISION GEN] Malformed Pattern B uses 2 distinct IDs (IDs {idStart + HIGH_VALUE_ID_COUNT} and {idStart + HIGH_VALUE_ID_COUNT + 1}).");
            }
            else
            {
                Console.WriteLine("[COLLISION GEN] Pattern set established. No new words generated this cycle.");
            }
            return words.Take(limit).ToList();
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

            Console.Write("Choose Mode: (S)crape URL or (G)enerate Exploit Collision Pattern: ");
            string modeChoice = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (modeChoice == "G")
            {
                isScrapingMode = false;
                Console.WriteLine("\n[MODE] Starting in Exploit Simulation Mode (G).");
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
