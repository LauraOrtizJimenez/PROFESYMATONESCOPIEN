using System.ComponentModel.DataAnnotations;

namespace Proyecto1.DTOs.Lobby
{
    public class CreateRoomRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Range(2, 6)]
        public int MaxPlayers { get; set; } = 4;

        // 🔐 NUEVO: tipo de sala
        /// <summary>
        /// false = sala pública (aparece en el listado)
        /// true  = sala privada (puede ocultarse del listado / pedir código)
        /// </summary>
        public bool IsPrivate { get; set; } = false;

        /// <summary>
        /// Código opcional para unirse a la sala si es privada.
        /// Si no quieres usar contraseña, puedes dejarlo siempre null.
        /// </summary>
        public string? AccessCode { get; set; }
    }
}