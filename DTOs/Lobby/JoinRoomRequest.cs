using System.ComponentModel.DataAnnotations;

namespace Proyecto1.DTOs.Lobby
{
    public class JoinRoomRequest
    {
        [Required]
        public int RoomId { get; set; }

        // 🔐 NUEVO: código opcional para salas privadas
        /// <summary>
        /// Código de acceso requerido si la sala es privada.
        /// Puede ser null o vacío para salas públicas.
        /// </summary>
        public string? AccessCode { get; set; }
    }
}