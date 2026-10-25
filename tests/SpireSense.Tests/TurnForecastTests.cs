using SpireSense.SpireSenseCode.Categories;
using SpireSense.SpireSenseCode.Overlay;
using Xunit;

namespace SpireSense.Tests;

public class TurnForecastTests
{
    private static PlayableAttack Attack(int cost, int damage) => new(cost, damage);

    [Fact]
    public void AnEmptyHandDoesNothing()
    {
        Assert.Equal(0, BestHand.Damage(Array.Empty<PlayableAttack>(), 3, out var played));
        Assert.Equal(0, played);
    }

    [Fact]
    public void PlaysEverythingWhenEnergyAllowsIt()
    {
        var hand = new[] { Attack(1, 6), Attack(1, 9) };

        Assert.Equal(15, BestHand.Damage(hand, 3, out var played));
        Assert.Equal(2, played);
    }

    [Fact]
    public void TakesTheBestSetRatherThanTheBestRate()
    {
        // The case that rules out picking by damage per energy: three cheap attacks have the better
        // rate, but with three energy the single expensive one is worth more than any two of them.
        var hand = new[] { Attack(2, 20), Attack(1, 6), Attack(1, 6), Attack(1, 6) };

        Assert.Equal(26, BestHand.Damage(hand, 3, out var played));
        Assert.Equal(2, played);
    }

    [Fact]
    public void ZeroCostAttacksAreAlwaysIncluded()
    {
        var hand = new[] { Attack(0, 4), Attack(0, 4), Attack(2, 11) };

        Assert.Equal(19, BestHand.Damage(hand, 2, out var played));
        Assert.Equal(3, played);
    }

    [Fact]
    public void ZeroCostAttacksStillCountWithNoEnergyAtAll()
    {
        var hand = new[] { Attack(0, 4), Attack(1, 9) };

        Assert.Equal(4, BestHand.Damage(hand, 0, out var played));
        Assert.Equal(1, played);
    }

    [Fact]
    public void AnAttackYouCannotAffordIsLeftOut()
    {
        var hand = new[] { Attack(3, 30), Attack(1, 5) };

        Assert.Equal(5, BestHand.Damage(hand, 1, out var played));
        Assert.Equal(1, played);
    }

    [Fact]
    public void CardsThatWouldLandForNothingAreIgnored()
    {
        // A card whose damage computes to zero against this enemy is not worth a slot in the set.
        var hand = new[] { Attack(1, 0), Attack(1, 7) };

        Assert.Equal(7, BestHand.Damage(hand, 1, out var played));
        Assert.Equal(1, played);
    }

    [Fact]
    public void NonsenseEnergyIsTreatedAsNoneRatherThanGuessed()
    {
        Assert.Equal(0, BestHand.Damage(new[] { Attack(1, 5) }, -1, out var played));
        Assert.Equal(0, played);
    }

    [Fact]
    public void BlockIsSubtractedFromWhatReachesYou()
    {
        var forecast = new TurnForecast(true, IncomingDamage: 24, Block: 9, 0, 0, 0, 0);

        Assert.Equal(15, forecast.AfterBlock);
    }

    [Fact]
    public void MoreBlockThanDamageMeansNothingGetsThrough()
    {
        var forecast = new TurnForecast(true, IncomingDamage: 6, Block: 20, 0, 0, 0, 0);

        Assert.Equal(0, forecast.AfterBlock);
    }

    [Fact]
    public void EnergyIsTheLimitOnlyWhenAnAttackWasLeftInHand()
    {
        var all = new TurnForecast(true, 0, 0, 0, AttacksPlayed: 3, AttacksInHand: 3, Energy: 3);
        var some = new TurnForecast(true, 0, 0, 0, AttacksPlayed: 2, AttacksInHand: 4, Energy: 3);

        Assert.False(all.EnergyLimited);
        Assert.True(some.EnergyLimited);
    }

    [Fact]
    public void OutsideCombatThereIsNothingToShow()
    {
        Assert.False(TurnForecast.None.HasCombat);
    }
}

public class ThisTurnPanelTests
{
    public ThisTurnPanelTests() => TestData.LoadRealTables();

    private static string Render(TurnForecast forecast)
    {
        var analysis = DeckAnalysis.Analyze(new[] { CardFacts.Named("StrikeIronclad", CardKind.Attack) });
        return OverlayText.Build(analysis, true, new CycleFigures(0, 0, 0, 5.0, Measured: false),
            default, forecast).ThisTurn;
    }

    [Fact]
    public void OutsideCombatTheSectionIsEmptySoItsDividerStaysOff()
    {
        Assert.Equal("", Render(TurnForecast.None));
    }

    [Fact]
    public void ShowsWhatIsComingAndWhatYouCanHitBackFor()
    {
        var text = Render(new TurnForecast(true, IncomingDamage: 24, Block: 0,
            OutgoingDamage: 31, AttacksPlayed: 2, AttacksInHand: 2, Energy: 3));

        Assert.Contains("This turn", text);
        Assert.Contains("Incoming:", text);
        Assert.Contains("24", text);
        Assert.Contains("Your damage:", text);
        Assert.Contains("31", text);
    }

    [Fact]
    public void TheAfterBlockLineAppearsOnlyWhenYouAreHoldingBlock()
    {
        var bare = Render(new TurnForecast(true, 24, 0, 10, 1, 1, 3));
        var blocked = Render(new TurnForecast(true, 24, 9, 10, 1, 1, 3));

        Assert.DoesNotContain("after block", bare);
        Assert.Contains("after block", blocked);
        Assert.Contains("15", blocked);
    }

    [Fact]
    public void SaysHowManyAttacksTheEnergyActuallyCovered()
    {
        var text = Render(new TurnForecast(true, 0, 0, 12, AttacksPlayed: 2, AttacksInHand: 5, Energy: 3));

        Assert.Contains("2 of 5", text);
    }

    [Fact]
    public void DoesNotNagWhenEveryAttackWasAffordable()
    {
        var text = Render(new TurnForecast(true, 0, 0, 12, AttacksPlayed: 3, AttacksInHand: 3, Energy: 3));

        Assert.DoesNotContain(" of 3", text);
    }
}
