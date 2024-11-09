using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using SchulCloud.Frontend.Options;
using System.Globalization;

namespace SchulCloud.Frontend.Components;

public sealed partial class CookieConsent : ComponentBase
{
    #region Injections
    [Inject]
    private IOptionsSnapshot<PresentationOptions> PresentationOptionsAccessor { get; set; } = default!;
    #endregion

    private CultureInfo Culture
    {
        get {
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;
            return _supportedCultures.FirstOrDefault(supportedCulture => supportedCulture.TwoLetterISOLanguageName == currentCulture.TwoLetterISOLanguageName)
                ??_supportedCultures[0];
        }
    }

    private readonly CultureInfo[] _supportedCultures = [
        new("en"),     // English
        new("en-GB"),     // English (UK)
        new("de"),     // Germany
        new("fr"),     // French
        new("es"),     // Spanish
        new("ca-ES"),     // Catalan
        new("it"),     // Italian
        new("sv"),     // Swedish
        new("no"),     // Norwegian
        new("nl"),     // Dutch
        new("pt"),     // Portugese
        new("fi"),     // Finnish
        new("hu"),     // Hungarian
        new("cs"),     // Czech
        new("hr"),     // Croatian
        new("da"),     // Danish
        new("sk"),     // Slovak
        new("sl"),     // Slovenian
        new("pl"),     // Polish
        new("el"),     // Greek
        new("he"),     // Hebrew
        new("mk"),     // Macedonian
        new("uk"),     // Ukrainian
        new("ro"),     // Romanian
        new("sr"),     // Serbian
        new("et"),     // Estonian
        new("lt"),     // Lithuanian
        new("lv"),     // Latvian
        new("ru"),     // Russian
        new("bg"),     // Bulgarian
        new("cy"),     // Welsh
        new("ja"),     // Japanese
        new("ar"),     // Arabic
        new("tr"),     // Turkish
        new("zh-WT"),     // Traditional Chinese
        new("oc")     // Occitan
        ];
}
