using Proyecto1.Models;
using Proyecto1.Models.Enums;
using Proyecto1.Infrastructure.Repositories.Interfaces;
using Proyecto1.Services.Interfaces;

namespace Proyecto1.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPlayerRepository _playerRepository;

        public RoomService(
            IRoomRepository roomRepository,
            IUserRepository userRepository,
            IPlayerRepository playerRepository)
        {
            _roomRepository = roomRepository;
            _userRepository = userRepository;
            _playerRepository = playerRepository;
        }

        // ==========================================================
        // Crear sala (pública / privada)
        // ==========================================================
        public async Task<Room> CreateRoomAsync(
            string name,
            int maxPlayers,
            int creatorUserId,
            bool isPrivate,
            string? accessCode)
        {
            if (isPrivate && string.IsNullOrWhiteSpace(accessCode))
            {
                // Si quieres que el código sea opcional incluso para privadas,
                // puedes eliminar este bloque o solo loguear un warning.
                throw new InvalidOperationException("Access code is required for private rooms.");
            }

            var room = new Room
            {
                Name = name,
                MaxPlayers = maxPlayers,
                CurrentPlayers = 0,
                CreatorUserId = creatorUserId,
                Status = RoomStatus.Open,

                IsPrivate = isPrivate,
                AccessCode = isPrivate ? accessCode : null
            };

            return await _roomRepository.CreateAsync(room);
        }

        // ==========================================================
        // Unirse a sala (valida código en salas privadas)
        // ==========================================================
        public async Task<Player> JoinRoomAsync(int roomId, int userId, string? accessCode)
        {
            var room = await _roomRepository.GetByIdWithPlayersAsync(roomId);
            if (room == null)
                throw new InvalidOperationException("Room not found");

            if (room.Status != RoomStatus.Open && room.Status != RoomStatus.Full)
                throw new InvalidOperationException("Room is not open");

            if (room.CurrentPlayers >= room.MaxPlayers)
                throw new InvalidOperationException("Room is full");

            if (room.Players.Any(p => p.UserId == userId))
                throw new InvalidOperationException("User already in room");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            // Validación de acceso para salas privadas
            var isCreator = room.CreatorUserId == userId;

            if (room.IsPrivate && !isCreator)
            {
                if (string.IsNullOrWhiteSpace(room.AccessCode))
                {
                    throw new InvalidOperationException("This room is misconfigured: private but has no access code.");
                }

                if (string.IsNullOrWhiteSpace(accessCode) ||
                    !string.Equals(room.AccessCode, accessCode, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Invalid access code for this room.");
                }
            }

            // No asignes GameId, déjalo null
            var player = new Player
            {
                UserId = userId,
                RoomId = roomId,
                GameId = null,
                Position = 0,
                TurnOrder = room.CurrentPlayers,
                Status = PlayerStatus.Waiting
            };

            await _playerRepository.CreateAsync(player);

            room.CurrentPlayers++;
            if (room.CurrentPlayers >= room.MaxPlayers)
                room.Status = RoomStatus.Full;

            await _roomRepository.UpdateAsync(room);

            return player;
        }

        // ==========================================================
        // Listar salas disponibles (el filtro de públicas lo hace el controller)
        // ==========================================================
        public async Task<List<Room>> GetAvailableRoomsAsync()
        {
            return await _roomRepository.GetAvailableRoomsAsync();
        }

        public async Task<Room?> GetRoomWithDetailsAsync(int roomId)
        {
            return await _roomRepository.GetByIdWithPlayersAsync(roomId);
        }
    }
}