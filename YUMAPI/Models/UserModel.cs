// ============================================================
//  Models/UserModel.cs
// ============================================================

using System;
using System.Collections.Generic;

namespace YUMAPI.Models
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; } // Hashé en MD5

        // Date à laquelle le compte a été créé
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Dernière recette consultée
        public MealListItem DerniereRecette { get; set; } = null;

        public List<MealListItem> Favoris { get; set; } = new List<MealListItem>();

        // Couleur accent choisie par l'utilisateur (sauvegardée dans son profil)
        public string CouleurTheme { get; set; } = "#FF6B35";
    }
}