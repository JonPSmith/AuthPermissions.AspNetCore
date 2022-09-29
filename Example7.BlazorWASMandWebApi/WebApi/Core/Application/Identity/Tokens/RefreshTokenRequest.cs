namespace Example7.BlazorWASMandWebApi.Application.Identity.Tokens;

public record RefreshTokenRequest(string Token, string RefreshToken);