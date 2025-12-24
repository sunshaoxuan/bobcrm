namespace BobCrm.App.Services;

public record InteractionToastMessage(Guid Id, string Title, string? Body, InteractionToastTone Tone, DateTime CreatedAt);
