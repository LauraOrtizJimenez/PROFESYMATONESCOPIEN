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

        public async Task<Room> CreateRoomAsync(string name, int maxPlayers, int creatorUserId)
        {
            var room = new Room
            {
                Name = name,
                MaxPlayers = maxPlayers,
                CurrentPlayers = 0,
                CreatorUserId = creatorUserId,
                Status = RoomStatus.Open
            };

            return await _roomRepository.CreateAsync(room);
        }

        public async Task<Player> JoinRoomAsync(int roomId, int userId)
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

            // ✅ NO asignes GameId, déjalo null
            var player = new Player
            {
                UserId = userId,
                RoomId = roomId,
                GameId = null, // ✅ NULL hasta que se cree el game
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