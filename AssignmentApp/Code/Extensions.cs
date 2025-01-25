namespace AssignmentApp.Code;

public static class Extensions
{
    public static IReadOnlyList<T> ToReadOnly<T>(this IEnumerable<T> seq)
    {
        return seq.ToArray();
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<T>> ToReadOnly<T>(this IDictionary<string, List<T>> seq)
    {
        return seq.ToDictionary(z => z.Key, z => z.Value.ToReadOnly());
    }

    public static IReadOnlyDictionary<string, T> CopyAdd<T>(this IReadOnlyDictionary<string, T> that, IEnumerable<KeyValuePair<string, T>> toAdd)
    {
        var d = new Dictionary<string, T>();    

        foreach (var kv in that.Concat(toAdd))
        {
            d.Add(kv.Key, kv.Value);
        }
        
        return d;
    }
}
