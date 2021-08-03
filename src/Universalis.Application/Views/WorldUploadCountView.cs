namespace Universalis.Application.Views
{
    public class WorldUploadCountView
    {
        /// <summary>
        /// The number of times an upload has occurred on this world.
        /// </summary>
        public double Count { get; set; }

        /// <summary>
        /// The proportion of uploads on this world to the total number of uploads.
        /// </summary>
        public double Proportion { get; set; }
    }
}