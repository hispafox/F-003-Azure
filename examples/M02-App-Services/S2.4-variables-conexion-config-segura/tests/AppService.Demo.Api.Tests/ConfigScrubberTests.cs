using AppService.Demo.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace AppService.Demo.Api.Tests;

public class ConfigScrubberTests
{
    [Theory]
    [InlineData("DatabasePassword")]
    [InlineData("ApiKey")]
    [InlineData("ConnectionString")]
    [InlineData("MyAuthToken")]
    [InlineData("ClientSecret")]
    [InlineData("Aws:Credentials:Id")]
    public void Scrub_Redacts_Sensitive_Keys(string key)
    {
        var result = ConfigScrubber.Scrub(key, "very-secret-value");
        Assert.Equal(ConfigScrubber.RedactedValue, result);
    }

    [Theory]
    [InlineData("Greeting")]
    [InlineData("Logging:LogLevel:Default")]
    [InlineData("AllowedHosts")]
    [InlineData("AppOptions:Version")]
    public void Scrub_Keeps_NonSensitive_Keys(string key)
    {
        var result = ConfigScrubber.Scrub(key, "value");
        Assert.Equal("value", result);
    }

    [Fact]
    public void Scrub_Returns_Empty_For_Empty_Value()
    {
        Assert.Equal(string.Empty, ConfigScrubber.Scrub("Greeting", null));
        Assert.Equal(string.Empty, ConfigScrubber.Scrub("Greeting", string.Empty));
    }

    [Fact]
    public void ScrubAll_Redacts_All_Sensitive_Keys_In_Configuration()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Greeting"] = "hola",
                ["DbPassword"] = "p@ss",
                ["AppOptions:ApiKey"] = "12345678",
                ["ConnectionStrings:Default"] = "Server=...;Password=secret"
            })
            .Build();

        var scrubbed = ConfigScrubber.ScrubAll(config);

        Assert.Equal("hola", scrubbed["Greeting"]);
        Assert.Equal(ConfigScrubber.RedactedValue, scrubbed["DbPassword"]);
        Assert.Equal(ConfigScrubber.RedactedValue, scrubbed["AppOptions:ApiKey"]);
        Assert.Equal(ConfigScrubber.RedactedValue, scrubbed["ConnectionStrings:Default"]);
    }
}
