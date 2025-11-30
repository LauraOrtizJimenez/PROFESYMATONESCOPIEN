using Proyecto1.Models;

namespace Proyecto1.Services.Interfaces
{
    public interface IRoomService
    {
        /// <summary>
        /// Crea una sala nueva.
        /// </summary>
        /// <param name="name">Nombre de la sala.</param>
        /// <param name="maxPlayers">Máximo de jugadores.</param>
        /// <param name="creatorUserId">Id del usuario creador.</param>
        /// <param name="isPrivate">true = sala privada, false = pública.</param>
        /// <param name="accessCode">
        /// Código de acceso opcional para salas privadas.
        /// Puede ser null para públicas o si no quieres usar código.
        /// </param>
        Task<Room> CreateRoomAsync(
            string name,
            int maxPlayers,
            int creatorUserId,
            bool isPrivate,
            string? accessCode
        );

        /// <summary>
        /// Une un usuario a una sala.
        /// </summary>
        /// <param name="roomId">Id de la sala.</param>
        /// <param name="userId">Id del usuario.</param>
        /// <param name="accessCode">
        /// Código de acceso para salas privadas.
        /// Puede ser null para salas públicas.
        /// </param>
        Task<Player> JoinRoomAsync(
            int roomId,
            int userId,
            string? accessCode
        );

        Task<List<Room>> GetAvailableRoomsAsync();
        Task<Room?> GetRoomWithDetailsAsync(int roomId);
    }
}