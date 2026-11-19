using System.Collections.ObjectModel;
using CableCast.Models;
using CableCast.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CableCast.ViewModels;

public partial class ProfilesViewModel : ObservableObject
{
    private readonly ProfileStore _store;

    public ObservableCollection<Profile> Profiles => _store.Profiles;

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ApplyCommand), nameof(DeleteCommand))]
    private Profile? _selected;

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(SaveAsCommand))]
    private string _newName = "";

    [ObservableProperty] private string _active = "";
    [ObservableProperty] private string _message = "";

    public ProfilesViewModel(ProfileStore store)
    {
        _store = store;
        Active = Loc.Format("Profiles_Active", store.ActiveName);
        store.ActiveChanged += () => Active = Loc.Format("Profiles_Active", store.ActiveName);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Apply()
    {
        if (Selected is null) return;
        _store.Apply(Selected);
        Message = Loc.Format("Profiles_Applied", Selected.Name);
    }

    [RelayCommand(CanExecute = nameof(CanSaveAs))]
    private void SaveAs()
    {
        var name = NewName.Trim();
        var p = _store.SaveAs(name);
        if (p is null) { Message = Loc.Format("Profiles_NameReserved", name); return; }
        Message = Loc.Format("Profiles_Saved", p.Name);
        NewName = "";
        Selected = p;
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        if (Selected is null) return;
        var name = Selected.Name;
        if (_store.Delete(Selected)) { Message = Loc.Format("Profiles_Deleted", name); Selected = null; }
    }

    private bool HasSelection() => Selected is not null;
    private bool CanDelete() => Selected is { BuiltIn: false };
    private bool CanSaveAs() => !string.IsNullOrWhiteSpace(NewName);
}
