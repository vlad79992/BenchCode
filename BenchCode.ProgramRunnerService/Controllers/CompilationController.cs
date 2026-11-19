using BenchCode.ProgramRunnerService.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BenchCode.ProgramRunnerService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CompilationController : ControllerBase
    {
        [HttpPost(Name = "CompileCode")]
        public async Task<ActionResult<string>> CompileCode(CompileRequest request)
        {
            return CreatedAtAction(
                nameof(CompileCode),
                new { },
                Guid.CreateVersion7().ToString()
                );
        }
    }
}