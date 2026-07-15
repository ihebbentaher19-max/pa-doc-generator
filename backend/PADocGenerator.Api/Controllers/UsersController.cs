using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PADocGenerator.Api.Data;
using PADocGenerator.Api.Models.Entities;

namespace PADocGenerator.Api.Controllers;

public record UserSummaryDto(Guid Id, string FullName, string Email, string Role, bool IsActive, DateTime CreatedAtUtc);
public record ChangeUserRoleDto(string NewRole);
public record SetUserActiveDto(bool IsActive);

/// <summary>
/// Module de gestion des rôles (section 6), volet administration : liste des
/// utilisateurs, changement de rôle, activation/désactivation de compte.
/// L'ensemble de ce contrôleur est réservé au rôle Administrateur.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrateur")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserSummaryDto>>> GetAll(CancellationToken ct)
    {
        var users = await _db.Users
            .OrderBy(u => u.FullName)
            .Select(u => new UserSummaryDto(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAtUtc))
            .ToListAsync(ct);

        return Ok(users);
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, ChangeUserRoleDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<UserRole>(dto.NewRole, ignoreCase: true, out var role))
            return BadRequest(new { message = "Rôle invalide. Valeurs autorisées : Utilisateur, Administrateur." });

        var user = await _db.Users.FindAsync([id], ct);
        if (user is null) return NotFound();

        user.Role = role;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, SetUserActiveDto dto, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync([id], ct);
        if (user is null) return NotFound();

        user.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
