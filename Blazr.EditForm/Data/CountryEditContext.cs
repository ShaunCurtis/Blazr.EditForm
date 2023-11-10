using Blazr.Core;

namespace Blazr.EditForm.Data;

public class CountryEditContext
{
    private DboCountry _baseRecord;
    public Guid Uid { get; private set; } = Guid.NewGuid();
    [TrackState] public string Name { get; set; } = string.Empty;
    [TrackState] public string Code { get; set; } = string.Empty;

    public CountryEditContext(DboCountry record)
    {
        _baseRecord = record;
        this.Load(record);
    }

    public void Load(DboCountry record) 
    {
        _baseRecord = record;
        this.Uid= record.Uid;
        this.Name= record.Name;
        this.Code= record.Code;
    }

    public DboCountry AsRecord
        => new DboCountry
        {
            Uid= this.Uid,
            Name= this.Name,
            Code= this.Code,
        };

    public void Reset()
        => this.Load(_baseRecord);
}
