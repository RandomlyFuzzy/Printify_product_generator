namespace PrintifyGenerator.Library.Tests.Modules.Sellingmodules;

public class SellerPlatformFeesTests
{
    [Fact]
    public void EstimatedCost_WithNoListingFee_ReturnsTransactionAndProcessing()
    {
        var fees = new SellerPlatformFees
        {
            ListingFeeUsd = 0m,
            TransactionFeeRate = 0.10m,
            PaymentProcessingRate = 0.03m,
            PaymentProcessingFlatFeeUsd = 0.30m
        };

        var cost = fees.EstimatedCost(100.00m, includeListingFee: false);

        // 0 + 10.00 + 3.00 + 0.30 = 13.30
        Assert.Equal(13.30m, cost);
    }

    [Fact]
    public void EstimatedCost_WithListingFee_IncludesListingFee()
    {
        var fees = new SellerPlatformFees
        {
            ListingFeeUsd = 0.20m,
            TransactionFeeRate = 0.065m,
            PaymentProcessingRate = 0.03m,
            PaymentProcessingFlatFeeUsd = 0.25m
        };

        var cost = fees.EstimatedCost(24.99m, includeListingFee: true);

        // 0.20 + 1.62435 + 0.7497 + 0.25 = 2.82405
        // But with decimal precision it should round correctly
        Assert.Equal(2.82405m, cost);
    }

    [Fact]
    public void EstimatedCost_WithZeroSale_ReturnsFlatFees()
    {
        var fees = new SellerPlatformFees
        {
            ListingFeeUsd = 0.35m,
            TransactionFeeRate = 0.10m,
            PaymentProcessingRate = 0.03m,
            PaymentProcessingFlatFeeUsd = 0.30m
        };

        var cost = fees.EstimatedCost(0m, includeListingFee: true);

        // 0.35 + 0 + 0 + 0.30 = 0.65
        Assert.Equal(0.65m, cost);
    }

    [Fact]
    public void EstimatedCost_WithOnlyTransactionFee_Works()
    {
        var fees = new SellerPlatformFees
        {
            TransactionFeeRate = 0.05m
        };

        var cost = fees.EstimatedCost(200.00m);

        Assert.Equal(10.00m, cost);
    }

    [Fact]
    public void EstimatedCost_WithHighSaleAmount_CalculatesCorrectly()
    {
        var fees = new SellerPlatformFees
        {
            TransactionFeeRate = 0.10m,
            PaymentProcessingRate = 0.03m,
            PaymentProcessingFlatFeeUsd = 0.30m
        };

        var cost = fees.EstimatedCost(1000.00m);

        // 100.00 + 30.00 + 0.30 = 130.30
        Assert.Equal(130.30m, cost);
    }
}
