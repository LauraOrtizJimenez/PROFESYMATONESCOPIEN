using System;
using Microsoft.Extensions.Logging;
using Proyecto1.DTOs.Games;
using Proyecto1.DTOs.Lobby;
using Proyecto1.DTOs.Moves;
using Proyecto1.Infrastructure.Repositories;
using Proyecto1.Infrastructure.Repositories.Interfaces;
using Proyecto1.Models;
using Proyecto1.Models.Enums;
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

        private const int COINS_REWARD_ON_WIN = 20;

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

        // ==========================================================
        // 🔥 LOBBY SUMMARY
        // ==========================================================
        public async Task<RoomSummaryDto> GetRoomSummaryAsync(int roomId)
        {
            var room = await _roomRepository.GetByIdWithPlayersAsync(roomId)
                ?? throw new InvalidOperationException($"Room {roomId} not found");

            return new RoomSummaryDto
            {
                Id = room.Id,
                Name = room.Name,
                Status = room.Status.ToString(),
                CreatedAt = room.CreatedAt,
                MaxPlayers = room.MaxPlayers,
                CurrentPlayers = room.Players.Count,
                PlayerNames = room.Players
                    .OrderBy(p => p.JoinedAt)
                    .Select(p => p.User.Username)
                    .ToList(),
                GameId = room.Game?.Id
            };
        }

        // ==========================================================
        // CREATE GAME
        // ==========================================================
        public async Task<Game> CreateGameAsync(int roomId)
        {
            var room = await _roomRepository.GetByIdWithPlayersAsync(roomId)
                ?? throw new InvalidOperationException("Room not found");

            var playersInRoom = room.Players
                .Where(p => p.RoomId == roomId && !p.GameId.HasValue)
                .ToList();

            if (playersInRoom.Count < 2)
                throw new InvalidOperationException("Need at least 2 players");

            var game = new Game
            {
                RoomId = roomId,
                Status = GameStatus.InProgress,
                CurrentTurnPlayerIndex = 0,
                CurrentTurnPhase = TurnPhase.WaitingForDice
            };

            await _gameRepository.CreateAsync(game);

            var board = _boardService.GenerateBoard(game.Id);
            game.Board = board;

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

        // ==========================================================
        // GET GAME STATE
        // ==========================================================
        public async Task<GameStateDto> GetGameStateAsync(int gameId)
        {
            var game = await _gameRepository.GetByIdWithDetailsAsync(gameId)
                ?? throw new InvalidOperationException("Game not found");

            var currentPlayer = _turnService.GetCurrentPlayer(game);

            return new GameStateDto
            {
                GameId = game.Id,
                Status = game.Status.ToString(),

                CurrentTurnPlayerIndex = game.CurrentTurnPlayerIndex,
                CurrentTurnPhase = game.CurrentTurnPhase.ToString(),

                CurrentPlayerId = currentPlayer?.Id,
                CurrentPlayerName = currentPlayer?.User.Username,

                Players = game.Players
                    .OrderBy(p => p.TurnOrder)
                    .Select(p => new PlayerGameDto
                    {
                        PlayerId = p.Id,
                        UserId = p.UserId,
                        Username = p.User.Username,
                        Position = p.Position,
                        TurnOrder = p.TurnOrder,
                        Status = p.Status.ToString(),
                        IsCurrentTurn = p.Id == currentPlayer?.Id,

                        TokenColorKey = p.User.SelectedTokenSkin?.ColorKey,
                        TokenIconKey = p.User.SelectedTokenSkin?.IconKey
                    })
                    .ToList(),

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
                WinnerName = game.Players
                    .FirstOrDefault(p => p.Id == game.WinnerPlayerId)
                    ?.User.Username
            };
        }

        // ==========================================================
        // ROLL DICE AND MOVE
        // ==========================================================
        public async Task<MoveResultDto> RollDiceAndMoveAsync(int gameId, int userId)
        {
            var game = await _gameRepository.GetByIdWithDetailsAsync(gameId)
                ?? throw new InvalidOperationException("Game not found");

            if (game.Status != GameStatus.InProgress)
                throw new InvalidOperationException("Game is not in progress");

            var player = await _playerRepository.GetByGameAndUserAsync(gameId, userId)
                ?? throw new InvalidOperationException("Player not in game");

            if (!_turnService.IsPlayerTurn(game, player.Id))
                throw new InvalidOperationException("Not your turn");

            if (game.CurrentTurnPhase != TurnPhase.WaitingForDice)
                throw new InvalidOperationException("Dice already rolled");

            int diceValue = _diceService.RollDice();
            int fromPosition = player.Position;
            int toPosition = Math.Min(fromPosition + diceValue, game.Board.Size);

            var result = new MoveResultDto
            {
                DiceValue = diceValue,
                FromPosition = fromPosition,
                ToPosition = toPosition
            };

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

            var boardService = _boardService as BoardService;
            var snakeDest = _boardService.GetSnakeDestination(game.Board, toPosition);
            var ladderDest = _boardService.GetLadderDestination(game.Board, toPosition);

            if (snakeDest.HasValue && boardService != null)
            {
                var profesorQuestion = boardService.GetProfesorQuestion(toPosition);

                if (profesorQuestion != null)
                {
                    result.RequiresProfesorAnswer = true;
                    result.ProfesorQuestion = profesorQuestion;
                    result.FinalPosition = toPosition;
                    result.SpecialEvent = "Profesor";
                    result.Message = $"Has caído con el profesor {profesorQuestion.Profesor}!";

                    await _playerRepository.UpdateAsync(player);
                    await _gameRepository.UpdateAsync(game);
                    return result;
                }

                player.Position = snakeDest.Value;
                result.FinalPosition = snakeDest.Value;
                result.SpecialEvent = "Profesor";
                result.Message = $"Profesor! Mueves de {toPosition} a {snakeDest.Value}";
            }
            else if (ladderDest.HasValue)
            {
                player.Position = ladderDest.Value;
                result.FinalPosition = ladderDest.Value;
                result.SpecialEvent = "Matón";
                result.Message = $"Matón! Subes de {toPosition} a {ladderDest.Value}";
            }
            else
            {
                result.FinalPosition = toPosition;
                result.Message = "Normal move";
            }

            if (player.Position >= game.Board.Size)
            {
                player.Status = PlayerStatus.Winner;
                game.Status = GameStatus.Finished;
                game.WinnerPlayerId = player.Id;
                game.FinishedAt = DateTime.UtcNow;

                if (player.User != null)
                {
                    player.User.GamesPlayed += 1;
                    player.User.GamesWon += 1;
                    player.User.Coins += COINS_REWARD_ON_WIN;
                }

                result.IsWinner = true;
                result.Message = "🎉 ¡Ganaste!";
            }
            else if (!result.RequiresProfesorAnswer)
            {
                _turnService.AdvanceTurn(game);
            }

            await _playerRepository.UpdateAsync(player);
            await _gameRepository.UpdateAsync(game);

            return result;
        }

        // ==========================================================
        // PROFESOR
        // ==========================================================
        public async Task<ProfesorQuestionDto?> GetProfesorQuestionAsync(int gameId, int userId)
        {
            var player = await _playerRepository.GetByGameAndUserAsync(gameId, userId);
            if (player == null) return null;

            var boardService = _boardService as BoardService;
            return boardService?.GetProfesorQuestion(player.Position);
        }

        public async Task<MoveResultDto> AnswerProfesorQuestionAsync(int gameId, int userId, string answer)
        {
            var player = await _playerRepository.GetByGameAndUserAsync(gameId, userId)
                ?? throw new InvalidOperationException("Player not in game");

            var boardService = _boardService as BoardService
                ?? throw new InvalidOperationException("Board service unavailable");

            if (!player.GameId.HasValue)
                throw new InvalidOperationException("Player is not attached to any game");

            var game = await _gameRepository.GetByIdWithDetailsAsync(player.GameId.Value)
                ?? throw new InvalidOperationException("Game not found");

            var result = new MoveResultDto
            {
                FromPosition = player.Position,
                FinalPosition = player.Position
            };

            bool isCorrect = boardService.ValidateProfesorAnswer(player.Position, answer);

            if (isCorrect)
            {
                result.Message = "¡Correcto! Mantienes tu posición.";
            }
            else
            {
                var snakeDest = boardService.GetSnakeDestination(game.Board, player.Position);
                if (snakeDest.HasValue)
                {
                    player.Position = snakeDest.Value;
                    result.FinalPosition = snakeDest.Value;
                }
                result.Message = "Incorrecto. El profesor te hace bajar.";
            }

            _turnService.AdvanceTurn(game);
            await _playerRepository.UpdateAsync(player);
            await _gameRepository.UpdateAsync(game);

            return result;
        }

        // ==========================================================
        // 🔥 SURRENDER (COMPLETO Y CORRECTO)
        // ==========================================================
        public async Task SurrenderAsync(int gameId, int userId)
        {
            var game = await _gameRepository.GetByIdWithDetailsAsync(gameId)
                ?? throw new InvalidOperationException("Game not found");

            var player = await _playerRepository.GetByGameAndUserAsync(gameId, userId)
                ?? throw new InvalidOperationException("Player not in game");

            if (game.Status == GameStatus.Finished)
                return;

            if (player.Status == PlayerStatus.Surrendered ||
                player.Status == PlayerStatus.Winner)
                return;

            if (player.User != null)
                player.User.GamesPlayed++;

            player.Status = PlayerStatus.Surrendered;
            await _playerRepository.UpdateAsync(player);

            if (_turnService.IsPlayerTurn(game, player.Id))
            {
                _turnService.AdvanceTurn(game);
            }

            var activePlayers = game.Players
                .Where(p => p.Status == PlayerStatus.Playing)
                .ToList();

            if (activePlayers.Count == 1)
            {
                var winner = activePlayers.First();
                winner.Status = PlayerStatus.Winner;

                game.Status = GameStatus.Finished;
                game.WinnerPlayerId = winner.Id;
                game.FinishedAt = DateTime.UtcNow;

                if (winner.User != null)
                {
                    winner.User.GamesPlayed += 1;
                    winner.User.GamesWon += 1;
                    winner.User.Coins += COINS_REWARD_ON_WIN;
                }

                await _playerRepository.UpdateAsync(winner);
            }

            await _gameRepository.UpdateAsync(game);
        }
    }
}