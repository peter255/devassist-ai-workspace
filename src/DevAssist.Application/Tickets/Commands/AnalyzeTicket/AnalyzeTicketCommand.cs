using DevAssist.Contracts.Tickets;
using MediatR;

namespace DevAssist.Application.Tickets.Commands.AnalyzeTicket;

public sealed record AnalyzeTicketCommand(string Text) : IRequest<AnalyzeTicketResponse>;
