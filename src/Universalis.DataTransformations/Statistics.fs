namespace Universalis.DataTransformations

[<AbstractClass; Sealed>]
type Statistics =
    /// <summary>
    /// Calculates the population standard deviation of the provided sequence of numbers.
    /// </summary>
    /// <param name="numbers">The population to calculate the standard deviation over.</param>
    static member PopulationStd(numbers: seq<single>) =
        let average = Seq.average numbers
        let length = Seq.length numbers
        sqrt ((Seq.fold (fun agg next -> agg + (single next - average) ** 2.0f) 0.0f numbers) / single length)

    /// <summary>
    /// Groups the provided sequence of numbers into a dictionary of numbers and the number
    /// of times each number appears in the sequence.
    /// </summary>
    /// <param name="numbers">The sequence of numbers to group.</param>
    static member GetDistribution(numbers: seq<int>) =
        dict (Seq.countBy (fun n -> n) numbers)

    /// <summary>
    /// Calculates the average number of timestamps per day.
    /// </summary>
    /// <param name="timestampsMs">The sequence of millisecond timestamps to evaluate.</param>
    /// <param name="unixNow">The current time in milliseconds since the UNIX epoch.</param>
    /// <param name="period">The period to calculate over.</param>
    static member VelocityPerDay(timestampsMs: seq<int64>, unixNow: int64, period: int64) =
        let filtered = seq { for t in timestampsMs do if t >= unixNow - period then t }
        if Seq.length filtered = 0 || period = 0L then
            0f
        else
            let nDays = single period / 86400000f
            single (Seq.length filtered) / nDays
