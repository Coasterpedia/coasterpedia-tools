namespace CoasterpediaTools.Authentication.Handler;

public record TokenResult(bool Success, UserToken? Token = null);