namespace Universalis.DataTransformations

[<AbstractClass; Sealed>]
type Statistics =
    static member PopulationStd(numbers: seq<float>) =
        let average = Seq.average numbers
        let scaled = Seq.map (fun n -> n / float (Seq.length (numbers))) numbers
        sqrt (Seq.fold (fun agg next -> agg + (float next - average) ** 2.0) 0.0 scaled)
