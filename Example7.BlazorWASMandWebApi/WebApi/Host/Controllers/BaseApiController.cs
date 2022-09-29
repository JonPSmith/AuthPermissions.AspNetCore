using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Example7.BlazorWASMandWebApi.Host.Controllers;

[ApiController]
public class BaseApiController : ControllerBase
{
    private ISender _mediator = null!;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}