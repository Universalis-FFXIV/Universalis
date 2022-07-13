using System;
using System.Collections.Generic;
using System.Linq;

namespace Universalis.Application.Common;

public static class InputProcessing
{
    /// <summary>
    /// Parses a string list of IDs into an enumerable of uints. Invalid IDs will be ignored.
    /// </summary>
    /// <param name="idList">The list to parse.</param>
    /// <returns>An enumerable of parsed uints.</returns>
    public static IEnumerable<uint> ParseIdList(string idList)
    {
        if (idList == null)
        {
            throw new ArgumentNullException(nameof(idList));
        }
        
        return idList
            .Replace(" ", "")
            .Split(',')
            .Where(id => uint.TryParse(id, out _))
            .Select(uint.Parse)
            .Distinct();
    }
}