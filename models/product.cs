using System;
using System.Collections.Generic;

public class Product
{
    public static readonly Dictionary<ProductCategory, int> _categoryCounters = new Dictionary<ProductCategory, int>();

    public string ProductId { get; private set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public ProductCategory Category { get; }

    public Product(string name, int quantity, ProductCategory category)
    {
        Name = name;
        Quantity = quantity;
        Category = category;
        GenerateProductId();
    }

    private void GenerateProductId()
    {
        if (!_categoryCounters.ContainsKey(Category))
        {
            _categoryCounters[Category] = 0;
        }

        _categoryCounters[Category]++;

        string prefix = Category switch
        {
            ProductCategory.Technology => "T",
            ProductCategory.Beauty => "B",
            ProductCategory.HomeAppliance => "H",
            _ => "U"
        };

        ProductId = $"{prefix}{_categoryCounters[Category]:D4}";
    }

 }