using System.Collections.Generic;

namespace Proyecto1.DTOs.Skins
{
    public class TokenSkinDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ColorKey { get; set; } = string.Empty;
        public string IconKey { get; set; } = string.Empty;
        public int PriceCoins { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateTokenSkinDto
    {
        public string Name { get; set; } = string.Empty;
        public string ColorKey { get; set; } = string.Empty;
        public string IconKey { get; set; } = string.Empty;
        public int PriceCoins { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UserSkinsDto
    {
        public int Coins { get; set; }
        public int? SelectedTokenSkinId { get; set; }
        public List<TokenSkinDto> OwnedSkins { get; set; } = new();
    }
}