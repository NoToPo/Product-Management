namespace ProductManagement.Domain.Enums;

/// <summary>Lifecycle state of a product in the catalogue.</summary>
public enum ProductStatus
{
    /// <summary>Created but not yet visible to shoppers.</summary>
    Draft = 0,

    /// <summary>Published and purchasable.</summary>
    Active = 1,

    /// <summary>Retired; hidden from listings but retained for history.</summary>
    Archived = 2
}
