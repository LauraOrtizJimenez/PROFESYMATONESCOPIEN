using Proyecto1.Models;
using Proyecto1.Services.Interfaces;

namespace Proyecto1.Services
{
    public class BoardService : IBoardService
    {
        private readonly Random _random;

        public BoardService()
        {
            _random = new Random();
        }

        public Board GenerateBoard(int gameId, int size = 100)
        {
            var board = new Board
            {
                GameId = gameId,
                Size = size,
                Snakes = new List<Snake>(),
                Ladders = new List<Ladder>()
            };

            // Posiciones ocupadas
            var occupiedPositions = new HashSet<int> { 1, size }; // Start y End

            // Generar serpientes (8-12)
            int snakeCount = _random.Next(8, 13);
            for (int i = 0; i < snakeCount; i++)
            {
                var snake = GenerateSnake(size, occupiedPositions);
                if (snake != null)
                {
                    board.Snakes.Add(snake);
                    occupiedPositions.Add(snake.HeadPosition);
                    occupiedPositions.Add(snake.TailPosition);
                }
            }

            // Generar escaleras (8-12)
            int ladderCount = _random.Next(8, 13);
            for (int i = 0; i < ladderCount; i++)
            {
                var ladder = GenerateLadder(size, occupiedPositions);
                if (ladder != null)
                {
                    board.Ladders.Add(ladder);
                    occupiedPositions.Add(ladder.BottomPosition);
                    occupiedPositions.Add(ladder.TopPosition);
                }
            }

            return board;
        }

        private Snake? GenerateSnake(int boardSize, HashSet<int> occupiedPositions)
        {
            int attempts = 0;
            while (attempts < 50)
            {
                int head = _random.Next(boardSize / 2, boardSize);
                int tail = _random.Next(2, head - 10);

                if (!occupiedPositions.Contains(head) && !occupiedPositions.Contains(tail))
                {
                    return new Snake
                    {
                        HeadPosition = head,
                        TailPosition = tail
                    };
                }
                attempts++;
            }
            return null;
        }

        private Ladder? GenerateLadder(int boardSize, HashSet<int> occupiedPositions)
        {
            int attempts = 0;
            while (attempts < 50)
            {
                int bottom = _random.Next(2, boardSize / 2);
                int top = _random.Next(bottom + 10, boardSize);

                if (!occupiedPositions.Contains(bottom) && !occupiedPositions.Contains(top))
                {
                    return new Ladder
                    {
                        BottomPosition = bottom,
                        TopPosition = top
                    };
                }
                attempts++;
            }
            return null;
        }

        public bool ValidatePosition(int position, int boardSize)
        {
            return position >= 0 && position <= boardSize;
        }

        public int? GetSnakeDestination(Board board, int position)
        {
            var snake = board.Snakes.FirstOrDefault(s => s.HeadPosition == position);
            return snake?.TailPosition;
        }

        public int? GetLadderDestination(Board board, int position)
        {
            var ladder = board.Ladders.FirstOrDefault(l => l.BottomPosition == position);
            return ladder?.TopPosition;
        }
    }
}
