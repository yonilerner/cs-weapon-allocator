using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorTest;

public class WeaponSelectionTests : BaseTestFixture
{
    [Test]
    public void SetWeaponPreferenceDirectly()
    {
        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.Terrorist, RoundType.FullBuy),
            Is.EqualTo(null));

        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, RoundType.FullBuy, CsItem.Galil);
        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.Terrorist, RoundType.FullBuy),
            Is.EqualTo(CsItem.Galil));

        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, RoundType.FullBuy, CsItem.AWP);
        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.Terrorist, RoundType.FullBuy),
            Is.EqualTo(CsItem.AWP));

        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, RoundType.Pistol, CsItem.Deagle);
        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.Terrorist, RoundType.Pistol),
            Is.EqualTo(CsItem.Deagle));

        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.CounterTerrorist, RoundType.HalfBuy),
            Is.EqualTo(null));
        Queries.SetWeaponPreferenceForUser(1, CsTeam.CounterTerrorist, RoundType.HalfBuy, CsItem.MP9);
        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.CounterTerrorist, RoundType.HalfBuy),
            Is.EqualTo(CsItem.MP9));
    }

    [Test]
    [TestCase(CsTeam.Terrorist, "galil", CsItem.Galil, "Galil' is now", "Galil' is no longer")]
    [TestCase(CsTeam.Terrorist, "krieg", CsItem.Krieg, "SG553' is now", "SG553' is no longer")]
    [TestCase(CsTeam.Terrorist, "mac10", CsItem.Mac10, "Mac10' is now", "Mac10' is no longer")]
    [TestCase(CsTeam.CounterTerrorist, "deag", CsItem.Deagle, "Deagle' is now", "Deagle' is no longer")]
    [TestCase(CsTeam.CounterTerrorist, "galil", null, "Galil' is not valid", null)]
    [TestCase(CsTeam.CounterTerrorist, "tec9", null, "Tec9' is not valid", null)]
    [TestCase(CsTeam.Terrorist, "poop", null, "not found", null)]
    public void SetWeaponPreferenceCommandSingleArg(
        CsTeam team, string itemInput,
        CsItem? expectedItem,
        string message,
        string? removeMessage
    )
    {
        var args = new List<string> {itemInput};

        var result = OnWeaponCommandHelper.Handle(args, 1, team, false, out var selectedItem);

        Assert.That(result, Does.Contain(message));
        Assert.That(selectedItem, Is.EqualTo(expectedItem));

        var roundType = expectedItem is not null
            ? WeaponHelpers.GetRoundTypeForWeapon(expectedItem.Value) ?? RoundType.Pistol
            : RoundType.Pistol;

        var setWeapon = Queries.GetUserSettings(1)?
            .GetWeaponPreference(team, roundType);
        Assert.That(setWeapon, Is.EqualTo(expectedItem));

        if (removeMessage is not null)
        {
            result = OnWeaponCommandHelper.Handle(args, 1, team, true, out _);
            Assert.That(result, Does.Contain(removeMessage));

            setWeapon = Queries.GetUserSettings(1)?.GetWeaponPreference(team, roundType);
            Assert.That(setWeapon, Is.EqualTo(null));
        }
    }

    [Test]
    [TestCase("T", CsTeam.Terrorist, "galil", CsItem.Galil, "Galil' is now")]
    [TestCase("T", CsTeam.Terrorist, "krieg", CsItem.Krieg, "SG553' is now")]
    [TestCase("T", CsTeam.Terrorist, "mac10", CsItem.Mac10, "Mac10' is now")]
    [TestCase("T", CsTeam.None, "mac10", null, "Mac10' is now")]
    [TestCase("CT", CsTeam.CounterTerrorist, "deag", CsItem.Deagle, "Deagle' is now")]
    [TestCase("CT", CsTeam.CounterTerrorist, "galil", null, "Galil' is not valid")]
    [TestCase("CT", CsTeam.CounterTerrorist, "tec9", null, "Tec9' is not valid")]
    [TestCase("T", CsTeam.Terrorist, "poop", null, "not found")]
    public void SetWeaponPreferenceCommandMultiArg(
        string teamInput,
        CsTeam currentTeam,
        string itemInput,
        CsItem? expectedItem,
        string message
    )
    {
        var args = new List<string> {itemInput, teamInput};

        var result = OnWeaponCommandHelper.Handle(args, 1, currentTeam, false, out var selectedItem);

        Assert.That(result, Does.Contain(message));
        Assert.That(selectedItem, Is.EqualTo(expectedItem));

        var roundType = expectedItem is not null
            ? WeaponHelpers.GetRoundTypeForWeapon(expectedItem.Value) ?? RoundType.Pistol
            : RoundType.Pistol;

        var setWeapon = Queries.GetUserSettings(1)?.GetWeaponPreference(Utils.ParseTeam(teamInput), roundType);
        Assert.That(setWeapon, Is.EqualTo(expectedItem));
    }

    [Test]
    [TestCase("ak", CsItem.AK47, WeaponSelectionType.PlayerChoice, CsItem.AK47, "AK47' is now")]
    [TestCase("ak", CsItem.Galil, WeaponSelectionType.PlayerChoice, null, "not allowed")]
    [TestCase("ak", CsItem.AK47, WeaponSelectionType.Default, null, "cannot choose")]
    public void SetWeaponPreferencesConfig(
        string itemName,
        CsItem? allowedItem,
        WeaponSelectionType weaponSelectionType,
        CsItem? expectedItem,
        string message
    )
    {
        var team = CsTeam.Terrorist;
        Configs.GetConfigData().AllowedWeaponSelectionTypes = new List<WeaponSelectionType> {weaponSelectionType};
        Configs.GetConfigData().UsableWeapons = new List<CsItem> { };
        if (allowedItem is not null)
        {
            Configs.GetConfigData().UsableWeapons.Add(allowedItem.Value);
        }

        var args = new List<string> {itemName};
        var result = OnWeaponCommandHelper.Handle(args, 1, team, false, out var selectedItem);

        Assert.That(result, Does.Contain(message));
        Assert.That(selectedItem, Is.EqualTo(expectedItem));

        var setWeapon = Queries.GetUserSettings(1)?.GetWeaponPreference(team, RoundType.FullBuy);
        Assert.That(setWeapon, Is.EqualTo(expectedItem));
    }

    [Test]
    public void RandomWeaponSelection()
    {
        for (var j = 0; j < 1000; j++)
        {
            Configs.OverrideConfigDataForTests(new ConfigData
            {
                RoundTypePercentages = new()
                {
                    {RoundType.Pistol, 5},
                    {RoundType.HalfBuy, 5},
                    {RoundType.FullBuy, 90},
                }
            });
            var numPistol = 0;
            var numHalfBuy = 0;
            var numFullBuy = 0;
            for (var i = 0; i < 1000; i++)
            {
                var randomRoundType = RoundTypeHelpers.GetRandomRoundType();
                switch (randomRoundType)
                {
                    case RoundType.Pistol:
                        numPistol++;
                        break;
                    case RoundType.HalfBuy:
                        numHalfBuy++;
                        break;
                    case RoundType.FullBuy:
                        numFullBuy++;
                        break;
                }
            }

            // Ranges are very permissive to avoid flakes
            Assert.That(numPistol, Is.InRange(20, 80));
            Assert.That(numHalfBuy, Is.InRange(20, 80));
            Assert.That(numFullBuy, Is.InRange(850, 950));
        }
    }
}
