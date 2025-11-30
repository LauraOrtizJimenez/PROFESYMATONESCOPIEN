using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Proyecto1.DTOs.Skins;
using Proyecto1.Infrastructure.Data;
using Proyecto1.Models;

namespace Proyecto1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkinsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SkinsController> _logger;

        public SkinsController(AppDbContext context, ILogger<SkinsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ------------------------------------------------------------------
        // Helper: obtener usuario actual desde el token (por Id)
        // ------------------------------------------------------------------
        private async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                var idClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier) ??
                    User.FindFirst("sub") ??
                    User.FindFirst("userId");

                if (idClaim == null) return null;
                if (!int.TryParse(idClaim.Value, out var userId)) return null;

                return await _context.Users
                    .Include(u => u.OwnedTokenSkins)
                        .ThenInclude(uts => uts.TokenSkin)
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading current user from claims");
                return null;
            }
        }

        // ------------------------------------------------------------------
        // Helper: saber si el usuario actual es el admin (username == "admin")
        // ------------------------------------------------------------------
        private async Task<bool> IsCurrentUserAdminAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;

            // Aquí tu convención: usuario admin
            return string.Equals(user.Username, "admin", StringComparison.OrdinalIgnoreCase);
        }

        private static TokenSkinDto MapToDto(TokenSkin skin) => new TokenSkinDto
        {
            Id = skin.Id,
            Name = skin.Name,
            ColorKey = skin.ColorKey,
            IconKey = skin.IconKey,
            PriceCoins = skin.PriceCoins,
            IsActive = skin.IsActive
        };

        // ==============================================================
        // GET api/skins   -> lista de skins activas para la tienda
        // ==============================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<TokenSkinDto>>> GetAllSkins()
        {
            var skins = await _context.TokenSkins
                .Where(s => s.IsActive)
                .OrderBy(s => s.PriceCoins)
                .ThenBy(s => s.Name)
                .ToListAsync();

            var dto = skins.Select(MapToDto).ToList();
            return Ok(dto);
        }

        // ==============================================================
        // GET api/skins/user  -> monedas + skins que posee el usuario
        // ==============================================================
        [HttpGet("user")]
        [Authorize]
        public async Task<ActionResult<UserSkinsDto>> GetMySkins()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Usuario no encontrado" });

            var owned = user.OwnedTokenSkins
                .Select(o => MapToDto(o.TokenSkin))
                .OrderBy(s => s.PriceCoins)
                .ThenBy(s => s.Name)
                .ToList();

            var result = new UserSkinsDto
            {
                Coins = user.Coins,
                SelectedTokenSkinId = user.SelectedTokenSkinId,
                OwnedSkins = owned
            };

            return Ok(result);
        }

        // ==============================================================
        // POST api/skins  -> crear nueva skin (solo admin)
        // body: CreateTokenSkinDto
        // ==============================================================
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<TokenSkinDto>> CreateSkin([FromBody] CreateTokenSkinDto dto)
        {
            if (!await IsCurrentUserAdminAsync())
            {
                return Forbid("Solo el usuario admin puede crear skins");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Name is required" });

            var exists = await _context.TokenSkins
                .AnyAsync(s => s.Name == dto.Name);

            if (exists)
                return BadRequest(new { message = "Ya existe una skin con ese nombre" });

            var skin = new TokenSkin
            {
                Name = dto.Name.Trim(),
                ColorKey = dto.ColorKey?.Trim() ?? string.Empty,
                IconKey = dto.IconKey?.Trim() ?? string.Empty,
                PriceCoins = dto.PriceCoins,
                IsActive = dto.IsActive
            };

            _context.TokenSkins.Add(skin);
            await _context.SaveChangesAsync();

            return Ok(MapToDto(skin));
        }

        // ==============================================================
        // POST api/skins/purchase/{skinId}  -> comprar skin con monedas
        // ==============================================================
        [HttpPost("purchase/{skinId:int}")]
        [Authorize]
        public async Task<ActionResult> PurchaseSkin(int skinId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Usuario no encontrado" });

            var skin = await _context.TokenSkins
                .FirstOrDefaultAsync(s => s.Id == skinId && s.IsActive);

            if (skin == null)
                return NotFound(new { message = "Skin no encontrada" });

            var alreadyOwned = await _context.UserTokenSkins
                .AnyAsync(uts => uts.UserId == user.Id && uts.TokenSkinId == skin.Id);

            if (alreadyOwned)
                return BadRequest(new { message = "Ya tienes esta skin" });

            if (user.Coins < skin.PriceCoins)
                return BadRequest(new { message = "No tienes suficientes monedas" });

            user.Coins -= skin.PriceCoins;

            var link = new UserTokenSkin
            {
                UserId = user.Id,
                TokenSkinId = skin.Id,
                PurchasedAt = DateTime.UtcNow
            };

            _context.UserTokenSkins.Add(link);

            // Si no tenía ninguna seleccionada, seleccionamos esta por defecto
            if (!user.SelectedTokenSkinId.HasValue)
            {
                user.SelectedTokenSkinId = skin.Id;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Skin comprada correctamente",
                coins = user.Coins,
                selectedTokenSkinId = user.SelectedTokenSkinId
            });
        }

        // ==============================================================
        // POST api/skins/select/{skinId}  -> seleccionar skin que ya posees
        // ==============================================================
        [HttpPost("select/{skinId:int}")]
        [Authorize]
        public async Task<ActionResult> SelectSkin(int skinId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Usuario no encontrado" });

            var ownsIt = await _context.UserTokenSkins
                .AnyAsync(uts => uts.UserId == user.Id && uts.TokenSkinId == skinId);

            if (!ownsIt)
                return BadRequest(new { message = "No posees esta skin" });

            var skinExists = await _context.TokenSkins
                .AnyAsync(s => s.Id == skinId && s.IsActive);

            if (!skinExists)
                return NotFound(new { message = "Skin no encontrada" });

            user.SelectedTokenSkinId = skinId;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Skin seleccionada",
                selectedTokenSkinId = user.SelectedTokenSkinId
            });
        }
    }
}