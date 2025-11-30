using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Proyecto1.Models
{
    public class TokenSkin
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Nombre visible en la tienda.</summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Clave de color que usará el front (ej: "green", "blue").</summary>
        [MaxLength(50)]
        public string ColorKey { get; set; } = string.Empty;

        /// <summary>Clave de icono/cara (ej: "classic", "nerd", "angry").</summary>
        [MaxLength(50)]
        public string IconKey { get; set; } = string.Empty;

        /// <summary>Precio en monedas.</summary>
        public int PriceCoins { get; set; } = 0;

        /// <summary>Si la skin está activa / se muestra en la tienda.</summary>
        public bool IsActive { get; set; } = true;
        
        // Usuarios que la poseen
        public ICollection<UserTokenSkin> Owners { get; set; } = new List<UserTokenSkin>();
    }
}