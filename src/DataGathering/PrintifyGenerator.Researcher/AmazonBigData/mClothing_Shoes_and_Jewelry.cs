using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class AmazonProduct
{
    // Occurs 7218481
    [JsonPropertyName("main_category")]
    public string maincategory { get; set; }

    // Occurs 7218481
    [JsonPropertyName("bought_together")]
    public string boughttogether { get; set; }

    // Occurs 7218481
    [JsonPropertyName("parent_asin")]
    public string parentasin { get; set; }

    // Occurs 7218481
    [JsonPropertyName("details")]
    public string details { get; set; }

    // Occurs 7218481
    [JsonPropertyName("categories")]
    public string categories { get; set; }

    // Occurs 7218481
    [JsonPropertyName("store")]
    public string store { get; set; }

    // Occurs 7218481
    [JsonPropertyName("images")]
    public string images { get; set; }

    // Occurs 7218481
    [JsonPropertyName("videos")]
    public string videos { get; set; }

    // Occurs 7218481
    [JsonPropertyName("description")]
    public string description { get; set; }

    // Occurs 7218481
    [JsonPropertyName("features")]
    public string features { get; set; }

    // Occurs 7218481
    [JsonPropertyName("rating_number")]
    public string ratingnumber { get; set; }

    // Occurs 7218481
    [JsonPropertyName("average_rating")]
    public string averagerating { get; set; }

    // Occurs 7218481
    [JsonPropertyName("title")]
    public string title { get; set; }

    // Occurs 7218481
    [JsonPropertyName("price")]
    public string price { get; set; }

    // Occurs 24712
    [JsonPropertyName("subtitle")]
    public string subtitle { get; set; }

    // Occurs 24712
    [JsonPropertyName("author")]
    public string author { get; set; }

}
public class Images
{
    // Occurs 34372087
    [JsonPropertyName("large")]
    public string large { get; set; }

    // Occurs 34372087
    [JsonPropertyName("variant")]
    public string variant { get; set; }

    // Occurs 34357370
    [JsonPropertyName("thumb")]
    public string thumb { get; set; }

    // Occurs 34357370
    [JsonPropertyName("hi_res")]
    public string hires { get; set; }

}
public class Videos
{
    // Occurs 1920092
    [JsonPropertyName("title")]
    public string title { get; set; }

    // Occurs 1920092
    [JsonPropertyName("url")]
    public string url { get; set; }

    // Occurs 1920092
    [JsonPropertyName("user_id")]
    public string userid { get; set; }

}
public class Details
{
    // Occurs 6776906
    [JsonPropertyName("Date First Available")]
    public string datefirstavailable { get; set; }

    // Occurs 6302055
    [JsonPropertyName("Department")]
    public string department { get; set; }

    // Occurs 4026135
    [JsonPropertyName("Item model number")]
    public string itemmodelnumber { get; set; }

    // Occurs 3923144
    [JsonPropertyName("Package Dimensions")]
    public string packagedimensions { get; set; }

    // Occurs 3421861
    [JsonPropertyName("Manufacturer")]
    public string manufacturer { get; set; }

    // Occurs 1866475
    [JsonPropertyName("Is Discontinued By Manufacturer,Is Discontinued by Manufacturer")]
    public string isdiscontinuedbymanufacturer { get; set; }

    // Occurs 1482615
    [JsonPropertyName("Product Dimensions")]
    public string productdimensions { get; set; }

    // Occurs 458612
    [JsonPropertyName("Item Weight")]
    public string itemweight { get; set; }

    // Occurs 357903
    [JsonPropertyName("Color")]
    public string color { get; set; }

    // Occurs 340443
    [JsonPropertyName("Brand")]
    public string brand { get; set; }

    // Occurs 305235
    [JsonPropertyName("Material")]
    public string material { get; set; }

    // Occurs 251573
    [JsonPropertyName("Style")]
    public string style { get; set; }

    // Occurs 187725
    [JsonPropertyName("Age Range (Description),Age Range Description")]
    public string agerangedescription { get; set; }

    // Occurs 155262
    [JsonPropertyName("Best Sellers Rank")]
    public string bestsellersrank { get; set; }

    // Occurs 133371
    [JsonPropertyName("Size")]
    public string size { get; set; }

    // Occurs 106145
    [JsonPropertyName("Country of Origin")]
    public string countryoforigin { get; set; }

    // Occurs 86907
    [JsonPropertyName("Brand Name")]
    public string brandname { get; set; }

    // Occurs 82624
    [JsonPropertyName("Part Number,Part number")]
    public string partnumber { get; set; }

    // Occurs 76860
    [JsonPropertyName("Package Weight")]
    public string packageweight { get; set; }

    // Occurs 76606
    [JsonPropertyName("Suggested Users")]
    public string suggestedusers { get; set; }

    // Occurs 76538
    [JsonPropertyName("Item Package Dimensions L x W x H")]
    public string itempackagedimensionslxwxh { get; set; }

    // Occurs 74895
    [JsonPropertyName("Batteries")]
    public string batteries { get; set; }

    // Occurs 73422
    [JsonPropertyName("Special Feature")]
    public string specialfeature { get; set; }

    // Occurs 60914
    [JsonPropertyName("Closure Type")]
    public string closuretype { get; set; }

    // Occurs 49855
    [JsonPropertyName("Pattern")]
    public string pattern { get; set; }

    // Occurs 46578
    [JsonPropertyName("Shape")]
    public string shape { get; set; }

    // Occurs 44915
    [JsonPropertyName("Item Dimensions  LxWxH,Item Dimensions LxWxH,Item Dimensions L x W x H")]
    public string itemdimensionslxwxh { get; set; }

    // Occurs 40323
    [JsonPropertyName("Batteries required,Batteries Required?,Batteries Required")]
    public string batteriesrequired { get; set; }

    // Occurs 37723
    [JsonPropertyName("Sport Type")]
    public string sporttype { get; set; }

    // Occurs 36051
    [JsonPropertyName("Manufacturer recommended age")]
    public string manufacturerrecommendedage { get; set; }

    // Occurs 35518
    [JsonPropertyName("Number of Items,Number Of Items")]
    public string numberofitems { get; set; }

    // Occurs 33440
    [JsonPropertyName("Fit Type")]
    public string fittype { get; set; }

    // Occurs 32418
    [JsonPropertyName("Occasion")]
    public string occasion { get; set; }

    // Occurs 31814
    [JsonPropertyName("Theme")]
    public string theme { get; set; }

    // Occurs 30240
    [JsonPropertyName("Neck Style")]
    public string neckstyle { get; set; }

    // Occurs 29456
    [JsonPropertyName("Fabric Type")]
    public string fabrictype { get; set; }

    // Occurs 25925
    [JsonPropertyName("Item Package Quantity")]
    public string itempackagequantity { get; set; }

    // Occurs 23970
    [JsonPropertyName("Model Year")]
    public string modelyear { get; set; }

    // Occurs 22796
    [JsonPropertyName("Sport")]
    public string sport { get; set; }

    // Occurs 20908
    [JsonPropertyName("Metal Type")]
    public string metaltype { get; set; }

    // Occurs 20903
    [JsonPropertyName("Sleeve Type")]
    public string sleevetype { get; set; }

    // Occurs 20891
    [JsonPropertyName("Model Name")]
    public string modelname { get; set; }

    // Occurs 20041
    [JsonPropertyName("Included Components")]
    public string includedcomponents { get; set; }

    // Occurs 19910
    [JsonPropertyName("Number Of Pieces,Number of pieces,Number of Pieces")]
    public string numberofpieces { get; set; }

    // Occurs 19119
    [JsonPropertyName("Number of Compartments")]
    public string numberofcompartments { get; set; }

    // Occurs 18138
    [JsonPropertyName("Product Care Instructions")]
    public string productcareinstructions { get; set; }

    // Occurs 17491
    [JsonPropertyName("Target Audience")]
    public string targetaudience { get; set; }

    // Occurs 17440
    [JsonPropertyName("Outer Material")]
    public string outermaterial { get; set; }

    // Occurs 15430
    [JsonPropertyName("Inner Material")]
    public string innermaterial { get; set; }

    // Occurs 14399
    [JsonPropertyName("Manufacturer Part Number")]
    public string manufacturerpartnumber { get; set; }

    // Occurs 14118
    [JsonPropertyName("Special Features,Special features")]
    public string specialfeatures { get; set; }

    // Occurs 13845
    [JsonPropertyName("Mounting Type")]
    public string mountingtype { get; set; }

    // Occurs 13830
    [JsonPropertyName("Finish Type")]
    public string finishtype { get; set; }

    // Occurs 13650
    [JsonPropertyName("Hand Orientation")]
    public string handorientation { get; set; }

    // Occurs 13440
    [JsonPropertyName("Material Type")]
    public string materialtype { get; set; }

    // Occurs 13418
    [JsonPropertyName("Batteries Included?,Batteries Included")]
    public string batteriesincluded { get; set; }

    // Occurs 13185
    [JsonPropertyName("Capacity Total")]
    public string capacitytotal { get; set; }

    // Occurs 12819
    [JsonPropertyName("UPC")]
    public string upc { get; set; }

    // Occurs 10742
    [JsonPropertyName("Hair Type")]
    public string hairtype { get; set; }

    // Occurs 10656
    [JsonPropertyName("Number of Drawers")]
    public string numberofdrawers { get; set; }

    // Occurs 8562
    [JsonPropertyName("Recommended Uses For Product")]
    public string recommendedusesforproduct { get; set; }

    // Occurs 8421
    [JsonPropertyName("Shirt form type")]
    public string shirtformtype { get; set; }

    // Occurs 8141
    [JsonPropertyName("Frame Material")]
    public string framematerial { get; set; }

    // Occurs 7904
    [JsonPropertyName("Clasp Type")]
    public string clasptype { get; set; }

    // Occurs 7481
    [JsonPropertyName("Finish")]
    public string finish { get; set; }

    // Occurs 7184
    [JsonPropertyName("Reusability")]
    public string reusability { get; set; }

    // Occurs 7149
    [JsonPropertyName("Lining Description")]
    public string liningdescription { get; set; }

    // Occurs 7026
    [JsonPropertyName("Unit Count")]
    public string unitcount { get; set; }

    // Occurs 6531
    [JsonPropertyName("Units")]
    public string units { get; set; }

    // Occurs 6307
    [JsonPropertyName("Metal Stamp")]
    public string metalstamp { get; set; }

    // Occurs 5545
    [JsonPropertyName("Warranty Description")]
    public string warrantydescription { get; set; }

    // Occurs 5433
    [JsonPropertyName("Care instructions")]
    public string careinstructions { get; set; }

    // Occurs 5334
    [JsonPropertyName("Handle Material")]
    public string handlematerial { get; set; }

    // Occurs 5000
    [JsonPropertyName("Closure")]
    public string closure { get; set; }

    // Occurs 4754
    [JsonPropertyName("Finish types")]
    public string finishtypes { get; set; }

    // Occurs 4074
    [JsonPropertyName("Release date")]
    public string releasedate { get; set; }

    // Occurs 3995
    [JsonPropertyName("International Shipping")]
    public string internationalshipping { get; set; }

    // Occurs 3995
    [JsonPropertyName("Domestic Shipping")]
    public string domesticshipping { get; set; }

    // Occurs 3948
    [JsonPropertyName("Import")]
    public string import { get; set; }

    // Occurs 3935
    [JsonPropertyName("Chain Type")]
    public string chaintype { get; set; }

    // Occurs 3612
    [JsonPropertyName("Specific Uses For Product")]
    public string specificusesforproduct { get; set; }

    // Occurs 3584
    [JsonPropertyName("Cartoon Character")]
    public string cartooncharacter { get; set; }

    // Occurs 3501
    [JsonPropertyName("Item Length (Description)")]
    public string itemlengthdescription { get; set; }

    // Occurs 3366
    [JsonPropertyName("Dimensions")]
    public string dimensions { get; set; }

    // Occurs 3359
    [JsonPropertyName("Collar Style")]
    public string collarstyle { get; set; }

    // Occurs 3116
    [JsonPropertyName("Other display features")]
    public string otherdisplayfeatures { get; set; }

    // Occurs 3105
    [JsonPropertyName("Target gender,Target Gender")]
    public string targetgender { get; set; }

    // Occurs 3018
    [JsonPropertyName("Number of Sets")]
    public string numberofsets { get; set; }

    // Occurs 2976
    [JsonPropertyName("Form Factor")]
    public string formfactor { get; set; }

    // Occurs 2442
    [JsonPropertyName("Assembly required,Assembly Required")]
    public string assemblyrequired { get; set; }

    // Occurs 2358
    [JsonPropertyName("Seasons")]
    public string seasons { get; set; }

    // Occurs 2204
    [JsonPropertyName("Capacity")]
    public string capacity { get; set; }

    // Occurs 2136
    [JsonPropertyName("Shaft Material")]
    public string shaftmaterial { get; set; }

    // Occurs 1956
    [JsonPropertyName("Item Length")]
    public string itemlength { get; set; }

    // Occurs 1896
    [JsonPropertyName("Item Form")]
    public string itemform { get; set; }

    // Occurs 1742
    [JsonPropertyName("Collection Name")]
    public string collectionname { get; set; }

    // Occurs 1738
    [JsonPropertyName("Stone Color")]
    public string stonecolor { get; set; }

    // Occurs 1609
    [JsonPropertyName("Model")]
    public string model { get; set; }

    // Occurs 1597
    [JsonPropertyName("Opening Mechanism")]
    public string openingmechanism { get; set; }

    // Occurs 1584
    [JsonPropertyName("Number of Labels")]
    public string numberoflabels { get; set; }

    // Occurs 1509
    [JsonPropertyName("Primary Stone Gem Type")]
    public string primarystonegemtype { get; set; }

    // Occurs 1390
    [JsonPropertyName("Use for")]
    public string usefor { get; set; }

    // Occurs 1316
    [JsonPropertyName("Additional product features")]
    public string additionalproductfeatures { get; set; }

    // Occurs 1303
    [JsonPropertyName("Water Resistance Depth")]
    public string waterresistancedepth { get; set; }

    // Occurs 1282
    [JsonPropertyName("Tick-repellent material")]
    public string tickrepellentmaterial { get; set; }

    // Occurs 1207
    [JsonPropertyName("Band Color")]
    public string bandcolor { get; set; }

    // Occurs 1201
    [JsonPropertyName("Vehicle Service Type")]
    public string vehicleservicetype { get; set; }

    // Occurs 1188
    [JsonPropertyName("Compatible Devices")]
    public string compatibledevices { get; set; }

    // Occurs 1175
    [JsonPropertyName("Gem Type")]
    public string gemtype { get; set; }

    // Occurs 1153
    [JsonPropertyName("Fabric cleaning")]
    public string fabriccleaning { get; set; }

    // Occurs 1151
    [JsonPropertyName("Band Material Type")]
    public string bandmaterialtype { get; set; }

    // Occurs 1143
    [JsonPropertyName("Language")]
    public string language { get; set; }

    // Occurs 1066
    [JsonPropertyName("Usage")]
    public string usage { get; set; }

    // Occurs 1021
    [JsonPropertyName("Fastener Type")]
    public string fastenertype { get; set; }

    // Occurs 1019
    [JsonPropertyName("Fastener Material")]
    public string fastenermaterial { get; set; }

    // Occurs 1014
    [JsonPropertyName("Import Designation")]
    public string importdesignation { get; set; }

    // Occurs 968
    [JsonPropertyName("material_composition,Material Composition")]
    public string materialcomposition { get; set; }

    // Occurs 929
    [JsonPropertyName("Material Feature")]
    public string materialfeature { get; set; }

    // Occurs 910
    [JsonPropertyName("Band Size")]
    public string bandsize { get; set; }

    // Occurs 838
    [JsonPropertyName("Team Name")]
    public string teamname { get; set; }

    // Occurs 819
    [JsonPropertyName("Power Source")]
    public string powersource { get; set; }

    // Occurs 810
    [JsonPropertyName("Stone Cut")]
    public string stonecut { get; set; }

    // Occurs 800
    [JsonPropertyName("Product Benefits")]
    public string productbenefits { get; set; }

    // Occurs 795
    [JsonPropertyName("")]
    public string Property { get; set; }

    // Occurs 774
    [JsonPropertyName("Color Name")]
    public string colorname { get; set; }

    // Occurs 753
    [JsonPropertyName("Maximum weight recommendation,Maximum Weight Recommendation")]
    public string maximumweightrecommendation { get; set; }

    // Occurs 668
    [JsonPropertyName("Antibacterial treatment")]
    public string antibacterialtreatment { get; set; }

    // Occurs 626
    [JsonPropertyName("Strap Type")]
    public string straptype { get; set; }

    // Occurs 602
    [JsonPropertyName("Screen Size")]
    public string screensize { get; set; }

    // Occurs 602
    [JsonPropertyName("Lock Type")]
    public string locktype { get; set; }

    // Occurs 598
    [JsonPropertyName("Compatible Phone Models")]
    public string compatiblephonemodels { get; set; }

    // Occurs 596
    [JsonPropertyName("Ply Rating")]
    public string plyrating { get; set; }

    // Occurs 582
    [JsonPropertyName("Display type,Display Type")]
    public string displaytype { get; set; }

    // Occurs 572
    [JsonPropertyName("Surface Recommendation")]
    public string surfacerecommendation { get; set; }

    // Occurs 572
    [JsonPropertyName("Band Width")]
    public string bandwidth { get; set; }

    // Occurs 565
    [JsonPropertyName("Whats in the box")]
    public string whatsinthebox { get; set; }

    // Occurs 556
    [JsonPropertyName("Incontinence Protector Type")]
    public string incontinenceprotectortype { get; set; }

    // Occurs 543
    [JsonPropertyName("Water Resistance Level")]
    public string waterresistancelevel { get; set; }

    // Occurs 541
    [JsonPropertyName("Weight")]
    public string weight { get; set; }

    // Occurs 535
    [JsonPropertyName("Room Type")]
    public string roomtype { get; set; }

    // Occurs 514
    [JsonPropertyName("Wallet compartment type")]
    public string walletcompartmenttype { get; set; }

    // Occurs 483
    [JsonPropertyName("Specific instructions for use")]
    public string specificinstructionsforuse { get; set; }

    // Occurs 476
    [JsonPropertyName("Skill Level")]
    public string skilllevel { get; set; }

    // Occurs 463
    [JsonPropertyName("Compatible Material")]
    public string compatiblematerial { get; set; }

    // Occurs 454
    [JsonPropertyName("Inside Pockets")]
    public string insidepockets { get; set; }

    // Occurs 446
    [JsonPropertyName("Weight Limit")]
    public string weightlimit { get; set; }

    // Occurs 436
    [JsonPropertyName("Package Type")]
    public string packagetype { get; set; }

    // Occurs 434
    [JsonPropertyName("Pocket Description")]
    public string pocketdescription { get; set; }

    // Occurs 429
    [JsonPropertyName("Standing screen display size")]
    public string standingscreendisplaysize { get; set; }

    // Occurs 413
    [JsonPropertyName("Glove Type")]
    public string glovetype { get; set; }

    // Occurs 408
    [JsonPropertyName("Scent")]
    public string scent { get; set; }

    // Occurs 404
    [JsonPropertyName("Lens Color")]
    public string lenscolor { get; set; }

    // Occurs 389
    [JsonPropertyName("Waist (cm)")]
    public string waistcm { get; set; }

    // Occurs 380
    [JsonPropertyName("Control Method")]
    public string controlmethod { get; set; }

    // Occurs 372
    [JsonPropertyName("Package Information")]
    public string packageinformation { get; set; }

    // Occurs 365
    [JsonPropertyName("Controller Type")]
    public string controllertype { get; set; }

    // Occurs 346
    [JsonPropertyName("Total Eaches")]
    public string totaleaches { get; set; }

    // Occurs 339
    [JsonPropertyName("Frame Type")]
    public string frametype { get; set; }

    // Occurs 339
    [JsonPropertyName("Number of Hooks")]
    public string numberofhooks { get; set; }

    // Occurs 329
    [JsonPropertyName("Stone Weight")]
    public string stoneweight { get; set; }

    // Occurs 326
    [JsonPropertyName("Filter Class")]
    public string filterclass { get; set; }

    // Occurs 325
    [JsonPropertyName("Total Diamond Weight")]
    public string totaldiamondweight { get; set; }

    // Occurs 322
    [JsonPropertyName("Operation Mode")]
    public string operationmode { get; set; }

    // Occurs 317
    [JsonPropertyName("Country/Region of origin,Country/Region Of Origin")]
    public string countryregionoforigin { get; set; }

    // Occurs 305
    [JsonPropertyName("Mounting Type Insert Mount")]
    public string mountingtypeinsertmount { get; set; }

    // Occurs 304
    [JsonPropertyName("Top Style")]
    public string topstyle { get; set; }

    // Occurs 284
    [JsonPropertyName("League")]
    public string league { get; set; }

    // Occurs 283
    [JsonPropertyName("Minimum weight recommendation,Minimum Weight Recommendation")]
    public string minimumweightrecommendation { get; set; }

    // Occurs 277
    [JsonPropertyName("Measurement System")]
    public string measurementsystem { get; set; }

    // Occurs 269
    [JsonPropertyName("Warranty Type")]
    public string warrantytype { get; set; }

    // Occurs 263
    [JsonPropertyName("Assembled Width")]
    public string assembledwidth { get; set; }

    // Occurs 262
    [JsonPropertyName("Is Stain Resistant")]
    public string isstainresistant { get; set; }

    // Occurs 258
    [JsonPropertyName("Our Recommended age")]
    public string ourrecommendedage { get; set; }

    // Occurs 254
    [JsonPropertyName("Specifications")]
    public string specifications { get; set; }

    // Occurs 253
    [JsonPropertyName("Target Species")]
    public string targetspecies { get; set; }

    // Occurs 251
    [JsonPropertyName("Assembled Height")]
    public string assembledheight { get; set; }

    // Occurs 251
    [JsonPropertyName("Is Dishwasher Safe")]
    public string isdishwashersafe { get; set; }

    // Occurs 248
    [JsonPropertyName("Connectivity Technology")]
    public string connectivitytechnology { get; set; }

    // Occurs 246
    [JsonPropertyName("Lifestyle")]
    public string lifestyle { get; set; }

    // Occurs 240
    [JsonPropertyName("Assembled Length")]
    public string assembledlength { get; set; }

    // Occurs 233
    [JsonPropertyName("Is Customizable")]
    public string iscustomizable { get; set; }

    // Occurs 229
    [JsonPropertyName("Are Batteries Included")]
    public string arebatteriesincluded { get; set; }

    // Occurs 229
    [JsonPropertyName("Maximum recommended load")]
    public string maximumrecommendedload { get; set; }

    // Occurs 224
    [JsonPropertyName("Orientation")]
    public string orientation { get; set; }

    // Occurs 219
    [JsonPropertyName("Size Map")]
    public string sizemap { get; set; }

    // Occurs 213
    [JsonPropertyName("Blade Material")]
    public string bladematerial { get; set; }

    // Occurs 211
    [JsonPropertyName("Series")]
    public string series { get; set; }

    // Occurs 210
    [JsonPropertyName("Shell Type")]
    public string shelltype { get; set; }

    // Occurs 207
    [JsonPropertyName("Volume")]
    public string volume { get; set; }

    // Occurs 205
    [JsonPropertyName("OEM Part Number")]
    public string oempartnumber { get; set; }

    // Occurs 204
    [JsonPropertyName("Embellishment")]
    public string embellishment { get; set; }

    // Occurs 195
    [JsonPropertyName("Exterior")]
    public string exterior { get; set; }

    // Occurs 191
    [JsonPropertyName("Item Volume")]
    public string itemvolume { get; set; }

    // Occurs 190
    [JsonPropertyName("Pricing")]
    public string pricing { get; set; }

    // Occurs 182
    [JsonPropertyName("Towel form type")]
    public string towelformtype { get; set; }

    // Occurs 182
    [JsonPropertyName("Type of item")]
    public string typeofitem { get; set; }

    // Occurs 181
    [JsonPropertyName("Sole Material")]
    public string solematerial { get; set; }

    // Occurs 180
    [JsonPropertyName("Load Capacity")]
    public string loadcapacity { get; set; }

    // Occurs 175
    [JsonPropertyName("Back Style")]
    public string backstyle { get; set; }

    // Occurs 171
    [JsonPropertyName("Cover Material")]
    public string covermaterial { get; set; }

    // Occurs 168
    [JsonPropertyName("Perfume")]
    public string perfume { get; set; }

    // Occurs 167
    [JsonPropertyName("Skin Type,Skin type")]
    public string skintype { get; set; }

    // Occurs 167
    [JsonPropertyName("Best uses")]
    public string bestuses { get; set; }

    // Occurs 163
    [JsonPropertyName("Year")]
    public string year { get; set; }

    // Occurs 163
    [JsonPropertyName("Installation Type")]
    public string installationtype { get; set; }

    // Occurs 162
    [JsonPropertyName("Is portable")]
    public string isportable { get; set; }

    // Occurs 162
    [JsonPropertyName("Publisher")]
    public string publisher { get; set; }

    // Occurs 157
    [JsonPropertyName("Blade Length")]
    public string bladelength { get; set; }

    // Occurs 155
    [JsonPropertyName("Voltage")]
    public string voltage { get; set; }

    // Occurs 154
    [JsonPropertyName("Display Style")]
    public string displaystyle { get; set; }

    // Occurs 145
    [JsonPropertyName("Label")]
    public string label { get; set; }

    // Occurs 141
    [JsonPropertyName("Number of discs,Number Of Discs")]
    public string numberofdiscs { get; set; }

    // Occurs 139
    [JsonPropertyName("Paper Finish")]
    public string paperfinish { get; set; }

    // Occurs 138
    [JsonPropertyName("Pre-printed")]
    public string preprinted { get; set; }

    // Occurs 138
    [JsonPropertyName("Thickness")]
    public string thickness { get; set; }

    // Occurs 138
    [JsonPropertyName("Blanket Form")]
    public string blanketform { get; set; }

    // Occurs 137
    [JsonPropertyName("ISBN 13")]
    public string isbn13 { get; set; }

    // Occurs 136
    [JsonPropertyName("ISBN 10")]
    public string isbn10 { get; set; }

    // Occurs 131
    [JsonPropertyName("Indoor/Outdoor Usage")]
    public string indooroutdoorusage { get; set; }

    // Occurs 130
    [JsonPropertyName("Human Interface Input")]
    public string humaninterfaceinput { get; set; }

    // Occurs 125
    [JsonPropertyName("Wheel type,Wheel Type")]
    public string wheeltype { get; set; }

    // Occurs 125
    [JsonPropertyName("Grip Size")]
    public string gripsize { get; set; }

    // Occurs 125
    [JsonPropertyName("Installation Method")]
    public string installationmethod { get; set; }

    // Occurs 122
    [JsonPropertyName("Head Style")]
    public string headstyle { get; set; }

    // Occurs 119
    [JsonPropertyName("Sheet Count")]
    public string sheetcount { get; set; }

    // Occurs 115
    [JsonPropertyName("Grip Type")]
    public string griptype { get; set; }

    // Occurs 113
    [JsonPropertyName("Type of Bulb")]
    public string typeofbulb { get; set; }

    // Occurs 111
    [JsonPropertyName("Light Source Type")]
    public string lightsourcetype { get; set; }

    // Occurs 110
    [JsonPropertyName("Height Map")]
    public string heightmap { get; set; }

    // Occurs 110
    [JsonPropertyName("Number Of Pockets")]
    public string numberofpockets { get; set; }

    // Occurs 109
    [JsonPropertyName("Certification")]
    public string certification { get; set; }

    // Occurs 106
    [JsonPropertyName("Extended Length")]
    public string extendedlength { get; set; }

    // Occurs 104
    [JsonPropertyName("Is Autographed")]
    public string isautographed { get; set; }

    // Occurs 100
    [JsonPropertyName("Horsepower")]
    public string horsepower { get; set; }

    // Occurs 100
    [JsonPropertyName("Auto Shutoff")]
    public string autoshutoff { get; set; }

    // Occurs 100
    [JsonPropertyName("Battery Cell Type")]
    public string batterycelltype { get; set; }

    // Occurs 98
    [JsonPropertyName("Number of Batteries")]
    public string numberofbatteries { get; set; }

    // Occurs 97
    [JsonPropertyName("UV Protection,UV protection")]
    public string uvprotection { get; set; }

    // Occurs 96
    [JsonPropertyName("Fill Material Type")]
    public string fillmaterialtype { get; set; }

    // Occurs 96
    [JsonPropertyName("Dishwasher compatible")]
    public string dishwashercompatible { get; set; }

    // Occurs 96
    [JsonPropertyName("Connector Type")]
    public string connectortype { get; set; }

    // Occurs 95
    [JsonPropertyName("Educational Objective")]
    public string educationalobjective { get; set; }

    // Occurs 95
    [JsonPropertyName("Number of Wheels")]
    public string numberofwheels { get; set; }

    // Occurs 95
    [JsonPropertyName("National Stock Number")]
    public string nationalstocknumber { get; set; }

    // Occurs 94
    [JsonPropertyName("Number of Handles")]
    public string numberofhandles { get; set; }

    // Occurs 94
    [JsonPropertyName("Unit Grouping")]
    public string unitgrouping { get; set; }

    // Occurs 94
    [JsonPropertyName("Exterior Finish")]
    public string exteriorfinish { get; set; }

    // Occurs 93
    [JsonPropertyName("Refill")]
    public string refill { get; set; }

    // Occurs 93
    [JsonPropertyName("Assembled Diameter")]
    public string assembleddiameter { get; set; }

    // Occurs 92
    [JsonPropertyName("Fill Material")]
    public string fillmaterial { get; set; }

    // Occurs 92
    [JsonPropertyName("Battery Description")]
    public string batterydescription { get; set; }

    // Occurs 92
    [JsonPropertyName("Item Firmness Description")]
    public string itemfirmnessdescription { get; set; }

    // Occurs 91
    [JsonPropertyName("Screen Surface Description")]
    public string screensurfacedescription { get; set; }

    // Occurs 89
    [JsonPropertyName("Cutting Diameter")]
    public string cuttingdiameter { get; set; }

    // Occurs 88
    [JsonPropertyName("Neck Size")]
    public string necksize { get; set; }

    // Occurs 87
    [JsonPropertyName("Dishwasher safe")]
    public string dishwashersafe { get; set; }

    // Occurs 86
    [JsonPropertyName("Connectivity technologies")]
    public string connectivitytechnologies { get; set; }

    // Occurs 86
    [JsonPropertyName("Spout Height")]
    public string spoutheight { get; set; }

    // Occurs 85
    [JsonPropertyName("Wattage")]
    public string wattage { get; set; }

    // Occurs 85
    [JsonPropertyName("Lens Material")]
    public string lensmaterial { get; set; }

    // Occurs 84
    [JsonPropertyName("Paperback")]
    public string paperback { get; set; }

    // Occurs 82
    [JsonPropertyName("Format")]
    public string format { get; set; }

    // Occurs 82
    [JsonPropertyName("Grade Rating")]
    public string graderating { get; set; }

    // Occurs 80
    [JsonPropertyName("Binding")]
    public string binding { get; set; }

    // Occurs 80
    [JsonPropertyName("Total gem weight")]
    public string totalgemweight { get; set; }

    // Occurs 80
    [JsonPropertyName("Base Type")]
    public string basetype { get; set; }

    // Occurs 79
    [JsonPropertyName("Grip Material")]
    public string gripmaterial { get; set; }

    // Occurs 75
    [JsonPropertyName("Insole Material")]
    public string insolematerial { get; set; }

    // Occurs 73
    [JsonPropertyName("Ink Color")]
    public string inkcolor { get; set; }

    // Occurs 71
    [JsonPropertyName("Sheet Size")]
    public string sheetsize { get; set; }

    // Occurs 69
    [JsonPropertyName("Coverage")]
    public string coverage { get; set; }

    // Occurs 69
    [JsonPropertyName("Handle Type")]
    public string handletype { get; set; }

    // Occurs 68
    [JsonPropertyName("Caster Type")]
    public string castertype { get; set; }

    // Occurs 68
    [JsonPropertyName("Alarm Clock")]
    public string alarmclock { get; set; }

    // Occurs 67
    [JsonPropertyName("Variety")]
    public string variety { get; set; }

    // Occurs 67
    [JsonPropertyName("Number of Players")]
    public string numberofplayers { get; set; }

    // Occurs 67
    [JsonPropertyName("Insole Type")]
    public string insoletype { get; set; }

    // Occurs 67
    [JsonPropertyName("Material free")]
    public string materialfree { get; set; }

    // Occurs 66
    [JsonPropertyName("Wireless communication technologies")]
    public string wirelesscommunicationtechnologies { get; set; }

    // Occurs 66
    [JsonPropertyName("Maximum Height Recommendation,Maximum height recommendation")]
    public string maximumheightrecommendation { get; set; }

    // Occurs 65
    [JsonPropertyName("Arch Type")]
    public string archtype { get; set; }

    // Occurs 65
    [JsonPropertyName("Handle/Lever Placement")]
    public string handleleverplacement { get; set; }

    // Occurs 64
    [JsonPropertyName("Flavor")]
    public string flavor { get; set; }

    // Occurs 64
    [JsonPropertyName("Readout Accuracy")]
    public string readoutaccuracy { get; set; }

    // Occurs 64
    [JsonPropertyName("Number of Shelves,Number of shelves")]
    public string numberofshelves { get; set; }

    // Occurs 63
    [JsonPropertyName("Lens Coating Description")]
    public string lenscoatingdescription { get; set; }

    // Occurs 63
    [JsonPropertyName("Breed Recommendation")]
    public string breedrecommendation { get; set; }

    // Occurs 61
    [JsonPropertyName("Is Oven Safe")]
    public string isovensafe { get; set; }

    // Occurs 61
    [JsonPropertyName("Furniture Finish")]
    public string furniturefinish { get; set; }

    // Occurs 60
    [JsonPropertyName("OS")]
    public string os { get; set; }

    // Occurs 58
    [JsonPropertyName("Battery Cell Composition")]
    public string batterycellcomposition { get; set; }

    // Occurs 57
    [JsonPropertyName("Connector Gender")]
    public string connectorgender { get; set; }

    // Occurs 57
    [JsonPropertyName("Original Release Date")]
    public string originalreleasedate { get; set; }

    // Occurs 55
    [JsonPropertyName("Wheel Size")]
    public string wheelsize { get; set; }

    // Occurs 55
    [JsonPropertyName("Battery Life,Battery life")]
    public string batterylife { get; set; }

    // Occurs 53
    [JsonPropertyName("Life Vest Type")]
    public string lifevesttype { get; set; }

    // Occurs 52
    [JsonPropertyName("Fabric Warmth Description")]
    public string fabricwarmthdescription { get; set; }

    // Occurs 52
    [JsonPropertyName("Back Material Type")]
    public string backmaterialtype { get; set; }

    // Occurs 52
    [JsonPropertyName("Runtime,Run time")]
    public string runtime { get; set; }

    // Occurs 52
    [JsonPropertyName("Auto Part Position")]
    public string autopartposition { get; set; }

    // Occurs 52
    [JsonPropertyName("Safety warning")]
    public string safetywarning { get; set; }

    // Occurs 51
    [JsonPropertyName("Shade Material")]
    public string shadematerial { get; set; }

    // Occurs 50
    [JsonPropertyName("Battery Capacity")]
    public string batterycapacity { get; set; }

    // Occurs 50
    [JsonPropertyName("Memory Storage Capacity")]
    public string memorystoragecapacity { get; set; }

    // Occurs 49
    [JsonPropertyName("Sensor Type")]
    public string sensortype { get; set; }

    // Occurs 49
    [JsonPropertyName("Operating System")]
    public string operatingsystem { get; set; }

    // Occurs 49
    [JsonPropertyName("Item Hardness")]
    public string itemhardness { get; set; }

    // Occurs 48
    [JsonPropertyName("Plant or Animal Product Type")]
    public string plantoranimalproducttype { get; set; }

    // Occurs 47
    [JsonPropertyName("Stone Clarity")]
    public string stoneclarity { get; set; }

    // Occurs 46
    [JsonPropertyName("Stone treatment method")]
    public string stonetreatmentmethod { get; set; }

    // Occurs 46
    [JsonPropertyName("Wheel Material")]
    public string wheelmaterial { get; set; }

    // Occurs 46
    [JsonPropertyName("Watch Movement")]
    public string watchmovement { get; set; }

    // Occurs 46
    [JsonPropertyName("Charging Time")]
    public string chargingtime { get; set; }

    // Occurs 45
    [JsonPropertyName("Item Styling")]
    public string itemstyling { get; set; }

    // Occurs 44
    [JsonPropertyName("Opacity")]
    public string opacity { get; set; }

    // Occurs 44
    [JsonPropertyName("Liquid Volume")]
    public string liquidvolume { get; set; }

    // Occurs 44
    [JsonPropertyName("Band Length")]
    public string bandlength { get; set; }

    // Occurs 43
    [JsonPropertyName("Material Type(s)")]
    public string materialtypes { get; set; }

    // Occurs 42
    [JsonPropertyName("Accessory Connection Type")]
    public string accessoryconnectiontype { get; set; }

    // Occurs 41
    [JsonPropertyName("Access Location")]
    public string accesslocation { get; set; }

    // Occurs 41
    [JsonPropertyName("Bike Type")]
    public string biketype { get; set; }

    // Occurs 40
    [JsonPropertyName("Chain Length")]
    public string chainlength { get; set; }

    // Occurs 39
    [JsonPropertyName("Temperature Rating")]
    public string temperaturerating { get; set; }

    // Occurs 39
    [JsonPropertyName("Display technology,Display Technology")]
    public string displaytechnology { get; set; }

    // Occurs 39
    [JsonPropertyName("Number of settings")]
    public string numberofsettings { get; set; }

    // Occurs 39
    [JsonPropertyName("Material Type Free")]
    public string materialtypefree { get; set; }

    // Occurs 38
    [JsonPropertyName("Wall Art Form")]
    public string wallartform { get; set; }

    // Occurs 37
    [JsonPropertyName("Stone Width")]
    public string stonewidth { get; set; }

    // Occurs 37
    [JsonPropertyName("Number of Light Sources")]
    public string numberoflightsources { get; set; }

    // Occurs 36
    [JsonPropertyName("Is Microwaveable")]
    public string ismicrowaveable { get; set; }

    // Occurs 36
    [JsonPropertyName("Map Type")]
    public string maptype { get; set; }

    // Occurs 36
    [JsonPropertyName("Case Material")]
    public string casematerial { get; set; }

    // Occurs 36
    [JsonPropertyName("Caster/Glide/Wheel Type")]
    public string casterglidewheeltype { get; set; }

    // Occurs 36
    [JsonPropertyName("Insulation Material")]
    public string insulationmaterial { get; set; }

    // Occurs 35
    [JsonPropertyName("Skin Tone")]
    public string skintone { get; set; }

    // Occurs 35
    [JsonPropertyName("Magnification Strength")]
    public string magnificationstrength { get; set; }

    // Occurs 35
    [JsonPropertyName("Lens Type")]
    public string lenstype { get; set; }

    // Occurs 34
    [JsonPropertyName("Weave Type")]
    public string weavetype { get; set; }

    // Occurs 34
    [JsonPropertyName("Wireless Communication Technology")]
    public string wirelesscommunicationtechnology { get; set; }

    // Occurs 34
    [JsonPropertyName("With Lid")]
    public string withlid { get; set; }

    // Occurs 33
    [JsonPropertyName("Occupancy")]
    public string occupancy { get; set; }

    // Occurs 32
    [JsonPropertyName("Cable Type")]
    public string cabletype { get; set; }

    // Occurs 32
    [JsonPropertyName("Sub Brand")]
    public string subbrand { get; set; }

    // Occurs 32
    [JsonPropertyName("Fishing Technique")]
    public string fishingtechnique { get; set; }

    // Occurs 31
    [JsonPropertyName("Shoe Width")]
    public string shoewidth { get; set; }

    // Occurs 30
    [JsonPropertyName("Studio")]
    public string studio { get; set; }

    // Occurs 30
    [JsonPropertyName("Reading age")]
    public string readingage { get; set; }

    // Occurs 29
    [JsonPropertyName("Locking")]
    public string locking { get; set; }

    // Occurs 29
    [JsonPropertyName("Stone Length")]
    public string stonelength { get; set; }

    // Occurs 29
    [JsonPropertyName("Paint Type")]
    public string painttype { get; set; }

    // Occurs 29
    [JsonPropertyName("Item Display Dimensions")]
    public string itemdisplaydimensions { get; set; }

    // Occurs 29
    [JsonPropertyName("RAM")]
    public string ram { get; set; }

    // Occurs 29
    [JsonPropertyName("Installation Type Self-Adhesive")]
    public string installationtypeselfadhesive { get; set; }

    // Occurs 29
    [JsonPropertyName("Item Thickness")]
    public string itemthickness { get; set; }

    // Occurs 28
    [JsonPropertyName("Operating Time")]
    public string operatingtime { get; set; }

    // Occurs 28
    [JsonPropertyName("Specialty")]
    public string specialty { get; set; }

    // Occurs 28
    [JsonPropertyName("Pile Height")]
    public string pileheight { get; set; }

    // Occurs 28
    [JsonPropertyName("Model number,Model Number")]
    public string modelnumber { get; set; }

    // Occurs 28
    [JsonPropertyName("Construction Type")]
    public string constructiontype { get; set; }

    // Occurs 27
    [JsonPropertyName("Point Type")]
    public string pointtype { get; set; }

    // Occurs 27
    [JsonPropertyName("Ruling Type")]
    public string rulingtype { get; set; }

    // Occurs 27
    [JsonPropertyName("Shaft Height")]
    public string shaftheight { get; set; }

    // Occurs 27
    [JsonPropertyName("Stone Shape")]
    public string stoneshape { get; set; }

    // Occurs 27
    [JsonPropertyName("Hardware Interface")]
    public string hardwareinterface { get; set; }

    // Occurs 27
    [JsonPropertyName("Expansion")]
    public string expansion { get; set; }

    // Occurs 26
    [JsonPropertyName("GPS")]
    public string gps { get; set; }

    // Occurs 26
    [JsonPropertyName("Base Material")]
    public string basematerial { get; set; }

    // Occurs 26
    [JsonPropertyName("Blade Edge")]
    public string bladeedge { get; set; }

    // Occurs 26
    [JsonPropertyName("Light Color")]
    public string lightcolor { get; set; }

    // Occurs 25
    [JsonPropertyName("Shape Irregular")]
    public string shapeirregular { get; set; }

    // Occurs 25
    [JsonPropertyName("Contributor")]
    public string contributor { get; set; }

    // Occurs 25
    [JsonPropertyName("Light fixture form")]
    public string lightfixtureform { get; set; }

    // Occurs 24
    [JsonPropertyName("Chest Size")]
    public string chestsize { get; set; }

    // Occurs 24
    [JsonPropertyName("Mounting Type Tabletop Mount")]
    public string mountingtypetabletopmount { get; set; }

    // Occurs 24
    [JsonPropertyName("Head Type")]
    public string headtype { get; set; }

    // Occurs 24
    [JsonPropertyName("Media Format")]
    public string mediaformat { get; set; }

    // Occurs 24
    [JsonPropertyName("Specification met,Specification Met")]
    public string specificationmet { get; set; }

    // Occurs 23
    [JsonPropertyName("Collection")]
    public string collection { get; set; }

    // Occurs 22
    [JsonPropertyName("Plug Format")]
    public string plugformat { get; set; }

    // Occurs 22
    [JsonPropertyName("Is Electric")]
    public string iselectric { get; set; }

    // Occurs 22
    [JsonPropertyName("Duration")]
    public string duration { get; set; }

    // Occurs 21
    [JsonPropertyName("Dial Color")]
    public string dialcolor { get; set; }

    // Occurs 21
    [JsonPropertyName("Total USB Ports")]
    public string totalusbports { get; set; }

    // Occurs 21
    [JsonPropertyName("Extension Length")]
    public string extensionlength { get; set; }

    // Occurs 21
    [JsonPropertyName("Golf Club Loft")]
    public string golfclubloft { get; set; }

    // Occurs 21
    [JsonPropertyName("Genre")]
    public string genre { get; set; }

    // Occurs 21
    [JsonPropertyName("Golf Club Flex")]
    public string golfclubflex { get; set; }

    // Occurs 21
    [JsonPropertyName("Diameter")]
    public string diameter { get; set; }

    // Occurs 20
    [JsonPropertyName("Hardware Platform")]
    public string hardwareplatform { get; set; }

    // Occurs 20
    [JsonPropertyName("Contains Liquid Contents")]
    public string containsliquidcontents { get; set; }

    // Occurs 20
    [JsonPropertyName("Ring Size")]
    public string ringsize { get; set; }

    // Occurs 20
    [JsonPropertyName("Handlebar height range")]
    public string handlebarheightrange { get; set; }

    // Occurs 20
    [JsonPropertyName("Actors")]
    public string actors { get; set; }

    // Occurs 20
    [JsonPropertyName("Model Info")]
    public string modelinfo { get; set; }

    // Occurs 20
    [JsonPropertyName("ASTM Fluid Rating")]
    public string astmfluidrating { get; set; }

    // Occurs 20
    [JsonPropertyName("Switch Installation Type")]
    public string switchinstallationtype { get; set; }

    // Occurs 20
    [JsonPropertyName("Hardcover")]
    public string hardcover { get; set; }

    // Occurs 19
    [JsonPropertyName("Switch Type")]
    public string switchtype { get; set; }

    // Occurs 19
    [JsonPropertyName("Belt Style")]
    public string beltstyle { get; set; }

    // Occurs 19
    [JsonPropertyName("Number of Processors")]
    public string numberofprocessors { get; set; }

    // Occurs 19
    [JsonPropertyName("Bristle Type")]
    public string bristletype { get; set; }

    // Occurs 19
    [JsonPropertyName("Door Style")]
    public string doorstyle { get; set; }

    // Occurs 19
    [JsonPropertyName("Is framed?")]
    public string isframed { get; set; }

    // Occurs 19
    [JsonPropertyName("Mounting Type Hanging Mount")]
    public string mountingtypehangingmount { get; set; }

    // Occurs 19
    [JsonPropertyName("Manufacturer Minimum Age (MONTHS)")]
    public string manufacturerminimumagemonths { get; set; }

    // Occurs 19
    [JsonPropertyName("Wheel Width")]
    public string wheelwidth { get; set; }

    // Occurs 19
    [JsonPropertyName("Resolution")]
    public string resolution { get; set; }

    // Occurs 19
    [JsonPropertyName("Input Voltage")]
    public string inputvoltage { get; set; }

    // Occurs 19
    [JsonPropertyName("Position")]
    public string position { get; set; }

    // Occurs 18
    [JsonPropertyName("Chamber Height")]
    public string chamberheight { get; set; }

    // Occurs 18
    [JsonPropertyName("Number of Strings")]
    public string numberofstrings { get; set; }

    // Occurs 18
    [JsonPropertyName("Item Diameter")]
    public string itemdiameter { get; set; }

    // Occurs 18
    [JsonPropertyName("Clarity")]
    public string clarity { get; set; }

    // Occurs 18
    [JsonPropertyName("Curtain Form")]
    public string curtainform { get; set; }

    // Occurs 18
    [JsonPropertyName("Pin type")]
    public string pintype { get; set; }

    // Occurs 18
    [JsonPropertyName("Suspension Type")]
    public string suspensiontype { get; set; }

    // Occurs 18
    [JsonPropertyName("Material Plastic")]
    public string materialplastic { get; set; }

    // Occurs 18
    [JsonPropertyName("Publication date,Publication Date")]
    public string publicationdate { get; set; }

    // Occurs 17
    [JsonPropertyName("CPSIA Cautionary Statement")]
    public string cpsiacautionarystatement { get; set; }

    // Occurs 17
    [JsonPropertyName("Instrument Key")]
    public string instrumentkey { get; set; }

    // Occurs 17
    [JsonPropertyName("Processor Brand")]
    public string processorbrand { get; set; }

    // Occurs 16
    [JsonPropertyName("Sticky notes")]
    public string stickynotes { get; set; }

    // Occurs 16
    [JsonPropertyName("Grade")]
    public string grade { get; set; }

    // Occurs 16
    [JsonPropertyName("Number of Doors")]
    public string numberofdoors { get; set; }

    // Occurs 16
    [JsonPropertyName("Shade Color")]
    public string shadecolor { get; set; }

    // Occurs 16
    [JsonPropertyName("Word Wise")]
    public string wordwise { get; set; }

    // Occurs 16
    [JsonPropertyName("Director")]
    public string director { get; set; }

    // Occurs 16
    [JsonPropertyName("Material Stainless Steel")]
    public string materialstainlesssteel { get; set; }

    // Occurs 16
    [JsonPropertyName("Ruling")]
    public string ruling { get; set; }

    // Occurs 16
    [JsonPropertyName("Lamp Type")]
    public string lamptype { get; set; }

    // Occurs 16
    [JsonPropertyName("Print length")]
    public string printlength { get; set; }

    // Occurs 16
    [JsonPropertyName("Material Ceramic")]
    public string materialceramic { get; set; }

    // Occurs 16
    [JsonPropertyName("File size")]
    public string filesize { get; set; }

    // Occurs 16
    [JsonPropertyName("Text to Speech")]
    public string texttospeech { get; set; }

    // Occurs 16
    [JsonPropertyName("Screen Reader")]
    public string screenreader { get; set; }

    // Occurs 16
    [JsonPropertyName("Enhanced typesetting")]
    public string enhancedtypesetting { get; set; }

    // Occurs 16
    [JsonPropertyName("X Ray")]
    public string xray { get; set; }

    // Occurs 16
    [JsonPropertyName("Card Description")]
    public string carddescription { get; set; }

    // Occurs 16
    [JsonPropertyName("Noise Level")]
    public string noiselevel { get; set; }

    // Occurs 15
    [JsonPropertyName("MPAA rating")]
    public string mpaarating { get; set; }

    // Occurs 15
    [JsonPropertyName("Shelf Type")]
    public string shelftype { get; set; }

    // Occurs 15
    [JsonPropertyName("Puzzle type")]
    public string puzzletype { get; set; }

    // Occurs 15
    [JsonPropertyName("Body Material")]
    public string bodymaterial { get; set; }

    // Occurs 15
    [JsonPropertyName("Thread Count")]
    public string threadcount { get; set; }

    // Occurs 15
    [JsonPropertyName("Number of Dividers")]
    public string numberofdividers { get; set; }

    // Occurs 15
    [JsonPropertyName("Minimum height recommendation")]
    public string minimumheightrecommendation { get; set; }

    // Occurs 15
    [JsonPropertyName("Chamber Width")]
    public string chamberwidth { get; set; }

    // Occurs 14
    [JsonPropertyName("Pillow Type")]
    public string pillowtype { get; set; }

    // Occurs 14
    [JsonPropertyName("Chamber Depth")]
    public string chamberdepth { get; set; }

    // Occurs 14
    [JsonPropertyName("Total Recycled Content Percentage")]
    public string totalrecycledcontentpercentage { get; set; }

    // Occurs 14
    [JsonPropertyName("Dosage Form")]
    public string dosageform { get; set; }

    // Occurs 14
    [JsonPropertyName("Shape Rectangular")]
    public string shaperectangular { get; set; }

    // Occurs 14
    [JsonPropertyName("Alarm")]
    public string alarm { get; set; }

    // Occurs 14
    [JsonPropertyName("Headphones Jack")]
    public string headphonesjack { get; set; }

    // Occurs 14
    [JsonPropertyName("Computer Memory Type")]
    public string computermemorytype { get; set; }

    // Occurs 14
    [JsonPropertyName("Ultraviolet Light Protection")]
    public string ultravioletlightprotection { get; set; }

    // Occurs 13
    [JsonPropertyName("Material Vinyl")]
    public string materialvinyl { get; set; }

    // Occurs 13
    [JsonPropertyName("Colour")]
    public string colour { get; set; }

    // Occurs 13
    [JsonPropertyName("Max Input Sheet Capacity")]
    public string maxinputsheetcapacity { get; set; }

    // Occurs 13
    [JsonPropertyName("Blade Shape")]
    public string bladeshape { get; set; }

    // Occurs 13
    [JsonPropertyName("Maximum Pressure")]
    public string maximumpressure { get; set; }

    // Occurs 13
    [JsonPropertyName("Data Transfer Rate")]
    public string datatransferrate { get; set; }

    // Occurs 13
    [JsonPropertyName("Lift Type")]
    public string lifttype { get; set; }

    // Occurs 13
    [JsonPropertyName("Display size,Display Size")]
    public string displaysize { get; set; }

    // Occurs 13
    [JsonPropertyName("Main Power Connector Type")]
    public string mainpowerconnectortype { get; set; }

    // Occurs 13
    [JsonPropertyName("Pole Material Type")]
    public string polematerialtype { get; set; }

    // Occurs 13
    [JsonPropertyName("Brightness")]
    public string brightness { get; set; }

    // Occurs 13
    [JsonPropertyName("Assembly time")]
    public string assemblytime { get; set; }

    // Occurs 13
    [JsonPropertyName("Material Wood")]
    public string materialwood { get; set; }

    // Occurs 13
    [JsonPropertyName("Audio Jack")]
    public string audiojack { get; set; }

    // Occurs 13
    [JsonPropertyName("Pencil Lead Degree (Hardness)")]
    public string pencilleaddegreehardness { get; set; }

    // Occurs 13
    [JsonPropertyName("Night vision")]
    public string nightvision { get; set; }

    // Occurs 12
    [JsonPropertyName("Packaging")]
    public string packaging { get; set; }

    // Occurs 12
    [JsonPropertyName("Material Polyester")]
    public string materialpolyester { get; set; }

    // Occurs 12
    [JsonPropertyName("Connectivity Protocol")]
    public string connectivityprotocol { get; set; }

    // Occurs 12
    [JsonPropertyName("Luminous Flux")]
    public string luminousflux { get; set; }

    // Occurs 12
    [JsonPropertyName("Mounting Type Freestanding,Mounting Type free_standing,Mounting Type Free Standing,Mounting Type Free_standing")]
    public string mountingtypefreestanding { get; set; }

    // Occurs 12
    [JsonPropertyName("Grade level")]
    public string gradelevel { get; set; }

    // Occurs 12
    [JsonPropertyName("Ram Memory Installed Size")]
    public string rammemoryinstalledsize { get; set; }

    // Occurs 12
    [JsonPropertyName("Control Type")]
    public string controltype { get; set; }

    // Occurs 12
    [JsonPropertyName("Seat Material Type")]
    public string seatmaterialtype { get; set; }

    // Occurs 12
    [JsonPropertyName("Hard Drive")]
    public string harddrive { get; set; }

    // Occurs 12
    [JsonPropertyName("Handle Diameter")]
    public string handlediameter { get; set; }

    // Occurs 12
    [JsonPropertyName("Material Metal")]
    public string materialmetal { get; set; }

    // Occurs 12
    [JsonPropertyName("Number of Jets")]
    public string numberofjets { get; set; }

    // Occurs 12
    [JsonPropertyName("Scanner Resolution")]
    public string scannerresolution { get; set; }

    // Occurs 11
    [JsonPropertyName("Fishing Line Type")]
    public string fishinglinetype { get; set; }

    // Occurs 11
    [JsonPropertyName("Coating")]
    public string coating { get; set; }

    // Occurs 11
    [JsonPropertyName("Color Code")]
    public string colorcode { get; set; }

    // Occurs 11
    [JsonPropertyName("Pad Type")]
    public string padtype { get; set; }

    // Occurs 11
    [JsonPropertyName("Rug Form Type")]
    public string rugformtype { get; set; }

    // Occurs 11
    [JsonPropertyName("Line Size")]
    public string linesize { get; set; }

    // Occurs 11
    [JsonPropertyName("Design")]
    public string design { get; set; }

    // Occurs 11
    [JsonPropertyName("Guitar Pick Thickness")]
    public string guitarpickthickness { get; set; }

    // Occurs 11
    [JsonPropertyName("International Protection Rating")]
    public string internationalprotectionrating { get; set; }

    // Occurs 10
    [JsonPropertyName("Tensile Strength")]
    public string tensilestrength { get; set; }

    // Occurs 10
    [JsonPropertyName("Top Material Type")]
    public string topmaterialtype { get; set; }

    // Occurs 10
    [JsonPropertyName("Surface Recommendation Wall")]
    public string surfacerecommendationwall { get; set; }

    // Occurs 10
    [JsonPropertyName("Bowl Material")]
    public string bowlmaterial { get; set; }

    // Occurs 10
    [JsonPropertyName("Material Glass")]
    public string materialglass { get; set; }

    // Occurs 10
    [JsonPropertyName("Occupant Capacity")]
    public string occupantcapacity { get; set; }

    // Occurs 10
    [JsonPropertyName("Grit Material")]
    public string gritmaterial { get; set; }

    // Occurs 10
    [JsonPropertyName("Screen Resolution")]
    public string screenresolution { get; set; }

    // Occurs 10
    [JsonPropertyName("Instrument")]
    public string instrument { get; set; }

    // Occurs 10
    [JsonPropertyName("Number of Speeds")]
    public string numberofspeeds { get; set; }

    // Occurs 10
    [JsonPropertyName("Brake Style")]
    public string brakestyle { get; set; }

    // Occurs 10
    [JsonPropertyName("Active Ingredients")]
    public string activeingredients { get; set; }

    // Occurs 9
    [JsonPropertyName("Sterility Rating")]
    public string sterilityrating { get; set; }

    // Occurs 9
    [JsonPropertyName("Safety Rating")]
    public string safetyrating { get; set; }

    // Occurs 9
    [JsonPropertyName("Processor")]
    public string processor { get; set; }

    // Occurs 9
    [JsonPropertyName("Chipset Brand")]
    public string chipsetbrand { get; set; }

    // Occurs 9
    [JsonPropertyName("Light Type")]
    public string lighttype { get; set; }

    // Occurs 9
    [JsonPropertyName("Maximum Weight Capacity")]
    public string maximumweightcapacity { get; set; }

    // Occurs 9
    [JsonPropertyName("Sound Level")]
    public string soundlevel { get; set; }

    // Occurs 9
    [JsonPropertyName("Light Direction")]
    public string lightdirection { get; set; }

    // Occurs 9
    [JsonPropertyName("Seat height,Seat Height")]
    public string seatheight { get; set; }

    // Occurs 9
    [JsonPropertyName("Battery Power Rating")]
    public string batterypowerrating { get; set; }

    // Occurs 9
    [JsonPropertyName("Head Material")]
    public string headmaterial { get; set; }

    // Occurs 9
    [JsonPropertyName("Fuel Type,Fuel type")]
    public string fueltype { get; set; }

    // Occurs 8
    [JsonPropertyName("Description Pile")]
    public string descriptionpile { get; set; }

    // Occurs 8
    [JsonPropertyName("Number of Fasteners")]
    public string numberoffasteners { get; set; }

    // Occurs 8
    [JsonPropertyName("Compatibility Options")]
    public string compatibilityoptions { get; set; }

    // Occurs 8
    [JsonPropertyName("Hard Drive Interface")]
    public string harddriveinterface { get; set; }

    // Occurs 8
    [JsonPropertyName("Surface Recommendation Window")]
    public string surfacerecommendationwindow { get; set; }

    // Occurs 8
    [JsonPropertyName("Is Cordless?")]
    public string iscordless { get; set; }

    // Occurs 8
    [JsonPropertyName("Grit Type")]
    public string grittype { get; set; }

    // Occurs 8
    [JsonPropertyName("Speaker Type")]
    public string speakertype { get; set; }

    // Occurs 8
    [JsonPropertyName("Body Shape")]
    public string bodyshape { get; set; }

    // Occurs 8
    [JsonPropertyName("Electric fan design")]
    public string electricfandesign { get; set; }

    // Occurs 8
    [JsonPropertyName("Thread Coverage")]
    public string threadcoverage { get; set; }

    // Occurs 8
    [JsonPropertyName("Rated")]
    public string rated { get; set; }

    // Occurs 8
    [JsonPropertyName("Video Capture Resolution")]
    public string videocaptureresolution { get; set; }

    // Occurs 8
    [JsonPropertyName("Brightness Rating")]
    public string brightnessrating { get; set; }

    // Occurs 8
    [JsonPropertyName("Hard Disk Size")]
    public string harddisksize { get; set; }

    // Occurs 8
    [JsonPropertyName("Cap Type")]
    public string captype { get; set; }

    // Occurs 8
    [JsonPropertyName("Platform")]
    public string platform { get; set; }

    // Occurs 8
    [JsonPropertyName("Camera Lens Description")]
    public string cameralensdescription { get; set; }

    // Occurs 8
    [JsonPropertyName("Material cotton,Material Cotton")]
    public string materialcotton { get; set; }

    // Occurs 8
    [JsonPropertyName("Diet Type")]
    public string diettype { get; set; }

    // Occurs 8
    [JsonPropertyName("Tray Type")]
    public string traytype { get; set; }

    // Occurs 7
    [JsonPropertyName("Maximum Flow Rate")]
    public string maximumflowrate { get; set; }

    // Occurs 7
    [JsonPropertyName("Product Name")]
    public string productname { get; set; }

    // Occurs 7
    [JsonPropertyName("Input Device")]
    public string inputdevice { get; set; }

    // Occurs 7
    [JsonPropertyName("Tab Cut")]
    public string tabcut { get; set; }

    // Occurs 7
    [JsonPropertyName("Binding Edge")]
    public string bindingedge { get; set; }

    // Occurs 7
    [JsonPropertyName("Amperage")]
    public string amperage { get; set; }

    // Occurs 7
    [JsonPropertyName("Valve Type")]
    public string valvetype { get; set; }

    // Occurs 7
    [JsonPropertyName("Grit Number")]
    public string gritnumber { get; set; }

    // Occurs 7
    [JsonPropertyName("Built-in Light")]
    public string builtinlight { get; set; }

    // Occurs 7
    [JsonPropertyName("Wick Quantity")]
    public string wickquantity { get; set; }

    // Occurs 7
    [JsonPropertyName("Average Battery Life (in hours)")]
    public string averagebatterylifeinhours { get; set; }

    // Occurs 7
    [JsonPropertyName("Graphics Coprocessor")]
    public string graphicscoprocessor { get; set; }

    // Occurs 7
    [JsonPropertyName("Number of USB 2.0 Ports")]
    public string numberofusb20ports { get; set; }

    // Occurs 7
    [JsonPropertyName("Mounting Type 独立式")]
    public string mountingtype独立式 { get; set; }

    // Occurs 7
    [JsonPropertyName("Durometer Hardness")]
    public string durometerhardness { get; set; }

    // Occurs 7
    [JsonPropertyName("Material Leather")]
    public string materialleather { get; set; }

    // Occurs 7
    [JsonPropertyName("Max Screen Resolution")]
    public string maxscreenresolution { get; set; }

    // Occurs 7
    [JsonPropertyName("Rail type")]
    public string railtype { get; set; }

    // Occurs 7
    [JsonPropertyName("Subtitles")]
    public string subtitles { get; set; }

    // Occurs 7
    [JsonPropertyName("Color Temperature")]
    public string colortemperature { get; set; }

    // Occurs 7
    [JsonPropertyName("Bulb Base")]
    public string bulbbase { get; set; }

    // Occurs 7
    [JsonPropertyName("Line Weight")]
    public string lineweight { get; set; }

    // Occurs 7
    [JsonPropertyName("Type")]
    public string type { get; set; }

    // Occurs 7
    [JsonPropertyName("Grit Description")]
    public string gritdescription { get; set; }

    // Occurs 6
    [JsonPropertyName("Number of Blades")]
    public string numberofblades { get; set; }

    // Occurs 6
    [JsonPropertyName("Table design")]
    public string tabledesign { get; set; }

    // Occurs 6
    [JsonPropertyName("Container Material")]
    public string containermaterial { get; set; }

    // Occurs 6
    [JsonPropertyName("Floor Area")]
    public string floorarea { get; set; }

    // Occurs 6
    [JsonPropertyName("Compatible Lubricant")]
    public string compatiblelubricant { get; set; }

    // Occurs 6
    [JsonPropertyName("Tab Position")]
    public string tabposition { get; set; }

    // Occurs 6
    [JsonPropertyName("Finderscope")]
    public string finderscope { get; set; }

    // Occurs 6
    [JsonPropertyName("Graphics Card Description")]
    public string graphicscarddescription { get; set; }

    // Occurs 6
    [JsonPropertyName("Bulb Shape Size")]
    public string bulbshapesize { get; set; }

    // Occurs 6
    [JsonPropertyName("Microphone Form Factor")]
    public string microphoneformfactor { get; set; }

    // Occurs 6
    [JsonPropertyName("Fragrance Concentration")]
    public string fragranceconcentration { get; set; }

    // Occurs 6
    [JsonPropertyName("Lexile measure")]
    public string lexilemeasure { get; set; }

    // Occurs 6
    [JsonPropertyName("Voice command")]
    public string voicecommand { get; set; }

    // Occurs 6
    [JsonPropertyName("Image Stabilization")]
    public string imagestabilization { get; set; }

    // Occurs 6
    [JsonPropertyName("Switch Style")]
    public string switchstyle { get; set; }

    // Occurs 6
    [JsonPropertyName("Harness type,Harness Type")]
    public string harnesstype { get; set; }

    // Occurs 6
    [JsonPropertyName("Drive System")]
    public string drivesystem { get; set; }

    // Occurs 6
    [JsonPropertyName("Thread Type")]
    public string threadtype { get; set; }

    // Occurs 6
    [JsonPropertyName("Sleeve cuff style")]
    public string sleevecuffstyle { get; set; }

    // Occurs 6
    [JsonPropertyName("Hard Drive Rotational Speed")]
    public string harddriverotationalspeed { get; set; }

    // Occurs 6
    [JsonPropertyName("Maximum Height")]
    public string maximumheight { get; set; }

    // Occurs 6
    [JsonPropertyName("Assembled Depth")]
    public string assembleddepth { get; set; }

    // Occurs 6
    [JsonPropertyName("Number of Ports")]
    public string numberofports { get; set; }

    // Occurs 6
    [JsonPropertyName("Rounds")]
    public string rounds { get; set; }

    // Occurs 6
    [JsonPropertyName("Networking Feature")]
    public string networkingfeature { get; set; }

    // Occurs 6
    [JsonPropertyName("Lens Correction Type")]
    public string lenscorrectiontype { get; set; }

    // Occurs 6
    [JsonPropertyName("Rim Size")]
    public string rimsize { get; set; }

    // Occurs 6
    [JsonPropertyName("Deck Length")]
    public string decklength { get; set; }

    // Occurs 6
    [JsonPropertyName("Mounting Type Wall Mount")]
    public string mountingtypewallmount { get; set; }

    // Occurs 6
    [JsonPropertyName("Wireless Type")]
    public string wirelesstype { get; set; }

    // Occurs 6
    [JsonPropertyName("Specific Uses")]
    public string specificuses { get; set; }

    // Occurs 5
    [JsonPropertyName("Number of Lights")]
    public string numberoflights { get; set; }

    // Occurs 5
    [JsonPropertyName("Blade Color")]
    public string bladecolor { get; set; }

    // Occurs 5
    [JsonPropertyName("String Material Type")]
    public string stringmaterialtype { get; set; }

    // Occurs 5
    [JsonPropertyName("Size (per pearl)")]
    public string sizeperpearl { get; set; }

    // Occurs 5
    [JsonPropertyName("Denomination Unit")]
    public string denominationunit { get; set; }

    // Occurs 5
    [JsonPropertyName("Bottle Type")]
    public string bottletype { get; set; }

    // Occurs 5
    [JsonPropertyName("Product Grade")]
    public string productgrade { get; set; }

    // Occurs 5
    [JsonPropertyName("Number of Doors 2")]
    public string numberofdoors2 { get; set; }

    // Occurs 5
    [JsonPropertyName("Wireless Remote")]
    public string wirelessremote { get; set; }

    // Occurs 5
    [JsonPropertyName("Phone Talk Time")]
    public string phonetalktime { get; set; }

    // Occurs 5
    [JsonPropertyName("Lower Temperature Rating")]
    public string lowertemperaturerating { get; set; }

    // Occurs 5
    [JsonPropertyName("Seating Capacity")]
    public string seatingcapacity { get; set; }

    // Occurs 5
    [JsonPropertyName("Shape Round")]
    public string shaperound { get; set; }

    // Occurs 5
    [JsonPropertyName("Door Hinges")]
    public string doorhinges { get; set; }

    // Occurs 5
    [JsonPropertyName("CPU Model")]
    public string cpumodel { get; set; }

    // Occurs 5
    [JsonPropertyName("Battery Type,Battery type")]
    public string batterytype { get; set; }

    // Occurs 5
    [JsonPropertyName("Occasion Birthday")]
    public string occasionbirthday { get; set; }

    // Occurs 5
    [JsonPropertyName("Tree Type")]
    public string treetype { get; set; }

    // Occurs 5
    [JsonPropertyName("Average Battery Life")]
    public string averagebatterylife { get; set; }

    // Occurs 5
    [JsonPropertyName("Deck Width")]
    public string deckwidth { get; set; }

    // Occurs 5
    [JsonPropertyName("Formulation Type")]
    public string formulationtype { get; set; }

    // Occurs 5
    [JsonPropertyName("Number of Compartments 11")]
    public string numberofcompartments11 { get; set; }

    // Occurs 5
    [JsonPropertyName("Lighting Method")]
    public string lightingmethod { get; set; }

    // Occurs 5
    [JsonPropertyName("Material Paper")]
    public string materialpaper { get; set; }

    // Occurs 5
    [JsonPropertyName("Flash Memory Type")]
    public string flashmemorytype { get; set; }

    // Occurs 5
    [JsonPropertyName("Aspect Ratio")]
    public string aspectratio { get; set; }

    // Occurs 5
    [JsonPropertyName("Distinctive feature")]
    public string distinctivefeature { get; set; }

    // Occurs 5
    [JsonPropertyName("Post-Consumer Recycled Content Percentage")]
    public string postconsumerrecycledcontentpercentage { get; set; }

    // Occurs 5
    [JsonPropertyName("SPARS Code")]
    public string sparscode { get; set; }

    // Occurs 5
    [JsonPropertyName("Graded By")]
    public string gradedby { get; set; }

    // Occurs 5
    [JsonPropertyName("Supported Application")]
    public string supportedapplication { get; set; }

    // Occurs 5
    [JsonPropertyName("Expected Blooming Period")]
    public string expectedbloomingperiod { get; set; }

    // Occurs 5
    [JsonPropertyName("Hard Disk Description")]
    public string harddiskdescription { get; set; }

    // Occurs 5
    [JsonPropertyName("Focus Type")]
    public string focustype { get; set; }

    // Occurs 5
    [JsonPropertyName("Whiteness")]
    public string whiteness { get; set; }

    // Occurs 5
    [JsonPropertyName("Hard Disk Form Factor")]
    public string harddiskformfactor { get; set; }

    // Occurs 5
    [JsonPropertyName("Battery Weight")]
    public string batteryweight { get; set; }

    // Occurs 5
    [JsonPropertyName("Pump type")]
    public string pumptype { get; set; }

    // Occurs 5
    [JsonPropertyName("Number of Resistance Levels")]
    public string numberofresistancelevels { get; set; }

    // Occurs 5
    [JsonPropertyName("Minimum Weight Capacity")]
    public string minimumweightcapacity { get; set; }

    // Occurs 5
    [JsonPropertyName("Number of Compartments 1")]
    public string numberofcompartments1 { get; set; }

    // Occurs 5
    [JsonPropertyName("Moisture Needs")]
    public string moistureneeds { get; set; }

    // Occurs 5
    [JsonPropertyName("Digital Storage Capacity")]
    public string digitalstoragecapacity { get; set; }

    // Occurs 4
    [JsonPropertyName("Objective Lens Diameter")]
    public string objectivelensdiameter { get; set; }

    // Occurs 4
    [JsonPropertyName("Tank Volume")]
    public string tankvolume { get; set; }

    // Occurs 4
    [JsonPropertyName("Tip count")]
    public string tipcount { get; set; }

    // Occurs 4
    [JsonPropertyName("Speed")]
    public string speed { get; set; }

    // Occurs 4
    [JsonPropertyName("Cable Feature")]
    public string cablefeature { get; set; }

    // Occurs 4
    [JsonPropertyName("Thread Size")]
    public string threadsize { get; set; }

    // Occurs 4
    [JsonPropertyName("Shank Type")]
    public string shanktype { get; set; }

    // Occurs 4
    [JsonPropertyName("Storage Volume")]
    public string storagevolume { get; set; }

    // Occurs 4
    [JsonPropertyName("Paper Weight")]
    public string paperweight { get; set; }

    // Occurs 4
    [JsonPropertyName("Maximum Range")]
    public string maximumrange { get; set; }

    // Occurs 4
    [JsonPropertyName("Surface Recommendation Floor")]
    public string surfacerecommendationfloor { get; set; }

    // Occurs 4
    [JsonPropertyName("Bearing Number")]
    public string bearingnumber { get; set; }

    // Occurs 4
    [JsonPropertyName("Hard Disk Interface")]
    public string harddiskinterface { get; set; }

    // Occurs 4
    [JsonPropertyName("Upholstery Fabric Type")]
    public string upholsteryfabrictype { get; set; }

    // Occurs 4
    [JsonPropertyName("Wireless Carrier")]
    public string wirelesscarrier { get; set; }

    // Occurs 4
    [JsonPropertyName("Seafood Production Method")]
    public string seafoodproductionmethod { get; set; }

    // Occurs 4
    [JsonPropertyName("Controls Type")]
    public string controlstype { get; set; }

    // Occurs 4
    [JsonPropertyName("Other camera features")]
    public string othercamerafeatures { get; set; }

    // Occurs 4
    [JsonPropertyName("Antenna")]
    public string antenna { get; set; }

    // Occurs 4
    [JsonPropertyName("Flash Memory Size")]
    public string flashmemorysize { get; set; }

    // Occurs 4
    [JsonPropertyName("Video Capture Format")]
    public string videocaptureformat { get; set; }

    // Occurs 4
    [JsonPropertyName("Base Width")]
    public string basewidth { get; set; }

    // Occurs 4
    [JsonPropertyName("Has Nonstick Coating")]
    public string hasnonstickcoating { get; set; }

    // Occurs 4
    [JsonPropertyName("Total Metal Weight")]
    public string totalmetalweight { get; set; }

    // Occurs 4
    [JsonPropertyName("Number of Programs")]
    public string numberofprograms { get; set; }

    // Occurs 4
    [JsonPropertyName("Cellular Technology")]
    public string cellulartechnology { get; set; }

    // Occurs 4
    [JsonPropertyName("Rechargeable Battery Included")]
    public string rechargeablebatteryincluded { get; set; }

    // Occurs 4
    [JsonPropertyName("Minimum Focal Length")]
    public string minimumfocallength { get; set; }

    // Occurs 4
    [JsonPropertyName("Current Rating")]
    public string currentrating { get; set; }

    // Occurs 4
    [JsonPropertyName("Certificate Type")]
    public string certificatetype { get; set; }

    // Occurs 4
    [JsonPropertyName("Material Silicone")]
    public string materialsilicone { get; set; }

    // Occurs 4
    [JsonPropertyName("Minimum Height")]
    public string minimumheight { get; set; }

    // Occurs 4
    [JsonPropertyName("Water Resistance Technology")]
    public string waterresistancetechnology { get; set; }

    // Occurs 4
    [JsonPropertyName("Pearl Shape")]
    public string pearlshape { get; set; }

    // Occurs 4
    [JsonPropertyName("Tool Tip Description")]
    public string tooltipdescription { get; set; }

    // Occurs 4
    [JsonPropertyName("Rotation Direction")]
    public string rotationdirection { get; set; }

    // Occurs 4
    [JsonPropertyName("Mildew-resistant")]
    public string mildewresistant { get; set; }

    // Occurs 4
    [JsonPropertyName("Number Theme")]
    public string numbertheme { get; set; }

    // Occurs 4
    [JsonPropertyName("Material Rubber")]
    public string materialrubber { get; set; }

    // Occurs 4
    [JsonPropertyName("Bulb Type")]
    public string bulbtype { get; set; }

    // Occurs 4
    [JsonPropertyName("Material Aluminum")]
    public string materialaluminum { get; set; }

    // Occurs 4
    [JsonPropertyName("Tea Variety")]
    public string teavariety { get; set; }

    // Occurs 4
    [JsonPropertyName("Sunlight Exposure")]
    public string sunlightexposure { get; set; }

    // Occurs 4
    [JsonPropertyName("Corner/Edge Style")]
    public string corneredgestyle { get; set; }

    // Occurs 4
    [JsonPropertyName("Compatible Fastener Range")]
    public string compatiblefastenerrange { get; set; }

    // Occurs 4
    [JsonPropertyName("Expected Planting Period")]
    public string expectedplantingperiod { get; set; }

    // Occurs 4
    [JsonPropertyName("Dubbed")]
    public string dubbed { get; set; }

    // Occurs 4
    [JsonPropertyName("Tension Level")]
    public string tensionlevel { get; set; }

    // Occurs 4
    [JsonPropertyName("Mounting Type Freestanding,Insert")]
    public string mountingtypefreestandinginsert { get; set; }

    // Occurs 4
    [JsonPropertyName("Hanging Method")]
    public string hangingmethod { get; set; }

    // Occurs 3
    [JsonPropertyName("Soil Type")]
    public string soiltype { get; set; }

    // Occurs 3
    [JsonPropertyName("Optical Zoom")]
    public string opticalzoom { get; set; }

    // Occurs 3
    [JsonPropertyName("Finish Type Polished")]
    public string finishtypepolished { get; set; }

    // Occurs 3
    [JsonPropertyName("Material Rhinestone")]
    public string materialrhinestone { get; set; }

    // Occurs 3
    [JsonPropertyName("Operating Voltage")]
    public string operatingvoltage { get; set; }

    // Occurs 3
    [JsonPropertyName("Style Antique")]
    public string styleantique { get; set; }

    // Occurs 3
    [JsonPropertyName("Number of Teeth")]
    public string numberofteeth { get; set; }

    // Occurs 3
    [JsonPropertyName("Air Flow Capacity")]
    public string airflowcapacity { get; set; }

    // Occurs 3
    [JsonPropertyName("Backing")]
    public string backing { get; set; }

    // Occurs 3
    [JsonPropertyName("Material Synthetic")]
    public string materialsynthetic { get; set; }

    // Occurs 3
    [JsonPropertyName("Number of Compartments 13")]
    public string numberofcompartments13 { get; set; }

    // Occurs 3
    [JsonPropertyName("Wheel Backspacing")]
    public string wheelbackspacing { get; set; }

    // Occurs 3
    [JsonPropertyName("Barrel Material Type")]
    public string barrelmaterialtype { get; set; }

    // Occurs 3
    [JsonPropertyName("Heating method,Heating Method")]
    public string heatingmethod { get; set; }

    // Occurs 3
    [JsonPropertyName("Number of Buttons")]
    public string numberofbuttons { get; set; }

    // Occurs 3
    [JsonPropertyName("Surface Recommendation Metal")]
    public string surfacerecommendationmetal { get; set; }

    // Occurs 3
    [JsonPropertyName("Auto Focus Technology")]
    public string autofocustechnology { get; set; }

    // Occurs 3
    [JsonPropertyName("Material Horn")]
    public string materialhorn { get; set; }

    // Occurs 3
    [JsonPropertyName("Back Material")]
    public string backmaterial { get; set; }

    // Occurs 3
    [JsonPropertyName("Ferrule Material")]
    public string ferrulematerial { get; set; }

    // Occurs 3
    [JsonPropertyName("Hose Length")]
    public string hoselength { get; set; }

    // Occurs 3
    [JsonPropertyName("Burner type")]
    public string burnertype { get; set; }

    // Occurs 3
    [JsonPropertyName("Strap Length")]
    public string straplength { get; set; }

    // Occurs 3
    [JsonPropertyName("Grit Rating")]
    public string gritrating { get; set; }

    // Occurs 3
    [JsonPropertyName("Video Standard")]
    public string videostandard { get; set; }

    // Occurs 3
    [JsonPropertyName("Eye Piece Lens Description")]
    public string eyepiecelensdescription { get; set; }

    // Occurs 3
    [JsonPropertyName("Number of Compartments 12")]
    public string numberofcompartments12 { get; set; }

    // Occurs 3
    [JsonPropertyName("Servings per Container")]
    public string servingspercontainer { get; set; }

    // Occurs 3
    [JsonPropertyName("Item Depth")]
    public string itemdepth { get; set; }

    // Occurs 3
    [JsonPropertyName("Wireless Communication Standard")]
    public string wirelesscommunicationstandard { get; set; }

    // Occurs 3
    [JsonPropertyName("USDA Hardiness Zone")]
    public string usdahardinesszone { get; set; }

    // Occurs 3
    [JsonPropertyName("Handle Type Pull Handle")]
    public string handletypepullhandle { get; set; }

    // Occurs 3
    [JsonPropertyName("Frequency")]
    public string frequency { get; set; }

    // Occurs 3
    [JsonPropertyName("Floor Length")]
    public string floorlength { get; set; }

    // Occurs 3
    [JsonPropertyName("Lens Design")]
    public string lensdesign { get; set; }

    // Occurs 3
    [JsonPropertyName("Material other,Material Other")]
    public string materialother { get; set; }

    // Occurs 3
    [JsonPropertyName("Inlet Connection Type")]
    public string inletconnectiontype { get; set; }

    // Occurs 3
    [JsonPropertyName("Outlet Connection Type")]
    public string outletconnectiontype { get; set; }

    // Occurs 3
    [JsonPropertyName("Viscosity")]
    public string viscosity { get; set; }

    // Occurs 3
    [JsonPropertyName("Performance Description")]
    public string performancedescription { get; set; }

    // Occurs 3
    [JsonPropertyName("Full Cure Time")]
    public string fullcuretime { get; set; }

    // Occurs 3
    [JsonPropertyName("Pattern match")]
    public string patternmatch { get; set; }

    // Occurs 3
    [JsonPropertyName("Write Speed")]
    public string writespeed { get; set; }

    // Occurs 3
    [JsonPropertyName("Strength")]
    public string strength { get; set; }

    // Occurs 3
    [JsonPropertyName("Setting Type")]
    public string settingtype { get; set; }

    // Occurs 3
    [JsonPropertyName("Fastener Size")]
    public string fastenersize { get; set; }

    // Occurs 3
    [JsonPropertyName("Total Power Outlets")]
    public string totalpoweroutlets { get; set; }

    // Occurs 3
    [JsonPropertyName("Bristle Material")]
    public string bristlematerial { get; set; }

    // Occurs 3
    [JsonPropertyName("CPU Speed")]
    public string cpuspeed { get; set; }

    // Occurs 3
    [JsonPropertyName("TV Size")]
    public string tvsize { get; set; }

    // Occurs 3
    [JsonPropertyName("Media Type")]
    public string mediatype { get; set; }

    // Occurs 3
    [JsonPropertyName("Phone Standby Time (with data)")]
    public string phonestandbytimewithdata { get; set; }

    // Occurs 3
    [JsonPropertyName("Spiral bound")]
    public string spiralbound { get; set; }

    // Occurs 3
    [JsonPropertyName("Bulb Features")]
    public string bulbfeatures { get; set; }

    // Occurs 3
    [JsonPropertyName("Focal Length Description")]
    public string focallengthdescription { get; set; }

    // Occurs 3
    [JsonPropertyName("Number Of Poles")]
    public string numberofpoles { get; set; }

    // Occurs 3
    [JsonPropertyName("Lens Fixed Focal Length")]
    public string lensfixedfocallength { get; set; }

    // Occurs 3
    [JsonPropertyName("Light Path Distance")]
    public string lightpathdistance { get; set; }

    // Occurs 3
    [JsonPropertyName("Max. compatible device size")]
    public string maxcompatibledevicesize { get; set; }

    // Occurs 3
    [JsonPropertyName("Pearl Type")]
    public string pearltype { get; set; }

    // Occurs 3
    [JsonPropertyName("Special Ingredients")]
    public string specialingredients { get; set; }

    // Occurs 3
    [JsonPropertyName("Strand Type")]
    public string strandtype { get; set; }

    // Occurs 3
    [JsonPropertyName("Stone height")]
    public string stoneheight { get; set; }

    // Occurs 3
    [JsonPropertyName("Cooling Method")]
    public string coolingmethod { get; set; }

    // Occurs 3
    [JsonPropertyName("Fastener Capacity")]
    public string fastenercapacity { get; set; }

    // Occurs 3
    [JsonPropertyName("Chocolate Type")]
    public string chocolatetype { get; set; }

    // Occurs 3
    [JsonPropertyName("Number of Drawers 2")]
    public string numberofdrawers2 { get; set; }

    // Occurs 3
    [JsonPropertyName("Material Polycarbonate")]
    public string materialpolycarbonate { get; set; }

    // Occurs 3
    [JsonPropertyName("Material Acrylic")]
    public string materialacrylic { get; set; }

    // Occurs 3
    [JsonPropertyName("Shaft Circumference")]
    public string shaftcircumference { get; set; }

    // Occurs 3
    [JsonPropertyName("Is Waterproof")]
    public string iswaterproof { get; set; }

    // Occurs 3
    [JsonPropertyName("Tool Flute Type")]
    public string toolflutetype { get; set; }

    // Occurs 3
    [JsonPropertyName("Number of USB 3.0 Ports")]
    public string numberofusb30ports { get; set; }

    // Occurs 3
    [JsonPropertyName("Color Screen")]
    public string colorscreen { get; set; }

    // Occurs 3
    [JsonPropertyName("Display Resolution Maximum")]
    public string displayresolutionmaximum { get; set; }

    // Occurs 3
    [JsonPropertyName("Finish Type Painted")]
    public string finishtypepainted { get; set; }

    // Occurs 3
    [JsonPropertyName("Installation Type Peel")]
    public string installationtypepeel { get; set; }

    // Occurs 3
    [JsonPropertyName("Wireless network technology")]
    public string wirelessnetworktechnology { get; set; }

    // Occurs 3
    [JsonPropertyName("Shape Arch & Bow")]
    public string shapearchbow { get; set; }

    // Occurs 3
    [JsonPropertyName("Graphics Card Ram Size")]
    public string graphicscardramsize { get; set; }

    // Occurs 3
    [JsonPropertyName("Musical Style")]
    public string musicalstyle { get; set; }

    // Occurs 3
    [JsonPropertyName("Simultaneous device usage")]
    public string simultaneousdeviceusage { get; set; }

    // Occurs 3
    [JsonPropertyName("Polar Pattern")]
    public string polarpattern { get; set; }

    // Occurs 3
    [JsonPropertyName("Compatible Mountings")]
    public string compatiblemountings { get; set; }

    // Occurs 3
    [JsonPropertyName("Contains")]
    public string contains { get; set; }

    // Occurs 3
    [JsonPropertyName("Power Source Battery Powered")]
    public string powersourcebatterypowered { get; set; }

    // Occurs 2
    [JsonPropertyName("Processor Count")]
    public string processorcount { get; set; }

    // Occurs 2
    [JsonPropertyName("Supported Internet Services")]
    public string supportedinternetservices { get; set; }

    // Occurs 2
    [JsonPropertyName("Included Security Features")]
    public string includedsecurityfeatures { get; set; }

    // Occurs 2
    [JsonPropertyName("Athlete")]
    public string athlete { get; set; }

    // Occurs 2
    [JsonPropertyName("Minimum Age Recomendation")]
    public string minimumagerecomendation { get; set; }

    // Occurs 2
    [JsonPropertyName("Maximum Rotational Speed")]
    public string maximumrotationalspeed { get; set; }

    // Occurs 2
    [JsonPropertyName("Surface Recommendation Vinyl,Surface Recommendation vinyl")]
    public string surfacerecommendationvinyl { get; set; }

    // Occurs 2
    [JsonPropertyName("Pages")]
    public string pages { get; set; }

    // Occurs 2
    [JsonPropertyName("Number of Drawers 6")]
    public string numberofdrawers6 { get; set; }

    // Occurs 2
    [JsonPropertyName("Style Modern")]
    public string stylemodern { get; set; }

    // Occurs 2
    [JsonPropertyName("Shelf Type Hanging Shelf")]
    public string shelftypehangingshelf { get; set; }

    // Occurs 2
    [JsonPropertyName("Surface Recommendation Cloth")]
    public string surfacerecommendationcloth { get; set; }

    // Occurs 2
    [JsonPropertyName("Material Bronze")]
    public string materialbronze { get; set; }

    // Occurs 2
    [JsonPropertyName("Surface Recommendation Nail,Surface Recommendation nail")]
    public string surfacerecommendationnail { get; set; }

    // Occurs 2
    [JsonPropertyName("Lines Per Page")]
    public string linesperpage { get; set; }

    // Occurs 2
    [JsonPropertyName("Signal Format")]
    public string signalformat { get; set; }

    // Occurs 2
    [JsonPropertyName("Inner Material Type")]
    public string innermaterialtype { get; set; }

    // Occurs 2
    [JsonPropertyName("Finish Type Black")]
    public string finishtypeblack { get; set; }

    // Occurs 2
    [JsonPropertyName("Caliber")]
    public string caliber { get; set; }

    // Occurs 2
    [JsonPropertyName("Shelf Type Wood")]
    public string shelftypewood { get; set; }

    // Occurs 2
    [JsonPropertyName("Exterior Finish Plastic,Exterior Finish plastic")]
    public string exteriorfinishplastic { get; set; }

    // Occurs 2
    [JsonPropertyName("Surface Recommendation Adjustable")]
    public string surfacerecommendationadjustable { get; set; }

    // Occurs 2
    [JsonPropertyName("Item Weight 1 Pounds")]
    public string itemweight1pounds { get; set; }

    // Occurs 2
    [JsonPropertyName("Speakers Maximum Output Power")]
    public string speakersmaximumoutputpower { get; set; }

    // Occurs 2
    [JsonPropertyName("Power Source Type")]
    public string powersourcetype { get; set; }

    // Occurs 2
    [JsonPropertyName("Color Rendering Index")]
    public string colorrenderingindex { get; set; }

    // Occurs 2
    [JsonPropertyName("Top Material")]
    public string topmaterial { get; set; }

    // Occurs 2
    [JsonPropertyName("Film Format Type")]
    public string filmformattype { get; set; }

    // Occurs 2
    [JsonPropertyName("Number of Compartments 9")]
    public string numberofcompartments9 { get; set; }

    // Occurs 2
    [JsonPropertyName("Golf Putter Lie Angle")]
    public string golfputterlieangle { get; set; }

    // Occurs 2
    [JsonPropertyName("Fit Type Universal")]
    public string fittypeuniversal { get; set; }

    // Occurs 2
    [JsonPropertyName("Number of Compartments 6")]
    public string numberofcompartments6 { get; set; }

    // Occurs 2
    [JsonPropertyName("Caffeine Content")]
    public string caffeinecontent { get; set; }

    // Occurs 2
    [JsonPropertyName("Guitar Pickup Configuration")]
    public string guitarpickupconfiguration { get; set; }

    // Occurs 2
    [JsonPropertyName("Caller Identification")]
    public string calleridentification { get; set; }

    // Occurs 2
    [JsonPropertyName("Corner Style")]
    public string cornerstyle { get; set; }

    // Occurs 2
    [JsonPropertyName("Tab Material")]
    public string tabmaterial { get; set; }

    // Occurs 2
    [JsonPropertyName("Wheel Backspacing 4.75 Inches")]
    public string wheelbackspacing475inches { get; set; }

    // Occurs 2
    [JsonPropertyName("Number of Compartments 2")]
    public string numberofcompartments2 { get; set; }

    // Occurs 2
    [JsonPropertyName("Core Size")]
    public string coresize { get; set; }

    // Occurs 2
    [JsonPropertyName("Surface Recommendation Wall, Door")]
    public string surfacerecommendationwalldoor { get; set; }

    // Occurs 2
    [JsonPropertyName("Finish Type Unfinished")]
    public string finishtypeunfinished { get; set; }

    // Occurs 2
    [JsonPropertyName("Maximum Compatible Wattage")]
    public string maximumcompatiblewattage { get; set; }

    // Occurs 2
    [JsonPropertyName("Edge Style")]
    public string edgestyle { get; set; }

    // Occurs 2
    [JsonPropertyName("Guitar Bridge System")]
    public string guitarbridgesystem { get; set; }

    // Occurs 2
    [JsonPropertyName("Upper Temperature Rating")]
    public string uppertemperaturerating { get; set; }

    // Occurs 2
    [JsonPropertyName("Handlebar Type")]
    public string handlebartype { get; set; }

    // Occurs 2
    [JsonPropertyName("Average Life")]
    public string averagelife { get; set; }

    // Occurs 2
    [JsonPropertyName("Writing Instrument Form")]
    public string writinginstrumentform { get; set; }

    // Occurs 2
    [JsonPropertyName("Keyboard Description")]
    public string keyboarddescription { get; set; }

    // Occurs 2
    [JsonPropertyName("Flow Rate")]
    public string flowrate { get; set; }

    // Occurs 2
    [JsonPropertyName("Output Voltage")]
    public string outputvoltage { get; set; }

    // Occurs 2
    [JsonPropertyName("Point Style")]
    public string pointstyle { get; set; }

    // Occurs 2
    [JsonPropertyName("Board book")]
    public string boardbook { get; set; }

    // Occurs 2
    [JsonPropertyName("Rim Width")]
    public string rimwidth { get; set; }

    // Occurs 2
    [JsonPropertyName("Folded size")]
    public string foldedsize { get; set; }

    // Occurs 2
    [JsonPropertyName("Sun Protection Factor")]
    public string sunprotectionfactor { get; set; }

    // Occurs 2
    [JsonPropertyName("Fretboard Material Type")]
    public string fretboardmaterialtype { get; set; }

    // Occurs 2
    [JsonPropertyName("Neck Material Type")]
    public string neckmaterialtype { get; set; }

    // Occurs 2
    [JsonPropertyName("Compatible products")]
    public string compatibleproducts { get; set; }

    // Occurs 2
    [JsonPropertyName("Radio Bands Supported")]
    public string radiobandssupported { get; set; }

    // Occurs 2
    [JsonPropertyName("Tuner Technology")]
    public string tunertechnology { get; set; }

    // Occurs 2
    [JsonPropertyName("Construction")]
    public string construction { get; set; }

    // Occurs 2
    [JsonPropertyName("Mounting Type Clip On")]
    public string mountingtypeclipon { get; set; }

    // Occurs 2
    [JsonPropertyName("Incandescent Equivalent Wattage")]
    public string incandescentequivalentwattage { get; set; }

    // Occurs 2
    [JsonPropertyName("Material Blend")]
    public string materialblend { get; set; }

    // Occurs 2
    [JsonPropertyName("Optical Drive Type")]
    public string opticaldrivetype { get; set; }

    // Occurs 2
    [JsonPropertyName("Audio-out Ports (#)")]
    public string audiooutports { get; set; }

    // Occurs 2
    [JsonPropertyName("Item Weight 13 Pounds")]
    public string itemweight13pounds { get; set; }

    // Occurs 2
    [JsonPropertyName("Section Width")]
    public string sectionwidth { get; set; }

    // Occurs 2
    [JsonPropertyName("Material Polyvinyl Chloride")]
    public string materialpolyvinylchloride { get; set; }

    // Occurs 2
    [JsonPropertyName("Alcohol Type")]
    public string alcoholtype { get; set; }

    // Occurs 2
    [JsonPropertyName("Photo Sensor Size")]
    public string photosensorsize { get; set; }

    // Occurs 2
    [JsonPropertyName("Memory Speed")]
    public string memoryspeed { get; set; }

    // Occurs 2
    [JsonPropertyName("Actuator Type")]
    public string actuatortype { get; set; }

    // Occurs 2
    [JsonPropertyName("Occasion Travel")]
    public string occasiontravel { get; set; }

    // Occurs 2
    [JsonPropertyName("Effective Still Resolution")]
    public string effectivestillresolution { get; set; }

    // Occurs 2
    [JsonPropertyName("Measurement Accuracy")]
    public string measurementaccuracy { get; set; }

    // Occurs 2
    [JsonPropertyName("Maximum Webcam Image Resolution")]
    public string maximumwebcamimageresolution { get; set; }

    // Occurs 2
    [JsonPropertyName("Folded Knife Size")]
    public string foldedknifesize { get; set; }

    // Occurs 2
    [JsonPropertyName("Number of Compartments 10")]
    public string numberofcompartments10 { get; set; }

    // Occurs 2
    [JsonPropertyName("Continuous Shooting Speed")]
    public string continuousshootingspeed { get; set; }

    // Occurs 2
    [JsonPropertyName("Photo Sensor Technology")]
    public string photosensortechnology { get; set; }

    // Occurs 2
    [JsonPropertyName("Memory Slots Available")]
    public string memoryslotsavailable { get; set; }

    // Occurs 2
    [JsonPropertyName("Lens Curvature Description")]
    public string lenscurvaturedescription { get; set; }

    // Occurs 2
    [JsonPropertyName("Hard Disk Rotational Speed")]
    public string harddiskrotationalspeed { get; set; }

    // Occurs 2
    [JsonPropertyName("MERV Rating")]
    public string mervrating { get; set; }

    // Occurs 2
    [JsonPropertyName("Material Spandex")]
    public string materialspandex { get; set; }

    // Occurs 2
    [JsonPropertyName("Handle")]
    public string handle { get; set; }

    // Occurs 2
    [JsonPropertyName("Rear Webcam Resolution")]
    public string rearwebcamresolution { get; set; }

    // Occurs 2
    [JsonPropertyName("SIM card slot count")]
    public string simcardslotcount { get; set; }

    // Occurs 2
    [JsonPropertyName("Viewing Area")]
    public string viewingarea { get; set; }

    // Occurs 2
    [JsonPropertyName("Compatible Device")]
    public string compatibledevice { get; set; }

    // Occurs 2
    [JsonPropertyName("Display resolution")]
    public string displayresolution { get; set; }

    // Occurs 2
    [JsonPropertyName("Handle Height")]
    public string handleheight { get; set; }

    // Occurs 2
    [JsonPropertyName("Contact Material")]
    public string contactmaterial { get; set; }

    // Occurs 2
    [JsonPropertyName("Service Provider")]
    public string serviceprovider { get; set; }

    // Occurs 2
    [JsonPropertyName("Mounting Type Insert,Inside Out")]
    public string mountingtypeinsertinsideout { get; set; }

    // Occurs 2
    [JsonPropertyName("Laser Beam Color")]
    public string laserbeamcolor { get; set; }

    // Occurs 2
    [JsonPropertyName("Flush Type")]
    public string flushtype { get; set; }

    // Occurs 2
    [JsonPropertyName("Cache Size")]
    public string cachesize { get; set; }

    // Occurs 2
    [JsonPropertyName("Light Source Type LED")]
    public string lightsourcetypeled { get; set; }

    // Occurs 2
    [JsonPropertyName("Print media")]
    public string printmedia { get; set; }

    // Occurs 2
    [JsonPropertyName("Bottle Nipple Type")]
    public string bottlenippletype { get; set; }

    // Occurs 2
    [JsonPropertyName("Cutting Width")]
    public string cuttingwidth { get; set; }

    // Occurs 2
    [JsonPropertyName("Mass Market Paperback")]
    public string massmarketpaperback { get; set; }

    // Occurs 2
    [JsonPropertyName("Vehicle Service Type Passenger Car")]
    public string vehicleservicetypepassengercar { get; set; }

    // Occurs 2
    [JsonPropertyName("Bearing Type")]
    public string bearingtype { get; set; }

    // Occurs 2
    [JsonPropertyName("Output Current")]
    public string outputcurrent { get; set; }

    // Occurs 2
    [JsonPropertyName("Response Time")]
    public string responsetime { get; set; }

    // Occurs 2
    [JsonPropertyName("Number of channels")]
    public string numberofchannels { get; set; }

    // Occurs 2
    [JsonPropertyName("Movement Type")]
    public string movementtype { get; set; }

    // Occurs 2
    [JsonPropertyName("Mirror Adjustment")]
    public string mirroradjustment { get; set; }

    // Occurs 2
    [JsonPropertyName("Read Speed")]
    public string readspeed { get; set; }

    // Occurs 2
    [JsonPropertyName("Resistance Mechanism")]
    public string resistancemechanism { get; set; }

    // Occurs 2
    [JsonPropertyName("Item Display dimensions L x W x H")]
    public string itemdisplaydimensionslxwxh { get; set; }

    // Occurs 2
    [JsonPropertyName("Top Material Type Polyester")]
    public string topmaterialtypepolyester { get; set; }

    // Occurs 2
    [JsonPropertyName("Air Gun Power Type")]
    public string airgunpowertype { get; set; }

    // Occurs 2
    [JsonPropertyName("Material Iron")]
    public string materialiron { get; set; }

    // Occurs 2
    [JsonPropertyName("Hole Count")]
    public string holecount { get; set; }

    // Occurs 2
    [JsonPropertyName("Material Care Instructions")]
    public string materialcareinstructions { get; set; }

    // Occurs 2
    [JsonPropertyName("Supports Bluetooth Technology")]
    public string supportsbluetoothtechnology { get; set; }

    // Occurs 2
    [JsonPropertyName("Backing Type")]
    public string backingtype { get; set; }

    // Occurs 2
    [JsonPropertyName("Minimum Compatible Size")]
    public string minimumcompatiblesize { get; set; }

    // Occurs 2
    [JsonPropertyName("Shape Square")]
    public string shapesquare { get; set; }

    // Occurs 1
    [JsonPropertyName("Maximum Stride Length")]
    public string maximumstridelength { get; set; }

    // Occurs 1
    [JsonPropertyName("Style RB-053")]
    public string stylerb053 { get; set; }

    // Occurs 1
    [JsonPropertyName("Refresh Rate")]
    public string refreshrate { get; set; }

    // Occurs 1
    [JsonPropertyName("Auto Part Position Rear")]
    public string autopartpositionrear { get; set; }

    // Occurs 1
    [JsonPropertyName("Material MDF, Pine & Iron")]
    public string materialmdfpineiron { get; set; }

    // Occurs 1
    [JsonPropertyName("Form Size")]
    public string formsize { get; set; }

    // Occurs 1
    [JsonPropertyName("Power Source Corded Electric, Battery Powered")]
    public string powersourcecordedelectricbatterypowered { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Clip On,Freestanding,Tabletop")]
    public string mountingtypecliponfreestandingtabletop { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Silicone, Plastic, Metal")]
    public string materialsiliconeplasticmetal { get; set; }

    // Occurs 1
    [JsonPropertyName("Frame Material Stainless Steel")]
    public string framematerialstainlesssteel { get; set; }

    // Occurs 1
    [JsonPropertyName("Speaker Diameter")]
    public string speakerdiameter { get; set; }

    // Occurs 1
    [JsonPropertyName("Number of Drawers 5")]
    public string numberofdrawers5 { get; set; }

    // Occurs 1
    [JsonPropertyName("Nominal Wall Thickness")]
    public string nominalwallthickness { get; set; }

    // Occurs 1
    [JsonPropertyName("Occasion Oktoberfest")]
    public string occasionoktoberfest { get; set; }

    // Occurs 1
    [JsonPropertyName("Bolt Pattern (Number of Holes)")]
    public string boltpatternnumberofholes { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Hanging,Wall Mount")]
    public string mountingtypehangingwallmount { get; set; }

    // Occurs 1
    [JsonPropertyName("Style 3-Pack")]
    public string style3pack { get; set; }

    // Occurs 1
    [JsonPropertyName("Style RB-015A")]
    public string stylerb015a { get; set; }

    // Occurs 1
    [JsonPropertyName("Occasion daily")]
    public string occasiondaily { get; set; }

    // Occurs 1
    [JsonPropertyName("Number of Compartments 4")]
    public string numberofcompartments4 { get; set; }

    // Occurs 1
    [JsonPropertyName("Cover Included")]
    public string coverincluded { get; set; }

    // Occurs 1
    [JsonPropertyName("Number of Drawers 3")]
    public string numberofdrawers3 { get; set; }

    // Occurs 1
    [JsonPropertyName("Awards won")]
    public string awardswon { get; set; }

    // Occurs 1
    [JsonPropertyName("Floor type")]
    public string floortype { get; set; }

    // Occurs 1
    [JsonPropertyName("Stem Material")]
    public string stemmaterial { get; set; }

    // Occurs 1
    [JsonPropertyName("RAM Memory Maximum Size")]
    public string rammemorymaximumsize { get; set; }

    // Occurs 1
    [JsonPropertyName("Subwoofer Diameter")]
    public string subwooferdiameter { get; set; }

    // Occurs 1
    [JsonPropertyName("Supported File Format")]
    public string supportedfileformat { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 10.4 Ounces")]
    public string itemweight104ounces { get; set; }

    // Occurs 1
    [JsonPropertyName("Terminal")]
    public string terminal { get; set; }

    // Occurs 1
    [JsonPropertyName("Telescope Mount Description")]
    public string telescopemountdescription { get; set; }

    // Occurs 1
    [JsonPropertyName("Expanded ISO Maximum")]
    public string expandedisomaximum { get; set; }

    // Occurs 1
    [JsonPropertyName("Maximum Incline Percentage")]
    public string maximuminclinepercentage { get; set; }

    // Occurs 1
    [JsonPropertyName("Speed Rating")]
    public string speedrating { get; set; }

    // Occurs 1
    [JsonPropertyName("Motherboard Compatability")]
    public string motherboardcompatability { get; set; }

    // Occurs 1
    [JsonPropertyName("Maximum Aperture")]
    public string maximumaperture { get; set; }

    // Occurs 1
    [JsonPropertyName("Digital Scene Transition")]
    public string digitalscenetransition { get; set; }

    // Occurs 1
    [JsonPropertyName("Case Type")]
    public string casetype { get; set; }

    // Occurs 1
    [JsonPropertyName("Min Shutter Speed")]
    public string minshutterspeed { get; set; }

    // Occurs 1
    [JsonPropertyName("JPEG quality level")]
    public string jpegqualitylevel { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Fabric with Mesh Pockets")]
    public string materialfabricwithmeshpockets { get; set; }

    // Occurs 1
    [JsonPropertyName("Input Device Interface")]
    public string inputdeviceinterface { get; set; }

    // Occurs 1
    [JsonPropertyName("Shelf Type Metal")]
    public string shelftypemetal { get; set; }

    // Occurs 1
    [JsonPropertyName("Top Width")]
    public string topwidth { get; set; }

    // Occurs 1
    [JsonPropertyName("Rod Length")]
    public string rodlength { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Canvas")]
    public string materialcanvas { get; set; }

    // Occurs 1
    [JsonPropertyName("Range")]
    public string range { get; set; }

    // Occurs 1
    [JsonPropertyName("Number of USB 2 Ports")]
    public string numberofusb2ports { get; set; }

    // Occurs 1
    [JsonPropertyName("Optical Tube Length")]
    public string opticaltubelength { get; set; }

    // Occurs 1
    [JsonPropertyName("Maximum Horsepower")]
    public string maximumhorsepower { get; set; }

    // Occurs 1
    [JsonPropertyName("Supports Bluetooth")]
    public string supportsbluetooth { get; set; }

    // Occurs 1
    [JsonPropertyName("Surface Recommendation Glass,Metal")]
    public string surfacerecommendationglassmetal { get; set; }

    // Occurs 1
    [JsonPropertyName("Capacity 6.5 Quarts")]
    public string capacity65quarts { get; set; }

    // Occurs 1
    [JsonPropertyName("Inlet Connection Size")]
    public string inletconnectionsize { get; set; }

    // Occurs 1
    [JsonPropertyName("Configuration")]
    public string configuration { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Bronze, Oak")]
    public string materialbronzeoak { get; set; }

    // Occurs 1
    [JsonPropertyName("Head Size")]
    public string headsize { get; set; }

    // Occurs 1
    [JsonPropertyName("Expected Plant Height")]
    public string expectedplantheight { get; set; }

    // Occurs 1
    [JsonPropertyName("Maximum Speed")]
    public string maximumspeed { get; set; }

    // Occurs 1
    [JsonPropertyName("Tip Type")]
    public string tiptype { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Metal Cast Iron")]
    public string materialmetalcastiron { get; set; }

    // Occurs 1
    [JsonPropertyName("Measurement Type")]
    public string measurementtype { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 5.7 Ounces")]
    public string itemweight57ounces { get; set; }

    // Occurs 1
    [JsonPropertyName("Video Output Resolution")]
    public string videooutputresolution { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Hung,Insert")]
    public string mountingtypehunginsert { get; set; }

    // Occurs 1
    [JsonPropertyName("Closure Material")]
    public string closurematerial { get; set; }

    // Occurs 1
    [JsonPropertyName("Comfort Layer Material")]
    public string comfortlayermaterial { get; set; }

    // Occurs 1
    [JsonPropertyName("Produce sold as")]
    public string producesoldas { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Door Mount,Insert")]
    public string mountingtypedoormountinsert { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Hanging")]
    public string mountingtypehanging { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Hardwood")]
    public string materialhardwood { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Stainless Steel,Synthetic,Opal")]
    public string materialstainlesssteelsyntheticopal { get; set; }

    // Occurs 1
    [JsonPropertyName("Biometric Security Feature")]
    public string biometricsecurityfeature { get; set; }

    // Occurs 1
    [JsonPropertyName("Material ceramic, fabric, wood")]
    public string materialceramicfabricwood { get; set; }

    // Occurs 1
    [JsonPropertyName("Wheel Backspacing 15 Micron")]
    public string wheelbackspacing15micron { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Rayon")]
    public string materialrayon { get; set; }

    // Occurs 1
    [JsonPropertyName("Maximum Voltage")]
    public string maximumvoltage { get; set; }

    // Occurs 1
    [JsonPropertyName("Capacitance")]
    public string capacitance { get; set; }

    // Occurs 1
    [JsonPropertyName("Controller Type Button Control")]
    public string controllertypebuttoncontrol { get; set; }

    // Occurs 1
    [JsonPropertyName("Power Source Corded Electric")]
    public string powersourcecordedelectric { get; set; }

    // Occurs 1
    [JsonPropertyName("Surface Recommendation nails")]
    public string surfacerecommendationnails { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 3 Pounds")]
    public string itemweight3pounds { get; set; }

    // Occurs 1
    [JsonPropertyName("Style Trunk")]
    public string styletrunk { get; set; }

    // Occurs 1
    [JsonPropertyName("Surface Recommendation Cloth, Glass, Wood, Paper, Plastic")]
    public string surfacerecommendationclothglasswoodpaperplastic { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Porcelain")]
    public string materialporcelain { get; set; }

    // Occurs 1
    [JsonPropertyName("Media Format Digital Video")]
    public string mediaformatdigitalvideo { get; set; }

    // Occurs 1
    [JsonPropertyName("Hollander Number")]
    public string hollandernumber { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Canvas, Leather")]
    public string materialcanvasleather { get; set; }

    // Occurs 1
    [JsonPropertyName("Circuit Breaker Type")]
    public string circuitbreakertype { get; set; }

    // Occurs 1
    [JsonPropertyName("Light Source Type Halogen")]
    public string lightsourcetypehalogen { get; set; }

    // Occurs 1
    [JsonPropertyName("ABPA Partslink Number")]
    public string abpapartslinknumber { get; set; }

    // Occurs 1
    [JsonPropertyName("Fit Type Vehicle Specific Fit")]
    public string fittypevehiclespecificfit { get; set; }

    // Occurs 1
    [JsonPropertyName("Exterior Finish Silver")]
    public string exteriorfinishsilver { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Foldable,Freestanding,Insert")]
    public string mountingtypefoldablefreestandinginsert { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 0.55 Pounds")]
    public string itemweight055pounds { get; set; }

    // Occurs 1
    [JsonPropertyName("Surface Recommendation Window, Wall, Door")]
    public string surfacerecommendationwindowwalldoor { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Stainless Steel, Plastic")]
    public string materialstainlesssteelplastic { get; set; }

    // Occurs 1
    [JsonPropertyName("Style Classic")]
    public string styleclassic { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Lego batman backpack, Lego batman school supplies")]
    public string materiallegobatmanbackpacklegobatmanschoolsupplies { get; set; }

    // Occurs 1
    [JsonPropertyName("Backing Material")]
    public string backingmaterial { get; set; }

    // Occurs 1
    [JsonPropertyName("Number of Keys")]
    public string numberofkeys { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Non-woven fabric, Polypropylene")]
    public string materialnonwovenfabricpolypropylene { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 42 Pounds")]
    public string itemweight42pounds { get; set; }

    // Occurs 1
    [JsonPropertyName("Material polyester, Engineered Wood")]
    public string materialpolyesterengineeredwood { get; set; }

    // Occurs 1
    [JsonPropertyName("Maximum Weight Recommendation 100 Kilograms")]
    public string maximumweightrecommendation100kilograms { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Insert,Foldable,Wall Mount")]
    public string mountingtypeinsertfoldablewallmount { get; set; }

    // Occurs 1
    [JsonPropertyName("Style Auto O/C W/ Chrome Handle")]
    public string styleautoocwchromehandle { get; set; }

    // Occurs 1
    [JsonPropertyName("Compatible with Rod Size")]
    public string compatiblewithrodsize { get; set; }

    // Occurs 1
    [JsonPropertyName("Floor Width")]
    public string floorwidth { get; set; }

    // Occurs 1
    [JsonPropertyName("Mint Mark")]
    public string mintmark { get; set; }

    // Occurs 1
    [JsonPropertyName("Film Color")]
    public string filmcolor { get; set; }

    // Occurs 1
    [JsonPropertyName("Handle Location")]
    public string handlelocation { get; set; }

    // Occurs 1
    [JsonPropertyName("Color Rendering Index (CRI)")]
    public string colorrenderingindexcri { get; set; }

    // Occurs 1
    [JsonPropertyName("Light Source Wattage")]
    public string lightsourcewattage { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Textile")]
    public string materialtextile { get; set; }

    // Occurs 1
    [JsonPropertyName("Gas Type")]
    public string gastype { get; set; }

    // Occurs 1
    [JsonPropertyName("Frame Material Wood")]
    public string framematerialwood { get; set; }

    // Occurs 1
    [JsonPropertyName("Style Steve Madden Amina Rhinestone Mini Top Handle")]
    public string stylestevemaddenaminarhinestoneminitophandle { get; set; }

    // Occurs 1
    [JsonPropertyName("Max Number of Supported Devices")]
    public string maxnumberofsupporteddevices { get; set; }

    // Occurs 1
    [JsonPropertyName("Recording Capacity")]
    public string recordingcapacity { get; set; }

    // Occurs 1
    [JsonPropertyName("Frame Material Metal")]
    public string framematerialmetal { get; set; }

    // Occurs 1
    [JsonPropertyName("Amperage Capacity")]
    public string amperagecapacity { get; set; }

    // Occurs 1
    [JsonPropertyName("Material not-applicable")]
    public string materialnotapplicable { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Zinc")]
    public string materialzinc { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Ceramic-Thread")]
    public string materialceramicthread { get; set; }

    // Occurs 1
    [JsonPropertyName("Assembly Required Yes")]
    public string assemblyrequiredyes { get; set; }

    // Occurs 1
    [JsonPropertyName("Top Material Type Vinyl, Iron")]
    public string topmaterialtypevinyliron { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Handheld")]
    public string mountingtypehandheld { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Found in image, Hand mount")]
    public string mountingtypefoundinimagehandmount { get; set; }

    // Occurs 1
    [JsonPropertyName("Tire Type")]
    public string tiretype { get; set; }

    // Occurs 1
    [JsonPropertyName("Test type")]
    public string testtype { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type On The Door")]
    public string mountingtypeonthedoor { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Cedar Wood")]
    public string materialcedarwood { get; set; }

    // Occurs 1
    [JsonPropertyName("Style 2-Tier with Drawer")]
    public string style2tierwithdrawer { get; set; }

    // Occurs 1
    [JsonPropertyName("Eye Relief")]
    public string eyerelief { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Insert,Surface")]
    public string mountingtypeinsertsurface { get; set; }

    // Occurs 1
    [JsonPropertyName("Vehicle Service Type Motorcycle")]
    public string vehicleservicetypemotorcycle { get; set; }

    // Occurs 1
    [JsonPropertyName("Mirror Lens Type")]
    public string mirrorlenstype { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Rubberwood, China Maple")]
    public string materialrubberwoodchinamaple { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Nylon")]
    public string materialnylon { get; set; }

    // Occurs 1
    [JsonPropertyName("Material MDF + Pine")]
    public string materialmdfpine { get; set; }

    // Occurs 1
    [JsonPropertyName("Suspension")]
    public string suspension { get; set; }

    // Occurs 1
    [JsonPropertyName("AC Adapter Current")]
    public string acadaptercurrent { get; set; }

    // Occurs 1
    [JsonPropertyName("Telephone Type")]
    public string telephonetype { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Pine Wood")]
    public string materialpinewood { get; set; }

    // Occurs 1
    [JsonPropertyName("Exterior Finish Chrome")]
    public string exteriorfinishchrome { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Cotton Blend")]
    public string materialcottonblend { get; set; }

    // Occurs 1
    [JsonPropertyName("Capacity 8 Ounces")]
    public string capacity8ounces { get; set; }

    // Occurs 1
    [JsonPropertyName("Metal Type Stainless Steel")]
    public string metaltypestainlesssteel { get; set; }

    // Occurs 1
    [JsonPropertyName("Style Asian")]
    public string styleasian { get; set; }

    // Occurs 1
    [JsonPropertyName("Shelf Type Floating Shelf")]
    public string shelftypefloatingshelf { get; set; }

    // Occurs 1
    [JsonPropertyName("Style Compatible")]
    public string stylecompatible { get; set; }

    // Occurs 1
    [JsonPropertyName("Metal Type Iron")]
    public string metaltypeiron { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Powder")]
    public string materialpowder { get; set; }

    // Occurs 1
    [JsonPropertyName("Cord Length")]
    public string cordlength { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Faux Fur")]
    public string materialfauxfur { get; set; }

    // Occurs 1
    [JsonPropertyName("Maximum Weight Recommendation 3 Pounds")]
    public string maximumweightrecommendation3pounds { get; set; }

    // Occurs 1
    [JsonPropertyName("Seat Depth")]
    public string seatdepth { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Bamboo")]
    public string materialbamboo { get; set; }

    // Occurs 1
    [JsonPropertyName("Proficiency Level")]
    public string proficiencylevel { get; set; }

    // Occurs 1
    [JsonPropertyName("Exterior Finish Rubber")]
    public string exteriorfinishrubber { get; set; }

    // Occurs 1
    [JsonPropertyName("Light Source Type krypton/led")]
    public string lightsourcetypekryptonled { get; set; }

    // Occurs 1
    [JsonPropertyName("Style Country")]
    public string stylecountry { get; set; }

    // Occurs 1
    [JsonPropertyName("Style Compact,Modern,Modern Vanity")]
    public string stylecompactmodernmodernvanity { get; set; }

    // Occurs 1
    [JsonPropertyName("Control Method Touch")]
    public string controlmethodtouch { get; set; }

    // Occurs 1
    [JsonPropertyName("Material PP+PET")]
    public string materialpppet { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 0.19 Kilograms")]
    public string itemweight019kilograms { get; set; }

    // Occurs 1
    [JsonPropertyName("Surface Recommendation Plastic")]
    public string surfacerecommendationplastic { get; set; }

    // Occurs 1
    [JsonPropertyName("Capacity 11 Ounces")]
    public string capacity11ounces { get; set; }

    // Occurs 1
    [JsonPropertyName("Temperature Condition")]
    public string temperaturecondition { get; set; }

    // Occurs 1
    [JsonPropertyName("String Material")]
    public string stringmaterial { get; set; }

    // Occurs 1
    [JsonPropertyName("Exposure Control Type")]
    public string exposurecontroltype { get; set; }

    // Occurs 1
    [JsonPropertyName("Vehicle Service Type Car")]
    public string vehicleservicetypecar { get; set; }

    // Occurs 1
    [JsonPropertyName("Frame Material Aluminum, Alloy Steel")]
    public string framematerialaluminumalloysteel { get; set; }

    // Occurs 1
    [JsonPropertyName("Net Content Weight")]
    public string netcontentweight { get; set; }

    // Occurs 1
    [JsonPropertyName("Action")]
    public string action { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type 顶天立地式")]
    public string mountingtype顶天立地式 { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Freestanding,Tabletop")]
    public string mountingtypefreestandingtabletop { get; set; }

    // Occurs 1
    [JsonPropertyName("Material pearl")]
    public string materialpearl { get; set; }

    // Occurs 1
    [JsonPropertyName("Style Lounger")]
    public string stylelounger { get; set; }

    // Occurs 1
    [JsonPropertyName("Shape Oval")]
    public string shapeoval { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Polyester, Stainless Steel")]
    public string materialpolyesterstainlesssteel { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Peltre")]
    public string materialpeltre { get; set; }

    // Occurs 1
    [JsonPropertyName("Exterior Finish Rubber,Metal")]
    public string exteriorfinishrubbermetal { get; set; }

    // Occurs 1
    [JsonPropertyName("Is heat sensitive?")]
    public string isheatsensitive { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Door Mount")]
    public string mountingtypedoormount { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Fur")]
    public string materialfur { get; set; }

    // Occurs 1
    [JsonPropertyName("Heater Surface Material")]
    public string heatersurfacematerial { get; set; }

    // Occurs 1
    [JsonPropertyName("Seat Material Type Polyester")]
    public string seatmaterialtypepolyester { get; set; }

    // Occurs 1
    [JsonPropertyName("Load Index 95")]
    public string loadindex95 { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Suede, Glass")]
    public string materialsuedeglass { get; set; }

    // Occurs 1
    [JsonPropertyName("Stationery")]
    public string stationery { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 7.5 Pounds")]
    public string itemweight75pounds { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 0.71 Pounds")]
    public string itemweight071pounds { get; set; }

    // Occurs 1
    [JsonPropertyName("Supported Standards")]
    public string supportedstandards { get; set; }

    // Occurs 1
    [JsonPropertyName("Component Type")]
    public string componenttype { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 30 Pounds")]
    public string itemweight30pounds { get; set; }

    // Occurs 1
    [JsonPropertyName("Item display height")]
    public string itemdisplayheight { get; set; }

    // Occurs 1
    [JsonPropertyName("Handle Type Knob, Pull Handle")]
    public string handletypeknobpullhandle { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 1 Grams")]
    public string itemweight1grams { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Textile, Linen")]
    public string materialtextilelinen { get; set; }

    // Occurs 1
    [JsonPropertyName("Boot Length")]
    public string bootlength { get; set; }

    // Occurs 1
    [JsonPropertyName("Capacity 15 Ounces")]
    public string capacity15ounces { get; set; }

    // Occurs 1
    [JsonPropertyName("Rim Size 15 Inches")]
    public string rimsize15inches { get; set; }

    // Occurs 1
    [JsonPropertyName("Pull Force")]
    public string pullforce { get; set; }

    // Occurs 1
    [JsonPropertyName("Capacity 13.5 Ounces")]
    public string capacity135ounces { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Cotton,Metal")]
    public string materialcottonmetal { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Polyurethane (PU)")]
    public string materialpolyurethanepu { get; set; }

    // Occurs 1
    [JsonPropertyName("Cutting Angle String")]
    public string cuttinganglestring { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Nylon, Cotton")]
    public string materialnyloncotton { get; set; }

    // Occurs 1
    [JsonPropertyName("Efficiency")]
    public string efficiency { get; set; }

    // Occurs 1
    [JsonPropertyName("Vehicle Service Type ATV")]
    public string vehicleservicetypeatv { get; set; }

    // Occurs 1
    [JsonPropertyName("Ultraviolet Light Protection Anti UV")]
    public string ultravioletlightprotectionantiuv { get; set; }

    // Occurs 1
    [JsonPropertyName("Rim Diameter")]
    public string rimdiameter { get; set; }

    // Occurs 1
    [JsonPropertyName("Number of Positions")]
    public string numberofpositions { get; set; }

    // Occurs 1
    [JsonPropertyName("Output Wattage")]
    public string outputwattage { get; set; }

    // Occurs 1
    [JsonPropertyName("UTQG")]
    public string utqg { get; set; }

    // Occurs 1
    [JsonPropertyName("Expanded ISO Minimum")]
    public string expandedisominimum { get; set; }

    // Occurs 1
    [JsonPropertyName("Talking Range Maximum")]
    public string talkingrangemaximum { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Silver")]
    public string materialsilver { get; set; }

    // Occurs 1
    [JsonPropertyName("Installation Type Wall Mount")]
    public string installationtypewallmount { get; set; }

    // Occurs 1
    [JsonPropertyName("Honey Type")]
    public string honeytype { get; set; }

    // Occurs 1
    [JsonPropertyName("Switch Type Touch Switch")]
    public string switchtypetouchswitch { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Beech Wood, Wood, Acrylic")]
    public string materialbeechwoodwoodacrylic { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Fabric")]
    public string materialfabric { get; set; }

    // Occurs 1
    [JsonPropertyName("Primary Supplement Type")]
    public string primarysupplementtype { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Wall Mount, Insert Mount")]
    public string mountingtypewallmountinsertmount { get; set; }

    // Occurs 1
    [JsonPropertyName("Color Family")]
    public string colorfamily { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Polyester fiber")]
    public string materialpolyesterfiber { get; set; }

    // Occurs 1
    [JsonPropertyName("Style Magnetic,Unique")]
    public string stylemagneticunique { get; set; }

    // Occurs 1
    [JsonPropertyName("Adjustable Length")]
    public string adjustablelength { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Wood, Glass")]
    public string materialwoodglass { get; set; }

    // Occurs 1
    [JsonPropertyName("Number of Doors 1")]
    public string numberofdoors1 { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Engineered Wood, Glass")]
    public string materialengineeredwoodglass { get; set; }

    // Occurs 1
    [JsonPropertyName("Handle Type Knob, Lever")]
    public string handletypeknoblever { get; set; }

    // Occurs 1
    [JsonPropertyName("Temperature Range")]
    public string temperaturerange { get; set; }

    // Occurs 1
    [JsonPropertyName("Metal Type Brass")]
    public string metaltypebrass { get; set; }

    // Occurs 1
    [JsonPropertyName("Shape D Shape")]
    public string shapedshape { get; set; }

    // Occurs 1
    [JsonPropertyName("Material BPA Free, Phthalate Free, Latex Free, Lead Free")]
    public string materialbpafreephthalatefreelatexfreeleadfree { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 1.92 Pounds")]
    public string itemweight192pounds { get; set; }

    // Occurs 1
    [JsonPropertyName("Center To Center Spacing")]
    public string centertocenterspacing { get; set; }

    // Occurs 1
    [JsonPropertyName("Tread Type")]
    public string treadtype { get; set; }

    // Occurs 1
    [JsonPropertyName("Tread Depth")]
    public string treaddepth { get; set; }

    // Occurs 1
    [JsonPropertyName("Number of Flutes")]
    public string numberofflutes { get; set; }

    // Occurs 1
    [JsonPropertyName("Audio Output Type")]
    public string audiooutputtype { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Alloy Steel")]
    public string materialalloysteel { get; set; }

    // Occurs 1
    [JsonPropertyName("Frame Material Alloy Steel")]
    public string framematerialalloysteel { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Polyurethane, Ethylene Vinyl Acetate")]
    public string materialpolyurethaneethylenevinylacetate { get; set; }

    // Occurs 1
    [JsonPropertyName("Cutting Angle")]
    public string cuttingangle { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Floor Mount")]
    public string mountingtypefloormount { get; set; }

    // Occurs 1
    [JsonPropertyName("Tire Diameter")]
    public string tirediameter { get; set; }

    // Occurs 1
    [JsonPropertyName("Rake Type")]
    public string raketype { get; set; }

    // Occurs 1
    [JsonPropertyName("Set Screw Thread Type")]
    public string setscrewthreadtype { get; set; }

    // Occurs 1
    [JsonPropertyName("Capacity 13 Ounces")]
    public string capacity13ounces { get; set; }

    // Occurs 1
    [JsonPropertyName("Capacity 400 Milliliters")]
    public string capacity400milliliters { get; set; }

    // Occurs 1
    [JsonPropertyName("CPU Manufacturer")]
    public string cpumanufacturer { get; set; }

    // Occurs 1
    [JsonPropertyName("Fit Type Universal Fit")]
    public string fittypeuniversalfit { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Copper")]
    public string materialcopper { get; set; }

    // Occurs 1
    [JsonPropertyName("Material cotton polyester blend")]
    public string materialcottonpolyesterblend { get; set; }

    // Occurs 1
    [JsonPropertyName("Power Connector Type")]
    public string powerconnectortype { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 16 Grams")]
    public string itemweight16grams { get; set; }

    // Occurs 1
    [JsonPropertyName("Cross section shape")]
    public string crosssectionshape { get; set; }

    // Occurs 1
    [JsonPropertyName("Remote Control Included?")]
    public string remotecontrolincluded { get; set; }

    // Occurs 1
    [JsonPropertyName("Seat Material Type Leather")]
    public string seatmaterialtypeleather { get; set; }

    // Occurs 1
    [JsonPropertyName("Auto Part Position Front")]
    public string autopartpositionfront { get; set; }

    // Occurs 1
    [JsonPropertyName("Exterior Finish Metal,Rubber")]
    public string exteriorfinishmetalrubber { get; set; }

    // Occurs 1
    [JsonPropertyName("Assembled Seat Height")]
    public string assembledseatheight { get; set; }

    // Occurs 1
    [JsonPropertyName("Shelf Type Plastic, Metal")]
    public string shelftypeplasticmetal { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Polystyrene")]
    public string materialpolystyrene { get; set; }

    // Occurs 1
    [JsonPropertyName("Maximum temperature")]
    public string maximumtemperature { get; set; }

    // Occurs 1
    [JsonPropertyName("Region of Origin")]
    public string regionoforigin { get; set; }

    // Occurs 1
    [JsonPropertyName("Ear Placement")]
    public string earplacement { get; set; }

    // Occurs 1
    [JsonPropertyName("Heat Output")]
    public string heatoutput { get; set; }

    // Occurs 1
    [JsonPropertyName("Heating Coverage")]
    public string heatingcoverage { get; set; }

    // Occurs 1
    [JsonPropertyName("String Gauge")]
    public string stringgauge { get; set; }

    // Occurs 1
    [JsonPropertyName("Capacity Description")]
    public string capacitydescription { get; set; }

    // Occurs 1
    [JsonPropertyName("Occasion Everyday")]
    public string occasioneveryday { get; set; }

    // Occurs 1
    [JsonPropertyName("Installation Type Ground Mount")]
    public string installationtypegroundmount { get; set; }

    // Occurs 1
    [JsonPropertyName("Handle Type Knob")]
    public string handletypeknob { get; set; }

    // Occurs 1
    [JsonPropertyName("Metal Type Aluminum")]
    public string metaltypealuminum { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Polypropylene")]
    public string materialpolypropylene { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Phone Mount,Tabletop")]
    public string mountingtypephonemounttabletop { get; set; }

    // Occurs 1
    [JsonPropertyName("Objective Lens Description")]
    public string objectivelensdescription { get; set; }

    // Occurs 1
    [JsonPropertyName("Drop Length")]
    public string droplength { get; set; }

    // Occurs 1
    [JsonPropertyName("Torque")]
    public string torque { get; set; }

    // Occurs 1
    [JsonPropertyName("Bolt Pattern (Pitch Circle Diameter)")]
    public string boltpatternpitchcirclediameter { get; set; }

    // Occurs 1
    [JsonPropertyName("Planter Form")]
    public string planterform { get; set; }

    // Occurs 1
    [JsonPropertyName("Drawer Type")]
    public string drawertype { get; set; }

    // Occurs 1
    [JsonPropertyName("Max Printspeed Monochrome")]
    public string maxprintspeedmonochrome { get; set; }

    // Occurs 1
    [JsonPropertyName("Maximum Print Speed (Color)")]
    public string maximumprintspeedcolor { get; set; }

    // Occurs 1
    [JsonPropertyName("Printer Output")]
    public string printeroutput { get; set; }

    // Occurs 1
    [JsonPropertyName("Printing Technology")]
    public string printingtechnology { get; set; }

    // Occurs 1
    [JsonPropertyName("Flash Point")]
    public string flashpoint { get; set; }

    // Occurs 1
    [JsonPropertyName("Maximum Tilt Angle")]
    public string maximumtiltangle { get; set; }

    // Occurs 1
    [JsonPropertyName("Fretboard Material")]
    public string fretboardmaterial { get; set; }

    // Occurs 1
    [JsonPropertyName("Real Angle of View")]
    public string realangleofview { get; set; }

    // Occurs 1
    [JsonPropertyName("Optical Sensor Technology")]
    public string opticalsensortechnology { get; set; }

    // Occurs 1
    [JsonPropertyName("Style Thermal,Fingerless")]
    public string stylethermalfingerless { get; set; }

    // Occurs 1
    [JsonPropertyName("Liquid Contents Description")]
    public string liquidcontentsdescription { get; set; }

    // Occurs 1
    [JsonPropertyName("Frequency Band Class")]
    public string frequencybandclass { get; set; }

    // Occurs 1
    [JsonPropertyName("Graduation Range")]
    public string graduationrange { get; set; }

    // Occurs 1
    [JsonPropertyName("Magnification Maximum")]
    public string magnificationmaximum { get; set; }

    // Occurs 1
    [JsonPropertyName("Bulb Length")]
    public string bulblength { get; set; }

    // Occurs 1
    [JsonPropertyName("Portable")]
    public string portable { get; set; }

    // Occurs 1
    [JsonPropertyName("Number of Compartments 3")]
    public string numberofcompartments3 { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Wool")]
    public string materialwool { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Attribute not applicable for product")]
    public string mountingtypeattributenotapplicableforproduct { get; set; }

    // Occurs 1
    [JsonPropertyName("Warranty")]
    public string warranty { get; set; }

    // Occurs 1
    [JsonPropertyName("Filter Type")]
    public string filtertype { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Leather, Rubber")]
    public string materialleatherrubber { get; set; }

    // Occurs 1
    [JsonPropertyName("Page numbers source ISBN")]
    public string pagenumberssourceisbn { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 1 Ounces")]
    public string itemweight1ounces { get; set; }

    // Occurs 1
    [JsonPropertyName("Has Self-Timer")]
    public string hasselftimer { get; set; }

    // Occurs 1
    [JsonPropertyName("Drain Type")]
    public string draintype { get; set; }

    // Occurs 1
    [JsonPropertyName("Image Capture Speed")]
    public string imagecapturespeed { get; set; }

    // Occurs 1
    [JsonPropertyName("Floor Area 1 Cubic Meters")]
    public string floorarea1cubicmeters { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Glass, Burlap")]
    public string materialglassburlap { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Non Woven Polypropylene")]
    public string materialnonwovenpolypropylene { get; set; }

    // Occurs 1
    [JsonPropertyName("Switch Type Rocker")]
    public string switchtyperocker { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Polyester, Cotton")]
    public string materialpolyestercotton { get; set; }

    // Occurs 1
    [JsonPropertyName("Style E-060")]
    public string stylee060 { get; set; }

    // Occurs 1
    [JsonPropertyName("Load Index")]
    public string loadindex { get; set; }

    // Occurs 1
    [JsonPropertyName("Pitch Circle Diameter")]
    public string pitchcirclediameter { get; set; }

    // Occurs 1
    [JsonPropertyName("Style Traditional")]
    public string styletraditional { get; set; }

    // Occurs 1
    [JsonPropertyName("Occasion Engagement, Wedding, Anniversary, Birthday")]
    public string occasionengagementweddinganniversarybirthday { get; set; }

    // Occurs 1
    [JsonPropertyName("Sun Protection")]
    public string sunprotection { get; set; }

    // Occurs 1
    [JsonPropertyName("Number of Drawers 1")]
    public string numberofdrawers1 { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Alloy")]
    public string materialalloy { get; set; }

    // Occurs 1
    [JsonPropertyName("Calculator Type")]
    public string calculatortype { get; set; }

    // Occurs 1
    [JsonPropertyName("Surface Recommendation Fabric")]
    public string surfacerecommendationfabric { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Stainless Steel, Silicone, Acrylonitrile Butadiene StyreneStainless Steel, Silicone, Acrylonitrile Butadiene Styrene  See more")]
    public string materialstainlesssteelsiliconeacrylonitrilebutadienestyrenestainlesssteelsiliconeacrylonitrilebutadienestyreneseemore { get; set; }

    // Occurs 1
    [JsonPropertyName("Coffee Maker Type")]
    public string coffeemakertype { get; set; }

    // Occurs 1
    [JsonPropertyName("Occasion Birthday, Mother's Day, Teacher's Day, Valentineâ€™s Day, Anniversary, Christmas, GraduationBirthday, Mother's Day, Teacher's Day, Valentineâ€™s Day, Anniversary, Christmas, Graduation  See more")]
    public string occasionbirthdaymothersdayteachersdayvalentineâsdayanniversarychristmasgraduationbirthdaymothersdayteachersdayvalentineâsdayanniversarychristmasgraduationseemore { get; set; }

    // Occurs 1
    [JsonPropertyName("Mounting Type Insert")]
    public string mountingtypeinsert { get; set; }

    // Occurs 1
    [JsonPropertyName("Tab Color")]
    public string tabcolor { get; set; }

    // Occurs 1
    [JsonPropertyName("Occasion Cocktail Party")]
    public string occasioncocktailparty { get; set; }

    // Occurs 1
    [JsonPropertyName("Heating Elements")]
    public string heatingelements { get; set; }

    // Occurs 1
    [JsonPropertyName("Occasion Bachelor Party")]
    public string occasionbachelorparty { get; set; }

    // Occurs 1
    [JsonPropertyName("Item Weight 0.05 Pounds")]
    public string itemweight005pounds { get; set; }

    // Occurs 1
    [JsonPropertyName("Recycled Content Percentage")]
    public string recycledcontentpercentage { get; set; }

    // Occurs 1
    [JsonPropertyName("Contact Current Rating")]
    public string contactcurrentrating { get; set; }

    // Occurs 1
    [JsonPropertyName("Answering System Type")]
    public string answeringsystemtype { get; set; }

    // Occurs 1
    [JsonPropertyName("Dialer Type")]
    public string dialertype { get; set; }

    // Occurs 1
    [JsonPropertyName("Maximum Switching Current")]
    public string maximumswitchingcurrent { get; set; }

    // Occurs 1
    [JsonPropertyName("Rim Size 18 Inches")]
    public string rimsize18inches { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Paper, Acrylic")]
    public string materialpaperacrylic { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Pewter, Glass, Crystal")]
    public string materialpewterglasscrystal { get; set; }

    // Occurs 1
    [JsonPropertyName("Material 316L stainless-steel")]
    public string material316lstainlesssteel { get; set; }

    // Occurs 1
    [JsonPropertyName("Style RB-039A")]
    public string stylerb039a { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Aluminum, Acrylonitrile Butadiene Styrene, Metal, PolycarbonateAluminum, Acrylonitrile Butadiene Styrene, Metal, Polycarbonate  See more")]
    public string materialaluminumacrylonitrilebutadienestyrenemetalpolycarbonatealuminumacrylonitrilebutadienestyrenemetalpolycarbonateseemore { get; set; }

    // Occurs 1
    [JsonPropertyName("Net Content Volume")]
    public string netcontentvolume { get; set; }

    // Occurs 1
    [JsonPropertyName("Movement Detection Technology")]
    public string movementdetectiontechnology { get; set; }

    // Occurs 1
    [JsonPropertyName("Gauge")]
    public string gauge { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Neoprene")]
    public string materialneoprene { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Polyurethane")]
    public string materialpolyurethane { get; set; }

    // Occurs 1
    [JsonPropertyName("Occasion Home")]
    public string occasionhome { get; set; }

    // Occurs 1
    [JsonPropertyName("Material Abs")]
    public string materialabs { get; set; }

}
public class DetailsBestsellersrank
{
    // Occurs 150455
    [JsonPropertyName("Clothing, Shoes & Jewelry")]
    public string clothingshoesjewelry { get; set; }

    // Occurs 7304
    [JsonPropertyName("Jewelry Boxes")]
    public string jewelryboxes { get; set; }

    // Occurs 6660
    [JsonPropertyName("Casual Daypack Backpacks")]
    public string casualdaypackbackpacks { get; set; }

    // Occurs 6598
    [JsonPropertyName("Men's Wallets")]
    public string menswallets { get; set; }

    // Occurs 4757
    [JsonPropertyName("Men's Fashion")]
    public string mensfashion { get; set; }

    // Occurs 4461
    [JsonPropertyName("Women's Wallets")]
    public string womenswallets { get; set; }

    // Occurs 4309
    [JsonPropertyName("Travel Duffel Bags")]
    public string travelduffelbags { get; set; }

    // Occurs 3964
    [JsonPropertyName("Kids' Backpacks")]
    public string kidsbackpacks { get; set; }

    // Occurs 3132
    [JsonPropertyName("Jewelry Trays")]
    public string jewelrytrays { get; set; }

    // Occurs 2983
    [JsonPropertyName("Jewelry Towers")]
    public string jewelrytowers { get; set; }

    // Occurs 2742
    [JsonPropertyName("Luggage Tags")]
    public string luggagetags { get; set; }

    // Occurs 2740
    [JsonPropertyName("Carry-On Luggage")]
    public string carryonluggage { get; set; }

    // Occurs 2722
    [JsonPropertyName("Women's Fashion")]
    public string womensfashion { get; set; }

    // Occurs 2289
    [JsonPropertyName("Suitcases")]
    public string suitcases { get; set; }

    // Occurs 2077
    [JsonPropertyName("Women's Keyrings & Keychains")]
    public string womenskeyringskeychains { get; set; }

    // Occurs 1947
    [JsonPropertyName("Travel Packing Organizers")]
    public string travelpackingorganizers { get; set; }

    // Occurs 1924
    [JsonPropertyName("Sports Duffel Bags")]
    public string sportsduffelbags { get; set; }

    // Occurs 1840
    [JsonPropertyName("Fashion Waist Packs")]
    public string fashionwaistpacks { get; set; }

    // Occurs 1811
    [JsonPropertyName("Sports & Outdoors")]
    public string sportsoutdoors { get; set; }

    // Occurs 1522
    [JsonPropertyName("Luggage Sets")]
    public string luggagesets { get; set; }

    // Occurs 1479
    [JsonPropertyName("Men's Cycling Gloves")]
    public string menscyclinggloves { get; set; }

    // Occurs 1375
    [JsonPropertyName("Women's Fashion Backpack Handbags")]
    public string womensfashionbackpackhandbags { get; set; }

    // Occurs 1342
    [JsonPropertyName("Women's Coin Purses & Pouches")]
    public string womenscoinpursespouches { get; set; }

    // Occurs 1315
    [JsonPropertyName("Women's Novelty Keychains")]
    public string womensnoveltykeychains { get; set; }

    // Occurs 1283
    [JsonPropertyName("Toys & Games")]
    public string toysgames { get; set; }

    // Occurs 1275
    [JsonPropertyName("Stick Umbrellas")]
    public string stickumbrellas { get; set; }

    // Occurs 1267
    [JsonPropertyName("Men's Novelty Applique Patches")]
    public string mensnoveltyappliquepatches { get; set; }

    // Occurs 1251
    [JsonPropertyName("Gym Drawstring Bags")]
    public string gymdrawstringbags { get; set; }

    // Occurs 1231
    [JsonPropertyName("Women's Crossbody Handbags")]
    public string womenscrossbodyhandbags { get; set; }

    // Occurs 1198
    [JsonPropertyName("Men's Novelty Keychains")]
    public string mensnoveltykeychains { get; set; }

    // Occurs 1108
    [JsonPropertyName("Women's Drop & Dangle Earrings")]
    public string womensdropdangleearrings { get; set; }

    // Occurs 1107
    [JsonPropertyName("Men's Novelty Buttons & Pins")]
    public string mensnoveltybuttonspins { get; set; }

    // Occurs 1073
    [JsonPropertyName("Travel Tote Bags")]
    public string traveltotebags { get; set; }

    // Occurs 1058
    [JsonPropertyName("Men's Keyrings & Keychains")]
    public string menskeyringskeychains { get; set; }

    // Occurs 1005
    [JsonPropertyName("Luggage")]
    public string luggage { get; set; }

    // Occurs 993
    [JsonPropertyName("Backpacks")]
    public string backpacks { get; set; }

    // Occurs 988
    [JsonPropertyName("Messenger Bags")]
    public string messengerbags { get; set; }

    // Occurs 938
    [JsonPropertyName("Women's Card Cases")]
    public string womenscardcases { get; set; }

    // Occurs 938
    [JsonPropertyName("Passport Covers")]
    public string passportcovers { get; set; }

    // Occurs 935
    [JsonPropertyName("Costume Weapons & Armor")]
    public string costumeweaponsarmor { get; set; }

    // Occurs 934
    [JsonPropertyName("Women's Tote Handbags")]
    public string womenstotehandbags { get; set; }

    // Occurs 917
    [JsonPropertyName("Watch Cabinets & Cases")]
    public string watchcabinetscases { get; set; }

    // Occurs 895
    [JsonPropertyName("Watch Repair Tools & Kits")]
    public string watchrepairtoolskits { get; set; }

    // Occurs 879
    [JsonPropertyName("Men's Military Accessories")]
    public string mensmilitaryaccessories { get; set; }

    // Occurs 850
    [JsonPropertyName("Jewelry Armoires")]
    public string jewelryarmoires { get; set; }

    // Occurs 840
    [JsonPropertyName("Luggage Straps")]
    public string luggagestraps { get; set; }

    // Occurs 826
    [JsonPropertyName("Passport Wallets")]
    public string passportwallets { get; set; }

    // Occurs 812
    [JsonPropertyName("Women's Shoulder Handbags")]
    public string womensshoulderhandbags { get; set; }

    // Occurs 796
    [JsonPropertyName("Women's Brooches & Pins")]
    public string womensbroochespins { get; set; }

    // Occurs 795
    [JsonPropertyName("Women's Bead Charms")]
    public string womensbeadcharms { get; set; }

    // Occurs 794
    [JsonPropertyName("Boys' Costumes")]
    public string boyscostumes { get; set; }

    // Occurs 777
    [JsonPropertyName("Handbag Organizers")]
    public string handbagorganizers { get; set; }

    // Occurs 776
    [JsonPropertyName("Novelty Clothing")]
    public string noveltyclothing { get; set; }

    // Occurs 730
    [JsonPropertyName("Women's Pendant Necklaces")]
    public string womenspendantnecklaces { get; set; }

    // Occurs 720
    [JsonPropertyName("Girls' Costumes")]
    public string girlscostumes { get; set; }

    // Occurs 715
    [JsonPropertyName("Folding Umbrellas")]
    public string foldingumbrellas { get; set; }

    // Occurs 692
    [JsonPropertyName("Women's Cycling Gloves")]
    public string womenscyclinggloves { get; set; }

    // Occurs 661
    [JsonPropertyName("Men's Cold Weather Gloves")]
    public string menscoldweathergloves { get; set; }

    // Occurs 656
    [JsonPropertyName("Men's Skiing & Snowboarding Gloves")]
    public string mensskiingsnowboardinggloves { get; set; }

    // Occurs 641
    [JsonPropertyName("Shoe Ice & Snow Grips")]
    public string shoeicesnowgrips { get; set; }

    // Occurs 640
    [JsonPropertyName("Briefcases")]
    public string briefcases { get; set; }

    // Occurs 640
    [JsonPropertyName("Men's Business Card Cases")]
    public string mensbusinesscardcases { get; set; }

    // Occurs 636
    [JsonPropertyName("Girls' Fashion")]
    public string girlsfashion { get; set; }

    // Occurs 632
    [JsonPropertyName("Men's Novelty T-Shirts")]
    public string mensnoveltytshirts { get; set; }

    // Occurs 627
    [JsonPropertyName("Men's Bowling Shoes")]
    public string mensbowlingshoes { get; set; }

    // Occurs 605
    [JsonPropertyName("Men's Costume Masks")]
    public string menscostumemasks { get; set; }

    // Occurs 594
    [JsonPropertyName("Loose Gemstones")]
    public string loosegemstones { get; set; }

    // Occurs 590
    [JsonPropertyName("Men's Costumes")]
    public string menscostumes { get; set; }

    // Occurs 587
    [JsonPropertyName("Women's Costumes")]
    public string womenscostumes { get; set; }

    // Occurs 579
    [JsonPropertyName("Garment Bags")]
    public string garmentbags { get; set; }

    // Occurs 576
    [JsonPropertyName("Men's Shops")]
    public string mensshops { get; set; }

    // Occurs 571
    [JsonPropertyName("Luggage & Travel Gear")]
    public string luggagetravelgear { get; set; }

    // Occurs 563
    [JsonPropertyName("Women's Cold Weather Gloves")]
    public string womenscoldweathergloves { get; set; }

    // Occurs 538
    [JsonPropertyName("Men's Rings")]
    public string mensrings { get; set; }

    // Occurs 538
    [JsonPropertyName("Men's Necklaces")]
    public string mensnecklaces { get; set; }

    // Occurs 535
    [JsonPropertyName("Men's Wrist Watches")]
    public string menswristwatches { get; set; }

    // Occurs 534
    [JsonPropertyName("Boys' Costume Accessories")]
    public string boyscostumeaccessories { get; set; }

    // Occurs 529
    [JsonPropertyName("Men's Military & Tactical Boots")]
    public string mensmilitarytacticalboots { get; set; }

    // Occurs 523
    [JsonPropertyName("Women's Novelty Buttons & Pins")]
    public string womensnoveltybuttonspins { get; set; }

    // Occurs 513
    [JsonPropertyName("Women's Shops")]
    public string womensshops { get; set; }

    // Occurs 511
    [JsonPropertyName("Men's Card Cases")]
    public string menscardcases { get; set; }

    // Occurs 509
    [JsonPropertyName("Women's Accessories")]
    public string womensaccessories { get; set; }

    // Occurs 500
    [JsonPropertyName("Women's Statement Rings")]
    public string womensstatementrings { get; set; }

    // Occurs 499
    [JsonPropertyName("Travel Wallets")]
    public string travelwallets { get; set; }

    // Occurs 497
    [JsonPropertyName("Women's Bowling Shoes")]
    public string womensbowlingshoes { get; set; }

    // Occurs 482
    [JsonPropertyName("Women's Costume Masks")]
    public string womenscostumemasks { get; set; }

    // Occurs 480
    [JsonPropertyName("Girls' Costume Accessories")]
    public string girlscostumeaccessories { get; set; }

    // Occurs 478
    [JsonPropertyName("Kids' Costumes")]
    public string kidscostumes { get; set; }

    // Occurs 476
    [JsonPropertyName("Jewelry Boxes & Organizers")]
    public string jewelryboxesorganizers { get; set; }

    // Occurs 469
    [JsonPropertyName("Boys' Fashion")]
    public string boysfashion { get; set; }

    // Occurs 459
    [JsonPropertyName("Jewelry Rolls")]
    public string jewelryrolls { get; set; }

    // Occurs 451
    [JsonPropertyName("Men's Baseball Caps")]
    public string mensbaseballcaps { get; set; }

    // Occurs 438
    [JsonPropertyName("Women's Wristlet Handbags")]
    public string womenswristlethandbags { get; set; }

    // Occurs 433
    [JsonPropertyName("Shoe Bags")]
    public string shoebags { get; set; }

    // Occurs 430
    [JsonPropertyName("Women's Flat Sandals")]
    public string womensflatsandals { get; set; }

    // Occurs 426
    [JsonPropertyName("Men's Accessories")]
    public string mensaccessories { get; set; }

    // Occurs 421
    [JsonPropertyName("Men's Activewear T-Shirts")]
    public string mensactiveweartshirts { get; set; }

    // Occurs 407
    [JsonPropertyName("Men's Balaclavas")]
    public string mensbalaclavas { get; set; }

    // Occurs 400
    [JsonPropertyName("Women's Choker Necklaces")]
    public string womenschokernecklaces { get; set; }

    // Occurs 391
    [JsonPropertyName("Women's Sunglasses")]
    public string womenssunglasses { get; set; }

    // Occurs 390
    [JsonPropertyName("Girls' Novelty Wallets")]
    public string girlsnoveltywallets { get; set; }

    // Occurs 388
    [JsonPropertyName("Women's Satchel Handbags")]
    public string womenssatchelhandbags { get; set; }

    // Occurs 386
    [JsonPropertyName("Women's Fashion Sneakers")]
    public string womensfashionsneakers { get; set; }

    // Occurs 378
    [JsonPropertyName("Women's Skiing & Snowboarding Gloves")]
    public string womensskiingsnowboardinggloves { get; set; }

    // Occurs 373
    [JsonPropertyName("Women's Stud Earrings")]
    public string womensstudearrings { get; set; }

    // Occurs 369
    [JsonPropertyName("Men's Cycling Shoes")]
    public string menscyclingshoes { get; set; }

    // Occurs 367
    [JsonPropertyName("Men's Athletic Socks")]
    public string mensathleticsocks { get; set; }

    // Occurs 365
    [JsonPropertyName("Women's Headbands")]
    public string womensheadbands { get; set; }

    // Occurs 353
    [JsonPropertyName("Men's Watch Bands")]
    public string menswatchbands { get; set; }

    // Occurs 347
    [JsonPropertyName("Women's Ankle Boots & Booties")]
    public string womensanklebootsbooties { get; set; }

    // Occurs 344
    [JsonPropertyName("Men's Costume Headwear")]
    public string menscostumeheadwear { get; set; }

    // Occurs 340
    [JsonPropertyName("Shoe Decoration Charms")]
    public string shoedecorationcharms { get; set; }

    // Occurs 339
    [JsonPropertyName("Women's Water Shoes")]
    public string womenswatershoes { get; set; }

    // Occurs 337
    [JsonPropertyName("Travel Accessories")]
    public string travelaccessories { get; set; }

    // Occurs 334
    [JsonPropertyName("Women's Checkbook Covers")]
    public string womenscheckbookcovers { get; set; }

    // Occurs 331
    [JsonPropertyName("Men's Sunglasses")]
    public string menssunglasses { get; set; }

    // Occurs 327
    [JsonPropertyName("Women's Stretch Bracelets")]
    public string womensstretchbracelets { get; set; }

    // Occurs 322
    [JsonPropertyName("Shoe Brushes")]
    public string shoebrushes { get; set; }

    // Occurs 321
    [JsonPropertyName("Women's Jewelry Sets")]
    public string womensjewelrysets { get; set; }

    // Occurs 315
    [JsonPropertyName("Women's Handbag Hangers")]
    public string womenshandbaghangers { get; set; }

    // Occurs 314
    [JsonPropertyName("Men's Casual Button-Down Shirts")]
    public string menscasualbuttondownshirts { get; set; }

    // Occurs 312
    [JsonPropertyName("Men's Money Clips")]
    public string mensmoneyclips { get; set; }

    // Occurs 310
    [JsonPropertyName("Women's Wrist Watches")]
    public string womenswristwatches { get; set; }

    // Occurs 306
    [JsonPropertyName("Rain Umbrellas")]
    public string rainumbrellas { get; set; }

    // Occurs 306
    [JsonPropertyName("Women's Athletic Socks")]
    public string womensathleticsocks { get; set; }

    // Occurs 301
    [JsonPropertyName("Men's T-Shirts")]
    public string menstshirts { get; set; }

    // Occurs 298
    [JsonPropertyName("Luggage Locks")]
    public string luggagelocks { get; set; }

    // Occurs 297
    [JsonPropertyName("Men's Industrial & Construction Boots")]
    public string mensindustrialconstructionboots { get; set; }

    // Occurs 287
    [JsonPropertyName("Women's Bangle Bracelets")]
    public string womensbanglebracelets { get; set; }

    // Occurs 286
    [JsonPropertyName("Women's Slippers")]
    public string womensslippers { get; set; }

    // Occurs 281
    [JsonPropertyName("Beauty & Personal Care")]
    public string beautypersonalcare { get; set; }

    // Occurs 280
    [JsonPropertyName("Shoelaces")]
    public string shoelaces { get; set; }

    // Occurs 277
    [JsonPropertyName("Men's Athletic Shirts & Tees")]
    public string mensathleticshirtstees { get; set; }

    // Occurs 273
    [JsonPropertyName("Men's Fashion Sneakers")]
    public string mensfashionsneakers { get; set; }

    // Occurs 264
    [JsonPropertyName("Women's Slide Sandals")]
    public string womensslidesandals { get; set; }

    // Occurs 262
    [JsonPropertyName("Women's Road Running Shoes")]
    public string womensroadrunningshoes { get; set; }

    // Occurs 261
    [JsonPropertyName("Men's Replacement Sunglass Lenses")]
    public string mensreplacementsunglasslenses { get; set; }

    // Occurs 259
    [JsonPropertyName("Women's Athletic Shirts & Tees")]
    public string womensathleticshirtstees { get; set; }

    // Occurs 258
    [JsonPropertyName("Men's Road Running Shoes")]
    public string mensroadrunningshoes { get; set; }

    // Occurs 256
    [JsonPropertyName("Girls' Pendants")]
    public string girlspendants { get; set; }

    // Occurs 254
    [JsonPropertyName("Men's Cycling Jerseys")]
    public string menscyclingjerseys { get; set; }

    // Occurs 254
    [JsonPropertyName("Kids' Luggage")]
    public string kidsluggage { get; set; }

    // Occurs 249
    [JsonPropertyName("Women's Activewear T-Shirts")]
    public string womensactiveweartshirts { get; set; }

    // Occurs 241
    [JsonPropertyName("Men's Military Clothing")]
    public string mensmilitaryclothing { get; set; }

    // Occurs 241
    [JsonPropertyName("Novelty & More")]
    public string noveltymore { get; set; }

    // Occurs 241
    [JsonPropertyName("Camping & Hiking Equipment")]
    public string campinghikingequipment { get; set; }

    // Occurs 241
    [JsonPropertyName("Men's Skullies & Beanies")]
    public string mensskulliesbeanies { get; set; }

    // Occurs 238
    [JsonPropertyName("Men's Athletic Shorts")]
    public string mensathleticshorts { get; set; }

    // Occurs 236
    [JsonPropertyName("Women's Costume Headwear")]
    public string womenscostumeheadwear { get; set; }

    // Occurs 222
    [JsonPropertyName("Boys' Skiing & Snowboarding Gloves")]
    public string boysskiingsnowboardinggloves { get; set; }

    // Occurs 221
    [JsonPropertyName("Women's Novelty T-Shirts")]
    public string womensnoveltytshirts { get; set; }

    // Occurs 221
    [JsonPropertyName("Women's Hobo Handbags")]
    public string womenshobohandbags { get; set; }

    // Occurs 221
    [JsonPropertyName("Women's Costume Wigs")]
    public string womenscostumewigs { get; set; }

    // Occurs 218
    [JsonPropertyName("Women's Chain Necklaces")]
    public string womenschainnecklaces { get; set; }

    // Occurs 216
    [JsonPropertyName("Men's Belts")]
    public string mensbelts { get; set; }

    // Occurs 216
    [JsonPropertyName("Women's Top-Handle Handbags")]
    public string womenstophandlehandbags { get; set; }

    // Occurs 214
    [JsonPropertyName("Girls' Rings")]
    public string girlsrings { get; set; }

    // Occurs 213
    [JsonPropertyName("Pre-Kindergarten Toys")]
    public string prekindergartentoys { get; set; }

    // Occurs 212
    [JsonPropertyName("Men's Novelty Wallets")]
    public string mensnoveltywallets { get; set; }

    // Occurs 209
    [JsonPropertyName("Women's Bracelets")]
    public string womensbracelets { get; set; }

    // Occurs 204
    [JsonPropertyName("Women's Yoga Leggings")]
    public string womensyogaleggings { get; set; }

    // Occurs 204
    [JsonPropertyName("Men's Hiking Boots")]
    public string menshikingboots { get; set; }

    // Occurs 204
    [JsonPropertyName("Boys' Necklaces")]
    public string boysnecklaces { get; set; }

    // Occurs 201
    [JsonPropertyName("Women's Robes")]
    public string womensrobes { get; set; }

    // Occurs 201
    [JsonPropertyName("Men's Polo Shirts")]
    public string menspoloshirts { get; set; }

    // Occurs 195
    [JsonPropertyName("Men's Clothing")]
    public string mensclothing { get; set; }

    // Occurs 195
    [JsonPropertyName("Women's Mid-Calf Boots")]
    public string womensmidcalfboots { get; set; }

    // Occurs 194
    [JsonPropertyName("Women's Hoop Earrings")]
    public string womenshoopearrings { get; set; }

    // Occurs 193
    [JsonPropertyName("Men's Novelty Hoodies")]
    public string mensnoveltyhoodies { get; set; }

    // Occurs 193
    [JsonPropertyName("Shoe Care & Accessories")]
    public string shoecareaccessories { get; set; }

    // Occurs 192
    [JsonPropertyName("Women's Hats & Caps")]
    public string womenshatscaps { get; set; }

    // Occurs 190
    [JsonPropertyName("Men's Running Gloves")]
    public string mensrunninggloves { get; set; }

    // Occurs 187
    [JsonPropertyName("Boys' Costume Masks")]
    public string boyscostumemasks { get; set; }

    // Occurs 187
    [JsonPropertyName("Arts, Crafts & Sewing")]
    public string artscraftssewing { get; set; }

    // Occurs 186
    [JsonPropertyName("Blouses & Button-Down Shirts")]
    public string blousesbuttondownshirts { get; set; }

    // Occurs 186
    [JsonPropertyName("Women's Anklets")]
    public string womensanklets { get; set; }

    // Occurs 184
    [JsonPropertyName("Women's T-Shirts")]
    public string womenstshirts { get; set; }

    // Occurs 184
    [JsonPropertyName("Boys' Bracelets")]
    public string boysbracelets { get; set; }

    // Occurs 184
    [JsonPropertyName("Jewelry Cleaning & Care Products")]
    public string jewelrycleaningcareproducts { get; set; }

    // Occurs 183
    [JsonPropertyName("Preschool Dress Up & Role Play")]
    public string preschooldressuproleplay { get; set; }

    // Occurs 182
    [JsonPropertyName("Women's Flip-Flops")]
    public string womensflipflops { get; set; }

    // Occurs 180
    [JsonPropertyName("Men's Jewelry")]
    public string mensjewelry { get; set; }

    // Occurs 178
    [JsonPropertyName("Women's Platform & Wedge Sandals")]
    public string womensplatformwedgesandals { get; set; }

    // Occurs 175
    [JsonPropertyName("Everyday Bras")]
    public string everydaybras { get; set; }

    // Occurs 174
    [JsonPropertyName("Women's Watch Bands")]
    public string womenswatchbands { get; set; }

    // Occurs 174
    [JsonPropertyName("Men's Costume Accessories")]
    public string menscostumeaccessories { get; set; }

    // Occurs 174
    [JsonPropertyName("Men's Sandals")]
    public string menssandals { get; set; }

    // Occurs 174
    [JsonPropertyName("Women's Stacking Rings")]
    public string womensstackingrings { get; set; }

    // Occurs 174
    [JsonPropertyName("Women's Costume Accessories")]
    public string womenscostumeaccessories { get; set; }

    // Occurs 173
    [JsonPropertyName("Women's Clothing")]
    public string womensclothing { get; set; }

    // Occurs 172
    [JsonPropertyName("Women's Ear Cuffs & Wraps")]
    public string womensearcuffswraps { get; set; }

    // Occurs 171
    [JsonPropertyName("Women's Walking Shoes")]
    public string womenswalkingshoes { get; set; }

    // Occurs 171
    [JsonPropertyName("Women's Fashion Scarves")]
    public string womensfashionscarves { get; set; }

    // Occurs 170
    [JsonPropertyName("Women's Clutch Handbags")]
    public string womensclutchhandbags { get; set; }

    // Occurs 169
    [JsonPropertyName("Women's Link Bracelets")]
    public string womenslinkbracelets { get; set; }

    // Occurs 169
    [JsonPropertyName("Women's Necklaces")]
    public string womensnecklaces { get; set; }

    // Occurs 167
    [JsonPropertyName("Women's Band Rings")]
    public string womensbandrings { get; set; }

    // Occurs 167
    [JsonPropertyName("Men's Hats & Caps")]
    public string menshatscaps { get; set; }

    // Occurs 167
    [JsonPropertyName("Our Brands")]
    public string ourbrands { get; set; }

    // Occurs 165
    [JsonPropertyName("Costume Facial Hair")]
    public string costumefacialhair { get; set; }

    // Occurs 162
    [JsonPropertyName("Women's Rain Footwear")]
    public string womensrainfootwear { get; set; }

    // Occurs 159
    [JsonPropertyName("Women's Snow Boots")]
    public string womenssnowboots { get; set; }

    // Occurs 157
    [JsonPropertyName("Watch Winders")]
    public string watchwinders { get; set; }

    // Occurs 157
    [JsonPropertyName("Women's Sport Headbands")]
    public string womenssportheadbands { get; set; }

    // Occurs 156
    [JsonPropertyName("Baby")]
    public string baby { get; set; }

    // Occurs 156
    [JsonPropertyName("Cycling Clothing")]
    public string cyclingclothing { get; set; }

    // Occurs 155
    [JsonPropertyName("Men's Sport Headbands")]
    public string menssportheadbands { get; set; }

    // Occurs 154
    [JsonPropertyName("Women's Skullies & Beanies")]
    public string womensskulliesbeanies { get; set; }

    // Occurs 154
    [JsonPropertyName("Women's Fashion Headbands")]
    public string womensfashionheadbands { get; set; }

    // Occurs 154
    [JsonPropertyName("Girls' Charm Bracelets")]
    public string girlscharmbracelets { get; set; }

    // Occurs 154
    [JsonPropertyName("Women's Yoga Pants")]
    public string womensyogapants { get; set; }

    // Occurs 154
    [JsonPropertyName("Women's Balaclavas")]
    public string womensbalaclavas { get; set; }

    // Occurs 153
    [JsonPropertyName("Shoe & Boot Trees")]
    public string shoeboottrees { get; set; }

    // Occurs 152
    [JsonPropertyName("Shoe Horns & Boot Jacks")]
    public string shoehornsbootjacks { get; set; }

    // Occurs 152
    [JsonPropertyName("Men's Hunting Shoes")]
    public string menshuntingshoes { get; set; }

    // Occurs 150
    [JsonPropertyName("Men's Work & Utility Shoes")]
    public string mensworkutilityshoes { get; set; }

    // Occurs 149
    [JsonPropertyName("Women's Business Card Cases")]
    public string womensbusinesscardcases { get; set; }

    // Occurs 148
    [JsonPropertyName("Men's Earrings")]
    public string mensearrings { get; set; }

    // Occurs 146
    [JsonPropertyName("Women's Handbags, Purses & Wallets")]
    public string womenshandbagspurseswallets { get; set; }

    // Occurs 146
    [JsonPropertyName("Women's Casual Dresses")]
    public string womenscasualdresses { get; set; }

    // Occurs 145
    [JsonPropertyName("Men's Coin Purses & Pouches")]
    public string menscoinpursespouches { get; set; }

    // Occurs 145
    [JsonPropertyName("Men's Military Shirts")]
    public string mensmilitaryshirts { get; set; }

    // Occurs 145
    [JsonPropertyName("Men's Tennis & Racquet Sport Shoes")]
    public string menstennisracquetsportshoes { get; set; }

    // Occurs 144
    [JsonPropertyName("Women's Military Clothing")]
    public string womensmilitaryclothing { get; set; }

    // Occurs 144
    [JsonPropertyName("Home & Kitchen")]
    public string homekitchen { get; set; }

    // Occurs 143
    [JsonPropertyName("Women's Wedding Bands")]
    public string womensweddingbands { get; set; }

    // Occurs 142
    [JsonPropertyName("Shoe Polishes")]
    public string shoepolishes { get; set; }

    // Occurs 140
    [JsonPropertyName("Women's Costume Accessory Sets")]
    public string womenscostumeaccessorysets { get; set; }

    // Occurs 140
    [JsonPropertyName("Women's Jewelry Charms")]
    public string womensjewelrycharms { get; set; }

    // Occurs 139
    [JsonPropertyName("Women's Wallets, Card Cases & Money Organizers")]
    public string womenswalletscardcasesmoneyorganizers { get; set; }

    // Occurs 137
    [JsonPropertyName("Costumes & Accessories")]
    public string costumesaccessories { get; set; }

    // Occurs 136
    [JsonPropertyName("Men's Shoulder Bags")]
    public string mensshoulderbags { get; set; }

    // Occurs 136
    [JsonPropertyName("Men's Rain Boots")]
    public string mensrainboots { get; set; }

    // Occurs 136
    [JsonPropertyName("Men's Eyeglass Cases")]
    public string menseyeglasscases { get; set; }

    // Occurs 136
    [JsonPropertyName("Women's Athletic Shorts")]
    public string womensathleticshorts { get; set; }

    // Occurs 135
    [JsonPropertyName("Kids' Dress-Up Accessories")]
    public string kidsdressupaccessories { get; set; }

    // Occurs 135
    [JsonPropertyName("Boys' Novelty Keychains")]
    public string boysnoveltykeychains { get; set; }

    // Occurs 134
    [JsonPropertyName("Jewelry Accessories")]
    public string jewelryaccessories { get; set; }

    // Occurs 133
    [JsonPropertyName("Men's Cross-Body Sling Bags")]
    public string menscrossbodyslingbags { get; set; }

    // Occurs 131
    [JsonPropertyName("Men's Walking Shoes")]
    public string menswalkingshoes { get; set; }

    // Occurs 131
    [JsonPropertyName("Men's Cold Weather Neck Gaiters")]
    public string menscoldweatherneckgaiters { get; set; }

    // Occurs 131
    [JsonPropertyName("Men's Casual Pants")]
    public string menscasualpants { get; set; }

    // Occurs 130
    [JsonPropertyName("Men's Water Shoes")]
    public string menswatershoes { get; set; }

    // Occurs 128
    [JsonPropertyName("Men's Work Utility & Safety Pants")]
    public string mensworkutilitysafetypants { get; set; }

    // Occurs 127
    [JsonPropertyName("Women's Shoes")]
    public string womensshoes { get; set; }

    // Occurs 127
    [JsonPropertyName("Women's Earrings")]
    public string womensearrings { get; set; }

    // Occurs 127
    [JsonPropertyName("Men's Trail Running Shoes")]
    public string menstrailrunningshoes { get; set; }

    // Occurs 127
    [JsonPropertyName("Men's Novelty Baseball Caps")]
    public string mensnoveltybaseballcaps { get; set; }

    // Occurs 126
    [JsonPropertyName("Women's Mules & Clogs")]
    public string womensmulesclogs { get; set; }

    // Occurs 125
    [JsonPropertyName("Women's Loafers & Slip-Ons")]
    public string womensloafersslipons { get; set; }

    // Occurs 125
    [JsonPropertyName("Men's Novelty Gloves")]
    public string mensnoveltygloves { get; set; }

    // Occurs 125
    [JsonPropertyName("Boys' Novelty Wallets")]
    public string boysnoveltywallets { get; set; }

    // Occurs 124
    [JsonPropertyName("Women's Leggings")]
    public string womensleggings { get; set; }

    // Occurs 123
    [JsonPropertyName("Women's Hiking Pants")]
    public string womenshikingpants { get; set; }

    // Occurs 123
    [JsonPropertyName("Men's Golf Shirts")]
    public string mensgolfshirts { get; set; }

    // Occurs 123
    [JsonPropertyName("Women's Trail Running Shoes")]
    public string womenstrailrunningshoes { get; set; }

    // Occurs 123
    [JsonPropertyName("Women's Fashion Hoodies & Sweatshirts")]
    public string womensfashionhoodiessweatshirts { get; set; }

    // Occurs 122
    [JsonPropertyName("Shoe Dryers")]
    public string shoedryers { get; set; }

    // Occurs 122
    [JsonPropertyName("Baby Boys' Bodysuits")]
    public string babyboysbodysuits { get; set; }

    // Occurs 121
    [JsonPropertyName("Women's Athletic One-Piece Swimsuits")]
    public string womensathleticonepieceswimsuits { get; set; }

    // Occurs 119
    [JsonPropertyName("Men's Skiing Jackets")]
    public string mensskiingjackets { get; set; }

    // Occurs 119
    [JsonPropertyName("Baby Girls' Bodysuits")]
    public string babygirlsbodysuits { get; set; }

    // Occurs 119
    [JsonPropertyName("Girls' Stud Earrings")]
    public string girlsstudearrings { get; set; }

    // Occurs 118
    [JsonPropertyName("Boys' Cold Weather Gloves")]
    public string boyscoldweathergloves { get; set; }

    // Occurs 117
    [JsonPropertyName("Men's Link Bracelets")]
    public string menslinkbracelets { get; set; }

    // Occurs 117
    [JsonPropertyName("Women's Clasp-Style Charms")]
    public string womensclaspstylecharms { get; set; }

    // Occurs 116
    [JsonPropertyName("Men's Slippers")]
    public string mensslippers { get; set; }

    // Occurs 115
    [JsonPropertyName("Women's Sports Bras")]
    public string womenssportsbras { get; set; }

    // Occurs 114
    [JsonPropertyName("Gym Tote Bags")]
    public string gymtotebags { get; set; }

    // Occurs 114
    [JsonPropertyName("Women's Athletic Pants")]
    public string womensathleticpants { get; set; }

    // Occurs 113
    [JsonPropertyName("Unique Finds")]
    public string uniquefinds { get; set; }

    // Occurs 113
    [JsonPropertyName("Women's Activewear Tank Tops")]
    public string womensactiveweartanktops { get; set; }

    // Occurs 112
    [JsonPropertyName("Men's Baseball & Softball Shoes")]
    public string mensbaseballsoftballshoes { get; set; }

    // Occurs 109
    [JsonPropertyName("Men's Fashion Hoodies & Sweatshirts")]
    public string mensfashionhoodiessweatshirts { get; set; }

    // Occurs 109
    [JsonPropertyName("Automotive")]
    public string automotive { get; set; }

    // Occurs 109
    [JsonPropertyName("Men's Outerwear Vests")]
    public string mensouterwearvests { get; set; }

    // Occurs 109
    [JsonPropertyName("Girls' Brooches")]
    public string girlsbrooches { get; set; }

    // Occurs 109
    [JsonPropertyName("Luggage Scales")]
    public string luggagescales { get; set; }

    // Occurs 108
    [JsonPropertyName("Men's Athletic Hoodies")]
    public string mensathletichoodies { get; set; }

    // Occurs 108
    [JsonPropertyName("Women's Belts")]
    public string womensbelts { get; set; }

    // Occurs 108
    [JsonPropertyName("Women's Italian Style Charms")]
    public string womensitalianstylecharms { get; set; }

    // Occurs 108
    [JsonPropertyName("Men's Novelty Belt Buckles")]
    public string mensnoveltybeltbuckles { get; set; }

    // Occurs 108
    [JsonPropertyName("Women's Rings")]
    public string womensrings { get; set; }

    // Occurs 108
    [JsonPropertyName("Men's Base Layers")]
    public string mensbaselayers { get; set; }

    // Occurs 108
    [JsonPropertyName("Women's Activewear")]
    public string womensactivewear { get; set; }

    // Occurs 107
    [JsonPropertyName("Men's Athletic & Outdoor Sandals & Slides")]
    public string mensathleticoutdoorsandalsslides { get; set; }

    // Occurs 107
    [JsonPropertyName("Women's Novelty Wallets")]
    public string womensnoveltywallets { get; set; }

    // Occurs 106
    [JsonPropertyName("Women's Jewelry")]
    public string womensjewelry { get; set; }

    // Occurs 106
    [JsonPropertyName("Men's Shoes")]
    public string mensshoes { get; set; }

    // Occurs 104
    [JsonPropertyName("Women's Flats")]
    public string womensflats { get; set; }

    // Occurs 103
    [JsonPropertyName("Women's Baseball Caps")]
    public string womensbaseballcaps { get; set; }

    // Occurs 102
    [JsonPropertyName("Women's Eyeglass Chains")]
    public string womenseyeglasschains { get; set; }

    // Occurs 102
    [JsonPropertyName("Sports Fan Shop")]
    public string sportsfanshop { get; set; }

    // Occurs 102
    [JsonPropertyName("Men's Novelty Accessories")]
    public string mensnoveltyaccessories { get; set; }

    // Occurs 102
    [JsonPropertyName("Women's Equestrian Sport Boots")]
    public string womensequestriansportboots { get; set; }

    // Occurs 101
    [JsonPropertyName("Men's Costume Wigs")]
    public string menscostumewigs { get; set; }

    // Occurs 100
    [JsonPropertyName("Office Products")]
    public string officeproducts { get; set; }

    // Occurs 100
    [JsonPropertyName("Baby Boys' Costumes")]
    public string babyboyscostumes { get; set; }

    // Occurs 100
    [JsonPropertyName("Boys' Cycling Gloves")]
    public string boyscyclinggloves { get; set; }

    // Occurs 99
    [JsonPropertyName("Girls' Accessories")]
    public string girlsaccessories { get; set; }

    // Occurs 99
    [JsonPropertyName("Men's Cuff Links")]
    public string menscufflinks { get; set; }

    // Occurs 99
    [JsonPropertyName("Men's Bracelets")]
    public string mensbracelets { get; set; }

    // Occurs 99
    [JsonPropertyName("Women's Strand Necklaces")]
    public string womensstrandnecklaces { get; set; }

    // Occurs 98
    [JsonPropertyName("Chef's Aprons")]
    public string chefsaprons { get; set; }

    // Occurs 98
    [JsonPropertyName("Women's Hiking Boots")]
    public string womenshikingboots { get; set; }

    // Occurs 98
    [JsonPropertyName("Women's Tanks & Camis")]
    public string womenstankscamis { get; set; }

    // Occurs 97
    [JsonPropertyName("Men's Compression Pants")]
    public string menscompressionpants { get; set; }

    // Occurs 95
    [JsonPropertyName("Shoe Care Kits")]
    public string shoecarekits { get; set; }

    // Occurs 95
    [JsonPropertyName("Men's Activewear")]
    public string mensactivewear { get; set; }

    // Occurs 95
    [JsonPropertyName("Women's Equestrian Breeches")]
    public string womensequestrianbreeches { get; set; }

    // Occurs 95
    [JsonPropertyName("Men's Loafers & Slip-Ons")]
    public string mensloafersslipons { get; set; }

    // Occurs 94
    [JsonPropertyName("Men's Novelty Bandanas")]
    public string mensnoveltybandanas { get; set; }

    // Occurs 94
    [JsonPropertyName("Women's Strand Bracelets")]
    public string womensstrandbracelets { get; set; }

    // Occurs 94
    [JsonPropertyName("Women's Pendants Only")]
    public string womenspendantsonly { get; set; }

    // Occurs 92
    [JsonPropertyName("Women's Knee-High Boots")]
    public string womenskneehighboots { get; set; }

    // Occurs 92
    [JsonPropertyName("Men's Totes")]
    public string menstotes { get; set; }

    // Occurs 92
    [JsonPropertyName("Sport Specific Clothing")]
    public string sportspecificclothing { get; set; }

    // Occurs 92
    [JsonPropertyName("Costume Makeup")]
    public string costumemakeup { get; set; }

    // Occurs 90
    [JsonPropertyName("Baby Boys' Socks")]
    public string babyboyssocks { get; set; }

    // Occurs 89
    [JsonPropertyName("Baby Girls' Hats & Caps")]
    public string babygirlshatscaps { get; set; }

    // Occurs 89
    [JsonPropertyName("Hiking Daypacks")]
    public string hikingdaypacks { get; set; }

    // Occurs 89
    [JsonPropertyName("Men's Active & Performance Shell Jackets")]
    public string mensactiveperformanceshelljackets { get; set; }

    // Occurs 88
    [JsonPropertyName("Cloth Diapers")]
    public string clothdiapers { get; set; }

    // Occurs 88
    [JsonPropertyName("Civil Service Uniforms")]
    public string civilserviceuniforms { get; set; }

    // Occurs 85
    [JsonPropertyName("Men's Cuff Bracelets")]
    public string menscuffbracelets { get; set; }

    // Occurs 85
    [JsonPropertyName("Sunglasses")]
    public string sunglasses { get; set; }

    // Occurs 85
    [JsonPropertyName("Women's Cycling Jerseys")]
    public string womenscyclingjerseys { get; set; }

    // Occurs 85
    [JsonPropertyName("Women's Novelty Clothing")]
    public string womensnoveltyclothing { get; set; }

    // Occurs 84
    [JsonPropertyName("Men's Snow Boots")]
    public string menssnowboots { get; set; }

    // Occurs 84
    [JsonPropertyName("Karate Suit Sets")]
    public string karatesuitsets { get; set; }

    // Occurs 83
    [JsonPropertyName("Men's Hiking Pants")]
    public string menshikingpants { get; set; }

    // Occurs 83
    [JsonPropertyName("Women's Cuff Bracelets")]
    public string womenscuffbracelets { get; set; }

    // Occurs 82
    [JsonPropertyName("Men's Novelty Socks")]
    public string mensnoveltysocks { get; set; }

    // Occurs 82
    [JsonPropertyName("Men's Handkerchiefs")]
    public string menshandkerchiefs { get; set; }

    // Occurs 81
    [JsonPropertyName("Girls' Hats & Caps")]
    public string girlshatscaps { get; set; }

    // Occurs 80
    [JsonPropertyName("Women's Cold Weather Mittens")]
    public string womenscoldweathermittens { get; set; }

    // Occurs 80
    [JsonPropertyName("Women's Activewear Leggings")]
    public string womensactivewearleggings { get; set; }

    // Occurs 80
    [JsonPropertyName("Women's Clip-On Earrings")]
    public string womenscliponearrings { get; set; }

    // Occurs 79
    [JsonPropertyName("Tennis Footwear")]
    public string tennisfootwear { get; set; }

    // Occurs 79
    [JsonPropertyName("Girls' Skiing & Snowboarding Gloves")]
    public string girlsskiingsnowboardinggloves { get; set; }

    // Occurs 79
    [JsonPropertyName("Baby Girls' Swim Diapers")]
    public string babygirlsswimdiapers { get; set; }

    // Occurs 79
    [JsonPropertyName("Women's Pullover Sweaters")]
    public string womenspulloversweaters { get; set; }

    // Occurs 78
    [JsonPropertyName("Health & Household")]
    public string healthhousehold { get; set; }

    // Occurs 78
    [JsonPropertyName("Men's Golf Shoes")]
    public string mensgolfshoes { get; set; }

    // Occurs 78
    [JsonPropertyName("Men's Hiking Shoes")]
    public string menshikingshoes { get; set; }

    // Occurs 78
    [JsonPropertyName("Women's Novelty Accessories")]
    public string womensnoveltyaccessories { get; set; }

    // Occurs 77
    [JsonPropertyName("Women's Evening Handbags")]
    public string womenseveninghandbags { get; set; }

    // Occurs 77
    [JsonPropertyName("Jewelry Chests")]
    public string jewelrychests { get; set; }

    // Occurs 77
    [JsonPropertyName("Ski Clothing")]
    public string skiclothing { get; set; }

    // Occurs 77
    [JsonPropertyName("Women's Boots")]
    public string womensboots { get; set; }

    // Occurs 76
    [JsonPropertyName("Women's Running Gloves")]
    public string womensrunninggloves { get; set; }

    // Occurs 76
    [JsonPropertyName("Women's Athletic Base Layers")]
    public string womensathleticbaselayers { get; set; }

    // Occurs 76
    [JsonPropertyName("Jiu-Jitsu Suit Sets")]
    public string jiujitsusuitsets { get; set; }

    // Occurs 76
    [JsonPropertyName("Women's Running Socks")]
    public string womensrunningsocks { get; set; }

    // Occurs 75
    [JsonPropertyName("Men's Cycling Shorts")]
    public string menscyclingshorts { get; set; }

    // Occurs 75
    [JsonPropertyName("Girls' Cold Weather Gloves")]
    public string girlscoldweathergloves { get; set; }

    // Occurs 75
    [JsonPropertyName("Women's Wraps & Pashminas")]
    public string womenswrapspashminas { get; set; }

    // Occurs 75
    [JsonPropertyName("Women's Pumps")]
    public string womenspumps { get; set; }

    // Occurs 74
    [JsonPropertyName("Men's Cross-Training Shoes")]
    public string menscrosstrainingshoes { get; set; }

    // Occurs 74
    [JsonPropertyName("Women's Cycling Shoes")]
    public string womenscyclingshoes { get; set; }

    // Occurs 74
    [JsonPropertyName("Men's Work Utility & Safety Overalls & Coveralls")]
    public string mensworkutilitysafetyoverallscoveralls { get; set; }

    // Occurs 73
    [JsonPropertyName("Women's ID Cases")]
    public string womensidcases { get; set; }

    // Occurs 73
    [JsonPropertyName("Tie Clips")]
    public string tieclips { get; set; }

    // Occurs 72
    [JsonPropertyName("Men's Athletic Supporters")]
    public string mensathleticsupporters { get; set; }

    // Occurs 72
    [JsonPropertyName("Men's Cycling Clothing")]
    public string menscyclingclothing { get; set; }

    // Occurs 72
    [JsonPropertyName("Men's Wedding Rings")]
    public string mensweddingrings { get; set; }

    // Occurs 71
    [JsonPropertyName("Women's Sun Hats")]
    public string womenssunhats { get; set; }

    // Occurs 71
    [JsonPropertyName("Men's Golf Clothing")]
    public string mensgolfclothing { get; set; }

    // Occurs 71
    [JsonPropertyName("Men's Bathrobes")]
    public string mensbathrobes { get; set; }

    // Occurs 71
    [JsonPropertyName("Men's Basketball Shoes")]
    public string mensbasketballshoes { get; set; }

    // Occurs 71
    [JsonPropertyName("Tools & Home Improvement")]
    public string toolshomeimprovement { get; set; }

    // Occurs 71
    [JsonPropertyName("Women's Rash Guard Shirts")]
    public string womensrashguardshirts { get; set; }

    // Occurs 70
    [JsonPropertyName("Women's Engagement Rings")]
    public string womensengagementrings { get; set; }

    // Occurs 70
    [JsonPropertyName("Men's Climbing Shoes")]
    public string mensclimbingshoes { get; set; }

    // Occurs 70
    [JsonPropertyName("Chef's Hats")]
    public string chefshats { get; set; }

    // Occurs 70
    [JsonPropertyName("Women's Casual Jackets")]
    public string womenscasualjackets { get; set; }

    // Occurs 69
    [JsonPropertyName("Men's Pullover Sweaters")]
    public string menspulloversweaters { get; set; }

    // Occurs 69
    [JsonPropertyName("Kitchen & Dining")]
    public string kitchendining { get; set; }

    // Occurs 69
    [JsonPropertyName("Women's Novelty Socks")]
    public string womensnoveltysocks { get; set; }

    // Occurs 69
    [JsonPropertyName("Boys' Activewear T-Shirts")]
    public string boysactiveweartshirts { get; set; }

    // Occurs 68
    [JsonPropertyName("Men's Suspenders")]
    public string menssuspenders { get; set; }

    // Occurs 68
    [JsonPropertyName("Women's Body Chains")]
    public string womensbodychains { get; set; }

    // Occurs 67
    [JsonPropertyName("Women's Lockets")]
    public string womenslockets { get; set; }

    // Occurs 67
    [JsonPropertyName("Women's Tunics")]
    public string womenstunics { get; set; }

    // Occurs 67
    [JsonPropertyName("Men's Chukka Boots")]
    public string menschukkaboots { get; set; }

    // Occurs 67
    [JsonPropertyName("Men's Running Socks")]
    public string mensrunningsocks { get; set; }

    // Occurs 66
    [JsonPropertyName("Kids' Costume Masks")]
    public string kidscostumemasks { get; set; }

    // Occurs 66
    [JsonPropertyName("Children's Jewelry Boxes")]
    public string childrensjewelryboxes { get; set; }

    // Occurs 66
    [JsonPropertyName("Women's Baseball & Softball Shoes")]
    public string womensbaseballsoftballshoes { get; set; }

    // Occurs 66
    [JsonPropertyName("Girls' Costume Masks")]
    public string girlscostumemasks { get; set; }

    // Occurs 66
    [JsonPropertyName("Girls' Bracelets")]
    public string girlsbracelets { get; set; }

    // Occurs 66
    [JsonPropertyName("Sports Fan T-Shirts")]
    public string sportsfantshirts { get; set; }

    // Occurs 66
    [JsonPropertyName("Men's Cold Weather Mittens")]
    public string menscoldweathermittens { get; set; }

    // Occurs 65
    [JsonPropertyName("Costume Feather Boas")]
    public string costumefeatherboas { get; set; }

    // Occurs 65
    [JsonPropertyName("Women's Jumpsuits")]
    public string womensjumpsuits { get; set; }

    // Occurs 65
    [JsonPropertyName("Women's Cardigans")]
    public string womenscardigans { get; set; }

    // Occurs 64
    [JsonPropertyName("Running Clothing")]
    public string runningclothing { get; set; }

    // Occurs 63
    [JsonPropertyName("Women's Body Piercing Rings")]
    public string womensbodypiercingrings { get; set; }

    // Occurs 63
    [JsonPropertyName("Men's Athletic Pants")]
    public string mensathleticpants { get; set; }

    // Occurs 63
    [JsonPropertyName("Men's Golf Belts")]
    public string mensgolfbelts { get; set; }

    // Occurs 63
    [JsonPropertyName("Baby Girls' Gloves & Mittens")]
    public string babygirlsglovesmittens { get; set; }

    // Occurs 63
    [JsonPropertyName("Women's Eyeglass Cases")]
    public string womenseyeglasscases { get; set; }

    // Occurs 62
    [JsonPropertyName("Girls' Jewelry")]
    public string girlsjewelry { get; set; }

    // Occurs 62
    [JsonPropertyName("Men's Pocket Watches")]
    public string menspocketwatches { get; set; }

    // Occurs 62
    [JsonPropertyName("Men's Work Utility & Safety Outerwear")]
    public string mensworkutilitysafetyouterwear { get; set; }

    // Occurs 62
    [JsonPropertyName("Baby Boys' Hats & Caps")]
    public string babyboyshatscaps { get; set; }

    // Occurs 62
    [JsonPropertyName("Baby Boys' Gloves & Mittens")]
    public string babyboysglovesmittens { get; set; }

    // Occurs 61
    [JsonPropertyName("Women's Cold Weather Scarves & Wraps")]
    public string womenscoldweatherscarveswraps { get; set; }

    // Occurs 61
    [JsonPropertyName("Baby Boys' Swim Diapers")]
    public string babyboysswimdiapers { get; set; }

    // Occurs 61
    [JsonPropertyName("Men's Sun Hats")]
    public string menssunhats { get; set; }

    // Occurs 61
    [JsonPropertyName("Men's Compression Shirts")]
    public string menscompressionshirts { get; set; }

    // Occurs 60
    [JsonPropertyName("Men's Fleece Jackets & Coats")]
    public string mensfleecejacketscoats { get; set; }

    // Occurs 60
    [JsonPropertyName("Women's Military & Tactical Boots")]
    public string womensmilitarytacticalboots { get; set; }

    // Occurs 60
    [JsonPropertyName("Women's Tennis & Racquet Sport Shoes")]
    public string womenstennisracquetsportshoes { get; set; }

    // Occurs 60
    [JsonPropertyName("Women's Fleece Jackets & Coats")]
    public string womensfleecejacketscoats { get; set; }

    // Occurs 59
    [JsonPropertyName("Men's Oxfords")]
    public string mensoxfords { get; set; }

    // Occurs 59
    [JsonPropertyName("Unique Clothing, Shoes & Jewelry")]
    public string uniqueclothingshoesjewelry { get; set; }

    // Occurs 59
    [JsonPropertyName("Men's Gloves & Mittens")]
    public string mensglovesmittens { get; set; }

    // Occurs 58
    [JsonPropertyName("Luggage Handle Wraps")]
    public string luggagehandlewraps { get; set; }

    // Occurs 58
    [JsonPropertyName("Men's Bow Ties")]
    public string mensbowties { get; set; }

    // Occurs 57
    [JsonPropertyName("Men's Boots")]
    public string mensboots { get; set; }

    // Occurs 57
    [JsonPropertyName("Women's Socks")]
    public string womenssocks { get; set; }

    // Occurs 57
    [JsonPropertyName("Men's Cycling Tights & Pants")]
    public string menscyclingtightspants { get; set; }

    // Occurs 57
    [JsonPropertyName("Socks for Men")]
    public string socksformen { get; set; }

    // Occurs 57
    [JsonPropertyName("Women's Equestrian Tournament Blouses")]
    public string womensequestriantournamentblouses { get; set; }

    // Occurs 56
    [JsonPropertyName("Costume Props")]
    public string costumeprops { get; set; }

    // Occurs 56
    [JsonPropertyName("Men's Costume Accessory Sets")]
    public string menscostumeaccessorysets { get; set; }

    // Occurs 56
    [JsonPropertyName("Women's Wrap Bracelets")]
    public string womenswrapbracelets { get; set; }

    // Occurs 56
    [JsonPropertyName("Women's Work & Utility Shoes")]
    public string womensworkutilityshoes { get; set; }

    // Occurs 55
    [JsonPropertyName("Girls' Wrist Watches")]
    public string girlswristwatches { get; set; }

    // Occurs 55
    [JsonPropertyName("Men's Western Boots")]
    public string menswesternboots { get; set; }

    // Occurs 55
    [JsonPropertyName("Women's One-Piece Swimsuits")]
    public string womensonepieceswimsuits { get; set; }

    // Occurs 55
    [JsonPropertyName("Women's Novelty Baseball Caps")]
    public string womensnoveltybaseballcaps { get; set; }

    // Occurs 55
    [JsonPropertyName("Men's Sweatshirts")]
    public string menssweatshirts { get; set; }

    // Occurs 55
    [JsonPropertyName("Women's Yoga Socks")]
    public string womensyogasocks { get; set; }

    // Occurs 55
    [JsonPropertyName("Men's Track Jackets")]
    public string menstrackjackets { get; set; }

    // Occurs 54
    [JsonPropertyName("Women's Sweatpants")]
    public string womenssweatpants { get; set; }

    // Occurs 54
    [JsonPropertyName("Men's Motorcycle & Combat Boots")]
    public string mensmotorcyclecombatboots { get; set; }

    // Occurs 54
    [JsonPropertyName("Kids' Party Supplies")]
    public string kidspartysupplies { get; set; }

    // Occurs 54
    [JsonPropertyName("Women's Sandals")]
    public string womenssandals { get; set; }

    // Occurs 54
    [JsonPropertyName("Girls' Boots")]
    public string girlsboots { get; set; }

    // Occurs 54
    [JsonPropertyName("Women's Novelty Gloves")]
    public string womensnoveltygloves { get; set; }

    // Occurs 54
    [JsonPropertyName("Women's Novelty Hoodies")]
    public string womensnoveltyhoodies { get; set; }

    // Occurs 53
    [JsonPropertyName("Men's Sweatpants")]
    public string menssweatpants { get; set; }

    // Occurs 53
    [JsonPropertyName("Women's Heeled Sandals")]
    public string womensheeledsandals { get; set; }

    // Occurs 53
    [JsonPropertyName("Women's No Show & Liner Socks")]
    public string womensnoshowlinersocks { get; set; }

    // Occurs 53
    [JsonPropertyName("Women's Faux Body Piercing Jewelry")]
    public string womensfauxbodypiercingjewelry { get; set; }

    // Occurs 53
    [JsonPropertyName("Women's Body Piercing Barbells")]
    public string womensbodypiercingbarbells { get; set; }

    // Occurs 52
    [JsonPropertyName("Boys' Hats & Caps")]
    public string boyshatscaps { get; set; }

    // Occurs 52
    [JsonPropertyName("Lingerie Straps")]
    public string lingeriestraps { get; set; }

    // Occurs 52
    [JsonPropertyName("Boys' Accessories")]
    public string boysaccessories { get; set; }

    // Occurs 52
    [JsonPropertyName("Boys' Wrist Watches")]
    public string boyswristwatches { get; set; }

    // Occurs 52
    [JsonPropertyName("Military Clothing")]
    public string militaryclothing { get; set; }

    // Occurs 52
    [JsonPropertyName("Women's Swimwear Cover Ups")]
    public string womensswimwearcoverups { get; set; }

    // Occurs 52
    [JsonPropertyName("Girls' Novelty Swimwear")]
    public string girlsnoveltyswimwear { get; set; }

    // Occurs 52
    [JsonPropertyName("Women's Jeans")]
    public string womensjeans { get; set; }

    // Occurs 52
    [JsonPropertyName("Men's Activewear Button-Down Shirts")]
    public string mensactivewearbuttondownshirts { get; set; }

    // Occurs 51
    [JsonPropertyName("Women's Golf Shoes")]
    public string womensgolfshoes { get; set; }

    // Occurs 51
    [JsonPropertyName("Boys' Jewelry")]
    public string boysjewelry { get; set; }

    // Occurs 51
    [JsonPropertyName("Girls' Wallets")]
    public string girlswallets { get; set; }

    // Occurs 51
    [JsonPropertyName("Baby Boys' Blanket Sleepers")]
    public string babyboysblanketsleepers { get; set; }

    // Occurs 50
    [JsonPropertyName("Men's Novelty Clothing")]
    public string mensnoveltyclothing { get; set; }

    // Occurs 50
    [JsonPropertyName("Men's Soccer Shoes")]
    public string menssoccershoes { get; set; }

    // Occurs 50
    [JsonPropertyName("Men's Baseball Jerseys")]
    public string mensbaseballjerseys { get; set; }

    // Occurs 49
    [JsonPropertyName("Girls' Sunglasses")]
    public string girlssunglasses { get; set; }

    // Occurs 49
    [JsonPropertyName("Women's Cold Weather Headbands")]
    public string womenscoldweatherheadbands { get; set; }

    // Occurs 49
    [JsonPropertyName("Women's Skiing Jackets")]
    public string womensskiingjackets { get; set; }

    // Occurs 49
    [JsonPropertyName("Luggage Carts")]
    public string luggagecarts { get; set; }

    // Occurs 48
    [JsonPropertyName("Men's Running Shirts")]
    public string mensrunningshirts { get; set; }

    // Occurs 48
    [JsonPropertyName("Men's Trench & Rain Coats")]
    public string menstrenchraincoats { get; set; }

    // Occurs 48
    [JsonPropertyName("Boys' Cold Weather Mittens")]
    public string boyscoldweathermittens { get; set; }

    // Occurs 48
    [JsonPropertyName("Women's Y-Necklaces")]
    public string womensynecklaces { get; set; }

    // Occurs 48
    [JsonPropertyName("Men's Military Pants")]
    public string mensmilitarypants { get; set; }

    // Occurs 47
    [JsonPropertyName("Boys' Boots")]
    public string boysboots { get; set; }

    // Occurs 47
    [JsonPropertyName("Men's Cycling Jackets")]
    public string menscyclingjackets { get; set; }

    // Occurs 46
    [JsonPropertyName("Men's Smartwatches")]
    public string menssmartwatches { get; set; }

    // Occurs 46
    [JsonPropertyName("Women's Outerwear Vests")]
    public string womensouterwearvests { get; set; }

    // Occurs 46
    [JsonPropertyName("Baby Girls' Blanket Sleepers")]
    public string babygirlsblanketsleepers { get; set; }

    // Occurs 46
    [JsonPropertyName("Women's Yoga Clothing")]
    public string womensyogaclothing { get; set; }

    // Occurs 46
    [JsonPropertyName("Baby Boys' One-Piece Rompers")]
    public string babyboysonepiecerompers { get; set; }

    // Occurs 46
    [JsonPropertyName("Decorative Boxes")]
    public string decorativeboxes { get; set; }

    // Occurs 45
    [JsonPropertyName("Women's Running Shorts")]
    public string womensrunningshorts { get; set; }

    // Occurs 45
    [JsonPropertyName("Men's Pendants")]
    public string menspendants { get; set; }

    // Occurs 45
    [JsonPropertyName("Baby Girls' Socks")]
    public string babygirlssocks { get; set; }

    // Occurs 44
    [JsonPropertyName("Women's Compression Socks")]
    public string womenscompressionsocks { get; set; }

    // Occurs 44
    [JsonPropertyName("Men's Down Jackets & Coats")]
    public string mensdownjacketscoats { get; set; }

    // Occurs 44
    [JsonPropertyName("Hunting Equipment")]
    public string huntingequipment { get; set; }

    // Occurs 44
    [JsonPropertyName("Women's Ballet & Dance Shoes")]
    public string womensballetdanceshoes { get; set; }

    // Occurs 44
    [JsonPropertyName("Women's Running Shirts")]
    public string womensrunningshirts { get; set; }

    // Occurs 44
    [JsonPropertyName("Men's Fedoras")]
    public string mensfedoras { get; set; }

    // Occurs 44
    [JsonPropertyName("Golf Umbrellas")]
    public string golfumbrellas { get; set; }

    // Occurs 44
    [JsonPropertyName("Shoe Protective Treatments")]
    public string shoeprotectivetreatments { get; set; }

    // Occurs 44
    [JsonPropertyName("Men's Athletic Shoes")]
    public string mensathleticshoes { get; set; }

    // Occurs 44
    [JsonPropertyName("Men's Activewear Tank Tops")]
    public string mensactiveweartanktops { get; set; }

    // Occurs 43
    [JsonPropertyName("Men's Costume Eyewear")]
    public string menscostumeeyewear { get; set; }

    // Occurs 43
    [JsonPropertyName("Shoe Dyes")]
    public string shoedyes { get; set; }

    // Occurs 43
    [JsonPropertyName("Women's Base Layers & Compression")]
    public string womensbaselayerscompression { get; set; }

    // Occurs 43
    [JsonPropertyName("Women's Leg Warmers")]
    public string womenslegwarmers { get; set; }

    // Occurs 43
    [JsonPropertyName("Girls' Novelty Keychains")]
    public string girlsnoveltykeychains { get; set; }

    // Occurs 43
    [JsonPropertyName("Men's Costume Footwear")]
    public string menscostumefootwear { get; set; }

    // Occurs 43
    [JsonPropertyName("Baby Girls' Costumes")]
    public string babygirlscostumes { get; set; }

    // Occurs 43
    [JsonPropertyName("Women's Cycling Clothing")]
    public string womenscyclingclothing { get; set; }

    // Occurs 42
    [JsonPropertyName("Men's Softball Jerseys")]
    public string menssoftballjerseys { get; set; }

    // Occurs 42
    [JsonPropertyName("Men's Soccer Jerseys")]
    public string menssoccerjerseys { get; set; }

    // Occurs 42
    [JsonPropertyName("Men's Boxer Briefs")]
    public string mensboxerbriefs { get; set; }

    // Occurs 42
    [JsonPropertyName("Hair Styling Accessories")]
    public string hairstylingaccessories { get; set; }

    // Occurs 42
    [JsonPropertyName("Baby Girls' One-Piece Rompers")]
    public string babygirlsonepiecerompers { get; set; }

    // Occurs 42
    [JsonPropertyName("Men's Cycling Caps")]
    public string menscyclingcaps { get; set; }

    // Occurs 41
    [JsonPropertyName("Women's Novelty Bandanas")]
    public string womensnoveltybandanas { get; set; }

    // Occurs 41
    [JsonPropertyName("Women's Volleyball Clothing")]
    public string womensvolleyballclothing { get; set; }

    // Occurs 41
    [JsonPropertyName("Women's Hiking Shoes")]
    public string womenshikingshoes { get; set; }

    // Occurs 41
    [JsonPropertyName("Women's Pajama Sets")]
    public string womenspajamasets { get; set; }

    // Occurs 40
    [JsonPropertyName("Men's Cowboy Hats")]
    public string menscowboyhats { get; set; }

    // Occurs 40
    [JsonPropertyName("Women's Blazers & Suit Jackets")]
    public string womensblazerssuitjackets { get; set; }

    // Occurs 40
    [JsonPropertyName("Toddler Dress Up & Role Play")]
    public string toddlerdressuproleplay { get; set; }

    // Occurs 40
    [JsonPropertyName("Men's Costume Robes, Capes & Jackets")]
    public string menscostumerobescapesjackets { get; set; }

    // Occurs 40
    [JsonPropertyName("Women's Snowboarding Clothing")]
    public string womenssnowboardingclothing { get; set; }

    // Occurs 40
    [JsonPropertyName("Women's Activewear Button-Down Shirts")]
    public string womensactivewearbuttondownshirts { get; set; }

    // Occurs 40
    [JsonPropertyName("Men's Jeans")]
    public string mensjeans { get; set; }

    // Occurs 40
    [JsonPropertyName("Men's Baseball Pants")]
    public string mensbaseballpants { get; set; }

    // Occurs 39
    [JsonPropertyName("Kids' & Babies' Costumes & Accessories")]
    public string kidsbabiescostumesaccessories { get; set; }

    // Occurs 39
    [JsonPropertyName("Girls' Cold Weather Mittens")]
    public string girlscoldweathermittens { get; set; }

    // Occurs 39
    [JsonPropertyName("Men's Yoga Clothing")]
    public string mensyogaclothing { get; set; }

    // Occurs 39
    [JsonPropertyName("Men's Wallets, Card Cases & Money Organizers")]
    public string menswalletscardcasesmoneyorganizers { get; set; }

    // Occurs 39
    [JsonPropertyName("Men's Skateboarding Shoes")]
    public string mensskateboardingshoes { get; set; }

    // Occurs 39
    [JsonPropertyName("Cell Phones & Accessories")]
    public string cellphonesaccessories { get; set; }

    // Occurs 39
    [JsonPropertyName("Women's Cross Training Shoes")]
    public string womenscrosstrainingshoes { get; set; }

    // Occurs 38
    [JsonPropertyName("Baby Boy's Clothing")]
    public string babyboysclothing { get; set; }

    // Occurs 38
    [JsonPropertyName("Uniforms, Work & Safety Clothing")]
    public string uniformsworksafetyclothing { get; set; }

    // Occurs 38
    [JsonPropertyName("Women's Exotic Costumes")]
    public string womensexoticcostumes { get; set; }

    // Occurs 38
    [JsonPropertyName("Tie Pins")]
    public string tiepins { get; set; }

    // Occurs 38
    [JsonPropertyName("Men's Chef Jackets")]
    public string menschefjackets { get; set; }

    // Occurs 38
    [JsonPropertyName("Women's Novelty Applique Patches")]
    public string womensnoveltyappliquepatches { get; set; }

    // Occurs 38
    [JsonPropertyName("Men's Sports Compression Shorts")]
    public string menssportscompressionshorts { get; set; }

    // Occurs 38
    [JsonPropertyName("Kids' Dress Up & Pretend Play")]
    public string kidsdressuppretendplay { get; set; }

    // Occurs 37
    [JsonPropertyName("Bra Extenders")]
    public string braextenders { get; set; }

    // Occurs 37
    [JsonPropertyName("Baby Girls' Leg Warmers")]
    public string babygirlslegwarmers { get; set; }

    // Occurs 37
    [JsonPropertyName("Baby Girls' Layette Sets")]
    public string babygirlslayettesets { get; set; }

    // Occurs 37
    [JsonPropertyName("Women's Volleyball Shoes")]
    public string womensvolleyballshoes { get; set; }

    // Occurs 37
    [JsonPropertyName("Boys' Clothing")]
    public string boysclothing { get; set; }

    // Occurs 37
    [JsonPropertyName("Women's Dresses")]
    public string womensdresses { get; set; }

    // Occurs 36
    [JsonPropertyName("Men's Running Shorts")]
    public string mensrunningshorts { get; set; }

    // Occurs 36
    [JsonPropertyName("Women's Bikini Sets")]
    public string womensbikinisets { get; set; }

    // Occurs 36
    [JsonPropertyName("Men's Neckties")]
    public string mensneckties { get; set; }

    // Occurs 36
    [JsonPropertyName("Women's Sheers")]
    public string womenssheers { get; set; }

    // Occurs 36
    [JsonPropertyName("Baby Girls' Clothing")]
    public string babygirlsclothing { get; set; }

    // Occurs 36
    [JsonPropertyName("Girls' Sandals")]
    public string girlssandals { get; set; }

    // Occurs 36
    [JsonPropertyName("Electronics")]
    public string electronics { get; set; }

    // Occurs 35
    [JsonPropertyName("Boys' T-Shirts")]
    public string boystshirts { get; set; }

    // Occurs 35
    [JsonPropertyName("Boys' Athletic Socks")]
    public string boysathleticsocks { get; set; }

    // Occurs 35
    [JsonPropertyName("Men's Eyeglass Chains")]
    public string menseyeglasschains { get; set; }

    // Occurs 35
    [JsonPropertyName("Girls' Dance Clothing")]
    public string girlsdanceclothing { get; set; }

    // Occurs 35
    [JsonPropertyName("Sports Fan Polo Shirts")]
    public string sportsfanpoloshirts { get; set; }

    // Occurs 35
    [JsonPropertyName("Novelty Toys & Amusements")]
    public string noveltytoysamusements { get; set; }

    // Occurs 35
    [JsonPropertyName("Women's Climbing Shoes")]
    public string womensclimbingshoes { get; set; }

    // Occurs 35
    [JsonPropertyName("Women's Costume Robes, Capes & Jackets")]
    public string womenscostumerobescapesjackets { get; set; }

    // Occurs 34
    [JsonPropertyName("Men's Military Outerwear")]
    public string mensmilitaryouterwear { get; set; }

    // Occurs 34
    [JsonPropertyName("Baby Clothing & Shoes")]
    public string babyclothingshoes { get; set; }

    // Occurs 34
    [JsonPropertyName("Women's Costume Leg Warmers & Hosiery")]
    public string womenscostumelegwarmershosiery { get; set; }

    // Occurs 34
    [JsonPropertyName("Women's Golf Shirts")]
    public string womensgolfshirts { get; set; }

    // Occurs 34
    [JsonPropertyName("Girls' Bangles")]
    public string girlsbangles { get; set; }

    // Occurs 34
    [JsonPropertyName("Women's Costumes & Accessories")]
    public string womenscostumesaccessories { get; set; }

    // Occurs 34
    [JsonPropertyName("Girls' Cycling Gloves")]
    public string girlscyclinggloves { get; set; }

    // Occurs 34
    [JsonPropertyName("Women's Medical Scrub Shirts")]
    public string womensmedicalscrubshirts { get; set; }

    // Occurs 33
    [JsonPropertyName("Baby Boys' Accessories")]
    public string babyboysaccessories { get; set; }

    // Occurs 33
    [JsonPropertyName("Women's Cycling Shorts")]
    public string womenscyclingshorts { get; set; }

    // Occurs 33
    [JsonPropertyName("Men's Costumes & Accessories")]
    public string menscostumesaccessories { get; set; }

    // Occurs 33
    [JsonPropertyName("Women's Athletic Hoodies")]
    public string womensathletichoodies { get; set; }

    // Occurs 32
    [JsonPropertyName("Men's Track Pants")]
    public string menstrackpants { get; set; }

    // Occurs 32
    [JsonPropertyName("Women's Skiing Pants")]
    public string womensskiingpants { get; set; }

    // Occurs 32
    [JsonPropertyName("Girls' Costume Wigs")]
    public string girlscostumewigs { get; set; }

    // Occurs 32
    [JsonPropertyName("Boys' Wallets")]
    public string boyswallets { get; set; }

    // Occurs 32
    [JsonPropertyName("Sports Fan Jewelry & Watches")]
    public string sportsfanjewelrywatches { get; set; }

    // Occurs 31
    [JsonPropertyName("Girls' Tees")]
    public string girlstees { get; set; }

    // Occurs 31
    [JsonPropertyName("Women's Smartwatches")]
    public string womenssmartwatches { get; set; }

    // Occurs 31
    [JsonPropertyName("Kitchen Reusable Grocery Bags")]
    public string kitchenreusablegrocerybags { get; set; }

    // Occurs 31
    [JsonPropertyName("Men's Basketball Clothing")]
    public string mensbasketballclothing { get; set; }

    // Occurs 31
    [JsonPropertyName("Women's Fascinators")]
    public string womensfascinators { get; set; }

    // Occurs 31
    [JsonPropertyName("Women's Snowboarding Pants")]
    public string womenssnowboardingpants { get; set; }

    // Occurs 31
    [JsonPropertyName("Women's Novelty Tanks & Camis")]
    public string womensnoveltytankscamis { get; set; }

    // Occurs 31
    [JsonPropertyName("Women's Casual Pants & Capris")]
    public string womenscasualpantscapris { get; set; }

    // Occurs 31
    [JsonPropertyName("Men's Base Layers & Compression")]
    public string mensbaselayerscompression { get; set; }

    // Occurs 31
    [JsonPropertyName("Women’s Garters")]
    public string womensgarters { get; set; }

    // Occurs 30
    [JsonPropertyName("Women's Tights")]
    public string womenstights { get; set; }

    // Occurs 30
    [JsonPropertyName("Women's Visors")]
    public string womensvisors { get; set; }

    // Occurs 30
    [JsonPropertyName("Baby Boys' Leg Warmers")]
    public string babyboyslegwarmers { get; set; }

    // Occurs 30
    [JsonPropertyName("Women's Athletic & Outdoor Sandals & Slides")]
    public string womensathleticoutdoorsandalsslides { get; set; }

    // Occurs 30
    [JsonPropertyName("Women's Charm Bracelets")]
    public string womenscharmbracelets { get; set; }

    // Occurs 29
    [JsonPropertyName("Women's Rompers")]
    public string womensrompers { get; set; }

    // Occurs 29
    [JsonPropertyName("Women's Slipper Socks")]
    public string womensslippersocks { get; set; }

    // Occurs 29
    [JsonPropertyName("Keychains")]
    public string keychains { get; set; }

    // Occurs 29
    [JsonPropertyName("Girls' Coin Purses & Pouches")]
    public string girlscoinpursespouches { get; set; }

    // Occurs 29
    [JsonPropertyName("Baby Boys' Hoodies & Activewear")]
    public string babyboyshoodiesactivewear { get; set; }

    // Occurs 29
    [JsonPropertyName("Luggage Tags & Handle Wraps")]
    public string luggagetagshandlewraps { get; set; }

    // Occurs 29
    [JsonPropertyName("Men's Medical Lab Coats")]
    public string mensmedicallabcoats { get; set; }

    // Occurs 29
    [JsonPropertyName("Toiletry Bags")]
    public string toiletrybags { get; set; }

    // Occurs 29
    [JsonPropertyName("Women's Eyewear Frames")]
    public string womenseyewearframes { get; set; }

    // Occurs 29
    [JsonPropertyName("Hanging Jewelry Organizers")]
    public string hangingjewelryorganizers { get; set; }

    // Occurs 29
    [JsonPropertyName("Women's Hunting Boots & Shoes")]
    public string womenshuntingbootsshoes { get; set; }

    // Occurs 28
    [JsonPropertyName("Men's Underwear")]
    public string mensunderwear { get; set; }

    // Occurs 28
    [JsonPropertyName("Industrial & Scientific")]
    public string industrialscientific { get; set; }

    // Occurs 28
    [JsonPropertyName("Men's Novelty Beanies & Knit Hats")]
    public string mensnoveltybeaniesknithats { get; set; }

    // Occurs 28
    [JsonPropertyName("Girls' Necklaces")]
    public string girlsnecklaces { get; set; }

    // Occurs 28
    [JsonPropertyName("Women's Raincoats")]
    public string womensraincoats { get; set; }

    // Occurs 28
    [JsonPropertyName("Men's Tank Shirts")]
    public string menstankshirts { get; set; }

    // Occurs 28
    [JsonPropertyName("Smartwatches")]
    public string smartwatches { get; set; }

    // Occurs 28
    [JsonPropertyName("Girls' Clothing")]
    public string girlsclothing { get; set; }

    // Occurs 28
    [JsonPropertyName("Kids' Equestrian Breeches")]
    public string kidsequestrianbreeches { get; set; }

    // Occurs 28
    [JsonPropertyName("Men's Skiing Bibs")]
    public string mensskiingbibs { get; set; }

    // Occurs 28
    [JsonPropertyName("Women's Collar Necklaces")]
    public string womenscollarnecklaces { get; set; }

    // Occurs 28
    [JsonPropertyName("Women's Snake Charm Bracelets")]
    public string womenssnakecharmbracelets { get; set; }

    // Occurs 27
    [JsonPropertyName("Women's Cricket Clothing")]
    public string womenscricketclothing { get; set; }

    // Occurs 27
    [JsonPropertyName("Costume Walking Sticks & Canes")]
    public string costumewalkingstickscanes { get; set; }

    // Occurs 27
    [JsonPropertyName("Equestrian Clothing")]
    public string equestrianclothing { get; set; }

    // Occurs 27
    [JsonPropertyName("Boys' Sneakers")]
    public string boyssneakers { get; set; }

    // Occurs 27
    [JsonPropertyName("Girls' Costume Footwear")]
    public string girlscostumefootwear { get; set; }

    // Occurs 27
    [JsonPropertyName("Men's Novelty Tanks Tops")]
    public string mensnoveltytankstops { get; set; }

    // Occurs 27
    [JsonPropertyName("Greeting Cards")]
    public string greetingcards { get; set; }

    // Occurs 27
    [JsonPropertyName("Women's Promise Rings")]
    public string womenspromiserings { get; set; }

    // Occurs 27
    [JsonPropertyName("Applique Patches")]
    public string appliquepatches { get; set; }

    // Occurs 27
    [JsonPropertyName("Men's Hiking Socks")]
    public string menshikingsocks { get; set; }

    // Occurs 27
    [JsonPropertyName("Girls' Belts")]
    public string girlsbelts { get; set; }

    // Occurs 27
    [JsonPropertyName("Women's Costume Footwear")]
    public string womenscostumefootwear { get; set; }

    // Occurs 26
    [JsonPropertyName("Boys' Cycling Clothing")]
    public string boyscyclingclothing { get; set; }

    // Occurs 26
    [JsonPropertyName("Men's Cold Weather Scarves")]
    public string menscoldweatherscarves { get; set; }

    // Occurs 26
    [JsonPropertyName("Men's Newsboy Caps")]
    public string mensnewsboycaps { get; set; }

    // Occurs 26
    [JsonPropertyName("Men's Costume Hats")]
    public string menscostumehats { get; set; }

    // Occurs 26
    [JsonPropertyName("Women's Compression Pants")]
    public string womenscompressionpants { get; set; }

    // Occurs 26
    [JsonPropertyName("Men's Snowboarding Clothing")]
    public string menssnowboardingclothing { get; set; }

    // Occurs 26
    [JsonPropertyName("Girls' Cold Weather Neck Gaiters")]
    public string girlscoldweatherneckgaiters { get; set; }

    // Occurs 26
    [JsonPropertyName("Women's Body Piercing Studs")]
    public string womensbodypiercingstuds { get; set; }

    // Occurs 26
    [JsonPropertyName("Baby Girls' Pajama Sets")]
    public string babygirlspajamasets { get; set; }

    // Occurs 26
    [JsonPropertyName("Girls' Drop & Dangle Earrings")]
    public string girlsdropdangleearrings { get; set; }

    // Occurs 26
    [JsonPropertyName("Men's Fire & Safety Shoes")]
    public string mensfiresafetyshoes { get; set; }

    // Occurs 25
    [JsonPropertyName("Men's Hiking Shirts")]
    public string menshikingshirts { get; set; }

    // Occurs 25
    [JsonPropertyName("Men's Henley Shirts")]
    public string menshenleyshirts { get; set; }

    // Occurs 25
    [JsonPropertyName("Bra Pads & Breast Enhancers")]
    public string brapadsbreastenhancers { get; set; }

    // Occurs 25
    [JsonPropertyName("Men's Skiing & Snowboarding Socks")]
    public string mensskiingsnowboardingsocks { get; set; }

    // Occurs 25
    [JsonPropertyName("Men's Running Pants")]
    public string mensrunningpants { get; set; }

    // Occurs 25
    [JsonPropertyName("Women's Running Clothing")]
    public string womensrunningclothing { get; set; }

    // Occurs 25
    [JsonPropertyName("Men's Collar Stays")]
    public string menscollarstays { get; set; }

    // Occurs 25
    [JsonPropertyName("Men's Activewear Polos")]
    public string mensactivewearpolos { get; set; }

    // Occurs 25
    [JsonPropertyName("Women's Fashion Overalls")]
    public string womensfashionoveralls { get; set; }

    // Occurs 25
    [JsonPropertyName("Men's Equestrian Sport Boots")]
    public string mensequestriansportboots { get; set; }

    // Occurs 25
    [JsonPropertyName("Men's Cargo Shorts")]
    public string menscargoshorts { get; set; }

    // Occurs 25
    [JsonPropertyName("Men's Activewear Vests")]
    public string mensactivewearvests { get; set; }

    // Occurs 25
    [JsonPropertyName("Boys' Baseball Pants")]
    public string boysbaseballpants { get; set; }

    // Occurs 24
    [JsonPropertyName("Outdoor Recreation")]
    public string outdoorrecreation { get; set; }

    // Occurs 24
    [JsonPropertyName("Costume Headwear for Kids")]
    public string costumeheadwearforkids { get; set; }

    // Occurs 24
    [JsonPropertyName("Women's Golf Belts")]
    public string womensgolfbelts { get; set; }

    // Occurs 24
    [JsonPropertyName("Girls' Lockets")]
    public string girlslockets { get; set; }

    // Occurs 24
    [JsonPropertyName("Climate Pledge Friendly")]
    public string climatepledgefriendly { get; set; }

    // Occurs 24
    [JsonPropertyName("Climate Pledge Friendly: Apparel")]
    public string climatepledgefriendlyapparel { get; set; }

    // Occurs 24
    [JsonPropertyName("Boys' Snowboarding Jackets")]
    public string boyssnowboardingjackets { get; set; }

    // Occurs 24
    [JsonPropertyName("Women's Costume Eyewear")]
    public string womenscostumeeyewear { get; set; }

    // Occurs 24
    [JsonPropertyName("Men's Novelty Scarves")]
    public string mensnoveltyscarves { get; set; }

    // Occurs 24
    [JsonPropertyName("Women's Sweatshirts")]
    public string womenssweatshirts { get; set; }

    // Occurs 24
    [JsonPropertyName("Girls' Hoop Earrings")]
    public string girlshoopearrings { get; set; }

    // Occurs 24
    [JsonPropertyName("Baby Girls' Accessories")]
    public string babygirlsaccessories { get; set; }

    // Occurs 24
    [JsonPropertyName("Women's Athletic Shoes")]
    public string womensathleticshoes { get; set; }

    // Occurs 24
    [JsonPropertyName("Women's Club & Night Out Dresses")]
    public string womensclubnightoutdresses { get; set; }

    // Occurs 24
    [JsonPropertyName("Fishing Hats")]
    public string fishinghats { get; set; }

    // Occurs 24
    [JsonPropertyName("Women's Polo Shirts")]
    public string womenspoloshirts { get; set; }

    // Occurs 24
    [JsonPropertyName("Shoe Cleaners")]
    public string shoecleaners { get; set; }

    // Occurs 23
    [JsonPropertyName("Bridal Veils")]
    public string bridalveils { get; set; }

    // Occurs 23
    [JsonPropertyName("Watch Accessories")]
    public string watchaccessories { get; set; }

    // Occurs 23
    [JsonPropertyName("Men's Fire & Safety Boots")]
    public string mensfiresafetyboots { get; set; }

    // Occurs 23
    [JsonPropertyName("Boys' Soccer Jerseys")]
    public string boyssoccerjerseys { get; set; }

    // Occurs 23
    [JsonPropertyName("Women's Work & Utility Boots")]
    public string womensworkutilityboots { get; set; }

    // Occurs 23
    [JsonPropertyName("Kids' Costume Wigs")]
    public string kidscostumewigs { get; set; }

    // Occurs 23
    [JsonPropertyName("Men's Board Shorts")]
    public string mensboardshorts { get; set; }

    // Occurs 23
    [JsonPropertyName("Women's Earmuffs")]
    public string womensearmuffs { get; set; }

    // Occurs 23
    [JsonPropertyName("Women's Wool & Pea Coats")]
    public string womenswoolpeacoats { get; set; }

    // Occurs 23
    [JsonPropertyName("Boys' Blanket Sleepers")]
    public string boysblanketsleepers { get; set; }

    // Occurs 23
    [JsonPropertyName("Maternity Nursing & Maternity Bras")]
    public string maternitynursingmaternitybras { get; set; }

    // Occurs 23
    [JsonPropertyName("Men's Fashion Scarves")]
    public string mensfashionscarves { get; set; }

    // Occurs 23
    [JsonPropertyName("Baby Boys' Pants Sets")]
    public string babyboyspantssets { get; set; }

    // Occurs 23
    [JsonPropertyName("Baby Girls' Tees")]
    public string babygirlstees { get; set; }

    // Occurs 23
    [JsonPropertyName("Exercise Gloves")]
    public string exercisegloves { get; set; }

    // Occurs 23
    [JsonPropertyName("Men's Cycling Bib Shorts")]
    public string menscyclingbibshorts { get; set; }

    // Occurs 22
    [JsonPropertyName("Men's Novelty Sunglasses & Eyewear")]
    public string mensnoveltysunglasseseyewear { get; set; }

    // Occurs 22
    [JsonPropertyName("Men's Hiking Clothing")]
    public string menshikingclothing { get; set; }

    // Occurs 22
    [JsonPropertyName("Women's Pearl Strand Necklaces")]
    public string womenspearlstrandnecklaces { get; set; }

    // Occurs 22
    [JsonPropertyName("Women's Half Slips")]
    public string womenshalfslips { get; set; }

    // Occurs 22
    [JsonPropertyName("Ring Sizers")]
    public string ringsizers { get; set; }

    // Occurs 22
    [JsonPropertyName("Snowboarding Clothing")]
    public string snowboardingclothing { get; set; }

    // Occurs 22
    [JsonPropertyName("Men's Skiing Pants")]
    public string mensskiingpants { get; set; }

    // Occurs 22
    [JsonPropertyName("Men's Shirts")]
    public string mensshirts { get; set; }

    // Occurs 22
    [JsonPropertyName("Sports & Outdoor Recreation Accessories")]
    public string sportsoutdoorrecreationaccessories { get; set; }

    // Occurs 22
    [JsonPropertyName("Boys' Water Shoes")]
    public string boyswatershoes { get; set; }

    // Occurs 22
    [JsonPropertyName("Girls' Water Shoes")]
    public string girlswatershoes { get; set; }

    // Occurs 22
    [JsonPropertyName("Women's Tops, Tees & Blouses")]
    public string womenstopsteesblouses { get; set; }

    // Occurs 22
    [JsonPropertyName("Boys' Cold Weather Accessories")]
    public string boyscoldweatheraccessories { get; set; }

    // Occurs 22
    [JsonPropertyName("Powersports Gloves")]
    public string powersportsgloves { get; set; }

    // Occurs 21
    [JsonPropertyName("Women's Over-the-Knee Boots")]
    public string womensoverthekneeboots { get; set; }

    // Occurs 21
    [JsonPropertyName("Baby Girls' Pant Sets")]
    public string babygirlspantsets { get; set; }

    // Occurs 21
    [JsonPropertyName("Patio, Lawn & Garden")]
    public string patiolawngarden { get; set; }

    // Occurs 21
    [JsonPropertyName("Costume Wands")]
    public string costumewands { get; set; }

    // Occurs 21
    [JsonPropertyName("Men's Tracksuits")]
    public string menstracksuits { get; set; }

    // Occurs 21
    [JsonPropertyName("Men's Mules & Clogs")]
    public string mensmulesclogs { get; set; }

    // Occurs 21
    [JsonPropertyName("Gym Bags")]
    public string gymbags { get; set; }

    // Occurs 21
    [JsonPropertyName("Baby Boys' Suits")]
    public string babyboyssuits { get; set; }

    // Occurs 21
    [JsonPropertyName("Kung Fu Suit Sets")]
    public string kungfusuitsets { get; set; }

    // Occurs 21
    [JsonPropertyName("Cosmetic Bags")]
    public string cosmeticbags { get; set; }

    // Occurs 21
    [JsonPropertyName("Women's Gloves & Mittens")]
    public string womensglovesmittens { get; set; }

    // Occurs 21
    [JsonPropertyName("Men's Soccer Clothing")]
    public string menssoccerclothing { get; set; }

    // Occurs 21
    [JsonPropertyName("Women's Skiing Bibs & Pants")]
    public string womensskiingbibspants { get; set; }

    // Occurs 21
    [JsonPropertyName("Boys' Bowling Shoes")]
    public string boysbowlingshoes { get; set; }

    // Occurs 21
    [JsonPropertyName("Women's Novelty Sunglasses & Eyewear")]
    public string womensnoveltysunglasseseyewear { get; set; }

    // Occurs 21
    [JsonPropertyName("Men's Leather & Faux Leather Jackets & Coats")]
    public string mensleatherfauxleatherjacketscoats { get; set; }

    // Occurs 20
    [JsonPropertyName("Women's Bomber Hats")]
    public string womensbomberhats { get; set; }

    // Occurs 20
    [JsonPropertyName("Baby Girls' Outerwear Jackets")]
    public string babygirlsouterwearjackets { get; set; }

    // Occurs 20
    [JsonPropertyName("Men's Football Shoes")]
    public string mensfootballshoes { get; set; }

    // Occurs 20
    [JsonPropertyName("Men's Novelty Neckties")]
    public string mensnoveltyneckties { get; set; }

    // Occurs 20
    [JsonPropertyName("Women's Dance Clothing")]
    public string womensdanceclothing { get; set; }

    // Occurs 20
    [JsonPropertyName("Boys' Novelty Accessories")]
    public string boysnoveltyaccessories { get; set; }

    // Occurs 20
    [JsonPropertyName("Men's Tie Sets")]
    public string menstiesets { get; set; }

    // Occurs 20
    [JsonPropertyName("Exercise & Fitness Apparel")]
    public string exercisefitnessapparel { get; set; }

    // Occurs 20
    [JsonPropertyName("Novelty Watches")]
    public string noveltywatches { get; set; }

    // Occurs 20
    [JsonPropertyName("Sports Fan Headbands")]
    public string sportsfanheadbands { get; set; }

    // Occurs 20
    [JsonPropertyName("Women's Novelty Scarves")]
    public string womensnoveltyscarves { get; set; }

    // Occurs 20
    [JsonPropertyName("Men's Undershirts")]
    public string mensundershirts { get; set; }

    // Occurs 20
    [JsonPropertyName("Women's Hiking Shorts")]
    public string womenshikingshorts { get; set; }

    // Occurs 20
    [JsonPropertyName("Women's Novelty Dresses")]
    public string womensnoveltydresses { get; set; }

    // Occurs 20
    [JsonPropertyName("Women's Fur & Faux Fur Jackets & Coats")]
    public string womensfurfauxfurjacketscoats { get; set; }

    // Occurs 19
    [JsonPropertyName("Women's Pocket Watches")]
    public string womenspocketwatches { get; set; }

    // Occurs 19
    [JsonPropertyName("Girls' Pant Sets")]
    public string girlspantsets { get; set; }

    // Occurs 19
    [JsonPropertyName("Women's Link Charm Bracelets")]
    public string womenslinkcharmbracelets { get; set; }

    // Occurs 19
    [JsonPropertyName("Boys' Belts")]
    public string boysbelts { get; set; }

    // Occurs 19
    [JsonPropertyName("Men's Track & Field & Cross Country Shoes")]
    public string menstrackfieldcrosscountryshoes { get; set; }

    // Occurs 19
    [JsonPropertyName("Men's Golf Pants")]
    public string mensgolfpants { get; set; }

    // Occurs 19
    [JsonPropertyName("Men's Work Utility & Safety Tops")]
    public string mensworkutilitysafetytops { get; set; }

    // Occurs 19
    [JsonPropertyName("Men's Volleyball Clothing")]
    public string mensvolleyballclothing { get; set; }

    // Occurs 19
    [JsonPropertyName("Men's Earmuffs")]
    public string mensearmuffs { get; set; }

    // Occurs 19
    [JsonPropertyName("Girls' Sneakers")]
    public string girlssneakers { get; set; }

    // Occurs 19
    [JsonPropertyName("Home Décor Accents")]
    public string homedécoraccents { get; set; }

    // Occurs 19
    [JsonPropertyName("Beads & Bead Assortments")]
    public string beadsbeadassortments { get; set; }

    // Occurs 19
    [JsonPropertyName("Sports Apparel & Equipment")]
    public string sportsapparelequipment { get; set; }

    // Occurs 19
    [JsonPropertyName("Boys' Rain Boots")]
    public string boysrainboots { get; set; }

    // Occurs 19
    [JsonPropertyName("Unique Home")]
    public string uniquehome { get; set; }

    // Occurs 18
    [JsonPropertyName("Baby Girls' One-Piece Footies")]
    public string babygirlsonepiecefooties { get; set; }

    // Occurs 18
    [JsonPropertyName("Women's Work Utility & Safety Outerwear")]
    public string womensworkutilitysafetyouterwear { get; set; }

    // Occurs 18
    [JsonPropertyName("Girls' Novelty Accessories")]
    public string girlsnoveltyaccessories { get; set; }

    // Occurs 18
    [JsonPropertyName("Boys' Sunglasses")]
    public string boyssunglasses { get; set; }

    // Occurs 18
    [JsonPropertyName("Men's Football Jerseys")]
    public string mensfootballjerseys { get; set; }

    // Occurs 18
    [JsonPropertyName("Women's Leather & Faux Leather Jackets & Coats")]
    public string womensleatherfauxleatherjacketscoats { get; set; }

    // Occurs 18
    [JsonPropertyName("Men's Suits")]
    public string menssuits { get; set; }

    // Occurs 18
    [JsonPropertyName("Baby Boys' Pajama Sets")]
    public string babyboyspajamasets { get; set; }

    // Occurs 18
    [JsonPropertyName("Men's Outerwear Jackets & Coats")]
    public string mensouterwearjacketscoats { get; set; }

    // Occurs 18
    [JsonPropertyName("Women's Hiking Clothing")]
    public string womenshikingclothing { get; set; }

    // Occurs 18
    [JsonPropertyName("Girls' Bathrobes")]
    public string girlsbathrobes { get; set; }

    // Occurs 18
    [JsonPropertyName("Baby Girls' Slippers")]
    public string babygirlsslippers { get; set; }

    // Occurs 18
    [JsonPropertyName("Women's Dance Skirts")]
    public string womensdanceskirts { get; set; }

    // Occurs 18
    [JsonPropertyName("Baby Boys' Tees")]
    public string babyboystees { get; set; }

    // Occurs 18
    [JsonPropertyName("Girls' Earrings")]
    public string girlsearrings { get; set; }

    // Occurs 18
    [JsonPropertyName("Women's Cycling Tights & Pants")]
    public string womenscyclingtightspants { get; set; }

    // Occurs 18
    [JsonPropertyName("Women's Skiing Clothing")]
    public string womensskiingclothing { get; set; }

    // Occurs 18
    [JsonPropertyName("Women's Toe Rings")]
    public string womenstoerings { get; set; }

    // Occurs 18
    [JsonPropertyName("Men's Cycling Vests")]
    public string menscyclingvests { get; set; }

    // Occurs 18
    [JsonPropertyName("Women's Tennis Bracelets")]
    public string womenstennisbracelets { get; set; }

    // Occurs 18
    [JsonPropertyName("Women's Cowboy Hats")]
    public string womenscowboyhats { get; set; }

    // Occurs 18
    [JsonPropertyName("Girls' Special Occasion Dresses")]
    public string girlsspecialoccasiondresses { get; set; }

    // Occurs 18
    [JsonPropertyName("Men's Novelty Sweatshirts")]
    public string mensnoveltysweatshirts { get; set; }

    // Occurs 18
    [JsonPropertyName("Women's Thermal Underwear Tops")]
    public string womensthermalunderweartops { get; set; }

    // Occurs 17
    [JsonPropertyName("Men's Rash Guard Shirts")]
    public string mensrashguardshirts { get; set; }

    // Occurs 17
    [JsonPropertyName("Hair Care Products")]
    public string haircareproducts { get; set; }

    // Occurs 17
    [JsonPropertyName("Novelty Clothing & More")]
    public string noveltyclothingmore { get; set; }

    // Occurs 17
    [JsonPropertyName("Girls' Costumes & Accessories")]
    public string girlscostumesaccessories { get; set; }

    // Occurs 17
    [JsonPropertyName("Men's Dress Shirts")]
    public string mensdressshirts { get; set; }

    // Occurs 17
    [JsonPropertyName("Women's Novelty Beanies & Knit Hats")]
    public string womensnoveltybeaniesknithats { get; set; }

    // Occurs 17
    [JsonPropertyName("Maternity Belly Bands")]
    public string maternitybellybands { get; set; }

    // Occurs 17
    [JsonPropertyName("Women's Pendants & Coins")]
    public string womenspendantscoins { get; set; }

    // Occurs 17
    [JsonPropertyName("Men's Eyewear Frames")]
    public string menseyewearframes { get; set; }

    // Occurs 17
    [JsonPropertyName("Women's Pajama Bottoms")]
    public string womenspajamabottoms { get; set; }

    // Occurs 17
    [JsonPropertyName("Women's Hiking Shirts")]
    public string womenshikingshirts { get; set; }

    // Occurs 17
    [JsonPropertyName("Golf Clothing")]
    public string golfclothing { get; set; }

    // Occurs 17
    [JsonPropertyName("Women's Body Piercing Jewelry")]
    public string womensbodypiercingjewelry { get; set; }

    // Occurs 17
    [JsonPropertyName("Jewelry Making Charms")]
    public string jewelrymakingcharms { get; set; }

    // Occurs 17
    [JsonPropertyName("Women's Insulated Shells")]
    public string womensinsulatedshells { get; set; }

    // Occurs 17
    [JsonPropertyName("Baby Girls' Special Occasion Dresses")]
    public string babygirlsspecialoccasiondresses { get; set; }

    // Occurs 17
    [JsonPropertyName("Women's Costume Bottoms")]
    public string womenscostumebottoms { get; set; }

    // Occurs 17
    [JsonPropertyName("Men's Wrestling Shoes")]
    public string menswrestlingshoes { get; set; }

    // Occurs 17
    [JsonPropertyName("Wrestling Equipment")]
    public string wrestlingequipment { get; set; }

    // Occurs 17
    [JsonPropertyName("Women's Berets")]
    public string womensberets { get; set; }

    // Occurs 17
    [JsonPropertyName("Girls' Rain Boots")]
    public string girlsrainboots { get; set; }

    // Occurs 17
    [JsonPropertyName("Cheerleading Footwear")]
    public string cheerleadingfootwear { get; set; }

    // Occurs 17
    [JsonPropertyName("Women's Cold Weather Neck Gaiters")]
    public string womenscoldweatherneckgaiters { get; set; }

    // Occurs 17
    [JsonPropertyName("Men's Cycling Leg Warmers")]
    public string menscyclinglegwarmers { get; set; }

    // Occurs 17
    [JsonPropertyName("Women's Novelty Skirts")]
    public string womensnoveltyskirts { get; set; }

    // Occurs 16
    [JsonPropertyName("Baby Girls' Robes")]
    public string babygirlsrobes { get; set; }

    // Occurs 16
    [JsonPropertyName("Men's Skiing Clothing")]
    public string mensskiingclothing { get; set; }

    // Occurs 16
    [JsonPropertyName("Boys' Swim Trunks")]
    public string boysswimtrunks { get; set; }

    // Occurs 16
    [JsonPropertyName("Women's Work Utility & Safety Pants")]
    public string womensworkutilitysafetypants { get; set; }

    // Occurs 16
    [JsonPropertyName("Men's Backpacking Boots")]
    public string mensbackpackingboots { get; set; }

    // Occurs 16
    [JsonPropertyName("Women's Tracksuits")]
    public string womenstracksuits { get; set; }

    // Occurs 16
    [JsonPropertyName("Women's Exotic Hosiery")]
    public string womensexotichosiery { get; set; }

    // Occurs 16
    [JsonPropertyName("Women's Cycling Caps")]
    public string womenscyclingcaps { get; set; }

    // Occurs 16
    [JsonPropertyName("Women's Activewear Vests")]
    public string womensactivewearvests { get; set; }

    // Occurs 16
    [JsonPropertyName("Boys' Hiking & Outdoor Recreation Hats & Headwear")]
    public string boyshikingoutdoorrecreationhatsheadwear { get; set; }

    // Occurs 16
    [JsonPropertyName("Women's Team Sports Shoes")]
    public string womensteamsportsshoes { get; set; }

    // Occurs 16
    [JsonPropertyName("Running Waist Packs")]
    public string runningwaistpacks { get; set; }

    // Occurs 16
    [JsonPropertyName("Boys' Snowboarding Clothing")]
    public string boyssnowboardingclothing { get; set; }

    // Occurs 16
    [JsonPropertyName("Men's Trunks Underwear")]
    public string menstrunksunderwear { get; set; }

    // Occurs 16
    [JsonPropertyName("Boys' Athletic Shirts & Tees")]
    public string boysathleticshirtstees { get; set; }

    // Occurs 16
    [JsonPropertyName("Men's Cotton Lightweight Jackets")]
    public string menscottonlightweightjackets { get; set; }

    // Occurs 16
    [JsonPropertyName("Men's Athletic Swimwear Briefs")]
    public string mensathleticswimwearbriefs { get; set; }

    // Occurs 16
    [JsonPropertyName("Men's Windbreakers")]
    public string menswindbreakers { get; set; }

    // Occurs 16
    [JsonPropertyName("Men's Running Shoes")]
    public string mensrunningshoes { get; set; }

    // Occurs 16
    [JsonPropertyName("Men's Active & Performance Insulated Jackets")]
    public string mensactiveperformanceinsulatedjackets { get; set; }

    // Occurs 15
    [JsonPropertyName("Fishing Gloves")]
    public string fishinggloves { get; set; }

    // Occurs 15
    [JsonPropertyName("Girls' Slippers")]
    public string girlsslippers { get; set; }

    // Occurs 15
    [JsonPropertyName("Men's Calf Socks")]
    public string menscalfsocks { get; set; }

    // Occurs 15
    [JsonPropertyName("Boys' Skiing Clothing")]
    public string boysskiingclothing { get; set; }

    // Occurs 15
    [JsonPropertyName("Men's Cycling Clothing Sets")]
    public string menscyclingclothingsets { get; set; }

    // Occurs 15
    [JsonPropertyName("Men's Running Clothing")]
    public string mensrunningclothing { get; set; }

    // Occurs 15
    [JsonPropertyName("Martial Arts Equipment")]
    public string martialartsequipment { get; set; }

    // Occurs 15
    [JsonPropertyName("Women's Lingerie Camisoles & Tanks")]
    public string womenslingeriecamisolestanks { get; set; }

    // Occurs 15
    [JsonPropertyName("Women's Shapewear Waist Cinchers")]
    public string womensshapewearwaistcinchers { get; set; }

    // Occurs 15
    [JsonPropertyName("Women's Coats, Jackets & Vests")]
    public string womenscoatsjacketsvests { get; set; }

    // Occurs 15
    [JsonPropertyName("Baby Boys' Pants")]
    public string babyboyspants { get; set; }

    // Occurs 15
    [JsonPropertyName("Girls' Activewear T-Shirts")]
    public string girlsactiveweartshirts { get; set; }

    // Occurs 15
    [JsonPropertyName("Women's Activewear Dresses")]
    public string womensactiveweardresses { get; set; }

    // Occurs 15
    [JsonPropertyName("Baby Girls' Sneakers")]
    public string babygirlssneakers { get; set; }

    // Occurs 15
    [JsonPropertyName("Girls' Flats")]
    public string girlsflats { get; set; }

    // Occurs 15
    [JsonPropertyName("Girls' One-Piece Swimwear")]
    public string girlsonepieceswimwear { get; set; }

    // Occurs 15
    [JsonPropertyName("Women's Denim Jackets")]
    public string womensdenimjackets { get; set; }

    // Occurs 15
    [JsonPropertyName("Women's Formal Dresses")]
    public string womensformaldresses { get; set; }

    // Occurs 15
    [JsonPropertyName("Girls' Volleyball Clothing")]
    public string girlsvolleyballclothing { get; set; }

    // Occurs 15
    [JsonPropertyName("Baby Boys' Raincoats & Jackets")]
    public string babyboysraincoatsjackets { get; set; }

    // Occurs 15
    [JsonPropertyName("Boys' Sandals")]
    public string boyssandals { get; set; }

    // Occurs 14
    [JsonPropertyName("Baby Girls' Sandals")]
    public string babygirlssandals { get; set; }

    // Occurs 14
    [JsonPropertyName("Baby Girls' Snow Suits")]
    public string babygirlssnowsuits { get; set; }

    // Occurs 14
    [JsonPropertyName("Fishing Equipment")]
    public string fishingequipment { get; set; }

    // Occurs 14
    [JsonPropertyName("Men's Chelsea Boots")]
    public string menschelseaboots { get; set; }

    // Occurs 14
    [JsonPropertyName("Women's Fedoras")]
    public string womensfedoras { get; set; }

    // Occurs 14
    [JsonPropertyName("Women's Active Wind & Rain Outerwear")]
    public string womensactivewindrainouterwear { get; set; }

    // Occurs 14
    [JsonPropertyName("Baby Girls' Nightgowns")]
    public string babygirlsnightgowns { get; set; }

    // Occurs 14
    [JsonPropertyName("Sports Fan Backpacks")]
    public string sportsfanbackpacks { get; set; }

    // Occurs 14
    [JsonPropertyName("Boys' Rash Guard Shirts")]
    public string boysrashguardshirts { get; set; }

    // Occurs 14
    [JsonPropertyName("Women's Thermal Underwear Sets")]
    public string womensthermalunderwearsets { get; set; }

    // Occurs 14
    [JsonPropertyName("Girls' Casual Dresses")]
    public string girlscasualdresses { get; set; }

    // Occurs 14
    [JsonPropertyName("Business Card Holders")]
    public string businesscardholders { get; set; }

    // Occurs 14
    [JsonPropertyName("Boys' Rings")]
    public string boysrings { get; set; }

    // Occurs 14
    [JsonPropertyName("Women's Cycling Clothing Sets")]
    public string womenscyclingclothingsets { get; set; }

    // Occurs 14
    [JsonPropertyName("Women's Compression Shorts")]
    public string womenscompressionshorts { get; set; }

    // Occurs 14
    [JsonPropertyName("Sports Fan Luggage Tags")]
    public string sportsfanluggagetags { get; set; }

    // Occurs 14
    [JsonPropertyName("Baby Boys' Snow Suits")]
    public string babyboyssnowsuits { get; set; }

    // Occurs 14
    [JsonPropertyName("Boys' Football Jerseys")]
    public string boysfootballjerseys { get; set; }

    // Occurs 14
    [JsonPropertyName("Boys' Costumes & Accessories")]
    public string boyscostumesaccessories { get; set; }

    // Occurs 14
    [JsonPropertyName("Boys' Athletic Shoes")]
    public string boysathleticshoes { get; set; }

    // Occurs 14
    [JsonPropertyName("Soccer Goalkeeper Gloves")]
    public string soccergoalkeepergloves { get; set; }

    // Occurs 13
    [JsonPropertyName("Men's Football Clothing")]
    public string mensfootballclothing { get; set; }

    // Occurs 13
    [JsonPropertyName("Swimming Equipment Bags")]
    public string swimmingequipmentbags { get; set; }

    // Occurs 13
    [JsonPropertyName("Amazon Renewed")]
    public string amazonrenewed { get; set; }

    // Occurs 13
    [JsonPropertyName("Women's Golf Clothing")]
    public string womensgolfclothing { get; set; }

    // Occurs 13
    [JsonPropertyName("Women's Cycling Jackets")]
    public string womenscyclingjackets { get; set; }

    // Occurs 13
    [JsonPropertyName("Boys' Soccer Shoes")]
    public string boyssoccershoes { get; set; }

    // Occurs 13
    [JsonPropertyName("Baby Girls' Playwear Dresses")]
    public string babygirlsplayweardresses { get; set; }

    // Occurs 13
    [JsonPropertyName("Women's Chef Jackets")]
    public string womenschefjackets { get; set; }

    // Occurs 13
    [JsonPropertyName("Girls' Sport Headbands")]
    public string girlssportheadbands { get; set; }

    // Occurs 13
    [JsonPropertyName("Women's Sweatsuits")]
    public string womenssweatsuits { get; set; }

    // Occurs 13
    [JsonPropertyName("Boys' Novelty T-Shirts")]
    public string boysnoveltytshirts { get; set; }

    // Occurs 13
    [JsonPropertyName("Men's Athletic Swimwear Jammers")]
    public string mensathleticswimwearjammers { get; set; }

    // Occurs 13
    [JsonPropertyName("Garment Covers")]
    public string garmentcovers { get; set; }

    // Occurs 13
    [JsonPropertyName("Electric Shoe Polishers")]
    public string electricshoepolishers { get; set; }

    // Occurs 13
    [JsonPropertyName("Boys' Athletic Shorts")]
    public string boysathleticshorts { get; set; }

    // Occurs 13
    [JsonPropertyName("Toy Figures & Playsets")]
    public string toyfiguresplaysets { get; set; }

    // Occurs 13
    [JsonPropertyName("Men's Running Jackets")]
    public string mensrunningjackets { get; set; }

    // Occurs 13
    [JsonPropertyName("Women's Athletic Two-Piece Swimsuits")]
    public string womensathletictwopieceswimsuits { get; set; }

    // Occurs 13
    [JsonPropertyName("Foot Arch Supports")]
    public string footarchsupports { get; set; }

    // Occurs 13
    [JsonPropertyName("Women's Swimwear Bottoms")]
    public string womensswimwearbottoms { get; set; }

    // Occurs 13
    [JsonPropertyName("Boys' Suspenders")]
    public string boyssuspenders { get; set; }

    // Occurs 13
    [JsonPropertyName("Resistance Bands")]
    public string resistancebands { get; set; }

    // Occurs 13
    [JsonPropertyName("Women's Bucket Hats")]
    public string womensbuckethats { get; set; }

    // Occurs 13
    [JsonPropertyName("Men's Novelty Jackets & Coats")]
    public string mensnoveltyjacketscoats { get; set; }

    // Occurs 12
    [JsonPropertyName("Men's Dental Grills")]
    public string mensdentalgrills { get; set; }

    // Occurs 12
    [JsonPropertyName("Men's Thermal Underwear Tops")]
    public string mensthermalunderweartops { get; set; }

    // Occurs 12
    [JsonPropertyName("Baby Wearable Blankets")]
    public string babywearableblankets { get; set; }

    // Occurs 12
    [JsonPropertyName("Boys' Slippers")]
    public string boysslippers { get; set; }

    // Occurs 12
    [JsonPropertyName("Personal Defense Equipment")]
    public string personaldefenseequipment { get; set; }

    // Occurs 12
    [JsonPropertyName("Fishing Boot & Wader Bags")]
    public string fishingbootwaderbags { get; set; }

    // Occurs 12
    [JsonPropertyName("Women's Bikini Tops")]
    public string womensbikinitops { get; set; }

    // Occurs 12
    [JsonPropertyName("Iron-on Transfers")]
    public string ironontransfers { get; set; }

    // Occurs 12
    [JsonPropertyName("Taekwondo Suit Sets")]
    public string taekwondosuitsets { get; set; }

    // Occurs 12
    [JsonPropertyName("Women's Newsboy Caps")]
    public string womensnewsboycaps { get; set; }

    // Occurs 12
    [JsonPropertyName("Women's Shapewear Bodysuits")]
    public string womensshapewearbodysuits { get; set; }

    // Occurs 12
    [JsonPropertyName("Men's Hiking Shorts")]
    public string menshikingshorts { get; set; }

    // Occurs 12
    [JsonPropertyName("Women's Snowboarding Jackets")]
    public string womenssnowboardingjackets { get; set; }

    // Occurs 12
    [JsonPropertyName("Boys' Rain Jackets")]
    public string boysrainjackets { get; set; }

    // Occurs 12
    [JsonPropertyName("Baby Boys' Nightgowns")]
    public string babyboysnightgowns { get; set; }

    // Occurs 12
    [JsonPropertyName("Women's Activewear Polos")]
    public string womensactivewearpolos { get; set; }

    // Occurs 12
    [JsonPropertyName("Men's Ice Hockey Jerseys")]
    public string mensicehockeyjerseys { get; set; }

    // Occurs 12
    [JsonPropertyName("Women's Activewear Skirts & Skorts")]
    public string womensactivewearskirtsskorts { get; set; }

    // Occurs 12
    [JsonPropertyName("Hiking Waist Packs")]
    public string hikingwaistpacks { get; set; }

    // Occurs 12
    [JsonPropertyName("Men's Novelty Bucket Hats")]
    public string mensnoveltybuckethats { get; set; }

    // Occurs 12
    [JsonPropertyName("Pocket Watch Chains")]
    public string pocketwatchchains { get; set; }

    // Occurs 12
    [JsonPropertyName("Men's Volleyball Shoes")]
    public string mensvolleyballshoes { get; set; }

    // Occurs 12
    [JsonPropertyName("Men's Novelty Sweaters")]
    public string mensnoveltysweaters { get; set; }

    // Occurs 12
    [JsonPropertyName("Badge Lanyards")]
    public string badgelanyards { get; set; }

    // Occurs 12
    [JsonPropertyName("Women's Pajama Tops")]
    public string womenspajamatops { get; set; }

    // Occurs 12
    [JsonPropertyName("Men's Top-Handle Bags")]
    public string menstophandlebags { get; set; }

    // Occurs 11
    [JsonPropertyName("Men's Hiking & Trekking Shoes")]
    public string menshikingtrekkingshoes { get; set; }

    // Occurs 11
    [JsonPropertyName("Baby Boys' Robes")]
    public string babyboysrobes { get; set; }

    // Occurs 11
    [JsonPropertyName("Baby Girls' Skirt Sets")]
    public string babygirlsskirtsets { get; set; }

    // Occurs 11
    [JsonPropertyName("Jiu-Jitsu Suit Belts")]
    public string jiujitsusuitbelts { get; set; }

    // Occurs 11
    [JsonPropertyName("Boys' Button-Down Shirts")]
    public string boysbuttondownshirts { get; set; }

    // Occurs 11
    [JsonPropertyName("Girls' Hiking & Outdoor Recreation Headwear")]
    public string girlshikingoutdoorrecreationheadwear { get; set; }

    // Occurs 11
    [JsonPropertyName("Men's Sport Coats & Blazers")]
    public string menssportcoatsblazers { get; set; }

    // Occurs 11
    [JsonPropertyName("Men's Basketball Jerseys")]
    public string mensbasketballjerseys { get; set; }

    // Occurs 11
    [JsonPropertyName("Women's Soccer Jerseys")]
    public string womenssoccerjerseys { get; set; }

    // Occurs 11
    [JsonPropertyName("Women's Work Utility & Safety Apparel")]
    public string womensworkutilitysafetyapparel { get; set; }

    // Occurs 11
    [JsonPropertyName("Sports Fan Baby Clothing")]
    public string sportsfanbabyclothing { get; set; }

    // Occurs 11
    [JsonPropertyName("Sports Fan Jerseys")]
    public string sportsfanjerseys { get; set; }

    // Occurs 11
    [JsonPropertyName("Women's Calf Socks")]
    public string womenscalfsocks { get; set; }

    // Occurs 11
    [JsonPropertyName("Girls' Activewear")]
    public string girlsactivewear { get; set; }

    // Occurs 11
    [JsonPropertyName("Women's Work & Safety Clothing")]
    public string womensworksafetyclothing { get; set; }

    // Occurs 11
    [JsonPropertyName("Men's Cycling Bib Pants")]
    public string menscyclingbibpants { get; set; }

    // Occurs 11
    [JsonPropertyName("Men's ID Cases")]
    public string mensidcases { get; set; }

    // Occurs 11
    [JsonPropertyName("Earring Backs & Findings")]
    public string earringbacksfindings { get; set; }

    // Occurs 11
    [JsonPropertyName("Breast Petals")]
    public string breastpetals { get; set; }

    // Occurs 11
    [JsonPropertyName("Tactical Backpacks")]
    public string tacticalbackpacks { get; set; }

    // Occurs 11
    [JsonPropertyName("Baby Boys' Layette Sets")]
    public string babyboyslayettesets { get; set; }

    // Occurs 11
    [JsonPropertyName("Women's Cold Weather Arm Warmers")]
    public string womenscoldweatherarmwarmers { get; set; }

    // Occurs 11
    [JsonPropertyName("Karate Suit Tops")]
    public string karatesuittops { get; set; }

    // Occurs 11
    [JsonPropertyName("Men's Baseball Clothing")]
    public string mensbaseballclothing { get; set; }

    // Occurs 11
    [JsonPropertyName("Men's Visors")]
    public string mensvisors { get; set; }

    // Occurs 11
    [JsonPropertyName("Boys' Swimwear Bodysuits")]
    public string boysswimwearbodysuits { get; set; }

    // Occurs 11
    [JsonPropertyName("Girls' Racquet Sport Shoes")]
    public string girlsracquetsportshoes { get; set; }

    // Occurs 11
    [JsonPropertyName("Sports Fan Rings")]
    public string sportsfanrings { get; set; }

    // Occurs 11
    [JsonPropertyName("Boys' Baseball & Softball Shoes")]
    public string boysbaseballsoftballshoes { get; set; }

    // Occurs 11
    [JsonPropertyName("Women's Health Care & Food Service Shoes")]
    public string womenshealthcarefoodserviceshoes { get; set; }

    // Occurs 11
    [JsonPropertyName("Soccer Clothing")]
    public string soccerclothing { get; set; }

    // Occurs 11
    [JsonPropertyName("Women's Briefs")]
    public string womensbriefs { get; set; }

    // Occurs 11
    [JsonPropertyName("Boys' Racquet Sport Shoes")]
    public string boysracquetsportshoes { get; set; }

    // Occurs 11
    [JsonPropertyName("Women's Folding Fans")]
    public string womensfoldingfans { get; set; }

    // Occurs 11
    [JsonPropertyName("Men's Watches")]
    public string menswatches { get; set; }

    // Occurs 11
    [JsonPropertyName("Men's Novelty Polo Shirts")]
    public string mensnoveltypoloshirts { get; set; }

    // Occurs 10
    [JsonPropertyName("Women's Shapewear Control Panties")]
    public string womensshapewearcontrolpanties { get; set; }

    // Occurs 10
    [JsonPropertyName("Women's Pants")]
    public string womenspants { get; set; }

    // Occurs 10
    [JsonPropertyName("Sports Fan Jackets")]
    public string sportsfanjackets { get; set; }

    // Occurs 10
    [JsonPropertyName("Women's Replacement Sunglass Lenses")]
    public string womensreplacementsunglasslenses { get; set; }

    // Occurs 10
    [JsonPropertyName("Boys' Coin Purses & Pouches")]
    public string boyscoinpursespouches { get; set; }

    // Occurs 10
    [JsonPropertyName("Women's Athletic Skirts")]
    public string womensathleticskirts { get; set; }

    // Occurs 10
    [JsonPropertyName("Girls' Cold Weather Hats & Caps")]
    public string girlscoldweatherhatscaps { get; set; }

    // Occurs 10
    [JsonPropertyName("Men's Body Piercing Jewelry")]
    public string mensbodypiercingjewelry { get; set; }

    // Occurs 10
    [JsonPropertyName("Men's Novelty Hats & Caps")]
    public string mensnoveltyhatscaps { get; set; }

    // Occurs 10
    [JsonPropertyName("Men's Card & ID Cases")]
    public string menscardidcases { get; set; }

    // Occurs 10
    [JsonPropertyName("Men's Costume Tops")]
    public string menscostumetops { get; set; }

    // Occurs 10
    [JsonPropertyName("Men's Boxer Shorts")]
    public string mensboxershorts { get; set; }

    // Occurs 10
    [JsonPropertyName("Baby Girls' Tights")]
    public string babygirlstights { get; set; }

    // Occurs 10
    [JsonPropertyName("Girls' Snowboarding Clothing")]
    public string girlssnowboardingclothing { get; set; }

    // Occurs 10
    [JsonPropertyName("Women's Ball Earrings")]
    public string womensballearrings { get; set; }

    // Occurs 10
    [JsonPropertyName("Tactical & Personal Defense Equipment")]
    public string tacticalpersonaldefenseequipment { get; set; }

    // Occurs 10
    [JsonPropertyName("Men's Novelty Belts")]
    public string mensnoveltybelts { get; set; }

    // Occurs 10
    [JsonPropertyName("Men's Health Care & Food Service Shoes")]
    public string menshealthcarefoodserviceshoes { get; set; }

    // Occurs 10
    [JsonPropertyName("Girls' Athletic Socks")]
    public string girlsathleticsocks { get; set; }

    // Occurs 10
    [JsonPropertyName("Beading Supplies")]
    public string beadingsupplies { get; set; }

    // Occurs 10
    [JsonPropertyName("Unique Sports & Outdoors")]
    public string uniquesportsoutdoors { get; set; }

    // Occurs 10
    [JsonPropertyName("Women's Down Jackets & Parkas")]
    public string womensdownjacketsparkas { get; set; }

    // Occurs 10
    [JsonPropertyName("Women's Watches")]
    public string womenswatches { get; set; }

    // Occurs 10
    [JsonPropertyName("Women's Nightgowns & Sleepshirts")]
    public string womensnightgownssleepshirts { get; set; }

    // Occurs 10
    [JsonPropertyName("Tactical Bags & Packs")]
    public string tacticalbagspacks { get; set; }

    // Occurs 10
    [JsonPropertyName("Medical Uniforms & Scrubs")]
    public string medicaluniformsscrubs { get; set; }

    // Occurs 10
    [JsonPropertyName("Multitools")]
    public string multitools { get; set; }

    // Occurs 10
    [JsonPropertyName("Baby Boys' One-Piece Footies")]
    public string babyboysonepiecefooties { get; set; }

    // Occurs 10
    [JsonPropertyName("Sports Fan Earrings")]
    public string sportsfanearrings { get; set; }

    // Occurs 10
    [JsonPropertyName("Men's Cricket Clothing")]
    public string menscricketclothing { get; set; }

    // Occurs 10
    [JsonPropertyName("Costume Makeup, Facial Hair & Adhesives")]
    public string costumemakeupfacialhairadhesives { get; set; }

    // Occurs 10
    [JsonPropertyName("Women's Novelty Sweatshirts")]
    public string womensnoveltysweatshirts { get; set; }

    // Occurs 10
    [JsonPropertyName("Unique Baby & Toddler")]
    public string uniquebabytoddler { get; set; }

    // Occurs 10
    [JsonPropertyName("Boys' Tracksuits")]
    public string boystracksuits { get; set; }

    // Occurs 10
    [JsonPropertyName("Books")]
    public string books { get; set; }

    // Occurs 10
    [JsonPropertyName("Baby Girls' Raincoats & Jackets")]
    public string babygirlsraincoatsjackets { get; set; }

    // Occurs 10
    [JsonPropertyName("Women's Skiing Bibs")]
    public string womensskiingbibs { get; set; }

    // Occurs 10
    [JsonPropertyName("Hiking Clothing")]
    public string hikingclothing { get; set; }

    // Occurs 10
    [JsonPropertyName("Men's Athletic Underwear")]
    public string mensathleticunderwear { get; set; }

    // Occurs 10
    [JsonPropertyName("Boys' Athletic Pants")]
    public string boysathleticpants { get; set; }

    // Occurs 10
    [JsonPropertyName("Women's Track Pants")]
    public string womenstrackpants { get; set; }

    // Occurs 10
    [JsonPropertyName("Baby Girls' Christening Clothing")]
    public string babygirlschristeningclothing { get; set; }

    // Occurs 10
    [JsonPropertyName("Baby Boys' Sneakers")]
    public string babyboyssneakers { get; set; }

    // Occurs 10
    [JsonPropertyName("Women's Athletic Skorts")]
    public string womensathleticskorts { get; set; }

    // Occurs 10
    [JsonPropertyName("Shoe Care Treatments & Dyes")]
    public string shoecaretreatmentsdyes { get; set; }

    // Occurs 9
    [JsonPropertyName("Running Equipment")]
    public string runningequipment { get; set; }

    // Occurs 9
    [JsonPropertyName("Boys' Skiing Jackets")]
    public string boysskiingjackets { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Golf Pants")]
    public string womensgolfpants { get; set; }

    // Occurs 9
    [JsonPropertyName("Boys' Football Pants")]
    public string boysfootballpants { get; set; }

    // Occurs 9
    [JsonPropertyName("Girls' Rain Jackets")]
    public string girlsrainjackets { get; set; }

    // Occurs 9
    [JsonPropertyName("Cloth Face Masks")]
    public string clothfacemasks { get; set; }

    // Occurs 9
    [JsonPropertyName("Men's Thermal Underwear Bottoms")]
    public string mensthermalunderwearbottoms { get; set; }

    // Occurs 9
    [JsonPropertyName("Fishing Accessories")]
    public string fishingaccessories { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Skiing & Snowboarding Socks")]
    public string womensskiingsnowboardingsocks { get; set; }

    // Occurs 9
    [JsonPropertyName("Boys' Loafers")]
    public string boysloafers { get; set; }

    // Occurs 9
    [JsonPropertyName("Cell Phone Holsters")]
    public string cellphoneholsters { get; set; }

    // Occurs 9
    [JsonPropertyName("Baby Girls' Boots")]
    public string babygirlsboots { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Novelty Pants & Capris")]
    public string womensnoveltypantscapris { get; set; }

    // Occurs 9
    [JsonPropertyName("Girls' Leggings")]
    public string girlsleggings { get; set; }

    // Occurs 9
    [JsonPropertyName("Men's Exotic G-Strings & Thongs")]
    public string mensexoticgstringsthongs { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Handbag Accessories")]
    public string womenshandbagaccessories { get; set; }

    // Occurs 9
    [JsonPropertyName("Men's Underwear Briefs")]
    public string mensunderwearbriefs { get; set; }

    // Occurs 9
    [JsonPropertyName("Beading & Jewelry Making")]
    public string beadingjewelrymaking { get; set; }

    // Occurs 9
    [JsonPropertyName("Maternity Activewear")]
    public string maternityactivewear { get; set; }

    // Occurs 9
    [JsonPropertyName("Powersports Protective Gear")]
    public string powersportsprotectivegear { get; set; }

    // Occurs 9
    [JsonPropertyName("Marine Dry Bags")]
    public string marinedrybags { get; set; }

    // Occurs 9
    [JsonPropertyName("Dive Skins")]
    public string diveskins { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Costume Hat")]
    public string womenscostumehat { get; set; }

    // Occurs 9
    [JsonPropertyName("Girls' Shoes")]
    public string girlsshoes { get; set; }

    // Occurs 9
    [JsonPropertyName("Sports Fan Sandals")]
    public string sportsfansandals { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Oxfords")]
    public string womensoxfords { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Softball Jerseys")]
    public string womenssoftballjerseys { get; set; }

    // Occurs 9
    [JsonPropertyName("Baby Girls' Skirts, Skooters & Skorts")]
    public string babygirlsskirtsskootersskorts { get; set; }

    // Occurs 9
    [JsonPropertyName("Musical Boxes & Figurines")]
    public string musicalboxesfigurines { get; set; }

    // Occurs 9
    [JsonPropertyName("Sports Fan Skullies & Beanies")]
    public string sportsfanskulliesbeanies { get; set; }

    // Occurs 9
    [JsonPropertyName("Sewing Patterns & Templates")]
    public string sewingpatternstemplates { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Body Piercing Tapers")]
    public string womensbodypiercingtapers { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Charms & Charm Bracelets")]
    public string womenscharmscharmbracelets { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Body Jewelry")]
    public string womensbodyjewelry { get; set; }

    // Occurs 9
    [JsonPropertyName("Men's Rugby Jerseys")]
    public string mensrugbyjerseys { get; set; }

    // Occurs 9
    [JsonPropertyName("Girls' Novelty T-Shirts")]
    public string girlsnoveltytshirts { get; set; }

    // Occurs 9
    [JsonPropertyName("Men's Pajama Bottoms")]
    public string menspajamabottoms { get; set; }

    // Occurs 9
    [JsonPropertyName("Girls' Skirts")]
    public string girlsskirts { get; set; }

    // Occurs 9
    [JsonPropertyName("Boys' Cold Weather Hats & Caps")]
    public string boyscoldweatherhatscaps { get; set; }

    // Occurs 9
    [JsonPropertyName("Unique Gifts")]
    public string uniquegifts { get; set; }

    // Occurs 9
    [JsonPropertyName("Decorative Hanging Ornaments")]
    public string decorativehangingornaments { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Body Piercing Screws")]
    public string womensbodypiercingscrews { get; set; }

    // Occurs 9
    [JsonPropertyName("Men's Football Pants")]
    public string mensfootballpants { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Costume Headbands")]
    public string womenscostumeheadbands { get; set; }

    // Occurs 9
    [JsonPropertyName("Purse Making Supplies")]
    public string pursemakingsupplies { get; set; }

    // Occurs 9
    [JsonPropertyName("Girls' Casual & Dress Socks")]
    public string girlscasualdresssocks { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Scarves & Wraps")]
    public string womensscarveswraps { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Dance Dresses")]
    public string womensdancedresses { get; set; }

    // Occurs 9
    [JsonPropertyName("Reusable Lunch Bags")]
    public string reusablelunchbags { get; set; }

    // Occurs 9
    [JsonPropertyName("Baby Boys' Shorts Sets")]
    public string babyboysshortssets { get; set; }

    // Occurs 9
    [JsonPropertyName("Baby Girls' Clothing & Shoes")]
    public string babygirlsclothingshoes { get; set; }

    // Occurs 9
    [JsonPropertyName("Patio Umbrellas")]
    public string patioumbrellas { get; set; }

    // Occurs 9
    [JsonPropertyName("Exercise & Fitness Accessories")]
    public string exercisefitnessaccessories { get; set; }

    // Occurs 9
    [JsonPropertyName("Baby Boys' Clothing & Shoes")]
    public string babyboysclothingshoes { get; set; }

    // Occurs 9
    [JsonPropertyName("Women's Sweater Vests")]
    public string womenssweatervests { get; set; }

    // Occurs 8
    [JsonPropertyName("Boys' Novelty Hoodies")]
    public string boysnoveltyhoodies { get; set; }

    // Occurs 8
    [JsonPropertyName("Men's Novelty Bow Ties")]
    public string mensnoveltybowties { get; set; }

    // Occurs 8
    [JsonPropertyName("Girls' Cold Weather Accessories Sets")]
    public string girlscoldweatheraccessoriessets { get; set; }

    // Occurs 8
    [JsonPropertyName("Martial Arts Swords")]
    public string martialartsswords { get; set; }

    // Occurs 8
    [JsonPropertyName("Men's Body Piercing Rings")]
    public string mensbodypiercingrings { get; set; }

    // Occurs 8
    [JsonPropertyName("Women's Costume Bodysuits")]
    public string womenscostumebodysuits { get; set; }

    // Occurs 8
    [JsonPropertyName("Women's Gymnastics Leotards")]
    public string womensgymnasticsleotards { get; set; }

    // Occurs 8
    [JsonPropertyName("Baby Girls' One Piece Swimsuits")]
    public string babygirlsonepieceswimsuits { get; set; }

    // Occurs 8
    [JsonPropertyName("Equestrian Equipment")]
    public string equestrianequipment { get; set; }

    // Occurs 8
    [JsonPropertyName("Badge Holders")]
    public string badgeholders { get; set; }

    // Occurs 8
    [JsonPropertyName("Women's Sunglasses & Eyewear Accessories")]
    public string womenssunglasseseyewearaccessories { get; set; }

    // Occurs 8
    [JsonPropertyName("Internal Frame Hiking Backpacks")]
    public string internalframehikingbackpacks { get; set; }

    // Occurs 8
    [JsonPropertyName("Boys' Athletic Swimwear Briefs")]
    public string boysathleticswimwearbriefs { get; set; }

    // Occurs 8
    [JsonPropertyName("Squash Footwear")]
    public string squashfootwear { get; set; }

    // Occurs 8
    [JsonPropertyName("Posters & Prints")]
    public string postersprints { get; set; }

    // Occurs 8
    [JsonPropertyName("Walking Canes")]
    public string walkingcanes { get; set; }

    // Occurs 8
    [JsonPropertyName("Boys' Softball Jerseys")]
    public string boyssoftballjerseys { get; set; }

    // Occurs 8
    [JsonPropertyName("Women's Cocktail Dresses")]
    public string womenscocktaildresses { get; set; }

    // Occurs 8
    [JsonPropertyName("Boys' Pants")]
    public string boyspants { get; set; }

    // Occurs 8
    [JsonPropertyName("Girls' Outerwear Jackets & Coats")]
    public string girlsouterwearjacketscoats { get; set; }

    // Occurs 8
    [JsonPropertyName("Women's Board Shorts")]
    public string womensboardshorts { get; set; }

    // Occurs 8
    [JsonPropertyName("Baby Boys' Sandals")]
    public string babyboyssandals { get; set; }

    // Occurs 8
    [JsonPropertyName("Women's Casual Shorts")]
    public string womenscasualshorts { get; set; }

    // Occurs 8
    [JsonPropertyName("Boys' Pendants")]
    public string boyspendants { get; set; }

    // Occurs 8
    [JsonPropertyName("Boys' Tuxedos")]
    public string boystuxedos { get; set; }

    // Occurs 8
    [JsonPropertyName("Men's Body Piercing Screws")]
    public string mensbodypiercingscrews { get; set; }

    // Occurs 8
    [JsonPropertyName("Girls' Pajama Sets")]
    public string girlspajamasets { get; set; }

    // Occurs 8
    [JsonPropertyName("Girls' Running Headwear")]
    public string girlsrunningheadwear { get; set; }

    // Occurs 8
    [JsonPropertyName("Men's Shapewear")]
    public string mensshapewear { get; set; }

    // Occurs 8
    [JsonPropertyName("Bridal Accessories")]
    public string bridalaccessories { get; set; }

    // Occurs 8
    [JsonPropertyName("Girls' Athletic One-Piece Swimsuits")]
    public string girlsathleticonepieceswimsuits { get; set; }

    // Occurs 8
    [JsonPropertyName("Men's Work & Safety Footwear")]
    public string mensworksafetyfootwear { get; set; }

    // Occurs 8
    [JsonPropertyName("Shoe, Jewelry & Watch Accessories")]
    public string shoejewelrywatchaccessories { get; set; }

    // Occurs 8
    [JsonPropertyName("Men's Suit Vests")]
    public string menssuitvests { get; set; }

    // Occurs 8
    [JsonPropertyName("Bowling Shoe Covers")]
    public string bowlingshoecovers { get; set; }

    // Occurs 8
    [JsonPropertyName("Baby Girls' Shorts")]
    public string babygirlsshorts { get; set; }

    // Occurs 8
    [JsonPropertyName("Index Card Filing Products")]
    public string indexcardfilingproducts { get; set; }

    // Occurs 8
    [JsonPropertyName("Baby Girls' Pants")]
    public string babygirlspants { get; set; }

    // Occurs 8
    [JsonPropertyName("Men's Activewear Sweaters")]
    public string mensactivewearsweaters { get; set; }

    // Occurs 8
    [JsonPropertyName("Girls' Novelty Buttons & Pins")]
    public string girlsnoveltybuttonspins { get; set; }

    // Occurs 8
    [JsonPropertyName("Adhesive Bras")]
    public string adhesivebras { get; set; }

    // Occurs 8
    [JsonPropertyName("Exercise & Fitness Equipment")]
    public string exercisefitnessequipment { get; set; }

    // Occurs 8
    [JsonPropertyName("Men's Wool Jackets & Coats")]
    public string menswooljacketscoats { get; set; }

    // Occurs 8
    [JsonPropertyName("Women's Suiting")]
    public string womenssuiting { get; set; }

    // Occurs 8
    [JsonPropertyName("Men's Sports Compression Socks")]
    public string menssportscompressionsocks { get; set; }

    // Occurs 8
    [JsonPropertyName("Girls' Equestrian Sport Boots")]
    public string girlsequestriansportboots { get; set; }

    // Occurs 8
    [JsonPropertyName("Forms, Recordkeeping & Money Handling")]
    public string formsrecordkeepingmoneyhandling { get; set; }

    // Occurs 8
    [JsonPropertyName("Golf Equipment")]
    public string golfequipment { get; set; }

    // Occurs 8
    [JsonPropertyName("Men's Body Piercing Barbells")]
    public string mensbodypiercingbarbells { get; set; }

    // Occurs 8
    [JsonPropertyName("Baby Boys' Shorts")]
    public string babyboysshorts { get; set; }

    // Occurs 8
    [JsonPropertyName("Toddler Safety Harnesses & Leashes")]
    public string toddlersafetyharnessesleashes { get; set; }

    // Occurs 8
    [JsonPropertyName("Baby Boys' Athletic & Outdoor Shoes")]
    public string babyboysathleticoutdoorshoes { get; set; }

    // Occurs 8
    [JsonPropertyName("Girls' Hiking Clothing")]
    public string girlshikingclothing { get; set; }

    // Occurs 8
    [JsonPropertyName("Gift Wrap Bags")]
    public string giftwrapbags { get; set; }

    // Occurs 7
    [JsonPropertyName("Boys' Volleyball Clothing")]
    public string boysvolleyballclothing { get; set; }

    // Occurs 7
    [JsonPropertyName("Water Bottles")]
    public string waterbottles { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Ice Skating Clothing")]
    public string womensiceskatingclothing { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Exotic Lingerie Sets")]
    public string womensexoticlingeriesets { get; set; }

    // Occurs 7
    [JsonPropertyName("Men's Work Utility & Safety Apparel")]
    public string mensworkutilitysafetyapparel { get; set; }

    // Occurs 7
    [JsonPropertyName("Jewelry Making Chains")]
    public string jewelrymakingchains { get; set; }

    // Occurs 7
    [JsonPropertyName("Boys' Outerwear Jackets")]
    public string boysouterwearjackets { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's ID Bracelets")]
    public string womensidbracelets { get; set; }

    // Occurs 7
    [JsonPropertyName("Baby Girls' Sweaters")]
    public string babygirlssweaters { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Novelty One-Piece Pajamas")]
    public string womensnoveltyonepiecepajamas { get; set; }

    // Occurs 7
    [JsonPropertyName("Airsoft Masks")]
    public string airsoftmasks { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Volleyball Jerseys")]
    public string womensvolleyballjerseys { get; set; }

    // Occurs 7
    [JsonPropertyName("Girls' Novelty Socks")]
    public string girlsnoveltysocks { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Soccer Shoes")]
    public string womenssoccershoes { get; set; }

    // Occurs 7
    [JsonPropertyName("Cell Phone Basic Cases")]
    public string cellphonebasiccases { get; set; }

    // Occurs 7
    [JsonPropertyName("Boys' Novelty One-Piece Pajamas")]
    public string boysnoveltyonepiecepajamas { get; set; }

    // Occurs 7
    [JsonPropertyName("Boys' Hiking & Outdoor Recreation Gloves")]
    public string boyshikingoutdoorrecreationgloves { get; set; }

    // Occurs 7
    [JsonPropertyName("Girls' Dance Skirts")]
    public string girlsdanceskirts { get; set; }

    // Occurs 7
    [JsonPropertyName("Boys' Activewear Polos")]
    public string boysactivewearpolos { get; set; }

    // Occurs 7
    [JsonPropertyName("Kids' Party Favor Sets")]
    public string kidspartyfavorsets { get; set; }

    // Occurs 7
    [JsonPropertyName("Baby Girls' Snow Pants & Bibs")]
    public string babygirlssnowpantsbibs { get; set; }

    // Occurs 7
    [JsonPropertyName("Baby Girls' Hoodies & Activewear")]
    public string babygirlshoodiesactivewear { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Exotic Apparel Accessories")]
    public string womensexoticapparelaccessories { get; set; }

    // Occurs 7
    [JsonPropertyName("Safety Work Gloves")]
    public string safetyworkgloves { get; set; }

    // Occurs 7
    [JsonPropertyName("Artificial Flowers")]
    public string artificialflowers { get; set; }

    // Occurs 7
    [JsonPropertyName("Girls' Lacrosse Clothing")]
    public string girlslacrosseclothing { get; set; }

    // Occurs 7
    [JsonPropertyName("Decorative Trays")]
    public string decorativetrays { get; set; }

    // Occurs 7
    [JsonPropertyName("Baby Boys' Undershirts")]
    public string babyboysundershirts { get; set; }

    // Occurs 7
    [JsonPropertyName("Space Saver Bags")]
    public string spacesaverbags { get; set; }

    // Occurs 7
    [JsonPropertyName("Dance Apparel")]
    public string danceapparel { get; set; }

    // Occurs 7
    [JsonPropertyName("Accessory & Keychain Carabiners")]
    public string accessorykeychaincarabiners { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Skirts")]
    public string womensskirts { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Body Piercing Tunnels")]
    public string womensbodypiercingtunnels { get; set; }

    // Occurs 7
    [JsonPropertyName("Men's Thong Underwear")]
    public string mensthongunderwear { get; set; }

    // Occurs 7
    [JsonPropertyName("Girls' Bowling Shoes")]
    public string girlsbowlingshoes { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Eternity Rings")]
    public string womenseternityrings { get; set; }

    // Occurs 7
    [JsonPropertyName("Men's Costumes & Cosplay Apparel")]
    public string menscostumescosplayapparel { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Anniversary Rings")]
    public string womensanniversaryrings { get; set; }

    // Occurs 7
    [JsonPropertyName("Sports Fan Sweatshirts & Hoodies")]
    public string sportsfansweatshirtshoodies { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Yoga Shirts")]
    public string womensyogashirts { get; set; }

    // Occurs 7
    [JsonPropertyName("Men's Snowboarding Jackets")]
    public string menssnowboardingjackets { get; set; }

    // Occurs 7
    [JsonPropertyName("Embroidered Appliqué Patches")]
    public string embroideredappliquépatches { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Softball Pants")]
    public string womenssoftballpants { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Athletic Jackets")]
    public string womensathleticjackets { get; set; }

    // Occurs 7
    [JsonPropertyName("Sports Fan Bags, Packs & Accessories")]
    public string sportsfanbagspacksaccessories { get; set; }

    // Occurs 7
    [JsonPropertyName("Calf & Shin Supports")]
    public string calfshinsupports { get; set; }

    // Occurs 7
    [JsonPropertyName("Jewelry Making Tools & Accessories")]
    public string jewelrymakingtoolsaccessories { get; set; }

    // Occurs 7
    [JsonPropertyName("Girls' Football Jerseys")]
    public string girlsfootballjerseys { get; set; }

    // Occurs 7
    [JsonPropertyName("Sports Fan Footwear")]
    public string sportsfanfootwear { get; set; }

    // Occurs 7
    [JsonPropertyName("Men's Sweater Vests")]
    public string menssweatervests { get; set; }

    // Occurs 7
    [JsonPropertyName("Women's Cycling Underwear")]
    public string womenscyclingunderwear { get; set; }

    // Occurs 7
    [JsonPropertyName("Men's ID Bracelets")]
    public string mensidbracelets { get; set; }

    // Occurs 7
    [JsonPropertyName("Men's Mountaineering Boots")]
    public string mensmountaineeringboots { get; set; }

    // Occurs 7
    [JsonPropertyName("Baby Girls' Leggings")]
    public string babygirlsleggings { get; set; }

    // Occurs 7
    [JsonPropertyName("Girls' Athletic Shirts & Tees")]
    public string girlsathleticshirtstees { get; set; }

    // Occurs 7
    [JsonPropertyName("Boys' Snow Boots")]
    public string boyssnowboots { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Compression Shirts")]
    public string womenscompressionshirts { get; set; }

    // Occurs 6
    [JsonPropertyName("Winter Sports Equipment")]
    public string wintersportsequipment { get; set; }

    // Occurs 6
    [JsonPropertyName("Tactical Bag Accessories")]
    public string tacticalbagaccessories { get; set; }

    // Occurs 6
    [JsonPropertyName("Sports Fan Sweaters")]
    public string sportsfansweaters { get; set; }

    // Occurs 6
    [JsonPropertyName("Baby Boys' Boots")]
    public string babyboysboots { get; set; }

    // Occurs 6
    [JsonPropertyName("Baby Girls' Undershirts")]
    public string babygirlsundershirts { get; set; }

    // Occurs 6
    [JsonPropertyName("Girls' Outerwear Jackets")]
    public string girlsouterwearjackets { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Exotic Dresses")]
    public string womensexoticdresses { get; set; }

    // Occurs 6
    [JsonPropertyName("Boys' Basketball Clothing")]
    public string boysbasketballclothing { get; set; }

    // Occurs 6
    [JsonPropertyName("Boys' Hiking Pants")]
    public string boyshikingpants { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Novelty Polo Shirts")]
    public string womensnoveltypoloshirts { get; set; }

    // Occurs 6
    [JsonPropertyName("Baby Girls' Rash Guard Sets")]
    public string babygirlsrashguardsets { get; set; }

    // Occurs 6
    [JsonPropertyName("Baby & Toddler Toys")]
    public string babytoddlertoys { get; set; }

    // Occurs 6
    [JsonPropertyName("Boys' Briefs Underwear")]
    public string boysbriefsunderwear { get; set; }

    // Occurs 6
    [JsonPropertyName("Collectible Figurines")]
    public string collectiblefigurines { get; set; }

    // Occurs 6
    [JsonPropertyName("Boys' Suits")]
    public string boyssuits { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Paddle Fans")]
    public string womenspaddlefans { get; set; }

    // Occurs 6
    [JsonPropertyName("Boys' Athletic Base Layers")]
    public string boysathleticbaselayers { get; set; }

    // Occurs 6
    [JsonPropertyName("Girls' Special Occasion Gloves")]
    public string girlsspecialoccasiongloves { get; set; }

    // Occurs 6
    [JsonPropertyName("Boys' Compression Shirts")]
    public string boyscompressionshirts { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Costume Tops & Corsets")]
    public string womenscostumetopscorsets { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Clutches & Evening Handbags")]
    public string womensclutcheseveninghandbags { get; set; }

    // Occurs 6
    [JsonPropertyName("Shoe Insoles")]
    public string shoeinsoles { get; set; }

    // Occurs 6
    [JsonPropertyName("Safety Footwear")]
    public string safetyfootwear { get; set; }

    // Occurs 6
    [JsonPropertyName("Kids' Multi-Item Party Favor Packs")]
    public string kidsmultiitempartyfavorpacks { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Handbags & Shoulder Bags")]
    public string menshandbagsshoulderbags { get; set; }

    // Occurs 6
    [JsonPropertyName("Kids' Home Store")]
    public string kidshomestore { get; set; }

    // Occurs 6
    [JsonPropertyName("Medical Compression Socks")]
    public string medicalcompressionsocks { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Socks & Hosiery")]
    public string menssockshosiery { get; set; }

    // Occurs 6
    [JsonPropertyName("Musical Instruments")]
    public string musicalinstruments { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Sunglasses & Eyewear Accessories")]
    public string menssunglasseseyewearaccessories { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Lingerie, Sleep & Lounge")]
    public string womenslingeriesleeplounge { get; set; }

    // Occurs 6
    [JsonPropertyName("Baby Boys' Outerwear Jackets")]
    public string babyboysouterwearjackets { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Snowboarding Pants")]
    public string menssnowboardingpants { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Active & Performance Jackets")]
    public string mensactiveperformancejackets { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Tankini Sets")]
    public string womenstankinisets { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Baseball Clothing")]
    public string womensbaseballclothing { get; set; }

    // Occurs 6
    [JsonPropertyName("Boys' Activewear")]
    public string boysactivewear { get; set; }

    // Occurs 6
    [JsonPropertyName("Hydration Packs")]
    public string hydrationpacks { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Athletic Swimwear")]
    public string womensathleticswimwear { get; set; }

    // Occurs 6
    [JsonPropertyName("Boys' Novelty Baseball Caps")]
    public string boysnoveltybaseballcaps { get; set; }

    // Occurs 6
    [JsonPropertyName("Boys' Basketball Shoes")]
    public string boysbasketballshoes { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Novelty Belt Buckles")]
    public string womensnoveltybeltbuckles { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Work & Safety Clothing")]
    public string mensworksafetyclothing { get; set; }

    // Occurs 6
    [JsonPropertyName("Sports Fan Bracelets")]
    public string sportsfanbracelets { get; set; }

    // Occurs 6
    [JsonPropertyName("Karate Suit Bottoms")]
    public string karatesuitbottoms { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Body Piercing Plugs")]
    public string womensbodypiercingplugs { get; set; }

    // Occurs 6
    [JsonPropertyName("Baby Girls' Shoes")]
    public string babygirlsshoes { get; set; }

    // Occurs 6
    [JsonPropertyName("Baby Boys' Sweaters")]
    public string babyboyssweaters { get; set; }

    // Occurs 6
    [JsonPropertyName("Personal Protective Equipment & Safety Gear")]
    public string personalprotectiveequipmentsafetygear { get; set; }

    // Occurs 6
    [JsonPropertyName("Baby Safety Products")]
    public string babysafetyproducts { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Bras")]
    public string womensbras { get; set; }

    // Occurs 6
    [JsonPropertyName("Boys' Fashion Hoodies & Sweatshirts")]
    public string boysfashionhoodiessweatshirts { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Special Occasion Accessories")]
    public string womensspecialoccasionaccessories { get; set; }

    // Occurs 6
    [JsonPropertyName("Preschool Toys")]
    public string preschooltoys { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Lingerie")]
    public string womenslingerie { get; set; }

    // Occurs 6
    [JsonPropertyName("Girls' Dance Dresses")]
    public string girlsdancedresses { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Card & ID Cases")]
    public string womenscardidcases { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Workout Top & Bottom Sets")]
    public string womensworkouttopbottomsets { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Pajama Sets")]
    public string menspajamasets { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Novelty Boxer Briefs")]
    public string mensnoveltyboxerbriefs { get; set; }

    // Occurs 6
    [JsonPropertyName("Boys' Novelty Gloves & Mittens")]
    public string boysnoveltyglovesmittens { get; set; }

    // Occurs 6
    [JsonPropertyName("Girls' Running Shoes")]
    public string girlsrunningshoes { get; set; }

    // Occurs 6
    [JsonPropertyName("Unique Toys")]
    public string uniquetoys { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Dress Pants")]
    public string mensdresspants { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Medical Lab Coats")]
    public string womensmedicallabcoats { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Novelty Swimwear")]
    public string mensnoveltyswimwear { get; set; }

    // Occurs 6
    [JsonPropertyName("Work Utility & Safety Clothing")]
    public string workutilitysafetyclothing { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Swim Trunks")]
    public string mensswimtrunks { get; set; }

    // Occurs 6
    [JsonPropertyName("Mountaineering & Ice Climbing Crampons")]
    public string mountaineeringiceclimbingcrampons { get; set; }

    // Occurs 6
    [JsonPropertyName("Girls' Golf Shirts")]
    public string girlsgolfshirts { get; set; }

    // Occurs 6
    [JsonPropertyName("Snowshoes")]
    public string snowshoes { get; set; }

    // Occurs 6
    [JsonPropertyName("Boys' Novelty Socks")]
    public string boysnoveltysocks { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Skateboard Shoes")]
    public string womensskateboardshoes { get; set; }

    // Occurs 6
    [JsonPropertyName("Girls' Cricket Clothing")]
    public string girlscricketclothing { get; set; }

    // Occurs 6
    [JsonPropertyName("Food Service Uniforms")]
    public string foodserviceuniforms { get; set; }

    // Occurs 6
    [JsonPropertyName("Women's Denim Shorts")]
    public string womensdenimshorts { get; set; }

    // Occurs 6
    [JsonPropertyName("Baby Girls' Mary Jane Flats")]
    public string babygirlsmaryjaneflats { get; set; }

    // Occurs 6
    [JsonPropertyName("Hiking Backpacks, Bags & Accessories")]
    public string hikingbackpacksbagsaccessories { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Rain Footwear")]
    public string mensrainfootwear { get; set; }

    // Occurs 6
    [JsonPropertyName("Kids' Equestrian Tournament Shirts")]
    public string kidsequestriantournamentshirts { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Activewear Undershirts")]
    public string mensactivewearundershirts { get; set; }

    // Occurs 6
    [JsonPropertyName("Baby Boys' Shoes")]
    public string babyboysshoes { get; set; }

    // Occurs 6
    [JsonPropertyName("Baby Boys' Oxfords & Loafers")]
    public string babyboysoxfordsloafers { get; set; }

    // Occurs 6
    [JsonPropertyName("Climate Pledge Friendly: Electronics")]
    public string climatepledgefriendlyelectronics { get; set; }

    // Occurs 6
    [JsonPropertyName("Cloth Diaper Covers")]
    public string clothdiapercovers { get; set; }

    // Occurs 6
    [JsonPropertyName("Men's Liner & Ankle Socks")]
    public string menslineranklesocks { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Pants")]
    public string menspants { get; set; }

    // Occurs 5
    [JsonPropertyName("Life Hacks")]
    public string lifehacks { get; set; }

    // Occurs 5
    [JsonPropertyName("Display Stands")]
    public string displaystands { get; set; }

    // Occurs 5
    [JsonPropertyName("Women's Socks & Hosiery")]
    public string womenssockshosiery { get; set; }

    // Occurs 5
    [JsonPropertyName("Boys' Wallets & Money Organizers")]
    public string boyswalletsmoneyorganizers { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Bomber Hats")]
    public string mensbomberhats { get; set; }

    // Occurs 5
    [JsonPropertyName("Powersports Protective Jackets")]
    public string powersportsprotectivejackets { get; set; }

    // Occurs 5
    [JsonPropertyName("Camping Coolers")]
    public string campingcoolers { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Work & Safety Boots")]
    public string mensworksafetyboots { get; set; }

    // Occurs 5
    [JsonPropertyName("Babies' Costumes")]
    public string babiescostumes { get; set; }

    // Occurs 5
    [JsonPropertyName("Sports Fan Charms")]
    public string sportsfancharms { get; set; }

    // Occurs 5
    [JsonPropertyName("Women's Running Jackets")]
    public string womensrunningjackets { get; set; }

    // Occurs 5
    [JsonPropertyName("Kung Fu Suit Bottoms")]
    public string kungfusuitbottoms { get; set; }

    // Occurs 5
    [JsonPropertyName("Boys' Tank Top Shirts")]
    public string boystanktopshirts { get; set; }

    // Occurs 5
    [JsonPropertyName("Women's Ice Skating Jackets")]
    public string womensiceskatingjackets { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Denim Shorts")]
    public string mensdenimshorts { get; set; }

    // Occurs 5
    [JsonPropertyName("Sports Fan Tank Tops")]
    public string sportsfantanktops { get; set; }

    // Occurs 5
    [JsonPropertyName("Motorcycle & Powersports")]
    public string motorcyclepowersports { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Boxing Trunks")]
    public string mensboxingtrunks { get; set; }

    // Occurs 5
    [JsonPropertyName("Office & School Supplies")]
    public string officeschoolsupplies { get; set; }

    // Occurs 5
    [JsonPropertyName("Tennis Bags")]
    public string tennisbags { get; set; }

    // Occurs 5
    [JsonPropertyName("Jewelry Making Display & Packaging Supplies")]
    public string jewelrymakingdisplaypackagingsupplies { get; set; }

    // Occurs 5
    [JsonPropertyName("Sports Fan Underwear")]
    public string sportsfanunderwear { get; set; }

    // Occurs 5
    [JsonPropertyName("Sports Fan Clothing")]
    public string sportsfanclothing { get; set; }

    // Occurs 5
    [JsonPropertyName("Indiegogo")]
    public string indiegogo { get; set; }

    // Occurs 5
    [JsonPropertyName("Boys' Pajama Sets")]
    public string boyspajamasets { get; set; }

    // Occurs 5
    [JsonPropertyName("Women's Cycling Vests")]
    public string womenscyclingvests { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Flat Front Shorts")]
    public string mensflatfrontshorts { get; set; }

    // Occurs 5
    [JsonPropertyName("Shark Tank Collection")]
    public string sharktankcollection { get; set; }

    // Occurs 5
    [JsonPropertyName("Girls' Skiing Bibs")]
    public string girlsskiingbibs { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Novelty Shirts")]
    public string mensnoveltyshirts { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Cycling Underwear")]
    public string menscyclingunderwear { get; set; }

    // Occurs 5
    [JsonPropertyName("Watch Bands")]
    public string watchbands { get; set; }

    // Occurs 5
    [JsonPropertyName("Women's Running Shoes")]
    public string womensrunningshoes { get; set; }

    // Occurs 5
    [JsonPropertyName("Boys' Activewear Button-Down Shirts")]
    public string boysactivewearbuttondownshirts { get; set; }

    // Occurs 5
    [JsonPropertyName("Girls' Skirt Sets")]
    public string girlsskirtsets { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Hiking & Outdoor Recreation Gloves")]
    public string menshikingoutdoorrecreationgloves { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Cardigan Sweaters")]
    public string menscardigansweaters { get; set; }

    // Occurs 5
    [JsonPropertyName("Women's Athletic Clothing Sets")]
    public string womensathleticclothingsets { get; set; }

    // Occurs 5
    [JsonPropertyName("Girls' Earmuffs")]
    public string girlsearmuffs { get; set; }

    // Occurs 5
    [JsonPropertyName("Boys' Basketball Jerseys")]
    public string boysbasketballjerseys { get; set; }

    // Occurs 5
    [JsonPropertyName("Girls' Novelty Hoodies")]
    public string girlsnoveltyhoodies { get; set; }

    // Occurs 5
    [JsonPropertyName("Games & Accessories")]
    public string gamesaccessories { get; set; }

    // Occurs 5
    [JsonPropertyName("Potholders & Oven Mitts")]
    public string potholdersovenmitts { get; set; }

    // Occurs 5
    [JsonPropertyName("Baby Girls' Training Underpants")]
    public string babygirlstrainingunderpants { get; set; }

    // Occurs 5
    [JsonPropertyName("Fitness Technology")]
    public string fitnesstechnology { get; set; }

    // Occurs 5
    [JsonPropertyName("Women's Weekend Skirts")]
    public string womensweekendskirts { get; set; }

    // Occurs 5
    [JsonPropertyName("Hunting & Shooting Accessories")]
    public string huntingshootingaccessories { get; set; }

    // Occurs 5
    [JsonPropertyName("Girls' Anklets")]
    public string girlsanklets { get; set; }

    // Occurs 5
    [JsonPropertyName("Women's Shapewear Tops")]
    public string womensshapeweartops { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Faux Body Piercing Jewelry")]
    public string mensfauxbodypiercingjewelry { get; set; }

    // Occurs 5
    [JsonPropertyName("Yoga Clothing")]
    public string yogaclothing { get; set; }

    // Occurs 5
    [JsonPropertyName("Safety Jackets")]
    public string safetyjackets { get; set; }

    // Occurs 5
    [JsonPropertyName("Girls' Blouses & Button-Down Shirts")]
    public string girlsblousesbuttondownshirts { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Activewear Undershorts")]
    public string mensactivewearundershorts { get; set; }

    // Occurs 5
    [JsonPropertyName("Sports Fan Duffel Bags")]
    public string sportsfanduffelbags { get; set; }

    // Occurs 5
    [JsonPropertyName("Hiking Daypacks & Casual Bags")]
    public string hikingdaypackscasualbags { get; set; }

    // Occurs 5
    [JsonPropertyName("Baby Boys' Overalls")]
    public string babyboysoveralls { get; set; }

    // Occurs 5
    [JsonPropertyName("Men's Bolo Ties")]
    public string mensboloties { get; set; }

    // Occurs 5
    [JsonPropertyName("Women's Hiking Socks")]
    public string womenshikingsocks { get; set; }

    // Occurs 5
    [JsonPropertyName("Women’s Garter Belts")]
    public string womensgarterbelts { get; set; }

    // Occurs 5
    [JsonPropertyName("Boys' Compression Shorts")]
    public string boyscompressionshorts { get; set; }

    // Occurs 5
    [JsonPropertyName("Laptop Backpacks")]
    public string laptopbackpacks { get; set; }

    // Occurs 5
    [JsonPropertyName("Boys' Cold Weather Accessories Sets")]
    public string boyscoldweatheraccessoriessets { get; set; }

    // Occurs 5
    [JsonPropertyName("Women's Wedding & Engagement Rings")]
    public string womensweddingengagementrings { get; set; }

    // Occurs 5
    [JsonPropertyName("Women's Boy Short Panties")]
    public string womensboyshortpanties { get; set; }

    // Occurs 5
    [JsonPropertyName("Camping Hand Warmers")]
    public string campinghandwarmers { get; set; }

    // Occurs 5
    [JsonPropertyName("Braces, Splints & Supports")]
    public string bracessplintssupports { get; set; }

    // Occurs 5
    [JsonPropertyName("Rhinestone & Sequin Embellishments")]
    public string rhinestonesequinembellishments { get; set; }

    // Occurs 5
    [JsonPropertyName("Outdoor Décor")]
    public string outdoordécor { get; set; }

    // Occurs 5
    [JsonPropertyName("Girls' Panties")]
    public string girlspanties { get; set; }

    // Occurs 4
    [JsonPropertyName("Boys' Baseball Clothing")]
    public string boysbaseballclothing { get; set; }

    // Occurs 4
    [JsonPropertyName("Sports Fan Handbags & Purses")]
    public string sportsfanhandbagspurses { get; set; }

    // Occurs 4
    [JsonPropertyName("Baby Boys' Snow & Rainwear")]
    public string babyboyssnowrainwear { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's Cycling Compression Shorts")]
    public string womenscyclingcompressionshorts { get; set; }

    // Occurs 4
    [JsonPropertyName("Hunting & Fishing Products")]
    public string huntingfishingproducts { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's G-Strings & Thongs")]
    public string womensgstringsthongs { get; set; }

    // Occurs 4
    [JsonPropertyName("Sewing Buttons")]
    public string sewingbuttons { get; set; }

    // Occurs 4
    [JsonPropertyName("Men's Thermal Underwear")]
    public string mensthermalunderwear { get; set; }

    // Occurs 4
    [JsonPropertyName("Men's Ties, Cummerbunds & Pocket Squares")]
    public string menstiescummerbundspocketsquares { get; set; }

    // Occurs 4
    [JsonPropertyName("Arts & Crafts Supplies")]
    public string artscraftssupplies { get; set; }

    // Occurs 4
    [JsonPropertyName("Boys' Soccer Clothing")]
    public string boyssoccerclothing { get; set; }

    // Occurs 4
    [JsonPropertyName("Gags & Practical Joke Toys")]
    public string gagspracticaljoketoys { get; set; }

    // Occurs 4
    [JsonPropertyName("Camera Cases")]
    public string cameracases { get; set; }

    // Occurs 4
    [JsonPropertyName("Boys' Boxer Briefs")]
    public string boysboxerbriefs { get; set; }

    // Occurs 4
    [JsonPropertyName("Girls' First Communion Veils")]
    public string girlsfirstcommunionveils { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's Novelty Jackets & Coats")]
    public string womensnoveltyjacketscoats { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's Trench Coats")]
    public string womenstrenchcoats { get; set; }

    // Occurs 4
    [JsonPropertyName("Boys' Outerwear Jackets & Coats")]
    public string boysouterwearjacketscoats { get; set; }

    // Occurs 4
    [JsonPropertyName("Baseball & Softball Equipment Bags")]
    public string baseballsoftballequipmentbags { get; set; }

    // Occurs 4
    [JsonPropertyName("Men's Costume Headbands")]
    public string menscostumeheadbands { get; set; }

    // Occurs 4
    [JsonPropertyName("Baby Boys' Rash Guard Shirts")]
    public string babyboysrashguardshirts { get; set; }

    // Occurs 4
    [JsonPropertyName("Window Treatment Valances")]
    public string windowtreatmentvalances { get; set; }

    // Occurs 4
    [JsonPropertyName("Boys' Pant Sets")]
    public string boyspantsets { get; set; }

    // Occurs 4
    [JsonPropertyName("Boys' Athletic Swimwear Jammers")]
    public string boysathleticswimwearjammers { get; set; }

    // Occurs 4
    [JsonPropertyName("Sports Fan Necklaces & Pendants")]
    public string sportsfannecklacespendants { get; set; }

    // Occurs 4
    [JsonPropertyName("Baby Girls' Fleece Jackets & Coats")]
    public string babygirlsfleecejacketscoats { get; set; }

    // Occurs 4
    [JsonPropertyName("Swimming Goggles")]
    public string swimminggoggles { get; set; }

    // Occurs 4
    [JsonPropertyName("Men's Diving Rash Guard Shirts")]
    public string mensdivingrashguardshirts { get; set; }

    // Occurs 4
    [JsonPropertyName("Tactical Pouches")]
    public string tacticalpouches { get; set; }

    // Occurs 4
    [JsonPropertyName("Boys' Running Socks")]
    public string boysrunningsocks { get; set; }

    // Occurs 4
    [JsonPropertyName("Aprons")]
    public string aprons { get; set; }

    // Occurs 4
    [JsonPropertyName("Sports Fan Fabric")]
    public string sportsfanfabric { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's Shorts")]
    public string womensshorts { get; set; }

    // Occurs 4
    [JsonPropertyName("Maternity Coats, Jackets & Vests")]
    public string maternitycoatsjacketsvests { get; set; }

    // Occurs 4
    [JsonPropertyName("Jewelry Clasps")]
    public string jewelryclasps { get; set; }

    // Occurs 4
    [JsonPropertyName("Girls' Dance Tops")]
    public string girlsdancetops { get; set; }

    // Occurs 4
    [JsonPropertyName("Men's Swim Briefs")]
    public string mensswimbriefs { get; set; }

    // Occurs 4
    [JsonPropertyName("Video Games")]
    public string videogames { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's Uniform Dress Shoes")]
    public string womensuniformdressshoes { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's Sweaters")]
    public string womenssweaters { get; set; }

    // Occurs 4
    [JsonPropertyName("Girls' Athletic Shoes")]
    public string girlsathleticshoes { get; set; }

    // Occurs 4
    [JsonPropertyName("Doll Accessories")]
    public string dollaccessories { get; set; }

    // Occurs 4
    [JsonPropertyName("Ice Hockey Clothing")]
    public string icehockeyclothing { get; set; }

    // Occurs 4
    [JsonPropertyName("Competitive Swimwear")]
    public string competitiveswimwear { get; set; }

    // Occurs 4
    [JsonPropertyName("Baseball Clothing")]
    public string baseballclothing { get; set; }

    // Occurs 4
    [JsonPropertyName("Boys' Athletic Jackets")]
    public string boysathleticjackets { get; set; }

    // Occurs 4
    [JsonPropertyName("Men's Chef Pants")]
    public string menschefpants { get; set; }

    // Occurs 4
    [JsonPropertyName("Boys' Skiing Bibs")]
    public string boysskiingbibs { get; set; }

    // Occurs 4
    [JsonPropertyName("Girls' Athletic Swimwear")]
    public string girlsathleticswimwear { get; set; }

    // Occurs 4
    [JsonPropertyName("Girls' Soccer Clothing")]
    public string girlssoccerclothing { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's Soccer Clothing")]
    public string womenssoccerclothing { get; set; }

    // Occurs 4
    [JsonPropertyName("Home Storage & Organization")]
    public string homestorageorganization { get; set; }

    // Occurs 4
    [JsonPropertyName("Girl's Fashion Scarves")]
    public string girlsfashionscarves { get; set; }

    // Occurs 4
    [JsonPropertyName("Boys' Skiing & Snowboarding Socks")]
    public string boysskiingsnowboardingsocks { get; set; }

    // Occurs 4
    [JsonPropertyName("Hiking Footwear & Accessories")]
    public string hikingfootwearaccessories { get; set; }

    // Occurs 4
    [JsonPropertyName("Men's Compression Leg Sleeves")]
    public string menscompressionlegsleeves { get; set; }

    // Occurs 4
    [JsonPropertyName("Boys' Novelty Buttons & Pins")]
    public string boysnoveltybuttonspins { get; set; }

    // Occurs 4
    [JsonPropertyName("Tennis & Racquet Sport Equipment")]
    public string tennisracquetsportequipment { get; set; }

    // Occurs 4
    [JsonPropertyName("Men's Team Sports Shoes")]
    public string mensteamsportsshoes { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's Anoraks")]
    public string womensanoraks { get; set; }

    // Occurs 4
    [JsonPropertyName("Shoe Measuring Devices")]
    public string shoemeasuringdevices { get; set; }

    // Occurs 4
    [JsonPropertyName("Hair Drying Towels")]
    public string hairdryingtowels { get; set; }

    // Occurs 4
    [JsonPropertyName("Cycling Equipment")]
    public string cyclingequipment { get; set; }

    // Occurs 4
    [JsonPropertyName("Display Risers")]
    public string displayrisers { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's Fashion Vests")]
    public string womensfashionvests { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's Bowling Clothing")]
    public string womensbowlingclothing { get; set; }

    // Occurs 4
    [JsonPropertyName("Men's Costume Bodysuits")]
    public string menscostumebodysuits { get; set; }

    // Occurs 4
    [JsonPropertyName("Girls' Fashion Hoodies & Sweatshirts")]
    public string girlsfashionhoodiessweatshirts { get; set; }

    // Occurs 4
    [JsonPropertyName("Jiu-Jitsu Suit Bottoms")]
    public string jiujitsusuitbottoms { get; set; }

    // Occurs 4
    [JsonPropertyName("Racquetball Footwear")]
    public string racquetballfootwear { get; set; }

    // Occurs 4
    [JsonPropertyName("Girls' Tights")]
    public string girlstights { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's Lingerie Sets")]
    public string womenslingeriesets { get; set; }

    // Occurs 4
    [JsonPropertyName("Girls' Novelty Clothing")]
    public string girlsnoveltyclothing { get; set; }

    // Occurs 4
    [JsonPropertyName("Women's Dental Grills")]
    public string womensdentalgrills { get; set; }

    // Occurs 4
    [JsonPropertyName("Wearable Technology")]
    public string wearabletechnology { get; set; }

    // Occurs 4
    [JsonPropertyName("Snow Skiing Equipment")]
    public string snowskiingequipment { get; set; }

    // Occurs 4
    [JsonPropertyName("Girls' Softball Jerseys")]
    public string girlssoftballjerseys { get; set; }

    // Occurs 4
    [JsonPropertyName("Boys' Running Gloves")]
    public string boysrunninggloves { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Yoga Clothing")]
    public string boysyogaclothing { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Chef Pants")]
    public string womenschefpants { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Athletic Hoodies")]
    public string girlsathletichoodies { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Garters & Garter Belts")]
    public string womensgartersgarterbelts { get; set; }

    // Occurs 3
    [JsonPropertyName("Hunting Backpacks & Duffle Bags")]
    public string huntingbackpacksdufflebags { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Dance Shorts")]
    public string girlsdanceshorts { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Snowboarding Jackets")]
    public string girlssnowboardingjackets { get; set; }

    // Occurs 3
    [JsonPropertyName("Men's Running Clothing Accessories")]
    public string mensrunningclothingaccessories { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Tennis Clothing")]
    public string girlstennisclothing { get; set; }

    // Occurs 3
    [JsonPropertyName("Men's Motorcycle Protective Boots")]
    public string mensmotorcycleprotectiveboots { get; set; }

    // Occurs 3
    [JsonPropertyName("Lighting & Ceiling Fans")]
    public string lightingceilingfans { get; set; }

    // Occurs 3
    [JsonPropertyName("Outdoor Recreation Accessories")]
    public string outdoorrecreationaccessories { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Bathrobes")]
    public string boysbathrobes { get; set; }

    // Occurs 3
    [JsonPropertyName("Cupcake Toppers")]
    public string cupcaketoppers { get; set; }

    // Occurs 3
    [JsonPropertyName("Wearable Blankets")]
    public string wearableblankets { get; set; }

    // Occurs 3
    [JsonPropertyName("Men's Denim Jackets")]
    public string mensdenimjackets { get; set; }

    // Occurs 3
    [JsonPropertyName("Wetsuits")]
    public string wetsuits { get; set; }

    // Occurs 3
    [JsonPropertyName("Baby Boys' Slippers")]
    public string babyboysslippers { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Novelty Beanies & Knit Hats")]
    public string boysnoveltybeaniesknithats { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Costume Wigs")]
    public string boyscostumewigs { get; set; }

    // Occurs 3
    [JsonPropertyName("Plush Purses")]
    public string plushpurses { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Ice Skating Tights")]
    public string girlsiceskatingtights { get; set; }

    // Occurs 3
    [JsonPropertyName("Cell Phone Screen Protectors")]
    public string cellphonescreenprotectors { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Ice Skating Dresses")]
    public string girlsiceskatingdresses { get; set; }

    // Occurs 3
    [JsonPropertyName("Maternity")]
    public string maternity { get; set; }

    // Occurs 3
    [JsonPropertyName("Sewing Tassels")]
    public string sewingtassels { get; set; }

    // Occurs 3
    [JsonPropertyName("Baby Boys' Christening Clothing")]
    public string babyboyschristeningclothing { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Base Layers & Compression")]
    public string boysbaselayerscompression { get; set; }

    // Occurs 3
    [JsonPropertyName("Full Wetsuits")]
    public string fullwetsuits { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Track Pants")]
    public string boystrackpants { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Fleece Jackets & Coats")]
    public string boysfleecejacketscoats { get; set; }

    // Occurs 3
    [JsonPropertyName("Football Rib Protectors")]
    public string footballribprotectors { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Lingerie Accessories")]
    public string womenslingerieaccessories { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Activewear Tank Tops")]
    public string girlsactiveweartanktops { get; set; }

    // Occurs 3
    [JsonPropertyName("Men's Body Piercing Tapers")]
    public string mensbodypiercingtapers { get; set; }

    // Occurs 3
    [JsonPropertyName("4-Star Store")]
    public string _4starstore { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Wear to Work Dresses")]
    public string womensweartoworkdresses { get; set; }

    // Occurs 3
    [JsonPropertyName("Jewelry Making Findings")]
    public string jewelrymakingfindings { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Hiking & Trekking Shoes")]
    public string womenshikingtrekkingshoes { get; set; }

    // Occurs 3
    [JsonPropertyName("Hanging Hook Display Stands")]
    public string hanginghookdisplaystands { get; set; }

    // Occurs 3
    [JsonPropertyName("Powersports Riding Headwear")]
    public string powersportsridingheadwear { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Exotic Apparel")]
    public string womensexoticapparel { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Equestrian Clothing")]
    public string womensequestrianclothing { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Gymnastics Leotards")]
    public string girlsgymnasticsleotards { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Active & Performance Outerwear")]
    public string womensactiveperformanceouterwear { get; set; }

    // Occurs 3
    [JsonPropertyName("Baby Girls' Swimwear Cover-Ups")]
    public string babygirlsswimwearcoverups { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Novelty Baseball Caps")]
    public string girlsnoveltybaseballcaps { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Equestrian Sport Boots")]
    public string boysequestriansportboots { get; set; }

    // Occurs 3
    [JsonPropertyName("Men's Medical Scrub Pants")]
    public string mensmedicalscrubpants { get; set; }

    // Occurs 3
    [JsonPropertyName("Scrapbooking Stickers & Sticker Machines")]
    public string scrapbookingstickersstickermachines { get; set; }

    // Occurs 3
    [JsonPropertyName("Soccer Equipment")]
    public string soccerequipment { get; set; }

    // Occurs 3
    [JsonPropertyName("Cycling Shoe Covers")]
    public string cyclingshoecovers { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Tennis Dresses")]
    public string womenstennisdresses { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Running Headwear")]
    public string boysrunningheadwear { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Golf Clothing Accessories")]
    public string boysgolfclothingaccessories { get; set; }

    // Occurs 3
    [JsonPropertyName("Cell Phone Cases & Covers")]
    public string cellphonecasescovers { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Cycling Clothing")]
    public string girlscyclingclothing { get; set; }

    // Occurs 3
    [JsonPropertyName("Toilet Brushes & Holders")]
    public string toiletbrushesholders { get; set; }

    // Occurs 3
    [JsonPropertyName("Baby & Toddler Bedding")]
    public string babytoddlerbedding { get; set; }

    // Occurs 3
    [JsonPropertyName("Sports Fan Wallets")]
    public string sportsfanwallets { get; set; }

    // Occurs 3
    [JsonPropertyName("Baby Girls' Snow & Rainwear")]
    public string babygirlssnowrainwear { get; set; }

    // Occurs 3
    [JsonPropertyName("Cell Phone Crossbody & Lanyard Cases")]
    public string cellphonecrossbodylanyardcases { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Cuff Links")]
    public string boyscufflinks { get; set; }

    // Occurs 3
    [JsonPropertyName("Water Booties & Socks")]
    public string waterbootiessocks { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Watches")]
    public string boyswatches { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Basketball Shoes")]
    public string girlsbasketballshoes { get; set; }

    // Occurs 3
    [JsonPropertyName("Jewelry Making Jump Rings")]
    public string jewelrymakingjumprings { get; set; }

    // Occurs 3
    [JsonPropertyName("Cup Dust Safety Disposable Masks")]
    public string cupdustsafetydisposablemasks { get; set; }

    // Occurs 3
    [JsonPropertyName("Baby Girls' Bloomers")]
    public string babygirlsbloomers { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Cycling Caps")]
    public string boyscyclingcaps { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Bow Ties")]
    public string boysbowties { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Chain Necklaces")]
    public string girlschainnecklaces { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Boxing Trunks")]
    public string boysboxingtrunks { get; set; }

    // Occurs 3
    [JsonPropertyName("Laptop Bags, Cases & Sleeves")]
    public string laptopbagscasessleeves { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Snowboard Boots")]
    public string womenssnowboardboots { get; set; }

    // Occurs 3
    [JsonPropertyName("Outdoor Backpack Pack Pockets")]
    public string outdoorbackpackpackpockets { get; set; }

    // Occurs 3
    [JsonPropertyName("Football Girdles")]
    public string footballgirdles { get; set; }

    // Occurs 3
    [JsonPropertyName("Sports Fan Baby Creepers & Rompers")]
    public string sportsfanbabycreepersrompers { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Snowboarding Pants")]
    public string boyssnowboardingpants { get; set; }

    // Occurs 3
    [JsonPropertyName("Leathercraft Accessories")]
    public string leathercraftaccessories { get; set; }

    // Occurs 3
    [JsonPropertyName("Arts & Crafts Storage Boxes & Organizers")]
    public string artscraftsstorageboxesorganizers { get; set; }

    // Occurs 3
    [JsonPropertyName("Household Fans")]
    public string householdfans { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Cold Weather Accessories")]
    public string girlscoldweatheraccessories { get; set; }

    // Occurs 3
    [JsonPropertyName("Flip Cell Phone Cases")]
    public string flipcellphonecases { get; set; }

    // Occurs 3
    [JsonPropertyName("Aikido Suits")]
    public string aikidosuits { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Volleyball Jerseys")]
    public string girlsvolleyballjerseys { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Costumes & Cosplay Apparel")]
    public string womenscostumescosplayapparel { get; set; }

    // Occurs 3
    [JsonPropertyName("Baby Boys' Swim Trunks")]
    public string babyboysswimtrunks { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Softball Pants")]
    public string girlssoftballpants { get; set; }

    // Occurs 3
    [JsonPropertyName("Baby Girls' Short Sets")]
    public string babygirlsshortsets { get; set; }

    // Occurs 3
    [JsonPropertyName("Kids' Electronics")]
    public string kidselectronics { get; set; }

    // Occurs 3
    [JsonPropertyName("Pet Supplies")]
    public string petsupplies { get; set; }

    // Occurs 3
    [JsonPropertyName("Football Clothing")]
    public string footballclothing { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Tanks & Camis")]
    public string girlstankscamis { get; set; }

    // Occurs 3
    [JsonPropertyName("Volleyball Clothing")]
    public string volleyballclothing { get; set; }

    // Occurs 3
    [JsonPropertyName("Powersports Protective Vests")]
    public string powersportsprotectivevests { get; set; }

    // Occurs 3
    [JsonPropertyName("Baby Gift Sets")]
    public string babygiftsets { get; set; }

    // Occurs 3
    [JsonPropertyName("Soccer Balls")]
    public string soccerballs { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Golf Shirts")]
    public string boysgolfshirts { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Cold Weather Scarves & Wraps")]
    public string girlscoldweatherscarveswraps { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Baseball Jerseys")]
    public string womensbaseballjerseys { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Exotic Bras")]
    public string womensexoticbras { get; set; }

    // Occurs 3
    [JsonPropertyName("Basketball Shooter Sleeves")]
    public string basketballshootersleeves { get; set; }

    // Occurs 3
    [JsonPropertyName("Baby Boys' One Piece Swimsuits")]
    public string babyboysonepieceswimsuits { get; set; }

    // Occurs 3
    [JsonPropertyName("Men's Athletic Swimwear")]
    public string mensathleticswimwear { get; set; }

    // Occurs 3
    [JsonPropertyName("Diaper Changing Backpacks")]
    public string diaperchangingbackpacks { get; set; }

    // Occurs 3
    [JsonPropertyName("Men's Rugby Shoes")]
    public string mensrugbyshoes { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Dance Tights")]
    public string girlsdancetights { get; set; }

    // Occurs 3
    [JsonPropertyName("Skateboarding Equipment")]
    public string skateboardingequipment { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Athletic Kneeskins")]
    public string womensathletickneeskins { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Running Clothing")]
    public string boysrunningclothing { get; set; }

    // Occurs 3
    [JsonPropertyName("Men's Bowling Shirts")]
    public string mensbowlingshirts { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Medical Scrub Sets")]
    public string womensmedicalscrubsets { get; set; }

    // Occurs 3
    [JsonPropertyName("Men's Cycling Compression Shorts")]
    public string menscyclingcompressionshorts { get; set; }

    // Occurs 3
    [JsonPropertyName("Powersports Handlebars & Parts")]
    public string powersportshandlebarsparts { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Compression Pants")]
    public string boyscompressionpants { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Athletic Supporters")]
    public string boysathleticsupporters { get; set; }

    // Occurs 3
    [JsonPropertyName("Boating Equipment")]
    public string boatingequipment { get; set; }

    // Occurs 3
    [JsonPropertyName("Wall & Tabletop Picture Frames")]
    public string walltabletoppictureframes { get; set; }

    // Occurs 3
    [JsonPropertyName("Lingerie Tape")]
    public string lingerietape { get; set; }

    // Occurs 3
    [JsonPropertyName("Powersports Balaclavas")]
    public string powersportsbalaclavas { get; set; }

    // Occurs 3
    [JsonPropertyName("Skiing Boot Bags")]
    public string skiingbootbags { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Bikini Sets")]
    public string girlsbikinisets { get; set; }

    // Occurs 3
    [JsonPropertyName("Hunting & Shooting Safety Glasses")]
    public string huntingshootingsafetyglasses { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Medical Scrub Jackets")]
    public string womensmedicalscrubjackets { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Athletic Hoodies")]
    public string boysathletichoodies { get; set; }

    // Occurs 3
    [JsonPropertyName("Men's Novelty Pants")]
    public string mensnoveltypants { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Running Shoes")]
    public string boysrunningshoes { get; set; }

    // Occurs 3
    [JsonPropertyName("Men's Dance Apparel")]
    public string mensdanceapparel { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Swimwear Bodysuits")]
    public string girlsswimwearbodysuits { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Exotic Lingerie Bodystockings")]
    public string womensexoticlingeriebodystockings { get; set; }

    // Occurs 3
    [JsonPropertyName("Sewing Trim & Embellishments")]
    public string sewingtrimembellishments { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Snow Pants & Bibs")]
    public string girlssnowpantsbibs { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Snow Boots")]
    public string girlssnowboots { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Medical Clothing")]
    public string womensmedicalclothing { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Medical Uniforms & Scrubs")]
    public string womensmedicaluniformsscrubs { get; set; }

    // Occurs 3
    [JsonPropertyName("Birthday Candles")]
    public string birthdaycandles { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Novelty Hats & Caps")]
    public string womensnoveltyhatscaps { get; set; }

    // Occurs 3
    [JsonPropertyName("Girls' Dance Shoes")]
    public string girlsdanceshoes { get; set; }

    // Occurs 3
    [JsonPropertyName("Sauna Suits")]
    public string saunasuits { get; set; }

    // Occurs 3
    [JsonPropertyName("Bed Throws")]
    public string bedthrows { get; set; }

    // Occurs 3
    [JsonPropertyName("Boys' Shoes")]
    public string boysshoes { get; set; }

    // Occurs 3
    [JsonPropertyName("Home Décor Products")]
    public string homedécorproducts { get; set; }

    // Occurs 3
    [JsonPropertyName("Women's Full Slips")]
    public string womensfullslips { get; set; }

    // Occurs 3
    [JsonPropertyName("Plush Figure Toys")]
    public string plushfiguretoys { get; set; }

    // Occurs 3
    [JsonPropertyName("Team Sports")]
    public string teamsports { get; set; }

    // Occurs 3
    [JsonPropertyName("Baby Girls' Dresses")]
    public string babygirlsdresses { get; set; }

    // Occurs 3
    [JsonPropertyName("Shirt Studs")]
    public string shirtstuds { get; set; }

    // Occurs 3
    [JsonPropertyName("Unique Electronics")]
    public string uniqueelectronics { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Snowboard Boots")]
    public string menssnowboardboots { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Teething Bibs")]
    public string babyteethingbibs { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Cheerleading Clothing")]
    public string menscheerleadingclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Polo Shirts")]
    public string girlspoloshirts { get; set; }

    // Occurs 2
    [JsonPropertyName("Maternity Nursing Tanks & Camis")]
    public string maternitynursingtankscamis { get; set; }

    // Occurs 2
    [JsonPropertyName("Bowling Clothing")]
    public string bowlingclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys One Piece Swimsuit")]
    public string boysonepieceswimsuit { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Golf Clothing Accessories")]
    public string mensgolfclothingaccessories { get; set; }

    // Occurs 2
    [JsonPropertyName("Action Figures")]
    public string actionfigures { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Girls' Tank Tops")]
    public string babygirlstanktops { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Novelty Pajama Sets")]
    public string womensnoveltypajamasets { get; set; }

    // Occurs 2
    [JsonPropertyName("Reflective Gear")]
    public string reflectivegear { get; set; }

    // Occurs 2
    [JsonPropertyName("Cabinet & Furniture Pulls")]
    public string cabinetfurniturepulls { get; set; }

    // Occurs 2
    [JsonPropertyName("Hunting Camouflage Accessories")]
    public string huntingcamouflageaccessories { get; set; }

    // Occurs 2
    [JsonPropertyName("Travel Pillows")]
    public string travelpillows { get; set; }

    // Occurs 2
    [JsonPropertyName("Eyeglass Care Products")]
    public string eyeglasscareproducts { get; set; }

    // Occurs 2
    [JsonPropertyName("Statues")]
    public string statues { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Novelty Leggings")]
    public string womensnoveltyleggings { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Body Piercing Plugs")]
    public string mensbodypiercingplugs { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Rings Enhancers")]
    public string womensringsenhancers { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Quilted Lightweight Jackets")]
    public string womensquiltedlightweightjackets { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Novelty Swimwear")]
    public string womensnoveltyswimwear { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Novelty Hipster Panties")]
    public string womensnoveltyhipsterpanties { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Exotic Sleepwear & Robe Sets")]
    public string womensexoticsleepwearrobesets { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Ice Hockey Clothing")]
    public string girlsicehockeyclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Sweatshirts")]
    public string boyssweatshirts { get; set; }

    // Occurs 2
    [JsonPropertyName("Sports Fan Backboards")]
    public string sportsfanbackboards { get; set; }

    // Occurs 2
    [JsonPropertyName("Clothing & Closet Storage")]
    public string clothingclosetstorage { get; set; }

    // Occurs 2
    [JsonPropertyName("Dress-Up Toy Makeup")]
    public string dressuptoymakeup { get; set; }

    // Occurs 2
    [JsonPropertyName("Maternity T-Shirts")]
    public string maternitytshirts { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Shrug Sweaters")]
    public string womensshrugsweaters { get; set; }

    // Occurs 2
    [JsonPropertyName("Safety Aprons")]
    public string safetyaprons { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Hiking & Outdoor Recreation Vests")]
    public string menshikingoutdoorrecreationvests { get; set; }

    // Occurs 2
    [JsonPropertyName("Snowboarding Equipment")]
    public string snowboardingequipment { get; set; }

    // Occurs 2
    [JsonPropertyName("Camping Towels")]
    public string campingtowels { get; set; }

    // Occurs 2
    [JsonPropertyName("Fruit & Vegetable Cleaning Brushes")]
    public string fruitvegetablecleaningbrushes { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Earring Jackets")]
    public string womensearringjackets { get; set; }

    // Occurs 2
    [JsonPropertyName("Martial Arts Equipment Bags")]
    public string martialartsequipmentbags { get; set; }

    // Occurs 2
    [JsonPropertyName("Powersports Protective Pants")]
    public string powersportsprotectivepants { get; set; }

    // Occurs 2
    [JsonPropertyName("Tennis Clothing")]
    public string tennisclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Roller Window Shades")]
    public string rollerwindowshades { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Chemises & Negligees")]
    public string womenschemisesnegligees { get; set; }

    // Occurs 2
    [JsonPropertyName("Toy Sports Products")]
    public string toysportsproducts { get; set; }

    // Occurs 2
    [JsonPropertyName("Desk Accessories & Workspace Organizers")]
    public string deskaccessoriesworkspaceorganizers { get; set; }

    // Occurs 2
    [JsonPropertyName("Kids' Stickers")]
    public string kidsstickers { get; set; }

    // Occurs 2
    [JsonPropertyName("Body Glitters")]
    public string bodyglitters { get; set; }

    // Occurs 2
    [JsonPropertyName("Laptop Bags")]
    public string laptopbags { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Mountaineering Boots")]
    public string womensmountaineeringboots { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Boys' Button-Down & Dress Shirts")]
    public string babyboysbuttondowndressshirts { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Activewear Sweaters")]
    public string womensactivewearsweaters { get; set; }

    // Occurs 2
    [JsonPropertyName("Judo Suit Sets")]
    public string judosuitsets { get; set; }

    // Occurs 2
    [JsonPropertyName("Kids' School Uniforms")]
    public string kidsschooluniforms { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Bridal Rings Sets")]
    public string womensbridalringssets { get; set; }

    // Occurs 2
    [JsonPropertyName("Kids' Play Bracelets")]
    public string kidsplaybracelets { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Novelty Tops & Tees")]
    public string womensnoveltytopstees { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Golf Clothing Accessories")]
    public string girlsgolfclothingaccessories { get; set; }

    // Occurs 2
    [JsonPropertyName("Other Sports Types")]
    public string othersportstypes { get; set; }

    // Occurs 2
    [JsonPropertyName("Beach Towels")]
    public string beachtowels { get; set; }

    // Occurs 2
    [JsonPropertyName("Dehumidifiers")]
    public string dehumidifiers { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Watches")]
    public string girlswatches { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Compression Leg Sleeves")]
    public string womenscompressionlegsleeves { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Outdoor Shoes")]
    public string womensoutdoorshoes { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Running Clothing")]
    public string girlsrunningclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Sports Fan Slippers")]
    public string sportsfanslippers { get; set; }

    // Occurs 2
    [JsonPropertyName("Paper & Plastic Household Supplies")]
    public string paperplastichouseholdsupplies { get; set; }

    // Occurs 2
    [JsonPropertyName("Zippers")]
    public string zippers { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Blanket Sleepers")]
    public string girlsblanketsleepers { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Equestrian Breeches")]
    public string mensequestrianbreeches { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Boxing Clothing")]
    public string womensboxingclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Sports Fan Baby Layette Sets")]
    public string sportsfanbabylayettesets { get; set; }

    // Occurs 2
    [JsonPropertyName("Stuffed Animals & Teddy Bears")]
    public string stuffedanimalsteddybears { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Walking Shoes")]
    public string boyswalkingshoes { get; set; }

    // Occurs 2
    [JsonPropertyName("Basketballs")]
    public string basketballs { get; set; }

    // Occurs 2
    [JsonPropertyName("Wart Removal Products")]
    public string wartremovalproducts { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Polo Shirts")]
    public string boyspoloshirts { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Shorts")]
    public string boysshorts { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Exotic Bustiers & Corsets")]
    public string womensexoticbustierscorsets { get; set; }

    // Occurs 2
    [JsonPropertyName("Camping Foot Warmers")]
    public string campingfootwarmers { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Pendant Enhancers")]
    public string womenspendantenhancers { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Girls' Clothing Sets")]
    public string babygirlsclothingsets { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Hiking Clothing")]
    public string boyshikingclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Diaper Bags")]
    public string diaperbags { get; set; }

    // Occurs 2
    [JsonPropertyName("Cell Phone Stands")]
    public string cellphonestands { get; set; }

    // Occurs 2
    [JsonPropertyName("Hand Tools")]
    public string handtools { get; set; }

    // Occurs 2
    [JsonPropertyName("Hat Racks")]
    public string hatracks { get; set; }

    // Occurs 2
    [JsonPropertyName("Kids' Throw Blankets")]
    public string kidsthrowblankets { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Boxing Clothing")]
    public string mensboxingclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Cycling Jerseys")]
    public string boyscyclingjerseys { get; set; }

    // Occurs 2
    [JsonPropertyName("Portable & Arranger Keyboards")]
    public string portablearrangerkeyboards { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Pocket Watches")]
    public string boyspocketwatches { get; set; }

    // Occurs 2
    [JsonPropertyName("Inline Skating Replacement Bearings")]
    public string inlineskatingreplacementbearings { get; set; }

    // Occurs 2
    [JsonPropertyName("Pen, Pencil & Marker Cases")]
    public string penpencilmarkercases { get; set; }

    // Occurs 2
    [JsonPropertyName("Paintball Masks")]
    public string paintballmasks { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Cheerleading Shoes")]
    public string girlscheerleadingshoes { get; set; }

    // Occurs 2
    [JsonPropertyName("Drysuits")]
    public string drysuits { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Exotic Teddies & Bodysuits")]
    public string womensexoticteddiesbodysuits { get; set; }

    // Occurs 2
    [JsonPropertyName("Sports Fan Golf Equipment")]
    public string sportsfangolfequipment { get; set; }

    // Occurs 2
    [JsonPropertyName("Cell Phone Accessories")]
    public string cellphoneaccessories { get; set; }

    // Occurs 2
    [JsonPropertyName("Power Tools & Hand Tools")]
    public string powertoolshandtools { get; set; }

    // Occurs 2
    [JsonPropertyName("Sports Fan Ornaments")]
    public string sportsfanornaments { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Oxfords")]
    public string boysoxfords { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Link Bracelets")]
    public string girlslinkbracelets { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Girls' Outerwear Vests")]
    public string babygirlsouterwearvests { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Body Piercing Studs")]
    public string mensbodypiercingstuds { get; set; }

    // Occurs 2
    [JsonPropertyName("Headlamps")]
    public string headlamps { get; set; }

    // Occurs 2
    [JsonPropertyName("Field, Court, Gym & Rink Equipment")]
    public string fieldcourtgymrinkequipment { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Girls' Swimwear")]
    public string babygirlsswimwear { get; set; }

    // Occurs 2
    [JsonPropertyName("Student Awards & Student Incentives")]
    public string studentawardsstudentincentives { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Equestrian Tournament Jackets")]
    public string womensequestriantournamentjackets { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Tennis Shorts")]
    public string womenstennisshorts { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Hiking Shirts")]
    public string boyshikingshirts { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Sleepwear")]
    public string womenssleepwear { get; set; }

    // Occurs 2
    [JsonPropertyName("Maternity Panties")]
    public string maternitypanties { get; set; }

    // Occurs 2
    [JsonPropertyName("Bar Tools & Drinkware")]
    public string bartoolsdrinkware { get; set; }

    // Occurs 2
    [JsonPropertyName("Football Gloves")]
    public string footballgloves { get; set; }

    // Occurs 2
    [JsonPropertyName("Reading Glasses")]
    public string readingglasses { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Athletic Two-Piece Swimsuits")]
    public string girlsathletictwopieceswimsuits { get; set; }

    // Occurs 2
    [JsonPropertyName("Bottle Openers")]
    public string bottleopeners { get; set; }

    // Occurs 2
    [JsonPropertyName("Sports Fan Wristbands")]
    public string sportsfanwristbands { get; set; }

    // Occurs 2
    [JsonPropertyName("Dolls")]
    public string dolls { get; set; }

    // Occurs 2
    [JsonPropertyName("Smartwatch Bands")]
    public string smartwatchbands { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Keepsake Products")]
    public string babykeepsakeproducts { get; set; }

    // Occurs 2
    [JsonPropertyName("Pool Rafts & Inflatable Ride-ons")]
    public string poolraftsinflatablerideons { get; set; }

    // Occurs 2
    [JsonPropertyName("Soccer Shin Guards")]
    public string soccershinguards { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Ice Hockey Jerseys")]
    public string boysicehockeyjerseys { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Girls' Athletic & Outdoor Shoes")]
    public string babygirlsathleticoutdoorshoes { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Shapewear")]
    public string womensshapewear { get; set; }

    // Occurs 2
    [JsonPropertyName("Golf Shoe Bags")]
    public string golfshoebags { get; set; }

    // Occurs 2
    [JsonPropertyName("Powersports Gear Bags")]
    public string powersportsgearbags { get; set; }

    // Occurs 2
    [JsonPropertyName("Powersports Face Masks")]
    public string powersportsfacemasks { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Care Products")]
    public string babycareproducts { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Girls' Footies & Rompers")]
    public string babygirlsfootiesrompers { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Ice Skating Jackets")]
    public string girlsiceskatingjackets { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Ice Hockey Jerseys")]
    public string womensicehockeyjerseys { get; set; }

    // Occurs 2
    [JsonPropertyName("Gardening Hand Tools")]
    public string gardeninghandtools { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Track & Field & Cross Country Shoes")]
    public string womenstrackfieldcrosscountryshoes { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Skiing Jackets")]
    public string girlsskiingjackets { get; set; }

    // Occurs 2
    [JsonPropertyName("Nursery Swaddling Blankets")]
    public string nurseryswaddlingblankets { get; set; }

    // Occurs 2
    [JsonPropertyName("Sports Fan Cell Phone Accessories")]
    public string sportsfancellphoneaccessories { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Snow & Rainwear")]
    public string girlssnowrainwear { get; set; }

    // Occurs 2
    [JsonPropertyName("Martial Arts Groin Protectors")]
    public string martialartsgroinprotectors { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Body Piercing Tunnels")]
    public string mensbodypiercingtunnels { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Running Gloves")]
    public string girlsrunninggloves { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Short Sets")]
    public string boysshortsets { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Cycling Shorts")]
    public string boyscyclingshorts { get; set; }

    // Occurs 2
    [JsonPropertyName("Maternity Intimate Apparel")]
    public string maternityintimateapparel { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Bath & Hooded Towels")]
    public string babybathhoodedtowels { get; set; }

    // Occurs 2
    [JsonPropertyName("Home Decorative Accessories")]
    public string homedecorativeaccessories { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Baseball & Softball Shoes")]
    public string girlsbaseballsoftballshoes { get; set; }

    // Occurs 2
    [JsonPropertyName("Safety & Security")]
    public string safetysecurity { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Hand Fans")]
    public string menshandfans { get; set; }

    // Occurs 2
    [JsonPropertyName("Paintball Gloves")]
    public string paintballgloves { get; set; }

    // Occurs 2
    [JsonPropertyName("Equestrian Protective Gear")]
    public string equestrianprotectivegear { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Basketball Clothing")]
    public string girlsbasketballclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Athletic Shorts")]
    public string girlsathleticshorts { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Football Shoes")]
    public string boysfootballshoes { get; set; }

    // Occurs 2
    [JsonPropertyName("Headphone Adapters")]
    public string headphoneadapters { get; set; }

    // Occurs 2
    [JsonPropertyName("Safety Goggles & Glasses")]
    public string safetygogglesglasses { get; set; }

    // Occurs 2
    [JsonPropertyName("Facial Masks")]
    public string facialmasks { get; set; }

    // Occurs 2
    [JsonPropertyName("Kids' Costume Hats")]
    public string kidscostumehats { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Boys' Snow Pants & Bibs")]
    public string babyboyssnowpantsbibs { get; set; }

    // Occurs 2
    [JsonPropertyName("Tumblers & Water Glasses")]
    public string tumblerswaterglasses { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Shapewear Thigh Slimmers")]
    public string womensshapewearthighslimmers { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Suit Jackets")]
    public string menssuitjackets { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Snow Suits")]
    public string girlssnowsuits { get; set; }

    // Occurs 2
    [JsonPropertyName("Indoor Fountain Stones & Sea Glass")]
    public string indoorfountainstonesseaglass { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Girls' Sleepwear & Robes")]
    public string babygirlssleepwearrobes { get; set; }

    // Occurs 2
    [JsonPropertyName("Powersports Base Layer Tops")]
    public string powersportsbaselayertops { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Cycling Leg Warmers")]
    public string womenscyclinglegwarmers { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Hipster Panties")]
    public string womenshipsterpanties { get; set; }

    // Occurs 2
    [JsonPropertyName("Climbing Equipment")]
    public string climbingequipment { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Outdoor Shoes")]
    public string boysoutdoorshoes { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Basketball Jerseys")]
    public string womensbasketballjerseys { get; set; }

    // Occurs 2
    [JsonPropertyName("Uniforms, Work & Safety")]
    public string uniformsworksafety { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Boys' Training Underpants")]
    public string babyboystrainingunderpants { get; set; }

    // Occurs 2
    [JsonPropertyName("Tablet Accessories")]
    public string tabletaccessories { get; set; }

    // Occurs 2
    [JsonPropertyName("Laundry Bags")]
    public string laundrybags { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Outdoor Shoes")]
    public string mensoutdoorshoes { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Girls' Down Jackets & Coats")]
    public string babygirlsdownjacketscoats { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Boys' Down Coats & Jackets")]
    public string babyboysdowncoatsjackets { get; set; }

    // Occurs 2
    [JsonPropertyName("Fitted Bed Sheets")]
    public string fittedbedsheets { get; set; }

    // Occurs 2
    [JsonPropertyName("Diving Duffels")]
    public string divingduffels { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Girls' Blouses")]
    public string babygirlsblouses { get; set; }

    // Occurs 2
    [JsonPropertyName("Airsoft Helmets")]
    public string airsofthelmets { get; set; }

    // Occurs 2
    [JsonPropertyName("Diving Boots")]
    public string divingboots { get; set; }

    // Occurs 2
    [JsonPropertyName("Decorative Signs & Plaques")]
    public string decorativesignsplaques { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Hiking Shoes")]
    public string boyshikingshoes { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Hiking Shoes")]
    public string girlshikingshoes { get; set; }

    // Occurs 2
    [JsonPropertyName("Household Cleaning Brushes")]
    public string householdcleaningbrushes { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Work Utility & Safety Tops")]
    public string womensworkutilitysafetytops { get; set; }

    // Occurs 2
    [JsonPropertyName("Hunting Game Belts & Bags")]
    public string huntinggamebeltsbags { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Hiking & Outdoor Recreation Waterproof Jackets")]
    public string womenshikingoutdoorrecreationwaterproofjackets { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Motorcycle Protective Boots")]
    public string womensmotorcycleprotectiveboots { get; set; }

    // Occurs 2
    [JsonPropertyName("Protective Safety Workwear")]
    public string protectivesafetyworkwear { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby & Toddler Carriers")]
    public string babytoddlercarriers { get; set; }

    // Occurs 2
    [JsonPropertyName("Decorative Jars")]
    public string decorativejars { get; set; }

    // Occurs 2
    [JsonPropertyName("Bathroom Accessories")]
    public string bathroomaccessories { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Ice Hockey Clothing")]
    public string mensicehockeyclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Rain Hats")]
    public string womensrainhats { get; set; }

    // Occurs 2
    [JsonPropertyName("Women's Medical Scrub Pants")]
    public string womensmedicalscrubpants { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Undershirts")]
    public string boysundershirts { get; set; }

    // Occurs 2
    [JsonPropertyName("Snow Sports Goggles")]
    public string snowsportsgoggles { get; set; }

    // Occurs 2
    [JsonPropertyName("Golf Duffel Bags")]
    public string golfduffelbags { get; set; }

    // Occurs 2
    [JsonPropertyName("Bathroom Towels")]
    public string bathroomtowels { get; set; }

    // Occurs 2
    [JsonPropertyName("Medical Procedure Masks")]
    public string medicalproceduremasks { get; set; }

    // Occurs 2
    [JsonPropertyName("Shorty Wetsuits")]
    public string shortywetsuits { get; set; }

    // Occurs 2
    [JsonPropertyName("Cheerleading Clothing")]
    public string cheerleadingclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Automotive Decals")]
    public string automotivedecals { get; set; }

    // Occurs 2
    [JsonPropertyName("Protective Caps, Hoods & Hairnets")]
    public string protectivecapshoodshairnets { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Tennis Clothing")]
    public string menstennisclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Equestrian Riding Gloves")]
    public string equestrianridinggloves { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Novelty Tights")]
    public string girlsnoveltytights { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Skiing Clothing")]
    public string girlsskiingclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Sports Fan Baseball Equipment")]
    public string sportsfanbaseballequipment { get; set; }

    // Occurs 2
    [JsonPropertyName("Sports Fan Basketball Equipment")]
    public string sportsfanbasketballequipment { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Activewear Undershorts")]
    public string boysactivewearundershorts { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Compression Shorts")]
    public string girlscompressionshorts { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Tennis Shirts")]
    public string menstennisshirts { get; set; }

    // Occurs 2
    [JsonPropertyName("Preserved Flowers")]
    public string preservedflowers { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Neckties")]
    public string boysneckties { get; set; }

    // Occurs 2
    [JsonPropertyName("Bath Rugs")]
    public string bathrugs { get; set; }

    // Occurs 2
    [JsonPropertyName("Security & Surveillance Equipment")]
    public string securitysurveillanceequipment { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Novelty One-Piece Pajamas")]
    public string mensnoveltyonepiecepajamas { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' ID Bracelets")]
    public string girlsidbracelets { get; set; }

    // Occurs 2
    [JsonPropertyName("Tobacco Pipe Bags & Pouches")]
    public string tobaccopipebagspouches { get; set; }

    // Occurs 2
    [JsonPropertyName("Racing Apparel")]
    public string racingapparel { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Loafers")]
    public string girlsloafers { get; set; }

    // Occurs 2
    [JsonPropertyName("Baby Boys' Leggings")]
    public string babyboysleggings { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Volleyball Jerseys")]
    public string mensvolleyballjerseys { get; set; }

    // Occurs 2
    [JsonPropertyName("Men's Rugby Clothing")]
    public string mensrugbyclothing { get; set; }

    // Occurs 2
    [JsonPropertyName("Girls' Skateboarding Shoes")]
    public string girlsskateboardingshoes { get; set; }

    // Occurs 2
    [JsonPropertyName("Boys' Athletic Swimwear")]
    public string boysathleticswimwear { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Track Pants")]
    public string girlstrackpants { get; set; }

    // Occurs 1
    [JsonPropertyName("Automotive Replacement Air Conditioning Products")]
    public string automotivereplacementairconditioningproducts { get; set; }

    // Occurs 1
    [JsonPropertyName("Reuseable Cleaning Cloths")]
    public string reuseablecleaningcloths { get; set; }

    // Occurs 1
    [JsonPropertyName("Medical Shoe Covers")]
    public string medicalshoecovers { get; set; }

    // Occurs 1
    [JsonPropertyName("Artificial Plants & Greenery")]
    public string artificialplantsgreenery { get; set; }

    // Occurs 1
    [JsonPropertyName("Snowshoeing Equipment")]
    public string snowshoeingequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Dance Shoes")]
    public string boysdanceshoes { get; set; }

    // Occurs 1
    [JsonPropertyName("Craft & Sewing Supplies Storage")]
    public string craftsewingsuppliesstorage { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Skiing Pants")]
    public string girlsskiingpants { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Hiking & Outdoor Recreation Headwear")]
    public string menshikingoutdoorrecreationheadwear { get; set; }

    // Occurs 1
    [JsonPropertyName("Airsoft Gloves")]
    public string airsoftgloves { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Pajama Shirts")]
    public string menspajamashirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Novelty Belts")]
    public string womensnoveltybelts { get; set; }

    // Occurs 1
    [JsonPropertyName("Decorative Masks")]
    public string decorativemasks { get; set; }

    // Occurs 1
    [JsonPropertyName("Healing Crystals")]
    public string healingcrystals { get; set; }

    // Occurs 1
    [JsonPropertyName("Hardware")]
    public string hardware { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Pants")]
    public string sportsfanpants { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Football Jerseys")]
    public string womensfootballjerseys { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Overalls")]
    public string boysoveralls { get; set; }

    // Occurs 1
    [JsonPropertyName("Wall Art")]
    public string wallart { get; set; }

    // Occurs 1
    [JsonPropertyName("Kids' Party Decorations")]
    public string kidspartydecorations { get; set; }

    // Occurs 1
    [JsonPropertyName("Door Stops")]
    public string doorstops { get; set; }

    // Occurs 1
    [JsonPropertyName("Baby Girls' Hair Accessories")]
    public string babygirlshairaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Hiking & Outdoor Recreation Vests")]
    public string womenshikingoutdoorrecreationvests { get; set; }

    // Occurs 1
    [JsonPropertyName("Cosmetic Train Cases")]
    public string cosmetictraincases { get; set; }

    // Occurs 1
    [JsonPropertyName("Swimwear")]
    public string swimwear { get; set; }

    // Occurs 1
    [JsonPropertyName("Paper & Printable Media")]
    public string paperprintablemedia { get; set; }

    // Occurs 1
    [JsonPropertyName("Pressure Washers")]
    public string pressurewashers { get; set; }

    // Occurs 1
    [JsonPropertyName("Bathtub Toys")]
    public string bathtubtoys { get; set; }

    // Occurs 1
    [JsonPropertyName("Bathroom Countertop Soap Dispensers")]
    public string bathroomcountertopsoapdispensers { get; set; }

    // Occurs 1
    [JsonPropertyName("Fracture & Cast Boots")]
    public string fracturecastboots { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Ice Hockey Clothing")]
    public string womensicehockeyclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Dance Pants")]
    public string girlsdancepants { get; set; }

    // Occurs 1
    [JsonPropertyName("Decorative Folding Fans")]
    public string decorativefoldingfans { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Rash Guard Shirts")]
    public string girlsrashguardshirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Compression Arm Sleeves")]
    public string menscompressionarmsleeves { get; set; }

    // Occurs 1
    [JsonPropertyName("Industrial Sealants")]
    public string industrialsealants { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Tennis Shorts")]
    public string menstennisshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Cycling Body Armor")]
    public string cyclingbodyarmor { get; set; }

    // Occurs 1
    [JsonPropertyName("Dart Carrying Cases & Wallets")]
    public string dartcarryingcaseswallets { get; set; }

    // Occurs 1
    [JsonPropertyName("Shooting")]
    public string shooting { get; set; }

    // Occurs 1
    [JsonPropertyName("Fabric Adhesives")]
    public string fabricadhesives { get; set; }

    // Occurs 1
    [JsonPropertyName("Jiu-Jitsu Suit Tops")]
    public string jiujitsusuittops { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Bowling Shirts")]
    public string womensbowlingshirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Drinking Flasks")]
    public string drinkingflasks { get; set; }

    // Occurs 1
    [JsonPropertyName("Baseball & Softball Catcher Helmets")]
    public string baseballsoftballcatcherhelmets { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Ice Skating Clothing")]
    public string girlsiceskatingclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Walking Shoes")]
    public string girlswalkingshoes { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Food Service Uniforms")]
    public string mensfoodserviceuniforms { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Sleeve Patches")]
    public string sportsfansleevepatches { get; set; }

    // Occurs 1
    [JsonPropertyName("Caulk")]
    public string caulk { get; set; }

    // Occurs 1
    [JsonPropertyName("Airsoft Equipment")]
    public string airsoftequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Novelty Shorts")]
    public string womensnoveltyshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Volleyball Equipment")]
    public string volleyballequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Novelty Bomber Hats")]
    public string womensnoveltybomberhats { get; set; }

    // Occurs 1
    [JsonPropertyName("Hunting Safety Belts & Harnesses")]
    public string huntingsafetybeltsharnesses { get; set; }

    // Occurs 1
    [JsonPropertyName("Mixing Bowls")]
    public string mixingbowls { get; set; }

    // Occurs 1
    [JsonPropertyName("Nordic Ski Bindings")]
    public string nordicskibindings { get; set; }

    // Occurs 1
    [JsonPropertyName("Lab Dispensing Bottles")]
    public string labdispensingbottles { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Swimwear Bodysuits")]
    public string mensswimwearbodysuits { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Swimwear Bodysuits")]
    public string womensswimwearbodysuits { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Activewear Leggings")]
    public string girlsactivewearleggings { get; set; }

    // Occurs 1
    [JsonPropertyName("Exercise & Fitness Footwear")]
    public string exercisefitnessfootwear { get; set; }

    // Occurs 1
    [JsonPropertyName("Paintball Clothing")]
    public string paintballclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Hunting Field Dressing Accessories")]
    public string huntingfielddressingaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Kids' Play Earrings")]
    public string kidsplayearrings { get; set; }

    // Occurs 1
    [JsonPropertyName("Melon Ballers")]
    public string melonballers { get; set; }

    // Occurs 1
    [JsonPropertyName("Cell Phone Grips")]
    public string cellphonegrips { get; set; }

    // Occurs 1
    [JsonPropertyName("Makeup Palettes")]
    public string makeuppalettes { get; set; }

    // Occurs 1
    [JsonPropertyName("Nail Art Tools")]
    public string nailarttools { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Hiking Boots")]
    public string girlshikingboots { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Basketball Clothing")]
    public string womensbasketballclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Musical Instrument Accessories")]
    public string musicalinstrumentaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Key Hooks")]
    public string keyhooks { get; set; }

    // Occurs 1
    [JsonPropertyName("Archery Targets")]
    public string archerytargets { get; set; }

    // Occurs 1
    [JsonPropertyName("Bookshelf Photo Albums")]
    public string bookshelfphotoalbums { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Novelty Belts")]
    public string girlsnoveltybelts { get; set; }

    // Occurs 1
    [JsonPropertyName("Beading Kits")]
    public string beadingkits { get; set; }

    // Occurs 1
    [JsonPropertyName("Bumper Stickers, Decals & Magnets")]
    public string bumperstickersdecalsmagnets { get; set; }

    // Occurs 1
    [JsonPropertyName("Breast Lift Tape")]
    public string breastlifttape { get; set; }

    // Occurs 1
    [JsonPropertyName("Filament Tape")]
    public string filamenttape { get; set; }

    // Occurs 1
    [JsonPropertyName("Gun Accessories, Maintenance & Storage")]
    public string gunaccessoriesmaintenancestorage { get; set; }

    // Occurs 1
    [JsonPropertyName("Lunch Boxes")]
    public string lunchboxes { get; set; }

    // Occurs 1
    [JsonPropertyName("Kids' Lunch Boxes")]
    public string kidslunchboxes { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Hiking Boots")]
    public string boyshikingboots { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Costume Bottoms")]
    public string menscostumebottoms { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Motorcycle Protective Shoes")]
    public string mensmotorcycleprotectiveshoes { get; set; }

    // Occurs 1
    [JsonPropertyName("3D Printers")]
    public string _3dprinters { get; set; }

    // Occurs 1
    [JsonPropertyName("Printmaking Supplies")]
    public string printmakingsupplies { get; set; }

    // Occurs 1
    [JsonPropertyName("Christmas Tree Skirts")]
    public string christmastreeskirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Index Card Files & Business Card Files")]
    public string indexcardfilesbusinesscardfiles { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Bustiers & Corsets")]
    public string womensbustierscorsets { get; set; }

    // Occurs 1
    [JsonPropertyName("Arena & Gaming Equipment")]
    public string arenagamingequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Home Use Medical Supplies & Equipment")]
    public string homeusemedicalsuppliesequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Camping Tent Accessories")]
    public string campingtentaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Football Clothing")]
    public string girlsfootballclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Lidded Home Storage Bins")]
    public string liddedhomestoragebins { get; set; }

    // Occurs 1
    [JsonPropertyName("Nursery Bed Blankets")]
    public string nurserybedblankets { get; set; }

    // Occurs 1
    [JsonPropertyName("Flatware Organizers")]
    public string flatwareorganizers { get; set; }

    // Occurs 1
    [JsonPropertyName("Lab Gowns")]
    public string labgowns { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Commuter Pass Cases")]
    public string womenscommuterpasscases { get; set; }

    // Occurs 1
    [JsonPropertyName("Furniture")]
    public string furniture { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Home & Kitchen")]
    public string sportsfanhomekitchen { get; set; }

    // Occurs 1
    [JsonPropertyName("Decorative Bowls")]
    public string decorativebowls { get; set; }

    // Occurs 1
    [JsonPropertyName("Bath Towels")]
    public string bathtowels { get; set; }

    // Occurs 1
    [JsonPropertyName("Baby Washcloths & Wash Gloves")]
    public string babywashclothswashgloves { get; set; }

    // Occurs 1
    [JsonPropertyName("Safety Shirts")]
    public string safetyshirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Cardigans")]
    public string girlscardigans { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Bowling Clothing")]
    public string mensbowlingclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Football Hand & Arm Pads")]
    public string footballhandarmpads { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Athletic Base Layers")]
    public string girlsathleticbaselayers { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Compression Arm Sleeves")]
    public string womenscompressionarmsleeves { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Cycling Bodysuits")]
    public string menscyclingbodysuits { get; set; }

    // Occurs 1
    [JsonPropertyName("Powersports Sunglasses")]
    public string powersportssunglasses { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Hiking & Outdoor Recreation Waterproof Jackets")]
    public string menshikingoutdoorrecreationwaterproofjackets { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Down Jackets & Coats")]
    public string girlsdownjacketscoats { get; set; }

    // Occurs 1
    [JsonPropertyName("Sewing Project Kits")]
    public string sewingprojectkits { get; set; }

    // Occurs 1
    [JsonPropertyName("All-Purpose Household Cleaners")]
    public string allpurposehouseholdcleaners { get; set; }

    // Occurs 1
    [JsonPropertyName("Snorkeling Packages")]
    public string snorkelingpackages { get; set; }

    // Occurs 1
    [JsonPropertyName("Powersports Accessories")]
    public string powersportsaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Baby Bibs & Burp Cloths Sets")]
    public string babybibsburpclothssets { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Novelty Belt Buckles")]
    public string boysnoveltybeltbuckles { get; set; }

    // Occurs 1
    [JsonPropertyName("Climbing Gloves")]
    public string climbinggloves { get; set; }

    // Occurs 1
    [JsonPropertyName("Water Pumps, Parts & Accessories")]
    public string waterpumpspartsaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Photo Albums")]
    public string photoalbums { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Dance Pants")]
    public string womensdancepants { get; set; }

    // Occurs 1
    [JsonPropertyName("Unfinished Wood")]
    public string unfinishedwood { get; set; }

    // Occurs 1
    [JsonPropertyName("Powersports Helmet Visors")]
    public string powersportshelmetvisors { get; set; }

    // Occurs 1
    [JsonPropertyName("Toy Kitchen Products")]
    public string toykitchenproducts { get; set; }

    // Occurs 1
    [JsonPropertyName("Baby Girls' Bikini Sets")]
    public string babygirlsbikinisets { get; set; }

    // Occurs 1
    [JsonPropertyName("Sunscreens")]
    public string sunscreens { get; set; }

    // Occurs 1
    [JsonPropertyName("Kitchen Small Appliances")]
    public string kitchensmallappliances { get; set; }

    // Occurs 1
    [JsonPropertyName("Archery Points")]
    public string archerypoints { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Tennis Clothing")]
    public string boystennisclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Desk Staplers")]
    public string deskstaplers { get; set; }

    // Occurs 1
    [JsonPropertyName("Christmas Pendant, Drop & Finial Ornaments")]
    public string christmaspendantdropfinialornaments { get; set; }

    // Occurs 1
    [JsonPropertyName("Surfing Equipment")]
    public string surfingequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Tire Repair Tools")]
    public string tirerepairtools { get; set; }

    // Occurs 1
    [JsonPropertyName("Hardware Nuts")]
    public string hardwarenuts { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Shorts")]
    public string sportsfanshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Nintendo Switch Games")]
    public string nintendoswitchgames { get; set; }

    // Occurs 1
    [JsonPropertyName("Face Protection Equipment")]
    public string faceprotectionequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Automotive Consoles & Organizers")]
    public string automotiveconsolesorganizers { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Cycling Vests")]
    public string boyscyclingvests { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Swimwear")]
    public string mensswimwear { get; set; }

    // Occurs 1
    [JsonPropertyName("Craft Kits")]
    public string craftkits { get; set; }

    // Occurs 1
    [JsonPropertyName("Coach, Referee & Umpire Gear")]
    public string coachrefereeumpiregear { get; set; }

    // Occurs 1
    [JsonPropertyName("Climbing Rope Bags")]
    public string climbingropebags { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Tuxedo Shirts")]
    public string menstuxedoshirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Replacement Ski Goggle Lenses")]
    public string replacementskigogglelenses { get; set; }

    // Occurs 1
    [JsonPropertyName("Jewelry Making Kits")]
    public string jewelrymakingkits { get; set; }

    // Occurs 1
    [JsonPropertyName("Rugby Clothing")]
    public string rugbyclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Hiking Socks")]
    public string girlshikingsocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports & Outdoor Play Toys")]
    public string sportsoutdoorplaytoys { get; set; }

    // Occurs 1
    [JsonPropertyName("Ice Hockey Skate Accessories")]
    public string icehockeyskateaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Baby Jerseys & Shirts")]
    public string sportsfanbabyjerseysshirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Liners & Ankle Socks")]
    public string girlslinersanklesocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Coolers")]
    public string sportsfancoolers { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Pet Dresses")]
    public string sportsfanpetdresses { get; set; }

    // Occurs 1
    [JsonPropertyName("Guitar & Bass Picks & Pick Holders")]
    public string guitarbasspickspickholders { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Auto Accessories")]
    public string sportsfanautoaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Throw Pillow Covers")]
    public string throwpillowcovers { get; set; }

    // Occurs 1
    [JsonPropertyName("Heel Cushions & Cups")]
    public string heelcushionscups { get; set; }

    // Occurs 1
    [JsonPropertyName("Cash Register Bags")]
    public string cashregisterbags { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Dresses")]
    public string girlsdresses { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Novelty Sweaters")]
    public string boysnoveltysweaters { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Dance Shorts")]
    public string womensdanceshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Photographic Storage Materials")]
    public string photographicstoragematerials { get; set; }

    // Occurs 1
    [JsonPropertyName("PC Accessories")]
    public string pcaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("GPS, Finders & Accessories")]
    public string gpsfindersaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Shoe Gaiters")]
    public string shoegaiters { get; set; }

    // Occurs 1
    [JsonPropertyName("Christmas Stockings")]
    public string christmasstockings { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' School Uniform Skirts")]
    public string girlsschooluniformskirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Diving Electronics")]
    public string divingelectronics { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Body Jewelry")]
    public string mensbodyjewelry { get; set; }

    // Occurs 1
    [JsonPropertyName("Climbing Crash Pads")]
    public string climbingcrashpads { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Tennis Skirts")]
    public string womenstennisskirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Snowboard Bindings")]
    public string snowboardbindings { get; set; }

    // Occurs 1
    [JsonPropertyName("Office Labels & Stickers")]
    public string officelabelsstickers { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Baby Pants & Shorts")]
    public string sportsfanbabypantsshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Tactical Knives")]
    public string tacticalknives { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Exotic Apparel")]
    public string mensexoticapparel { get; set; }

    // Occurs 1
    [JsonPropertyName("Binder Pouches")]
    public string binderpouches { get; set; }

    // Occurs 1
    [JsonPropertyName("Cycling Hydration & Nutrition")]
    public string cyclinghydrationnutrition { get; set; }

    // Occurs 1
    [JsonPropertyName("Baby Stroller Replacement Parts")]
    public string babystrollerreplacementparts { get; set; }

    // Occurs 1
    [JsonPropertyName("Toy Stacking Block Sets")]
    public string toystackingblocksets { get; set; }

    // Occurs 1
    [JsonPropertyName("Household Cleaning Tools")]
    public string householdcleaningtools { get; set; }

    // Occurs 1
    [JsonPropertyName("Household Floor Cleaners")]
    public string householdfloorcleaners { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Cheerleading Clothing")]
    public string girlscheerleadingclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Air Conditioner Replacement Motors")]
    public string airconditionerreplacementmotors { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Undershirts, Tanks & Camisoles")]
    public string girlsundershirtstankscamisoles { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Belts")]
    public string sportsfanbelts { get; set; }

    // Occurs 1
    [JsonPropertyName("Automotive Replacement Engine Cooling & Climate Control")]
    public string automotivereplacementenginecoolingclimatecontrol { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Clogs & Mules")]
    public string girlsclogsmules { get; set; }

    // Occurs 1
    [JsonPropertyName("Boxing Training Gloves")]
    public string boxingtraininggloves { get; set; }

    // Occurs 1
    [JsonPropertyName("Unique Beauty & Health")]
    public string uniquebeautyhealth { get; set; }

    // Occurs 1
    [JsonPropertyName("Adult Electric Bicycles")]
    public string adultelectricbicycles { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Cheerleading Briefs & Shorts")]
    public string girlscheerleadingbriefsshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Coin Trays & Coin Boxes")]
    public string cointrayscoinboxes { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Cycling Bib Shorts")]
    public string womenscyclingbibshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Glassware & Drinkware")]
    public string glasswaredrinkware { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Pocket Squares")]
    public string menspocketsquares { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Novelty Gloves & Mittens")]
    public string girlsnoveltyglovesmittens { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Activewear Polos")]
    public string girlsactivewearpolos { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Tote Bags")]
    public string sportsfantotebags { get; set; }

    // Occurs 1
    [JsonPropertyName("Hunting & Tactical Knives & Tools")]
    public string huntingtacticalknivestools { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Skiing Pants")]
    public string boysskiingpants { get; set; }

    // Occurs 1
    [JsonPropertyName("Snowboard Bags")]
    public string snowboardbags { get; set; }

    // Occurs 1
    [JsonPropertyName("Paperweights")]
    public string paperweights { get; set; }

    // Occurs 1
    [JsonPropertyName("Baby Boys' Outerwear Jackets & Coats")]
    public string babyboysouterwearjacketscoats { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Oxford & Derby Boots")]
    public string mensoxfordderbyboots { get; set; }

    // Occurs 1
    [JsonPropertyName("Christmas Ornaments")]
    public string christmasornaments { get; set; }

    // Occurs 1
    [JsonPropertyName("Wetsuit Pants")]
    public string wetsuitpants { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Swim Pants")]
    public string womensswimpants { get; set; }

    // Occurs 1
    [JsonPropertyName("Wetsuit Tops")]
    public string wetsuittops { get; set; }

    // Occurs 1
    [JsonPropertyName("Kids' Artist Aprons & Smocks")]
    public string kidsartistapronssmocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Aquarium Gravel")]
    public string aquariumgravel { get; set; }

    // Occurs 1
    [JsonPropertyName("Aquarium Décor")]
    public string aquariumdécor { get; set; }

    // Occurs 1
    [JsonPropertyName("Toddler Art & Creativity Toys")]
    public string toddlerartcreativitytoys { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Novelty Leg Warmers")]
    public string girlsnoveltylegwarmers { get; set; }

    // Occurs 1
    [JsonPropertyName("Maternity Tights & Hosiery")]
    public string maternitytightshosiery { get; set; }

    // Occurs 1
    [JsonPropertyName("Cricket Sets")]
    public string cricketsets { get; set; }

    // Occurs 1
    [JsonPropertyName("Ceramic & Pottery Supplies")]
    public string ceramicpotterysupplies { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Novelty Underwear")]
    public string mensnoveltyunderwear { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Cheerleading Apparel")]
    public string womenscheerleadingapparel { get; set; }

    // Occurs 1
    [JsonPropertyName("Tape Measures")]
    public string tapemeasures { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Varsity Jackets")]
    public string mensvarsityjackets { get; set; }

    // Occurs 1
    [JsonPropertyName("Water Sports")]
    public string watersports { get; set; }

    // Occurs 1
    [JsonPropertyName("Drawing Pencils")]
    public string drawingpencils { get; set; }

    // Occurs 1
    [JsonPropertyName("Cut Resistant Gloves")]
    public string cutresistantgloves { get; set; }

    // Occurs 1
    [JsonPropertyName("Household Cleaning")]
    public string householdcleaning { get; set; }

    // Occurs 1
    [JsonPropertyName("Athletic Tapes & Wraps")]
    public string athletictapeswraps { get; set; }

    // Occurs 1
    [JsonPropertyName("Duck Calls & Lures")]
    public string duckcallslures { get; set; }

    // Occurs 1
    [JsonPropertyName("Pumps & Plumbing Equipment")]
    public string pumpsplumbingequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Baseball & Softball Face Guards")]
    public string baseballsoftballfaceguards { get; set; }

    // Occurs 1
    [JsonPropertyName("Indoor String Lights")]
    public string indoorstringlights { get; set; }

    // Occurs 1
    [JsonPropertyName("Kids' Room Decor Lamps & Lighting")]
    public string kidsroomdecorlampslighting { get; set; }

    // Occurs 1
    [JsonPropertyName("Cricket Footwear")]
    public string cricketfootwear { get; set; }

    // Occurs 1
    [JsonPropertyName("Cell Phone Charms")]
    public string cellphonecharms { get; set; }

    // Occurs 1
    [JsonPropertyName("Home Storage Hooks")]
    public string homestoragehooks { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Sport Coats & Blazers")]
    public string boyssportcoatsblazers { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Novelty Shorts")]
    public string mensnoveltyshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Food Service Display Stands")]
    public string foodservicedisplaystands { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Cycling Tights, Pants & Shorts")]
    public string womenscyclingtightspantsshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Novelty Headwraps")]
    public string womensnoveltyheadwraps { get; set; }

    // Occurs 1
    [JsonPropertyName("Automotive Exterior Accessories")]
    public string automotiveexterioraccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Thermal Underwear Sets")]
    public string boysthermalunderwearsets { get; set; }

    // Occurs 1
    [JsonPropertyName("Outdoor Flags & Banners")]
    public string outdoorflagsbanners { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Activewear Dresses")]
    public string girlsactiveweardresses { get; set; }

    // Occurs 1
    [JsonPropertyName("Cycling Accessories")]
    public string cyclingaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Aromatherapy Diffusers")]
    public string aromatherapydiffusers { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Triathlon Skinsuits")]
    public string menstriathlonskinsuits { get; set; }

    // Occurs 1
    [JsonPropertyName("Serveware")]
    public string serveware { get; set; }

    // Occurs 1
    [JsonPropertyName("Durable Medical Equipment")]
    public string durablemedicalequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Kickboxing Suits")]
    public string kickboxingsuits { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Novelty Belt Buckles")]
    public string girlsnoveltybeltbuckles { get; set; }

    // Occurs 1
    [JsonPropertyName("Baby Boys' Tuxedos")]
    public string babyboystuxedos { get; set; }

    // Occurs 1
    [JsonPropertyName("Ironing Products")]
    public string ironingproducts { get; set; }

    // Occurs 1
    [JsonPropertyName("Tactical Vests")]
    public string tacticalvests { get; set; }

    // Occurs 1
    [JsonPropertyName("Bike Headlight-Taillight Combinations")]
    public string bikeheadlighttaillightcombinations { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Dance Tights")]
    public string womensdancetights { get; set; }

    // Occurs 1
    [JsonPropertyName("Ski & Snowboard Car Racks")]
    public string skisnowboardcarracks { get; set; }

    // Occurs 1
    [JsonPropertyName("Stand-Up Paddleboards")]
    public string standuppaddleboards { get; set; }

    // Occurs 1
    [JsonPropertyName("Threaded Inserts")]
    public string threadedinserts { get; set; }

    // Occurs 1
    [JsonPropertyName("Blankets & Throws")]
    public string blanketsthrows { get; set; }

    // Occurs 1
    [JsonPropertyName("Safety Pants")]
    public string safetypants { get; set; }

    // Occurs 1
    [JsonPropertyName("Ice Fishing Ice Spearing Equipment")]
    public string icefishingicespearingequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Health Care Products")]
    public string healthcareproducts { get; set; }

    // Occurs 1
    [JsonPropertyName("Magnetic Therapy Products")]
    public string magnetictherapyproducts { get; set; }

    // Occurs 1
    [JsonPropertyName("Padfolio Ring Binders")]
    public string padfolioringbinders { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Boxing Trunks")]
    public string womensboxingtrunks { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Ice Skating Dresses")]
    public string womensiceskatingdresses { get; set; }

    // Occurs 1
    [JsonPropertyName("Backcountry Equipment")]
    public string backcountryequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Billiard Gloves")]
    public string billiardgloves { get; set; }

    // Occurs 1
    [JsonPropertyName("Diving Backpacks")]
    public string divingbackpacks { get; set; }

    // Occurs 1
    [JsonPropertyName("Crib Bed Skirts")]
    public string cribbedskirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Sleepwear")]
    public string menssleepwear { get; set; }

    // Occurs 1
    [JsonPropertyName("Binocular, Camera & Camcorder Straps")]
    public string binocularcameracamcorderstraps { get; set; }

    // Occurs 1
    [JsonPropertyName("Dog Memorials & Funerary")]
    public string dogmemorialsfunerary { get; set; }

    // Occurs 1
    [JsonPropertyName("Craft & Hobby Fabric")]
    public string crafthobbyfabric { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Thermal Underwear Union Suits")]
    public string mensthermalunderwearunionsuits { get; set; }

    // Occurs 1
    [JsonPropertyName("Christmas Stockings & Holders")]
    public string christmasstockingsholders { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Cheerleading Clothing")]
    public string boyscheerleadingclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Accessory Organizers")]
    public string accessoryorganizers { get; set; }

    // Occurs 1
    [JsonPropertyName("Closet Clothes Hangers")]
    public string closetclotheshangers { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Nightgowns & Sleep Shirts")]
    public string girlsnightgownssleepshirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Laptop Accessories")]
    public string laptopaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Electric Guitar Bags & Cases")]
    public string electricguitarbagscases { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Sunglasses")]
    public string sportsfansunglasses { get; set; }

    // Occurs 1
    [JsonPropertyName("Curling Equipment")]
    public string curlingequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Barometers")]
    public string barometers { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Running Shorts")]
    public string girlsrunningshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Jewelry Making Bead Looms")]
    public string jewelrymakingbeadlooms { get; set; }

    // Occurs 1
    [JsonPropertyName("Hanging Shoe Organizers")]
    public string hangingshoeorganizers { get; set; }

    // Occurs 1
    [JsonPropertyName("Beam Trolleys")]
    public string beamtrolleys { get; set; }

    // Occurs 1
    [JsonPropertyName("Camping & Hiking Hydration Canteens")]
    public string campinghikinghydrationcanteens { get; set; }

    // Occurs 1
    [JsonPropertyName("Protective Body Equipment")]
    public string protectivebodyequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Cheerleading Uniforms")]
    public string womenscheerleadinguniforms { get; set; }

    // Occurs 1
    [JsonPropertyName("Eye Care Products")]
    public string eyecareproducts { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Cycling Leg Warmers")]
    public string boyscyclinglegwarmers { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Cycling Leg Warmers")]
    public string girlscyclinglegwarmers { get; set; }

    // Occurs 1
    [JsonPropertyName("Decorative Urns")]
    public string decorativeurns { get; set; }

    // Occurs 1
    [JsonPropertyName("Shoe Treatments & Polishes")]
    public string shoetreatmentspolishes { get; set; }

    // Occurs 1
    [JsonPropertyName("Wall Safes")]
    public string wallsafes { get; set; }

    // Occurs 1
    [JsonPropertyName("Identification Wristbands")]
    public string identificationwristbands { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Novelty One-Piece Pajamas")]
    public string girlsnoveltyonepiecepajamas { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Activewear Vests")]
    public string girlsactivewearvests { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Activewear Vests")]
    public string boysactivewearvests { get; set; }

    // Occurs 1
    [JsonPropertyName("Fishing Nets")]
    public string fishingnets { get; set; }

    // Occurs 1
    [JsonPropertyName("Bicycle Car Racks")]
    public string bicyclecarracks { get; set; }

    // Occurs 1
    [JsonPropertyName("Smartwatch Accessories")]
    public string smartwatchaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Backpacking Boots")]
    public string womensbackpackingboots { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Baby Booties & Socks")]
    public string sportsfanbabybootiessocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Cycling Bib Pants")]
    public string womenscyclingbibpants { get; set; }

    // Occurs 1
    [JsonPropertyName("Automotive Interior Accessories")]
    public string automotiveinterioraccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Professional Medical Supplies")]
    public string professionalmedicalsupplies { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Medical Scrub Shirts")]
    public string mensmedicalscrubshirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Gymnastics Clothing")]
    public string gymnasticsclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Socks")]
    public string boyssocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Cosmetic Display Cases")]
    public string cosmeticdisplaycases { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Boxing Robes")]
    public string mensboxingrobes { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Dance Tops")]
    public string womensdancetops { get; set; }

    // Occurs 1
    [JsonPropertyName("Disposable Diapers")]
    public string disposablediapers { get; set; }

    // Occurs 1
    [JsonPropertyName("Safety Boot & Shoe Covers")]
    public string safetybootshoecovers { get; set; }

    // Occurs 1
    [JsonPropertyName("Color-Coding Labels")]
    public string colorcodinglabels { get; set; }

    // Occurs 1
    [JsonPropertyName("Nursery Décor")]
    public string nurserydécor { get; set; }

    // Occurs 1
    [JsonPropertyName("Martial Arts Headgear")]
    public string martialartsheadgear { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Yoga Pants")]
    public string mensyogapants { get; set; }

    // Occurs 1
    [JsonPropertyName("Swim Vests")]
    public string swimvests { get; set; }

    // Occurs 1
    [JsonPropertyName("Baseballs")]
    public string baseballs { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Basketball Shoes")]
    public string womensbasketballshoes { get; set; }

    // Occurs 1
    [JsonPropertyName("Tool Belts")]
    public string toolbelts { get; set; }

    // Occurs 1
    [JsonPropertyName("Breast Pump Accessories")]
    public string breastpumpaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Tap Dancing Equipment")]
    public string tapdancingequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Car Care")]
    public string carcare { get; set; }

    // Occurs 1
    [JsonPropertyName("Baby Girls' Tops")]
    public string babygirlstops { get; set; }

    // Occurs 1
    [JsonPropertyName("Golf Club Bag Accessories")]
    public string golfclubbagaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Grocery & Gourmet Food")]
    public string grocerygourmetfood { get; set; }

    // Occurs 1
    [JsonPropertyName("Kids' Party Headwear")]
    public string kidspartyheadwear { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Exotic Underwear")]
    public string womensexoticunderwear { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Lacrosse Clothing")]
    public string womenslacrosseclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' School Uniform Clothing")]
    public string boysschooluniformclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Lazy Susans")]
    public string lazysusans { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Running Clothing Accessories")]
    public string womensrunningclothingaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Arts, Crafts & Sewing Storage")]
    public string artscraftssewingstorage { get; set; }

    // Occurs 1
    [JsonPropertyName("Gardening Gloves")]
    public string gardeninggloves { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Hiking & Outdoor Recreation 3-in-1 Jackets")]
    public string menshikingoutdoorrecreation3in1jackets { get; set; }

    // Occurs 1
    [JsonPropertyName("BMX Equipment")]
    public string bmxequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Snow Sport Helmets")]
    public string snowsporthelmets { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Activewear Tank Tops")]
    public string boysactiveweartanktops { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Novelty Cowboy Hats")]
    public string mensnoveltycowboyhats { get; set; }

    // Occurs 1
    [JsonPropertyName("Doll Clothing & Accessories Sets")]
    public string dollclothingaccessoriessets { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Novelty Bras")]
    public string womensnoveltybras { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Cycling Tights")]
    public string boyscyclingtights { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Soccer Shoes")]
    public string girlssoccershoes { get; set; }

    // Occurs 1
    [JsonPropertyName("Weight Lifting Belts")]
    public string weightliftingbelts { get; set; }

    // Occurs 1
    [JsonPropertyName("Archery Accessories")]
    public string archeryaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Skate & Skateboarding Padded Shorts")]
    public string skateskateboardingpaddedshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Doll Clothing")]
    public string dollclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Personal Pill Organizers")]
    public string personalpillorganizers { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Slipper Socks")]
    public string boysslippersocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Gymnastics Hand Grips")]
    public string gymnasticshandgrips { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Lacrosse Clothing")]
    public string menslacrosseclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Football Protective Gear")]
    public string footballprotectivegear { get; set; }

    // Occurs 1
    [JsonPropertyName("Baby Boys' Footies & Rompers")]
    public string babyboysfootiesrompers { get; set; }

    // Occurs 1
    [JsonPropertyName("Cell Phone Charging Stations")]
    public string cellphonechargingstations { get; set; }

    // Occurs 1
    [JsonPropertyName("Safety Vests")]
    public string safetyvests { get; set; }

    // Occurs 1
    [JsonPropertyName("Kids' Party Balloons")]
    public string kidspartyballoons { get; set; }

    // Occurs 1
    [JsonPropertyName("Baseball Accessories")]
    public string baseballaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Toy Foam Blasters & Guns")]
    public string toyfoamblastersguns { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Cuff Bracelets")]
    public string girlscuffbracelets { get; set; }

    // Occurs 1
    [JsonPropertyName("Kendo Suits")]
    public string kendosuits { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Tools & Home Improvement")]
    public string sportsfantoolshomeimprovement { get; set; }

    // Occurs 1
    [JsonPropertyName("Hard Hat Accessories")]
    public string hardhataccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Bathroom Sink Vanity Trays")]
    public string bathroomsinkvanitytrays { get; set; }

    // Occurs 1
    [JsonPropertyName("Storage Drawer Units")]
    public string storagedrawerunits { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Football Equipment")]
    public string sportsfanfootballequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Ice Hockey Socks")]
    public string mensicehockeysocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Refillable Cosmetic Containers")]
    public string refillablecosmeticcontainers { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Athletic Jackets")]
    public string girlsathleticjackets { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Tennis Shirts")]
    public string girlstennisshirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Card Games")]
    public string cardgames { get; set; }

    // Occurs 1
    [JsonPropertyName("Vases")]
    public string vases { get; set; }

    // Occurs 1
    [JsonPropertyName("Nintendo Legacy Systems")]
    public string nintendolegacysystems { get; set; }

    // Occurs 1
    [JsonPropertyName("Pins & Tacks")]
    public string pinstacks { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Hiking Socks")]
    public string boyshikingsocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Tennis Skirts")]
    public string girlstennisskirts { get; set; }

    // Occurs 1
    [JsonPropertyName("Arts & Crafts Easels")]
    public string artscraftseasels { get; set; }

    // Occurs 1
    [JsonPropertyName("Arm Supports")]
    public string armsupports { get; set; }

    // Occurs 1
    [JsonPropertyName("Hot Water Bottles")]
    public string hotwaterbottles { get; set; }

    // Occurs 1
    [JsonPropertyName("Knitting & Crochet Notions")]
    public string knittingcrochetnotions { get; set; }

    // Occurs 1
    [JsonPropertyName("Eyeglass Cleaning Tissues & Cloths")]
    public string eyeglasscleaningtissuescloths { get; set; }

    // Occurs 1
    [JsonPropertyName("Maternity Shorts")]
    public string maternityshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Skiing & Snowboarding Socks")]
    public string girlsskiingsnowboardingsocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Neon Signs")]
    public string neonsigns { get; set; }

    // Occurs 1
    [JsonPropertyName("Craft Feathers & Boas")]
    public string craftfeathersboas { get; set; }

    // Occurs 1
    [JsonPropertyName("Fasteners")]
    public string fasteners { get; set; }

    // Occurs 1
    [JsonPropertyName("Cards & Card Stock")]
    public string cardscardstock { get; set; }

    // Occurs 1
    [JsonPropertyName("Wine Accessory Sets")]
    public string wineaccessorysets { get; set; }

    // Occurs 1
    [JsonPropertyName("Napkin Rings")]
    public string napkinrings { get; set; }

    // Occurs 1
    [JsonPropertyName("Maternity Sweaters")]
    public string maternitysweaters { get; set; }

    // Occurs 1
    [JsonPropertyName("Jewelry Making Polishing & Buffing")]
    public string jewelrymakingpolishingbuffing { get; set; }

    // Occurs 1
    [JsonPropertyName("Check Writers")]
    public string checkwriters { get; set; }

    // Occurs 1
    [JsonPropertyName("Kids' MP3 Players")]
    public string kidsmp3players { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Hand Fans")]
    public string womenshandfans { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Fleece Jackets & Coats")]
    public string girlsfleecejacketscoats { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Dance Clothing")]
    public string boysdanceclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Christening Gifts & Gowns")]
    public string christeninggiftsgowns { get; set; }

    // Occurs 1
    [JsonPropertyName("Nursery Hampers")]
    public string nurseryhampers { get; set; }

    // Occurs 1
    [JsonPropertyName("Baby Boys' Swimwear Sunsuits")]
    public string babyboysswimwearsunsuits { get; set; }

    // Occurs 1
    [JsonPropertyName("Beaded Appliqué Patches")]
    public string beadedappliquépatches { get; set; }

    // Occurs 1
    [JsonPropertyName("Child Safety Booster Car Seats")]
    public string childsafetyboostercarseats { get; set; }

    // Occurs 1
    [JsonPropertyName("Ski & Snowboard Tuning Equipment")]
    public string skisnowboardtuningequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Office Carrying Cases")]
    public string officecarryingcases { get; set; }

    // Occurs 1
    [JsonPropertyName("Breastfeeding Supplies")]
    public string breastfeedingsupplies { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Golf Gift Sets")]
    public string sportsfangolfgiftsets { get; set; }

    // Occurs 1
    [JsonPropertyName("Golf Gloves")]
    public string golfgloves { get; set; }

    // Occurs 1
    [JsonPropertyName("Woks & Stir-Fry Pans")]
    public string woksstirfrypans { get; set; }

    // Occurs 1
    [JsonPropertyName("Press On False Nails")]
    public string pressonfalsenails { get; set; }

    // Occurs 1
    [JsonPropertyName("Flower Plants & Seeds")]
    public string flowerplantsseeds { get; set; }

    // Occurs 1
    [JsonPropertyName("Kids' Paper Craft Kits")]
    public string kidspapercraftkits { get; set; }

    // Occurs 1
    [JsonPropertyName("Automotive Interior Mirrors")]
    public string automotiveinteriormirrors { get; set; }

    // Occurs 1
    [JsonPropertyName("Household Supplies")]
    public string householdsupplies { get; set; }

    // Occurs 1
    [JsonPropertyName("Scrapbooking Embellishments")]
    public string scrapbookingembellishments { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Cycling Rain Pants")]
    public string menscyclingrainpants { get; set; }

    // Occurs 1
    [JsonPropertyName("Sauna Accessories")]
    public string saunaaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Snowboarding Pants")]
    public string girlssnowboardingpants { get; set; }

    // Occurs 1
    [JsonPropertyName("Gift Wrap Boxes")]
    public string giftwrapboxes { get; set; }

    // Occurs 1
    [JsonPropertyName("Maternity Nursing Sleep Shirts & Nightgowns")]
    public string maternitynursingsleepshirtsnightgowns { get; set; }

    // Occurs 1
    [JsonPropertyName("Office Storage Supplies")]
    public string officestoragesupplies { get; set; }

    // Occurs 1
    [JsonPropertyName("Christmas Snow Globes")]
    public string christmassnowglobes { get; set; }

    // Occurs 1
    [JsonPropertyName("Medical Eye Patches")]
    public string medicaleyepatches { get; set; }

    // Occurs 1
    [JsonPropertyName("Desk Supplies, Organizers & Dispensers")]
    public string desksuppliesorganizersdispensers { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Triathlon Skinsuits")]
    public string womenstriathlonskinsuits { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Dress Coats")]
    public string girlsdresscoats { get; set; }

    // Occurs 1
    [JsonPropertyName("Camera Bags & Cases")]
    public string camerabagscases { get; set; }

    // Occurs 1
    [JsonPropertyName("Retail Mannequins")]
    public string retailmannequins { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Gymnastics Clothing")]
    public string womensgymnasticsclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Camping Personal Care Products")]
    public string campingpersonalcareproducts { get; set; }

    // Occurs 1
    [JsonPropertyName("Cheerleading Equipment")]
    public string cheerleadingequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Back Cushions & Seat Cushions")]
    public string backcushionsseatcushions { get; set; }

    // Occurs 1
    [JsonPropertyName("Body Skin Care Products")]
    public string bodyskincareproducts { get; set; }

    // Occurs 1
    [JsonPropertyName("Body Scrubs & Treatments")]
    public string bodyscrubstreatments { get; set; }

    // Occurs 1
    [JsonPropertyName("Kids' Lunch Bags")]
    public string kidslunchbags { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Novelty Sweaters")]
    public string womensnoveltysweaters { get; set; }

    // Occurs 1
    [JsonPropertyName("Leisure Sports & Games Equipment")]
    public string leisuresportsgamesequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Laptop Messenger & Shoulder Bags")]
    public string laptopmessengershoulderbags { get; set; }

    // Occurs 1
    [JsonPropertyName("Cigar Accessories & Humidors")]
    public string cigaraccessorieshumidors { get; set; }

    // Occurs 1
    [JsonPropertyName("Karate Suit Belts")]
    public string karatesuitbelts { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Body Piercing Retainers")]
    public string womensbodypiercingretainers { get; set; }

    // Occurs 1
    [JsonPropertyName("Kitchen Utensils & Gadgets")]
    public string kitchenutensilsgadgets { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Athletic Underwear")]
    public string womensathleticunderwear { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Slipper Socks")]
    public string mensslippersocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Makeup Bags & Cases")]
    public string makeupbagscases { get; set; }

    // Occurs 1
    [JsonPropertyName("Personal Makeup Mirrors")]
    public string personalmakeupmirrors { get; set; }

    // Occurs 1
    [JsonPropertyName("Team Practice Vests")]
    public string teampracticevests { get; set; }

    // Occurs 1
    [JsonPropertyName("Compact & Travel Mirrors")]
    public string compacttravelmirrors { get; set; }

    // Occurs 1
    [JsonPropertyName("Award Medals")]
    public string awardmedals { get; set; }

    // Occurs 1
    [JsonPropertyName("Cigarette Holders")]
    public string cigaretteholders { get; set; }

    // Occurs 1
    [JsonPropertyName("Towel Racks")]
    public string towelracks { get; set; }

    // Occurs 1
    [JsonPropertyName("Boxing Clothing")]
    public string boxingclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Strength Training Bars")]
    public string strengthtrainingbars { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Dance Shoes")]
    public string mensdanceshoes { get; set; }

    // Occurs 1
    [JsonPropertyName("Diving Fins")]
    public string divingfins { get; set; }

    // Occurs 1
    [JsonPropertyName("Diaper Changing Totes")]
    public string diaperchangingtotes { get; set; }

    // Occurs 1
    [JsonPropertyName("Closet Mounted Storage & Organization Systems")]
    public string closetmountedstorageorganizationsystems { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Hiking Shorts")]
    public string boyshikingshorts { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Soccer Equipment")]
    public string sportsfansoccerequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Cake Toppers")]
    public string caketoppers { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Paddling Clothing")]
    public string womenspaddlingclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Volleyball Protective Gear")]
    public string volleyballprotectivegear { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Exotic Underwear Briefs")]
    public string mensexoticunderwearbriefs { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Ice Hockey Clothing")]
    public string boysicehockeyclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Camera & Photo Lighting")]
    public string cameraphotolighting { get; set; }

    // Occurs 1
    [JsonPropertyName("Combination Locks")]
    public string combinationlocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Xbox Series X & S Consoles, Games & Accessories")]
    public string xboxseriesxsconsolesgamesaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Scarves")]
    public string sportsfanscarves { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Medical Scrub Sets")]
    public string mensmedicalscrubsets { get; set; }

    // Occurs 1
    [JsonPropertyName("Toddler Stuffed Animals & Toys")]
    public string toddlerstuffedanimalstoys { get; set; }

    // Occurs 1
    [JsonPropertyName("Pilates Circles")]
    public string pilatescircles { get; set; }

    // Occurs 1
    [JsonPropertyName("Exotic Apparel")]
    public string exoticapparel { get; set; }

    // Occurs 1
    [JsonPropertyName("Powersports Rain Boot Covers")]
    public string powersportsrainbootcovers { get; set; }

    // Occurs 1
    [JsonPropertyName("Replacement Bike Cleats")]
    public string replacementbikecleats { get; set; }

    // Occurs 1
    [JsonPropertyName("Jewelry Patterns")]
    public string jewelrypatterns { get; set; }

    // Occurs 1
    [JsonPropertyName("Baking Dishes")]
    public string bakingdishes { get; set; }

    // Occurs 1
    [JsonPropertyName("Sleeveless Wetsuits")]
    public string sleevelesswetsuits { get; set; }

    // Occurs 1
    [JsonPropertyName("Joint Wraps")]
    public string jointwraps { get; set; }

    // Occurs 1
    [JsonPropertyName("Camping Pillows")]
    public string campingpillows { get; set; }

    // Occurs 1
    [JsonPropertyName("Bungee Cords")]
    public string bungeecords { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Novelty Neckties")]
    public string boysnoveltyneckties { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Calf Socks")]
    public string boyscalfsocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Horse Hoof Picks")]
    public string horsehoofpicks { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Drawstring Bags")]
    public string sportsfandrawstringbags { get; set; }

    // Occurs 1
    [JsonPropertyName("Camping Tents")]
    public string campingtents { get; set; }

    // Occurs 1
    [JsonPropertyName("Maternity Nursing Knits & Tees")]
    public string maternitynursingknitstees { get; set; }

    // Occurs 1
    [JsonPropertyName("Men's Activewear Leggings")]
    public string mensactivewearleggings { get; set; }

    // Occurs 1
    [JsonPropertyName("Sewing Lace")]
    public string sewinglace { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Cycling Jerseys")]
    public string girlscyclingjerseys { get; set; }

    // Occurs 1
    [JsonPropertyName("Casino Equipment")]
    public string casinoequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Air Mattresses")]
    public string airmattresses { get; set; }

    // Occurs 1
    [JsonPropertyName("Hobby RC Quadcopters & Multirotors")]
    public string hobbyrcquadcoptersmultirotors { get; set; }

    // Occurs 1
    [JsonPropertyName("Teaching Materials")]
    public string teachingmaterials { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Soccer Jerseys")]
    public string girlssoccerjerseys { get; set; }

    // Occurs 1
    [JsonPropertyName("Furniture Corner & Edge Safety Bumpers")]
    public string furniturecorneredgesafetybumpers { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Golf Clothing")]
    public string girlsgolfclothing { get; set; }

    // Occurs 1
    [JsonPropertyName("Candy Servers")]
    public string candyservers { get; set; }

    // Occurs 1
    [JsonPropertyName("Sorting & Stacking Toys")]
    public string sortingstackingtoys { get; set; }

    // Occurs 1
    [JsonPropertyName("Sewing Thread")]
    public string sewingthread { get; set; }

    // Occurs 1
    [JsonPropertyName("Waist Trimmers")]
    public string waisttrimmers { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Bodysuit Tops")]
    public string womensbodysuittops { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Down Jackets & Coats")]
    public string boysdownjacketscoats { get; set; }

    // Occurs 1
    [JsonPropertyName("Magic Kits & Accessories")]
    public string magickitsaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Equestrian Saddle Pads")]
    public string equestriansaddlepads { get; set; }

    // Occurs 1
    [JsonPropertyName("Tapestries")]
    public string tapestries { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Key Chains")]
    public string sportsfankeychains { get; set; }

    // Occurs 1
    [JsonPropertyName("Baby Activity & Entertainment Products")]
    public string babyactivityentertainmentproducts { get; set; }

    // Occurs 1
    [JsonPropertyName("Medical Isolation Gowns")]
    public string medicalisolationgowns { get; set; }

    // Occurs 1
    [JsonPropertyName("Outdoor Backpack Accessories")]
    public string outdoorbackpackaccessories { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Hiking & Outdoor Recreation Softshell Jackets")]
    public string girlshikingoutdoorrecreationsoftshelljackets { get; set; }

    // Occurs 1
    [JsonPropertyName("Triathlon Equipment")]
    public string triathlonequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Baby Stroller Bunting Bags")]
    public string babystrollerbuntingbags { get; set; }

    // Occurs 1
    [JsonPropertyName("Electric Breast Pumps")]
    public string electricbreastpumps { get; set; }

    // Occurs 1
    [JsonPropertyName("Jar Candles")]
    public string jarcandles { get; set; }

    // Occurs 1
    [JsonPropertyName("Football Facemasks")]
    public string footballfacemasks { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Panties")]
    public string womenspanties { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Tennis Dresses")]
    public string girlstennisdresses { get; set; }

    // Occurs 1
    [JsonPropertyName("Boys' Hiking & Outdoor Recreation Softshell Jackets")]
    public string boyshikingoutdoorrecreationsoftshelljackets { get; set; }

    // Occurs 1
    [JsonPropertyName("Music Recording Equipment")]
    public string musicrecordingequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Girls' Running Socks")]
    public string girlsrunningsocks { get; set; }

    // Occurs 1
    [JsonPropertyName("Women's Bikini Panties")]
    public string womensbikinipanties { get; set; }

    // Occurs 1
    [JsonPropertyName("Sports Fan Outdoor Flags")]
    public string sportsfanoutdoorflags { get; set; }

    // Occurs 1
    [JsonPropertyName("Diving & Snorkeling Equipment")]
    public string divingsnorkelingequipment { get; set; }

    // Occurs 1
    [JsonPropertyName("Miniature Novelty Toys")]
    public string miniaturenoveltytoys { get; set; }

    // Occurs 1
    [JsonPropertyName("Camping Survival Kits")]
    public string campingsurvivalkits { get; set; }

    // Occurs 1
    [JsonPropertyName("Thermocoolers")]
    public string thermocoolers { get; set; }

    // Occurs 1
    [JsonPropertyName("Softball Clothing")]
    public string softballclothing { get; set; }

}
public class Author
{
    // Occurs 109
    [JsonPropertyName("avatar")]
    public string avatar { get; set; }

    // Occurs 109
    [JsonPropertyName("name")]
    public string name { get; set; }

    // Occurs 109
    [JsonPropertyName("about")]
    public string about { get; set; }


}
