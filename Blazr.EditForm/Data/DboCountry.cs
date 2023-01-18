namespace Blazr.EditForm.Data
{
    public record DboCountry
    {
        public Guid Uid { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
    }
}
