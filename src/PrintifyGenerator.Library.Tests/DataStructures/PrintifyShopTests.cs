namespace PrintifyGenerator.Library.Tests.DataStructures;

public class PrintifyShopTests
{
    [Fact]
    public void CanCreatePrintifyShop()
    {
        var shop = new PrintifyShop
        {
            id = 12345,
            title = "My Test Shop",
            sales_channel = "shopify"
        };

        Assert.Equal(12345, shop.id);
        Assert.Equal("My Test Shop", shop.title);
        Assert.Equal("shopify", shop.sales_channel);
    }

    [Fact]
    public void DefaultConstructor_SetsEmptyStrings()
    {
        var shop = new PrintifyShop();
        Assert.Equal(0, shop.id);
        Assert.Equal("", shop.title);
        Assert.Equal("", shop.sales_channel);
    }
}
