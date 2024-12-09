using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm;

internal sealed class CytoscapePassThroughMapping
{
    public CytoscapePassThroughMapping(string visualProperty, string column, string type)
    {
        VisualProperty = visualProperty;
        Column = column;
        Type = type;
    }

    public string Column { get; }
    public string Type { get; }
    public string VisualProperty { get; }

    public string AsJson() {
        return "{" +
            $"\"mappingType\": \"passthrough\"," +
            $"\"mappingColumn\": \"{Column}\"," +
            $"\"mappingColumnType\": \"{Type}\"," +
            $"\"visualProperty\": \"{VisualProperty}\"" +
        "}";
    }
}

internal class CytoscapeVisualPropertyMappings {
    private readonly List<CytoscapePassThroughMapping> _mappings;

    public CytoscapeVisualPropertyMappings()
    {
        _mappings = new List<CytoscapePassThroughMapping>();
    }

    public void AddMapping(CytoscapePassThroughMapping mapping) {
        _mappings.Add(mapping);
    }

    public string AsJson() {
        return $"[{string.Join(",", _mappings.Select(m => m.AsJson()))}]";
    }
}
