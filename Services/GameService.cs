using Proyecto1.Models;
using Proyecto1.Models.Enums;
using Proyecto1.Infrastructure.Repositories.Interfaces;
using Proyecto1.DTOs.Games;
using Proyecto1.DTOs.Moves;
using Proyecto1.Infrastructure.Repositories;
using Proyecto1.Services.Interfaces;

namespace Proyecto1.Services
{
     public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IBoardService _boardService;
        private readonly IDiceService _diceService;
        private readonly ITurnService _turnService;
        private readonly ILogger<GameService> _logger;

        public GameService(
            IGameRepository gameRepository,
            IRoomRepository roomRepository,
            IPlayerRepository playerRepository,
            IBoardService boardService,
            IDiceService diceService,
            ITurnService turnService,
            ILogger<GameService> logger)
        {
            _gameRepository = gameRepository;
            _roomRepository = roomRepository;
            _playerRepository = playerRepository;
            _boardService = boardService;
            _diceService = diceService;
            _turnService = turnService;
            _logger = logger;
        }

        public async Task<Game> CreateGameAsync(int roomId)
        {
            var room = await _roomRepository.GetByIdWithPlayersAsync(roomId);
            if (room == null)
                throw new InvalidOperationException("Room not found");

            // ✅ Busca players que están en la room SIN GameId
            var playersInRoom = room.Players
                .Where(p => p.RoomId == roomId && !p.GameId.HasValue)
                .ToList();

            if (playersInRoom.Count < 2)
                throw new InvalidOperationException($"Need at least 2 players (current: {playersInRoom.Count})");

            var game = new Game
            {
                RoomId = roomId,
                Status = GameStatus.InProgress,
                CurrentTurnPlayerIndex = 0,
                CurrentTurnPhase = TurnPhase.WaitingForDice
            };

            await _gameRepository.CreateAsync(game);

            // Generar tablero
            var board = _boardService.GenerateBoard(game.Id);
            game.Board = board;

            // ✅ AHORA SÍ asigna el GameId a los players
            int turnOrder = 0;
            foreach (var player in playersInRoom)
            {
                player.GameId = game.Id;
                player.TurnOrder = turnOrder++;
                player.Status = PlayerStatus.Playing;
                await _playerRepository.UpdateAsync(player);
            }

            room.Status = RoomStatus.InGame;
            await _roomRepository.UpdateAsync(room);

            await _gameRepository.UpdateAsync(game);

            return game;
        }
        public async Task<GameStateDto> GetGameStateAsync(int gameId)
        {
            var game = await _gameRepository.GetByIdWithDetailsAsync(gameId);
            if (game == null)
                throw new InvalidOperationException("Game not found");

            var currentPlayer = _turnService.GetCurrentPlayer(game);

            return new GameStateDto
            {
                GameId = game.Id,
                Status = game.Status.ToString(),
                CurrentTurnPlayerIndex = game.CurrentTurnPlayerIndex,
                CurrentTurnPhase = game.CurrentTurnPhase.ToString(),
                CurrentPlayerId = currentPlayer?.Id,
                CurrentPlayerName = currentPlayer?.User.Username,
                Players = game.Players.Select(p => new PlayerStateDto
                {
                    PlayerId = p.Id,
                    UserId = p.UserId,
                    Username = p.User.Username,
                    Position = p.Position,
                    TurnOrder = p.TurnOrder,
                    Status = p.Status.ToString(),
                    IsCurrentTurn = p.Id == currentPlayer?.Id
                }).ToList(),
                Board = new BoardStateDto
                {
                    Size = game.Board.Size,
                    Snakes = game.Board.Snakes.Select(s => new SnakeDto
                    {
                        HeadPosition = s.HeadPosition,
                        TailPosition = s.TailPosition
                    }).ToList(),
                    Ladders = game.Board.Ladders.Select(l => new LadderDto
                    {
                        BottomPosition = l.BottomPosition,
                        TopPosition = l.TopPosition
                    }).ToList()
                },
                WinnerPlayerId = game.WinnerPlayerId,
                WinnerName = game.Players.FirstOrDefault(p => p.Id == game.WinnerPlayerId)?.User.Username
            };
        }

