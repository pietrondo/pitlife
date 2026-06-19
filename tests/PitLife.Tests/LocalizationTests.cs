using PitLife.Localization;

namespace PitLife.Tests;

public class LocalizationTests
{
    [Fact]
    public void DefaultLanguage_IsItalian()
    {
        I18n.SetLanguage("it");

        Assert.Equal("it", I18n.CurrentLanguage);
        Assert.Equal("INIZIA", I18n.T("menu.start"));
        Assert.Equal("Leone", I18n.Species("Lion"));
    }

    [Fact]
    public void Catalogs_HaveIdenticalKeys()
    {
        Assert.Equal(
            I18n.Keys("it").OrderBy(key => key),
            I18n.Keys("en").OrderBy(key => key));
    }

    [Fact]
    public void SetLanguage_SwitchesCatalogAndSupportsRegionalCodes()
    {
        try
        {
            I18n.SetLanguage("en-US");
            Assert.Equal("START", I18n.T("menu.start"));

            I18n.SetLanguage("it-IT");
            Assert.Equal("Età: 3,5s", I18n.Format("creature.age", 3.5f));
        }
        finally
        {
            I18n.SetLanguage("it");
        }
    }

    [Fact]
    public void MissingKey_FallsBackToKeyWithoutThrowing()
    {
        Assert.Equal("missing.key", I18n.T("missing.key"));
    }
}
