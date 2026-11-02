using Prosjekt.Models.Enums;

namespace Prosjekt.Xunit.Test.ModelTest
{
    public class EntryStatusFlowTest
    {
        [Theory]
        [InlineData(EntryStatus.New, EntryStatus.New)]
        [InlineData(EntryStatus.New, EntryStatus.UnderReview)]
        [InlineData(EntryStatus.Assigned, EntryStatus.UnderReview)]
        [InlineData(EntryStatus.Assigned, EntryStatus.Resolved)]
        public void CanMove_SameOrOneStep_ReturnsTrue(EntryStatus from, EntryStatus to)
        {
            var result = EntryStatusFlow.CanMove(from, to);

            Assert.True(result);
        }

        [Theory]
        [InlineData(EntryStatus.New, EntryStatus.Assigned)]
        [InlineData(EntryStatus.New, EntryStatus.Resolved)]
        [InlineData(EntryStatus.Resolved, EntryStatus.UnderReview)]
        public void CanMove_MoreThanOneStep_ReturnsFalse(EntryStatus from, EntryStatus to)
        {
            var result = EntryStatusFlow.CanMove(from, to);

            Assert.False(result);
        }

        [Theory]
        [InlineData(EntryStatus.New, EntryStatus.UnderReview)]
        [InlineData(EntryStatus.UnderReview, EntryStatus.Assigned)]
        [InlineData(EntryStatus.Assigned, EntryStatus.Resolved)]
        public void Next_ReturnsNextStatus(EntryStatus current, EntryStatus expected)
        {
            var result = EntryStatusFlow.Next(current);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Next_WhenResolved_ReturnsNull()
        {
            var result = EntryStatusFlow.Next(EntryStatus.Resolved);

            Assert.Null(result);
        }
    }
}