        public async Task<MoveResultDto> RollDiceAndMoveAsync(int gameId, int userId)
        {
            var game = await _gameRepository.GetByIdWithDetailsAsync(gameId);
            if (game == null)
                throw new InvalidOperationException("Game not found");

            if (game.Status != GameStatus.InProgress)
                throw new InvalidOperationException("Game is not in progress");

            var player = await _playerRepository.GetByGameAndUserAsync(gameId, userId);
            if (player == null)
                throw new InvalidOperationException("Player not in game");

            if (!_turnService.IsPlayerTurn(game, player.Id))
                throw new InvalidOperationException("Not your turn");

            if (game.CurrentTurnPhase != TurnPhase.WaitingForDice)
                throw new InvalidOperationException("Dice already rolled");

            // Tirar dado
            int diceValue = _diceService.RollDice();
            int fromPosition = player.Position;
            int toPosition = Math.Min(fromPosition + diceValue, game.Board.Size);

            var result = new MoveResultDto
            {
                DiceValue = diceValue,
                FromPosition = fromPosition,
                ToPosition = toPosition,
                IsWinner = false
            };

            // Si se pasa del tamaño del tablero, no se mueve
            if (fromPosition + diceValue > game.Board.Size)
            {
                result.ToPosition = fromPosition;
                result.FinalPosition = fromPosition;
                result.Message = "Roll exceeds board size, stay in place";
                _turnService.AdvanceTurn(game);
                await _gameRepository.UpdateAsync(game);
                return result;
            }

            player.Position = toPosition;

            // Verificar serpientes y escaleras
            var snakeDest = _boardService.GetSnakeDestination(game.Board, toPosition);
            var ladderDest = _boardService.GetLadderDestination(game.Board, toPosition);

            if (snakeDest.HasValue)
            {
                player.Position = snakeDest.Value;
                result.FinalPosition = snakeDest.Value;
                result.SpecialEvent = "Snake";
                result.Message = $"Hit a snake! Moved from {toPosition} to {snakeDest.Value}";
            }
            else if (ladderDest.HasValue)
            {
                player.Position = ladderDest.Value;
                result.FinalPosition = ladderDest.Value;
                result.SpecialEvent = "Ladder";
                result.Message = $"Climbed a ladder! Moved from {toPosition} to {ladderDest.Value}";
            }
            else
            {
                result.FinalPosition = toPosition;
                result.Message = "Normal move";
            }

            // Verificar victoria
            if (player.Position >= game.Board.Size)
            {
                player.Status = PlayerStatus.Winner;
                game.Status = GameStatus.Finished;
                game.WinnerPlayerId = player.Id;
                game.FinishedAt = DateTime.UtcNow;
                result.IsWinner = true;
                result.Message = "🎉 You won!";
            }
            else
            {
                _turnService.AdvanceTurn(game);
            }

            await _playerRepository.UpdateAsync(player);
            await _gameRepository.UpdateAsync(game);

            return result;
        }

        public async Task SurrenderAsync(int gameId, int userId)
        {
            var game = await _gameRepository.GetByIdWithDetailsAsync(gameId);
            if (game == null)
                throw new InvalidOperationException("Game not found");

            var player = await _playerRepository.GetByGameAndUserAsync(gameId, userId);
            if (player == null)
                throw new InvalidOperationException("Player not in game");

            player.Status = PlayerStatus.Surrendered;
            await _playerRepository.UpdateAsync(player);

            var activePlayers = game.Players.Where(p => p.Status == PlayerStatus.Playing).ToList();
            if (activePlayers.Count == 1)
            {
                var winner = activePlayers.First();
                winner.Status = PlayerStatus.Winner;
                game.Status = GameStatus.Finished;
                game.WinnerPlayerId = winner.Id;
                game.FinishedAt = DateTime.UtcNow;
                await _playerRepository.UpdateAsync(winner);
            }

            await _gameRepository.UpdateAsync(game);
        }
    }
}