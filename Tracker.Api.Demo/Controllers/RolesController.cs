using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracker.Api.Demo.Database;
using Tracker.Api.Demo.Database.Entities;
using Tracker.AspNet.Attributes;

namespace Tracker.Api.Demo.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class RolesController(DatabaseContext dbContext) : ControllerBase
{
    [HttpGet]
    [Track(tables: "roles")]
    public ActionResult<IEnumerable<Role>> GetAll()
    {
        return dbContext.Roles.ToList();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Role role)
    {
        await dbContext.Roles.AddAsync(role);
        await dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPatch]
    public async Task<IActionResult> Update(Role role)
    {
        dbContext.Roles.Update(role);
        await dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await dbContext.Roles
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync();
        return Ok();
    }
}
