using DbUp;
using System.Reflection;

namespace DbMigrator
{
    public static class ScriptStack
    {
        /// <summary>
        /// Runs the migration scripts on the database, in order.
        /// </summary>
        /// <param name="connectionString">The MySQL database connection string.</param>
        public static void Apply(string connectionString)
        {
            EnsureDatabase.For.MySqlDatabase(connectionString);

            var result = DeployChanges.To
                .MySqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .LogToConsole()
                .Build()
                .PerformUpgrade();

            if (!result.Successful)
            {
                throw result.Error;
            }
        }
    }
}