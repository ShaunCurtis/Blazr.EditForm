Question

>> How do you prevent navigation when an edit form is dirty?

This article demonstrates how to do so.

Data derived from any data source, such as an API or database, should be read only.  It's a copy of the originsl data at thr time it was copied.  It's not the original data.  Treat it as immutable, use a  `record` value object rather than `class` objects to represent the data.

This is my demo record.  It's a simple record of the name and registration code for a country.  All the properties are declared as immutable.

```csharp
public record DboCountry
{
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}
```

I've built a simple dummy data pipeline with get and save async methods to emulate normal API calls.

```csharp
public interface ICountryDataBroker
{
    public ValueTask<DboCountry> GetItemAsync(Guid uid);
    public ValueTask<bool> SaveItemAsync(DboCountry item);
}

public class CountryAPIDataBroker
{
    // Normally inject the HttpClient 
    public CountryAPIDataBroker() { }

    public async ValueTask<DboCountry> GetItemAsync(Guid uid)
    {
        // Emulate getting record from the API
        await Task.Delay(500);
        return new() { Uid = uid, Name = "United Kingdom", Code = "UK" };
    }

    public async ValueTask<bool> SaveItemAsync(DboCountry item)
    {
        // Emulate saving the record to the API
        await Task.Delay(500);
        return true;
    }
}
```

To edit a `DboCountry` record we need an editable object.  You can add validation to this class or build the necessary fluid validation classes from it.  I've added Fluent Validation to demonstrate,

```csharp
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
```

Next we need a Presentation layer servive to hold and manage the data used by the edit form.  The presenter is a `Transient` service, so it's important not to do anything in it that requires implementing `IDisposable`.

```csharp
public class CountryEditorPresenter
{
    private ICountryDataBroker _broker;

    public CountryEditContext Record { get; private set; } = new(new());

    public CountryEditorPresenter(ICountryDataBroker broker)
        => _broker = broker;

    public async ValueTask<bool> GetItemAsync(Guid uid)
    {
        var record = await _broker.GetItemAsync(uid);

        //Logic to check we got a record

        // Create a new record edit context
        this.Record = new(record);
        
        return true;
    }

    public async ValueTask<bool> SaveItemAsync()
    {
        // Get the record to save
        var saveRecord = this.Record.AsRecord;
        await _broker.SaveItemAsync(saveRecord);

        // Create a new record edit context
        this.Record = new(saveRecord);

        return true;
    }

    public void Reset()
        => this.Record.Reset();
}
```

The services are registered as follows:

```csharp
builder.Services.AddScoped<ICountryDataBroker, CountryAPIDataBroker>();
builder.Services.AddTransient<CountryEditorPresenter>();
builder.Services.AddTransient<IValidator<CountryEditContext>, CountryValidator>();
```

Finally the edit form.  It's a normal form with the button state controlled by the record edit state.  

The form locking is accomplished by:

1. Wiring the NavigationManager's `RegisterLocationChangingHandler` to a handler that prevents/allows navigation based on the form state.  This prevents intra SPA navigation.
2. Adding a `NavigationLock` component to the form and wiring it up to the form state. This prevents external navigation including using the back button.

```html
@page "/"
@inject CountryEditorPresenter Presenter
@inject NavigationManager NavManager
@inject IJSRuntime Js
@implements IDisposable

@if (_editContext is null)
{
    <div class="alert alert-info">Loading</div>
}
else
{
    <PageTitle>Index</PageTitle>
    <EditForm EditContext=_editContext>
        <FluentValidationValidator DisableAssemblyScanning="@true" />
        <BlazrEditStateTracker />
        <div class="mb-2">
            <label class="form-label">Country</label>
            <BlazrInputText class="form-control" @bind-Value="this.Presenter.Record.Name" />
            <ValidationMessage For="() => this.Presenter.Record.Name" />
        </div>
        <div class="mb-2">
            <label class="form-label">Code</label>
            <BlazrInputText class="form-control" @bind-Value=this.Presenter.Record.Code />
            <ValidationMessage For="() => this.Presenter.Record.Code" />
        </div>
        <div class="mb-2 text-end">
            <button class="btn btn-warning" hidden="@_isClean" @onclick="this.Reset">Reset</button>
            <button class="btn btn-success" disabled="@_isClean" @onclick="this.Save">Save</button>
            <button class="btn btn-danger" hidden="@_isClean" @onclick="this.ExitWithoutSave">Exit Without Saving</button>
            <button class="btn btn-dark" hidden="@_isDirty" @onclick="this.Exit">Exit</button>
        </div>
        <div class="mb-2">
            <ValidationSummary />
        </div>
    </EditForm>

    <NavigationLock ConfirmExternalNavigation="_isDirty" />
}

@code {
    private EditContext _editContext = default!;
    private IDisposable? _navLockerDispose;
    private BlazrEditStateStore? _editStateStore => _editContext?.GetStateStore();
    private bool _isDirty => _editStateStore?.IsDirty() ?? false;
    private bool _isClean => !_isDirty;

    protected override async Task OnInitializedAsync()
    {
        await Presenter.GetItemAsync(Guid.NewGuid());
        _editContext = new EditContext(Presenter.Record);
        _navLockerDispose = NavManager.RegisterLocationChangingHandler(this.OnLocationChanging);
    }

    private ValueTask OnLocationChanging(LocationChangingContext context)
    {
        // Prevent navigation if the edit context is dirty
        if (_isDirty)
            context.PreventNavigation();

        return ValueTask.CompletedTask;
    }

    private async Task Save()
    {
        if (_isClean)
            return;

        // Validate the form
        if (_editContext?.Validate() ?? false)
        {
            await this.Presenter.SaveItemAsync();
            // mock an async call to the data pipeline to save the record
            var updatedRecord = this.Presenter.Record.AsRecord;
            await Task.Delay(100);
            // Error handling code here

            // This will reset the edit context and the EditStateTracker
            _editContext = new EditContext(this.Presenter.Record);
        }

    }

    private void Reset()
    {
        if (_isClean)
            return;

        this.Presenter.Reset();
            _editStateStore?.Reset();
    }

    private void Exit()
    {
        // Belt and braces check before exiting
        if (_isClean)
            this.NavManager.NavigateTo("/counter");
    }

    private async Task ExitWithoutSave()
    {
        // Confirm with a Js Confirm popup
        bool confirmed = await Js.InvokeAsync<bool>("confirm", "Are you sure you want to exit without saving?");

        if (confirmed)
        {
            // Reset the EditStateStore - it will now be clean, so we can exit
            _editStateStore?.Reset();
            this.NavManager.NavigateTo("/counter");
        }
    }

    public void Dispose()
        => _navLockerDispose?.Dispose();
}
```

### For reference 

This is `RazrInputText`:

```html
@namespace Blazr.EditForm
@inherits InputText

<input @attributes="AdditionalAttributes"
       class="@CssClass"
       @bind="CurrentValueAsString"
       @bind:event="oninput" />
```

This is `CountryValidator`:

```csharp
public class CountryValidator : AbstractValidator<CountryEditContext>
{
    public CountryValidator()
    {
        RuleFor(p => p.Name)
        .NotEmpty().WithMessage("You must enter a Name")
        .MaximumLength(50).WithMessage("Name cannot be longer than 50 characters");

        RuleFor(p => p.Code)
        .NotEmpty().WithMessage("You must enter a Code for the Country")
        .MaximumLength(4).WithMessage("A country code is 1, 2, 3 or 4 letters");
    }
}
```

Note that the navigation features used to prevent navigation are new to Net7.0.