namespace Universalis.DbAccess.Tests
{
    internal static class CollectionUtils
    {
        private const string DatabaseNameBase = "universalisTest";

        public static string GetDatabaseName(string affix)
            => DatabaseNameBase + affix;
    }
}