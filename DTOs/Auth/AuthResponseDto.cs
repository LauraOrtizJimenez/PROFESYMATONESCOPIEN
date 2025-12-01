namespace Proyecto1.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int Wins { get; set; }  // 👈 AGREGAR ESTA LÍNEA
    }
}