
using Microsoft.AspNetCore.Mvc;

namespace Example7.BlazorWASMandWebApi.Host.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class VersionedApiController : BaseApiController
{
}

