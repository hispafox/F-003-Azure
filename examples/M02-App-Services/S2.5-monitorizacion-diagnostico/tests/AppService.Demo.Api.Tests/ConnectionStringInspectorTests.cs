using AppService.Demo.Api.Configuration;

namespace AppService.Demo.Api.Tests;

public class ConnectionStringInspectorTests
{
    [Fact]
    public void Extracts_Server_And_Database_Without_Password()
    {
        const string connStr = "Server=tcp:srv.database.windows.net;Database=db-prod;User=admin;Password=secret;Encrypt=true";

        var fields = ConnectionStringInspector.ExtractSafeFields(connStr);

        Assert.Equal("tcp:srv.database.windows.net", fields["Server"]);
        Assert.Equal("db-prod", fields["Database"]);
        Assert.Equal("true", fields["Encrypt"]);
        Assert.False(fields.ContainsKey("Password"));
        Assert.False(fields.ContainsKey("User"));
    }

    [Fact]
    public void Returns_Empty_For_Null_Or_Empty()
    {
        Assert.Empty(ConnectionStringInspector.ExtractSafeFields(null));
        Assert.Empty(ConnectionStringInspector.ExtractSafeFields(""));
        Assert.Empty(ConnectionStringInspector.ExtractSafeFields("   "));
    }

    [Fact]
    public void Recognises_Data_Source_As_Server_Equivalent()
    {
        const string connStr = "Data Source=local\\sql;Initial Catalog=db";

        var fields = ConnectionStringInspector.ExtractSafeFields(connStr);

        Assert.Equal("local\\sql", fields["Data Source"]);
        Assert.Equal("db", fields["Initial Catalog"]);
    }
}
