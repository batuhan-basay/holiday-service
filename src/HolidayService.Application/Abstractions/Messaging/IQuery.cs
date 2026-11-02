using HolidayService.Domain.Common;
using MediatR;

namespace HolidayService.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
