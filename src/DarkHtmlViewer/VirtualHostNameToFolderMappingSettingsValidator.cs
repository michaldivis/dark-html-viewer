using FluentValidation;

namespace DarkHtmlViewer
{
    public class VirtualHostNameToFolderMappingSettingsValidator : AbstractValidator<VirtualHostNameToFolderMappingSettings>
    {
        public VirtualHostNameToFolderMappingSettingsValidator()
        {
            When(settings => settings.IsEnabled, () =>
            {
                RuleFor(a => a.Hostname).NotEmpty();
                RuleFor(a => a.FolderPath).MustBeAnExistingFolderPath();
            });
        }
    }
}
