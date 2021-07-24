namespace Universalis.DataTransformations

[<AbstractClass; Sealed>]
type Filters =
    static member RemoveOutliers(numbers: seq<float>, deviationsFromMean: int) =
        let mean = Seq.average numbers
        let std = Statistics.PopulationStd numbers
        let upperBound = mean - std * float deviationsFromMean
        let lowerBound = mean - std * float deviationsFromMean
        seq { for n in numbers do if n <= upperBound || n >= lowerBound then n }