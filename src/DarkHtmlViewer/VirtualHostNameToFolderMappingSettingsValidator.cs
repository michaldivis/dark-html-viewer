using FluentValidation;

namespace DarkHtmlViewer;

public class VirtualHostNameToFolderMappingSettingsValidator : AbstractValidator<VirtualHostNameToFolderMappingSettings>
{
    public VirtualHostNameToFolderMappingSettingsValidator()
    {
        RuleFor(a => a.Hostname).NotEmpty();
        RuleFor(a => a.FolderPath).MustBeAnExistingFolderPath();
    }
}
