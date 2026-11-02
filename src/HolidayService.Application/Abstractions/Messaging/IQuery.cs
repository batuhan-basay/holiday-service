using HolidayService.Domain.Common;
using MediatR;

namespace HolidayService.Application.Abstractions.Messaging;

/// <summary>
/// A read-only request. Every query answers with a <see cref="Result{TResponse}"/> so
/// expected failures (unknown country, provider down) never travel as exceptions.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
