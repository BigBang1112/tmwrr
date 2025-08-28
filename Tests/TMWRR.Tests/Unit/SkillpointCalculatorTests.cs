namespace TMWRR.Tests.Unit;

public class SkillpointCalculatorTests
{
    [Test]
    public async Task GetRanksForSkillpoints_ShouldReturnCorrectRanks_ForGivenInput()
    {
        // Arrange
        int[] input = [1, 2, 2, 4, 5, 5, 5, 8];
        int[] expected = [1, 3, 3, 4, 7, 7, 7, 8];

        // Act
        var result = SkillpointCalculator.GetRanksForSkillpoints(input);

        // Assert
        await Assert.That(result).IsEquivalentTo(expected);
    }

    [Test]
    [Arguments(10, 3, 234)]
    [Arguments(5, 1, 400)]
    [Arguments(8, 8, 0)]
    [Arguments(7, 3, 134)]
    public async Task CalculateSkillpoints_ShouldReturnCorrectValue(int playerCount, int rank, int expected)
    {
        // Act
        var result = SkillpointCalculator.CalculateSkillpoints(playerCount, rank);

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }
}
