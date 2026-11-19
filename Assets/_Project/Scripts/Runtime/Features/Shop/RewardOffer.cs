namespace _Project.Features.Shop
{
    public class RewardOffer
    {
        public RewardOffer(int freeRank, int discountRank)
        {
            FreeRank = freeRank;
            DiscountRank = discountRank;
        }

        public int FreeRank { get; }
        public int DiscountRank { get; }
        public bool HasFree => FreeRank > 0;
        public bool HasDiscount => DiscountRank > 0;
    }
}
