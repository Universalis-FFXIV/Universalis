using System;
using System.Collections.Generic;
using System.Linq;

namespace Universalis.Application.Common;

public static class InputProcessing
{
    /// <summary>
    /// Parses a string list of IDs into an enumerable of ints. Invalid IDs will be ignored.
    /// </summary>
    /// <param name="idList">The list to parse.</param>
    /// <returns>An enumerable of parsed ints.</returns>
    public static IEnumerable<int> ParseIdList(string idList)
    {
        if (idList == null)
        {
            throw new ArgumentNullException(nameof(idList));
        }

        return idList
            .Replace(" ", "")
            .Split(',')
            .Where(id => int.TryParse(id, out _))
            .Select(int.Parse)
            .Distinct();
    }

    public static HashSet<string> ParseFields(string fields) {
        return new HashSet<string>(fields?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>());
    }
}