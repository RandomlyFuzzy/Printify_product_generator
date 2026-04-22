public record ProductMeta
{
    public string Title { get; init; }
    public string Description { get; init; }
    public string[] Tags { get; init; }

    public static ProductMeta PromptExample()
    {
        return new ProductMeta
        {
            Title = "Fiery Phoenix Art Long Sleeve Sweatshirt Graphic - Mythological Flame Design",
            Description = "<ul><li>Embrace the intensity of mythology with this striking long sleeve sweatshirt. Featuring a dynamic graphic of a mythical bird engulfed in vibrant flames, this design captures the essence of the phoenix and the raw power of fire. The artwork showcases intricate details of the bird&#39;s feathers and the intense heat of the flames, set against a bold, fiery orange background. <strong>Features:</strong>High-quality, vivid graphic print that stands out.</li><li>Comfortable and soft fabric perfect for everyday wear.</li><li>Bold design inspired by fantasy and mythological aesthetics.</li><li>Ideal for fans of fire, mythology, tattoo art, and rock aesthetics.</li></ul><p>This sweatshirt makes an incredible statement. It is perfect for adding a touch of dramatic, fiery art to any casual outfit. Use it for lounging, concerts, creative projects, or as a unique gift for art lovers, fantasy enthusiasts, or anyone who loves powerful, dramatic designs. It’s a perfect statement piece for those who appreciate bold, mythological imagery.</p>",
            Tags = "Pheonix, T-shirt, Long Sleeve, Sweatshirt, Greek, Mythology, Casual, Cotton, Comfortable".Split(", ")
        };
    }
}