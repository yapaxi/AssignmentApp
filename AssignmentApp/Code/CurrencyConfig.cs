namespace AssignmentApp.Code;

public record CurrencyConfig(
    IReadOnlyList<string> DefaultFiatCurrencySymbols
);
