using FluentMigrator;

namespace Universalis.DbAccess.Migrations;

// Add user_agent to the upload_log to capture the HTTP User-Agent header from
// the request that generated each audit row. Nullable because not every code
// path that emits an audit entry has a request context (background flushes,
// future internal callers), and clients are not required to send the header.
[Migration(17)]
public class AddUploadLogUserAgent : Migration
{
    public override void Up()
    {
        Alter.Table("upload_log").AddColumn("user_agent").AsString(512).Nullable();
    }

    public override void Down()
    {
        Delete.Column("user_agent").FromTable("upload_log");
    }
}
