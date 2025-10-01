using Tracker.nn;
using Tracker;
using System.Collections.Generic;

// Füge die Definition von PatternFilter hinzu, falls sie fehlt
public abstract class PatternFilter
{
    public abstract IO Apply(IO input);
}

// Erweiterung der IO-Klasse für ConversationTree
public class TreeIO : IO
{
    public ConversationTree Tree { get; set; }
}

public class TreeBuilderFilter : PatternFilter
{
    // Optional: Konfiguration für Partner-Bruteforce etc.
    private readonly int maxPatternsToPermute;

    public TreeBuilderFilter(int maxPatternsToPermute = 5)
    {
        this.maxPatternsToPermute = maxPatternsToPermute;
    }

    // Erwartet: IO mit IOVECTOR als Patternsequenz
    // Gibt: TreeIO mit ConversationTree als Property
    public override IO Apply(IO input)
    {
        // Schritt 1: Patternsequenz in Pattern-Objekte umwandeln
        var pattern = new Pattern();
        typeof(Pattern)
            .GetProperty("Sequence")
            .SetValue(pattern, new List<int>(input.IOVECTOR));
        var patternsToPermute = new List<Pattern> { pattern };

        // Schritt 2: ConversationTree generieren
        var tree = new ConversationTree();
        tree.BuildTree(patternsToPermute);

        // Schritt 3: Rückgabe als TreeIO
        return new TreeIO
        {
            IOVECTOR = input.IOVECTOR,
            Tree = tree
        };
    }
}

public class PartnerAssignmentFilter : PatternFilter
{
    public override IO Apply(IO input)
    {
        var treeIO = input as TreeIO;
        if (treeIO?.Tree == null)
            return input;

        // Beispiel: Partner-IDs zuweisen (Dummy-Logik)
        AssignPartnersRecursive(treeIO.Tree, 1);

        return treeIO;
    }

    private void AssignPartnersRecursive(ConversationTree node, int partnerId)
    {
        if (node == null) return;
        // Hier könntest du z. B. node.NodePattern eine Partner-ID zuweisen
        // node.NodePattern.PartnerId = partnerId; // Property ggf. ergänzen!
        foreach (var child in node.Children)
            AssignPartnersRecursive(child, partnerId + 1);
    }
}