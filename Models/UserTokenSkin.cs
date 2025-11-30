using System;

namespace Proyecto1.Models
{
    public class UserTokenSkin
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int TokenSkinId { get; set; }
        public TokenSkin TokenSkin { get; set; } = null!;

        /// <summary>
        /// Fecha en que se compró la skin.
        /// </summary>
        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    }
}