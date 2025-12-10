using MediatR;

namespace ArchoCybo.Application.Features.Auth;

public record LoginCommand(string Username, string Password) : IRequest<string>;
