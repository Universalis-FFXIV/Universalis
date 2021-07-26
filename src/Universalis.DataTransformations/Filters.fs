namespace Universalis.DataTransformations

[<AbstractClass; Sealed>]
type Filters =
    /// <summary>
    /// Removes outliers from a sequence according to standard deviations from the mean,
    /// returning a new sequence with the outliers removed.
    /// </summary>
    /// <param name="numbers">The sequence to filter.</param>
    /// <param name="deviationsFromMean">The number of standard deviations from the mean to keep.</param>
    static member RemoveOutliers(numbers: seq<float>, deviationsFromMean: int) =
        let mean = Seq.average numbers
        let std = Statistics.PopulationStd numbers
        let upperBound = mean + std * float deviationsFromMean
        let lowerBound = mean - std * float deviationsFromMean
        seq { for n in numbers do if n <= upperBound || n >= lowerBound then n }
