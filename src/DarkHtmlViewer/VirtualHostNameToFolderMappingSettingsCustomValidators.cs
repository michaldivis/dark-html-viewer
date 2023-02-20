using FluentValidation;
using System.IO;

namespace DarkHtmlViewer;

public static class VirtualHostNameToFolderMappingSettingsCustomValidators
{
    public static IRuleBuilderOptions<T, string?> MustBeAnExistingFolderPath<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Must(text => IsExistingFolderPath(text)).WithMessage("The text is not an existing folder path");
    }

    private static bool IsExistingFolderPath(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var isExistingFolderPath = Directory.Exists(text);

        return isExistingFolderPath;
    }
}
